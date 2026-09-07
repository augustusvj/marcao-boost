type Bindings = Cloudflare.Env & { BOOTSTRAP_KEY: string };
type JsonObject = Record<string, unknown>;
interface UserRow {
  id: string; name: string; email: string; password_salt: string; password_hash: string;
  role: "client" | "admin"; status: "pending" | "active" | "blocked"; permissions: string;
  force_password_reset: number; failed_attempts: number; locked_until: string | null;
  created_at: string; updated_at: string; last_login_at: string | null;
}

const headers = {
  "content-type": "application/json; charset=utf-8",
  "access-control-allow-origin": "*",
  "access-control-allow-headers": "authorization,content-type,x-bootstrap-key",
  "access-control-allow-methods": "GET,POST,PATCH,OPTIONS",
  "cache-control": "no-store",
  "x-content-type-options": "nosniff",
};

const json = (data: unknown, status = 200) => new Response(JSON.stringify(data), { status, headers });
const now = () => new Date().toISOString();
const id = () => crypto.randomUUID();
const bytesToB64 = (b: Uint8Array) => btoa(String.fromCharCode(...b));
const b64ToBytes = (s: string) => Uint8Array.from(atob(s), c => c.charCodeAt(0));

async function sha256(value: string) {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return bytesToB64(new Uint8Array(digest));
}

async function passwordHash(password: string, saltB64?: string) {
  const salt = saltB64 ? b64ToBytes(saltB64) : crypto.getRandomValues(new Uint8Array(16));
  const key = await crypto.subtle.importKey("raw", new TextEncoder().encode(password), "PBKDF2", false, ["deriveBits"]);
  const bits = await crypto.subtle.deriveBits({ name: "PBKDF2", hash: "SHA-256", salt, iterations: 100000 }, key, 256);
  return { salt: bytesToB64(salt), hash: bytesToB64(new Uint8Array(bits)) };
}

function safeUser(row: UserRow) {
  return { id: row.id, name: row.name, email: row.email, role: row.role, status: row.status, permissions: JSON.parse(row.permissions || "[]"), forcePasswordReset: !!row.force_password_reset, createdAt: row.created_at, lastLoginAt: row.last_login_at };
}

async function body(req: Request): Promise<JsonObject> {
  if (!req.headers.get("content-type")?.includes("application/json")) throw new Error("JSON_REQUIRED");
  const size = Number(req.headers.get("content-length") || "0");
  if (size > 16384) throw new Error("BODY_TOO_LARGE");
  return await req.json<JsonObject>();
}

async function auth(req: Request, env: Bindings, admin = false) {
  const raw = (req.headers.get("authorization") || "").replace(/^Bearer\s+/i, "");
  if (!raw) return null;
  const tokenHash = await sha256(raw);
  const row = await env.DB.prepare(`SELECT u.* FROM sessions s JOIN users u ON u.id=s.user_id WHERE s.token_hash=? AND s.expires_at>? AND u.status='active'`).bind(tokenHash, now()).first<UserRow>();
  if (!row || (admin && row.role !== "admin")) return null;
  return row;
}

async function issueSession(env: Bindings, userId: string) {
  const raw = bytesToB64(crypto.getRandomValues(new Uint8Array(32))).replace(/[+/=]/g, "");
  const expires = new Date(Date.now() + 30 * 86400000).toISOString();
  await env.DB.prepare("INSERT INTO sessions(token_hash,user_id,expires_at,created_at) VALUES(?,?,?,?)").bind(await sha256(raw), userId, expires, now()).run();
  return { token: raw, expiresAt: expires };
}

async function audit(env: Bindings, actor: string | null, target: string | null, action: string, details = "") {
  await env.DB.prepare("INSERT INTO audit_logs(actor_user_id,target_user_id,action,details,created_at) VALUES(?,?,?,?,?)").bind(actor, target, action, details, now()).run();
}

async function secretsEqual(a: string, b: string) {
  const [ah, bh] = await Promise.all([sha256(a), sha256(b)]); let diff = 0;
  for (let i = 0; i < ah.length; i++) diff |= ah.charCodeAt(i) ^ bh.charCodeAt(i);
  return diff === 0;
}

export default {
  async fetch(req: Request, env: Bindings): Promise<Response> {
    if (req.method === "OPTIONS") return new Response(null, { status: 204, headers });
    const url = new URL(req.url); const path = url.pathname.replace(/\/$/, "") || "/";
    try {
      if (path === "/" && req.method === "GET") return json({ service: "Marcão Boost", ok: true, version: env.APP_VERSION });

      if (path === "/auth/register" && req.method === "POST") {
        const data = await body(req); const name = String(data.name || "").trim(); const email = String(data.email || "").trim().toLowerCase(); const password = String(data.password || "");
        if (name.length < 2 || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email) || password.length < 10) return json({ error: "Dados inválidos. Use nome, e-mail válido e senha com 10 ou mais caracteres." }, 400);
        const exists = await env.DB.prepare("SELECT id FROM users WHERE email=?").bind(email).first(); if (exists) return json({ error: "Este e-mail já está cadastrado." }, 409);
        const pass = await passwordHash(password); const userId = id(); const time = now();
        await env.DB.prepare("INSERT INTO users(id,name,email,password_salt,password_hash,created_at,updated_at) VALUES(?,?,?,?,?,?,?)").bind(userId, name, email, pass.salt, pass.hash, time, time).run();
        await audit(env, null, userId, "register"); return json({ ok: true, status: "pending", message: "Cadastro enviado. Aguarde a aprovação do administrador." }, 201);
      }

      if (path === "/auth/login" && req.method === "POST") {
        const data = await body(req); const email = String(data.email || "").trim().toLowerCase(); const password = String(data.password || "");
        const user = await env.DB.prepare("SELECT * FROM users WHERE email=?").bind(email).first<UserRow>();
        if (!user) return json({ error: "E-mail ou senha incorretos." }, 401);
        if (user.locked_until && user.locked_until > now()) return json({ error: "Conta temporariamente bloqueada por tentativas. Tente novamente mais tarde." }, 429);
        const pass = await passwordHash(password, user.password_salt);
        if (!(await secretsEqual(pass.hash, user.password_hash))) {
          const attempts = Number(user.failed_attempts || 0) + 1; const lock = attempts >= 5 ? new Date(Date.now() + 15 * 60000).toISOString() : null;
          await env.DB.prepare("UPDATE users SET failed_attempts=?,locked_until=? WHERE id=?").bind(attempts >= 5 ? 0 : attempts, lock, user.id).run();
          return json({ error: "E-mail ou senha incorretos." }, 401);
        }
        if (user.status === "pending") return json({ error: "Seu cadastro ainda aguarda aprovação." }, 403);
        if (user.status === "blocked") return json({ error: "Esta conta foi bloqueada pelo administrador." }, 403);
        const session = await issueSession(env, user.id); await env.DB.prepare("UPDATE users SET failed_attempts=0,locked_until=NULL,last_login_at=?,updated_at=? WHERE id=?").bind(now(), now(), user.id).run();
        await audit(env, user.id, user.id, "login"); return json({ ...session, user: safeUser(user) });
      }

      if (path === "/auth/me" && req.method === "GET") { const user = await auth(req, env); return user ? json({ user: safeUser(user) }) : json({ error: "Sessão inválida." }, 401); }

      if (path === "/auth/change-password" && req.method === "POST") {
        const user = await auth(req, env); if (!user) return json({ error: "Sessão inválida." }, 401);
        const data = await body(req); const password = String(data.password || ""); if (password.length < 8) return json({ error: "A nova senha precisa ter 8 ou mais caracteres." }, 400);
        const pass = await passwordHash(password); await env.DB.batch([
          env.DB.prepare("UPDATE users SET password_salt=?,password_hash=?,force_password_reset=0,updated_at=? WHERE id=?").bind(pass.salt, pass.hash, now(), user.id),
          env.DB.prepare("DELETE FROM sessions WHERE user_id=?").bind(user.id)
        ]); await audit(env, user.id, user.id, "change_password"); return json({ ok: true });
      }

      if (path === "/admin/bootstrap" && req.method === "POST") {
        if (!env.BOOTSTRAP_KEY || !(await secretsEqual(req.headers.get("x-bootstrap-key") || "", env.BOOTSTRAP_KEY))) return json({ error: "Não autorizado." }, 401);
        if (await env.DB.prepare("SELECT id FROM users WHERE role='admin'").first()) return json({ error: "O proprietário já foi criado." }, 409);
        const data = await body(req); const name = String(data.name || "").trim(); const email = String(data.email || "").trim().toLowerCase(); const password = String(data.password || "");
        if (name.length < 2 || !email.includes("@") || password.length < 10) return json({ error: "Informe nome, e-mail e senha com ao menos 10 caracteres." }, 400);
        const pass = await passwordHash(password); const userId = id(); const time = now();
        await env.DB.prepare("INSERT INTO users(id,name,email,password_salt,password_hash,role,status,permissions,created_at,updated_at) VALUES(?,?,?,?,?,'admin','active','[\"admin\"]',?,?)").bind(userId, name, email, pass.salt, pass.hash, time, time).run();
        await audit(env, userId, userId, "bootstrap_admin"); return json({ ok: true });
      }

      const admin = await auth(req, env, true);
      if (path.startsWith("/admin/") && !admin) return json({ error: "Acesso administrativo necessário." }, 401);

      if (path === "/admin/users" && req.method === "GET") {
        const q = (url.searchParams.get("q") || "").trim(); const rows = q ? await env.DB.prepare("SELECT * FROM users WHERE role='client' AND (name LIKE ? OR email LIKE ?) ORDER BY created_at DESC LIMIT 250").bind(`%${q}%`, `%${q}%`).all<UserRow>() : await env.DB.prepare("SELECT * FROM users WHERE role='client' ORDER BY created_at DESC LIMIT 250").all<UserRow>();
        return json({ users: rows.results.map(safeUser) });
      }

      const match = path.match(/^\/admin\/users\/([^/]+)\/(status|permissions|reset-password)$/);
      if (match && req.method === "PATCH") {
        if (!admin) return json({ error: "Acesso administrativo necessário." }, 401);
        const targetId = match[1], action = match[2], data = await body(req); const target = await env.DB.prepare("SELECT * FROM users WHERE id=? AND role='client'").bind(targetId).first<UserRow>();
        if (!target) return json({ error: "Usuário não encontrado." }, 404);
        if (action === "status") { const status = String(data.status); if (!["active","pending","blocked"].includes(status)) return json({ error: "Status inválido." }, 400); await env.DB.batch([env.DB.prepare("UPDATE users SET status=?,updated_at=? WHERE id=?").bind(status, now(), targetId), env.DB.prepare("DELETE FROM sessions WHERE user_id=?").bind(targetId)]); await audit(env, admin.id, targetId, "status:" + status); return json({ ok: true }); }
        if (action === "permissions") { const allowed = ["optimize","cleanup","restore"]; const permissions = Array.isArray(data.permissions) ? data.permissions.filter((x: unknown) => typeof x === "string" && allowed.includes(x)) : []; await env.DB.prepare("UPDATE users SET permissions=?,updated_at=? WHERE id=?").bind(JSON.stringify(permissions), now(), targetId).run(); await audit(env, admin.id, targetId, "permissions", JSON.stringify(permissions)); return json({ ok: true }); }
        const password = String(data.temporaryPassword || ""); if (password.length < 10) return json({ error: "A senha temporária precisa ter 10 ou mais caracteres." }, 400); const pass = await passwordHash(password);
        await env.DB.batch([env.DB.prepare("UPDATE users SET password_salt=?,password_hash=?,force_password_reset=1,failed_attempts=0,locked_until=NULL,updated_at=? WHERE id=?").bind(pass.salt, pass.hash, now(), targetId), env.DB.prepare("DELETE FROM sessions WHERE user_id=?").bind(targetId)]); await audit(env, admin.id, targetId, "reset_password"); return json({ ok: true });
      }

      return json({ error: "Rota não encontrada." }, 404);
    } catch (error: unknown) {
      if (error instanceof Error && error.message === "JSON_REQUIRED") return json({ error: "Envie os dados em JSON." }, 415);
      if (error instanceof Error && error.message === "BODY_TOO_LARGE") return json({ error: "Solicitação muito grande." }, 413);
      console.error(error); return json({ error: "Não foi possível concluir a solicitação." }, 500);
    }
  }
};

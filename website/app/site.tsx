'use client';

import { FormEvent, useEffect, useState } from 'react';
import {
  ArrowDownToLine,
  Check,
  ChevronRight,
  Code2,
  Cpu,
  Gauge,
  LockKeyhole,
  Menu,
  MonitorCog,
  ShieldCheck,
  SlidersHorizontal,
  Sparkles,
  UserRound,
  X,
  Zap,
} from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { MatrixBackground } from './matrix-background';

type SiteSettings = {
  headline: string;
  status: string;
  downloadUrl: string;
  version: string;
};
type SessionUser = {
  id: string;
  name: string;
  email: string;
  role: 'admin' | 'client';
  status: 'pending' | 'active' | 'blocked';
  permissions: string[];
  forcePasswordReset: boolean;
};
type AuthResponse = {
  token: string;
  user: SessionUser;
};
const defaultSettings: SiteSettings = {
  headline: 'Seu Windows. Sem limites.',
  status: 'Versão 2.3 disponível',
  downloadUrl:
    'https://github.com/augustusvj/marcao-boost/releases/download/v2.3.0/MarcaoBoost-2.3-Cliente.zip',
  version: 'Versão 2.3',
};

const SESSION_KEY = 'marcao-boost-session';
const TOKEN_KEY = 'marcao-boost-token';
const ADMIN_EMAIL = 'avjvava@gmail.com';
const API_URL = 'https://marcao-boost-api.marcao-boost.workers.dev';

async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
  token?: string,
): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });
  const data = (await response.json().catch(() => ({}))) as {
    error?: string;
  };
  if (!response.ok) {
    throw new Error(data.error || 'Não foi possível conectar ao serviço.');
  }
  return data as T;
}

function Brand({ compact = false }: { compact?: boolean }) {
  return (
    <a href="#inicio" className="brand" aria-label="Marcão Boost — início">
      <span className="brand-mark">M</span>
      {!compact && (
        <span>
          Marcão <b>Boost</b>
        </span>
      )}
    </a>
  );
}

function GitHubMark({ size = 21 }: { size?: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="currentColor"
      aria-hidden="true"
    >
      <path d="M12 .7C5.63.7.5 5.83.5 12.2c0 5.1 3.29 9.43 7.86 10.96.58.1.79-.25.79-.56v-2.24c-3.2.7-3.88-1.36-3.88-1.36-.52-1.34-1.28-1.69-1.28-1.69-1.05-.72.08-.7.08-.7 1.16.08 1.77 1.19 1.77 1.19 1.03 1.77 2.7 1.26 3.36.96.1-.75.4-1.26.74-1.55-2.56-.29-5.25-1.28-5.25-5.69 0-1.26.45-2.28 1.19-3.09-.12-.29-.52-1.46.11-3.05 0 0 .97-.31 3.16 1.18a10.98 10.98 0 0 1 5.76 0c2.19-1.49 3.15-1.18 3.15-1.18.63 1.59.23 2.76.11 3.05.74.81 1.19 1.83 1.19 3.09 0 4.42-2.7 5.39-5.27 5.68.42.36.78 1.06.78 2.14v3.17c0 .31.21.67.79.56a11.51 11.51 0 0 0 7.85-10.96C23.5 5.83 18.37.7 12 .7Z" />
    </svg>
  );
}

function AccountDialog({
  user,
  onAuth,
  onLogout,
}: {
  user: SessionUser | null;
  onAuth: (user: SessionUser) => void;
  onLogout: () => void;
}) {
  const [message, setMessage] = useState('');
  const [open, setOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<'login' | 'register'>('login');
  const [busy, setBusy] = useState(false);

  const login = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const email = String(form.get('email')).trim().toLowerCase();
    const password = String(form.get('password'));
    setBusy(true);
    setMessage('Conectando à Gestão...');
    try {
      const result = await apiRequest<AuthResponse>('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      });
      localStorage.setItem(TOKEN_KEY, result.token);
      localStorage.setItem(SESSION_KEY, JSON.stringify(result.user));
      onAuth(result.user);
      setMessage('Login realizado e permissões sincronizadas.');
      setTimeout(() => setOpen(false), 550);
    } catch (error) {
      setMessage(
        error instanceof Error ? error.message : 'Não foi possível entrar.',
      );
    } finally {
      setBusy(false);
    }
  };

  const register = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const name = String(form.get('name')).trim();
    const email = String(form.get('email')).trim().toLowerCase();
    const password = String(form.get('password'));
    setBusy(true);
    setMessage('Enviando cadastro para a Gestão...');
    try {
      const result = await apiRequest<{ message?: string }>('/auth/register', {
        method: 'POST',
        body: JSON.stringify({ name, email, password }),
      });
      setMessage(
        result.message ||
          'Cadastro enviado. Aguarde a aprovação no Marcão Boost Gestão.',
      );
      event.currentTarget.reset();
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : 'Não foi possível enviar o cadastro.',
      );
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        setOpen(nextOpen);
        if (nextOpen) setMessage('');
      }}
    >
      <DialogTrigger className="nav-login">
        <UserRound size={16} /> {user ? user.name.split(' ')[0] : 'Entrar'}
      </DialogTrigger>
      <DialogContent className="account-dialog">
        <div className="auth-energy" aria-hidden="true">
          <span />
          <span />
          <span />
        </div>
        {user ? (
          <div className="account-summary">
            <span className="account-avatar">{user.name.charAt(0)}</span>
            <div>
              <DialogTitle>{user.name}</DialogTitle>
              <DialogDescription>{user.email}</DialogDescription>
            </div>
            <span className="role-badge">
              {user.role === 'admin' ? 'Administrador' : 'Cliente'}
            </span>
            <button
              className="secondary-button logout-button"
              type="button"
              onClick={() => {
                localStorage.removeItem(SESSION_KEY);
                localStorage.removeItem(TOKEN_KEY);
                onLogout();
                setOpen(false);
              }}
            >
              Sair da conta
            </button>
          </div>
        ) : (
          <>
            <DialogHeader>
              <div className="auth-signal" aria-hidden="true">
                <i />
                <i />
                <i />
              </div>
              <DialogTitle>Acesse sua conta</DialogTitle>
              <DialogDescription>
                Entre ou crie uma conta para acessar as áreas protegidas.
              </DialogDescription>
            </DialogHeader>
            <Tabs
              value={activeTab}
              onValueChange={(value) => {
                setActiveTab(value as 'login' | 'register');
                setMessage('');
              }}
              className={`auth-tabs auth-tabs-${activeTab}`}
            >
              <TabsList className="account-tabs">
                <TabsTrigger value="login">Entrar</TabsTrigger>
                <TabsTrigger value="register">Criar conta</TabsTrigger>
              </TabsList>
              <TabsContent value="login" className="account-panel panel-login">
                <form className="account-form" onSubmit={login}>
                  <label>
                    E-mail
                    <input
                      name="email"
                      type="email"
                      placeholder="voce@email.com"
                      required
                    />
                  </label>
                  <label>
                    Senha
                    <input
                      name="password"
                      type="password"
                      placeholder="••••••••"
                      minLength={6}
                      required
                    />
                  </label>
                  <button
                    className="primary-button"
                    type="submit"
                    disabled={busy}
                  >
                    {busy ? 'Conectando...' : 'Entrar'}{' '}
                    <ChevronRight size={17} />
                  </button>
                </form>
              </TabsContent>
              <TabsContent
                value="register"
                className="account-panel panel-register"
              >
                <form className="account-form" onSubmit={register}>
                  <label>
                    Nome
                    <input
                      name="name"
                      type="text"
                      placeholder="Seu nome"
                      required
                    />
                  </label>
                  <label>
                    E-mail
                    <input
                      name="email"
                      type="email"
                      placeholder="voce@email.com"
                      required
                    />
                  </label>
                  <label>
                    Senha
                    <input
                      name="password"
                      type="password"
                      placeholder="Mínimo de 10 caracteres"
                      minLength={10}
                      required
                    />
                  </label>
                  <button
                    className="primary-button"
                    type="submit"
                    disabled={busy}
                  >
                    {busy ? 'Enviando...' : 'Criar conta'}{' '}
                    <ChevronRight size={17} />
                  </button>
                  <small className="form-hint">
                    O cadastro aparecerá no Marcão Boost Gestão para aprovação.
                  </small>
                </form>
              </TabsContent>
            </Tabs>
          </>
        )}
        {message && (
          <p className="form-message" role="status">
            {message}
          </p>
        )}
      </DialogContent>
    </Dialog>
  );
}

function AdminDialog({
  settings,
  onSave,
  user,
}: {
  settings: SiteSettings;
  onSave: (next: SiteSettings) => void;
  user: SessionUser | null;
}) {
  const [draft, setDraft] = useState(settings);
  const [saved, setSaved] = useState(false);
  const update = (key: keyof SiteSettings, value: string) =>
    setDraft((current) => ({ ...current, [key]: value }));
  return (
    <Dialog>
      <DialogTrigger className="admin-link">
        <SlidersHorizontal size={15} /> Painel administrativo
      </DialogTrigger>
      <DialogContent className="account-dialog">
        <DialogHeader>
          <DialogTitle>Painel administrativo</DialogTitle>
          <DialogDescription>
            {user?.role === 'admin'
              ? 'Edite o conteúdo principal desta versão do site.'
              : 'Entre com uma conta administrativa para editar o site.'}
          </DialogDescription>
        </DialogHeader>
        {user?.role === 'admin' ? (
          <form
            className="account-form"
            onSubmit={(event) => {
              event.preventDefault();
              onSave(draft);
              setSaved(true);
            }}
          >
            <label>
              Título principal
              <input
                value={draft.headline}
                onChange={(e) => update('headline', e.target.value)}
              />
            </label>
            <label>
              Status da versão
              <input
                value={draft.status}
                onChange={(e) => update('status', e.target.value)}
              />
            </label>
            <label>
              Link de download
              <input
                value={draft.downloadUrl}
                onChange={(e) => update('downloadUrl', e.target.value)}
                placeholder="https://.../MarcaoBoost.exe"
              />
            </label>
            <label>
              Nome da versão
              <input
                value={draft.version}
                onChange={(e) => update('version', e.target.value)}
                placeholder="1.0.0"
              />
            </label>
            <button className="primary-button" type="submit">
              Salvar alterações <Check size={17} />
            </button>
          </form>
        ) : (
          <div className="locked-panel">
            <LockKeyhole />
            <p>Somente administradores podem alterar o conteúdo.</p>
          </div>
        )}
        {saved && (
          <p className="form-message" role="status">
            Alterações salvas neste dispositivo.
          </p>
        )}
      </DialogContent>
    </Dialog>
  );
}

export function MarcaoSite() {
  const [menuOpen, setMenuOpen] = useState(false);
  const [settings, setSettings] = useState<SiteSettings>(defaultSettings);
  const [user, setUser] = useState<SessionUser | null>(null);
  const [downloadNoticeOpen, setDownloadNoticeOpen] = useState(false);
  const [managementDownloading, setManagementDownloading] = useState(false);
  useEffect(() => {
    window.localStorage.removeItem('marcao-boost-users');
    try {
      const stored = window.localStorage.getItem('marcao-site-settings');
      if (stored) {
        const saved = JSON.parse(stored) as Partial<SiteSettings>;
        setSettings({
          ...defaultSettings,
          ...saved,
          downloadUrl: saved.downloadUrl?.trim() || defaultSettings.downloadUrl,
          version: saved.version?.trim() || defaultSettings.version,
          status: saved.status?.trim() || defaultSettings.status,
        });
      }
      const token = window.localStorage.getItem(TOKEN_KEY);
      if (token) {
        void apiRequest<{ user: SessionUser }>('/auth/me', {}, token)
          .then(({ user: verifiedUser }) => {
            window.localStorage.setItem(
              SESSION_KEY,
              JSON.stringify(verifiedUser),
            );
            setUser(verifiedUser);
          })
          .catch(() => {
            window.localStorage.removeItem(TOKEN_KEY);
            window.localStorage.removeItem(SESSION_KEY);
            setUser(null);
          });
      }
    } catch {
      window.localStorage.removeItem('marcao-site-settings');
      window.localStorage.removeItem(SESSION_KEY);
      window.localStorage.removeItem(TOKEN_KEY);
    }
  }, []);
  const saveSettings = (next: SiteSettings) => {
    setSettings(next);
    window.localStorage.setItem('marcao-site-settings', JSON.stringify(next));
  };
  const startDownload = () => {
    if (!settings.downloadUrl) {
      setDownloadNoticeOpen(true);
      return;
    }
    window.open(settings.downloadUrl, '_blank', 'noopener,noreferrer');
  };
  const isAdmin =
    user?.role === 'admin' && user.email.toLowerCase() === ADMIN_EMAIL;
  const downloadManagement = async () => {
    const token = window.localStorage.getItem(TOKEN_KEY);
    if (!isAdmin || !token || managementDownloading) return;

    setManagementDownloading(true);
    try {
      const response = await fetch(`${API_URL}/admin/download-management`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!response.ok) {
        const data = (await response.json().catch(() => ({}))) as {
          error?: string;
        };
        throw new Error(data.error || 'Não foi possível baixar a Gestão.');
      }

      const blobUrl = URL.createObjectURL(await response.blob());
      const link = document.createElement('a');
      link.href = blobUrl;
      link.download = 'MarcaoBoost-Gestao-1.1.zip';
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(blobUrl);
    } catch (error) {
      window.alert(
        error instanceof Error
          ? error.message
          : 'Não foi possível baixar a Gestão.',
      );
    } finally {
      setManagementDownloading(false);
    }
  };
  return (
    <main>
      <MatrixBackground />
      <header className="topbar">
        <Brand />
        <button
          className="menu-button"
          onClick={() => setMenuOpen(!menuOpen)}
          aria-label="Abrir menu"
        >
          {menuOpen ? <X /> : <Menu />}
        </button>
        <nav
          className={menuOpen ? 'nav-links open' : 'nav-links'}
          onClick={() => setMenuOpen(false)}
        >
          <a href="#sobre">Sobre</a>
          <a href="#diferenciais">Diferenciais</a>
          <a href="#download">Download</a>
          <a href="#faq">FAQ</a>
          <a
            className="github-nav"
            href="https://github.com/augustusvj/marcao-boost"
            target="_blank"
            rel="noreferrer"
          >
            <Code2 size={17} /> GitHub
          </a>
          {isAdmin && (
            <button
              className="management-nav"
              type="button"
              onClick={() => void downloadManagement()}
              disabled={managementDownloading}
            >
              <MonitorCog size={17} />
              {managementDownloading ? 'Baixando...' : 'Gestão'}
            </button>
          )}
          <AccountDialog
            user={user}
            onAuth={setUser}
            onLogout={() => setUser(null)}
          />
        </nav>
      </header>

      <section id="inicio" className="hero section-shell">
        <div className="hero-copy">
          <div className="eyebrow">
            <span className="pulse" /> {settings.status}
          </div>
          <h1>{settings.headline}</h1>
          <p className="hero-text">
            Uma experiência criada para quem quer extrair mais da máquina, jogar
            com fluidez e manter o controle de cada detalhe.
          </p>
          <div className="hero-actions">
            <button
              className="primary-button"
              type="button"
              onClick={startDownload}
            >
              <ArrowDownToLine size={19} /> Baixar agora
            </button>
            <a className="secondary-button" href="#sobre">
              Conhecer o projeto <ChevronRight size={18} />
            </a>
          </div>
          <div className="hero-meta">
            <span>
              <Check size={15} /> Interface simples
            </span>
            <span>
              <Check size={15} /> Ajustes reversíveis
            </span>
            <span>
              <Check size={15} /> Comunidade ativa
            </span>
          </div>
        </div>
        <div className="hero-visual">
          <div className="logo-halo" />
          <img
            src="/marcao-logo.png"
            alt="Símbolo M do Marcão Boost formado por código verde"
          />
          <div className="system-card card-a">
            <Cpu size={18} />
            <span>Resposta do sistema</span>
            <b>otimizada</b>
          </div>
          <div className="system-card card-b">
            <Gauge size={18} />
            <span>Modo desempenho</span>
            <b>ativo</b>
          </div>
        </div>
      </section>

      <section id="sobre" className="content-section section-shell">
        <div className="section-heading">
          <span>01 — O PROJETO</span>
          <h2>Feito para dar espaço ao que realmente importa.</h2>
          <p>
            Marcão Boost reúne uma experiência direta, transparente e feita para
            a comunidade. Você decide como usar, quando aplicar e o que manter.
          </p>
        </div>
        <div className="metrics">
          <article>
            <b>01</b>
            <span>Instalação guiada</span>
          </article>
          <article>
            <b>24/7</b>
            <span>Acesso à comunidade</span>
          </article>
          <article>
            <b>100%</b>
            <span>Controle do usuário</span>
          </article>
        </div>
      </section>

      <section id="diferenciais" className="features section-shell">
        <div className="section-label">02 — DIFERENCIAIS</div>
        <div className="feature-grid">
          <article className="feature-main">
            <Sparkles />
            <span>EXPERIÊNCIA</span>
            <h2>
              Mais leve.
              <br />
              Mais rápida.
              <br />
              <em>Mais sua.</em>
            </h2>
            <p>
              Uma jornada sem excesso: do primeiro acesso às configurações
              avançadas.
            </p>
          </article>
          <article>
            <Zap />
            <h3>Resposta imediata</h3>
            <p>Navegação rápida, ações claras e feedback em cada etapa.</p>
          </article>
          <article>
            <ShieldCheck />
            <h3>Controle e confiança</h3>
            <p>
              As mudanças são explicadas e podem ser revisadas quando você
              quiser.
            </p>
          </article>
          <article>
            <MonitorCog />
            <h3>Seu perfil, suas escolhas</h3>
            <p>
              Preferências e permissões acompanham a mesma conta do aplicativo.
            </p>
          </article>
          <article>
            <LockKeyhole />
            <h3>Área protegida</h3>
            <p>
              Login, cadastro e painel administrativo organizados por nível de
              acesso.
            </p>
          </article>
        </div>
      </section>

      <section id="download" className="download-section section-shell">
        <div>
          <span className="section-label">03 — DOWNLOAD</span>
          <h2>Pronto para começar?</h2>
          <p>
            Baixe a versão mais recente do Marcão Boost e siga o guia de
            instalação.
          </p>
        </div>
        <div className="download-card">
          <div className="download-icon">
            <ArrowDownToLine />
          </div>
          <div>
            <span>{settings.downloadUrl ? 'VERSÃO ATUAL' : 'LANÇAMENTO'}</span>
            <h3>Marcão Boost</h3>
            <p>{settings.version} • Windows 10 e 11 • 64 bits</p>
          </div>
          <button
            className="primary-button"
            type="button"
            onClick={startDownload}
          >
            Download <ArrowDownToLine size={18} />
          </button>
        </div>
      </section>

      <section id="faq" className="faq section-shell">
        <span className="section-label">04 — PERGUNTAS</span>
        <h2>Antes de instalar</h2>
        <details>
          <summary>Preciso de uma conta?</summary>
          <p>
            A conta é usada para manter seu acesso, preferências e permissões
            sincronizados com o aplicativo.
          </p>
        </details>
        <details>
          <summary>Posso desfazer alterações?</summary>
          <p>
            As opções do projeto são apresentadas de forma clara, com
            orientações para revisão e reversão quando disponível.
          </p>
        </details>
        <details>
          <summary>Onde encontro suporte?</summary>
          <p>
            Use o repositório no GitHub para acompanhar novidades, documentação
            e canais oficiais do projeto.
          </p>
        </details>
      </section>

      <footer className="footer">
        <div className="section-shell footer-grid">
          <div>
            <Brand />
            <p>
              Feito por Marcão.
              <br />
              Equipe Ardu Studio © 2026.
            </p>
          </div>
          <div className="footer-links">
            <a href="#sobre">Sobre</a>
            <a href="#download">Download</a>
            <a href="#faq">FAQ</a>
          </div>
          <div className="footer-actions">
            <a
              className="github-footer-link"
              href="https://github.com/augustusvj/marcao-boost"
              target="_blank"
              rel="noreferrer"
              aria-label="Abrir Marcão Boost no GitHub"
              title="GitHub do Marcão Boost"
            >
              <GitHubMark />
            </a>
            {user?.role === 'admin' &&
              user.email.toLowerCase() === ADMIN_EMAIL && (
              <AdminDialog
                settings={settings}
                onSave={saveSettings}
                user={user}
              />
              )}
          </div>
        </div>
      </footer>
      <Dialog open={downloadNoticeOpen} onOpenChange={setDownloadNoticeOpen}>
        <DialogContent className="account-dialog download-dialog">
          <DialogHeader>
            <DialogTitle>Download em preparação</DialogTitle>
            <DialogDescription>
              O instalador ainda não foi adicionado. Um administrador pode
              inserir o link pelo painel administrativo.
            </DialogDescription>
          </DialogHeader>
          <button
            className="primary-button"
            type="button"
            onClick={() => setDownloadNoticeOpen(false)}
          >
            Entendi <Check size={17} />
          </button>
        </DialogContent>
      </Dialog>
    </main>
  );
}

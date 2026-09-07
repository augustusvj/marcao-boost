# Publicar no Railway

O projeto já inclui `Dockerfile` e `railway.toml`.

1. Envie esta pasta para um repositório no GitHub.
2. No Railway, escolha **New Project → Deploy from GitHub repo**.
3. Selecione o repositório. O Railway detectará o `Dockerfile` automaticamente.
4. Depois do deploy, abra **Settings → Networking → Generate Domain**.

O contêiner usa automaticamente a variável `PORT` fornecida pelo Railway e serve a versão estática por Nginx. O caminho `/health` está configurado como healthcheck.

## Executar localmente

```bash
npm install
npm run dev
```

Abra `http://localhost:3000` (ou a porta exibida pelo terminal).

## Contas desta versão

O cadastro e o login funcionam no armazenamento do navegador. Somente a conta `avjvava@gmail.com` recebe o papel de administrador e pode editar título, status, versão e link de download. Para compartilhar contas e permissões entre aparelhos ou sincronizá-las com o aplicativo Marcão Boost, conecte o site à API do aplicativo antes da publicação definitiva.

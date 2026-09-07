# Publicar no Railway

O projeto já inclui `Dockerfile` e `railway.toml`.

1. No serviço do repositório, configure `website` como diretório raiz do projeto.
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

O cadastro, o login e as permissões usam a mesma API do aplicativo Marcão Boost. A navegação administrativa e o download protegido da Gestão aparecem somente para a conta `avjvava@gmail.com` autenticada com o papel de administrador.

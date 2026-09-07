<div align="center">
  <img src="assets/MarcaoBoostIcon.png" width="118" alt="Ícone Marcão Boost">
  <h1>Marcão Boost</h1>
  <p>Otimizador de Windows com ajustes explicados, restauração automática e gestão remota de usuários.</p>

  [![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-4f91ff?style=flat-square)](#requisitos)
  [![Versão](https://img.shields.io/badge/versão-2.3-9cff43?style=flat-square)](https://github.com/augustusvj/marcao-boost/releases/latest)
  [![Cloudflare](https://img.shields.io/badge/API-Cloudflare%20Workers-f59e0b?style=flat-square)](server)

  [Baixar Marcão Boost 2.3](https://github.com/augustusvj/marcao-boost/releases/download/v2.3.0/MarcaoBoost-2.3-Cliente.zip) · [English](README.en.md)
</div>

> O download público contém somente o aplicativo do cliente. O executável Marcão Boost Gestão é reservado ao proprietário e entregue apenas pela área administrativa autenticada do site.

## Visão geral

O Marcão Boost reúne ajustes de desempenho, jogos, rede, aparência, privacidade e GPU em uma interface limpa. Cada opção informa o que faz, o impacto esperado e o nível de atenção antes da confirmação. Mudanças persistentes guardam o valor anterior para restauração.

O projeto é formado por três partes:

- **Marcão Boost Cliente:** aplicativo WPF para Windows, executado como administrador.
- **Marcão Boost Gestão:** painel separado para aprovar ou bloquear contas, definir permissões e emitir redefinições de senha.
- **API:** Cloudflare Worker com banco D1, autenticação, sessões e registro de ações administrativas.

## Download

1. [Baixe `MarcaoBoost-2.3-Cliente.zip`](https://github.com/augustusvj/marcao-boost/releases/download/v2.3.0/MarcaoBoost-2.3-Cliente.zip) ou abra a página de [Releases](https://github.com/augustusvj/marcao-boost/releases/latest).
2. Confirme o SHA-256 do arquivo, se desejar.
3. Extraia o ZIP e execute `MarcaoBoost.exe`.
4. Confirme a solicitação de administrador do Windows.

O aplicativo não precisa ser instalado. O arquivo oficial da versão 2.3 possui SHA-256:

```text
D556517EC272719D69E5CD60F64CC95E16F5E022750F2FC591B207F9957601F7
```

## Recursos

- Cadastro e login por usuário, com aprovação e permissões controladas pela Gestão.
- Ajustes agrupados em Games, Desempenho, Rede, Aparência, Privacidade, NVIDIA, AMD e Hardware.
- Aplicação com um clique, confirmação prévia e mensagens com som de sucesso ou erro.
- Backup local dos valores alterados e tela de restauração.
- Limpeza de `%TEMP%`, Temp do Windows, Prefetch antigo, miniaturas, relatórios de erro e caches gráficos.
- Integração NVIDIA por NVAPI para energia, textura, cache de shaders e baixa latência quando suportada.
- Integração AMD por ADLX para Anti-Lag, Chill, Enhanced Sync, limite de quadros e Radeon Boost quando suportados.
- Detecção de incompatibilidade de driver ou hardware e retorno de erro legível.
- Interface sem barra de título clara, cantos arredondados, transições e rolagem visualmente discreta.
- Encerramento real do processo ao fechar a janela.

## Segurança e comportamento

O Marcão Boost não promete ganhos iguais em todos os computadores. Resultados dependem do hardware, driver, versão do Windows e jogos utilizados. Ajustes marcados como “Atenção” ou “Teste por jogo” devem ser avaliados individualmente.

- Documentos pessoais não fazem parte da limpeza.
- Arquivos em uso ou protegidos são ignorados e informados ao usuário.
- Senhas são derivadas com PBKDF2 e salt individual; sessões são armazenadas como hash no banco.
- O segredo inicial da Gestão e credenciais reais não fazem parte deste repositório.
- O binário da Gestão não é anexado às releases públicas; a API libera o arquivo privado somente para uma sessão administrativa válida.

Consulte também [SECURITY.md](SECURITY.md) e [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Requisitos

- Windows 10 ou Windows 11 de 64 bits.
- Acesso à internet para cadastro, login e permissões.
- Privilégios de administrador para ajustes de sistema e limpeza protegida.
- Driver NVIDIA ou AMD compatível para as funções específicas de GPU.

## Estrutura do repositório

```text
assets/             ícone e identidade visual
src/client/         aplicativo Marcão Boost Cliente
src/admin/          código do painel Marcão Boost Gestão
src/native/         integrações NVIDIA NVAPI e AMD ADLX
server/             Cloudflare Worker, D1 e migrações
.github/workflows/  geração automática de releases do cliente
```

## Compilar o cliente

Em um Windows de 64 bits, abra o PowerShell na raiz do projeto:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
./src/client/build-client.ps1
```

O resultado será criado em `outputs/MarcaoBoost.exe`. Os auxiliares nativos compilados usados pelo cliente estão em `src/client/native`. Para recompilá-los, obtenha os SDKs oficiais NVIDIA NVAPI e AMD ADLX e respeite as licenças incluídas.

## Executar a API em uma conta própria

```powershell
cd server
npm install
Copy-Item wrangler.example.jsonc wrangler.jsonc
npx wrangler d1 create marcao-boost-users
```

Coloque o ID retornado em `wrangler.jsonc`, aplique a migração e defina um segredo inicial forte:

```powershell
npx wrangler d1 migrations apply marcao-boost-users --remote
npx wrangler secret put BOOTSTRAP_KEY
npm run deploy
```

Para oferecer o download administrativo, crie `server/private-assets`, coloque nela o arquivo `MarcaoBoost-Gestao-1.1.zip` e publique novamente. Essa pasta é ignorada pelo Git e o Worker exige autenticação administrativa antes de entregar o arquivo.

Altere `AppConfig.ApiUrl` nos dois arquivos `WpfShared.cs` para o endereço do seu Worker antes de compilar. A Gestão exige o segredo inicial apenas para a criação do primeiro proprietário; ele nunca deve ser commitado.

## Gestão

O código do painel administrativo é público para transparência, mas o acesso depende de uma conta com função `admin`. Para uma implantação própria:

```powershell
./src/admin/build-admin.ps1 -BootstrapSecret "SEU_SEGREDO_LOCAL"
```

O segredo é colocado somente no binário local durante a compilação por meio de um arquivo temporário apagado ao final. Não publique esse binário nem compartilhe o segredo.

## Avisos legais

NVIDIA, AMD, Microsoft e Cloudflare são marcas de seus respectivos proprietários. O Marcão Boost não é endossado por essas empresas. O código é disponibilizado publicamente para transparência e revisão; nenhuma licença geral de redistribuição é concedida neste momento. Componentes de terceiros seguem seus próprios termos.

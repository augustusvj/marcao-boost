<div align="center">
  <img src="assets/MarcaoBoostIcon.png" width="118" alt="Marcão Boost icon">
  <h1>Marcão Boost</h1>
  <p>A Windows optimizer with explained tweaks, automatic backups, and remote user management.</p>

  [![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-4f91ff?style=flat-square)](#requirements)
  [![Version](https://img.shields.io/badge/version-2.3-9cff43?style=flat-square)](https://github.com/augustusvj/marcao-boost/releases/latest)
  [![Cloudflare](https://img.shields.io/badge/API-Cloudflare%20Workers-f59e0b?style=flat-square)](server)

  [Download Marcão Boost](https://github.com/augustusvj/marcao-boost/releases/latest) · [Português](README.md)
</div>

> The public download contains only the client application. The Marcão Boost Gestão executable is owner-only and is not distributed.

## Overview

Marcão Boost brings performance, gaming, network, appearance, privacy, and GPU tweaks into a clean interface. Every option explains its purpose, expected impact, and attention level before confirmation. Persistent changes save their previous value for restoration.

The project has three parts:

- **Marcão Boost Client:** a WPF application for Windows that runs as administrator.
- **Marcão Boost Gestão:** a separate owner dashboard for approving or blocking accounts, assigning permissions, and issuing password resets.
- **API:** a Cloudflare Worker backed by D1, with authentication, sessions, and administrative audit records.

## Download

1. Open the [Releases](https://github.com/augustusvj/marcao-boost/releases/latest) page.
2. Download `MarcaoBoost-2.3-Cliente.zip`.
3. Extract the ZIP and run `MarcaoBoost.exe`.
4. Accept the Windows administrator prompt.

No installation is required. The official version 2.3 archive has this SHA-256 digest:

```text
D556517EC272719D69E5CD60F64CC95E16F5E022750F2FC591B207F9957601F7
```

## Features

- Per-user registration and login, with approval and permissions controlled by Gestão.
- Tweaks grouped into Games, Performance, Network, Appearance, Privacy, NVIDIA, AMD, and Hardware.
- One-click actions with confirmation, success sounds, and readable error messages.
- Local backup of changed values and a dedicated restoration screen.
- Cleanup for `%TEMP%`, Windows Temp, old Prefetch data, thumbnails, error reports, and graphics caches.
- NVIDIA NVAPI integration for power, texture filtering, shader cache, and low latency when supported.
- AMD ADLX integration for Anti-Lag, Chill, Enhanced Sync, frame-rate limiting, and Radeon Boost when supported.
- Clear errors for unsupported hardware or drivers.
- Custom dark window chrome, rounded controls, animated transitions, and visually hidden scrollbars.
- Full process shutdown when the application window is closed.

## Safety and behavior

Marcão Boost does not promise identical gains on every computer. Results depend on hardware, drivers, the Windows version, and each game. Tweaks marked “Attention” or “Test per game” should be evaluated individually.

- Personal documents are never included in cleanup.
- Locked or protected files are skipped and reported to the user.
- Passwords are derived with PBKDF2 and individual salts; session tokens are stored as hashes.
- Real credentials and the initial Gestão bootstrap secret are not part of this repository.
- The Gestão binary is never attached to public releases.

See [SECURITY.md](SECURITY.md) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Requirements

- 64-bit Windows 10 or Windows 11.
- Internet access for registration, login, and permission checks.
- Administrator privileges for system tweaks and protected cleanup areas.
- A compatible NVIDIA or AMD driver for vendor-specific GPU features.

## Repository layout

```text
assets/             icon and visual identity
src/client/         Marcão Boost Client application
src/admin/          Marcão Boost Gestão source code
src/native/         NVIDIA NVAPI and AMD ADLX integrations
server/             Cloudflare Worker, D1, and migrations
.github/workflows/  automated client release pipeline
```

## Build the client

On 64-bit Windows, open PowerShell at the repository root:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
./src/client/build-client.ps1
```

The build is written to `outputs/MarcaoBoost.exe`. Prebuilt native helpers used by the client are under `src/client/native`. To rebuild them, obtain the official NVIDIA NVAPI and AMD ADLX SDKs and follow their included licenses.

## Run the API in your own account

```powershell
cd server
npm install
Copy-Item wrangler.example.jsonc wrangler.jsonc
npx wrangler d1 create marcao-boost-users
```

Put the returned ID in `wrangler.jsonc`, apply the migration, and set a strong bootstrap secret:

```powershell
npx wrangler d1 migrations apply marcao-boost-users --remote
npx wrangler secret put BOOTSTRAP_KEY
npm run deploy
```

Update `AppConfig.ApiUrl` in both `WpfShared.cs` files before compiling. Gestão uses the bootstrap secret only to create the first owner; it must never be committed.

## Gestão

The administration dashboard source is public for transparency, but access requires an account with the `admin` role. For your own deployment:

```powershell
./src/admin/build-admin.ps1 -BootstrapSecret "YOUR_LOCAL_SECRET"
```

The secret is injected only into the local binary through a temporary source file that is deleted after compilation. Do not publish that binary or share the secret.

## Legal notice

NVIDIA, AMD, Microsoft, and Cloudflare are trademarks of their respective owners. Marcão Boost is not endorsed by those companies. Source is publicly visible for transparency and review; no repository-wide redistribution license is granted at this time. Third-party components remain governed by their own terms.

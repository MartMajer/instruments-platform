# tools/frameworks

Use this folder to keep all local command entry points in one place for development.

This folder intentionally contains:

- `ensure-tool-links.sh` — detects installed tool binaries and writes stable shims into `bin/`.
- `activate.sh` — prepends `tools/frameworks/bin` to your `PATH`.
- `bin/` — generated symlinks (local only; not committed as binaries).

### Setup (Linux)

```bash
sudo tools/frameworks/install-linux-toolchain.sh
```

For a one-shot install-and-start flow, run:

```bash
sudo tools/frameworks/bootstrap-linux-dev.sh
```

The install script does a full apt-based setup for this repository (`dotnet` 9, Node.js 20.x, `npm`, `docker`, `pwsh`) and then writes shims into `tools/frameworks/bin`.

On Ubuntu-family systems where `docker-compose-plugin` is unavailable, it falls back to `docker-compose`.

If GitHub API access is restricted where you work, export `DOCKER_COMPOSE_VERSION` before running the installer:

```bash
  export DOCKER_COMPOSE_VERSION=2.34.1
```

If this script cannot upgrade Node on your distro packages, it falls back to NodeSource 20.x for this repo's required frontend toolchain.

If apt reports `nodejs : Conflicts: npm` during NodeSource install, rerun these cleanup steps and retry:

```bash
sudo apt remove -y npm
sudo apt --fix-broken install
sudo tools/frameworks/install-linux-toolchain.sh
```

If `dotnet restore` fails due to network instability or stale NuGet cache, run:

```bash
./tools/frameworks/fix-dotnet-restore.sh
```

This helper uses repo-local cache paths (`.dotnet-home`, `.home`) and retries with no-cache,
non-parallel restore settings.

If you prefer to only re-sync shims after a manual install, run:

```bash
bash tools/frameworks/ensure-tool-links.sh
```

After tools are available on your system and linked here, start services as usual:

```bash
./start-all.sh
```

To stop local dev services started by `start-all.sh`, run from repo root:

```bash
./stop-all.sh
```

Required CLI tools for local startup remain:

- `dotnet` (SDK 9)
- `node` and `npm` (Node.js 20.19+ required for frontend tooling)
- `docker`
- `pwsh` (PowerShell 7; `powershell` is accepted as a fallback)

If tools are missing, the startup script prints exactly which one is required.

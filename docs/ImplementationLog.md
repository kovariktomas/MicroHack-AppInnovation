### 2025-08-28 (Infrastructure - network redesign with Bastion)
- Adjusted per-user VNet to /22 CIDR with dedicated `vms` /24 and `AzureBastionSubnet` /26.
- Removed public IP from VM NIC; Standard Public IP now attached to Azure Bastion (Basic SKU) for secure RDP.
- Replaced permissive NSG rules with single rule allowing RDP only from VirtualNetwork (Bastion access path).
- Added Bastion host deployment and set Public IP SKU to Standard to satisfy Bastion requirements.
### 2025-08-28 (Infrastructure - inline Public IP)
- Removed separate `pip.bicep` module; inlined Public IP resource into `workload.bicep` (Standard SKU Static) simplifying module graph.
- Updated Bastion host to reference inlined `publicIp` resource id directly.
- README updated (removed module reference, clarified NSG restriction & subnet layout).
### 2025-08-28 (Infrastructure - VM provisioning script)
- Added `baseInfra/scripts/setup.ps1` headless provisioning script (Git, .NET SDK, SQL Express, clone repo, publish app, create Windows Service, firewall rule, env vars). Intended for later Custom Script Extension integration.
### 2025-08-28 (Infrastructure - provisioning script simplification)
- Removed parameters from `setup.ps1`; replaced with top-of-file configuration block for simpler manual tweaking during early testing phase.
### 2025-08-28 (Infrastructure - Git install fallback)
- Enhanced `setup.ps1` Git installation: tries winget, then Chocolatey, then direct silent installer download (pinned version) for Windows Server environments lacking winget.
### 2025-08-28 (Infrastructure - .NET install robustness)
- Improved .NET SDK detection & installation in `setup.ps1` (graceful failure handling, removed invalid -Verbose switch, added PATH refresh and post-install verification).
### 2025-08-28 (Infrastructure - SQL Express arg quoting fix)
- Corrected quoting for `/SQLSVCACCOUNT` in SQL Express silent install args to avoid PowerShell parser error; added log echo of arguments.
### 2025-08-28 (Infrastructure - SQL Express installer robustness)
- Added multi-URL download strategy, bootstrap detection, two-step media download, improved silent install args & timeout loop in `setup.ps1`.
### 2025-08-28 (Infrastructure - SQL Express bootstrap simplification)
- Removed two-step ACTION=Download path; bootstrap now invoked directly with install args and exit code checked.
### 2025-08-28 (Infrastructure - SQL Express extraction install)
- Adjusted `setup.ps1` to always self-extract installer then run inner setup.exe with minimal supported flags (resolving unrecognized settings errors).
### 2025-08-28 (Infrastructure - SQL Express multi-strategy)
- Replaced extraction approach with tiered install (winget -> Chocolatey -> manual bootstrap) plus extended polling.
### 2025-08-28 (Infrastructure - provisioning simplification - Chocolatey baseline)
- Simplified `setup.ps1` by ensuring Chocolatey is installed first, then using it uniformly for Git, .NET SDK, and SQL Server Express (removed multi-strategy logic & manual bootstrap fallback to reduce complexity on Server images lacking winget).
### 2025-08-28 (Infrastructure - service creation diagnostics)
- Added verbose diagnostics & error handling around Windows Service creation in `setup.ps1` (captures sc.exe output, validates existence, fails fast if missing).
### 2025-08-28 (Infrastructure - service creation fallback & self-contained option)
- Added fallback using `New-Service` if `sc.exe create` doesn't materialize service; introduced optional self-contained publish mode to simplify service binPath.
### 2025-08-28 (Infrastructure - pin .NET SDK 8.0.0)
- Modified provisioning script to install exact .NET 8.0.0 (SDK 8.0.100) via dotnet-install script instead of major version heuristic.
### 2025-08-28 (Infrastructure - switch .NET pin to Chocolatey)
- Adjusted provisioning script to use Chocolatey for pinned .NET 8.0 SDK installation (tries multiple package IDs, removes dotnet-install script usage as per requirement).
### 2025-08-28 (Infrastructure - exact .NET SDK 8.0.413 pin)
- Updated provisioning script to require and install only .NET SDK 8.0.413 via Chocolatey (fails fast if not available or mismatched).
### 2025-08-28 (Infrastructure - simplify .NET 8.0.413 install)
- Reduced .NET SDK install logic to a single Chocolatey install attempt (removed multi-package loop, added concise validation).
### 2025-08-28 (Infrastructure - simplify runtime startup)
- Removed service / publish / env var configuration from provisioning script; added startup scheduled task executing `dotnet run` in repo directory for auto-start after reboot.
### 2025-08-28 (Infrastructure - desktop shortcut & browser launch)
- Replaced scheduled task approach with creation of desktop shortcut invoking `start-app.ps1`.
- `start-app.ps1` now sets ASPNETCORE_URLS to http://localhost:5000, starts the app in background, and opens default browser to that URL after short delay.
### 2025-08-28 (Infrastructure - VM Custom Script Extension)
- Added Custom Script Extension to `workload.bicep` executing `setup.ps1` from GitHub URL during VM provisioning (installs dependencies, creates desktop shortcut & browser launch script).
### 2025-08-29 (Infrastructure - NAT Gateway)
### 2025-08-29 (App / Infra - DB creation responsibility shift)
- Removed unconditional `EnsureCreated()` database creation from application startup (now guarded and only creates tables if DB reachable; no CREATE DATABASE attempt).
- Added `SKIP_DB_INIT` env var gate (default '0'); infra/script now pre-creates database aligning with Azure SQL model where database is provisioned separately.
- Updated `setup.ps1` to add config variables for `$DatabaseName`, optional SQL login creation, and to idempotently create the database via `sqlcmd`.
- Rationale: principle of least privilege & forward compatibility with Azure SQL where server-level CREATE DATABASE may not be permitted to app principal.
### 2025-08-29 (Infrastructure - sqlcmd handling simplification)
- Simplified `Ensure-SqlCmd` to probe only two observed install locations and perform a single winget install attempt, removing multi-fallback complexity for clarity on Server 2025 hosts.
### 2025-08-29 (Infrastructure - sqlcmd robustness follow-up)
- Enhanced `Ensure-SqlCmd` to: verify execution (`sqlcmd -?`), persist discovered directory to Machine PATH, and fall back to classic `Microsoft.SQLServer.CommandLineTools` if modern package present but non-functional.
### 2025-08-29 (Infrastructure - setup.ps1 winget removal & simplification)
- Rewrote provisioning script to eliminate `winget` dependency (not available during Custom Script Extension under SYSTEM) using direct downloads:
	- .NET SDK via `dotnet-install.ps1` channel 8.0
	- SQL Server 2022 Express bootstrap with silent arguments enabling mixed mode + TCP 1433
	- Modern `sqlcmd` (go-sqlcmd) GitHub release zip (pinned version 1.7.0)
- Structured into numbered steps with concise helper functions (Step / Retry / Wait-For) for clarity.
- Moved configuration constants to a single block at top; removed previous multifallback logic and legacy variable section.
- Database & login provisioning now performed using SA credential established at install, then tested with application login.
- Start script generation unchanged in behavior (updated to use new config variables).
### 2025-08-29 (Infrastructure - persist .NET PATH)
- Updated `setup.ps1` to append `C:\Program Files\dotnet` to Machine PATH and set `DOTNET_ROOT` so `dotnet` CLI is available after VM reboot (fixes post-restart 'dotnet not found' issue under new sessions / scheduled tasks).
### 2025-08-29 (Infrastructure - delayed app auto-start)
- Added `start-app-delayed.ps1` wrapper (60s sleep) and updated scheduled task in `setup.ps1` to invoke it, mitigating race where first logon occurs before user profile & PATH (with dotnet) are fully initialized.
### 2025-08-29 (Infrastructure - WSL enablement)
- `setup.ps1` now always enables `Microsoft-Windows-Subsystem-Linux` and `VirtualMachinePlatform` (idempotent) and schedules a reboot (60s) only if features were newly enabled. Prepares for dev tooling (e.g., Rancher Desktop) requiring WSL.
### 2025-08-29 (Infrastructure - dev tools post-logon installer)
- Added Step 8 creating one-time scheduled task `DevToolsInstallOnce` plus `C:\dev-tools-install.ps1` to install VS Code, Azure CLI, Rancher Desktop, and SSMS via winget on first interactive logon; task self-unregisters after success and writes marker file.
### 2025-08-29 (Infrastructure - simplify startup & dev tools execution)
- Removed scheduled tasks (auto-start & dev tools). Retained plain scripts `C:\start-app.ps1` and `C:\dev-tools-install.ps1` for manual execution. Eliminated delayed wrapper. DISM 3010 exit code now treated as success requiring reboot.
### 2025-08-30 (Infrastructure - dev tools script update)
- Added Git (`Git.Git`) installation to `C:\dev-tools-install.ps1` script.
### 2025-09-01 (Challenge 01 - Azure SQL serverless template)
- Added `solutions/ch01/bicep/main.bicep` to deploy Azure SQL logical server + single serverless database (General Purpose tier) with auto-pause 60 minutes and max 2 vCores (implicit min 0.5).
- Implemented unique server naming via `uniqueString(resourceGroup().id)` and firewall rule parameterizing application public IP.
- Included example parameters file `main.bicepparam` and README with deployment instructions & rationale for omitted explicit `minCapacity`.
### 2025-09-01 (Challenge 01 - Bicep fixes & lint cleanup)
- Replaced incorrect `securestring` ARM-style type with Bicep secure decorator (`@secure() param administratorLoginPassword string`).
- Simplified child database resource syntax using `parent` property; removed unnecessary `dependsOn` and updated output to still surface names (no functional change).
- Addressed linter warnings: removed quotes around tag key `workload` (left quotes for `managed-by` which contains a hyphen) and eliminated `use-parent-property` & `no-unnecessary-dependson` warnings.
- Left API version at preview per original template (only warning-level diagnostics); consider moving to latest stable typed version in future hardening pass.
### 2025-09-01 (App - Dockerfile for Blazor Server)
- Added multi-stage Linux Dockerfile (`dotnet/Dockerfile`) targeting .NET 8 (SDK -> aspnet runtime).
- Uses Debian-based images (not Alpine) to avoid additional native dependency installs for `Microsoft.Data.SqlClient`.
- Sets default `ASPNETCORE_URLS=http://0.0.0.0:8080`, exposes 8080, and creates non-root `appuser`.
- README updated with build/run instructions including volume mounts for images & seed catalog.
### 2025-09-01 (App - Dockerfile publish fix)
- Removed `--no-restore` from publish to prevent intermittent `NETSDK1064` (missing analyzer package) during layered build; publish now performs final restore ensuring completeness.
### 2025-09-01 (Challenge 01 - Add Azure Container Registry)
- Extended `solutions/ch01/bicep/main.bicep` to provision Azure Container Registry with unique name (`acr${uniqueString(resourceGroup().id)}`) and configurable SKU (Basic default; allowed Standard/Premium).
- Added outputs for registry name & login server; disabled admin user (prefer AAD) and documented usage in README.
- Updated parameter file and Bicep README to reflect new `acrSku` parameter and combined scope (SQL + ACR).
### 2025-09-01 (Challenge 01 - Allow Azure services firewall rule)
- Added `AllowAzureServices` firewall rule (0.0.0.0 start/end) to SQL server in `solutions/ch01/bicep/main.bicep` to permit connections from Azure services when app lacks fixed outbound IP.
### 2025-09-01 (Challenge 01 - Container Apps deployment)
- Extended `solutions/ch01/bicep/main.bicep` with Azure Container Apps managed environment (consumption workload profile) and container app referencing ACR image `lego-catalog/app:latest`.
- Added storage account + two Azure Files shares (seed + images) mounted at `/mnt/seed` and `/mnt/images`; env vars `IMAGE_ROOT_PATH` and `SEED_DATA_PATH` updated accordingly.
- Implemented secret for `SQL_CONNECTION_STRING` and AcrPull role assignment via system-assigned identity.
- Configured HTTP ingress (external) on port 8080 and HTTP-based autoscale 0..3 replicas (concurrentRequests=50 threshold).
### 2025-09-01 (Challenge 01 - Container Apps deployment fix)
- Fixed Bicep compile issues: replaced fractional CPU (0.5) with integer 1 due to Bicep numeric literal limitation; added explanatory comment.
- Adjusted role assignment resource name to exclude runtime principalId (now deterministic GUID using ACR id + app name) resolving BCP120.
### 2025-09-01 (Challenge 01 - ACR image pull hardening)
- Switched Container App from system-assigned to user-assigned managed identity for ACR pulls following official guidance (pre-create identity, grant AcrPull before app deploy) to avoid cold-start race where image pull occurs before role assignment propagates.
- Added `userAssignedIdentityName` parameter & identity resource, updated role assignment to use identity principalId, configured container registry block with identity resource ID.
- Exposed identity resource ID in outputs for diagnostics.
### 2025-09-03 (CI/CD - Simple GitHub Actions workflow)
- Added `.github/workflows/simple.yaml` triggering on `push` to `main` affecting `dotnet/**` and manual `workflow_dispatch`.
- Implements OIDC Azure login (id-token permission) using repository variables (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`).
- Builds Docker image from `dotnet/Dockerfile` tagging only with workflow run ID (`<GITHUB_RUN_ID>`), no `latest` tag to enforce explicit version pinning, pushes to ACR `${{ vars.ACR_NAME }}` under repository `lego-catalog/app`.
- Updates Azure Container App `lego-catalog-app` in resource group `${{ vars.RESOURCE_GROUP_NAME }}` with new image and sets `IMAGE_VERSION` env var.
- Notes: placeholder disabled docker/login-action kept for future direct push optimization; relies on `az acr login` after Azure CLI OIDC auth.
### 2025-09-03 (CI/CD - GitHub Actions Managed Identity in Bicep)
- Extended `solutions/ch03/bicep/main.bicep` with GitHub Actions user-assigned managed identity + federated identity credential (issuer `token.actions.githubusercontent.com`).
- Added parameters: `githubOrg`, `githubRepo`, `githubBranch`, `githubActionsIdentityName` (defaults set to repository details and `main`).
- Added Contributor role assignment for identity at resource group scope (guid derived) enabling infrastructure + container app updates.
- Outputs now include `ghActionsIdentityClientId` / principal & resource IDs for populating GitHub repository variables (`AZURE_CLIENT_ID`).
- Updated Bicep README documenting new parameters, outputs, and setup instructions for OIDC.
### 2025-09-03 (Container Apps - Enable multiple revisions)
- Changed `activeRevisionsMode` from `Single` to `Multiple` in `solutions/ch03/bicep/main.bicep` to support advanced deployment workflow (parallel revisions, manual promotion / traffic splitting).
- Added inline comment explaining rationale; groundwork for upcoming multi-step GitHub Actions pipeline (blue/green or canary style) described in challenge README.
### 2025-09-03 (CI/CD - Multi-environment GitHub federation)
- Added two additional federated identity credentials to GitHub Actions managed identity for environments `staging` and `production` (subjects `repo:<org>/<repo>:environment:staging|production`).
- Allows using GitHub Environments with protection rules & approvals while reusing the same Azure Managed Identity.
- Updated README (ch03) with instructions on referencing environment-based OIDC in workflows.
### 2025-09-03 (CI/CD - Multi-revision staged promotion workflow)
- Refactored `.github/workflows/simple.yaml` into two jobs: `build_and_stage` (environment: staging) and `promote_production` (environment: production with approval).
- Staging job builds & pushes image (tag = run id), creates new revision, forces 0% traffic to new revision (old stays 100%) and exports revision names.
- Production job (after manual approval) shifts traffic to new revision (100%) and deactivates previous revision to reduce drift.
- Supports blue/green style promotion using Azure Container Apps multiple revisions mode and GitHub Environments gating.
### 2025-09-03 (CI/CD - Production cleanup enhancement)
- Updated production promotion step to deactivate all previously active revisions (not just the immediately prior one) after shifting traffic to the new revision.
- Ensures a single active revision remains, simplifying rollback logic and reducing resource usage.
### 2025-09-03 (App - OpenTelemetry instrumentation)
- Added OpenTelemetry packages (core, hosting, OTLP exporter, AspNetCore, SqlClient, Http, Runtime, Process instrumentations).
- Configured `Program.cs` with resource builder (service name fallback `lego-catalog`), tracing, metrics, logging, and OTLP exporter via `.UseOtlpExporter()` (env driven, no hard-coded endpoints).
- Introduced custom `ActivitySource` + `Meter` and counter `lego.perf_endpoint.invocations` plus a small internal activity segment for `/perftest/catalog` post-processing.
- Updated README with comprehensive OTEL_* environment variable documentation and quick start collector example.
- Chose secure defaults: do not emit full SQL text; rely on sampling env vars if set; query redaction remains enabled unless experimental disable variable applied.
### 2025-09-03 (Challenge 04 - OpenTelemetry Collector + App Insights wiring)
- Extended `solutions/ch04/bicep/main.bicep` with optional monitoring stack controlled by `enableOtel` (default true).
- Added Log Analytics workspace + workspace-based Application Insights component (web kind) when enabled.
- Injected `appInsightsConfiguration.connectionString` and `openTelemetryConfiguration` (traces + logs destinations = `appInsights`) into Container Apps managed environment using preview API version supporting OTEL.
- Added new parameters: `enableOtel`, `logAnalyticsWorkspaceName`, `appInsightsName`, `logAnalyticsRetentionInDays` (with validation range 30-730 days).
- Updated parameter file with defaults & commented override examples; README (ch04) documents usage, validation steps, and notes on metrics limitation.
- Exposed outputs for connection string and resource names (or 'disabled' sentinel) to aid testing and GitHub workflows.
### 2025-09-03 (Infrastructure - RG tagging for security exemption)
- Added `SecurityControl=ignore` tag to per-user resource group in `baseInfra/bicep/userInfra.bicep` to satisfy security scanning exemption requirement.
### 2025-09-08 (Dev Experience - VS Code Dev Container)
- Added `.devcontainer/` with `Dockerfile` (base `mcr.microsoft.com/devcontainers/dotnet:1-8.0-bookworm`) installing Azure CLI, azd, and upgrading Bicep CLI.
- Included Docker CLI & mounted host Docker socket for building/testing container images inside the dev container.
- Added `devcontainer.json` configuring extensions: C# (`ms-dotnettools.csharp`), Bicep, Docker, GitHub Copilot + Chat; sets default solution and restores on open.
- Created workspace file `MicroHack-AppInnovation.code-workspace` with recommended extensions & solution focus.
- Rationale: consistent Linux environment across contributors (macOS/Windows hosts) with pinned .NET 8 toolchain & Azure CLIs, enabling infra (Bicep) + app (Container Apps) workflows.
- Notes: `postCreateCommand` surfaces versions for quick diagnostics; Docker group membership enables image build via host daemon; telemetry disabled for reproducibility.
### 2025-09-08 (Dev Experience - Dev Container .NET roll-forward fix)
- Removed `DOTNET_ROLL_FORWARD=disable` (set to `LatestPatch`) in `.devcontainer/Dockerfile` to allow patch roll-forward (previous setting caused runtime error when app built for 8.0.0 but only 8.0.19 present).
- Rationale: default behavior (LatestPatch) ensures security updates & avoids manual pin churn while keeping major/minor stable.
- No code changes required in application; rebuild dev container to apply (`Rebuild Container`).
### 2025-09-08 (Dev Experience - SQL Server Express sidecar)
- Updated `devcontainer.json` `postCreateCommand` to launch a persistent `microhack-sql` container (`mcr.microsoft.com/mssql/server:2022-latest`, `MSSQL_PID=Express`).
- Added `MSSQL_SA_PASSWORD` env var (placeholder `YourStrong!Passw0rd!` – recommend override via local customization) and mapped port 1433 for host access.
- Data persisted in named Docker volume `microhack-sql-data`; startup idempotent (skips if container already exists).
- Rationale: sidecar container avoids complexity of running SQL Server service inside main dev container (no systemd), keeps image lean, and mirrors production external DB topology.
## Implementation Log
### YYYY-MM-DD Split setup script into modular stages
Refactored `baseInfra/scripts/setup.ps1` into an orchestration-only script. Added modular scripts:
* `SQL_install.ps1` – installs & configures SQL Server (static TCP 1433 + firewall), installs `sqlcmd`, provisions DB/login.
* `App_install.ps1` – installs .NET SDK if needed, downloads source, creates start script using static port connection string.
* `Dev_install_initial.ps1` – enables WSL + VirtualMachinePlatform, creates reboot sentinel if needed.
* `Dev_install_post_reboot.ps1` – installs developer tooling after reboot, then cleans up scheduled task & sentinel.

Introduced status tracking file `C:\install_status.txt` with stages: `sql`, `app`, `dev`, `devpost` each set to `pending|running|failed|success`. Orchestrator is idempotent and skips completed stages.


### 2025-08-27
Initial implementation of Python data generator (`main.py`):
- Uses Azure OpenAI `AzureOpenAI` client & Responses API for structured text (categories/items) and image generation.
- Batch size default reduced to 20 per new requirement.
- Model now returns `imagePrompt`; no local heuristic assembly.
- Pydantic models for categories, generated items, and catalog items; basic validation (prefix + forbidden tokens).
- Simple resume logic (loads existing `catalog.json` if `--resume`).
- Concurrency for image generation via asyncio semaphore.
- Idempotent category generation unless `--force-categories`.

Future improvements (not yet implemented):
- Robust retry/backoff logic (current version relies on implicit SDK behavior only)
- Enhanced schema / banned token scanning & logging
- Partial save of images progress & failed images list
- More granular error handling & exponential backoff for rate limits

#### Later on 2025-08-27 (same day)
Adjustments & troubleshooting:
- Removed unsupported `response_format` / `modalities` parameters after SDK errors; switched to `responses.parse` with `text_format` using Pydantic models for structured outputs.
- Migrated from deprecated Pydantic v1 `@validator` to v2 `@field_validator` to remove deprecation warnings.
- Multiple failed attempts to generate images via Responses API (direct modalities, then tool invocation) resulted in HTTP 400; pivoted to dedicated `images.generate` API which succeeded for the majority of items.
- Added retry loop (simple exponential backoff) around image generation; still basic and could classify errors better.
- Generated full set target of 200 catalog entries; only 198 images produced (2 failures) during first pass.
- Implemented maintenance utility `prune_missing_images.py` to detect & optionally prune catalog entries whose images are missing. Ran with `--prune` producing backup `catalog.json.bak` and pruned catalog now at 198 entries.
- Environment cleanup: removed unused IMAGE_SIZE env var; batch size kept default 20 in code (note: earlier `.env` still had BATCH_SIZE=50; code path prefers explicit CLI or default constant).
- Logging still minimal; future improvement to record failed image requests with reason codes.

Next potential enhancements:
- Add `--repair-missing-images` workflow to attempt regeneration before pruning.
- Persist a `failed_images.json` with error metadata for audit.
- Align `.env` BATCH_SIZE with default or read it explicitly to avoid confusion.
- Add simple tests under `tests/` for: category generation shape; item batch shape; missing image pruning logic.

### 2025-08-27 (later)
Added initial .NET Blazor Server application (`dotnet/`):
- net9.0 Blazor Server app with EF Core SqlServer; automatic `EnsureCreated` on startup.
- Environment variable overrides (`SQL_CONNECTION_STRING`, `SEED_DATA_PATH`, `IMAGE_ROOT_PATH`, `SKIP_STARTUP_IMPORT`).
- Repository + service layer (`FigureRepository`, `CategoryRepository`, `FigureCatalogService`).
- Startup hosted service to import seed data when DB empty.
- Import page (`/Import`) implemented as Razor Page for file upload of `catalog.json` (idempotent insert-only for new figure IDs).
- Static image serving endpoint `/images/{file}` backed by configurable root path.
- Basic UI: list, search, category filter, detail view.
- README with run instructions.

Deferred (documented for future): blob image store, telemetry.

#### 2025-08-27 (decision: keep simplicity)
- Reverted migration setup back to `EnsureCreated` to avoid external tooling steps.
- Removed Tools package & design-time factory; migrations deferred until schema changes justify complexity.

#### 2025-08-27 (import simplification)
- Removed manual Import page and button; startup import now always runs (idempotent) without SKIP/FORCE flags.
- Startup service logs only total parsed + newly added.

### 2025-08-27 (UI modernization pass)
- Replaced simplistic top bar with sticky header, navigation using `NavLink`, GitHub link, and theme toggle.
- Later same day: removed brand/logo & navigation links (single-page app) keeping only theme toggle + GitHub link in compact header.
- Added dark/light theme with persisted preference via localStorage and CSS custom properties.
- Introduced modern card grid with hover elevation, skeleton loading placeholders, responsive layout, and accessible keyboard interaction.
- Enhanced figure detail page layout (two-column responsive) and badge styles.
- Added Inter font, gradient brand text, refined buttons (primary/ghost) and toolbar styling.
- Implemented reset filters, improved empty state, and focus navigation (updated selector to `h1,h2,h3`).
- Updated CSS with light mode fallback, reduced-motion support, and improved scrollbar styling.
 - Removed obsolete Import navigation (automatic startup import only) and associated Razor Page.

### 2025-08-27 (Infrastructure - initial Bicep modules)
- Added base `bicep/` templates: `main.bicep` (subscription loop), `userInfra.bicep` (RG + pip per user), `pip.bicep` (Public IP resource group module).
- Implemented initial naming convention (later revised) `userNNN-rg` / `userNNN-pip` with zero-padded indices starting at 1.
- Added Deployment Stack CLI instructions to `baseInfra/README.md` for create/update, what-if, listing, and destroy operations.
- Chose `westeurope` default location (adjustable via parameter).
- Future: extend module to include VNET + VM + initialization scripts.

### 2025-08-27 (Infrastructure - naming revision & fix)
- Updated naming to CAF-style prefix ordering: `rg-userNNN`, `pip-userNNN`.
- Fixed Bicep BCP144 error by indexing module collection in output comprehension.

### 2025-08-27 (Infrastructure - per-user network + VM)
- Extended `userInfra.bicep` to provision VNet, Subnet, NSG (RDP/HTTP/HTTPS), NIC, and Windows Server 2022 VM per user.
- Added parameters for admin credentials, VM size, accelerated networking, and optional custom CIDRs.
- Updated `main.bicep` to pass secure admin credentials and fixed loop off-by-one (range now 1..n inclusive).
- README updated with new resource list & CLI examples including credentials.

### 2025-08-28 (Infrastructure - module refactor & lint fixes)
- Introduced `workload.bicep` (resource group scope) containing PIP, NSG, VNet, NIC, VM.
- Simplified `userInfra.bicep` to only create RG and call workload module.
- Addressed Bicep scope errors (BCP037/BCP139) and removed unnecessary dependsOn warnings.
- Updated README to document new module list.
### 2025-09-16 (Infrastructure - multi-script Custom Script Extension)
- Updated `baseInfra/bicep/workload.bicep` Custom Script Extension to download all modular provisioning scripts (`setup.ps1` orchestrator plus stage scripts: `SQL_install.ps1`, `App_install.ps1`, `Dev_install_initial.ps1`, `Dev_install_post_reboot.ps1`).
- Rationale: ensures orchestrator has local copies for idempotent stage execution and post-reboot scheduled task without needing additional network fetches beyond initial extension run.
- Implemented via new variables listing each raw GitHub URL and aggregated `provisioningScriptFiles` array passed to `fileUris`.
### 2025-09-16 (Dev Tools - system-wide VS Code)
- Modified `Dev_install_post_reboot.ps1` to install Visual Studio Code with `--scope machine` via winget.
- Added fallback to direct system installer download if winget machine-scope install returns non-zero exit code or throws.
- Rationale: original per-user install failed when script ran under SYSTEM before a user profile existed.
### 2025-09-19 (Infrastructure - Terraform azapi translation)
- Added Terraform implementation (`baseInfra/terraform`) mirroring Bicep per-user environment deployment using `azapi_resource` for all Azure resource types.
- Root module loops `n` user environments via `for_each` on range; each environment module provisions RG, Public IPs, NAT Gateway, NSG, VNet + subnets, NIC, Bastion, Windows VM, and Custom Script Extension applying existing provisioning scripts.
- Implemented rich variable descriptions per project guidance; outputs aggregate resource group, VM, and VNet names.
- Chose `azapi` exclusively for resources (still declaring `azurerm` provider to satisfy auth & data lookups) to meet requirement of using azapi instead of azurerm resources.
- Included sample `config.auto.tfvars` with placeholder password and guidance to override securely.
### 2025-09-19 (Infrastructure - Terraform module refactor)
- Split `modules/user_environment/main.tf` into multiple focused files: `variables.tf`, `locals.tf`, `main.tf` (RG only), `networking.tf`, `bastion.tf`, `vm.tf`, and `outputs.tf`.
- Rationale: improve readability, enable targeted future changes (e.g., swapping VM image or network rules) without touching unrelated logical sections.
- No functional changes; resource names, dependencies, and outputs remain identical for state continuity.
### 2025-09-19 (Infrastructure - Terraform variable simplification)
- Removed variables: `enable_accelerated_networking`, `override_vnet_address_space`, `override_subnet_prefix` to enforce consistent environment layout and reduce input surface.
- CIDR derivation now always `10.<index>.0.0/22` (VNet) with fixed `vms` `/24` and Bastion `/26`; accelerated networking hardcoded `false` for predictable provisioning across sizes.
- Updated root module, module interface, locals, and networking configuration accordingly; cleaned `config.auto.tfvars`.
### 2025-09-19 (Infrastructure - Terraform docs & outputs cleanup)
- Updated Terraform README to remove deprecated variables and clarify fixed CIDR scheme.
- Corrected root `outputs.tf` to reference `module.user_environment` (previously `module.user`).
- Added historical note explaining removal of override & acceleration variables.
### 2025-09-19 (Infrastructure - Terraform module providers declaration)
- Added `providers.tf` inside `modules/user_environment` declaring required providers (`azapi`, `azurerm`) for clearer module boundaries and potential future reuse.
- Left actual provider configuration only in root to avoid duplicate auth blocks per Terraform best practice.
### 2025-09-19 (Infrastructure - Terraform Entra user automation)
- Added `manage_entra_users` flag plus `entra_user_domain` and `entra_user_password` variables.
- Created `modules/entra_user` to provision one Entra ID user per environment (UPN pattern `labuserNNN@domain`).
- Added conditional Owner role assignment in `user_environment` module when a user object id supplied.
- Root outputs extended with user principal names and object IDs.
- README updated describing optional user provisioning and RBAC behavior.
### 2025-09-19 (Infrastructure - Terraform module variable docs)
- Added rich multiline descriptions to variables in `modules/user_environment/variables.tf` and `modules/entra_user/variables.tf` for clarity and parity with root variable documentation.
### 2025-09-19 (Infrastructure - Entra user naming alignment)
- Updated Entra user module to use `userNNN` (was `labuserNNN`) to match Azure resource naming convention (rg-userNNN, vm-userNNN, etc.).
### 2025-09-19 (Infrastructure - Role assignment naming fix)
- Replaced invalid `uuid()` usage with `uuidv5()` deterministic GUID for Owner role assignment resource in `user_environment` (rbac.tf) to ensure idempotent apply.
### 2025-09-19 (Infrastructure - Role assignment count fix)
- Introduced `create_role_assignment` explicit boolean to avoid unknown count evaluation.
- Updated rbac resource to use this flag instead of checking nullable ID directly and added lifecycle precondition validating presence of user object id.
### 2025-09-19 (Infrastructure - VM system-assigned managed identity)
- Enabled system-assigned managed identity on workshop VM (azapi VM body identity type SystemAssigned).
- Added Owner role assignment targeting VM identity principal for per-user resource group (separate from optional user Owner assignment).
- Updated Terraform README to reflect identity & RBAC change.
### 2025-09-19 (Infrastructure - RBAC refactor constants)
- Consolidated repeated Owner role GUID usage into locals (`owner_role_definition_id`, `role_assignment_ns`) in `rbac.tf` for maintainability.
- Updated uuidv5 calls to reference namespace local instead of duplicating GUID string.
### 2025-09-19 (Docs - README proofreading & consistency pass)
- Root `README.md`: grammar fixes (cloud leverage sentence, "you're interested" correction, clarified challenge 5 description, consistent environment variable terminology, tightened tips section wording).
- `solutions/ch01/README.md`: corrected numerous typos (appsetings/appsettings, yu/you), improved step wording, clarified Docker and ACR steps, rewrote bonus section for clarity.
- `solutions/ch02/README.md`: fixed misspellings (compes→comes, Lucost→Locust, Seoptember→September), restructured explanation of Load Testing vs Playwright, clarified metric interpretation.
- `solutions/ch03/README.md`: improved pipeline narrative, fixed grammar (int→at, paralel→parallel), standardized prompts and environment variable guidance.
- `solutions/ch04/README.md`: fixed phrasing (ready so send→ready to send), clarified OpenTelemetry Collector integration steps.
- `dataGenerator/README.md`: corrected "Fotorealistic" to "Photorealistic", added missing TARGET_COUNT variable, normalized numbering (sequential sections), improved educational disclaimer, adjusted JSON example.
- `solutions/ch04/bicep/README.md`: added duplication placeholder note recommending future monitoring-specific content.
- Minor consistency adjustments (use of “frontend”, “OpenTelemetry”, clarified non-vendor instrumentation approach).
### 2025-09-19 (Docs - Challenge 5 descriptions)
- Added detailed `challenges/ch05-enterprise/README.md` outlining enterprise security hardening focus (network isolation, private endpoints, WAF / Front Door, Entra ID auth, Managed Identity, CMK encryption, governance, observability) plus flexible deliverables.
- Added comprehensive `challenges/ch05-innovation/README.md` describing optional AI enhancement tracks (RAG chatbot, semantic search, translations, image generation, personalization) with architectural guidance and grounding best practices.
- Updated root `README.md` challenge listing with concise summaries for both flavors.
### 2025-09-20 (GitHub provisioning helper - interactive auth & env)
- Added `python-dotenv` dependency and `.env.sample` to `baseInfra/github`.
- Replaced placeholder `main.py` with implementation that:
	* Loads `.env` (token + desired org name)
	* Prompts for `GITHUB_TOKEN` if missing
	* Authenticates via PyGitHub and lists existing organizations
	* Documents limitation that free org creation must be manual (web flow) – no API call attempted
- Expanded `baseInfra/github/README.md` with usage instructions, token scope guidance, and limitation note.
- Future (not yet implemented): org member invites, repo templating, Azure billing linkage.
### 2025-09-20 (GitHub provisioning helper - switch to gh CLI auth)
- Refactored `baseInfra/github/main.py` to remove PAT prompting and rely exclusively on GitHub CLI (`gh auth token`).
- Updated `.env.sample` to drop `GITHUB_TOKEN` (now only `ORG_NAME`).
- Revised `baseInfra/github/README.md` to document gh-only authentication workflow and required setup steps.
- Rationale: simpler UX, no local secret storage, leverages existing secure token handling by GitHub CLI.
### 2025-09-20 (GitHub provisioning helper - gh status flag fix)
- Removed unsupported `--exit-code` flag from `gh auth status` invocation and replaced with output parsing fallback.
### 2025-09-20 (GitHub org access checker simplification)
- Simplified `baseInfra/github/main.py` to only validate access to `ORG_NAME` using GitHub CLI token.
- Removed listing of organizations and token validation verbosity; output now single-line `OK <org>` or error.
- Updated README to reflect new purpose and exit codes (0=success,1=config/auth,2=no access).
### 2025-09-20 (GitHub repo copy & template scaffolding)
- Added env vars `SOURCE_REPO`, `TARGET_REPO_NAME`, `MAKE_TEMPLATE` plus sample values.
- Extended `main.py` to create a new repository in target org (idempotent) and optionally flag it as template.
- Current limitation: repository content is not auto-copied; script notes manual steps to push source contents.
- README updated with usage and manual content population instructions.
### 2025-09-20 (GitHub repo copy simplification)
- Removed `TARGET_REPO_NAME` and `MAKE_TEMPLATE` options; destination name now always matches source repo name and repository is always marked as template.
- Updated `.env.sample`, README, and logic in `main.py` accordingly.
### 2025-09-20 (GitHub repo content synchronization)
- Added GitPython dependency and implemented automatic content sync: when copying a public `SOURCE_REPO`, if the destination org repo is newly created or empty (no branches), script clones source (full history) and pushes only the default branch to destination, then marks repo as template.
- Idempotent reruns: skip sync if destination already has branches; still enforce template flag.
- README updated to describe automated sync and provide manual mirror instructions for all refs.
### 2025-09-20 (GitHub helper README simplification)
- Rewrote `baseInfra/github/README.md` into a concise 5-step quick guide (create org, install/login gh CLI, configure `.env`, install deps, run). Removed verbose explanations to streamline onboarding.
### 2025-09-20 (GitHub helper user provisioning)
- Added `users.yaml.sample` and support for `USERS_FILE` env var (default `users.yaml`).
- Script now:
	* Invites users (by login or email) with role member/admin via GitHub CLI API calls.
	* Creates per-user repos named `<login>-<templateRepo>` when `SOURCE_REPO` provided, copying template content (default branch history).
	* Skips existing members/repos; resilient to partial failures.
- Added PyYAML dependency for parsing.
### 2025-09-20 (GitHub helper refactor & output normalization)
- Refactored `main.py` into smaller functions: `_ensure_org_template_repo`, `_handle_users_file`, `_invite`, `print_step`, `print_ok`.
- Standardized all progress output to "Action ... ✔" lines; removed ad-hoc NOTE messages.
- Added explicit step messages for template marking, content sync, per-user repo creation, and invitations.
### 2025-09-21 (GitHub helper - switch invitations to PyGithub)
- Replaced `gh api` subprocess-based invitation flow with direct PyGithub `Organization.create_invitation` calls (login -> invitee_id, email -> email param).
- Added heuristic handling for HTTP 422 responses: treat messages indicating existing membership or pending invite as success (idempotent reruns).
- Distinguish 403 (insufficient privileges / missing admin:org scope) from other errors in output.
- Simplifies code (no subprocess parsing) and unifies error handling via `GithubException`.
### 2025-09-21 (GitHub helper - enforce handle-only invites)
- Removed email-only invitation path; each users.yaml entry must supply `login`.
- Skips and logs entries missing login (`Skipping entry (login missing)`).
- Simplifies per-user repo logic (no unknown-login branch) and invitation helper signature.
### 2025-09-21 (GitHub helper - private per-user repos & access control)
- Modified per-user repository creation to force `private=True` regardless of template visibility, meeting requirement that only the user and org admins can access.
- Added explicit collaborator grant (`push` permission) for the owning user after repo creation and content sync to ensure access even if future default org base permissions are restricted.
- Output now includes a step line: `Granting access to <login> on <repo>` with success check mark; failures surface HTTP status for troubleshooting.
### 2025-09-21 (GitHub helper - per-user logging delimiter & counters)
- Added delimiter line `---` before each user block plus header `Processing <login> (<n> remaining)` to make multi-user runs easier to scan.
- Remaining count reflects only entries with a valid `login` key (skips invalid entries) for accurate progress reporting.
- Maintains consistent step/checkmark output style for new header lines (idempotent reruns unaffected).
### 2025-09-21 (GitHub helper - switch to template snapshot generation for user repos)
- Replaced clone/push history-preserving approach with GitHub server-side template generation (`/generate` endpoint) for per-user repositories.
- Significantly faster for many users; does not retain original commit history (intentional per requirement to only need a snapshot).
- Added helper `_generate_from_template` for low-level POST; reused existing collaborator grant step post-generation.
### 2025-09-21 (GitHub helper - README Azure billing note)
- Added explicit README section describing manual-only Azure subscription billing linkage steps and optional `.env` identifiers (`AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID`).
- Clarified per-user repo creation now uses snapshot (no history) from template.
### 2025-09-21 (GitHub helper - Copilot seat assignment)
- Added support for assigning GitHub Copilot Business seats to users flagged with `copilot: true` in `users.yaml`.
- Bulk assigns after processing all users via POST `/orgs/{org}/copilot/billing/selected_users` (treats 422 as idempotent success; surfaces 403 privilege errors).
- Requires token scopes/permissions capable of managing Copilot billing (e.g., manage_billing:copilot or sufficient org admin rights with appropriate fine-grained token).
### 2025-09-21 (GitHub helper - per-user Copilot seat assignment refinement)
- Changed seat assignment from bulk post-loop to immediate per-user invocation for faster feedback and clearer error correlation.
- Introduced `_assign_copilot_seat` wrapper (reuses bulk endpoint with single username) maintaining idempotent 422 handling.
- Removed accumulation list logic; each `copilot: true` user now emits its own assignment step.
### 2025-09-21 (GitHub helper - Copilot diagnostics & verification)
- Added pre-flight subscription status check (`_copilot_preflight`) printing plan, seat mode, and public code policy.
- Enhanced 422 handling with message snippet instead of silent success assumption.
- Added post-assignment verification (`_verify_copilot_seat`) to confirm active seat or report pending state.
- Provides clearer reasons when assignments are skipped (e.g., not enabled, wrong seat mode, billing/policy issues).
### 2025-09-22 (GitHub helper - remove Copilot automation)
- Removed all Copilot-related logic (pre-flight subscription check, seat assignment, verification).
- Rationale: organization will manage Copilot features & seats manually; API endpoints for advanced features not publicly supported/stable.
- Simplified `_handle_users_file` to only perform invitations and per-user repository provisioning.
- Removed helper functions `_assign_copilot_seats`, `_assign_copilot_seat`, `_copilot_preflight`, `_verify_copilot_seat` and associated output paths.
### 2025-09-22 (GitHub helper - Codespaces policy enablement)
- Added `_ensure_codespaces_all` to attempt setting organization Codespaces permissions to allow all members & repositories.
- Best-effort: logs non-fatal error (e.g., 404 if endpoint unavailable for plan or preview not enabled) and continues.
- Rationale: streamline workshop setup so every invited member can open Codespaces without manual org settings adjustment.
### 2025-09-22 (GitHub helper - simplify user list format)
- Simplified `users.yaml` and sample to plain list of GitHub usernames (removed `is_admin`, `copilot`).
- Updated parsing to accept either new string list or legacy dict entries (backward compatible).
- Invitation now always uses role `direct_member`; admin elevation handled manually if needed.
### 2025-09-22 (GitHub helper - Codespaces policy graceful handling)
- Adjusted `_ensure_codespaces_all` to treat 404 as non-fatal with "endpoint not available (skipping)" message.
- Added `SKIP_CODESPACES_POLICY` env var gate (values 1/true/yes) to bypass policy attempt entirely.
- Rationale: avoid noisy ERROR output for orgs/plans without the endpoint while keeping idempotent setup.
### 2025-09-22 (GitHub helper - Codespaces endpoint correction)
- Replaced undocumented `/codespaces/permissions` usage with documented `PUT /orgs/{org}/codespaces/access` for visibility management.
- Added `CODESPACES_INCLUDE_OUTSIDE` env var to toggle inclusion of outside collaborators (`all_members_and_outside_collaborators`).
- Endpoint unavailability (404), insufficient permission (403), or validation (422) now reported as neutral skip lines.
### 2025-09-22 (GitHub helper - Codespaces policy removal & run summary)
- Removed `_ensure_codespaces_all` invocation and function after decision to manage Codespaces visibility manually outside automation (reduced moving parts, avoided misleading 404 skips on orgs without feature enabled).
- Added end-of-run summary block with delimiter `---` reporting counts: invited, already members, per-user repos created, repos skipped.
- Rationale: focus script strictly on deterministic idempotent provisioning (org access check, template repo, user invites, per-user repos) and improve operator feedback while keeping output concise.
### 2025-09-24 (Infrastructure - Terraform subscription parameterization)
- Replaced hard-coded subscription GUID in `providers.tf` with new `subscription_id` variable.
- Added `subscription_id` entry to `config.auto.tfvars` for default workshop usage; value can now be overridden via `TF_VAR_subscription_id` env var or CLI flag without editing provider file.
- Updated variable documentation (`variables.tf`) explaining fallback behavior if omitted and rationale (portability & reproducibility across environments).
- No impact to state: provider configuration change only; resources remain bound to same subscription when value unchanged.
### 2025-09-24 (Infrastructure - Multi-region distribution)
- Removed single `location` variable in favor of required `locations` list.
- Implemented round-robin mapping of user index -> region `(i-1) % len(locations)` in root `main.tf` (`user_location_map`).
- Updated `config.auto.tfvars` sample to include two regions and documentation in Terraform README (variables table + new Region Distribution section with example).
- Validation enforces at least one non-empty region; changing assigned region for existing index forces full environment recreation (expected behavior noted in docs).
### 2025-09-24 (Infrastructure - Revert VNet race mitigation change)
- Reverted two-phase VNet + separate subnet resources back to original single azapi VNet resource with inline subnet definitions.
- Reason: separate subnet resources introduced update/idempotency complications; opting to keep simpler inline model despite occasional transient 404 previously under investigation.
- NIC subnet reference restored to string interpolation form (`.../subnets/vms`).

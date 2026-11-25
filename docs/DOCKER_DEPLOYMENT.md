# LiBot Backend Docker Deployment

This repository packages only the LiBot ASP.NET Core API. SQL Server remains installed on the Azure VM and is never placed in a container.

## Architecture

The flow is Windows development machine -> GitHub -> GitHub Actions -> GHCR -> Azure VM Docker container. The container listens on port 8080, and the VM maps 5205:8080. The container connects to the existing VM SQL Server through host.docker.internal,1433. Port 1433 must remain private.

The API can run LiBotDatabaseInitializer during startup. Local development keeps this enabled by default so EF Core migrations and reference-data seeding continue to work. The Azure deployment script forces Database__ApplyMigrations=false, so it does not migrate or seed the existing LiBotDb database. The script does not delete the database or recreate SQL Server.

The existing health endpoint is GET /api/health. It is a liveness endpoint and does not query SQL Server. Startup failures are visible in docker logs.

## Local Docker build

Run in Windows PowerShell from the repository root:

    docker version
    docker build --progress=plain -t libbot-api:local .

The multi-stage Dockerfile restores and publishes src/LiBot.Api/LiBot.Api.csproj. The final image contains only published output and the .NET 8 ASP.NET runtime. It runs as the non-root APP_UID user.

## Local container run

Create an environment file outside Git and replace its placeholders:

    Copy-Item .env.example .env.docker
    notepad .env.docker

For SQL Server running on the Windows host, use SQL authentication with Server=host.docker.internal,1433. A Linux .NET container cannot use Trusted_Connection=True without additional Kerberos configuration.

    docker run --rm --name libot-api-local --add-host=host.docker.internal:host-gateway --env-file .env.docker --env ASPNETCORE_URLS=http://+:8080 --publish 5205:8080 libbot-api:local

Check the API from another PowerShell window:

    Invoke-WebRequest http://localhost:5205/api/health
    docker logs --tail 100 libot-api-local

Remove the local file after testing with Remove-Item .env.docker.

## Environment variables

Use ASP.NET Core names such as ConnectionStrings__LiBot and VoiceTools__ApiToken. The image does not contain .env, passwords, tokens, or SSH keys. Production values include:

- ConnectionStrings__LiBot: use Server=host.docker.internal,1433;Database=LiBotDb plus the SQL login configured on the VM.
- VoiceTools__ApiToken: a long random service token.
- LiBot__Host, LiBot__Port, and LiBot__RuntimeEndpoints__real__*: reachable robot runtime settings.
- LiBot__BackendBaseUrl: the backend URL reachable by the robot.
- ASPNETCORE_URLS: http://+:8080 on Azure; the deployment script also enforces this value.
- Database__ApplyMigrations: false on Azure so the existing database schema and data are not changed by container startup.

Store real values in /var/www/libbot-api/.env on the VM with mode 600. .env.example contains placeholders only.

## GitHub Actions and GHCR

.github/workflows/deploy.yml runs on pushes to main and through workflow_dispatch. It restores, builds, tests, builds the image, and publishes:

    ghcr.io/capstonelibbot/libbot-api:latest
    ghcr.io/capstonelibbot/libbot-api:<commit-sha>

The publish job uses GITHUB_TOKEN with contents: read and packages: write. A manual run has a deploy input that defaults to false.

Configure these GitHub secrets before using deployment:

- AZURE_HOST: VM hostname or public IP.
- AZURE_USER: azureadmin.
- AZURE_SSH_KEY: complete SSH private key used by Actions.
- AZURE_KNOWN_HOSTS: verified known_hosts line for the VM.
- GHCR_READ_TOKEN: optional read:packages token when the GHCR package is private.

The deploy job assumes that scripts/deploy-azure.sh is installed at /var/www/libbot-api/deploy-azure.sh and /var/www/libbot-api/.env already exists. It uses strict SSH host-key checking. The package may be public, or the VM may be authenticated to GHCR with a token that has only read:packages.

## Prepare the Azure VM

Run in an SSH terminal on Ubuntu. These commands do not install SQL Server, change the database, or open port 1433:

    sudo mkdir -p /var/www/libbot-api
    sudo chown -R "$USER":"$USER" /var/www/libbot-api
    chmod 700 /var/www/libbot-api

Install the script from a trusted Windows PowerShell session. Replace the SSH key path if necessary:

    scp -i "$env:USERPROFILE\.ssh\azure_libot_new" .\scripts\deploy-azure.sh azureadmin@20.239.60.50:/var/www/libbot-api/deploy-azure.sh

Back on the VM:

    chmod 700 /var/www/libbot-api/deploy-azure.sh
    nano /var/www/libbot-api/.env
    chmod 600 /var/www/libbot-api/.env

The VM file must include real values. Its database entry must use the existing host SQL Server:

    ASPNETCORE_URLS=http://+:8080
    Database__ApplyMigrations=false
    ConnectionStrings__LiBot=Server=host.docker.internal,1433;Database=LiBotDb;User Id=REPLACE_WITH_SQL_LOGIN;Password=REPLACE_WITH_PASSWORD;MultipleActiveResultSets=True;TrustServerCertificate=True
    VoiceTools__ApiToken=REPLACE_WITH_LONG_RANDOM_SECRET
    LiBot__Host=REPLACE_WITH_ROBOT_HOST
    LiBot__Port=9000
    LiBot__ConnectTimeoutSeconds=5
    LiBot__ResponseTimeoutSeconds=120
    LiBot__RuntimeEndpoints__real__Host=REPLACE_WITH_ROBOT_HOST
    LiBot__RuntimeEndpoints__real__Port=9000
    LiBot__BackendBaseUrl=http://20.239.60.50:5205

Confirm SQL Server is reachable from the VM without opening it publicly:

    sudo ss -lntp | grep ':1433'

## Deploy and update

Run on the Azure VM after the environment file and script are prepared:

    IMAGE_TAG=<commit-sha> /var/www/libbot-api/deploy-azure.sh

For a private package, authenticate Docker on the VM with a read:packages token, or pass it through a protected session without saving it in the repository:

    read -r -s GHCR_READ_TOKEN
    export GHCR_USERNAME=capstonelibbot
    export GHCR_READ_TOKEN
    IMAGE_TAG=<commit-sha> /var/www/libbot-api/deploy-azure.sh
    unset GHCR_READ_TOKEN

The script pulls the image, preserves the old container until the replacement remains running, maps 5205:8080, adds host.docker.internal:host-gateway, and uses --restart unless-stopped. On failure it prints recent logs and attempts to restore the previous container.

## Logs, troubleshooting, and rollback

Run on the VM:

    docker ps --filter 'name=^/libot-api$'
    docker logs --tail 200 libot-api
    docker logs -f libot-api
    curl --fail http://127.0.0.1:5205/api/health

Use MultipleActiveResultSets=True or False, never an empty value. For SQL connection errors, verify the SQL listener, credentials, and host.docker.internal,1433 from the VM. For image pull errors, verify GHCR visibility or read:packages authentication. Do not expose port 1433 to fix a container connection problem.

Rollback to a previously published image without changing the database:

    IMAGE_TAG=<previous-commit-sha> /var/www/libbot-api/deploy-azure.sh

## Validation

Run in Windows PowerShell:

    dotnet build LiBot.sln --configuration Release
    dotnet test tests\LiBot.VoiceTools.Tests\LiBot.VoiceTools.Tests.csproj --configuration Release
    dotnet test tests\LiBot.Zone.Tests\LiBot.Zone.Tests.csproj --configuration Release
    docker build --progress=plain -t libbot-api:validation .

The Azure deploy job is intentionally manual and is not run by this repository preparation task.


# Self-Hosted Single-Layer Application with Single-Port Hosting

This document describes how to host a **Single-Layer ABP Framework version 10** application using **Angular(21)** and **ASP.NET Core (.NET 10)** on a **single port**, deployed as a **Windows Service** using **Kestrel**.

The Angular application is served as a static SPA from the ASP.NET Core application, while **HTTP APIs** and authentication endpoints are exposed through route prefixes.

---

## Overview

This solution is built using the **ABP Single-Layer (Single Project) architecture**.

In this hosting model:

- All application layers (domain, application services, data access, and HTTP APIs) are hosted within a single ASP.NET Core project
- The Angular frontend is built separately and served as static files by the same ASP.NET Core application
- Both the backend APIs and the frontend are hosted on a single HTTPS port
- The application is self-hosted using Kestrel and does not require IIS or a reverse proxy

This approach is suitable for small to medium-sized internal applications, on-premise deployments, and offline environments where simplicity and ease of deployment are primary concerns.

---

## Why Use This Hosting Model?

This hosting model is designed to simplify deployment and operations for **small to medium-sized internal applications**.

### Single-Port Hosting for Backend and Frontend

Serving both the backend APIs and the Angular frontend from the **same port** provides the following benefits:

- Simplifies server and network configuration
- Avoids cross-origin requests by using a same-origin deployment model
- Eliminates the need for complex or permissive Cross-Origin Resource Sharing (CORS) configuration
- Avoids managing multiple domains or ports
- Enables same-origin authentication and authorization

### Using Kestrel Without IIS

ASP.NET Core includes the **Kestrel** web server as a built-in, high-performance hosting option.

For small internal or on-premise applications:

- IIS is not strictly required
- Kestrel provides sufficient performance and stability
- Deployment is simplified by avoiding IIS configuration and maintenance
- The application can be distributed as a self-contained deployment folder with an entry executable

### Running as a Windows Service

Running the application as a **Windows Service** provides the following advantages:

- Automatic startup when the operating system starts
- No manual user interaction required to run the application
- Improved reliability for long-running services
- Better integration with Windows service management and monitoring

---

## Required NuGet Packages

```bash
dotnet add package Volo.Abp.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.Extensions.Hosting.WindowsServices
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Async
dotnet add package Serilog.Sinks.EventLog
```

---

## Minimal `.csproj` Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Volo.Abp.EntityFrameworkCore.Sqlite" />
    <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" />
    <PackageReference Include="Serilog.AspNetCore" />
    <PackageReference Include="Serilog.Sinks.Async" />
    <PackageReference Include="Serilog.Sinks.EventLog" />
  </ItemGroup>
</Project>
```

---

## Architecture

### Components

- **ASP.NET Core Host**  
  Hosts ABP Framework modules, HTTP APIs, authentication endpoints, and static files.

- **Angular SPA**  
  Built separately and served from the ASP.NET Core static file middleware.

- **SQLite Database**  
  Used as the application database via Entity Framework Core.
  This lightweight, file-based database is suitable for single-node,
  on-premise, and offline deployments.

- **Kestrel Web Server**  
  Hosts the application over HTTPS.

- **Windows Service**  
  Runs the application as a background service.

---

### Request Routing

| Path | Description |
| --- | --- |
| `/` | Redirects to `/app` |
| `/app/*` | Angular SPA and client-side routes |
| `/api/*` | ABP HTTP APIs |
| `/connect/*` | OpenIddict endpoints |
| `/account/*` | Account endpoints |
| `/swagger` | Swagger UI |
| `/health-status` | Health check JSON endpoint |
| `/health-ui` | Health check UI |

---

## Solution Structure (Example)

```cmd
SelfHostApp/
├── angular/
│   ├── src/
│   │   ├── app/
│   │   ├── assets/
│   │   └── environments/
│   ├── angular.json
│   ├── package.json
│   └── dist/
│
├── SelfHostApp/
│   ├── Controllers/
│   ├── Data/
│   ├── HealthChecks/
│   ├── Localization/
│   ├── Migrations/
│   ├── Permissions/
│   ├── Services/
│   ├── wwwroot/
│   │   └── app/
│   ├── Program.cs
│   ├── SelfHostApp.csproj
│   ├── SelfHostAppModule.cs
│   └── appsettings.json
│
├── migrate-database.ps1
├── SelfHostApp.slnx
└── README.md
```

---

## Hosting Angular as a Static SPA

Angular is built separately and served as static files by ASP.NET Core.

### SPA Fallback Configuration

in `program.cs` add after `app.InitializeApplicationAsync();`

```csharp
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/app"));

app.MapFallbackToFile(
    "/app/{*path:nonfile}",
    "app/index.html"
);
```

**This ensures:**

- Angular is served from /app

- Client-side routing works correctly

- Refreshing deep links does not return 404s

While running as windows service if you want logs, then you can add in `program.cs`

```csharp
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, "logs-.txt");

        var loggerConfiguration = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true
            ));

        if (!WindowsServiceHelpers.IsWindowsService())
        {
            loggerConfiguration.WriteTo.Console();
        }

        if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
        {
            loggerConfiguration.WriteTo.EventLog(
                source: "SelfHostApp",
                manageEventSource: true,
                restrictedToMinimumLevel: LogEventLevel.Information
            );
        }

...

```

---

## Application Configuration

The following examples show the relevant configuration for Kestrel HTTPS hosting,
single-port frontend/backend hosting, authentication, database access, and local
development launch settings.

### ✅ Section 1: `appsettings.json`

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:44321",
        "Certificate": {
          "Path": "C:\\Services\\SelfHostApp\\SelfHostAppHttps.pfx",
          "Password": "SelfHostApp@123"
        }
      }
    }
  },

  "App": {
    "SelfUrl": "https://localhost:44321",
    "ClientUrl": "https://localhost:44321/app",
    "CorsOrigins": "https://localhost:44321",
    "RedirectAllowedUrls": "https://localhost:44321,https://localhost:44321/app",
    "HealthCheckUrl": "/health-status"
  },

...

  "ConnectionStrings": {
    "Default": "Data Source=C:\\SQLiteDB\\SelfHostApp.db;" 
  },
   "AuthServer": {
    "Authority": "https://localhost:44321",
    "SwaggerClientId": "SelfHostApp_Swagger",
    "CertificatePassPhrase": "fdc9de61-f3be-4d9e-a9d8-ea94e2779ed8"
  },
  "Settings": {
    "Abp.Identity.Password.RequireNonAlphanumeric": "false",
    "Abp.Identity.Password.RequireLowercase": "false",
    "Abp.Identity.Password.RequireUppercase": "false",
    "Abp.Identity.Password.RequireDigit": "false"
  },
  "StringEncryption": {
    "DefaultPassPhrase": "DqYoPZoo7xyJgbYw"
  },
  "OpenIddict": {
    "Applications": {
      "SelfHostApp_App": {
        "ClientId": "SelfHostApp_App",
        "RootUrl": "https://localhost:44321/app"
      },
      "SelfHostApp_Swagger": {
        "ClientId": "SelfHostApp_Swagger",
        "RootUrl": "https://localhost:44321"
      }
    }
  }
}
```

### Notes

- Backend and frontend share the same origin

- `/app` is the SPA root

- The `Kestrel` section defines the HTTPS URL that the Windows Service listens on.

- `SelfHostAppHttps.pfx` is the HTTPS certificate used by Kestrel.

- `openiddict.pfx` is separate and is used by OpenIddict for token signing and encryption.

- Because the `Kestrel` section is in `appsettings.json`, the HTTPS certificate file
  must exist anywhere this application is started, including local development and
  Windows Service hosting.

### ✅ Section 2: `launchSettings.json` (`Properties/launchSettings.json`)

The following configuration is used **only during local development**.
It is not used when running the application as a Windows Service.

```json
{
  "profiles": {
    "SelfHostApp": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "https://localhost:44321",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Purpose

- Aligns local development with production hosting
- Ensures correct routing and asset resolution
- Reduces environment-specific issues

---

## Next: Application Development (ABP Conventions)

- **Domain Model** – Define entities and aggregate roots  
- **Persistence** – Configure entity mapping and EF Core migrations  
- **Application Services** – Expose use cases using DTOs  
- **Security** – Enable authentication and permission-based authorization  
- **Initial Data** – Seed users, roles, and permissions  
- **Localization** – Register UI, menu, and permission texts  
- **Client Proxies** – Generate Angular service clients  
- **UI Integration** – Add components, routes, and menus  

---

## Angular Environment Configuration

Configure the Angular environment files to use the same base URL as the backend API.

Modify `environment.ts` located under `angular/src/environments/`:

```typescript
import { Environment } from '@abp/ng.core';

const baseUrl = 'https://localhost:44321';

const oAuthConfig = {
  issuer: baseUrl + '/',
  redirectUri: baseUrl + '/app',
  clientId: 'SelfHostApp_App',
  responseType: 'code',
  scope: 'offline_access SelfHostApp',
  requireHttps: true,
};

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'SelfHostApp',
  },
  oAuthConfig,
  apis: {
    default: {
      url: baseUrl,
      rootNamespace: 'SelfHostApp',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
} as Environment;
```

Modify `environment.prod.ts` in the same directory:

```typescript
import { Environment } from '@abp/ng.core';

const baseUrl = 'https://localhost:44321';

const oAuthConfig = {
  issuer: baseUrl + '/',
  redirectUri: baseUrl + '/app',
  clientId: 'SelfHostApp_App',
  responseType: 'code',
  scope: 'offline_access SelfHostApp',
  requireHttps: true,
};

export const environment = {
  production: true,
  application: {
    baseUrl,
    name: 'SelfHostApp',
  },
  oAuthConfig,
  apis: {
    default: {
      url: baseUrl,
      rootNamespace: 'SelfHostApp',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
} as Environment;
```

### Notes

- Both environments use the same `baseUrl` because the application is hosted on a
  single HTTPS endpoint in both development and production.

- The `/app` path matches the Angular SPA hosting configuration in ASP.NET Core.

- The OAuth configuration must match the OpenIddict application settings
  configured on the backend.

---

## Angular Base Path Configuration

Angular must be configured to run under `/app` . Modify `angular.json` to configure the Angular application for single-port hosting under the `/app` path.

add `baseHref`,`deployUrl` and define same `port` number as `api`.

```json
"sourceRoot": "src",
      "prefix": "app",
      "architect": {
        "build": {
          "builder": "@angular/build:application",
          "options": {
            "outputPath": "dist/SelfHostApp",
            "index": "src/index.html",
            "browser": "src/main.ts",
            "polyfills": ["src/polyfills.ts"],
            "baseHref": "/app/",
            "deployUrl": "/app/",
            ...
          }
          "serve": {
          "builder": "@angular/build:dev-server",
          "options": {
            "port": 44321,
            "ssl": true
          },
          ...
          }
        }
      }
```

The Angular application is configured to run under the `/app` path. In the
published application, Angular is served by ASP.NET Core on the same HTTPS port
as the backend API.

If running `ng serve` separately during development, ensure it does not conflict
with the backend process listening on the same port.

- **`baseHref`**  
  Sets the base URL for the Angular router.  
  Since the Angular application is hosted under `/app`, the `baseHref` value must be
  set to `/app/` (`/wwwroot/app`) to ensure that routing and asset resolution work correctly.

- **`deployUrl`**  
  Defines the base path for loading static assets such as JavaScript, CSS, and images.  
  Setting `deployUrl` `/app/` (`/wwwroot/app`) to ensures that all assets are loaded relative to the
  Angular application path when served from the ASP.NET Core `wwwroot` directory.

- **`port`**  
  Configures the Angular development server port. The checked-in value matches
  the backend port to mirror the published single-port model, but only one
  process can listen on the port at a time during separate development runs.

- **`ssl`**  
  Enables HTTPS for the Angular development server.  
  This is required to match the HTTPS configuration of the backend and to ensure
  compatibility with authentication and secure cookies.

These settings ensure that the Angular application behaves consistently in both
development and production environments.

---

## Angular Build During Publish (MSBuild)

The Angular frontend is built automatically during the ASP.NET Core publish process using an MSBuild target defined in the backend project’s `.csproj` file.

This removes the need for manual Angular build steps and ensures that the frontend and backend are always deployed together.

```xml
<PropertyGroup>
 ...

 	<SpaRoot>..\angular\</SpaRoot>
	<SpaDist>$(SpaRoot)dist\SelfHostApp\browser\</SpaDist>
</PropertyGroup>

<Target Name="BuildAngular"
        Condition="'$(Configuration)' == 'Release'"
        BeforeTargets="Publish">

	<Message Text="Building Angular SPA (production)..." Importance="high" />

	<Exec WorkingDirectory="$(SpaRoot)" Command="yarn install --frozen-lockfile" />
	<Exec WorkingDirectory="$(SpaRoot)" Command="yarn build:prod" />

<ItemGroup>
		<SpaDistFiles Include="$(SpaDist)**\*" />
</ItemGroup>

 	<RemoveDir Directories="wwwroot\app" Condition="Exists('wwwroot\app')" />
	<MakeDir Directories="wwwroot\app" />

	<Copy
    SourceFiles="@(SpaDistFiles)"
    DestinationFolder="wwwroot\app\%(RecursiveDir)"
    SkipUnchangedFiles="true" />
</Target>
```

### How It Works

- The ASP.NET Core project defines Angular paths using `SpaRoot` and `SpaDist`

- A custom MSBuild target runs **before the Publish target**

- Angular is built **only when the configuration is Release**

- The compiled SPA is copied into `wwwroot/app`

- ASP.NET Core serves the Angular app as static files

This target does not run for Debug builds or during `dotnet run`.

### Build Trigger

The Angular build is triggered from the backend project file (`.csproj`) during:

```cmd
dotnet publish -c Release
```
---
## Solution Structure Assumptions

The Angular build and publish process assumes the following solution structure:

```text
SelfHostApp/
├── angular/
│   ├── src/
│   │   ├── app/
│   │   ├── assets/
│   │   └── environments/
│   ├── angular.json
│   ├── package.json
│   └── dist/
│
├── SelfHostApp/
│   ├── Controllers/
│   ├── Data/
│   ├── HealthChecks/
│   ├── Localization/
│   ├── Migrations/
│   ├── Permissions/
│   ├── Services/
│   ├── wwwroot/
│   │   └── app/
│   ├── Program.cs
│   ├── SelfHostApp.csproj
│   ├── SelfHostAppModule.cs
│   └── appsettings.json
│
├── migrate-database.ps1
├── SelfHostApp.slnx
└── README.md
```

### How Files Are Copied

During `dotnet publish`, the backend project uses an MSBuild `Copy` task to move
the Angular production build output into the ASP.NET Core static files directory.

```xml
<Copy
  SourceFiles="@(SpaDistFiles)"
  DestinationFolder="wwwroot\app\%(RecursiveDir)"
  SkipUnchangedFiles="true" />
```

### Notes

- The Angular output path is defined by `SpaDist` in the backend `.csproj` file.

- All files from the Angular `browser` build output are copied into `wwwroot/app`.

- The directory structure is preserved automatically using `%(RecursiveDir)`.

- If the Angular project name or output path changes, only `SpaRoot` or `SpaDist`
needs to be updated in the `.csproj`.

- No frontend copy scripts or post-build hooks are required.

---

## Publishing the Application

Run the following command from the directory that contains the ASP.NET Core
project (`.csproj`) file.

```cmd
dotnet publish -c Release -r win-x64 --self-contained true -o C:\Services\SelfHostApp
```

This produces a self-contained deployable application folder with `SelfHostApp.exe`
as the entry executable.

---

## HTTPS Certificate for Kestrel

Create or provide a separate HTTPS certificate for Kestrel.

For local or internal testing, an exported development certificate can be created
from an elevated Command Prompt:

```cmd
dotnet dev-certs https -ep C:\Services\SelfHostApp\SelfHostAppHttps.pfx -p SelfHostApp@123
```

The generated certificate path and password must match the `Kestrel` section in
`appsettings.json`:

```json
"Certificate": {
  "Path": "C:\\Services\\SelfHostApp\\SelfHostAppHttps.pfx",
  "Password": "SelfHostApp@123"
}
```

The Windows Service account must have permission to read this `.pfx` file.

For production environments, use a certificate issued by a trusted certificate
authority instead of a development certificate.

---

## Configure to Run as a Windows Service

Run the following commands from an **elevated Command Prompt**
(Administrator privileges are required).

### Create the Windows Service

```cmd
sc create SelfHostApp ^
binPath= "C:\Services\SelfHostApp\SelfHostApp.exe" ^
start= auto
```

The listening URL and HTTPS certificate are read from the `Kestrel` section in
`appsettings.json`. `launchSettings.json` is used only during local development
and is not applied when the application runs as a Windows Service.

### Start the Service

```cmd
sc start SelfHostApp
```

### Query Service Status

```cmd
sc query SelfHostApp
```

If the service is running, the output will be similar to the following:

```cmd
SERVICE_NAME: SelfHostApp
        TYPE               : 10  WIN32_OWN_PROCESS
        STATE              : 4  RUNNING
        ...
```

This indicates that the service has started successfully.

After the service has started, open a web browser and navigate to: `https://localhost:44321/app`

The Angular application is served by the ASP.NET Core backend.

After accessing the application, permissions can be granted to `roles` or `users`
to allow performing application-specific operations.

Permissions can be managed from the **Administration** section of the application
by assigning the relevant application permissions to `roles` or individual `users`.

### Stop the Service

```cmd
sc stop SelfHostApp
```

---

## Logging

Logs are written to: `C:\Services\SelfHostApp\Logs\`

**Supports**:

- Rolling file logs
- Windows Event Log (when running as service)

---

## Advantages

- Single executable simple deployment
- Single-port hosting
- No IIS dependency
- Suitable for offline environments

---

## Limitations

- Windows-only deployment as it is used as Windows Service.

---

## Extending the Solution

- Support Linux deployments
- Use external database providers

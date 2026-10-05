# Railway Deployment Guide for Student Clearance System

## Prerequisites

1. Railway account with an active project
2. A service running the Student Clearance web application
3. PostgreSQL database (either Railway-hosted or external)

## Step 1: Set Up PostgreSQL Database

### Option A: Use Railway PostgreSQL Plugin (Recommended)

1. In your Railway project dashboard, click **+ Add**
2. Select **PostgreSQL**
3. Wait for the database to be provisioned (~1-2 minutes)
4. The database will automatically set environment variables:
   - `PGHOST`
   - `PGPORT`
   - `PGDATABASE`
   - `PGUSER`
   - `PGPASSWORD`

### Option B: Link External PostgreSQL

If using an external database, configure these environment variables manually:
- `PGHOST`: Database host
- `PGPORT`: Database port (usually 5432)
- `PGDATABASE`: Database name
- `PGUSER`: Username
- `PGPASSWORD`: Password

Or set a complete connection string:
- `ConnectionStrings__DefaultConnection`: `postgresql://user:password@host:port/database`

## Step 2: Configure Required Environment Variables

In Railway, go to your service's **Variables** tab and add the following:

### Database Configuration
- **Option 1 (Automatic from PostgreSQL plugin)**: No action needed - Railway sets PGHOST, PGPORT, PGDATABASE, PGUSER, PGPASSWORD automatically
- **Option 2 (Manual connection string)**:
  - Key: `ConnectionStrings__DefaultConnection`
  - Value: `postgresql://username:password@hostname:5432/database_name`

### Initial Admin Account
- Key: `InitialAdmin__Password` (or `InitialAdmin_Password`)
- Value: A strong password for the admin account (e.g., `SecureAdminPass123!`)
- Key: `InitialAdmin__Email` (optional)
- Value: Admin email (defaults to `admin@university.edu` if not set)

## Step 3: Environment Variable Naming Conventions

Railway uses the following pattern for nested configuration:
- Underscores (`_`) in environment variable names map to colons (`:`) in configuration
- `InitialAdmin__Password` → `InitialAdmin:Password`
- `ConnectionStrings__DefaultConnection` → `ConnectionStrings:DefaultConnection`

**Important**: Use double underscores (`__`) for nested configuration in environment variables.

## Step 4: Dockerfile Configuration (Already Configured)

The application uses this deployment configuration:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source
COPY ["src/StudentClearanceSystem.Web/StudentClearanceSystem.Web.csproj", "src/StudentClearanceSystem.Web/"]
RUN dotnet restore "src/StudentClearanceSystem.Web/StudentClearanceSystem.Web.csproj"
COPY . .
WORKDIR /source/src/StudentClearanceSystem.Web
RUN dotnet publish "StudentClearanceSystem.Web.csproj" --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet StudentClearanceSystem.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]
```

The railway.toml file handles:
- Health checks on `/` endpoint
- Automatic restart on failures (max 10 retries)
- Proper port configuration for Railway

## Step 5: Deployment Troubleshooting

### Common Errors and Solutions

#### Error: "Missing required Railway PostgreSQL environment variables"
- **Cause**: PostgreSQL database not linked to the service
- **Solution**: 
  1. Click **+ Add** in Railway project
  2. Select **PostgreSQL**
  3. Wait for provisioning
  4. Redeploy your service

#### Error: "Production deployment requires 'InitialAdmin:Password' configuration"
- **Cause**: Admin password not set
- **Solution**: 
  1. Go to service Variables
  2. Add `InitialAdmin__Password` with a secure password value
  3. Redeploy

#### Error: "No database connection configured"
- **Cause**: Neither automatic PostgreSQL nor manual connection string configured
- **Solution**: 
  1. Ensure PostgreSQL is linked (see Step 1, Option A)
  2. OR manually configure `ConnectionStrings__DefaultConnection`
  3. Redeploy

#### Application starts but can't connect to database
- **Cause**: SSL certificate issues or network connectivity
- **Solution**:
  1. Railway PostgreSQL uses SSL by default (SslMode=Require)
  2. Ensure your PostgreSQL instance supports SSL connections
  3. If using external database, verify network access

### Viewing Logs

To diagnose deployment crashes:
1. In Railway dashboard, select your service
2. Click **Deployments** tab
3. Select the failed deployment
4. Click **View logs** to see the error messages

## Step 6: Post-Deployment Verification

1. Open your deployed application URL
2. Log in with:
   - Email: `admin@university.edu` (or what you set in InitialAdmin__Email)
   - Password: The password set in InitialAdmin__Password
3. Verify the application loads successfully

## Step 7: Database Migrations

The application automatically runs EF Core migrations on startup via `context.Database.MigrateAsync()`. No manual migration step is required.

### Initial Data Seeding

On first deployment, the DbInitializer will automatically create:
- **Roles**: Admin, Student
- **Admin Account**: Using InitialAdmin settings
- **Clearance Departments**: Library, Finance, Academic Department, Student Affairs, Examination Office, ICT Department, Hostel

## Summary Checklist

- [ ] PostgreSQL database is linked to the Railway service (or external DB is configured)
- [ ] Environment variables are set:
  - [ ] `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD` (if using Railway PostgreSQL)
  - [ ] OR `ConnectionStrings__DefaultConnection` (if using manual connection string)
  - [ ] `InitialAdmin__Password` is set to a secure value
  - [ ] `InitialAdmin__Email` is set (optional)
- [ ] Service is deployed and healthy
- [ ] Application is accessible at the Railway URL
- [ ] You can log in with the admin account

## Additional Resources

- [Railway PostgreSQL Documentation](https://railway.app/docs/databases/postgresql)
- [Railway Environment Variables](https://railway.app/docs/references/variables)
- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration)

# Student Clearance System

## Deploying to Railway

This ASP.NET Core 9 application uses SQL Server. Railway deploys the app from
the repository's `Dockerfile`; the database must be hosted separately and
reachable from Railway.

1. Create a Railway project and deploy this GitHub repository.
2. In the app service's **Variables**, set:
   - `ConnectionStrings__DefaultConnection` to the SQL Server connection string
     supplied by your database host.
   - `InitialAdmin__Email` to the initial administrator's email address
     (optional; defaults to `admin@university.edu`).
   - `InitialAdmin__Password` to a strong, unique password.
3. Make sure the SQL Server connection string permits remote connections from
   Railway and uses the TLS settings required by your database host.
4. Redeploy after setting the variables. On startup, the app applies its EF Core
   migrations and seeds the initial roles, administrator, and clearance
   departments.

Do not commit production connection strings or passwords. Add them as Railway
service variables. A newly created administrator account is marked to change
its password after signing in.

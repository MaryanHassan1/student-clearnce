# Student Clearance System

## Deploying to Railway

This ASP.NET Core 9 application uses PostgreSQL. Railway deploys the app from
the repository's `Dockerfile`.

1. In the Railway project, add a PostgreSQL database service.
2. In the app service's **Variables**, set `ConnectionStrings__DefaultConnection`
   to a PostgreSQL connection string using Railway's private database variables.
   For a database service named `Postgres`, Railway variable references can be
   used as follows:

   ```text
   Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require;Trust Server Certificate=true
   ```

3. Set `InitialAdmin__Password` to a strong, unique password. Optionally set
   `InitialAdmin__Email`; it defaults to `admin@university.edu`.
4. Deploy the app. On startup, it applies its EF Core migrations and seeds the
   initial roles, administrator, and clearance departments.
5. Generate a public domain for the app service in Railway's **Networking**
   settings.

Do not commit production connection strings or passwords. Add them as Railway
service variables. A newly created administrator account is marked to change
its password after signing in.

The PostgreSQL migrations create a new database schema. They do not migrate
existing data from a SQL Server database.

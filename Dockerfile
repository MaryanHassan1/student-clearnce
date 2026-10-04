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

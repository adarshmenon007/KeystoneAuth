# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json ./
COPY src/Keystone.Domain/Keystone.Domain.csproj src/Keystone.Domain/
COPY src/Keystone.Contracts/Keystone.Contracts.csproj src/Keystone.Contracts/
COPY src/Keystone.Application/Keystone.Application.csproj src/Keystone.Application/
COPY src/Keystone.Infrastructure/Keystone.Infrastructure.csproj src/Keystone.Infrastructure/
COPY src/Keystone.Api/Keystone.Api.csproj src/Keystone.Api/
RUN dotnet restore src/Keystone.Api/Keystone.Api.csproj
COPY src/ src/
RUN dotnet publish src/Keystone.Api/Keystone.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN mkdir /data && chown app /data
COPY --from=build /app/publish .
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Keystone.Api.dll"]
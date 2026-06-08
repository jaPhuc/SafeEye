FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /repo

COPY SafeEye.sln .
COPY src/SafeEye.Domain/SafeEye.Domain.csproj             src/SafeEye.Domain/
COPY src/SafeEye.Application/SafeEye.Application.csproj   src/SafeEye.Application/
COPY src/SafeEye.Infrastructure/SafeEye.Infrastructure.csproj src/SafeEye.Infrastructure/
COPY src/SafeEye.API/SafeEye.API.csproj                   src/SafeEye.API/

RUN dotnet restore

COPY . .

RUN dotnet publish src/SafeEye.API/SafeEye.API.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "SafeEye.API.dll"]
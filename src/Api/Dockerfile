# syntax=docker/dockerfile:1.7
# Build context is the repository root: docker build -f src/Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Domain/InterviewPrep.Domain.csproj src/Domain/
COPY src/Application/InterviewPrep.Application.csproj src/Application/
COPY src/Infrastructure/InterviewPrep.Infrastructure.csproj src/Infrastructure/
COPY src/Api/InterviewPrep.Api.csproj src/Api/
RUN dotnet restore src/Api/InterviewPrep.Api.csproj
COPY src/ src/
RUN dotnet publish src/Api/InterviewPrep.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache krb5-libs
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "InterviewPrep.Api.dll"]

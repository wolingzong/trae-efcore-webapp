# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY efcore-webapp/*.csproj efcore-webapp/
COPY efcore-webapp.Tests/*.csproj efcore-webapp.Tests/
RUN dotnet restore efcore-webapp/EfCoreWebApp.csproj

# Copy source code and build
COPY efcore-webapp/ efcore-webapp/
COPY efcore-webapp.Tests/ efcore-webapp.Tests/
WORKDIR /src/efcore-webapp
RUN dotnet build EfCoreWebApp.csproj -c Release -o /app/build

# Test stage
FROM build AS test
WORKDIR /src
RUN dotnet test efcore-webapp.Tests/efcore-webapp.Tests.csproj --configuration Release --logger trx --results-directory /testresults

# Publish stage
FROM build AS publish
WORKDIR /src/efcore-webapp
RUN dotnet publish EfCoreWebApp.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install dependencies for Puppeteer
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    ca-certificates \
    procps \
    libxss1 \
    && wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list' \
    && apt-get update \
    && apt-get install -y google-chrome-stable fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf libxss1 \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Copy published app
COPY --from=publish --chown=appuser:appuser /app/publish .

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome-stable

EXPOSE 8080

ENTRYPOINT ["dotnet", "EfCoreWebApp.dll"]
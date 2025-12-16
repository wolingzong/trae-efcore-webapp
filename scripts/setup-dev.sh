#!/bin/bash

# Development environment setup script
set -e

echo "🔧 Setting up development environment..."

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

# Check prerequisites
log_info "Checking prerequisites..."

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    log_warn ".NET SDK not found. Please install .NET 9.0 SDK"
    exit 1
fi

# Check Docker
if ! command -v docker &> /dev/null; then
    log_warn "Docker not found. Please install Docker"
    exit 1
fi

# Check Docker Compose
if ! command -v docker-compose &> /dev/null; then
    log_warn "Docker Compose not found. Please install Docker Compose"
    exit 1
fi

log_info "✅ All prerequisites are installed"

# Restore NuGet packages
log_info "Restoring NuGet packages..."
dotnet restore

# Start SQL Server container
log_info "Starting SQL Server container..."
docker-compose up -d sqlserver

# Wait for SQL Server
log_info "Waiting for SQL Server to be ready..."
sleep 30

# Build the application
log_info "Building application..."
dotnet build --configuration Debug

log_info "🎉 Development environment setup completed!"
echo ""
echo "📝 Next steps:"
echo "  1. Run the application: cd efcore-webapp && dotnet run"
echo "  2. Run tests: cd efcore-webapp.Tests && dotnet test"
echo "  3. Access application: http://localhost:5000"
echo "  4. Access products: http://localhost:5000/products"
echo ""
echo "🐳 Docker commands:"
echo "  - Start all services: docker-compose up"
echo "  - Stop all services: docker-compose down"
echo "  - View logs: docker-compose logs"
#!/bin/bash

# Deployment script for EF Core Web App
set -e

echo "🚀 Starting deployment process..."

# Configuration
APP_NAME="efcore-webapp"
DOCKER_IMAGE="$APP_NAME:latest"
CONTAINER_NAME="$APP_NAME-container"
PORT="8080"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    log_error "Docker is not running. Please start Docker and try again."
    exit 1
fi

# Stop existing container if running
if docker ps -q -f name=$CONTAINER_NAME | grep -q .; then
    log_info "Stopping existing container..."
    docker stop $CONTAINER_NAME
    docker rm $CONTAINER_NAME
fi

# Build new image
log_info "Building Docker image..."
docker build -t $DOCKER_IMAGE .

# Run database migration (if needed)
log_info "Starting SQL Server container..."
docker-compose up -d sqlserver

# Wait for SQL Server to be ready
log_info "Waiting for SQL Server to be ready..."
sleep 30

# Run the application
log_info "Starting application container..."
docker run -d \
    --name $CONTAINER_NAME \
    --network traework_app-network \
    -p $PORT:8080 \
    -e ASPNETCORE_ENVIRONMENT=Production \
    -e "ConnectionStrings__DefaultConnection=Server=sqlserver;Database=MyWebAppDb;User ID=sa;Password=YourStrong@Password;TrustServerCertificate=True;" \
    $DOCKER_IMAGE

# Health check
log_info "Performing health check..."
sleep 10

if curl -f http://localhost:$PORT > /dev/null 2>&1; then
    log_info "✅ Deployment successful! Application is running on http://localhost:$PORT"
else
    log_error "❌ Deployment failed! Application is not responding."
    docker logs $CONTAINER_NAME
    exit 1
fi

# Run tests
log_info "Running integration tests..."
docker-compose run --rm test-runner

log_info "🎉 Deployment completed successfully!"
echo "📊 Application URL: http://localhost:$PORT"
echo "📈 Products page: http://localhost:$PORT/products"
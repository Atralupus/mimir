<div align="center">
    <img src="./docs/assets/logo.png" width="144" />
    <h2>Mimir</h2>
    <p>A service that allows you to easily query real-time data from the Nine Chronicles chain via GraphQL.</p>

[![Discord](https://img.shields.io/discord/928926944937013338.svg?color=7289da&logo=discord&logoColor=white)][Discord]

</div>

[Discord]: https://planetarium.dev/discord

> [!TIP]
> If you're new to Nine Chronicles, try to visit our **Developer Portal**!
>
> https://nine-chronicles.dev/

# Mimir

Mimir is a GraphQL API for 9c blockchain data.

## Projects

- **Mimir**: Main GraphQL API
- **Mimir.Worker**: Background service for blockchain data synchronization
- **Mimir.HangfireWorker**: Background service for asynchronous data completion using Hangfire
- **Mimir.HangfireAPI**: Hangfire dashboard API for monitoring jobs
- **Mimir.MongoDB**: MongoDB data access layer
- **Mimir.Initializer**: Data initialization service
- **Lib9c.GraphQL**: GraphQL types and extensions
- **Lib9c.Models**: Data models

## Architecture

### Data Flow
1. **Mimir.Worker**: Continuously polls blockchain data and stores in MongoDB
2. **Mimir API**: Serves GraphQL requests from MongoDB
3. **Mimir.HangfireWorker**: Handles asynchronous data completion when data is missing

### Hangfire Worker System
- **Redis**: Used as message broker for Hangfire jobs
- **Data Completion**: When GraphQL requests find missing Agent/Avatar data, jobs are queued
- **NotFound Cache**: Prevents repeated attempts for non-existent data (7-day cache)
- **Monitoring**: Hangfire dashboard available at `/hangfire` endpoint

## Setup

### Prerequisites
- .NET 8.0
- MongoDB
- Redis

### Running with Docker Compose
```bash
docker-compose up -d
```

### Configuration
Set environment variables for each service:

#### Mimir API
- `MONGODB_CONNECTION_STRING`: MongoDB connection string
- `PLANET_TYPE`: Planet type (e.g., "odin")

#### Mimir.Worker
- `WORKER_CONFIG_FILE`: Configuration file path
- `WORKER_POLLER_TYPE`: Poller type (BlockPoller, TxPoller, DiffPoller)

#### Mimir.HangfireWorker
- `HANGFIRE_REDIS_CONNECTION_STRING`: Redis connection string
- `HANGFIRE_MONGODB_CONNECTION_STRING`: MongoDB connection string
- `HANGFIRE_HEADLESS_ENDPOINTS`: Headless GraphQL endpoints
- `HANGFIRE_JWT_ISSUER`: JWT issuer
- `HANGFIRE_JWT_SECRET_KEY`: JWT secret key

#### Mimir.HangfireAPI
- `HANGFIRE_API_REDIS_CONNECTION_STRING`: Redis connection string

## Development

### Building
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Running Individual Services
```bash
# GraphQL API
dotnet run --project Mimir

# Blockchain Worker
dotnet run --project Mimir.Worker

# Hangfire Worker
dotnet run --project Mimir.HangfireWorker

# Hangfire Dashboard
dotnet run --project Mimir.HangfireAPI
```

## Data Completion Workflow

1. **GraphQL Request**: Client requests Agent/Avatar data
2. **Data Check**: API checks if data exists in MongoDB
3. **Job Queue**: If data is missing, Hangfire job is queued
4. **Blockchain Check**: Worker checks if data exists in blockchain
5. **Data Storage**: If found, data is stored in MongoDB
6. **Cache Management**: If not found, address is cached for 7 days

## Monitoring

- **Hangfire Dashboard**: Access at `http://localhost:5000/hangfire`
- **Job Monitoring**: View job status, retry failed jobs, monitor performance
- **Redis Keys**: 
  - `hangfire:*`: Hangfire job data
  - `notfound:agent:*`: Cached non-existent agent addresses
  - `notfound:avatar:*`: Cached non-existent avatar addresses

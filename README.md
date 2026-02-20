# stream-droid-server

StreamDroid Server is a layered backend service built with .NET 9, implementing gRPC communication secured by JWT authentication.

This branch (network/gRPC-jwt) focuses specifically on networking architecture and authentication flow design rather than production deployment concerns. In its current form, it is not intended for deployment.

## Architecture Overview

The solution structure follows Clean Architecture principles.

```
StreamDroid.sln
├── StreamDroid.Application       # Application layer (composition and configuration root)
├── StreamDroid.Core              # Core models and interfaces
├── StreamDroid.Domain            # Business logic and services
├── StreamDroid.Infrastructure    # Persistence concerns
├── StreamDroid.Shared            # Cross-shared utilities
├── *.Tests                       # Unit, Component and Integration tests
```

## Authentication Design (JWT + gRPC)

This branch demonstrates securing gRPC endpoints using JWT.

### Authentication Flow

1. Client authenticates externally and obtains a JWT.
2. Client sends token via gRPC metadata (Authorization: Bearer <token>).
3. Server validates:
    * Signature
    * Issuer
    * Audience
    * Expiration
4. Claims are attached to the request context.
5. Application services authorize based on claims.

### Why JWT?

* Stateless authentication
* Compatible with mobile, desktop and web clients

### gRPC Design

* HTTP/2 transport
* Protobuf contracts define service boundaries
* Interceptors handle cross-cutting concerns (token refresh, correlation, exception handling and logging)

gRPC was chosen over REST to:

* Improve performance
* Enforce strict API contracts across devices
* Support strongly typed client generation

## Testing Strategy

Each major layer has an associated test project.

Testing focuses on:

* Business logic validation
* Service behavior correctness
* Domain rule enforcement

CI pipelines automatically:

* Restore dependencies
* Build the solution
* Run all tests

This ensures structural integrity across changes.

## Running the Project

Requirements

* .NET 9 SDK
* Twitch App Credentials or Twitch CLI Event Sub instance

### Build
```
dotnet build
```

### Run Tests
```
dotnet test
```

### Run Server
```
dotnet run --project StreamDroid.Application
```

## Engineering Tradeoffs

This project intentionally prioritizes:

* Clear architectural boundaries
* Separation of concerns
* Testability
* Authentication correctness

## Future Enhancements

If evolved into production:

* Structured logging (Serilog/OpenTelemetry)
* Health checks
* Containerization
* Key rotation strategy
* Role-based authorization policies

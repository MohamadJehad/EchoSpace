# EchoSpace - Clean Architecture

A .NET 8.0 Web API project structured using Clean Architecture principles with proper separation of concerns.

## Project Structure

```
EchoSpace.CleanArchitecture/
├── src/
│   ├── EchoSpace.Core/           # Business logic and domain entities
│   ├── EchoSpace.Infrastructure/ # Data access and external services
│   ├── EchoSpace.UI/            # Web API controllers and presentation
│   └── EchoSpace.Tools/         # Utility services and cross-cutting concerns
├── tests/
│   └── EchoSpace.Tests/         # Unit and integration tests
└── .github/workflows/           # CI/CD pipeline
```

## Architecture Layers

### 🏗️ Core Project (`EchoSpace.Core`)
**Purpose**: Contains business logic, entities, and domain rules
- **Entities**: Domain models (User, etc.)
- **Interfaces**: Business logic contracts (IUserService, IUserRepository)
- **Services**: Business logic implementations (UserService)
- **DTOs**: Data transfer objects for API contracts

### 🗄️ Infrastructure Project (`EchoSpace.Infrastructure`)
**Purpose**: Contains data access, external services, and infrastructure concerns
- **Data**: Entity Framework DbContext
- **Repositories**: Data access implementations
- **External Services**: Third-party integrations

### 🌐 UI Project (`EchoSpace.UI`)
**Purpose**: Contains presentation layer (Web API, Controllers, etc.)
- **Controllers**: API endpoints
- **Configuration**: Swagger, authentication, middleware
- **Request/Response Models**: API-specific DTOs

### 🔧 Tools Project (`EchoSpace.Tools`)
**Purpose**: Contains utility services and cross-cutting concerns
- **EmailSender**: Email service implementation
- **Logging**: Logging utilities
- **Caching**: Caching services
- **Common Helpers**: Shared utility classes

### 🧪 Tests Project (`EchoSpace.Tests`)
**Purpose**: Contains all test projects
- **Unit Tests**: Business logic testing
- **Integration Tests**: Infrastructure testing
- **API Tests**: Controller testing

## Features

- ✅ **Clean Architecture**: Proper separation of concerns
- ✅ **Entity Framework Core**: Data access with SQL Server
- ✅ **Swagger/OpenAPI**: API documentation
- ✅ **Unit Tests**: Comprehensive test coverage with xUnit and Moq
- ✅ **CI/CD**: GitHub Actions workflow for PR builds
- ✅ **Error Handling**: Try-catch blocks with proper logging
- ✅ **DTOs**: Separate request/response models
- ✅ **Dependency Injection**: Proper DI container setup

## API Endpoints

- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

## Getting Started

1. **Clone the repository**
2. **Restore dependencies**:
   ```bash
   dotnet restore EchoSpace.CleanArchitecture.sln
   ```
3. **Build the solution**:
   ```bash
   dotnet build EchoSpace.CleanArchitecture.sln
   ```
4. **Run tests**:
   ```bash
   dotnet test EchoSpace.CleanArchitecture.sln
   ```
5. **Run the application**:
   ```bash
   dotnet run --project src/EchoSpace.UI/EchoSpace.UI.csproj
   ```

## Database Setup

1. **Create migration**:
   ```bash
   dotnet ef migrations add InitialCreate --project src/EchoSpace.Infrastructure --startup-project src/EchoSpace.UI
   ```
2. **Update database**:
   ```bash
   dotnet ef database update --project src/EchoSpace.Infrastructure --startup-project src/EchoSpace.UI
   ```

## Benefits of This Architecture

- **Separation of Concerns**: Each project has a single responsibility
- **Testability**: Easy to unit test business logic in isolation
- **Maintainability**: Changes in one layer don't affect others
- **Scalability**: Easy to add new features or replace implementations
- **Dependency Management**: Clear dependency direction (Core ← Infrastructure ← UI)

## Technology Stack

- **.NET 8.0**
- **Entity Framework Core 9.0**
- **SQL Server**
- **Swagger/OpenAPI**
- **xUnit** (Testing)
- **Moq** (Mocking)
- **GitHub Actions** (CI/CD)

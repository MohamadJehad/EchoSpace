# EchoSpace - Clean Architecture

A full-stack social media application built with ASP.NET Core Web API (Backend) and Angular (Frontend), structured using Clean Architecture principles with proper separation of concerns.

## Project Structure

```
EchoSpace.CleanArchitecture/
├── src/
│   ├── EchoSpace.Core/           # Business logic and domain entities
│   ├── EchoSpace.Infrastructure/ # Data access and external services
│   ├── EchoSpace.UI/            # Web API controllers and presentation
│   ├── EchoSpace.Tools/         # Utility services and cross-cutting concerns
│   └── EchoSpace.Web.Client/    # Angular frontend application
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

### 💻 Frontend Project (`EchoSpace.Web.Client`)
**Purpose**: Angular-based user interface
- **Components**: Login, Register, User List components
- **Services**: API integration and data management
- **Routing**: Client-side navigation
- **Styling**: Tailwind CSS for modern UI

## Features

- ✅ **Clean Architecture**: Proper separation of concerns
- ✅ **Entity Framework Core**: Data access with SQL Server
- ✅ **Swagger/OpenAPI**: API documentation
- ✅ **Unit Tests**: Comprehensive test coverage with xUnit and Moq
- ✅ **CI/CD**: GitHub Actions workflow for PR builds (Backend + Frontend)
- ✅ **Error Handling**: Try-catch blocks with proper logging
- ✅ **DTOs**: Separate request/response models
- ✅ **Dependency Injection**: Proper DI container setup
- ✅ **Angular Frontend**: Modern SPA with Tailwind CSS
- ✅ **Responsive Design**: Mobile-first approach
- ✅ **Authentication Pages**: Login and Register with social OAuth ready

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

## 🚀 Running the Application

### **Backend (ASP.NET Core API)**

#### Quick Start
```bash
cd src/EchoSpace.UI
dotnet restore
dotnet run
```

#### Access the API
- **HTTPS**: `https://localhost:7131`
- **HTTP**: `http://localhost:5005`
- **Swagger UI**: `https://localhost:7131/swagger/index.html`

### **Frontend (Angular)**

#### Prerequisites
- Node.js v18 or higher
- npm or yarn

#### Installation
```bash
cd src/EchoSpace.Web.Client
npm install
```

#### Run Development Server
```bash
npm start
# or
ng s
```

#### Access the Frontend
- **Development**: `http://localhost:4200`
- **Available Routes**:
  - `/` - User list page
  - `/login` - Login page
  - `/register` - Registration page

### **Running Both Services**

**Terminal 1 - Backend:**
```bash
cd src/EchoSpace.UI
dotnet run
```

**Terminal 2 - Frontend:**
```bash
cd src/EchoSpace.Web.Client
npm start
```

Then open `http://localhost:4200` in your browser!

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

### Backend
- **.NET 9.0**
- **Entity Framework Core 9.0**
- **SQL Server**
- **Swagger/OpenAPI**
- **ASP.NET Core Web API**

### Frontend
- **Angular 19**
- **TypeScript**
- **Tailwind CSS**
- **RxJS**
- **Standalone Components**

### Testing & DevOps
- **xUnit** (Testing)
- **Moq** (Mocking)
- **GitHub Actions** (CI/CD)
- **Karma/Jasmine** (Angular Testing)

## 📚 Additional Resources

### API Documentation
- **Swagger UI**: `https://localhost:7131/swagger/index.html`
- **Postman Collection**: Import endpoints from Swagger UI

### Frontend Documentation
- **Environment Config**: See `src/EchoSpace.Web.Client/ENV_GUIDE.md`
- **Auth Pages**: See `src/EchoSpace.Web.Client/AUTH_PAGES.md`
- **Frontend README**: See `src/EchoSpace.Web.Client/README.md`

## 🎯 Application URLs

| Service | URL | Description |
|---------|-----|-------------|
| Frontend | `http://localhost:4200` | Angular SPA |
| Backend API | `https://localhost:7131` | ASP.NET Core API |
| Swagger | `https://localhost:7131/swagger` | API Documentation |
| Login Page | `http://localhost:4200/login` | User Login |
| Register Page | `http://localhost:4200/register` | User Registration |

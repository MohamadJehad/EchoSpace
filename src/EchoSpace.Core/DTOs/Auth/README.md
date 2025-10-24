# Authentication DTOs

This folder contains all Data Transfer Objects (DTOs) related to authentication.

## Files

- **RegisterRequest.cs** - DTO for user registration
- **LoginRequest.cs** - DTO for user login
- **RefreshTokenRequest.cs** - DTO for token refresh
- **LogoutRequest.cs** - DTO for user logout
- **AuthResponse.cs** - DTO for authentication responses (tokens + user info)
- **UserDto.cs** - DTO for user information

## Namespace

All DTOs are in the `EchoSpace.Core.DTOs.Auth` namespace.

## Usage

```csharp
using EchoSpace.Core.DTOs.Auth;

// Example usage
var loginRequest = new LoginRequest
{
    Email = "user@example.com",
    Password = "securepassword"
};
```


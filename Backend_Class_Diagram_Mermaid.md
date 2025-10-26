# EchoSpace Backend - Complete Class Diagram

```mermaid
classDiagram
    %% ===========================================
    %% ENUMS
    %% ===========================================
    class UserRole {
        <<enumeration>>
        User
        Admin
        Moderator
    }

    %% ===========================================
    %% ENTITIES (Domain Models)
    %% ===========================================
    class User {
        +Guid Id
        +string Name
        +string Email
        +string UserName
        +string? PasswordHash
        +UserRole Role
        +bool EmailConfirmed
        +bool LockoutEnabled
        +DateTimeOffset? LockoutEnd
        +int AccessFailedCount
        +DateTime? LastLoginAt
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +string? TotpSecretKey
        +bool EmailVerified
        +string? EmailVerificationCode
        +DateTime? EmailVerificationCodeExpiry
        +int EmailVerificationAttempts
    }

    class AuthProvider {
        +Guid AuthId
        +Guid UserId
        +string Provider
        +string ProviderUid
        +string? AccessToken
        +DateTime CreatedAt
    }

    class UserSession {
        +Guid SessionId
        +Guid UserId
        +string RefreshToken
        +string? DeviceInfo
        +string? IpAddress
        +DateTime ExpiresAt
        +DateTime CreatedAt
    }

    class PasswordResetToken {
        +Guid Id
        +Guid UserId
        +string Token
        +DateTime ExpiresAt
        +DateTime CreatedAt
        +bool IsUsed
        +DateTime? UsedAt
    }

    class Post {
        +Guid PostId
        +Guid UserId
        +string Content
        +string? ImageUrl
        +DateTime CreatedAt
        +DateTime? UpdatedAt
    }

    class Comment {
        +Guid CommentId
        +Guid PostId
        +Guid UserId
        +string Content
        +DateTime CreatedAt
    }

    class Like {
        +Guid LikeId
        +Guid PostId
        +Guid UserId
        +DateTime CreatedAt
    }

    class Follow {
        +Guid FollowerId
        +Guid FollowedId
        +DateTime CreatedAt
    }

    %% ===========================================
    %% DTOs (Data Transfer Objects)
    %% ===========================================
    class UserDto {
        +Guid Id
        +string Name
        +string Email
        +string UserName
        +string Role
    }

    class AuthResponse {
        +string? AccessToken
        +string? RefreshToken
        +int ExpiresIn
        +UserDto? User
        +bool RequiresTotp
        +bool RequiresEmailVerification
    }

    class PostDto {
        +Guid PostId
        +Guid UserId
        +string Content
        +string? ImageUrl
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +int LikesCount
        +int CommentsCount
        +bool IsLikedByCurrentUser
        +string AuthorName
        +string AuthorEmail
        +string AuthorUserName
    }

    class CommentDto {
        +Guid CommentId
        +Guid PostId
        +Guid UserId
        +string Content
        +DateTime CreatedAt
        +string UserName
        +string UserEmail
    }

    class RegisterRequest {
        +string Email
        +string Name
        +string Password
    }

    class LoginRequest {
        +string Email
        +string Password
    }

    class RefreshTokenRequest {
        +string RefreshToken
    }

    class LogoutRequest {
        +string RefreshToken
    }

    class ForgotPasswordRequest {
        +string Email
    }

    class ForgotPasswordResponse {
        +bool Success
        +string Message
    }

    class ValidateResetTokenRequest {
        +string Token
    }

    class ValidateResetTokenResponse {
        +bool IsValid
        +string Message
    }

    class ResetPasswordRequest {
        +string Token
        +string NewPassword
    }

    class TotpSetupRequest {
        +string Email
    }

    class TotpSetupResponse {
        +string QrCodeUrl
        +string SecretKey
        +string ManualEntryKey
    }

    class TotpVerificationRequest {
        +string Email
        +string Code
    }

    class EmailVerificationRequest {
        +string Email
        +string Code
    }

    class CreatePostRequest {
        +Guid UserId
        +string Content
        +string? ImageUrl
    }

    class UpdatePostRequest {
        +string Content
        +string? ImageUrl
    }

    class CreateCommentRequest {
        +Guid PostId
        +Guid UserId
        +string Content
    }

    class UpdateCommentRequest {
        +string Content
    }

    class CreateUserRequest {
        +string Name
        +string Email
    }

    class UpdateUserRequest {
        +string Name
        +string Email
    }

    class SearchResultDto {
        +Guid Id
        +string Name
        +string UserName
        +string Email
        +double MatchScore
    }

    class CompleteRegistrationRequest {
        +string Email
        +string Code
    }

    class TestEmailRequest {
        +string Email
    }

    %% ===========================================
    %% INTERFACES
    %% ===========================================
    class IAuthService {
        <<interface>>
        +Task~AuthResponse~ RegisterAsync(RegisterRequest)
        +Task~AuthResponse~ LoginAsync(LoginRequest)
        +Task~AuthResponse~ VerifyTotpAndLoginAsync(string, string)
        +Task~AuthResponse~ RefreshTokenAsync(string)
        +Task LogoutAsync(string)
        +Task~AuthResponse~ GoogleLoginAsync(string, string, string)
        +Task~ForgotPasswordResponse~ ForgotPasswordAsync(ForgotPasswordRequest)
        +Task~ValidateResetTokenResponse~ ValidateResetTokenAsync(ValidateResetTokenRequest)
        +Task~bool~ ResetPasswordAsync(ResetPasswordRequest)
        +Task~AuthResponse~ CompleteRegistrationWithEmailVerificationAsync(string, string)
    }

    class IUserService {
        <<interface>>
        +Task~IEnumerable~User~~ GetAllAsync()
        +Task~User?~ GetByIdAsync(Guid)
        +Task~User~ CreateAsync(CreateUserRequest)
        +Task~User?~ UpdateAsync(Guid, UpdateUserRequest)
        +Task~bool~ DeleteAsync(Guid)
    }

    class IPostService {
        <<interface>>
        +Task~IEnumerable~PostDto~~ GetAllAsync()
        +Task~PostDto?~ GetByIdAsync(Guid)
        +Task~IEnumerable~PostDto~~ GetByUserIdAsync(Guid)
        +Task~IEnumerable~PostDto~~ GetRecentAsync(int)
        +Task~PostDto~ CreateAsync(CreatePostRequest)
        +Task~PostDto?~ UpdateAsync(Guid, UpdatePostRequest)
        +Task~bool~ DeleteAsync(Guid)
        +Task~bool~ ExistsAsync(Guid)
        +Task~bool~ IsOwnerAsync(Guid, Guid)
    }

    class ICommentService {
        <<interface>>
        +Task~IEnumerable~CommentDto~~ GetAllAsync()
        +Task~CommentDto?~ GetByIdAsync(Guid)
        +Task~IEnumerable~CommentDto~~ GetByPostIdAsync(Guid)
        +Task~IEnumerable~CommentDto~~ GetByUserIdAsync(Guid)
        +Task~CommentDto~ CreateAsync(CreateCommentRequest)
        +Task~CommentDto?~ UpdateAsync(Guid, UpdateCommentRequest)
        +Task~bool~ DeleteAsync(Guid)
        +Task~bool~ ExistsAsync(Guid)
        +Task~bool~ IsOwnerAsync(Guid, Guid)
        +Task~int~ GetCountByPostIdAsync(Guid)
    }

    class ITotpService {
        <<interface>>
        +Task~TotpSetupResponse~ SetupTotpAsync(string)
        +Task~bool~ VerifyTotpAsync(string, string)
        +Task~bool~ SendEmailVerificationCodeAsync(string)
        +Task~bool~ VerifyEmailCodeAsync(string, string)
        +Task~string~ GenerateQrCodeAsync(string, string)
    }

    class ISearchService {
        <<interface>>
        +Task~IEnumerable~SearchResultDto~~ SearchUsersAsync(string, int)
    }

    class IUserRepository {
        <<interface>>
        +Task~IEnumerable~User~~ GetAllAsync()
        +Task~User?~ GetByIdAsync(Guid)
        +Task~User~ AddAsync(User)
        +Task~User?~ UpdateAsync(User)
        +Task~bool~ DeleteAsync(Guid)
    }

    class IPostRepository {
        <<interface>>
        +Task~IEnumerable~Post~~ GetAllAsync()
        +Task~Post?~ GetByIdAsync(Guid)
        +Task~IEnumerable~Post~~ GetByUserIdAsync(Guid)
        +Task~IEnumerable~Post~~ GetRecentAsync(int)
        +Task~Post~ AddAsync(Post)
        +Task~Post?~ UpdateAsync(Post)
        +Task~bool~ DeleteAsync(Guid)
        +Task~bool~ ExistsAsync(Guid)
    }

    class ICommentRepository {
        <<interface>>
        +Task~IEnumerable~Comment~~ GetAllAsync()
        +Task~Comment?~ GetByIdAsync(Guid)
        +Task~IEnumerable~Comment~~ GetByPostIdAsync(Guid)
        +Task~IEnumerable~Comment~~ GetByUserIdAsync(Guid)
        +Task~Comment~ AddAsync(Comment)
        +Task~Comment?~ UpdateAsync(Comment)
        +Task~bool~ DeleteAsync(Guid)
        +Task~bool~ ExistsAsync(Guid)
        +Task~int~ GetCountByPostIdAsync(Guid)
    }

    class IEmailSender {
        <<interface>>
        +Task SendEmailAsync(string, string, string)
        +Task SendEmailAsync(string, string, string, string)
        +Task SendBulkEmailAsync(IEnumerable~string~, string, string)
    }

    %% ===========================================
    %% SERVICES (Core Layer)
    %% ===========================================
    class UserService {
        -IUserRepository _userRepository
        +UserService(IUserRepository)
        +Task~IEnumerable~User~~ GetAllAsync()
        +Task~User?~ GetByIdAsync(Guid)
        +Task~User~ CreateAsync(CreateUserRequest)
        +Task~User?~ UpdateAsync(Guid, UpdateUserRequest)
        +Task~bool~ DeleteAsync(Guid)
    }

    class PostService {
        -IPostRepository _postRepository
        +PostService(IPostRepository)
        +Task~IEnumerable~PostDto~~ GetAllAsync()
        +Task~PostDto?~ GetByIdAsync(Guid)
        +Task~IEnumerable~PostDto~~ GetByUserIdAsync(Guid)
        +Task~IEnumerable~PostDto~~ GetRecentAsync(int)
        +Task~PostDto~ CreateAsync(CreatePostRequest)
        +Task~PostDto?~ UpdateAsync(Guid, UpdatePostRequest)
        +Task~bool~ DeleteAsync(Guid)
        +Task~bool~ ExistsAsync(Guid)
        +Task~bool~ IsOwnerAsync(Guid, Guid)
        -PostDto MapToDto(Post)
    }

    class CommentService {
        -ICommentRepository _commentRepository
        +CommentService(ICommentRepository)
        +Task~IEnumerable~CommentDto~~ GetAllAsync()
        +Task~CommentDto?~ GetByIdAsync(Guid)
        +Task~IEnumerable~CommentDto~~ GetByPostIdAsync(Guid)
        +Task~IEnumerable~CommentDto~~ GetByUserIdAsync(Guid)
        +Task~CommentDto~ CreateAsync(CreateCommentRequest)
        +Task~CommentDto?~ UpdateAsync(Guid, UpdateCommentRequest)
        +Task~bool~ DeleteAsync(Guid)
        +Task~bool~ ExistsAsync(Guid)
        +Task~bool~ IsOwnerAsync(Guid, Guid)
        +Task~int~ GetCountByPostIdAsync(Guid)
        -CommentDto MapToDto(Comment)
    }

    %% ===========================================
    %% SERVICES (Infrastructure Layer)
    %% ===========================================
    class AuthService {
        -EchoSpaceDbContext _context
        -IConfiguration _configuration
        -ILogger~AuthService~ _logger
        -IEmailSender _emailSender
        -ITotpService _totpService
        +AuthService(EchoSpaceDbContext, IConfiguration, ILogger, IEmailSender, ITotpService)
        +Task~AuthResponse~ RegisterAsync(RegisterRequest)
        +Task~AuthResponse~ LoginAsync(LoginRequest)
        +Task~AuthResponse~ VerifyTotpAndLoginAsync(string, string)
        +Task~AuthResponse~ RefreshTokenAsync(string)
        +Task LogoutAsync(string)
        +Task~AuthResponse~ GoogleLoginAsync(string, string, string)
        +Task~ForgotPasswordResponse~ ForgotPasswordAsync(ForgotPasswordRequest)
        +Task~ValidateResetTokenResponse~ ValidateResetTokenAsync(ValidateResetTokenRequest)
        +Task~bool~ ResetPasswordAsync(ResetPasswordRequest)
        +Task~AuthResponse~ CompleteRegistrationWithEmailVerificationAsync(string, string)
        -Task~AuthResponse~ GenerateTokensAsync(User)
        -string GenerateAccessToken(User)
        -string GenerateRefreshToken()
        -string HashPassword(string)
        -bool VerifyPassword(string, string?)
        -string GenerateSecureToken()
    }

    class TotpService {
        -EchoSpaceDbContext _context
        -IConfiguration _configuration
        -ILogger~TotpService~ _logger
        -IEmailSender _emailSender
        +TotpService(EchoSpaceDbContext, IConfiguration, ILogger, IEmailSender)
        +Task~TotpSetupResponse~ SetupTotpAsync(string)
        +Task~bool~ VerifyTotpAsync(string, string)
        +Task~bool~ SendEmailVerificationCodeAsync(string)
        +Task~bool~ VerifyEmailCodeAsync(string, string)
        +Task~string~ GenerateQrCodeAsync(string, string)
        -byte[] GenerateSecretKey()
        -string ToBase32(byte[])
        -byte[] FromBase32(string)
        -long GetCurrentTimeStep()
        -string GenerateTotpCode(byte[], long)
    }

    class SearchService {
        -EchoSpaceDbContext _context
        +SearchService(EchoSpaceDbContext)
        +Task~IEnumerable~SearchResultDto~~ SearchUsersAsync(string, int)
        -double CalculateSimilarity(string, string)
        -int LevenshteinDistance(string, string)
    }

    class EmailSender {
        -ILogger~EmailSender~ _logger
        -EmailSettings _emailSettings
        +EmailSender(ILogger, IOptions~EmailSettings~)
        +Task SendEmailAsync(string, string, string)
        +Task SendEmailAsync(string, string, string, string)
        +Task SendBulkEmailAsync(IEnumerable~string~, string, string)
    }

    %% ===========================================
    %% REPOSITORIES (Infrastructure Layer)
    %% ===========================================
    class UserRepository {
        -EchoSpaceDbContext _dbContext
        +UserRepository(EchoSpaceDbContext)
        +Task~IEnumerable~User~~ GetAllAsync()
        +Task~User?~ GetByIdAsync(Guid)
        +Task~User~ AddAsync(User)
        +Task~User?~ UpdateAsync(User)
        +Task~bool~ DeleteAsync(Guid)
    }

    class PostRepository {
        -EchoSpaceDbContext _dbContext
        +PostRepository(EchoSpaceDbContext)
        +Task~IEnumerable~Post~~ GetAllAsync()
        +Task~Post?~ GetByIdAsync(Guid)
        +Task~IEnumerable~Post~~ GetByUserIdAsync(Guid)
        +Task~IEnumerable~Post~~ GetRecentAsync(int)
        +Task~Post~ AddAsync(Post)
        +Task~Post?~ UpdateAsync(Post)
        +Task~bool~ DeleteAsync(Guid)
        +Task~bool~ ExistsAsync(Guid)
    }

    class CommentRepository {
        -EchoSpaceDbContext _dbContext
        +CommentRepository(EchoSpaceDbContext)
        +Task~IEnumerable~Comment~~ GetAllAsync()
        +Task~Comment?~ GetByIdAsync(Guid)
        +Task~IEnumerable~Comment~~ GetByPostIdAsync(Guid)
        +Task~IEnumerable~Comment~~ GetByUserIdAsync(Guid)
        +Task~Comment~ AddAsync(Comment)
        +Task~Comment?~ UpdateAsync(Comment)
        +Task~bool~ DeleteAsync(Guid)
        +Task~bool~ ExistsAsync(Guid)
        +Task~int~ GetCountByPostIdAsync(Guid)
    }

    %% ===========================================
    %% DBCONTEXT
    %% ===========================================
    class EchoSpaceDbContext {
        +DbSet~User~ Users
        +DbSet~UserSession~ UserSessions
        +DbSet~AuthProvider~ AuthProviders
        +DbSet~PasswordResetToken~ PasswordResetTokens
        +DbSet~Post~ Posts
        +DbSet~Comment~ Comments
        +DbSet~Like~ Likes
        +DbSet~Follow~ Follows
        +EchoSpaceDbContext(DbContextOptions~EchoSpaceDbContext~)
        +OnModelCreating(ModelBuilder)
    }

    %% ===========================================
    %% CONTROLLERS (UI Layer)
    %% ===========================================
    class AuthController {
        -IAuthService _authService
        -ITotpService _totpService
        -ILogger~AuthController~ _logger
        -IHttpClientFactory _httpClientFactory
        -IEmailSender _emailSender
        -EchoSpaceDbContext _context
        +AuthController(IAuthService, ITotpService, ILogger, IHttpClientFactory, IEmailSender, EchoSpaceDbContext)
        +Task~IActionResult~ Register(RegisterRequest)
        +Task~IActionResult~ Login(LoginRequest)
        +Task~IActionResult~ RefreshToken(RefreshTokenRequest)
        +Task~IActionResult~ Logout(LogoutRequest)
        +IActionResult GoogleLogin()
        +Task~IActionResult~ GoogleCallback(string, string)
        +Task~IActionResult~ TestEmail(TestEmailRequest)
        +Task~IActionResult~ ForgotPassword(ForgotPasswordRequest)
        +Task~IActionResult~ ValidateResetToken(ValidateResetTokenRequest)
        +Task~IActionResult~ ResetPassword(ResetPasswordRequest)
        +Task~IActionResult~ SetupTotp(TotpSetupRequest)
        +Task~IActionResult~ VerifyTotp(TotpVerificationRequest)
        +Task~IActionResult~ SendEmailVerification(TotpSetupRequest)
        +Task~IActionResult~ VerifyEmail(EmailVerificationRequest)
        +Task~IActionResult~ SetupTotpForExistingUser(TotpSetupRequest)
        +Task~IActionResult~ CompleteRegistration(CompleteRegistrationRequest)
    }

    class PostsController {
        -ILogger~PostsController~ _logger
        -IPostService _postService
        +PostsController(ILogger, IPostService)
        +Task~ActionResult~IEnumerable~PostDto~~ GetPosts(CancellationToken)
        +Task~ActionResult~PostDto~~ GetPost(Guid, CancellationToken)
        +Task~ActionResult~IEnumerable~PostDto~~ GetPostsByUser(Guid, CancellationToken)
        +Task~ActionResult~IEnumerable~PostDto~~ GetRecentPosts(int, CancellationToken)
        +Task~ActionResult~PostDto~~ CreatePost(CreatePostRequest, CancellationToken)
        +Task~ActionResult~PostDto~~ UpdatePost(Guid, UpdatePostRequest, Guid, CancellationToken)
        +Task~ActionResult~ DeletePost(Guid, Guid, CancellationToken)
        +Task~ActionResult~ PostExists(Guid, CancellationToken)
    }

    class CommentsController {
        -ILogger~CommentsController~ _logger
        -ICommentService _commentService
        +CommentsController(ILogger, ICommentService)
        +GetComments()
        +GetComment(Guid)
        +GetCommentsByPost(Guid)
        +GetCommentsByUser(Guid)
        +CreateComment(CreateCommentRequest)
        +UpdateComment(Guid, UpdateCommentRequest, Guid)
        +DeleteComment(Guid, Guid)
    }

    class UsersController {
        -ILogger~UsersController~ _logger
        -IUserService _userService
        +UsersController(ILogger, IUserService)
        +GetUsers()
        +GetUser(Guid)
        +CreateUser(CreateUserRequest)
        +UpdateUser(Guid, UpdateUserRequest)
        +DeleteUser(Guid)
    }

    class SearchController {
        -ILogger~SearchController~ _logger
        -ISearchService _searchService
        +SearchController(ILogger, ISearchService)
        +SearchUsers(string)
    }

    class SuggestedUsersController {
        -ILogger~SuggestedUsersController~ _logger
        -IUserRepository _userRepository
        +SuggestedUsersController(ILogger, IUserRepository)
        +GetSuggestedUsers(Guid)
    }

    %% ===========================================
    %% RELATIONSHIPS - Entities
    %% ===========================================
    User "1" --> "*" UserSession : has sessions
    User "1" --> "*" AuthProvider : has auth providers
    User "1" --> "*" PasswordResetToken : has reset tokens
    User "1" --> "*" Post : creates
    User "1" --> "*" Comment : writes
    User "1" --> "*" Like : creates
    User "1" --> "*" Follow : followers
    User "1" --> "*" Follow : following
    User "1" --> "1" UserRole : has role

    Post "1" --> "*" Comment : has comments
    Post "1" --> "*" Like : has likes
    Post "1" --> "1" User : belongs to

    Comment "1" --> "1" Post : belongs to
    Comment "1" --> "1" User : written by

    Like "1" --> "1" Post : likes
    Like "1" --> "1" User : liked by

    Follow "1" --> "1" User : follower
    Follow "1" --> "1" User : followed

    PasswordResetToken "1" --> "1" User : belongs to

    AuthProvider "1" --> "1" User : belongs to

    %% ===========================================
    %% RELATIONSHIPS - Services
    %% ===========================================
    UserService ..|> IUserService : implements
    UserService --> IUserRepository : uses

    PostService ..|> IPostService : implements
    PostService --> IPostRepository : uses
    IPostService ..> PostDto : returns
    IPostService ..> CreatePostRequest : accepts
    IPostService ..> UpdatePostRequest : accepts

    CommentService ..|> ICommentService : implements
    CommentService --> ICommentRepository : uses
    ICommentService ..> CommentDto : returns
    ICommentService ..> CreateCommentRequest : accepts
    ICommentService ..> UpdateCommentRequest : accepts

    AuthService ..|> IAuthService : implements
    AuthService --> EchoSpaceDbContext : uses
    AuthService --> IEmailSender : uses
    AuthService --> ITotpService : uses
    IAuthService ..> AuthResponse : returns
    IAuthService ..> ForgotPasswordResponse : returns
    IAuthService ..> ValidateResetTokenResponse : returns

    TotpService ..|> ITotpService : implements
    TotpService --> EchoSpaceDbContext : uses
    TotpService --> IEmailSender : uses
    ITotpService ..> TotpSetupResponse : returns

    SearchService ..|> ISearchService : implements
    SearchService --> EchoSpaceDbContext : uses
    ISearchService ..> SearchResultDto : returns

    EmailSender ..|> IEmailSender : implements

    %% ===========================================
    %% RELATIONSHIPS - Repositories
    %% ===========================================
    UserRepository ..|> IUserRepository : implements
    UserRepository --> EchoSpaceDbContext : uses

    PostRepository ..|> IPostRepository : implements
    PostRepository --> EchoSpaceDbContext : uses

    CommentRepository ..|> ICommentRepository : implements
    CommentRepository --> EchoSpaceDbContext : uses

    %% ===========================================
    %% RELATIONSHIPS - Controllers
    %% ===========================================
    AuthController --> IAuthService : uses
    AuthController --> ITotpService : uses
    AuthController --> IEmailSender : uses
    AuthController --> EchoSpaceDbContext : uses

    PostsController --> IPostService : uses

    CommentsController --> ICommentService : uses

    UsersController --> IUserService : uses

    SearchController --> ISearchService : uses

    SuggestedUsersController --> IUserRepository : uses

    %% ===========================================
    %% RELATIONSHIPS - DbContext
    %% ===========================================
    EchoSpaceDbContext --> User : manages
    EchoSpaceDbContext --> UserSession : manages
    EchoSpaceDbContext --> AuthProvider : manages
    EchoSpaceDbContext --> PasswordResetToken : manages
    EchoSpaceDbContext --> Post : manages
    EchoSpaceDbContext --> Comment : manages
    EchoSpaceDbContext --> Like : manages
    EchoSpaceDbContext --> Follow : manages

    %% ===========================================
    %% RELATIONSHIPS - DTOs to Services
    %% ===========================================
    AuthService ..> AuthResponse : returns
    AuthService ..> ForgotPasswordResponse : returns
    AuthService ..> ValidateResetTokenResponse : returns
    AuthService ..> UserDto : creates
    AuthResponse *-- UserDto : contains
    AuthService ..> RegisterRequest : accepts
    AuthService ..> LoginRequest : accepts
    AuthService ..> ForgotPasswordRequest : accepts
    AuthService ..> ValidateResetTokenRequest : accepts
    AuthService ..> ResetPasswordRequest : accepts
    
    PostService ..> PostDto : returns
    PostService ..> Post : converts from
    PostService ..> CreatePostRequest : accepts
    PostService ..> UpdatePostRequest : accepts
    PostDto ..|> Post : maps from
    
    CommentService ..> CommentDto : returns
    CommentService ..> Comment : converts from
    CommentService ..> CreateCommentRequest : accepts
    CommentService ..> UpdateCommentRequest : accepts
    CommentDto ..|> Comment : maps from
    
    TotpService ..> TotpSetupResponse : returns
    TotpService ..> TotpSetupRequest : accepts
    
    SearchService ..> SearchResultDto : returns
    SearchService ..> User : queries
    
    UserService ..> User : returns
    UserService ..> CreateUserRequest : accepts
    UserService ..> UpdateUserRequest : accepts

    %% ===========================================
    %% RELATIONSHIPS - Entity to DTO Mappings
    %% ===========================================
    User ||--o{ UserDto : maps to
    Post ||--o{ PostDto : maps to
    Comment ||--o{ CommentDto : maps to
    User ||--o{ SearchResultDto : converts to

    %% ===========================================
    %% RELATIONSHIPS - DTOs to Controllers
    %% ===========================================
    AuthController ..> RegisterRequest : receives
    AuthController ..> LoginRequest : receives
    AuthController ..> RefreshTokenRequest : receives
    AuthController ..> LogoutRequest : receives
    AuthController ..> ForgotPasswordRequest : receives
    AuthController ..> ValidateResetTokenRequest : receives
    AuthController ..> ResetPasswordRequest : receives
    AuthController ..> TotpSetupRequest : receives
    AuthController ..> TotpVerificationRequest : receives
    AuthController ..> EmailVerificationRequest : receives
    AuthController ..> AuthResponse : returns
    
    PostsController ..> CreatePostRequest : receives
    PostsController ..> UpdatePostRequest : receives
    PostsController ..> PostDto : returns
    
    CommentsController ..> CreateCommentRequest : receives
    CommentsController ..> UpdateCommentRequest : receives
    CommentsController ..> CommentDto : returns
    
    UsersController ..> CreateUserRequest : receives
    UsersController ..> UpdateUserRequest : receives
    
    SearchController ..> SearchResultDto : returns
```

## Architecture Layers

### 1. **Core Layer (EchoSpace.Core)**
- **Entities**: Domain models representing business entities
- **DTOs**: Data Transfer Objects for API communication
- **Interfaces**: Contracts for services and repositories
- **Services**: Business logic implementation (Core Services)
- **Enums**: Enumerations used across the application

### 2. **Infrastructure Layer (EchoSpace.Infrastructure)**
- **DbContext**: Entity Framework database context
- **Repositories**: Data access layer implementations
- **Services**: Infrastructure services (Auth, TOTP, Search)
- **Migrations**: Database migration files

### 3. **UI Layer (EchoSpace.UI)**
- **Controllers**: API endpoints handling HTTP requests
- Entry point for the backend application

### 4. **Tools Layer (EchoSpace.Tools)**
- **Services**: Utility services like EmailSender
- **EmailSettings**: Configuration for email service
- **Interfaces**: Tool-specific interfaces

## Key Relationships

### Entity Relationships
- **User** has many UserSessions, AuthProviders, Posts, Comments, Likes
- **User** participates in Follow relationships (follower/followed)
- **Post** has many Comments and Likes
- **Comment** and **Like** belong to both Post and User

### Service Layer Relationships
- **Core Services** (UserService, PostService, CommentService) depend on their respective repositories
- **Infrastructure Services** (AuthService, TotpService, SearchService) depend on DbContext
- **Controllers** depend on services and orchestrate the application flow

### Repository Pattern
- Repositories implement interfaces and provide data access abstraction
- All repositories depend on EchoSpaceDbContext
- Services depend on repository interfaces, enabling testability


# EchoSpace Registration Flow

## Complete Registration Flow

### Step 1: User Registration
1. User fills out registration form with:
   - Full Name
   - Email Address
   - Password (with complexity requirements)
   - Confirm Password

2. **Backend Processing:**
   - Validates input
   - Checks email uniqueness
   - Hashes password with PBKDF2 (100,000 iterations)
   - Creates user account
   - **Sends 6-digit verification code to email**
   - Returns `RequiresEmailVerification = true`

3. **Frontend:**
   - Moves to Step 2 (Email Verification)

---

### Step 2: Email Verification
1. User receives 6-digit code via email
2. User enters verification code
3. **Backend Processing:**
   - Verifies code is valid and not expired
   - Checks attempts (max 3 attempts)
   - Marks email as confirmed
   - **Generates JWT tokens (Access + Refresh)**
   - Sets `EmailConfirmed = true`

4. **Frontend:**
   - **Automatically saves tokens to session**
   - User is now authenticated
   - Moves to Step 3 (TOTP Setup)

---

### Step 3: TOTP Setup (2FA)
1. User scans QR code with authenticator app
2. User enters 6-digit TOTP code to verify
3. **Backend Processing:**
   - Validates TOTP code
   - Saves TOTP secret key
   - User remains authenticated

4. **Frontend:**
   - Redirects to `/home`

---

## Complete Flow Summary

```
Registration Form
      ↓
  Email Sent
      ↓
Email Verification (6-digit code)
      ↓
Tokens Generated & Stored
      ↓
TOTP Setup (QR Code)
      ↓
TOTP Verified
      ↓
   Home Page ✅
```

## Key Security Features

### ✅ Email Verification
- Prevents fake email registration
- 10-minute expiry on verification codes
- 3-attempt limit
- Code expires after use

### ✅ Secure Password Storage
- PBKDF2 with 100,000 iterations
- Unique salt per password
- Minimum 10 characters
- Requires uppercase + special character

### ✅ Multi-Factor Authentication
- TOTP required for all users
- 30-second rotating codes
- HMAC-SHA1 encryption

### ✅ Session Management
- Short-lived access tokens (15 minutes)
- Refresh token rotation (7 days)
- Automatic session persistence

---

## User Experience

### No Extra Login Required
The user **does NOT** need to login after registration because:

1. **After email verification:** Tokens are automatically generated and stored
2. **During TOTP setup:** User remains authenticated (tokens in session)
3. **After TOTP completion:** User is immediately redirected to home

### Step Progression
- User can go **Back** from any step
- Can resend verification codes (with cooldown)
- Clear error messages
- Loading states for all operations

---

## Implementation Files

### Backend
- `AuthService.RegisterAsync()` - Creates user, sends email
- `AuthService.CompleteRegistrationWithEmailVerificationAsync()` - Verifies email, generates tokens
- `TotpService.SendEmailVerificationCodeAsync()` - Sends 6-digit code
- `TotpService.VerifyEmailCodeAsync()` - Validates code
- `TotpService.SetupTotpAsync()` - Generates QR code
- `TotpService.VerifyTotpAsync()` - Validates TOTP

### Frontend
- `RegisterComponent` - 3-step flow management
- `EmailVerificationComponent` - Email code input
- `TotpSetupComponent` - QR code generation and verification
- `AuthService` - API calls and session management

---

## Registration Endpoints

```
POST /api/auth/register
  → Creates user, sends email verification code

POST /api/auth/complete-registration
  → Verifies email code, generates tokens

POST /api/auth/send-email-verification
  → Resends verification code

POST /api/auth/setup-totp
  → Generates TOTP secret and QR code

POST /api/auth/verify-totp
  → Verifies TOTP code during setup
```

---

## Flow Diagram

```
┌─────────────────────┐
│  Registration Form  │
│  (Name, Email, Pwd) │
└──────────┬───────────┘
           │
           ▼
┌─────────────────────┐
│   Email Verification│
│   6-digit code      │
│   (Tokens Generated)│
└──────────┬───────────┘
           │
           ▼
┌─────────────────────┐
│   TOTP Setup        │
│   QR Code + Verify  │
└──────────┬───────────┘
           │
           ▼
┌─────────────────────┐
│     Home Page ✅    │
│  (Fully Authenticated)│
└─────────────────────┘
```

---

## Testing the Flow

1. **Register** at `http://localhost:4200/register`
2. Fill form with valid data
3. Check email for 6-digit code
4. Enter code → Should move to TOTP setup
5. Scan QR code with authenticator app
6. Enter TOTP code → Should redirect to home
7. **User is logged in!** No need to login again.

---

## Error Handling

- Email already exists → Clear error message
- Invalid verification code → Shows remaining attempts
- Code expired → Option to resend
- TOTP verification failed → Can retry
- Network errors → Graceful handling

---

## Security Measures

✅ No user enumeration (generic error messages)  
✅ Rate limiting (should be added)  
✅ Password strength validation  
✅ Email verification required  
✅ TOTP mandatory  
✅ Secure token storage  
✅ Session management

---

*This flow ensures maximum security while providing a smooth user experience.*

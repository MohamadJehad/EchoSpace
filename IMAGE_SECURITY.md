# Image Upload and Security Handling

## Overview

This document describes the image upload flow and the security measures implemented to protect against various attacks including MIME type sniffing, cross-site scripting (XSS), and malicious file uploads.

## Image Upload Flow

### 1. Upload Request

**Endpoint:** `POST /api/images/upload`

**Authentication:** Required (`[Authorize]` attribute)

**Request Format:** `multipart/form-data`

**Flow:**
1. User authenticates and receives JWT token
2. Frontend sends image file with authentication header
3. Backend validates user identity from JWT token
4. User ID is extracted from token claims (prevents user ID spoofing)

### 2. Security Validations During Upload

#### File Validation

```csharp
// File existence check
if (request.File == null || request.File.Length == 0)
{
    throw new ArgumentException("File is required and cannot be empty");
}

// File size validation (10MB limit)
if (request.File.Length > MaxFileSize) // 10 * 1024 * 1024 bytes
{
    throw new ArgumentException($"File size exceeds maximum allowed size of 10MB");
}

// Content-Type whitelist validation
private readonly string[] AllowedImageTypes = { 
    "image/jpeg", 
    "image/jpg", 
    "image/png", 
    "image/gif", 
    "image/webp" 
};

if (!AllowedImageTypes.Contains(request.File.ContentType.ToLower()))
{
    throw new ArgumentException($"File type {request.File.ContentType} is not allowed");
}
```

**Security Benefits:**
- **Prevents oversized uploads** that could cause DoS attacks
- **Restricts file types** to only safe image formats
- **Prevents executable file uploads** (e.g., `.exe`, `.js`, `.php`)

#### Authentication & Authorization

- **JWT Token Required:** All upload endpoints require authentication
- **User ID from Token:** User ID is extracted from JWT claims, not from request body
- **Prevents ID Spoofing:** Users cannot upload images as other users

### 3. Storage

**Storage Location:** Azure Blob Storage

**Naming Strategy:**
- Images are stored with GUID-based blob names (not original filenames)
- Prevents path traversal attacks and filename conflicts
- Original filename is stored in database metadata only

**Container Organization:**
- `images` - User uploaded images
- `ai-images` - AI-generated images
- `system-images` - System-generated images
- `imported-images` - Externally imported images

**Metadata Storage:**
- Image metadata (ContentType, Size, UserId, etc.) stored in SQL database
- ContentType is explicitly stored to prevent MIME sniffing later

## Image Serving and Security Headers

### Secure Image Serving Endpoint

**Endpoint:** `GET /api/images/{imageId}/serve`

**Authentication:** Optional (`[AllowAnonymous]` - can be changed to `[Authorize]` if needed)

**Purpose:** Proxy images from blob storage with explicit security headers

### Security Headers Applied

#### 1. Content-Type Header
```csharp
Response.Headers["Content-Type"] = contentType; // e.g., "image/jpeg"
```
**Purpose:** Explicitly tells the browser the content type
**Protection:** Prevents browser from guessing content type

#### 2. X-Content-Type-Options: nosniff
```csharp
Response.Headers["X-Content-Type-Options"] = "nosniff";
```
**Purpose:** Prevents MIME type sniffing
**Protection:** 
- Browsers will NOT guess content type if header is missing
- Prevents malicious files from being treated as scripts
- Critical for preventing XSS attacks via image uploads

**Attack Scenario Prevented:**
```
Attacker uploads a file named "image.jpg" but it's actually JavaScript code
Without nosniff: Browser might sniff and execute it as script
With nosniff: Browser respects Content-Type and treats it as image
```

#### 3. X-Frame-Options: DENY
```csharp
Response.Headers["X-Frame-Options"] = "DENY";
```
**Purpose:** Prevents clickjacking attacks
**Protection:** Prevents page from being embedded in iframes

#### 4. Cache-Control
```csharp
Response.Headers["Cache-Control"] = "public, max-age=31536000"; // 1 year
```
**Purpose:** Controls caching behavior
**Protection:** Ensures proper caching while maintaining security

#### 5. Content-Security-Policy
```csharp
Response.Headers["Content-Security-Policy"] = 
    "default-src 'none'; img-src 'self' data:;";
```
**Purpose:** Restricts what resources can be loaded
**Protection:** Prevents loading of external malicious resources

### Complete Security Headers Example

```csharp
[HttpGet("{imageId}/serve")]
[AllowAnonymous]
public async Task<IActionResult> ServeImage(Guid imageId)
{
    // Get image metadata
    var image = await _imageRepository.GetByIdAsync(imageId);
    if (image == null)
    {
        return NotFound(new { message = "Image not found" });
    }

    // Download from blob storage
    var imageBytes = await _blobStorageService.DownloadBlobAsync(
        image.ContainerName, 
        image.BlobName);
    
    // Determine content type from stored metadata
    var contentType = !string.IsNullOrEmpty(image.ContentType) 
        ? image.ContentType 
        : "image/jpeg"; // Safe default

    // Set security headers
    Response.Headers["X-Content-Type-Options"] = "nosniff";
    Response.Headers["Content-Type"] = contentType;
    Response.Headers["X-Frame-Options"] = "DENY";
    Response.Headers["Cache-Control"] = "public, max-age=31536000";
    Response.Headers["Content-Security-Policy"] = 
        "default-src 'none'; img-src 'self' data:;";

    return File(imageBytes, contentType);
}
```

## Global Security Headers (Middleware)

Additional security headers are applied globally via `SecurityHeadersMiddleware`:

```csharp
// Applied to all responses
context.Response.Headers["X-Frame-Options"] = "DENY";
context.Response.Headers["X-Content-Type-Options"] = "nosniff";
context.Response.Headers["Content-Security-Policy"] = 
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
    "style-src 'self' 'unsafe-inline'; " +
    "img-src 'self' data: https: blob:; " +
    "font-src 'self' data:; " +
    "connect-src 'self' https:; " +
    "frame-ancestors 'none';";
context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
context.Response.Headers["Strict-Transport-Security"] = 
    "max-age=31536000; includeSubDomains; preload";
```

## Attack Vectors Prevented

### 1. MIME Type Sniffing Attack
**Attack:** Upload malicious JavaScript file with `.jpg` extension
**Prevention:** 
- Content-Type validation during upload (whitelist)
- `X-Content-Type-Options: nosniff` header when serving
- Explicit `Content-Type` header from stored metadata

### 2. Cross-Site Scripting (XSS)
**Attack:** Upload image with embedded script or SVG with JavaScript
**Prevention:**
- Content-Type whitelist prevents SVG uploads (if not in whitelist)
- `X-Content-Type-Options: nosniff` prevents execution
- `Content-Security-Policy` restricts script execution

### 3. Path Traversal Attack
**Attack:** Upload file with `../../../etc/passwd` in filename
**Prevention:**
- GUID-based blob naming (no original filename in path)
- Original filename stored only in database metadata

### 4. File Size DoS Attack
**Attack:** Upload extremely large files to exhaust server resources
**Prevention:**
- 10MB file size limit enforced during upload
- Validation before processing/uploading to blob storage

### 5. Unauthorized Access
**Attack:** User tries to upload images as another user
**Prevention:**
- JWT token required for all uploads
- User ID extracted from token claims (not request body)
- Cannot spoof user identity

### 6. Clickjacking
**Attack:** Embed image in malicious iframe
**Prevention:**
- `X-Frame-Options: DENY` header
- `frame-ancestors 'none'` in CSP

## Best Practices Implemented

1. ✅ **Whitelist Approach:** Only allow specific image types (not blacklist)
2. ✅ **Size Limits:** Enforce maximum file size
3. ✅ **Secure Storage:** Use GUID-based naming, not original filenames
4. ✅ **Explicit Content-Type:** Store and serve explicit MIME types
5. ✅ **Security Headers:** Multiple layers of protection
6. ✅ **Authentication:** All uploads require valid JWT token
7. ✅ **Metadata Storage:** ContentType stored in database for verification
8. ✅ **Logging:** All uploads and deletions are logged for audit

## Testing Security Headers

### Using Browser Developer Tools
1. Open browser DevTools (F12)
2. Navigate to Network tab
3. Request an image: `GET /api/images/{imageId}/serve`
4. Check Response Headers:
   - `Content-Type: image/jpeg`
   - `X-Content-Type-Options: nosniff`
   - `X-Frame-Options: DENY`
   - `Cache-Control: public, max-age=31536000`

### Using curl
```bash
curl -I https://localhost:7131/api/images/{imageId}/serve
```

### Using PowerShell
```powershell
Invoke-WebRequest -Uri "https://localhost:7131/api/images/{imageId}/serve" -Method Head | Select-Object -ExpandProperty Headers
```

## Summary

The image upload and serving system implements multiple layers of security:

1. **Upload Security:**
   - Authentication required
   - File type whitelist
   - File size limits
   - GUID-based storage naming

2. **Serving Security:**
   - Explicit Content-Type headers
   - X-Content-Type-Options: nosniff
   - X-Frame-Options: DENY
   - Content-Security-Policy
   - Cache-Control headers

3. **Global Security:**
   - SecurityHeadersMiddleware applies headers to all responses
   - Additional CSP, Referrer-Policy, and HSTS headers

These measures work together to prevent MIME sniffing, XSS attacks, clickjacking, and other security vulnerabilities related to image handling.


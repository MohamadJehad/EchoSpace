# PowerShell script to generate SSL certificates for localhost using .NET dev certificate
Write-Host "Generating SSL certificates for localhost..." -ForegroundColor Green

$sslDir = $PSScriptRoot

# Method 1: Try to use .NET dev certificate (if available)
Write-Host "`nAttempting to use .NET dev certificate..." -ForegroundColor Cyan
try {
    # Export certificate
    dotnet dev-certs https --export-path "$sslDir\cert.pem" --format Pem --no-password
    
    if (Test-Path "$sslDir\cert.pem") {
        Write-Host "✓ Certificate exported successfully" -ForegroundColor Green
        
        # .NET dev-certs doesn't export the private key separately
        # We need to generate a new certificate with both key and cert
        Write-Host "`nNote: .NET dev-certs doesn't export the private key." -ForegroundColor Yellow
        Write-Host "Generating a new self-signed certificate with key..." -ForegroundColor Cyan
        
        # Use PowerShell to create a self-signed certificate
        $cert = New-SelfSignedCertificate `
            -DnsName "localhost", "*.localhost", "127.0.0.1" `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -KeyAlgorithm RSA `
            -KeyLength 2048 `
            -NotAfter (Get-Date).AddYears(1) `
            -FriendlyName "EchoSpace Localhost Dev Certificate"
        
        # Export certificate
        $certPath = "$sslDir\cert.pem"
        $cert | Export-Certificate -FilePath $certPath -Type CERT
        
        # Export private key
        $keyPath = "$sslDir\key.pem"
        $pwd = ConvertTo-SecureString -String "temp" -Force -AsPlainText
        Export-PfxCertificate -Cert $cert -FilePath "$sslDir\temp.pfx" -Password $pwd | Out-Null
        
        # Extract key from PFX using certutil (Windows built-in)
        certutil -exportPFX -p "temp" "$sslDir\temp.pfx" "$sslDir\temp.pfx" | Out-Null
        
        # Convert PFX to PEM format (simplified - we'll use OpenSSL if available, otherwise manual)
        Write-Host "`nAttempting to extract private key..." -ForegroundColor Cyan
        
        # Check if OpenSSL is available
        $openssl = Get-Command openssl -ErrorAction SilentlyContinue
        if ($openssl) {
            openssl pkcs12 -in "$sslDir\temp.pfx" -nocerts -nodes -out $keyPath -passin pass:temp
            Remove-Item "$sslDir\temp.pfx" -Force
            Write-Host "✓ Private key extracted using OpenSSL" -ForegroundColor Green
        } else {
            Write-Host "`nOpenSSL not found. Please install OpenSSL or use the following:" -ForegroundColor Yellow
            Write-Host "1. Download OpenSSL: https://slproweb.com/products/Win32OpenSSL.html" -ForegroundColor Yellow
            Write-Host "2. Or use the browser's 'Continue anyway' option for development" -ForegroundColor Yellow
            Write-Host "3. Or install mkcert: https://github.com/FiloSottile/mkcert" -ForegroundColor Yellow
            Remove-Item "$sslDir\temp.pfx" -Force
            exit 1
        }
        
        # Remove certificate from store (optional)
        Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -Force
        
        Write-Host "`n✓ Certificates generated successfully!" -ForegroundColor Green
        Write-Host "Files created:" -ForegroundColor Cyan
        Write-Host "  - key.pem (private key)" -ForegroundColor White
        Write-Host "  - cert.pem (certificate)" -ForegroundColor White
        Write-Host "`nNext steps:" -ForegroundColor Yellow
        Write-Host "1. Trust the certificate in Windows:" -ForegroundColor White
        Write-Host "   certutil -addstore -f 'ROOT' cert.pem" -ForegroundColor Gray
        Write-Host "2. Restart your browser" -ForegroundColor White
        Write-Host "3. Restart the Angular dev server" -ForegroundColor White
    }
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "`nFalling back to manual certificate generation..." -ForegroundColor Yellow
}


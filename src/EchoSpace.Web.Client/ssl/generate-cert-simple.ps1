# Simple PowerShell script to generate SSL certificates for localhost
Write-Host "Generating SSL certificates for localhost..." -ForegroundColor Green

$sslDir = $PSScriptRoot

# Create self-signed certificate with private key
Write-Host "Creating self-signed certificate..." -ForegroundColor Cyan

$cert = New-SelfSignedCertificate -DnsName "localhost", "*.localhost", "127.0.0.1", "[::1]" -CertStoreLocation "Cert:\CurrentUser\My" -KeyAlgorithm RSA -KeyLength 2048 -NotAfter (Get-Date).AddYears(1) -FriendlyName "EchoSpace Localhost Dev Certificate" -KeyUsage DigitalSignature, KeyEncipherment -KeyExportPolicy Exportable

Write-Host "✓ Certificate created in certificate store" -ForegroundColor Green

# Export certificate to PEM
$certPath = "$sslDir\cert.pem"
$cert | Export-Certificate -FilePath $certPath -Type CERT | Out-Null
Write-Host "✓ Certificate exported to cert.pem" -ForegroundColor Green

# Export to PFX first (needed to get private key)
$pfxPath = "$sslDir\temp.pfx"
$password = ConvertTo-SecureString -String "temp123" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $password | Out-Null

# Check if OpenSSL is available
$openssl = Get-Command openssl -ErrorAction SilentlyContinue
if ($openssl) {
    Write-Host "Extracting private key using OpenSSL..." -ForegroundColor Cyan
    $keyPath = "$sslDir\key.pem"
    & openssl pkcs12 -in $pfxPath -nocerts -nodes -out $keyPath -passin pass:temp123 2>&1 | Out-Null
    
    if (Test-Path $keyPath) {
        Write-Host "✓ Private key extracted to key.pem" -ForegroundColor Green
        Remove-Item $pfxPath -Force
    } else {
        Write-Host "✗ Failed to extract private key" -ForegroundColor Red
    }
} else {
    Write-Host ""
    Write-Host "OpenSSL not found. The certificate is created but the private key needs OpenSSL to extract." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "1. Install OpenSSL: https://slproweb.com/products/Win32OpenSSL.html" -ForegroundColor White
    Write-Host "2. Then run this script again" -ForegroundColor White
    Write-Host "3. Or use: openssl pkcs12 -in temp.pfx -nocerts -nodes -out key.pem -passin pass:temp123" -ForegroundColor Gray
}

# Remove certificate from store (optional - comment out if you want to keep it)
# Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -Force

Write-Host ""
Write-Host "✓ Certificate generation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Trust the certificate: certutil -addstore -f 'ROOT' cert.pem" -ForegroundColor White
Write-Host "2. Close all browser windows" -ForegroundColor White
Write-Host "3. Restart Angular dev server" -ForegroundColor White

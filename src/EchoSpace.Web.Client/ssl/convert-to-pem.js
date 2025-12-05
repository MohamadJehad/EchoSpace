// Convert DER certificate to PEM format
const fs = require('fs');
const forge = require('node-forge');
const path = require('path');

const sslDir = __dirname;
const certDerPath = path.join(sslDir, 'cert.pem'); // Currently DER format
const certPemPath = path.join(sslDir, 'cert.pem'); // Will be PEM format

console.log('Converting certificate from DER to PEM format...\n');

try {
    // Read the DER certificate
    const certDer = fs.readFileSync(certDerPath);
    
    // Convert DER to ASN1
    const asn1 = forge.asn1.fromDer(certDer.toString('binary'));
    
    // Parse certificate
    const cert = forge.pki.certificateFromAsn1(asn1);
    
    // Convert to PEM format
    const pem = forge.pki.certificateToPem(cert);
    
    // Write PEM certificate
    fs.writeFileSync(certPemPath, pem);
    
    console.log('✓ Certificate converted to PEM format successfully!');
    console.log(`✓ Saved to: ${certPemPath}`);
    
    // Verify the format
    const pemContent = fs.readFileSync(certPemPath, 'utf8');
    if (pemContent.includes('-----BEGIN CERTIFICATE-----')) {
        console.log('✓ PEM format verified (contains BEGIN/END headers)');
    } else {
        console.error('✗ Warning: PEM format may be incorrect');
    }
    
} catch (error) {
    console.error('Error converting certificate:', error.message);
    console.error('\nTrying alternative method...');
    
    // Alternative: Use the certificate from the PFX we already have
    try {
        // Re-export from certificate store
        const { execSync } = require('child_process');
        
        // Get certificate from store and export as PEM
        const cert = require('child_process').execSync(
            'powershell -Command "Get-ChildItem -Path Cert:\\CurrentUser\\My | Where-Object { $_.FriendlyName -eq \'EchoSpace Localhost Dev\' } | Select-Object -First 1 | ForEach-Object { $_.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert) }"',
            { encoding: 'buffer' }
        );
        
        // Convert using forge
        const asn1 = forge.asn1.fromDer(cert.toString('binary'));
        const x509 = forge.pki.certificateFromAsn1(asn1);
        const pem = forge.pki.certificateToPem(x509);
        fs.writeFileSync(certPemPath, pem);
        console.log('✓ Certificate converted using alternative method!');
    } catch (altError) {
        console.error('Alternative method failed:', altError.message);
        console.error('\nPlease regenerate the certificate using the full process.');
        process.exit(1);
    }
}


// Node.js script to extract private key from PFX file
const fs = require('fs');
const { execSync } = require('child_process');
const path = require('path');

const sslDir = __dirname;
const pfxPath = path.join(sslDir, 'temp.pfx');
const keyPath = path.join(sslDir, 'key.pem');
const password = 'temp123';

console.log('Extracting private key from PFX file...\n');

// Check if PFX file exists
if (!fs.existsSync(pfxPath)) {
    console.error('Error: temp.pfx not found!');
    console.log('Please run the certificate generation first.');
    process.exit(1);
}

try {
    // Use Node.js crypto or openssl command
    // Since we don't have OpenSSL, we'll use a Node.js library approach
    // But first, let's try using the system's certutil or a Node.js package
    
    // Method 1: Try using Node.js forge library (if available)
    try {
        const forge = require('node-forge');
        const pfxData = fs.readFileSync(pfxPath);
        const pfxAsn1 = forge.asn1.fromDer(pfxData.toString('binary'));
        const pfx = forge.pkcs12.pkcs12FromAsn1(pfxAsn1, password);
        
        // Get the private key
        const keyBags = pfx.getBags({ bagType: forge.pki.oids.pkcs8ShroudedKeyBag });
        const keyBag = keyBags[forge.pki.oids.pkcs8ShroudedKeyBag];
        
        if (keyBag && keyBag.length > 0) {
            const privateKey = keyBag[0].key;
            const pemKey = forge.pki.privateKeyToPem(privateKey);
            fs.writeFileSync(keyPath, pemKey);
            console.log('✓ Private key extracted successfully using node-forge!');
            console.log(`✓ Saved to: ${keyPath}`);
            
            // Clean up
            fs.unlinkSync(pfxPath);
            console.log('✓ Cleaned up temp.pfx');
            process.exit(0);
        }
    } catch (forgeError) {
        // node-forge not available, try alternative
    }
    
    // Method 2: Use PowerShell to extract (Windows only)
    console.log('Trying alternative method...');
    try {
        // Use certutil to convert PFX to PEM (Windows built-in)
        const certPath = path.join(sslDir, 'temp-cert.pem');
        execSync(`certutil -decode temp.pfx temp-base64.txt`, { cwd: sslDir, stdio: 'ignore' });
        
        // This is complex, let's use a simpler approach
        console.log('\nAlternative: Using mkcert (free and recommended)');
        console.log('Download from: https://github.com/FiloSottile/mkcert/releases');
        console.log('Then run: mkcert -key-file key.pem -cert-file cert.pem localhost 127.0.0.1\n');
        
    } catch (error) {
        console.error('Error:', error.message);
    }
    
    console.log('\nSince node-forge is not installed, here are your options:');
    console.log('1. Install node-forge: npm install node-forge');
    console.log('2. Use mkcert (recommended): https://github.com/FiloSottile/mkcert/releases');
    console.log('3. Use browser "Continue anyway" for development');
    
} catch (error) {
    console.error('Error extracting key:', error.message);
    process.exit(1);
}


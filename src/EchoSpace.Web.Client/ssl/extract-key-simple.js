// Simple Node.js script to extract private key from PFX using node-forge
const fs = require('fs');
const forge = require('node-forge');
const path = require('path');

const sslDir = __dirname;
const pfxPath = path.join(sslDir, 'temp.pfx');
const keyPath = path.join(sslDir, 'key.pem');
const password = 'temp123';

console.log('Extracting private key from PFX file...\n');

if (!fs.existsSync(pfxPath)) {
    console.error('Error: temp.pfx not found!');
    process.exit(1);
}

try {
    // Read PFX file
    const pfxData = fs.readFileSync(pfxPath, 'binary');
    const pfxAsn1 = forge.asn1.fromDer(pfxData);
    const pfx = forge.pkcs12.pkcs12FromAsn1(pfxAsn1, false, password);
    
    // Get the private key
    const keyBags = pfx.getBags({ bagType: forge.pki.oids.pkcs8ShroudedKeyBag });
    const keyBag = keyBags[forge.pki.oids.pkcs8ShroudedKeyBag];
    
    if (keyBag && keyBag.length > 0) {
        const privateKey = keyBag[0].key;
        const pemKey = forge.pki.privateKeyToPem(privateKey);
        fs.writeFileSync(keyPath, pemKey);
        console.log('✓ Private key extracted successfully!');
        console.log(`✓ Saved to: ${keyPath}`);
        
        // Clean up
        fs.unlinkSync(pfxPath);
        console.log('✓ Cleaned up temp.pfx');
        console.log('\nNext steps:');
        console.log('1. Trust the certificate: certutil -addstore -f "ROOT" cert.pem');
        console.log('2. Close all browser windows');
        console.log('3. Restart Angular dev server');
    } else {
        console.error('Error: Could not find private key in PFX file');
        process.exit(1);
    }
} catch (error) {
    console.error('Error extracting key:', error.message);
    console.error('\nTrying alternative method...');
    
    // Alternative: Try with different bag types
    try {
        const pfxData = fs.readFileSync(pfxPath, 'binary');
        const pfxAsn1 = forge.asn1.fromDer(pfxData);
        const pfx = forge.pkcs12.pkcs12FromAsn1(pfxAsn1, false, password);
        
        // Try all bag types
        const bags = pfx.getBags({ bagType: forge.pki.oids.privateKeyBag });
        if (bags[forge.pki.oids.privateKeyBag] && bags[forge.pki.oids.privateKeyBag].length > 0) {
            const privateKey = bags[forge.pki.oids.privateKeyBag][0].key;
            const pemKey = forge.pki.privateKeyToPem(privateKey);
            fs.writeFileSync(keyPath, pemKey);
            console.log('✓ Private key extracted successfully (alternative method)!');
            fs.unlinkSync(pfxPath);
            console.log('✓ Cleaned up temp.pfx');
        } else {
            throw new Error('No private key found');
        }
    } catch (altError) {
        console.error('Alternative method also failed:', altError.message);
        process.exit(1);
    }
}


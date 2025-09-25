#!/usr/bin/env node

/**
 * Bonjour Service Discovery Debug Tool
 * 
 * This script helps debug Bonjour/mDNS service discovery issues,
 * particularly with Android clients connecting to the RNet-Pi service.
 * 
 * Usage:
 *   node debug_bonjour.js [publish|discover|both]
 * 
 * Environment Variables:
 *   BONJOUR_DEBUG=true     Enable verbose logging
 *   SERVICE_NAME=name      Override default service name
 *   SERVICE_PORT=port      Override default port (3000)
 *   SERVICE_TYPE=type      Override service type (rnet)
 */

const bonjour = require("bonjour-service");

const mode = process.argv[2] || 'both';
const serviceName = process.env.SERVICE_NAME || 'RNet-Debug-Test';
const servicePort = parseInt(process.env.SERVICE_PORT) || 3000;
const serviceType = process.env.SERVICE_TYPE || 'rnet';

console.log('=== RNet-Pi Bonjour Debug Tool ===');
console.log(`Mode: ${mode}`);
console.log(`Service Name: ${serviceName}`);
console.log(`Service Port: ${servicePort}`);
console.log(`Service Type: ${serviceType}`);
console.log('=====================================\n');

const bonjourInstance = new bonjour.Bonjour();

if (mode === 'publish' || mode === 'both') {
    console.log('Publishing test service...');
    
    const service = bonjourInstance.publish({
        name: serviceName,
        type: serviceType,
        port: servicePort,
        txt: {
            version: '1.1.1',
            protocol: 'tcp',
            model: 'rnet-pi',
            capabilities: 'audio,zones,sources',
            debug: 'true'
        }
    });

    service.on('up', () => {
        console.log(`✓ Service "${serviceName}" is now advertised on the network`);
        console.log(`  Type: _${serviceType}._tcp.local`);
        console.log(`  Port: ${servicePort}`);
    });

    service.on('error', (err) => {
        console.error(`✗ Service error: ${err.message}`);
    });
}

if (mode === 'discover' || mode === 'both') {
    console.log('\nDiscovering services on the network...');
    console.log('(This will run for 30 seconds)\n');

    // Discover RNet services
    bonjourInstance.find({ type: serviceType }, (service) => {
        console.log(`🔍 Found ${serviceType} service:`);
        console.log(`   Name: ${service.name}`);
        console.log(`   Host: ${service.host}`);
        console.log(`   Port: ${service.port}`);
        console.log(`   Addresses: ${service.addresses?.join(', ') || 'none'}`);
        if (service.txt) {
            console.log(`   TXT Records:`);
            Object.entries(service.txt).forEach(([key, value]) => {
                console.log(`     ${key}: ${value}`);
            });
        }
        console.log('');
    });

    // Also discover common Android-friendly service types for comparison
    const androidFriendlyTypes = ['http', 'homekit', 'airplay'];
    androidFriendlyTypes.forEach(type => {
        bonjourInstance.find({ type }, (service) => {
            console.log(`📱 Found Android-friendly ${type} service: ${service.name} at ${service.host}:${service.port}`);
        });
    });
}

// Cleanup after 30 seconds
setTimeout(() => {
    console.log('\n=== Debug session completed ===');
    console.log('To troubleshoot Android discovery issues:');
    console.log('1. Check if your Android app discovers the service above');
    console.log('2. Try different service types: http, homekit, tcp');
    console.log('3. Ensure Android device is on the same network');
    console.log('4. Check Android app\'s mDNS/Bonjour permissions');
    console.log('5. Test with Android mDNS discovery apps from Play Store');
    
    bonjourInstance.destroy();
    process.exit(0);
}, 30000);

// Handle process termination
process.on('SIGINT', () => {
    console.log('\nShutting down...');
    bonjourInstance.destroy();
    process.exit(0);
});
/**
 * MessageLogger - Centralized logging for all messaging functions
 * Provides consistent logging format for serial and network messages
 */
class MessageLogger {
    /**
     * Log a serial message (RNet communication)
     * @param {string} direction - 'SENT' or 'RECEIVED' 
     * @param {Object} packet - The packet object
     * @param {Buffer} buffer - Raw buffer data (optional)
     */
    static logSerial(direction, packet, buffer = null) {
        const packetName = packet ? packet.constructor.name : 'Unknown';
        const timestamp = new Date().toISOString();
        
        let message = `[SERIAL ${direction}] ${packetName}`;
        
        // Add packet details if available
        if (packet) {
            const details = MessageLogger._extractPacketDetails(packet);
            if (details) {
                message += ` - ${details}`;
            }
        }
        
        // Add buffer info if available
        if (buffer) {
            message += ` [${buffer.length} bytes: ${buffer.toString('hex').substring(0, 32)}${buffer.length > 16 ? '...' : ''}]`;
        }
        
        console.log(message);
    }
    
    /**
     * Log a network message (Server communication)  
     * @param {string} direction - 'SENT' or 'RECEIVED'
     * @param {Object} packet - The packet object
     * @param {string} clientInfo - Client address or 'BROADCAST' (optional)
     */
    static logNetwork(direction, packet, clientInfo = null) {
        const packetName = packet ? packet.constructor.name : 'Unknown';
        const timestamp = new Date().toISOString();
        
        let message = `[NETWORK ${direction}] ${packetName}`;
        
        // Add client info
        if (clientInfo) {
            message += ` ${direction === 'SENT' ? 'to' : 'from'} ${clientInfo}`;
        }
        
        // Add packet details if available
        if (packet) {
            const details = MessageLogger._extractPacketDetails(packet);
            if (details) {
                message += ` - ${details}`;
            }
        }
        
        console.log(message);
    }
    
    /**
     * Extract relevant details from packet objects for logging
     * @param {Object} packet - The packet object
     * @returns {string} - Formatted details string
     */
    static _extractPacketDetails(packet) {
        if (!packet) return null;
        
        const details = [];
        
        // Common RNet packet properties
        if (packet.targetControllerID !== undefined) {
            details.push(`target: ${packet.targetControllerID}-${packet.targetZoneID}`);
        }
        if (packet.sourceControllerID !== undefined) {
            details.push(`source: ${packet.sourceControllerID}-${packet.sourceZoneID}`);
        }
        
        // Extract packet-specific data
        if (typeof packet.getPower === 'function') {
            details.push(`power: ${packet.getPower()}`);
        }
        if (typeof packet.getVolume === 'function') {
            details.push(`volume: ${packet.getVolume()}`);
        }
        if (typeof packet.getSourceID === 'function') {
            details.push(`sourceID: ${packet.getSourceID()}`);
        }
        if (typeof packet.getID === 'function') {
            details.push(`ID: 0x${packet.getID().toString(16).padStart(2, '0')}`);
        }
        
        return details.length > 0 ? details.join(', ') : null;
    }
}

module.exports = MessageLogger;
const EventEmitter = require("events");
const net = require("net");

const Client = require("./TCPClient");

/**
 * TCP Server with Bonjour/mDNS Service Discovery
 * 
 * ANDROID COMPATIBILITY NOTES:
 * 
 * The current implementation uses a custom service type "rnet" which may not be
 * easily discoverable by Android applications. For better Android compatibility,
 * consider these recommendations:
 * 
 * 1. SERVICE TYPE ALTERNATIVES:
 *    - Use "_http._tcp" with a custom path for web-based discovery
 *    - Use "_tcp._local" as a more generic TCP service
 *    - Consider "_homekit._tcp" if targeting IoT/smart home apps
 * 
 * 2. SERVICE NAME IMPROVEMENTS:
 *    - Avoid spaces and special characters in service names
 *    - Use descriptive but short names (Android may truncate long names)
 *    - Consider adding device model/version info
 * 
 * 3. TXT RECORD ENHANCEMENTS:
 *    - Add "path=/api" or similar for HTTP-based discovery
 *    - Include "version", "model", "serial" fields
 *    - Add "capabilities" field describing supported features
 * 
 * 4. NETWORK CONSIDERATIONS:
 *    - Some Android devices may have issues with link-local addresses
 *    - Ensure the service is advertised on the correct network interface
 *    - Consider dual-stack IPv4/IPv6 support
 * 
 * Example improved configuration:
 * {
 *   name: "RNet-Controller-ABC123",
 *   type: "homekit",  // or "http" with path
 *   port: this._port,
 *   txt: {
 *     version: "1.1.1",
 *     model: "RNet-Pi",
 *     protocol: "tcp",
 *     path: "/rnet",
 *     capabilities: "audio,zones,sources"
 *   }
 * }
 */
class Server extends EventEmitter {
    constructor(name, host, port) {
        super();

        this._name = name;
        this._port = port;
        this._clients = [];

        if (!host) {
            this._host = "0.0.0.0";
        }
        else {
            this._host = host;
        }

        this._server = net.createServer();

        this._server.on("error", (err) => {
            this.emit("error", err);
        });

        this._server.on("connection", (conn) => {
            this._handleConnection(conn);
        });
    }

    start() {
        this._server.listen(this._port, this._host, () => {
            try {
                const bonjour = require("bonjour-service");

                console.log(`[BONJOUR] Publishing service: ${this._name} - type: rnet - port: ${this._port}`);
                this._bonjour = new bonjour.Bonjour();
                
                // Publish the service with additional metadata for better Android compatibility
                this._service = this._bonjour.publish({
                    name: this._name, 
                    type: "rnet", 
                    port: this._port,
                    txt: {
                        version: "1.1.1",
                        protocol: "tcp"
                    }
                });

                // Add service event listeners for debugging
                this._service.on('up', () => {
                    console.log(`[BONJOUR] Service "${this._name}" is now advertised on the network`);
                });

                this._service.on('error', (err) => {
                    console.error(`[BONJOUR] Service error: ${err.message}`);
                });

                console.log(`[BONJOUR] Service published successfully`);

                // Add discovery monitoring for debugging (optional - can be enabled for troubleshooting)
                if (process.env.BONJOUR_DEBUG === "true") {
                    console.log("[BONJOUR] Debug mode enabled - monitoring network services");
                    this._bonjour.find({ type: 'rnet' }, (service) => {
                        console.log(`[BONJOUR] Discovered RNet service: ${service.name} at ${service.host}:${service.port}`);
                    });
                }
            }
            catch (e) {
                console.warn(`[BONJOUR] Failed to initialize Bonjour service: ${e.message}`);
                console.warn("[BONJOUR] Remotes won't be able to automatically find this controller.");
            }

            this.emit("start");
        });
    }

    broadcastBuffer(buffer) {
        for (let client of this._clients) {
            client.sendBuffer(buffer);
        }
    }

    stop(callback) {
        // Properly stop Bonjour service if it exists
        if (this._service) {
            try {
                console.log("[BONJOUR] Stopping service advertisement");
                this._service.stop();
                console.log("[BONJOUR] Service stopped successfully");
            } catch (e) {
                console.warn(`[BONJOUR] Error stopping service: ${e.message}`);
            }
        }

        for (let client of this._clients) {
            client.disconnect();
        }
        this._server.close(() => {
            console.info("Server stopped.")
            callback();
        });
    }

    getName() {
        return this._name;
    }

    setName(name) {
        if (name != this._name) {
            const oldName = this._name;
            this._name = name;

            // Update Bonjour service with new name if service is running
            if (this._service != null && this._bonjour != null) {
                try {
                    console.log(`[BONJOUR] Updating service name from "${oldName}" to "${name}"`);
                    this._service.stop();
                    
                    this._service = this._bonjour.publish({
                        name: this._name, 
                        type: "rnet", 
                        port: this._port,
                        txt: {
                            version: "1.1.1",
                            protocol: "tcp"
                        }
                    });

                    // Re-add event listeners
                    this._service.on('up', () => {
                        console.log(`[BONJOUR] Service "${this._name}" is now advertised on the network`);
                    });

                    this._service.on('error', (err) => {
                        console.error(`[BONJOUR] Service error: ${err.message}`);
                    });

                    console.log(`[BONJOUR] Service name updated successfully`);
                } catch (e) {
                    console.warn(`[BONJOUR] Error updating service name: ${e.message}`);
                }
            }
        }
    }

    getAddress() {
        const addr = this._server.address();
        return addr.address + ":" + addr.port;
    }

    getClientCount() {
        return this._clients.length;
    }

    _handleConnection(conn) {
        const client = new Client(conn)
        .once("close", () => {
            if (client.isSubscribed()) {
                this.emit("client_disconnect", client);

                let i = this._clients.indexOf(client);
                this._clients.splice(i, 1);
            }
        })
        .once("subscribed", () => {
            // Ready to tell the world!
            this._clients.push(client);
            this.emit("client_connected", client);
        })
        .on("packet", (packet) => {
            this.emit("packet", client, packet);
        });
    }
}

module.exports = Server;

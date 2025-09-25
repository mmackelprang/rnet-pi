RNET Pi
===
Using the RS-232 "automation" port on older Russound whole home audio systems, we can control them using a low-power computer such as a Raspberry Pi via a USB to serial adapter in order to retrofit modern day "smart" capabilites. RNET-Pi is a Node.JS server created to act as a proxy between smart devices and these legacy audio systems.

Features
---
- Front-end Android app -- Use your mobile phone or tablet to control your Russound system. ([Google Play](https://play.google.com/store/apps/details?id=me.zachcheatham.rnetremote))
- IFTTT support -- Allows the ability to automate your system using IFTT or utilize assistants such as Google Home or Alexa.
- Volume limit -- Individually limit zones to a maximum volume.
- Message Logging -- Comprehensive logging of all serial (RNet) and network messages for debugging and monitoring.
- Chromecast Audio Integration
  - Display currently playing media on wall plate displays.
  - Control Chromecast using existing wall plates.
  - (Configurable) Automatically activate zones and switch to appropriate source when Chromecast begins playing media.
  - (Configurable) Automaticallly turn off zones using a Cast device when media is no longer being played.

### Planned Features
 - Sonos Connect support.
 - Direct integration with Alexa and Google Home opposed to using IFTTT.
 - Web interface

### Supported Systems
In theory, this *should* work with the CAS44, CAA66, CAM6.6, and CAV6.6, but has only been tested with the CAV6.6. If you run into any issues with other devices, feel free to open an issue. The more support, the better.

Installation
---
##### Required Hardware
- [Raspberry Pi](https://www.raspberrypi.org/) or similar device running Linux
*This software most likely will work on Windows or macOS, but it's only been tested on Linux*
- Male USB to male RS-232 adapter ([Amazon](https://www.amazon.com/TRENDnet-Converter-Installation-Universal-TU-S9/dp/B0007T27H8)) *Not a specific recommendation, just an example.*
##### Download and Install
1. Verify your Raspberry Pi is up to date by running:
`sudo apt update && sudo apt upgrade`
2. Install [Node.JS](https://nodejs.org/en/):
`sudo apt install nodejs`
3. Install [forever-service](https://github.com/zapty/forever-service) in order to have RNET Pi run automatically at boot:
`sudo npm install -g forever-service`
4. Download RNET Pi:
`git clone https://gitlab.com/zachcheatham/rnet-pi.git`
`cd rnet-pi`
5. Download and install required libraries:
`npm install`
6. Install RNET Pi to a service for autostarting at boot:
`sudo forever-service install -s ./src/app.js rnet-pi`
##### Configuration
1. Run the server once to generate a config file
`npm start` *Wait for startup to complete* `^C`
2. Determine the device path of the serial adapter.
*The adapter should not be connected at this point!*
   1. Get a current listing of devices:
   `ls /dev/`
   3. Connect the RS232 adapter to the Russound device's serial port and the Pi's USB port.
   4. Get another listing of devices:
   `ls /dev/`
   5. Compare results to determine the newly connected adapter. For example, my adapter is `/dev/tty-usbserial1`
3. Open the configuration file for editing:
`nano config.json`
*These are low level config options that you shouldn't have to ever edit again.*
4. Replace the `serialDevice` property by replacing the existing value `/dev/tty-usbserial1` with the `/dev/` path you determined in step two. There's a good chance your adapter will by the same path.
5. [Advanced Users] Set the address and port you want the server to bind to here. If you don't know why you would change these, you can leave them alone.
6. Save and exit the configuration file by pressing `CTRL+O` followed by `CTRL+X`
##### Start the server
1. If you want to be sure the server starts up successfully, run `npm start` to run the server in your current console. This will close when you log out.
2. If you see `Connected to RNet!` in the terminal, everything is probably working normally. You can now exit `CTRL+C` so we can start the server as a service.
3. Start the server as a service:
`sudo systemctl start rnet-pi`
##### Setup the Zones and Sources
The RNET RS-232 protocol has no zone naming, method of determining which zones and sources have physical connections, or method to retrieve the names of sources. All of that is up to you. Before you can start using this system, you must connect to this newly created server using the [RNET Remote](https://play.google.com/store/apps/details?id=me.zachcheatham.rnetremote) app and add zones and sources.

Docker Installation (Alternative)
---
RNET-Pi now supports containerized deployment using Docker, which provides easier installation and better isolation.

### Prerequisites
- [Docker](https://docs.docker.com/get-docker/) installed on your system
- USB-to-RS232 adapter connected to your Russound system

### Quick Start with Docker Compose
1. Clone the repository:
   ```bash
   git clone https://gitlab.com/zachcheatham/rnet-pi.git
   cd rnet-pi
   ```

2. Update device path in `docker-compose.yml` if needed:
   ```yaml
   devices:
     - "/dev/ttyUSB0:/dev/ttyUSB0"  # Adjust this path as needed
   ```

3. Start the container:
   ```bash
   docker-compose up -d
   ```

### Manual Docker Build and Run
1. Build the Docker image:
   ```bash
   docker build -t rnet-pi .
   ```

2. Run the container:
   ```bash
   docker run -d \
     --name rnet-pi \
     -p 3000:3000 \
     --device=/dev/ttyUSB0:/dev/ttyUSB0 \
     -v $(pwd)/config.json:/app/config.json \
     -v $(pwd)/sources.json:/app/sources.json \
     -v $(pwd)/zones.json:/app/zones.json \
     rnet-pi
   ```

### Docker Configuration Notes
- The container runs on Node.js 18 (LTS) for maximum compatibility
- Configuration files are mounted as volumes for persistence
- The container uses `--device` to access the USB serial adapter  
- Network mode is set to `host` for mDNS/Bonjour service discovery
- Health checks are included to monitor container status
- The application starts automatically when the container starts

### Finding Your Serial Device
To find the correct device path for your USB-to-RS232 adapter:
```bash
# Before plugging in the adapter
ls /dev/tty*

# After plugging in the adapter  
ls /dev/tty*

# Compare the output - the new device is your adapter
# Common paths: /dev/ttyUSB0, /dev/ttyACM0, /dev/tty-usbserial1
```

Message Logging
---
RNET-Pi includes comprehensive message logging to help with debugging and monitoring system activity. All serial communication with RNet hardware and network communication with client applications is logged with detailed information.

### Logging Format
- **Serial Messages**: `[SERIAL SENT/RECEIVED] PacketName - details [buffer_size bytes: hex_data]`
- **Network Messages**: `[NETWORK SENT/RECEIVED] PacketName to/from client_address - details`

### What Gets Logged
- **Serial (RNet) Messages**:
  - All outgoing commands sent to RNet hardware (zone control, source changes, etc.)
  - All incoming responses from RNet hardware (status updates, zone information, etc.)
  - Raw buffer data in hexadecimal format for low-level debugging
- **Network Messages**:
  - All messages sent to client applications (mobile apps, web interfaces)
  - All commands received from client applications
  - Broadcast messages sent to all connected clients
  - Individual messages sent to specific clients

### Example Log Output
```
[SERIAL SENT] SetVolumePacket - target: 0-1, source: 0-0 [12 bytes: f0000170000070...]
[NETWORK RECEIVED] PacketC2SZoneVolume from 192.168.1.100 - ID: 0x09
[NETWORK SENT] PacketS2CZoneVolume to BROADCAST - ID: 0x0F
```

Logs are written to the console and can be redirected to files using standard output redirection or logging services like systemd-journald when running as a service.

Bonjour/mDNS Service Discovery Debugging
---
If you're having trouble with Android apps discovering the RNet-Pi service automatically, use the included debug tool:

### Quick Debug
```bash
# Test both service publishing and discovery
node debug_bonjour.js both

# Only test service publishing
node debug_bonjour.js publish

# Only test service discovery
node debug_bonjour.js discover
```

### Debug Environment Variables
```bash
# Enable verbose Bonjour logging in the main application
BONJOUR_DEBUG=true npm run dev

# Test with different service configurations
SERVICE_NAME="My-RNet-Controller" SERVICE_TYPE="http" node debug_bonjour.js
```

### Android Compatibility Issues
The default service uses type `_rnet._tcp` which may not be discovered by all Android applications. If you're experiencing issues:

1. **Test Discovery**: Use an Android mDNS browser app from the Play Store to verify the service is visible
2. **Network Check**: Ensure your Android device is on the same WiFi network as the RNet-Pi
3. **Service Type**: Consider changing the service type in `src/server/TCPServer.js` to:
   - `_http._tcp` for web-based discovery
   - `_homekit._tcp` for IoT/smart home compatibility
   - `_tcp._local` for generic TCP services

4. **Service Name**: Avoid spaces and special characters in service names for better Android compatibility

### Bonjour Service Logs
When running RNet-Pi, look for `[BONJOUR]` prefixed messages in the logs:
```
[BONJOUR] Publishing service: My Controller - type: rnet - port: 3000
[BONJOUR] Service "My Controller" is now advertised on the network
[BONJOUR] Service published successfully
```
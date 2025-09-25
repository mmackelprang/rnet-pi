const RNetPacket = require("./RNetPacket");
//const Buffer = require("buffer");

class HandshakePacket extends RNetPacket {
    constructor(controllerID, handshakeType) {
        super();
        this.targetControllerID = controllerID;
        this.messageType = 0x02;
        this._handshakeType = handshakeType;
    }

    getMessageBody() {
        const buffer = Buffer.alloc(1);
        buffer.writeUInt8(this._handshakeType, 0);
        return buffer;
    }
}

HandshakePacket.fromPacket = function(rNetPacket) {
    if (rNetPacket instanceof RNetPacket) {
        const handshakePacket = new HandshakePacket();
        rNetPacket.copyToPacket(handshakePacket);
        
        if (!Buffer.isBuffer(rNetPacket.messageBody)) {
            throw new Error("HandshakePacket.fromPacket: messageBody is not a valid Buffer");
        }
        
        handshakePacket._handshakeType = rNetPacket.messageBody.readUInt8(0);
        return handshakePacket;
    }
    else {
        throw new TypeError("Cannot create HandshakePacket with anything other than RNetPacket");
    }
}

module.exports = HandshakePacket;

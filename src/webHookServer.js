const EventEmitter = require("events");
const express = require("express");
const path = require("path");
const ExtraZoneParam = require("./rnet/extraZoneParam");

class WebHookServer extends EventEmitter {
    constructor(port, password, rNet) {
        super();

        if (!password) {
            console.warn("To use enable Web Hooks, set \"webHookPassword\" in config.json");
            return;
        }

        this._port = port;
        this._app = express();

        // Serve static files from public directory
        this._app.use(express.static(path.join(__dirname, '..', 'public')));

        this._app.use(function(req, res, next) {
            if (req.query.pass != password) {
                res.sendStatus(401);
                console.warn("[Web Hook] Bad password in request.");
            }
            else {
                next();
            }
        });

        this._app.use(function(req, res, next) {
            if (!rNet.isConnected()) {
                res.type("txt").status(503).send("RNet not connected.");
            }
            else {
                next();
            }
        });

        this._app.put("/on", function(req, res) {
            rNet.setAllPower(true);
            res.sendStatus(200);
        });

        this._app.put("/off", function(req, res) {
            rNet.setAllPower(false);
            res.sendStatus(200);
        });

        // API endpoints for Web UI
        this._app.get("/api/status", function(req, res) {
            res.json({ connected: rNet.isConnected() });
        });

        this._app.get("/api/zones", function(req, res) {
            const zones = [];
            for (let ctrllrID = 0; ctrllrID < rNet.getControllersSize(); ctrllrID++) {
                for (let zoneID = 0; zoneID < rNet.getZonesSize(ctrllrID); zoneID++) {
                    const zone = rNet.getZone(ctrllrID, zoneID);
                    if (zone) {
                        zones.push({
                            name: zone.getName(),
                            power: zone.getPower(),
                            volume: zone.getVolume(),
                            source: zone.getSourceID(),
                            muted: zone.getMuted(),
                            maxVolume: zone.getMaxVolume(),
                            bass: zone.getParameter(ExtraZoneParam.BASS),
                            treble: zone.getParameter(ExtraZoneParam.TREBLE),
                            loudness: zone.getParameter(ExtraZoneParam.LOUDNESS),
                            turnOnVolume: zone.getParameter(ExtraZoneParam.TURN_ON_VOLUME),
                            doNotDisturb: zone.getParameter(ExtraZoneParam.DO_NOT_DISTURB)
                        });
                    }
                }
            }
            res.json(zones);
        });

        this._app.get("/api/sources", function(req, res) {
            const sources = [];
            for (let sourceID = 0; sourceID < rNet.getSourcesSize(); sourceID++) {
                const source = rNet.getSource(sourceID);
                if (source) {
                    sources.push({
                        id: sourceID,
                        name: source.getName(),
                        type: source.getType()
                    });
                }
            }
            res.json(sources);
        });

        this._app.put("/mute", function(req, res) {
            rNet.setAllMute(true, 1000);
            res.sendStatus(200);
        });

        this._app.put("/unmute", function(req, res) {
            rNet.setAllMute(false, 1000);
            res.sendStatus(200);
        });

        // Zone lookup middleware function
        const findZone = function(req, res, next) {
            const zone = rNet.findZoneByName(req.params.zone);
            if (zone) {
                req.zone = zone;
                next();
            }
            else {
                console.warn("[Web Hook] Unknown zone " + req.params.zone + ".");
                res.sendStatus(404);
            }
        };

        this._app.put("/:zone/volume/:volume", findZone, function(req, res) {
            req.zone.setVolume(Math.floor(parseInt(req.params.volume) / 2) * 2);
            res.sendStatus(200);
        });

        this._app.put("/:zone/source/:source", findZone, function(req, res) {
            const source = rNet.findSourceByName(req.params.source);
            if (source !== false) {
                req.zone.setSourceID(source.getSourceID());
                res.sendStatus(200);
            }
            else {
                res.sendStatus(404);
            }
        });

        this._app.put("/:zone/mute", findZone, function(req, res) {
            req.zone.setMute(true, 1000);
            res.sendStatus(200);
        });

        this._app.put("/:zone/unmute", findZone, function(req, res) {
            req.zone.setMute(false, 1000);
            res.sendStatus(200);
        });

        this._app.put("/:zone/on", findZone, function(req, res) {
            req.zone.setPower(true);
            res.sendStatus(200);
        });

        this._app.put("/:zone/off", findZone, function(req, res) {
            req.zone.setPower(false);
            res.sendStatus(200);
        });

        this._app.put("/:zone/parameter/:paramId/:value", findZone, function(req, res) {
            const paramId = parseInt(req.params.paramId);
            let value = req.params.value;
            
            // Convert string boolean values
            if (value === 'true') value = true;
            else if (value === 'false') value = false;
            else if (!isNaN(value)) value = parseInt(value);
            
            if (req.zone.setParameter(paramId, value)) {
                res.sendStatus(200);
            } else {
                res.sendStatus(400);
            }
        });

        this._app.put("/:zone/maxvolume/:volume", findZone, function(req, res) {
            const volume = parseInt(req.params.volume);
            if (volume >= 0 && volume <= 100) {
                req.zone.setMaxVolume(volume);
                res.sendStatus(200);
            } else {
                res.sendStatus(400);
            }
        });
    }

    start() {
        if (this._app) {
            this._server = this._app.listen(this._port);
            console.info("Web hook server running on port " + this._port);
        }
    }

    stop() {
        if (this._app && this._server) {
            this._server.close();
            this._server = undefined;
            console.info("Web hook server stopped.");
        }
    }
}

module.exports = WebHookServer;

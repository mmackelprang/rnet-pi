class RNetWebUI {
    constructor() {
        this.currentZone = null;
        this.zones = [];
        this.sources = [];
        this.password = new URLSearchParams(window.location.search).get('pass') || '';
        
        if (!this.password) {
            alert('Please add ?pass=YOUR_PASSWORD to the URL');
            return;
        }
        
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadData();
        this.checkConnection();
        
        // Refresh data every 5 seconds
        setInterval(() => this.loadData(), 5000);
    }

    setupEventListeners() {
        // Global controls
        document.getElementById('allOn').addEventListener('click', () => this.globalControl('/on'));
        document.getElementById('allOff').addEventListener('click', () => this.globalControl('/off'));
        document.getElementById('allMute').addEventListener('click', () => this.globalControl('/mute'));
        document.getElementById('allUnmute').addEventListener('click', () => this.globalControl('/unmute'));

        // Modal controls
        const modal = document.getElementById('zoneModal');
        const closeBtn = document.querySelector('.close');
        
        closeBtn.addEventListener('click', () => this.closeModal());
        modal.addEventListener('click', (e) => {
            if (e.target === modal) this.closeModal();
        });

        // Zone modal controls
        document.getElementById('zonePowerOn').addEventListener('click', () => this.zoneControl('on'));
        document.getElementById('zonePowerOff').addEventListener('click', () => this.zoneControl('off'));
        document.getElementById('zoneMute').addEventListener('click', () => this.zoneControl('mute'));
        document.getElementById('zoneUnmute').addEventListener('click', () => this.zoneControl('unmute'));
        document.getElementById('loudnessToggle').addEventListener('click', () => this.toggleParameter(2));
        document.getElementById('dndToggle').addEventListener('click', () => this.toggleParameter(6));

        // Sliders
        document.getElementById('volumeSlider').addEventListener('input', (e) => {
            document.getElementById('volumeValue').textContent = e.target.value;
            this.debounce(() => this.zoneControl(`volume/${e.target.value}`), 300);
        });

        document.getElementById('bassSlider').addEventListener('input', (e) => {
            document.getElementById('bassValue').textContent = e.target.value;
            this.debounce(() => this.setParameter(0, e.target.value), 300);
        });

        document.getElementById('trebleSlider').addEventListener('input', (e) => {
            document.getElementById('trebleValue').textContent = e.target.value;
            this.debounce(() => this.setParameter(1, e.target.value), 300);
        });

        document.getElementById('turnOnVolumeSlider').addEventListener('input', (e) => {
            document.getElementById('turnOnVolumeValue').textContent = e.target.value;
            this.debounce(() => this.setParameter(4, e.target.value), 300);
        });

        document.getElementById('maxVolumeSlider').addEventListener('input', (e) => {
            document.getElementById('maxVolumeValue').textContent = e.target.value;
            this.debounce(() => this.setMaxVolume(e.target.value), 300);
        });

        // Source selection
        document.getElementById('sourceSelect').addEventListener('change', (e) => {
            this.zoneControl(`source/${encodeURIComponent(e.target.value)}`);
        });
    }

    debounce(func, wait) {
        clearTimeout(this.debounceTimeout);
        this.debounceTimeout = setTimeout(func, wait);
    }

    async checkConnection() {
        try {
            const response = await fetch(`/api/status?pass=${this.password}`);
            const statusText = document.getElementById('status-text');
            const statusBar = document.getElementById('connection-status');
            
            if (response.ok) {
                const data = await response.json();
                if (data.connected) {
                    statusText.textContent = 'Connected to RNet';
                    statusBar.className = 'status-bar connected';
                } else {
                    statusText.textContent = 'RNet not connected';
                    statusBar.className = 'status-bar error';
                }
            } else {
                statusText.textContent = 'Connection error';
                statusBar.className = 'status-bar error';
            }
        } catch (error) {
            console.error('Connection check failed:', error);
            document.getElementById('status-text').textContent = 'Connection failed';
            document.getElementById('connection-status').className = 'status-bar error';
        }
    }

    async loadData() {
        try {
            const [zonesResponse, sourcesResponse] = await Promise.all([
                fetch(`/api/zones?pass=${this.password}`),
                fetch(`/api/sources?pass=${this.password}`)
            ]);

            if (zonesResponse.ok && sourcesResponse.ok) {
                this.zones = await zonesResponse.json();
                this.sources = await sourcesResponse.json();
                this.renderZones();
                this.updateSourceSelect();
            }
        } catch (error) {
            console.error('Failed to load data:', error);
        }
    }

    renderZones() {
        const container = document.getElementById('zones-container');
        container.innerHTML = '';

        this.zones.forEach(zone => {
            const zoneCard = document.createElement('div');
            zoneCard.className = `zone-card ${zone.power ? 'powered' : ''} ${zone.muted ? 'muted' : ''}`;
            
            const sourceName = this.sources.find(s => s.id === zone.source)?.name || 'Unknown';
            
            zoneCard.innerHTML = `
                <div class="zone-header">
                    <div class="zone-name">${zone.name}</div>
                    <div class="zone-status">
                        <div class="status-indicator ${zone.power ? 'powered' : ''}"></div>
                        <div class="status-indicator ${zone.muted ? 'muted' : ''}"></div>
                    </div>
                </div>
                <div class="zone-info">
                    <div>Volume: ${zone.volume}</div>
                    <div>Source: ${sourceName}</div>
                    <div>Max Vol: ${zone.maxVolume}</div>
                    <div>Status: ${zone.power ? (zone.muted ? 'Muted' : 'On') : 'Off'}</div>
                </div>
                <div class="zone-controls">
                    <button class="btn ${zone.power ? 'btn-success' : 'btn-secondary'}" 
                            onclick="rnetUI.quickZoneControl('${zone.name}', '${zone.power ? 'off' : 'on'}')">
                        ${zone.power ? 'On' : 'Off'}
                    </button>
                    <button class="btn ${zone.muted ? 'btn-warning' : 'btn-success'}" 
                            onclick="rnetUI.quickZoneControl('${zone.name}', '${zone.muted ? 'unmute' : 'mute'}')">
                        ${zone.muted ? 'Muted' : 'Unmuted'}
                    </button>
                    <button class="btn btn-primary" onclick="rnetUI.openZoneModal('${zone.name}')">
                        Controls
                    </button>
                </div>
            `;
            
            container.appendChild(zoneCard);
        });
    }

    updateSourceSelect() {
        const select = document.getElementById('sourceSelect');
        select.innerHTML = '';
        
        this.sources.forEach(source => {
            const option = document.createElement('option');
            option.value = source.name;
            option.textContent = source.name;
            select.appendChild(option);
        });
    }

    openZoneModal(zoneName) {
        const zone = this.zones.find(z => z.name === zoneName);
        if (!zone) return;

        this.currentZone = zone;
        document.getElementById('modalTitle').textContent = `${zone.name} Controls`;
        
        // Update modal controls with current values
        document.getElementById('volumeSlider').value = zone.volume;
        document.getElementById('volumeValue').textContent = zone.volume;
        
        document.getElementById('bassSlider').value = zone.bass || 0;
        document.getElementById('bassValue').textContent = zone.bass || 0;
        
        document.getElementById('trebleSlider').value = zone.treble || 0;
        document.getElementById('trebleValue').textContent = zone.treble || 0;
        
        document.getElementById('turnOnVolumeSlider').value = zone.turnOnVolume || 0;
        document.getElementById('turnOnVolumeValue').textContent = zone.turnOnVolume || 0;
        
        document.getElementById('maxVolumeSlider').value = zone.maxVolume;
        document.getElementById('maxVolumeValue').textContent = zone.maxVolume;
        
        // Update source selection
        const sourceSelect = document.getElementById('sourceSelect');
        const currentSource = this.sources.find(s => s.id === zone.source);
        if (currentSource) {
            sourceSelect.value = currentSource.name;
        }
        
        // Update button states
        document.getElementById('loudnessToggle').textContent = zone.loudness ? 'Loudness: ON' : 'Loudness: OFF';
        document.getElementById('dndToggle').textContent = zone.doNotDisturb ? 'DND: ON' : 'DND: OFF';
        
        document.getElementById('zoneModal').style.display = 'block';
    }

    closeModal() {
        document.getElementById('zoneModal').style.display = 'none';
        this.currentZone = null;
    }

    async globalControl(action) {
        try {
            const response = await fetch(`${action}?pass=${this.password}`, {
                method: 'PUT'
            });
            
            if (response.ok) {
                // Refresh data after successful action
                setTimeout(() => this.loadData(), 500);
            } else {
                console.error('Global control failed:', response.statusText);
            }
        } catch (error) {
            console.error('Global control error:', error);
        }
    }

    async quickZoneControl(zoneName, action) {
        try {
            const response = await fetch(`/${encodeURIComponent(zoneName)}/${action}?pass=${this.password}`, {
                method: 'PUT'
            });
            
            if (response.ok) {
                setTimeout(() => this.loadData(), 500);
            } else {
                console.error('Zone control failed:', response.statusText);
            }
        } catch (error) {
            console.error('Zone control error:', error);
        }
    }

    async zoneControl(action) {
        if (!this.currentZone) return;
        
        try {
            const response = await fetch(`/${encodeURIComponent(this.currentZone.name)}/${action}?pass=${this.password}`, {
                method: 'PUT'
            });
            
            if (response.ok) {
                setTimeout(() => this.loadData(), 500);
            } else {
                console.error('Zone control failed:', response.statusText);
            }
        } catch (error) {
            console.error('Zone control error:', error);
        }
    }

    async setParameter(paramId, value) {
        if (!this.currentZone) return;
        
        try {
            const response = await fetch(`/${encodeURIComponent(this.currentZone.name)}/parameter/${paramId}/${value}?pass=${this.password}`, {
                method: 'PUT'
            });
            
            if (response.ok) {
                setTimeout(() => this.loadData(), 500);
            } else {
                console.error('Parameter set failed:', response.statusText);
            }
        } catch (error) {
            console.error('Parameter set error:', error);
        }
    }

    async toggleParameter(paramId) {
        if (!this.currentZone) return;
        
        let currentValue;
        switch (paramId) {
            case 2: // Loudness
                currentValue = this.currentZone.loudness;
                break;
            case 6: // Do Not Disturb
                currentValue = this.currentZone.doNotDisturb;
                break;
            default:
                return;
        }
        
        this.setParameter(paramId, !currentValue);
    }

    async setMaxVolume(value) {
        if (!this.currentZone) return;
        
        try {
            const response = await fetch(`/${encodeURIComponent(this.currentZone.name)}/maxvolume/${value}?pass=${this.password}`, {
                method: 'PUT'
            });
            
            if (response.ok) {
                setTimeout(() => this.loadData(), 500);
            } else {
                console.error('Max volume set failed:', response.statusText);
            }
        } catch (error) {
            console.error('Max volume set error:', error);
        }
    }
}

// Initialize the UI when the page loads
let rnetUI;
document.addEventListener('DOMContentLoaded', () => {
    rnetUI = new RNetWebUI();
});

// Handle keyboard shortcuts
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && rnetUI) {
        rnetUI.closeModal();
    }
});
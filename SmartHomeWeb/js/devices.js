/*
 * ============================================
 * SMART HOME WEB - Gestion de Dispositivos
 * ============================================
 * Modulo para cargar, mostrar y controlar
 * los dispositivos del hogar inteligente.
 * Replica exactamente la interfaz del cliente JavaFX.
 */

var Devices = {
    
    // Cache de dispositivos cargados
    deviceList: [],
    
    // Cache de habitaciones
    roomList: [],
    
    // Cargar todos los dispositivos desde el servidor
    loadAll: async function() {
        try {
            var devices = await API.get('/api/devices');
            this.deviceList = devices;
            return devices;
        } catch (error) {
            Log.add('Error cargando dispositivos: ' + error.message);
            return [];
        }
    },
    
    // Cargar habitaciones
    loadRooms: async function() {
        try {
            var data = await API.get('/api/rooms');
            this.roomList = data.rooms || [];
            return this.roomList;
        } catch (error) {
            Log.add('Error cargando habitaciones: ' + error.message);
            return [];
        }
    },
    
    // Enviar comando de control
    sendCommand: async function(deviceId, command, value) {
        try {
            var data = {
                deviceId: deviceId,
                command: command
            };
            
            if (value !== undefined) {
                data.value = String(value);
            }
            
            var response = await API.post('/api/control', data);
            
            if (response.status === 'OK') {
                Log.add('Comando enviado: ' + command + ' -> ' + deviceId);
                this.updateLocalDevice(deviceId, response);
            }
            
            return response;
            
        } catch (error) {
            Log.add('Error enviando comando: ' + error.message);
            throw error;
        }
    },
    
    // Cambiar color de un dispositivo
    setColor: async function(deviceId, hexColor) {
        try {
            var data = {
                deviceId: deviceId,
                command: 'SET_COLOR',
                value: hexColor
            };
            
            var response = await API.post('/api/control', data);
            
            if (response.status === 'OK') {
                Log.add('Color cambiado: ' + hexColor);
            }
            
            return response;
        } catch (error) {
            Log.add('Error cambiando color: ' + error.message);
            throw error;
        }
    },
    
    // Actualizar dispositivo en cache local
    updateLocalDevice: function(deviceId, response) {
        var device = this.deviceList.find(function(d) {
            return d.id === deviceId;
        });
        
        if (device) {
            if (response.newStatus !== undefined) {
                device.status = response.newStatus;
            }
            if (response.newValue !== undefined) {
                device.value = response.newValue;
            }
        }
    },
    
    // Renderizar dispositivos organizados por categoria (como en JavaFX)
    render: function(devices) {
        var grid = document.getElementById('devicesGrid');
        if (!grid) return;
        
        grid.innerHTML = '';
        
        if (!devices || devices.length === 0) {
            grid.innerHTML = '<div class="loading">No hay dispositivos disponibles</div>';
            return;
        }
        
        // Separar por tipo (igual que JavaFX)
        var climas = [];
        var luces = [];
        var speakers = [];
        var cameras = [];
        var otros = []; // Porton, TV, Lavadora
        
        var self = this;
        
        devices.forEach(function(device) {
            var type = device.type;
            if (type === 'ac') {
                climas.push(device);
            } else if (type === 'light') {
                luces.push(device);
            } else if (type === 'speaker') {
                speakers.push(device);
            } else if (type === 'camera') {
                cameras.push(device);
            } else {
                otros.push(device);
            }
        });
        
        // Seccion DISPOSITIVOS (Porton, TV, Lavadora)
        if (otros.length > 0) {
            grid.appendChild(this.createSectionHeader('DISPOSITIVOS', 'section-otros'));
            var otrosGrid = document.createElement('div');
            otrosGrid.className = 'devices-row otros-grid';
            otros.forEach(function(device) {
                otrosGrid.appendChild(self.createCompactCard(device));
            });
            grid.appendChild(otrosGrid);
        }
        
        // Seccion LUCES
        if (luces.length > 0) {
            grid.appendChild(this.createSectionHeader('LUCES', 'section-luces'));
            var lucesGrid = document.createElement('div');
            lucesGrid.className = 'devices-row luces-grid';
            luces.forEach(function(device) {
                lucesGrid.appendChild(self.createLuzCard(device));
            });
            grid.appendChild(lucesGrid);
        }
        
        // Seccion BOCINAS INTELIGENTES
        if (speakers.length > 0) {
            grid.appendChild(this.createSectionHeader('BOCINAS INTELIGENTES', 'section-speakers'));
            var speakersGrid = document.createElement('div');
            speakersGrid.className = 'devices-row speakers-grid';
            speakers.forEach(function(device) {
                speakersGrid.appendChild(self.createSpeakerCard(device));
            });
            grid.appendChild(speakersGrid);
        }
        
        // Seccion CLIMAS
        if (climas.length > 0) {
            grid.appendChild(this.createSectionHeader('CLIMAS', 'section-climas'));
            var climasGrid = document.createElement('div');
            climasGrid.className = 'devices-row climas-grid';
            climas.forEach(function(device) {
                climasGrid.appendChild(self.createClimaCard(device));
            });
            grid.appendChild(climasGrid);
        }
        
        // Seccion CAMARAS DE SEGURIDAD
        if (cameras.length > 0) {
            grid.appendChild(this.createSectionHeader('CAMARAS DE SEGURIDAD', 'section-cameras'));
            var camerasGrid = document.createElement('div');
            camerasGrid.className = 'devices-row cameras-grid';
            cameras.forEach(function(device) {
                camerasGrid.appendChild(self.createCameraCard(device));
            });
            grid.appendChild(camerasGrid);
        }
    },
    
    // Crear encabezado de seccion
    createSectionHeader: function(title, className) {
        var header = document.createElement('div');
        header.className = 'section-header ' + className;
        header.innerHTML = '<h3>' + title + '</h3>';
        return header;
    },
    
    // Tarjeta compacta para Porton, TV, Lavadora
    createCompactCard: function(device) {
        var id = device.id;
        var name = device.name;
        var type = device.type;
        var status = device.status === true || device.status === 'true';
        
        // Logica especial segun tipo
        var isDoor = (type === 'door');
        var isTV = (type === 'tv');
        
        // Para puertas: status=true significa cerrado, pero queremos mostrar abierto/cerrado correctamente
        // Para TV: status=true significa mostrada
        var displayStatus = isDoor ? !status : status;
        
        // Textos segun tipo
        var statusOn = isDoor ? 'Abierto' : (isTV ? 'Mostrada' : 'Encendido');
        var statusOff = isDoor ? 'Cerrado' : (isTV ? 'Escondida' : 'Apagado');
        
        // Para TV: boton verde = Mostrar (envia ON), boton rojo = Esconder (envia OFF)
        var btnOnLabel = isDoor ? 'Abrir' : (isTV ? 'Mostrar' : 'ON');
        var btnOffLabel = isDoor ? 'Cerrar' : (isTV ? 'Esconder' : 'OFF');
        
        var card = document.createElement('div');
        card.className = 'device-card compact-card';
        card.dataset.id = id;
        
        card.innerHTML = 
            '<div class="card-header">' +
                '<span class="device-name">' + name + '</span>' +
            '</div>' +
            '<div class="device-status ' + (displayStatus ? 'on' : 'off') + '">' +
                '<span class="status-dot"></span> ' + (displayStatus ? statusOn : statusOff) +
            '</div>' +
            '<div class="device-controls">' +
                '<button class="btn-on" data-action="' + (isDoor ? 'OFF' : 'ON') + '">' + btnOnLabel + '</button>' +
                '<button class="btn-off" data-action="' + (isDoor ? 'ON' : 'OFF') + '">' + btnOffLabel + '</button>' +
            '</div>';
        
        this.attachBasicListeners(card, device);
        return card;
    },
    
    // Tarjeta de luz con brillo y colores (igual que JavaFX)
    createLuzCard: function(device) {
        var id = device.id;
        var name = device.name;
        var room = device.room;
        var status = device.status === true || device.status === 'true';
        var value = parseInt(device.value) || 3000; // Default 3000 como en JavaFX
        var color = device.color || '#FFFFFF';
        
        // Clampear valor entre 0-6000
        value = Math.max(0, Math.min(6000, value));
        
        // Nombre corto
        var shortName = name.replace('Luz ', '');
        
        var card = document.createElement('div');
        card.className = 'device-card luz-card' + (status ? ' on' : '');
        card.dataset.id = id;
        
        card.innerHTML = 
            '<div class="card-header">' +
                '<span class="device-name">Luz ' + shortName + '</span>' +
            '</div>' +
            '<div class="device-room">' + room + '</div>' +
            '<div class="device-status ' + (status ? 'on' : 'off') + '">' +
                '<span class="status-dot"></span> ' + (status ? 'Encendida' : 'Apagada') +
                '<span class="color-preview" style="background-color: ' + color + '"></span>' +
            '</div>' +
            '<div class="device-controls">' +
                '<button class="btn-on" data-action="ON">ON</button>' +
                '<button class="btn-off" data-action="OFF">OFF</button>' +
            '</div>' +
            '<div class="slider-control brillo-control">' +
                '<span class="slider-label">Brillo:</span>' +
                '<input type="range" min="0" max="6000" value="' + value + '" class="brillo-slider">' +
                '<input type="number" min="0" max="6000" value="' + value + '" class="brillo-input">' +
            '</div>' +
            '<div class="color-buttons">' +
                '<button class="color-btn" data-color="#FFFFFF" style="background:#FFFFFF" title="Blanco"></button>' +
                '<button class="color-btn" data-color="#FFCC00" style="background:#FFCC00" title="Calido"></button>' +
                '<button class="color-btn" data-color="#FF3333" style="background:#FF3333" title="Rojo"></button>' +
                '<button class="color-btn" data-color="#00FFFF" style="background:#00FFFF" title="Cyan"></button>' +
                '<button class="color-btn" data-color="#3399FF" style="background:#3399FF" title="Azul"></button>' +
                '<button class="color-btn" data-color="#33FF33" style="background:#33FF33" title="Verde"></button>' +
                '<button class="color-btn" data-color="#FF66FF" style="background:#FF66FF" title="Rosa"></button>' +
                '<input type="color" class="color-picker" value="' + color + '" title="Color personalizado">' +
            '</div>';
        
        this.attachLuzListeners(card, device);
        return card;
    },
    
    // Tarjeta de bocina inteligente (Echo Dot)
    createSpeakerCard: function(device) {
        var id = device.id;
        var name = device.name;
        var room = device.room;
        var volume = parseInt(device.value) || 80;
        volume = Math.max(0, Math.min(100, volume));
        
        // Determinar estado de reproducción por el campo color (CMD:PLAY, CMD:PAUSE, etc.)
        var colorCmd = device.color || '';
        var isPlaying = colorCmd.indexOf('PLAY') !== -1;
        
        // Obtener tracks del dispositivo (enviados por Unity)
        var tracks = device.tracks || [];
        var trackOptions = '';
        if (tracks.length > 0) {
            for (var i = 0; i < tracks.length; i++) {
                trackOptions += '<option value="' + i + '">' + tracks[i] + '</option>';
            }
        } else {
            trackOptions = '<option value="0">Sin pistas</option>';
        }
        
        var card = document.createElement('div');
        card.className = 'device-card speaker-card';
        card.dataset.id = id;
        
        card.innerHTML = 
            '<div class="card-header">' +
                '<span class="device-name">' + name + '</span>' +
                '<span class="device-status-text ' + (isPlaying ? 'on' : '') + '">' + 
                    (isPlaying ? 'Reproduciendo' : 'Detenido') + 
                '</span>' +
            '</div>' +
            '<div class="device-room">' + room + '</div>' +
            '<div class="speaker-controls">' +
                '<button class="speaker-btn" data-cmd="PREV" title="Anterior">&#9198;</button>' +
                '<button class="speaker-btn play-btn ' + (isPlaying ? 'playing' : '') + '" data-cmd="TOGGLE" title="' + (isPlaying ? 'Pausar' : 'Reproducir') + '">' + 
                    (isPlaying ? '&#9208;' : '&#9654;') + 
                '</button>' +
                '<button class="speaker-btn" data-cmd="NEXT" title="Siguiente">&#9197;</button>' +
                '<button class="speaker-btn stop-btn" data-cmd="STOP" title="Detener">&#9209;</button>' +
            '</div>' +
            '<div class="slider-control volume-control">' +
                '<span class="slider-label">Vol:</span>' +
                '<input type="range" min="0" max="100" value="' + volume + '" class="volume-slider">' +
                '<span class="volume-value">' + volume + '%</span>' +
            '</div>' +
            '<div class="track-selector">' +
                '<span class="slider-label">Pista:</span>' +
                '<select class="track-select">' +
                    trackOptions +
                '</select>' +
            '</div>';
        
        this.attachSpeakerListeners(card, device);
        return card;
    },
    
    // Tarjeta de clima
    createClimaCard: function(device) {
        var id = device.id;
        var name = device.name;
        var status = device.status === true || device.status === 'true';
        
        var shortName = name.replace('Clima ', '');
        
        var card = document.createElement('div');
        card.className = 'device-card clima-card';
        card.dataset.id = id;
        
        card.innerHTML = 
            '<div class="card-header">' +
                '<span class="device-name">Clima ' + shortName + '</span>' +
            '</div>' +
            '<div class="device-status ' + (status ? 'on' : 'off') + '">' +
                '<span class="status-dot"></span> ' + (status ? 'Encendido' : 'Apagado') +
            '</div>' +
            '<div class="device-controls">' +
                '<button class="btn-on" data-action="ON">ON</button>' +
                '<button class="btn-off" data-action="OFF">OFF</button>' +
            '</div>';
        
        this.attachBasicListeners(card, device);
        return card;
    },
    
    // Tarjeta de camara con streaming en vivo
    createCameraCard: function(device) {
        var id = device.id;
        var name = device.name;
        var room = device.room;
        var status = device.status === true || device.status === 'true';
        var lightValue = parseInt(device.value) || 0;
        var lightOn = lightValue > 0;
        
        // Generar cameraId del nombre
        var cameraId = name.toLowerCase()
            .replace('camara ', 'cam_')
            .replace('cámara ', 'cam_')
            .replace(/[áàä]/g, 'a')
            .replace(/[éèë]/g, 'e')
            .replace(/[íìï]/g, 'i')
            .replace(/[óòö]/g, 'o')
            .replace(/[úùü]/g, 'u')
            .replace(/ /g, '_');
        
        var card = document.createElement('div');
        card.className = 'device-card camera-card';
        card.dataset.id = id;
        card.dataset.cameraId = cameraId;
        
        card.innerHTML = 
            '<div class="card-header">' +
                '<span class="device-name">' + name + '</span>' +
                '<span class="status-indicator ' + (status ? 'on' : 'off') + '"></span>' +
            '</div>' +
            '<div class="device-room">' + room + '</div>' +
            '<div class="camera-feed">' +
                '<div class="camera-placeholder">Click Play para iniciar</div>' +
                '<img class="camera-stream" style="display:none;" alt="' + name + '">' +
            '</div>' +
            '<div class="camera-video-controls">' +
                '<button class="camera-btn play-btn" title="Iniciar/Pausar">&#9654;</button>' +
                '<button class="camera-btn fullscreen-btn" title="Pantalla completa">&#9974;</button>' +
                '<span class="fps-label">-- FPS</span>' +
            '</div>' +
            '<div class="camera-device-controls">' +
                '<button class="camera-toggle-btn ' + (status ? 'on' : '') + '">' + 
                    (status ? 'CAM OFF' : 'CAM ON') + 
                '</button>' +
                '<button class="light-toggle-btn ' + (lightOn ? 'on' : '') + '">' + 
                    (lightOn ? 'LUZ OFF' : 'LUZ ON') + 
                '</button>' +
            '</div>';
        
        this.attachCameraListeners(card, device, cameraId);
        return card;
    },
    
    // Event listeners basicos (ON/OFF)
    attachBasicListeners: function(card, device) {
        var self = this;
        var id = device.id;
        
        card.querySelectorAll('button[data-action]').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var action = this.dataset.action;
                self.sendCommand(id, action)
                    .then(function() {
                        self.refreshCard(card, id);
                    })
                    .catch(function(err) {
                        console.error('Error:', err);
                    });
            });
        });
    },
    
    // Event listeners para luces
    attachLuzListeners: function(card, device) {
        var self = this;
        var id = device.id;
        
        // Botones ON/OFF
        this.attachBasicListeners(card, device);
        
        // Slider de brillo
        var slider = card.querySelector('.brillo-slider');
        var input = card.querySelector('.brillo-input');
        
        slider.addEventListener('input', function() {
            input.value = this.value;
        });
        
        slider.addEventListener('change', function() {
            var val = this.value;
            self.sendCommand(id, 'SET_VALUE', val);
            Log.add('Intensidad: ' + val);
        });
        
        input.addEventListener('change', function() {
            var val = Math.max(0, Math.min(6000, parseInt(this.value) || 0));
            this.value = val;
            slider.value = val;
            self.sendCommand(id, 'SET_VALUE', val);
            Log.add('Intensidad: ' + val);
        });
        
        // Botones de color
        var colorPreview = card.querySelector('.color-preview');
        
        card.querySelectorAll('.color-btn').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var color = this.dataset.color;
                self.setColor(id, color);
                if (colorPreview) {
                    colorPreview.style.backgroundColor = color;
                }
            });
        });
        
        // Selector de color personalizado
        var colorPicker = card.querySelector('.color-picker');
        if (colorPicker) {
            colorPicker.addEventListener('change', function() {
                var color = this.value.toUpperCase();
                self.setColor(id, color);
                if (colorPreview) {
                    colorPreview.style.backgroundColor = color;
                }
            });
        }
    },
    
    // Event listeners para speakers
    attachSpeakerListeners: function(card, device) {
        var self = this;
        var id = device.id;
        
        // Botones de control
        card.querySelectorAll('.speaker-btn').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var cmd = this.dataset.cmd;
                var statusText = card.querySelector('.device-status-text');
                
                if (cmd === 'TOGGLE') {
                    var isPlaying = this.classList.contains('playing');
                    // Enviar PLAY o PAUSE como comando de speaker
                    self.sendSpeakerCommand(id, isPlaying ? 'PAUSE' : 'PLAY');
                    // Actualizar boton localmente
                    this.classList.toggle('playing');
                    var nowPlaying = this.classList.contains('playing');
                    this.innerHTML = nowPlaying ? '&#9208;' : '&#9654;';
                    this.title = nowPlaying ? 'Pausar' : 'Reproducir';
                    // Actualizar texto de estado
                    if (statusText) {
                        statusText.textContent = nowPlaying ? 'Reproduciendo' : 'Detenido';
                        statusText.classList.toggle('on', nowPlaying);
                    }
                } else if (cmd === 'STOP') {
                    self.sendSpeakerCommand(id, 'STOP');
                    // Resetear boton play
                    var playBtn = card.querySelector('.play-btn');
                    if (playBtn) {
                        playBtn.classList.remove('playing');
                        playBtn.innerHTML = '&#9654;';
                        playBtn.title = 'Reproducir';
                    }
                    // Actualizar texto de estado
                    if (statusText) {
                        statusText.textContent = 'Detenido';
                        statusText.classList.remove('on');
                    }
                } else {
                    self.sendSpeakerCommand(id, cmd);
                }
            });
        });
        
        // Slider de volumen
        var volumeSlider = card.querySelector('.volume-slider');
        var volumeValue = card.querySelector('.volume-value');
        
        volumeSlider.addEventListener('input', function() {
            volumeValue.textContent = this.value + '%';
        });
        
        volumeSlider.addEventListener('change', function() {
            self.sendCommand(id, 'SET_VALUE', this.value);
            Log.add('Volumen: ' + this.value + '%');
        });
        
        // Selector de pista
        var trackSelect = card.querySelector('.track-select');
        trackSelect.addEventListener('change', function() {
            self.sendSpeakerCommand(id, this.value);
            Log.add('Pista: ' + (parseInt(this.value) + 1));
        });
    },
    
    // Enviar comando especial de speaker
    sendSpeakerCommand: async function(deviceId, command) {
        try {
            var data = {
                deviceId: deviceId,
                command: 'SPEAKER_CMD',
                value: command
            };
            
            await API.post('/api/control', data);
            Log.add('Comando speaker: ' + command);
        } catch (error) {
            Log.add('Error comando speaker: ' + error.message);
        }
    },
    
    // Event listeners para camaras
    attachCameraListeners: function(card, device, cameraId) {
        var self = this;
        var id = device.id;
        var isStreaming = false;
        var streamInterval = null;
        var frameCount = 0;
        var lastFpsTime = Date.now();
        
        var placeholder = card.querySelector('.camera-placeholder');
        var streamImg = card.querySelector('.camera-stream');
        var playBtn = card.querySelector('.play-btn');
        var fpsLabel = card.querySelector('.fps-label');
        var fullscreenBtn = card.querySelector('.fullscreen-btn');
        var camToggleBtn = card.querySelector('.camera-toggle-btn');
        var lightToggleBtn = card.querySelector('.light-toggle-btn');
        
        // URL del stream
        var frameUrl = CONFIG.getCameraUrl() + '/camera/frame?id=' + cameraId;
        
        // Play/Pause stream
        playBtn.addEventListener('click', function() {
            if (!isStreaming) {
                // Iniciar stream
                isStreaming = true;
                playBtn.innerHTML = '&#9208;'; // Pause icon
                playBtn.classList.add('streaming');
                placeholder.style.display = 'none';
                streamImg.style.display = 'block';
                
                frameCount = 0;
                lastFpsTime = Date.now();
                
                // Cargar frames
                streamInterval = setInterval(function() {
                    streamImg.src = frameUrl + '&t=' + Date.now();
                    frameCount++;
                    
                    var now = Date.now();
                    if (now - lastFpsTime >= 1000) {
                        fpsLabel.textContent = frameCount + ' FPS';
                        frameCount = 0;
                        lastFpsTime = now;
                    }
                }, 50); // ~20 FPS
                
            } else {
                // Detener stream
                isStreaming = false;
                playBtn.innerHTML = '&#9654;'; // Play icon
                playBtn.classList.remove('streaming');
                
                if (streamInterval) {
                    clearInterval(streamInterval);
                    streamInterval = null;
                }
                
                streamImg.style.display = 'none';
                streamImg.src = '';
                placeholder.style.display = 'block';
                fpsLabel.textContent = '-- FPS';
            }
        });
        
        // Pantalla completa
        fullscreenBtn.addEventListener('click', function() {
            self.openFullscreen(device.name, cameraId);
        });
        
        // Toggle camara ON/OFF
        camToggleBtn.addEventListener('click', function() {
            var isOn = this.classList.contains('on');
            self.sendCommand(id, isOn ? 'OFF' : 'ON')
                .then(function() {
                    camToggleBtn.classList.toggle('on');
                    camToggleBtn.textContent = isOn ? 'CAM ON' : 'CAM OFF';
                    card.querySelector('.status-indicator').className = 
                        'status-indicator ' + (isOn ? 'off' : 'on');
                });
        });
        
        // Toggle luz IR
        lightToggleBtn.addEventListener('click', function() {
            var isOn = this.classList.contains('on');
            self.sendCommand(id, 'SET_VALUE', isOn ? '0' : '1')
                .then(function() {
                    lightToggleBtn.classList.toggle('on');
                    lightToggleBtn.textContent = isOn ? 'LUZ ON' : 'LUZ OFF';
                });
        });
    },
    
    // Abrir camara en pantalla completa
    openFullscreen: function(name, cameraId) {
        var modal = document.getElementById('fullscreenModal');
        var img = document.getElementById('fullscreenImage');
        var nameEl = document.getElementById('fullscreenCameraName');
        
        if (!modal || !img) return;
        
        var streamUrl = CONFIG.getCameraUrl() + '/camera/stream?id=' + cameraId;
        img.src = streamUrl;
        
        if (nameEl) {
            nameEl.textContent = name;
        }
        
        modal.classList.remove('hidden');
    },
    
    // Actualizar UI de una tarjeta
    refreshCard: function(card, deviceId) {
        var device = this.deviceList.find(function(d) {
            return d.id === deviceId;
        });
        
        if (!device) return;
        
        var statusEl = card.querySelector('.device-status');
        if (statusEl) {
            var isOn = device.status === true || device.status === 'true';
            statusEl.className = 'device-status ' + (isOn ? 'on' : 'off');
            
            // Actualizar texto segun tipo
            var type = device.type;
            var statusText = isOn ? 'Encendido' : 'Apagado';
            if (type === 'door') {
                statusText = isOn ? 'Cerrado' : 'Abierto';
            } else if (type === 'tv') {
                statusText = isOn ? 'Mostrada' : 'Escondida';
            } else if (type === 'light') {
                statusText = isOn ? 'Encendida' : 'Apagada';
            }
            
            statusEl.innerHTML = '<span class="status-dot"></span> ' + statusText;
        }
        
        // Actualizar clase de tarjeta
        if (device.status) {
            card.classList.add('on');
        } else {
            card.classList.remove('on');
        }
    },
    
    // Rellenar selector de habitaciones
    populateRoomFilter: function() {
        var select = document.getElementById('roomFilter');
        if (!select) return;
        
        select.innerHTML = '<option value="">Todas</option>';
        
        this.roomList.forEach(function(room) {
            var option = document.createElement('option');
            option.value = room;
            option.textContent = room;
            select.appendChild(option);
        });
    }
};

// Objeto Log para mensajes
var Log = {
    maxEntries: 50,
    
    add: function(message) {
        var logContent = document.getElementById('logContent');
        if (!logContent) {
            console.log('[LOG]', message);
            return;
        }
        
        var entry = document.createElement('div');
        entry.className = 'log-entry';
        
        var time = new Date().toLocaleTimeString();
        entry.innerHTML = '<span class="time">' + time + '</span>' + message;
        
        logContent.insertBefore(entry, logContent.firstChild);
        
        while (logContent.children.length > this.maxEntries) {
            logContent.removeChild(logContent.lastChild);
        }
    },
    
    clear: function() {
        var logContent = document.getElementById('logContent');
        if (logContent) {
            logContent.innerHTML = '';
        }
    }
};

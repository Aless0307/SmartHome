/*
 * ============================================
 * SMART HOME WEB - Visor de Camaras
 * ============================================
 * Modulo para mostrar feeds en vivo de las
 * camaras de seguridad usando streaming MJPEG.
 */

const Cameras = {
    
    // Lista de camaras detectadas
    cameraList: [],
    
    // Streams activos (imagenes en actualizacion)
    activeStreams: {},
    
    // Camara en pantalla completa
    fullscreenCamera: null,
    
    /*
     * Detecta las camaras disponibles desde el servidor
     * @returns {Promise<Array>} - Lista de camaras
     */
    detectCameras: async function() {
        try {
            const url = CONFIG.getCameraUrl() + '/camera/list';
            const response = await fetch(url);
            
            if (!response.ok) {
                throw new Error('Error detectando camaras');
            }
            
            const data = await response.json();
            this.cameraList = data.cameras || [];
            
            console.log('Camaras detectadas:', this.cameraList.length);
            return this.cameraList;
            
        } catch (error) {
            console.error('Error detectando camaras:', error.message);
            
            // Si falla, intentar obtener camaras de la lista de dispositivos
            return this.getCamerasFromDevices();
        }
    },
    
    /*
     * Obtiene las camaras de la lista de dispositivos
     * como alternativa si el endpoint de camaras falla
     * @returns {Array} - Lista de camaras
     */
    getCamerasFromDevices: async function() {
        try {
            const devices = await API.get('/api/devices?type=camera');
            
            this.cameraList = devices.map(function(d) {
                return {
                    id: d.id,
                    name: d.name,
                    room: d.room,
                    status: d.status
                };
            });
            
            return this.cameraList;
            
        } catch (error) {
            console.error('Error obteniendo camaras:', error);
            return [];
        }
    },
    
    /*
     * Renderiza el grid de camaras en el DOM
     */
    render: function() {
        const grid = document.getElementById('camerasGrid');
        if (!grid) return;
        
        grid.innerHTML = '';
        
        if (this.cameraList.length === 0) {
            grid.innerHTML = '<div class="loading">No se encontraron camaras</div>';
            return;
        }
        
        const self = this;
        
        this.cameraList.forEach(function(camera) {
            const card = self.createCameraCard(camera);
            grid.appendChild(card);
        });
    },
    
    /*
     * Crea el elemento HTML de una tarjeta de camara
     * @param {object} camera - Datos de la camara
     * @returns {HTMLElement} - Elemento de la tarjeta
     */
    createCameraCard: function(camera) {
        const card = document.createElement('div');
        card.className = 'camera-card';
        card.dataset.id = camera.id;
        
        // URL del stream MJPEG
        const streamUrl = CONFIG.getCameraUrl() + '/camera/stream?id=' + camera.id;
        
        card.innerHTML = 
            '<div class="camera-feed">' +
                '<span class="status-indicator offline">OFFLINE</span>' +
                '<div class="placeholder">Camara detenida</div>' +
                '<img src="" alt="' + camera.name + '" style="display: none;">' +
            '</div>' +
            '<div class="camera-info-bar">' +
                '<div>' +
                    '<div class="camera-name">' + camera.name + '</div>' +
                    '<div class="camera-room">' + camera.room + '</div>' +
                '</div>' +
                '<div class="camera-actions">' +
                    '<button class="btn-start">Iniciar</button>' +
                    '<button class="btn-stop" style="display: none;">Detener</button>' +
                    '<button class="btn-fullscreen">Ampliar</button>' +
                '</div>' +
            '</div>';
        
        // Guardar referencia al stream URL
        card.dataset.streamUrl = streamUrl;
        
        // Agregar event listeners
        this.attachCameraListeners(card, camera);
        
        return card;
    },
    
    /*
     * Agrega event listeners a una tarjeta de camara
     * @param {HTMLElement} card - Elemento de la tarjeta
     * @param {object} camera - Datos de la camara
     */
    attachCameraListeners: function(card, camera) {
        const self = this;
        const btnStart = card.querySelector('.btn-start');
        const btnStop = card.querySelector('.btn-stop');
        const btnFullscreen = card.querySelector('.btn-fullscreen');
        
        btnStart.addEventListener('click', function() {
            self.startStream(card, camera.id);
        });
        
        btnStop.addEventListener('click', function() {
            self.stopStream(card, camera.id);
        });
        
        btnFullscreen.addEventListener('click', function() {
            self.openFullscreen(camera);
        });
    },
    
    /*
     * Inicia el stream de una camara
     * @param {HTMLElement} card - Tarjeta de la camara
     * @param {string} cameraId - ID de la camara
     */
    startStream: function(card, cameraId) {
        const img = card.querySelector('img');
        const placeholder = card.querySelector('.placeholder');
        const indicator = card.querySelector('.status-indicator');
        const btnStart = card.querySelector('.btn-start');
        const btnStop = card.querySelector('.btn-stop');
        
        // Obtener URL del stream
        const streamUrl = card.dataset.streamUrl;
        
        // Mostrar imagen y ocultar placeholder
        img.style.display = 'block';
        placeholder.style.display = 'none';
        
        // Actualizar indicador
        indicator.className = 'status-indicator live';
        indicator.textContent = 'EN VIVO';
        
        // Actualizar botones
        btnStart.style.display = 'none';
        btnStop.style.display = 'inline-block';
        
        // Iniciar stream MJPEG
        img.src = streamUrl;
        
        // Guardar referencia
        this.activeStreams[cameraId] = {
            card: card,
            img: img
        };
        
        console.log('Stream iniciado:', cameraId);
    },
    
    /*
     * Detiene el stream de una camara
     * @param {HTMLElement} card - Tarjeta de la camara
     * @param {string} cameraId - ID de la camara
     */
    stopStream: function(card, cameraId) {
        const img = card.querySelector('img');
        const placeholder = card.querySelector('.placeholder');
        const indicator = card.querySelector('.status-indicator');
        const btnStart = card.querySelector('.btn-start');
        const btnStop = card.querySelector('.btn-stop');
        
        // Detener carga de imagen
        img.src = '';
        img.style.display = 'none';
        placeholder.style.display = 'block';
        
        // Actualizar indicador
        indicator.className = 'status-indicator offline';
        indicator.textContent = 'OFFLINE';
        
        // Actualizar botones
        btnStart.style.display = 'inline-block';
        btnStop.style.display = 'none';
        
        // Eliminar referencia
        delete this.activeStreams[cameraId];
        
        console.log('Stream detenido:', cameraId);
    },
    
    /*
     * Inicia todos los streams
     */
    startAll: function() {
        const self = this;
        document.querySelectorAll('.camera-card').forEach(function(card) {
            const cameraId = card.dataset.id;
            if (!self.activeStreams[cameraId]) {
                self.startStream(card, cameraId);
            }
        });
    },
    
    /*
     * Detiene todos los streams
     */
    stopAll: function() {
        const self = this;
        document.querySelectorAll('.camera-card').forEach(function(card) {
            const cameraId = card.dataset.id;
            if (self.activeStreams[cameraId]) {
                self.stopStream(card, cameraId);
            }
        });
    },
    
    /*
     * Abre una camara en pantalla completa
     * @param {object} camera - Datos de la camara
     */
    openFullscreen: function(camera) {
        const modal = document.getElementById('fullscreenModal');
        const img = document.getElementById('fullscreenImage');
        const nameEl = document.getElementById('fullscreenCameraName');
        
        if (!modal || !img) return;
        
        // Configurar stream
        const streamUrl = CONFIG.getCameraUrl() + '/camera/stream?id=' + camera.id;
        img.src = streamUrl;
        
        // Mostrar nombre
        if (nameEl) {
            nameEl.textContent = camera.name + ' - ' + camera.room;
        }
        
        // Mostrar modal
        modal.classList.remove('hidden');
        
        this.fullscreenCamera = camera.id;
    },
    
    /*
     * Cierra la vista de pantalla completa
     */
    closeFullscreen: function() {
        const modal = document.getElementById('fullscreenModal');
        const img = document.getElementById('fullscreenImage');
        
        if (modal) {
            modal.classList.add('hidden');
        }
        
        if (img) {
            img.src = '';
        }
        
        this.fullscreenCamera = null;
    }
};

/*
 * Inicializacion de la pagina de camaras
 */
async function initCamerasPage() {
    // Verificar sesion
    if (!Auth.checkSession()) {
        return;
    }
    
    // Restaurar configuracion del servidor
    const savedConfig = localStorage.getItem('smarthome_server');
    if (savedConfig) {
        try {
            const config = JSON.parse(savedConfig);
            CONFIG.setServer(config.host, config.port);
        } catch (e) {
            console.error('Error cargando config:', e);
        }
    }
    
    // Actualizar UI con info del usuario
    Auth.updateUserUI();
    
    // Configurar eventos
    setupCameraEventListeners();
    
    // Detectar y mostrar camaras
    await Cameras.detectCameras();
    Cameras.render();
}

/*
 * Configura los event listeners de la pagina de camaras
 */
function setupCameraEventListeners() {
    // Boton detectar camaras
    const detectBtn = document.getElementById('detectCamerasBtn');
    if (detectBtn) {
        detectBtn.addEventListener('click', async function() {
            const grid = document.getElementById('camerasGrid');
            if (grid) {
                grid.innerHTML = '<div class="loading">Detectando camaras...</div>';
            }
            await Cameras.detectCameras();
            Cameras.render();
        });
    }
    
    // Boton iniciar todas
    const startAllBtn = document.getElementById('startAllBtn');
    if (startAllBtn) {
        startAllBtn.addEventListener('click', function() {
            Cameras.startAll();
        });
    }
    
    // Boton detener todas
    const stopAllBtn = document.getElementById('stopAllBtn');
    if (stopAllBtn) {
        stopAllBtn.addEventListener('click', function() {
            Cameras.stopAll();
        });
    }
    
    // Boton cerrar fullscreen
    const closeFullscreenBtn = document.getElementById('closeFullscreenBtn');
    if (closeFullscreenBtn) {
        closeFullscreenBtn.addEventListener('click', function() {
            Cameras.closeFullscreen();
        });
    }
    
    // Cerrar modal con click fuera
    const modal = document.getElementById('fullscreenModal');
    if (modal) {
        modal.addEventListener('click', function(e) {
            if (e.target === modal) {
                Cameras.closeFullscreen();
            }
        });
    }
    
    // Cerrar modal con tecla Escape
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && Cameras.fullscreenCamera) {
            Cameras.closeFullscreen();
        }
    });
}

// Inicializar cuando cargue el DOM
document.addEventListener('DOMContentLoaded', initCamerasPage);

/*
 * ============================================
 * SMART HOME WEB - Cliente WebSocket
 * ============================================
 * Conexión en tiempo real con el servidor
 * para recibir actualizaciones de dispositivos
 */

var WebSocketClient = {
    
    socket: null,
    reconnectAttempts: 0,
    maxReconnectAttempts: 5,
    reconnectDelay: 3000,
    isConnected: false,
    
    // Callbacks para eventos
    onDeviceChanged: null,
    onConnected: null,
    onDisconnected: null,
    
    /**
     * Conecta al servidor WebSocket
     */
    connect: function() {
        var self = this;
        
        // Obtener host del servidor (mismo que REST pero puerto 5002)
        var serverHost = CONFIG.SERVER_HOST || window.location.hostname || 'localhost';
        var wsUrl = 'ws://' + serverHost + ':5002';
        
        console.log('[WS] Conectando a:', wsUrl);
        
        try {
            this.socket = new WebSocket(wsUrl);
            
            this.socket.onopen = function() {
                self.isConnected = true;
                self.reconnectAttempts = 0;
                console.log('[WS] Conectado al servidor');
                Log.add('🔗 Conectado en tiempo real');
                
                if (self.onConnected) {
                    self.onConnected();
                }
                
                // Enviar ping inicial
                self.sendPing();
            };
            
            this.socket.onmessage = function(event) {
                self.handleMessage(event.data);
            };
            
            this.socket.onclose = function(event) {
                self.isConnected = false;
                console.log('[WS] Desconectado:', event.code, event.reason);
                
                if (self.onDisconnected) {
                    self.onDisconnected();
                }
                
                // Intentar reconectar
                self.attemptReconnect();
            };
            
            this.socket.onerror = function(error) {
                console.error('[WS] Error:', error);
            };
            
        } catch (e) {
            console.error('[WS] Error creando WebSocket:', e);
            this.attemptReconnect();
        }
    },
    
    /**
     * Intenta reconectar al servidor
     */
    attemptReconnect: function() {
        var self = this;
        
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.log('[WS] Máximo de intentos alcanzado, usando polling');
            Log.add('⚠️ Sin conexión en tiempo real, usando polling');
            // Activar polling como fallback
            if (typeof startAutoRefresh === 'function') {
                startAutoRefresh();
            }
            return;
        }
        
        this.reconnectAttempts++;
        console.log('[WS] Reintentando conexión (' + this.reconnectAttempts + '/' + this.maxReconnectAttempts + ')...');
        
        setTimeout(function() {
            self.connect();
        }, this.reconnectDelay);
    },
    
    /**
     * Procesa un mensaje recibido del servidor
     */
    handleMessage: function(data) {
        try {
            var message = JSON.parse(data);
            console.log('[WS] Mensaje recibido:', message.action || 'unknown');
            
            switch (message.action) {
                case 'DEVICE_CHANGED':
                    this.handleDeviceChanged(message);
                    break;
                    
                case 'PONG':
                    // Respuesta a ping, conexión OK
                    break;
                    
                case 'REGISTERED':
                    console.log('[WS] Registrado para notificaciones');
                    break;
                    
                default:
                    console.log('[WS] Acción desconocida:', message.action);
            }
            
        } catch (e) {
            console.error('[WS] Error procesando mensaje:', e);
        }
    },
    
    /**
     * Maneja actualización de dispositivo
     */
    handleDeviceChanged: function(message) {
        var deviceId = message.deviceId;
        var deviceData = message.device;
        var source = message.source || 'unknown';
        
        console.log('[WS] Dispositivo actualizado:', deviceId, 'desde:', source);
        
        // Actualizar en la lista local
        if (Devices && Devices.deviceList) {
            for (var i = 0; i < Devices.deviceList.length; i++) {
                if (Devices.deviceList[i].id === deviceId) {
                    // Parsear si viene como string
                    var updated = typeof deviceData === 'string' ? JSON.parse(deviceData) : deviceData;
                    Devices.deviceList[i] = updated;
                    break;
                }
            }
        }
        
        // Re-renderizar dispositivos
        if (typeof applyFilters === 'function') {
            applyFilters();
        }
        
        // Callback personalizado
        if (this.onDeviceChanged) {
            this.onDeviceChanged(deviceId, deviceData);
        }
        
        // Mostrar en log
        if (deviceData) {
            var name = typeof deviceData === 'string' ? JSON.parse(deviceData).name : deviceData.name;
            Log.add('📡 ' + name + ' actualizado');
        }
    },
    
    /**
     * Envía un ping al servidor
     */
    sendPing: function() {
        if (this.socket && this.socket.readyState === WebSocket.OPEN) {
            this.socket.send(JSON.stringify({ action: 'PING' }));
        }
    },
    
    /**
     * Envía un mensaje al servidor
     */
    send: function(message) {
        if (this.socket && this.socket.readyState === WebSocket.OPEN) {
            this.socket.send(JSON.stringify(message));
        }
    },
    
    /**
     * Desconecta del servidor
     */
    disconnect: function() {
        this.reconnectAttempts = this.maxReconnectAttempts; // Evitar reconexión
        if (this.socket) {
            this.socket.close();
            this.socket = null;
        }
        this.isConnected = false;
    }
};

// Indicador visual de conexión
var ConnectionIndicator = {
    
    element: null,
    
    init: function() {
        // Crear elemento indicador
        this.element = document.createElement('div');
        this.element.id = 'connectionIndicator';
        this.element.className = 'connection-indicator';
        this.element.innerHTML = '<span class="dot"></span><span class="text">Conectando...</span>';
        document.body.appendChild(this.element);
        
        this.setStatus('connecting');
    },
    
    setStatus: function(status) {
        if (!this.element) return;
        
        this.element.className = 'connection-indicator ' + status;
        var textEl = this.element.querySelector('.text');
        
        switch (status) {
            case 'connected':
                textEl.textContent = 'En línea';
                break;
            case 'disconnected':
                textEl.textContent = 'Desconectado';
                break;
            case 'connecting':
                textEl.textContent = 'Conectando...';
                break;
        }
    }
};

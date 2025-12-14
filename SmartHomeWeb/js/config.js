/*
 * ============================================
 * SMART HOME WEB - Configuracion
 * ============================================
 * Variables globales de configuracion
 * para la conexion con el servidor.
 */

const CONFIG = {
    // URL base del servidor REST
    // Ahora usa el mismo host que sirve la página (nginx reverse proxy)
    serverHost: window.location.hostname || 'localhost',
    serverPort: window.location.port || 80,
    
    // Puerto del servidor de streaming de camaras (ahora via nginx en /camera/)
    cameraStreamPort: window.location.port || 80,
    
    // Tiempo de espera para peticiones (ms)
    requestTimeout: 10000,
    
    // Intervalo de actualizacion automatica (ms)
    // 0 = desactivado
    autoRefreshInterval: 0,
    
    // Obtener la URL base de la API (ahora relativa, nginx hace proxy a /api/)
    getApiUrl: function() {
        const protocol = window.location.protocol;
        const host = window.location.host;
        return protocol + '//' + host;
    },
    
    // Obtener la URL del servidor de camaras (ahora via /camera/)
    getCameraUrl: function() {
        const protocol = window.location.protocol;
        const host = window.location.host;
        return protocol + '//' + host + '/camera';
    },
    
    // Actualizar configuracion del servidor
    setServer: function(host, port) {
        this.serverHost = host || window.location.hostname || 'localhost';
        this.serverPort = parseInt(port) || window.location.port || 80;
    }
};

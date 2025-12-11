/*
 * ============================================
 * SMART HOME WEB - Configuracion
 * ============================================
 * Variables globales de configuracion
 * para la conexion con el servidor.
 */

const CONFIG = {
    // URL base del servidor REST
    // Se actualiza al hacer login con los datos del formulario
    serverHost: 'localhost',
    serverPort: 8080,
    
    // Puerto del servidor de streaming de camaras
    cameraStreamPort: 8081,
    
    // Tiempo de espera para peticiones (ms)
    requestTimeout: 10000,
    
    // Intervalo de actualizacion automatica (ms)
    // 0 = desactivado
    autoRefreshInterval: 0,
    
    // Obtener la URL base de la API
    getApiUrl: function() {
        return 'http://' + this.serverHost + ':' + this.serverPort;
    },
    
    // Obtener la URL del servidor de camaras
    getCameraUrl: function() {
        return 'http://' + this.serverHost + ':' + this.cameraStreamPort;
    },
    
    // Actualizar configuracion del servidor
    setServer: function(host, port) {
        this.serverHost = host || 'localhost';
        this.serverPort = parseInt(port) || 8080;
    }
};

/*
 * ============================================
 * SMART HOME WEB - Cliente API REST
 * ============================================
 * Modulo para comunicacion con el servidor
 * mediante peticiones HTTP REST.
 */

const API = {
    
    /*
     * Realiza una peticion GET al servidor
     * @param {string} endpoint - Ruta del endpoint (ej: /api/devices)
     * @returns {Promise} - Promesa con la respuesta JSON
     */
    get: async function(endpoint) {
        const url = CONFIG.getApiUrl() + endpoint;
        
        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: this.getHeaders()
            });
            
            if (!response.ok) {
                throw new Error('Error ' + response.status + ': ' + response.statusText);
            }
            
            return await response.json();
            
        } catch (error) {
            console.error('Error en GET ' + endpoint + ':', error.message);
            throw error;
        }
    },
    
    /*
     * Realiza una peticion POST al servidor
     * @param {string} endpoint - Ruta del endpoint
     * @param {object} data - Datos a enviar en el body
     * @returns {Promise} - Promesa con la respuesta JSON
     */
    post: async function(endpoint, data) {
        const url = CONFIG.getApiUrl() + endpoint;
        
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: this.getHeaders(),
                body: JSON.stringify(data)
            });
            
            if (!response.ok) {
                // Intentar obtener mensaje de error del servidor
                const errorData = await response.json().catch(function() { 
                    return { error: response.statusText }; 
                });
                throw new Error(errorData.error || 'Error ' + response.status);
            }
            
            return await response.json();
            
        } catch (error) {
            console.error('Error en POST ' + endpoint + ':', error.message);
            throw error;
        }
    },
    
    /*
     * Realiza una peticion PUT al servidor
     * @param {string} endpoint - Ruta del endpoint
     * @param {object} data - Datos a enviar en el body
     * @returns {Promise} - Promesa con la respuesta JSON
     */
    put: async function(endpoint, data) {
        const url = CONFIG.getApiUrl() + endpoint;
        
        try {
            const response = await fetch(url, {
                method: 'PUT',
                headers: this.getHeaders(),
                body: JSON.stringify(data)
            });
            
            if (!response.ok) {
                const errorData = await response.json().catch(function() { 
                    return { error: response.statusText }; 
                });
                throw new Error(errorData.error || 'Error ' + response.status);
            }
            
            return await response.json();
            
        } catch (error) {
            console.error('Error en PUT ' + endpoint + ':', error.message);
            throw error;
        }
    },
    
    /*
     * Realiza una peticion DELETE al servidor
     * @param {string} endpoint - Ruta del endpoint
     * @returns {Promise} - Promesa con la respuesta JSON
     */
    delete: async function(endpoint) {
        const url = CONFIG.getApiUrl() + endpoint;
        
        try {
            const response = await fetch(url, {
                method: 'DELETE',
                headers: this.getHeaders()
            });
            
            if (!response.ok) {
                const errorData = await response.json().catch(function() { 
                    return { error: response.statusText }; 
                });
                throw new Error(errorData.error || 'Error ' + response.status);
            }
            
            return await response.json();
            
        } catch (error) {
            console.error('Error en DELETE ' + endpoint + ':', error.message);
            throw error;
        }
    },
    
    /*
     * Construye los headers para las peticiones
     * Incluye el token JWT si existe
     * @returns {object} - Headers de la peticion
     */
    getHeaders: function() {
        const headers = {
            'Content-Type': 'application/json'
        };
        
        // Agregar token de autenticacion si existe
        const token = Auth.getToken();
        if (token) {
            headers['Authorization'] = 'Bearer ' + token;
        }
        
        return headers;
    }
};

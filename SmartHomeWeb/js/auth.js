/*
 * ============================================
 * SMART HOME WEB - Autenticacion
 * ============================================
 * Modulo para manejar el login, logout y
 * la sesion del usuario con JWT.
 */

const Auth = {
    
    // Claves para localStorage
    TOKEN_KEY: 'smarthome_token',
    USER_KEY: 'smarthome_user',
    
    /*
     * Intenta iniciar sesion con las credenciales
     * @param {string} username - Nombre de usuario
     * @param {string} password - Contrasena
     * @returns {Promise} - Promesa con el resultado
     */
    login: async function(username, password) {
        try {
            const response = await API.post('/api/login', {
                username: username,
                password: password
            });
            
            // Si el login fue exitoso, guardar datos de sesion
            if (response.status === 'OK' && response.token) {
                this.saveSession(response.token, {
                    username: response.username,
                    role: response.role
                });
                return { success: true, user: response };
            } else {
                return { success: false, error: response.error || 'Error de autenticacion' };
            }
            
        } catch (error) {
            return { success: false, error: error.message };
        }
    },
    
    /*
     * Cierra la sesion del usuario
     */
    logout: function() {
        localStorage.removeItem(this.TOKEN_KEY);
        localStorage.removeItem(this.USER_KEY);
        
        // Redirigir al login
        window.location.href = 'index.html';
    },
    
    /*
     * Guarda los datos de la sesion en localStorage
     * @param {string} token - Token JWT
     * @param {object} user - Datos del usuario
     */
    saveSession: function(token, user) {
        localStorage.setItem(this.TOKEN_KEY, token);
        localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    },
    
    /*
     * Obtiene el token JWT guardado
     * @returns {string|null} - Token o null si no existe
     */
    getToken: function() {
        return localStorage.getItem(this.TOKEN_KEY);
    },
    
    /*
     * Obtiene los datos del usuario guardado
     * @returns {object|null} - Usuario o null si no existe
     */
    getUser: function() {
        const userJson = localStorage.getItem(this.USER_KEY);
        if (userJson) {
            try {
                return JSON.parse(userJson);
            } catch (e) {
                return null;
            }
        }
        return null;
    },
    
    /*
     * Verifica si hay una sesion activa
     * @returns {boolean} - true si hay sesion
     */
    isLoggedIn: function() {
        return this.getToken() !== null;
    },
    
    /*
     * Verifica la sesion y redirige si es necesario
     * Llamar al cargar paginas protegidas
     */
    checkSession: function() {
        if (!this.isLoggedIn()) {
            window.location.href = 'index.html';
            return false;
        }
        return true;
    },
    
    /*
     * Actualiza la UI con la info del usuario
     */
    updateUserUI: function() {
        const user = this.getUser();
        const userInfoEl = document.getElementById('userInfo');
        const logoutBtn = document.getElementById('logoutBtn');
        
        if (userInfoEl && user) {
            userInfoEl.textContent = user.username + ' (' + user.role + ')';
        }
        
        if (logoutBtn) {
            logoutBtn.addEventListener('click', function() {
                Auth.logout();
            });
        }
    }
};

/*
 * Inicializacion para la pagina de login
 */
function initLoginPage() {
    const loginForm = document.getElementById('loginForm');
    const statusEl = document.getElementById('loginStatus');
    
    if (!loginForm) return;
    
    // Si ya hay sesion, redirigir al dashboard
    if (Auth.isLoggedIn()) {
        window.location.href = 'dashboard.html';
        return;
    }
    
    loginForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        
        // Obtener valores del formulario
        const host = document.getElementById('serverHost').value.trim();
        const port = document.getElementById('serverPort').value.trim();
        const username = document.getElementById('username').value.trim();
        const password = document.getElementById('password').value;
        
        // Actualizar configuracion del servidor
        CONFIG.setServer(host, port);
        
        // Guardar config del servidor para otras paginas
        localStorage.setItem('smarthome_server', JSON.stringify({
            host: host,
            port: port
        }));
        
        // Mostrar estado de carga
        statusEl.textContent = 'Conectando...';
        statusEl.className = 'status-message loading';
        
        const loginBtn = document.getElementById('loginBtn');
        loginBtn.disabled = true;
        
        // Intentar login
        const result = await Auth.login(username, password);
        
        if (result.success) {
            statusEl.textContent = 'Conexion exitosa';
            statusEl.className = 'status-message success';
            
            // Redirigir al dashboard
            setTimeout(function() {
                window.location.href = 'dashboard.html';
            }, 500);
        } else {
            statusEl.textContent = result.error;
            statusEl.className = 'status-message error';
            loginBtn.disabled = false;
        }
    });
}

// Ejecutar al cargar la pagina de login
if (document.getElementById('loginForm')) {
    document.addEventListener('DOMContentLoaded', initLoginPage);
}

/*
 * ============================================
 * SMART HOME WEB - Aplicacion Principal
 * ============================================
 * Inicializacion y logica principal del
 * dashboard de control.
 */

/*
 * Funcion de inicializacion del dashboard
 * Se ejecuta cuando carga la pagina
 */
async function initDashboard() {
    // Verificar sesion
    if (!Auth.checkSession()) {
        return;
    }
    
    // Restaurar configuracion del servidor
    loadServerConfig();
    
    // Actualizar UI con info del usuario
    Auth.updateUserUI();
    
    // Configurar eventos de la interfaz
    setupEventListeners();
    
    // Cargar datos iniciales
    await loadInitialData();
    
    Log.add('Dashboard iniciado');
}

/*
 * Carga la configuracion del servidor guardada
 */
function loadServerConfig() {
    const savedConfig = localStorage.getItem('smarthome_server');
    if (savedConfig) {
        try {
            const config = JSON.parse(savedConfig);
            CONFIG.setServer(config.host, config.port);
        } catch (e) {
            console.error('Error cargando config:', e);
        }
    }
}

/*
 * Configura los event listeners de la interfaz
 */
function setupEventListeners() {
    // Boton de actualizar
    const refreshBtn = document.getElementById('refreshBtn');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', function() {
            Log.add('Actualizando dispositivos...');
            loadDevices();
        });
    }
    
    // Filtro de habitacion
    const roomFilter = document.getElementById('roomFilter');
    if (roomFilter) {
        roomFilter.addEventListener('change', applyFilters);
    }
    
    // Filtro de tipo
    const typeFilter = document.getElementById('typeFilter');
    if (typeFilter) {
        typeFilter.addEventListener('change', applyFilters);
    }
    
    // Boton limpiar log
    const clearLogBtn = document.getElementById('clearLogBtn');
    if (clearLogBtn) {
        clearLogBtn.addEventListener('click', function() {
            Log.clear();
        });
    }
}

/*
 * Carga los datos iniciales (habitaciones y dispositivos)
 */
async function loadInitialData() {
    try {
        // Cargar habitaciones primero
        await Devices.loadRooms();
        Devices.populateRoomFilter();
        
        // Luego cargar dispositivos
        await loadDevices();
        
    } catch (error) {
        Log.add('Error cargando datos: ' + error.message);
    }
}

/*
 * Carga y muestra los dispositivos
 */
async function loadDevices() {
    const grid = document.getElementById('devicesGrid');
    if (grid) {
        grid.innerHTML = '<div class="loading">Cargando dispositivos...</div>';
    }
    
    try {
        const devices = await Devices.loadAll();
        Log.add('Cargados ' + devices.length + ' dispositivos');
        
        // Aplicar filtros y renderizar
        applyFilters();
        
    } catch (error) {
        if (grid) {
            grid.innerHTML = '<div class="loading">Error al cargar dispositivos</div>';
        }
    }
}

/*
 * Aplica los filtros seleccionados y re-renderiza
 */
function applyFilters() {
    const roomFilter = document.getElementById('roomFilter');
    const typeFilter = document.getElementById('typeFilter');
    
    const selectedRoom = roomFilter ? roomFilter.value : '';
    const selectedType = typeFilter ? typeFilter.value : '';
    
    // Filtrar dispositivos
    let filtered = Devices.deviceList;
    
    if (selectedRoom) {
        filtered = filtered.filter(function(d) {
            return d.room === selectedRoom;
        });
    }
    
    if (selectedType) {
        filtered = filtered.filter(function(d) {
            return d.type === selectedType;
        });
    }
    
    // Renderizar
    Devices.render(filtered);
}

// Configurar modal de fullscreen
function setupFullscreenModal() {
    var modal = document.getElementById('fullscreenModal');
    var closeBtn = document.getElementById('closeModalBtn');
    var img = document.getElementById('fullscreenImage');
    
    if (!modal) return;
    
    // Cerrar con boton X
    if (closeBtn) {
        closeBtn.addEventListener('click', function() {
            modal.classList.add('hidden');
            if (img) img.src = '';
        });
    }
    
    // Cerrar al hacer click fuera de la imagen
    modal.addEventListener('click', function(e) {
        if (e.target === modal) {
            modal.classList.add('hidden');
            if (img) img.src = '';
        }
    });
    
    // Cerrar con tecla Escape
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && !modal.classList.contains('hidden')) {
            modal.classList.add('hidden');
            if (img) img.src = '';
        }
    });
}

// Inicializar cuando cargue el DOM
document.addEventListener('DOMContentLoaded', function() {
    initDashboard();
    setupFullscreenModal();
});

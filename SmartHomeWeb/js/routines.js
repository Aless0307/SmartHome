/*
 * ============================================
 * SMART HOME WEB - Gestion de Rutinas
 * ============================================
 * Modulo para crear, editar y gestionar
 * rutinas del hogar inteligente.
 * Ejecucion manual con controles interactivos.
 */

var Routines = {
    
    // Lista de rutinas guardadas
    routineList: [],
    
    // Rutina en edicion actual
    currentRoutine: null,
    
    // Acciones temporales para la rutina en edicion
    tempActions: [],
    
    // Clave para localStorage
    STORAGE_KEY: 'smarthome_routines',
    
    // Flag para saber si ya se crearon las predeterminadas
    DEFAULTS_KEY: 'smarthome_routines_defaults_created',
    
    /*
     * Cargar rutinas desde localStorage
     */
    loadFromStorage: function() {
        try {
            var saved = localStorage.getItem(this.STORAGE_KEY);
            if (saved) {
                this.routineList = JSON.parse(saved);
            }
        } catch (error) {
            Log.add('Error cargando rutinas: ' + error.message);
            this.routineList = [];
        }
        return this.routineList;
    },
    
    /*
     * Guardar rutinas en localStorage
     */
    saveToStorage: function() {
        try {
            localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.routineList));
        } catch (error) {
            Log.add('Error guardando rutinas: ' + error.message);
        }
    },
    
    /*
     * Crear rutinas predeterminadas basadas en los dispositivos disponibles
     */
    createDefaultRoutines: function() {
        // Solo crear si no se han creado antes
        if (localStorage.getItem(this.DEFAULTS_KEY) === 'true') {
            return;
        }
        
        // Necesitamos dispositivos cargados
        if (!Devices.deviceList || Devices.deviceList.length === 0) {
            return;
        }
        
        var self = this;
        
        // Buscar dispositivos por tipo
        var luces = Devices.deviceList.filter(function(d) { return d.type === 'light'; });
        var speakers = Devices.deviceList.filter(function(d) { return d.type === 'speaker'; });
        var tvs = Devices.deviceList.filter(function(d) { return d.type === 'tv'; });
        var doors = Devices.deviceList.filter(function(d) { return d.type === 'door'; });
        var cameras = Devices.deviceList.filter(function(d) { return d.type === 'camera'; });
        var acs = Devices.deviceList.filter(function(d) { return d.type === 'ac'; });
        
        // ==================== RUTINA: Buenos dias ====================
        var buenosDiasActions = [];
        
        // Abrir porton si existe
        if (doors.length > 0) {
            buenosDiasActions.push({
                deviceId: doors[0].id,
                deviceType: 'door',
                deviceName: doors[0].name,
                actionType: 'TURN_ON',
                value: null
            });
        }
        
        // Encender luces con brillo alto y color calido
        luces.forEach(function(luz) {
            buenosDiasActions.push({
                deviceId: luz.id,
                deviceType: 'light',
                deviceName: luz.name,
                actionType: 'TURN_ON',
                value: null
            });
            buenosDiasActions.push({
                deviceId: luz.id,
                deviceType: 'light',
                deviceName: luz.name,
                actionType: 'SET_BRIGHTNESS',
                value: '5000'
            });
            buenosDiasActions.push({
                deviceId: luz.id,
                deviceType: 'light',
                deviceName: luz.name,
                actionType: 'SET_COLOR',
                value: '#FFCC00'
            });
        });
        
        // Reproducir musica en speaker
        if (speakers.length > 0) {
            buenosDiasActions.push({
                deviceId: speakers[0].id,
                deviceType: 'speaker',
                deviceName: speakers[0].name,
                actionType: 'SET_VOLUME',
                value: '50'
            });
            buenosDiasActions.push({
                deviceId: speakers[0].id,
                deviceType: 'speaker',
                deviceName: speakers[0].name,
                actionType: 'SPEAKER_PLAY',
                value: null
            });
        }
        
        if (buenosDiasActions.length > 0) {
            this.routineList.push({
                id: this.generateId(),
                name: 'Buenos Dias',
                description: 'Abre el porton, enciende luces con color calido y reproduce musica',
                actions: buenosDiasActions,
                enabled: true,
                createdAt: new Date().toISOString(),
                isDefault: true
            });
        }
        
        // ==================== RUTINA: Buenas noches ====================
        var buenasNochesActions = [];
        
        // Apagar todas las luces
        luces.forEach(function(luz) {
            buenasNochesActions.push({
                deviceId: luz.id,
                deviceType: 'light',
                deviceName: luz.name,
                actionType: 'TURN_OFF',
                value: null
            });
        });
        
        // Cerrar porton
        if (doors.length > 0) {
            buenasNochesActions.push({
                deviceId: doors[0].id,
                deviceType: 'door',
                deviceName: doors[0].name,
                actionType: 'TURN_OFF',
                value: null
            });
        }
        
        // Detener musica
        if (speakers.length > 0) {
            buenasNochesActions.push({
                deviceId: speakers[0].id,
                deviceType: 'speaker',
                deviceName: speakers[0].name,
                actionType: 'SPEAKER_STOP',
                value: null
            });
        }
        
        // Apagar TV
        if (tvs.length > 0) {
            buenasNochesActions.push({
                deviceId: tvs[0].id,
                deviceType: 'tv',
                deviceName: tvs[0].name,
                actionType: 'TURN_OFF',
                value: null
            });
        }
        
        // Encender camaras de seguridad
        cameras.forEach(function(cam) {
            buenasNochesActions.push({
                deviceId: cam.id,
                deviceType: 'camera',
                deviceName: cam.name,
                actionType: 'TURN_ON',
                value: null
            });
        });
        
        if (buenasNochesActions.length > 0) {
            this.routineList.push({
                id: this.generateId(),
                name: 'Buenas Noches',
                description: 'Apaga luces, cierra porton, detiene musica y activa camaras',
                actions: buenasNochesActions,
                enabled: true,
                createdAt: new Date().toISOString(),
                isDefault: true
            });
        }
        
        // ==================== RUTINA: Noche de pelicula ====================
        var peliculaActions = [];
        
        // Mostrar TV
        if (tvs.length > 0) {
            peliculaActions.push({
                deviceId: tvs[0].id,
                deviceType: 'tv',
                deviceName: tvs[0].name,
                actionType: 'TURN_OFF',
                value: null
            });
        }
        
        // Luces tenues con color morado/azul
        luces.forEach(function(luz) {
            peliculaActions.push({
                deviceId: luz.id,
                deviceType: 'light',
                deviceName: luz.name,
                actionType: 'SET_BRIGHTNESS',
                value: '1000'
            });
            peliculaActions.push({
                deviceId: luz.id,
                deviceType: 'light',
                deviceName: luz.name,
                actionType: 'SET_COLOR',
                value: '#3399FF'
            });
        });
        
        // Pausar musica si esta sonando
        if (speakers.length > 0) {
            peliculaActions.push({
                deviceId: speakers[0].id,
                deviceType: 'speaker',
                deviceName: speakers[0].name,
                actionType: 'SPEAKER_PAUSE',
                value: null
            });
        }
        
        if (peliculaActions.length > 0) {
            this.routineList.push({
                id: this.generateId(),
                name: 'Noche de Pelicula',
                description: 'Muestra TV, baja luces con color azul y pausa la musica',
                actions: peliculaActions,
                enabled: true,
                createdAt: new Date().toISOString(),
                isDefault: true
            });
        }
        
        // ==================== RUTINA: Fiesta ====================
        var fiestaActions = [];
        
        // Luces de colores brillantes
        var coloresFiesta = ['#FF3333', '#33FF33', '#3399FF', '#FF66FF', '#FFCC00', '#00FFFF'];
        luces.forEach(function(luz, index) {
            var color = coloresFiesta[index % coloresFiesta.length];
            fiestaActions.push({
                deviceId: luz.id,
                deviceType: 'light',
                deviceName: luz.name,
                actionType: 'TURN_ON',
                value: null
            });
            fiestaActions.push({
                deviceId: luz.id,
                deviceType: 'light',
                deviceName: luz.name,
                actionType: 'SET_BRIGHTNESS',
                value: '6000'
            });
            fiestaActions.push({
                deviceId: luz.id,
                deviceType: 'light',
                deviceName: luz.name,
                actionType: 'SET_COLOR',
                value: color
            });
        });
        
        // Musica a todo volumen
        if (speakers.length > 0) {
            speakers.forEach(function(speaker) {
                fiestaActions.push({
                    deviceId: speaker.id,
                    deviceType: 'speaker',
                    deviceName: speaker.name,
                    actionType: 'SET_VOLUME',
                    value: '100'
                });
                fiestaActions.push({
                    deviceId: speaker.id,
                    deviceType: 'speaker',
                    deviceName: speaker.name,
                    actionType: 'SPEAKER_PLAY',
                    value: null
                });
            });
        }
        
        if (fiestaActions.length > 0) {
            this.routineList.push({
                id: this.generateId(),
                name: 'Modo Fiesta',
                description: 'Luces de colores al maximo y musica a todo volumen',
                actions: fiestaActions,
                enabled: true,
                createdAt: new Date().toISOString(),
                isDefault: true
            });
        }
        
        // ==================== RUTINA: Salir de casa ====================
        var salirActions = [];
        
        // Apagar todo
        luces.forEach(function(luz) {
            salirActions.push({
                deviceId: luz.id,
                deviceType: 'light',
                deviceName: luz.name,
                actionType: 'TURN_OFF',
                value: null
            });
        });
        
        if (speakers.length > 0) {
            speakers.forEach(function(speaker) {
                salirActions.push({
                    deviceId: speaker.id,
                    deviceType: 'speaker',
                    deviceName: speaker.name,
                    actionType: 'SPEAKER_STOP',
                    value: null
                });
            });
        }
        
        if (tvs.length > 0) {
            salirActions.push({
                deviceId: tvs[0].id,
                deviceType: 'tv',
                deviceName: tvs[0].name,
                actionType: 'TURN_ON',
                value: null
            });
        }
        
        // Apagar climas
        acs.forEach(function(ac) {
            salirActions.push({
                deviceId: ac.id,
                deviceType: 'ac',
                deviceName: ac.name,
                actionType: 'TURN_OFF',
                value: null
            });
        });
        
        // Cerrar porton
        if (doors.length > 0) {
            salirActions.push({
                deviceId: doors[0].id,
                deviceType: 'door',
                deviceName: doors[0].name,
                actionType: 'TURN_OFF',
                value: null
            });
        }
        
        // Activar camaras
        cameras.forEach(function(cam) {
            salirActions.push({
                deviceId: cam.id,
                deviceType: 'camera',
                deviceName: cam.name,
                actionType: 'TURN_ON',
                value: null
            });
        });
        
        if (salirActions.length > 0) {
            this.routineList.push({
                id: this.generateId(),
                name: 'Salir de Casa',
                description: 'Apaga todo, cierra porton y activa seguridad',
                actions: salirActions,
                enabled: true,
                createdAt: new Date().toISOString(),
                isDefault: true
            });
        }
        
        // ==================== RUTINA: Llegue a casa ====================
        var llegarActions = [];
        
        // Abrir porton
        if (doors.length > 0) {
            llegarActions.push({
                deviceId: doors[0].id,
                deviceType: 'door',
                deviceName: doors[0].name,
                actionType: 'TURN_ON',
                value: null
            });
        }
        
        // Encender luces principales con color blanco
        if (luces.length > 0) {
            llegarActions.push({
                deviceId: luces[0].id,
                deviceType: 'light',
                deviceName: luces[0].name,
                actionType: 'TURN_ON',
                value: null
            });
            llegarActions.push({
                deviceId: luces[0].id,
                deviceType: 'light',
                deviceName: luces[0].name,
                actionType: 'SET_BRIGHTNESS',
                value: '4000'
            });
            llegarActions.push({
                deviceId: luces[0].id,
                deviceType: 'light',
                deviceName: luces[0].name,
                actionType: 'SET_COLOR',
                value: '#FFFFFF'
            });
        }
        
        if (llegarActions.length > 0) {
            this.routineList.push({
                id: this.generateId(),
                name: 'Llegue a Casa',
                description: 'Abre el porton y enciende la luz principal',
                actions: llegarActions,
                enabled: true,
                createdAt: new Date().toISOString(),
                isDefault: true
            });
        }
        
        // Guardar y marcar como creadas
        this.saveToStorage();
        localStorage.setItem(this.DEFAULTS_KEY, 'true');
        
        Log.add('Rutinas predeterminadas creadas: ' + this.routineList.length);
    },
    
    /*
     * Crear una nueva rutina
     */
    create: function(data) {
        var routine = {
            id: this.generateId(),
            name: data.name,
            description: data.description || '',
            actions: data.actions,
            enabled: true,
            createdAt: new Date().toISOString()
        };
        
        this.routineList.push(routine);
        this.saveToStorage();
        Log.add('Rutina creada: ' + routine.name);
        
        return routine;
    },
    
    /*
     * Actualizar una rutina existente
     */
    update: function(id, data) {
        var index = this.routineList.findIndex(function(r) {
            return r.id === id;
        });
        
        if (index !== -1) {
            this.routineList[index] = Object.assign(this.routineList[index], data);
            this.saveToStorage();
            Log.add('Rutina actualizada: ' + data.name);
            return this.routineList[index];
        }
        
        return null;
    },
    
    /*
     * Eliminar una rutina
     */
    delete: function(id) {
        var index = this.routineList.findIndex(function(r) {
            return r.id === id;
        });
        
        if (index !== -1) {
            var removed = this.routineList.splice(index, 1)[0];
            this.saveToStorage();
            Log.add('Rutina eliminada: ' + removed.name);
            return true;
        }
        
        return false;
    },
    
    /*
     * Activar/desactivar una rutina
     */
    toggle: function(id) {
        var routine = this.routineList.find(function(r) {
            return r.id === id;
        });
        
        if (routine) {
            routine.enabled = !routine.enabled;
            this.saveToStorage();
            Log.add('Rutina ' + (routine.enabled ? 'activada' : 'desactivada') + ': ' + routine.name);
            return routine;
        }
        
        return null;
    },
    
    /*
     * Ejecutar una rutina manualmente
     */
    execute: async function(id) {
        var routine = this.routineList.find(function(r) {
            return r.id === id;
        });
        
        if (!routine) {
            Log.add('Rutina no encontrada');
            return;
        }
        
        if (!routine.enabled) {
            Log.add('La rutina esta desactivada');
            return;
        }
        
        Log.add('Ejecutando rutina: ' + routine.name);
        
        // Ejecutar cada accion secuencialmente
        for (var i = 0; i < routine.actions.length; i++) {
            var action = routine.actions[i];
            try {
                await this.executeAction(action);
                Log.add('  OK: ' + action.deviceName + ' - ' + this.formatActionText(action));
            } catch (error) {
                Log.add('  Error en ' + action.deviceName + ': ' + error.message);
            }
            // Pequena pausa entre acciones
            await this.delay(200);
        }
        
        Log.add('Rutina completada: ' + routine.name);
        
        // Actualizar dispositivos en la cache
        await Devices.loadAll();
    },
    
    /*
     * Ejecutar una accion individual
     */
    executeAction: async function(action) {
        var deviceId = action.deviceId;
        var actionType = action.actionType;
        
        switch (actionType) {
            case 'TURN_ON':
                // Para puertas y TV, el comando esta invertido
                if (action.deviceType === 'door' || action.deviceType === 'tv') {
                    await Devices.sendCommand(deviceId, 'OFF');
                } else {
                    await Devices.sendCommand(deviceId, 'ON');
                }
                break;
                
            case 'TURN_OFF':
                if (action.deviceType === 'door' || action.deviceType === 'tv') {
                    await Devices.sendCommand(deviceId, 'ON');
                } else {
                    await Devices.sendCommand(deviceId, 'OFF');
                }
                break;
                
            case 'SET_BRIGHTNESS':
                await Devices.sendCommand(deviceId, 'SET_VALUE', action.value);
                break;
                
            case 'SET_COLOR':
                await Devices.setColor(deviceId, action.value);
                break;
                
            case 'SET_VOLUME':
                await Devices.sendCommand(deviceId, 'SET_VALUE', action.value);
                break;
                
            case 'SPEAKER_PLAY':
                await Devices.sendSpeakerCommand(deviceId, 'PLAY');
                break;
                
            case 'SPEAKER_PAUSE':
                await Devices.sendSpeakerCommand(deviceId, 'PAUSE');
                break;
                
            case 'SPEAKER_STOP':
                await Devices.sendSpeakerCommand(deviceId, 'STOP');
                break;
                
            case 'SPEAKER_NEXT':
                await Devices.sendSpeakerCommand(deviceId, 'NEXT');
                break;
                
            case 'SPEAKER_PREV':
                await Devices.sendSpeakerCommand(deviceId, 'PREV');
                break;
                
            case 'CAMERA_LIGHT_ON':
                await Devices.sendCommand(deviceId, 'SET_VALUE', '1');
                break;
                
            case 'CAMERA_LIGHT_OFF':
                await Devices.sendCommand(deviceId, 'SET_VALUE', '0');
                break;
                
            default:
                throw new Error('Accion no reconocida: ' + actionType);
        }
    },
    
    /*
     * Delay helper
     */
    delay: function(ms) {
        return new Promise(function(resolve) {
            setTimeout(resolve, ms);
        });
    },
    
    /*
     * Generar ID unico
     */
    generateId: function() {
        return 'routine_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    },
    
    /*
     * Renderizar lista de rutinas
     */
    render: function() {
        var grid = document.getElementById('routinesGrid');
        if (!grid) return;
        
        grid.innerHTML = '';
        
        if (this.routineList.length === 0) {
            grid.innerHTML = '<div class="empty-state">' +
                '<div class="empty-icon">&#128197;</div>' +
                '<p>No hay rutinas configuradas</p>' +
                '<p class="text-muted">Crea tu primera rutina para automatizar tu hogar</p>' +
            '</div>';
            return;
        }
        
        var self = this;
        this.routineList.forEach(function(routine) {
            grid.appendChild(self.createRoutineCard(routine));
        });
    },
    
    /*
     * Crear tarjeta de rutina
     */
    createRoutineCard: function(routine) {
        var card = document.createElement('div');
        card.className = 'routine-card' + (routine.enabled ? '' : ' disabled');
        card.dataset.id = routine.id;
        
        // Contar acciones
        var actionsCount = routine.actions ? routine.actions.length : 0;
        
        // Generar resumen de acciones
        var actionsSummary = this.getActionsSummary(routine.actions);
        
        card.innerHTML = 
            '<div class="routine-header">' +
                '<div class="routine-info">' +
                    '<h4 class="routine-name">' + routine.name + '</h4>' +
                    '<p class="routine-description">' + (routine.description || 'Sin descripcion') + '</p>' +
                '</div>' +
                '<label class="toggle-switch">' +
                    '<input type="checkbox" ' + (routine.enabled ? 'checked' : '') + '>' +
                    '<span class="toggle-slider"></span>' +
                '</label>' +
            '</div>' +
            '<div class="routine-actions-summary">' +
                '<span class="actions-count">' + actionsCount + ' acciones</span>' +
                '<div class="actions-preview">' + actionsSummary + '</div>' +
            '</div>' +
            '<div class="routine-controls">' +
                '<button class="btn-execute" title="Ejecutar ahora">&#9654; Ejecutar</button>' +
                '<button class="btn-edit" title="Editar">&#9998; Editar</button>' +
                '<button class="btn-delete" title="Eliminar">&#128465; Eliminar</button>' +
            '</div>';
        
        // Event listeners
        var self = this;
        
        // Toggle enabled
        var toggle = card.querySelector('input[type="checkbox"]');
        toggle.addEventListener('change', function() {
            self.toggle(routine.id);
            self.render();
        });
        
        // Ejecutar
        var executeBtn = card.querySelector('.btn-execute');
        executeBtn.addEventListener('click', function() {
            executeBtn.disabled = true;
            executeBtn.innerHTML = '&#8987; Ejecutando...';
            self.execute(routine.id).then(function() {
                executeBtn.disabled = false;
                executeBtn.innerHTML = '&#9654; Ejecutar';
            });
        });
        
        // Editar
        var editBtn = card.querySelector('.btn-edit');
        editBtn.addEventListener('click', function() {
            self.openEditModal(routine);
        });
        
        // Eliminar
        var deleteBtn = card.querySelector('.btn-delete');
        deleteBtn.addEventListener('click', function() {
            if (confirm('Seguro que quieres eliminar la rutina "' + routine.name + '"?')) {
                self.delete(routine.id);
                self.render();
            }
        });
        
        return card;
    },
    
    /*
     * Obtener resumen de acciones para mostrar
     */
    getActionsSummary: function(actions) {
        if (!actions || actions.length === 0) {
            return '<span class="text-muted">Sin acciones</span>';
        }
        
        var self = this;
        var items = actions.slice(0, 3).map(function(action) {
            var icon = self.getDeviceIcon(action.deviceType);
            return '<span class="action-preview-item">' + icon + ' ' + action.deviceName + '</span>';
        });
        
        if (actions.length > 3) {
            items.push('<span class="text-muted">+' + (actions.length - 3) + ' mas</span>');
        }
        
        return items.join('');
    },
    
    /*
     * Obtener icono segun tipo de dispositivo
     */
    getDeviceIcon: function(deviceType) {
        var icons = {
            'light': '&#128161;',
            'speaker': '&#128266;',
            'tv': '&#128250;',
            'door': '&#128682;',
            'camera': '&#127909;',
            'ac': '&#10052;'
        };
        return icons[deviceType] || '&#9881;';
    },
    
    /*
     * Formatear texto de accion
     */
    formatActionText: function(action) {
        var textos = {
            'TURN_ON': 'Encender',
            'TURN_OFF': 'Apagar',
            'SET_BRIGHTNESS': 'Brillo: ' + action.value,
            'SET_COLOR': 'Color: ' + action.value,
            'SET_VOLUME': 'Volumen: ' + action.value + '%',
            'SPEAKER_PLAY': 'Reproducir',
            'SPEAKER_PAUSE': 'Pausar',
            'SPEAKER_STOP': 'Detener',
            'SPEAKER_NEXT': 'Siguiente',
            'SPEAKER_PREV': 'Anterior',
            'CAMERA_LIGHT_ON': 'Luz IR ON',
            'CAMERA_LIGHT_OFF': 'Luz IR OFF'
        };
        
        // Ajustar para door y tv
        if (action.deviceType === 'door') {
            if (action.actionType === 'TURN_ON') return 'Abrir';
            if (action.actionType === 'TURN_OFF') return 'Cerrar';
        }
        if (action.deviceType === 'tv') {
            if (action.actionType === 'TURN_ON') return 'Esconder';
            if (action.actionType === 'TURN_OFF') return 'Mostrar';
        }
        
        return textos[action.actionType] || action.actionType;
    },
    
    /*
     * Abrir modal para crear nueva rutina
     */
    openCreateModal: function() {
        this.currentRoutine = null;
        this.tempActions = [];
        
        // Resetear formulario
        var nameInput = document.getElementById('routineName');
        var descInput = document.getElementById('routineDescription');
        if (nameInput) nameInput.value = '';
        if (descInput) descInput.value = '';
        
        // Actualizar titulo
        var title = document.getElementById('modalTitle');
        if (title) title.textContent = 'Nueva Rutina';
        
        // Limpiar acciones
        this.renderTempActions();
        
        // Mostrar modal
        var modal = document.getElementById('routineModal');
        if (modal) modal.classList.remove('hidden');
    },
    
    /*
     * Abrir modal para editar rutina
     */
    openEditModal: function(routine) {
        this.currentRoutine = routine;
        this.tempActions = routine.actions ? JSON.parse(JSON.stringify(routine.actions)) : [];
        
        // Cargar datos en el formulario
        var nameInput = document.getElementById('routineName');
        var descInput = document.getElementById('routineDescription');
        
        if (nameInput) nameInput.value = routine.name;
        if (descInput) descInput.value = routine.description || '';
        
        // Actualizar titulo
        var title = document.getElementById('modalTitle');
        if (title) title.textContent = 'Editar Rutina';
        
        // Renderizar acciones
        this.renderTempActions();
        
        // Mostrar modal
        var modal = document.getElementById('routineModal');
        if (modal) modal.classList.remove('hidden');
    },
    
    /*
     * Cerrar modal de rutina
     */
    closeModal: function() {
        var modal = document.getElementById('routineModal');
        if (modal) modal.classList.add('hidden');
        this.currentRoutine = null;
        this.tempActions = [];
    },
    
    /*
     * Renderizar acciones temporales
     */
    renderTempActions: function() {
        var container = document.getElementById('actionsContainer');
        if (!container) return;
        
        container.innerHTML = '';
        
        if (this.tempActions.length === 0) {
            container.innerHTML = '<p class="text-muted">No hay acciones. Agrega dispositivos para configurar la rutina.</p>';
            return;
        }
        
        var self = this;
        this.tempActions.forEach(function(action, index) {
            var actionEl = document.createElement('div');
            actionEl.className = 'action-item';
            
            var icon = self.getDeviceIcon(action.deviceType);
            var actionText = self.formatActionText(action);
            
            actionEl.innerHTML = 
                '<div class="action-info">' +
                    '<span class="action-icon">' + icon + '</span>' +
                    '<div class="action-details">' +
                        '<span class="action-device">' + action.deviceName + '</span>' +
                        '<span class="action-command">' + actionText + '</span>' +
                    '</div>' +
                '</div>' +
                '<div class="action-buttons">' +
                    '<button type="button" class="btn-move-up" data-index="' + index + '" title="Subir">&#9650;</button>' +
                    '<button type="button" class="btn-move-down" data-index="' + index + '" title="Bajar">&#9660;</button>' +
                    '<button type="button" class="btn-remove-action" data-index="' + index + '" title="Eliminar">&times;</button>' +
                '</div>';
            container.appendChild(actionEl);
        });
        
        // Event listeners
        container.querySelectorAll('.btn-remove-action').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var index = parseInt(this.dataset.index);
                self.tempActions.splice(index, 1);
                self.renderTempActions();
            });
        });
        
        container.querySelectorAll('.btn-move-up').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var index = parseInt(this.dataset.index);
                if (index > 0) {
                    var temp = self.tempActions[index];
                    self.tempActions[index] = self.tempActions[index - 1];
                    self.tempActions[index - 1] = temp;
                    self.renderTempActions();
                }
            });
        });
        
        container.querySelectorAll('.btn-move-down').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var index = parseInt(this.dataset.index);
                if (index < self.tempActions.length - 1) {
                    var temp = self.tempActions[index];
                    self.tempActions[index] = self.tempActions[index + 1];
                    self.tempActions[index + 1] = temp;
                    self.renderTempActions();
                }
            });
        });
    },
    
    /*
     * Abrir modal para agregar accion
     */
    openActionModal: function() {
        // Limpiar seleccion previa
        var deviceSelect = document.getElementById('actionDevice');
        var controlsContainer = document.getElementById('deviceControlsContainer');
        
        if (deviceSelect) {
            deviceSelect.innerHTML = '<option value="">Seleccionar dispositivo...</option>';
            
            // Agrupar dispositivos por tipo
            var grupos = {
                'light': { label: 'Luces', devices: [] },
                'speaker': { label: 'Bocinas', devices: [] },
                'tv': { label: 'TV', devices: [] },
                'door': { label: 'Puertas', devices: [] },
                'camera': { label: 'Camaras', devices: [] },
                'ac': { label: 'Clima', devices: [] },
                'otros': { label: 'Otros', devices: [] }
            };
            
            Devices.deviceList.forEach(function(device) {
                var tipo = device.type;
                if (grupos[tipo]) {
                    grupos[tipo].devices.push(device);
                } else {
                    grupos['otros'].devices.push(device);
                }
            });
            
            // Crear optgroups
            Object.keys(grupos).forEach(function(key) {
                var grupo = grupos[key];
                if (grupo.devices.length > 0) {
                    var optgroup = document.createElement('optgroup');
                    optgroup.label = grupo.label;
                    
                    grupo.devices.forEach(function(device) {
                        var option = document.createElement('option');
                        option.value = device.id;
                        option.textContent = device.name + ' (' + device.room + ')';
                        option.dataset.type = device.type;
                        option.dataset.name = device.name;
                        optgroup.appendChild(option);
                    });
                    
                    deviceSelect.appendChild(optgroup);
                }
            });
        }
        
        // Limpiar controles
        if (controlsContainer) {
            controlsContainer.innerHTML = '<p class="text-muted">Selecciona un dispositivo para ver sus controles</p>';
        }
        
        // Mostrar modal
        var modal = document.getElementById('actionModal');
        if (modal) modal.classList.remove('hidden');
    },
    
    /*
     * Cerrar modal de accion
     */
    closeActionModal: function() {
        var modal = document.getElementById('actionModal');
        if (modal) modal.classList.add('hidden');
    },
    
    /*
     * Mostrar controles segun el dispositivo seleccionado
     */
    showDeviceControls: function() {
        var deviceSelect = document.getElementById('actionDevice');
        var controlsContainer = document.getElementById('deviceControlsContainer');
        
        if (!deviceSelect || !controlsContainer) return;
        
        var selectedOption = deviceSelect.options[deviceSelect.selectedIndex];
        if (!selectedOption || !selectedOption.value) {
            controlsContainer.innerHTML = '<p class="text-muted">Selecciona un dispositivo para ver sus controles</p>';
            return;
        }
        
        var deviceId = selectedOption.value;
        var deviceType = selectedOption.dataset.type;
        var deviceName = selectedOption.dataset.name;
        
        // Obtener dispositivo completo
        var device = Devices.deviceList.find(function(d) {
            return d.id === deviceId;
        });
        
        // Generar controles segun tipo
        var html = this.generateControlsForType(deviceType, device);
        controlsContainer.innerHTML = html;
        
        // Configurar eventos de los controles
        this.setupControlEvents(deviceId, deviceType, deviceName);
    },
    
    /*
     * Generar HTML de controles segun tipo de dispositivo
     */
    generateControlsForType: function(deviceType, device) {
        var html = '<div class="device-controls-panel">';
        
        switch (deviceType) {
            case 'light':
                var currentBrillo = parseInt(device.value) || 3000;
                var currentColor = device.color || '#FFFFFF';
                
                html += 
                    '<div class="control-section">' +
                        '<label class="control-label">Estado</label>' +
                        '<div class="control-buttons">' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_ON">Encender</button>' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_OFF">Apagar</button>' +
                        '</div>' +
                    '</div>' +
                    '<div class="control-section">' +
                        '<label class="control-label">Brillo (0-6000)</label>' +
                        '<div class="control-slider">' +
                            '<input type="range" id="ctrlBrillo" min="0" max="6000" value="' + currentBrillo + '">' +
                            '<input type="number" id="ctrlBrilloNum" min="0" max="6000" value="' + currentBrillo + '">' +
                            '<button type="button" class="ctrl-btn small" data-action="SET_BRIGHTNESS">Aplicar</button>' +
                        '</div>' +
                    '</div>' +
                    '<div class="control-section">' +
                        '<label class="control-label">Color</label>' +
                        '<div class="control-colors">' +
                            '<button type="button" class="color-btn" data-color="#FFFFFF" style="background:#FFFFFF"></button>' +
                            '<button type="button" class="color-btn" data-color="#FFCC00" style="background:#FFCC00"></button>' +
                            '<button type="button" class="color-btn" data-color="#FF3333" style="background:#FF3333"></button>' +
                            '<button type="button" class="color-btn" data-color="#00FFFF" style="background:#00FFFF"></button>' +
                            '<button type="button" class="color-btn" data-color="#3399FF" style="background:#3399FF"></button>' +
                            '<button type="button" class="color-btn" data-color="#33FF33" style="background:#33FF33"></button>' +
                            '<button type="button" class="color-btn" data-color="#FF66FF" style="background:#FF66FF"></button>' +
                            '<input type="color" id="ctrlColorPicker" value="' + currentColor + '">' +
                        '</div>' +
                    '</div>';
                break;
                
            case 'speaker':
                var currentVolume = parseInt(device.value) || 80;
                
                html += 
                    '<div class="control-section">' +
                        '<label class="control-label">Reproduccion</label>' +
                        '<div class="control-buttons">' +
                            '<button type="button" class="ctrl-btn" data-action="SPEAKER_PREV">&#9198; Anterior</button>' +
                            '<button type="button" class="ctrl-btn" data-action="SPEAKER_PLAY">&#9654; Play</button>' +
                            '<button type="button" class="ctrl-btn" data-action="SPEAKER_PAUSE">&#9208; Pausa</button>' +
                            '<button type="button" class="ctrl-btn" data-action="SPEAKER_NEXT">&#9197; Siguiente</button>' +
                            '<button type="button" class="ctrl-btn" data-action="SPEAKER_STOP">&#9209; Stop</button>' +
                        '</div>' +
                    '</div>' +
                    '<div class="control-section">' +
                        '<label class="control-label">Volumen (0-100%)</label>' +
                        '<div class="control-slider">' +
                            '<input type="range" id="ctrlVolumen" min="0" max="100" value="' + currentVolume + '">' +
                            '<span id="ctrlVolumenLabel">' + currentVolume + '%</span>' +
                            '<button type="button" class="ctrl-btn small" data-action="SET_VOLUME">Aplicar</button>' +
                        '</div>' +
                    '</div>';
                break;
                
            case 'tv':
                html += 
                    '<div class="control-section">' +
                        '<label class="control-label">TV Lift</label>' +
                        '<div class="control-buttons">' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_ON">Esconder TV</button>' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_OFF">Mostrar TV</button>' +
                        '</div>' +
                    '</div>';
                break;
                
            case 'door':
                html += 
                    '<div class="control-section">' +
                        '<label class="control-label">Porton</label>' +
                        '<div class="control-buttons">' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_ON">Abrir</button>' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_OFF">Cerrar</button>' +
                        '</div>' +
                    '</div>';
                break;
                
            case 'camera':
                html += 
                    '<div class="control-section">' +
                        '<label class="control-label">Camara</label>' +
                        '<div class="control-buttons">' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_ON">Encender</button>' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_OFF">Apagar</button>' +
                        '</div>' +
                    '</div>' +
                    '<div class="control-section">' +
                        '<label class="control-label">Luz IR</label>' +
                        '<div class="control-buttons">' +
                            '<button type="button" class="ctrl-btn" data-action="CAMERA_LIGHT_ON">Luz ON</button>' +
                            '<button type="button" class="ctrl-btn" data-action="CAMERA_LIGHT_OFF">Luz OFF</button>' +
                        '</div>' +
                    '</div>';
                break;
                
            case 'ac':
                html += 
                    '<div class="control-section">' +
                        '<label class="control-label">Clima</label>' +
                        '<div class="control-buttons">' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_ON">Encender</button>' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_OFF">Apagar</button>' +
                        '</div>' +
                    '</div>';
                break;
                
            default:
                html += 
                    '<div class="control-section">' +
                        '<label class="control-label">Controles basicos</label>' +
                        '<div class="control-buttons">' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_ON">Encender</button>' +
                            '<button type="button" class="ctrl-btn" data-action="TURN_OFF">Apagar</button>' +
                        '</div>' +
                    '</div>';
        }
        
        html += '</div>';
        return html;
    },
    
    /*
     * Configurar eventos de los controles
     */
    setupControlEvents: function(deviceId, deviceType, deviceName) {
        var self = this;
        var container = document.getElementById('deviceControlsContainer');
        if (!container) return;
        
        // Botones de accion
        container.querySelectorAll('.ctrl-btn[data-action]').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var action = this.dataset.action;
                var value = null;
                
                // Obtener valor si es necesario
                if (action === 'SET_BRIGHTNESS') {
                    value = document.getElementById('ctrlBrilloNum').value;
                } else if (action === 'SET_VOLUME') {
                    value = document.getElementById('ctrlVolumen').value;
                }
                
                self.addActionToTemp(deviceId, deviceType, deviceName, action, value);
            });
        });
        
        // Botones de color
        container.querySelectorAll('.color-btn').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var color = this.dataset.color;
                self.addActionToTemp(deviceId, deviceType, deviceName, 'SET_COLOR', color);
            });
        });
        
        // Color picker
        var colorPicker = document.getElementById('ctrlColorPicker');
        if (colorPicker) {
            colorPicker.addEventListener('change', function() {
                self.addActionToTemp(deviceId, deviceType, deviceName, 'SET_COLOR', this.value.toUpperCase());
            });
        }
        
        // Sincronizar sliders
        var brilloSlider = document.getElementById('ctrlBrillo');
        var brilloNum = document.getElementById('ctrlBrilloNum');
        if (brilloSlider && brilloNum) {
            brilloSlider.addEventListener('input', function() {
                brilloNum.value = this.value;
            });
            brilloNum.addEventListener('input', function() {
                brilloSlider.value = this.value;
            });
        }
        
        var volumenSlider = document.getElementById('ctrlVolumen');
        var volumenLabel = document.getElementById('ctrlVolumenLabel');
        if (volumenSlider && volumenLabel) {
            volumenSlider.addEventListener('input', function() {
                volumenLabel.textContent = this.value + '%';
            });
        }
    },
    
    /*
     * Agregar accion a la lista temporal
     */
    addActionToTemp: function(deviceId, deviceType, deviceName, actionType, value) {
        var action = {
            deviceId: deviceId,
            deviceType: deviceType,
            deviceName: deviceName,
            actionType: actionType,
            value: value
        };
        
        this.tempActions.push(action);
        this.renderTempActions();
        this.closeActionModal();
        
        Log.add('Accion agregada: ' + deviceName + ' - ' + this.formatActionText(action));
    },
    
    /*
     * Guardar rutina desde formulario
     */
    saveFromForm: function() {
        var nameInput = document.getElementById('routineName');
        var descInput = document.getElementById('routineDescription');
        
        if (!nameInput || !nameInput.value.trim()) {
            Log.add('El nombre es obligatorio');
            return false;
        }
        
        if (this.tempActions.length === 0) {
            Log.add('Agrega al menos una accion');
            return false;
        }
        
        var data = {
            name: nameInput.value.trim(),
            description: descInput ? descInput.value.trim() : '',
            actions: this.tempActions
        };
        
        if (this.currentRoutine) {
            this.update(this.currentRoutine.id, data);
        } else {
            this.create(data);
        }
        
        this.closeModal();
        this.render();
        
        return true;
    }
};

/*
 * ============================================
 * INICIALIZACION DE LA PAGINA DE RUTINAS
 * ============================================
 */

async function initRoutinesPage() {
    // Verificar sesion
    if (!Auth.checkSession()) {
        return;
    }
    
    // Restaurar configuracion del servidor
    loadServerConfigRoutines();
    
    // Actualizar UI con info del usuario
    Auth.updateUserUI();
    
    // Cargar dispositivos para las acciones
    try {
        await Devices.loadAll();
        Log.add('Dispositivos cargados: ' + Devices.deviceList.length);
    } catch (error) {
        Log.add('Error cargando dispositivos: ' + error.message);
    }
    
    // Cargar rutinas guardadas
    Routines.loadFromStorage();
    
    // Crear rutinas predeterminadas si no existen
    if (Routines.routineList.length === 0 && Devices.deviceList.length > 0) {
        Routines.createDefaultRoutines();
    }
    
    // Renderizar rutinas
    Routines.render();
    
    // Configurar eventos
    setupRoutineEvents();
    
    Log.add('Pagina de rutinas lista');
}

function loadServerConfigRoutines() {
    var savedConfig = localStorage.getItem('smarthome_server');
    if (savedConfig) {
        try {
            var config = JSON.parse(savedConfig);
            CONFIG.setServer(config.host, config.port);
        } catch (e) {
            console.error('Error cargando config:', e);
        }
    }
}

function setupRoutineEvents() {
    // Boton nueva rutina
    var addBtn = document.getElementById('addRoutineBtn');
    if (addBtn) {
        addBtn.addEventListener('click', function() {
            Routines.openCreateModal();
        });
    }
    
    // Cerrar modal rutina
    var closeModalBtn = document.getElementById('closeModalBtn');
    if (closeModalBtn) {
        closeModalBtn.addEventListener('click', function() {
            Routines.closeModal();
        });
    }
    
    // Cancelar rutina
    var cancelBtn = document.getElementById('cancelRoutineBtn');
    if (cancelBtn) {
        cancelBtn.addEventListener('click', function() {
            Routines.closeModal();
        });
    }
    
    // Formulario de rutina
    var routineForm = document.getElementById('routineForm');
    if (routineForm) {
        routineForm.addEventListener('submit', function(e) {
            e.preventDefault();
            Routines.saveFromForm();
        });
    }
    
    // Boton agregar accion
    var addActionBtn = document.getElementById('addActionBtn');
    if (addActionBtn) {
        addActionBtn.addEventListener('click', function() {
            Routines.openActionModal();
        });
    }
    
    // Cerrar modal accion
    var closeActionBtn = document.getElementById('closeActionModalBtn');
    if (closeActionBtn) {
        closeActionBtn.addEventListener('click', function() {
            Routines.closeActionModal();
        });
    }
    
    // Cambio de dispositivo
    var deviceSelect = document.getElementById('actionDevice');
    if (deviceSelect) {
        deviceSelect.addEventListener('change', function() {
            Routines.showDeviceControls();
        });
    }
    
    // Boton limpiar log
    var clearLogBtn = document.getElementById('clearLogBtn');
    if (clearLogBtn) {
        clearLogBtn.addEventListener('click', function() {
            Log.clear();
        });
    }
    
    // Cerrar modales con click fuera
    var routineModal = document.getElementById('routineModal');
    if (routineModal) {
        routineModal.addEventListener('click', function(e) {
            if (e.target === routineModal) {
                Routines.closeModal();
            }
        });
    }
    
    var actionModal = document.getElementById('actionModal');
    if (actionModal) {
        actionModal.addEventListener('click', function(e) {
            if (e.target === actionModal) {
                Routines.closeActionModal();
            }
        });
    }
    
    // Cerrar con Escape
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            Routines.closeModal();
            Routines.closeActionModal();
        }
    });
}

// Inicializar cuando cargue el DOM
document.addEventListener('DOMContentLoaded', function() {
    initRoutinesPage();
});

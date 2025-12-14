/**
 * energy.js - Modulo de consumo energetico para Smart Home
 * Muestra estadisticas y graficas de consumo electrico
 */

// URL base de la API (usa CONFIG de config.js)
function getApiBase() {
    return CONFIG.getApiUrl();
}

// Graficas de Chart.js
let chartByHour = null;
let chartByType = null;
let chartByDay = null;

// Intervalo de actualizacion automatica
let updateInterval = null;

// Colores para las graficas
const CHART_COLORS = {
    primary: 'rgba(74, 222, 128, 1)',      // Verde
    primaryBg: 'rgba(74, 222, 128, 0.2)',
    secondary: 'rgba(96, 165, 250, 1)',    // Azul
    secondaryBg: 'rgba(96, 165, 250, 0.2)',
    warning: 'rgba(251, 191, 36, 1)',      // Amarillo
    warningBg: 'rgba(251, 191, 36, 0.2)',
    danger: 'rgba(248, 113, 113, 1)',      // Rojo
    dangerBg: 'rgba(248, 113, 113, 0.2)',
    purple: 'rgba(167, 139, 250, 1)',
    purpleBg: 'rgba(167, 139, 250, 0.2)',
    cyan: 'rgba(34, 211, 238, 1)',
    cyanBg: 'rgba(34, 211, 238, 0.2)'
};

// Colores por tipo de dispositivo
const TYPE_COLORS = {
    'light': CHART_COLORS.warning,
    'tv': CHART_COLORS.secondary,
    'speaker': CHART_COLORS.purple,
    'camera': CHART_COLORS.cyan,
    'ac': CHART_COLORS.danger,
    'door': CHART_COLORS.primary,
    'washer': '#ff69b4'
};

/**
 * Inicializar el modulo de energia
 */
async function initEnergyModule() {
    console.log('[ENERGY] Inicializando modulo de energia...');
    
    // Verificar autenticacion usando Auth de auth.js
    if (!Auth.isLoggedIn()) {
        window.location.href = 'index.html';
        return;
    }
    
    // Configurar info de usuario usando Auth
    Auth.updateUserUI();
    
    // Configurar refresh de logs
    const refreshLogsBtn = document.getElementById('refreshLogsBtn');
    if (refreshLogsBtn) {
        refreshLogsBtn.onclick = loadEnergyLogs;
    }
    
    // Configurar cambio de limite
    const logsLimit = document.getElementById('logsLimit');
    if (logsLimit) {
        logsLimit.onchange = loadEnergyLogs;
    }
    
    // Inicializar graficas
    initCharts();
    
    // Cargar datos iniciales
    await loadAllData();
    
    // Actualizar cada 30 segundos
    updateInterval = setInterval(loadAllData, 30000);
    
    console.log('[ENERGY] Modulo inicializado');
}

/**
 * Inicializar graficas con Chart.js
 */
function initCharts() {
    // Config comun
    const commonOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                labels: {
                    color: 'rgba(255,255,255,0.8)'
                }
            }
        },
        scales: {
            y: {
                ticks: { color: 'rgba(255,255,255,0.6)' },
                grid: { color: 'rgba(255,255,255,0.1)' }
            },
            x: {
                ticks: { color: 'rgba(255,255,255,0.6)' },
                grid: { color: 'rgba(255,255,255,0.1)' }
            }
        }
    };
    
    // Grafica de consumo por hora
    const ctxHour = document.getElementById('chartByHour');
    if (ctxHour) {
        chartByHour = new Chart(ctxHour, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'kWh',
                    data: [],
                    borderColor: CHART_COLORS.primary,
                    backgroundColor: CHART_COLORS.primaryBg,
                    fill: true,
                    tension: 0.3
                }]
            },
            options: commonOptions
        });
    }
    
    // Grafica por tipo (donut)
    const ctxType = document.getElementById('chartByType');
    if (ctxType) {
        chartByType = new Chart(ctxType, {
            type: 'doughnut',
            data: {
                labels: [],
                datasets: [{
                    data: [],
                    backgroundColor: Object.values(TYPE_COLORS),
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            color: 'rgba(255,255,255,0.8)',
                            padding: 15
                        }
                    }
                }
            }
        });
    }
    
    // Grafica de consumo por dia
    const ctxDay = document.getElementById('chartByDay');
    if (ctxDay) {
        chartByDay = new Chart(ctxDay, {
            type: 'bar',
            data: {
                labels: [],
                datasets: [{
                    label: 'kWh',
                    data: [],
                    backgroundColor: CHART_COLORS.secondary,
                    borderRadius: 5
                }]
            },
            options: commonOptions
        });
    }
}

/**
 * Cargar todos los datos
 */
async function loadAllData() {
    console.log('[ENERGY] Cargando datos...');
    
    try {
        await Promise.all([
            loadSummary(),
            loadByHour(),
            loadByType(),
            loadByDay(),
            loadActiveDevices(),
            loadEnergyLogs()
        ]);
    } catch (error) {
        console.error('[ENERGY] Error cargando datos:', error);
    }
}

/**
 * Cargar resumen general
 */
async function loadSummary() {
    try {
        const response = await fetch(`${getApiBase()}/api/energy?type=summary`);
        const data = await response.json();
        
        // Actualizar tarjetas
        const currentWatts = document.getElementById('currentWatts');
        const todayKwh = document.getElementById('todayKwh');
        const estimatedCost = document.getElementById('estimatedCost');
        
        if (currentWatts) currentWatts.textContent = data.currentWatts?.toFixed(0) || '0';
        if (todayKwh) todayKwh.textContent = data.totalKwh?.toFixed(2) || '0.00';
        if (estimatedCost) estimatedCost.textContent = '$' + (data.costoEstimado?.toFixed(2) || '0.00');
        
    } catch (error) {
        console.error('[ENERGY] Error cargando resumen:', error);
    }
}

/**
 * Cargar consumo por hora
 */
async function loadByHour() {
    try {
        const response = await fetch(`${getApiBase()}/api/energy?type=byHour`);
        const data = await response.json();
        
        if (chartByHour && data.consumoByHour) {
            const labels = [];
            const values = [];
            
            for (let i = 0; i < 24; i++) {
                labels.push(i + ':00');
                values.push(parseFloat(data.consumoByHour[i]) || 0);
            }
            
            chartByHour.data.labels = labels;
            chartByHour.data.datasets[0].data = values;
            chartByHour.update();
        }
    } catch (error) {
        console.error('[ENERGY] Error cargando datos por hora:', error);
    }
}

/**
 * Cargar consumo por tipo
 */
async function loadByType() {
    try {
        const response = await fetch(`${getApiBase()}/api/energy?type=byType`);
        const data = await response.json();
        
        if (chartByType && data.consumoByType) {
            const labels = [];
            const values = [];
            const colors = [];
            
            // Mapeo de nombres de tipos para mostrar
            const typeNames = {
                'light': 'Luces',
                'tv': 'TV',
                'speaker': 'Altavoz',
                'camera': 'Camaras',
                'ac': 'Clima',
                'door': 'Porton',
                'washer': 'Lavadora'
            };
            
            for (const [type, kwh] of Object.entries(data.consumoByType)) {
                if (parseFloat(kwh) > 0) {
                    labels.push(typeNames[type] || type);
                    values.push(parseFloat(kwh));
                    colors.push(TYPE_COLORS[type] || '#888');
                }
            }
            
            chartByType.data.labels = labels;
            chartByType.data.datasets[0].data = values;
            chartByType.data.datasets[0].backgroundColor = colors;
            chartByType.update();
        }
    } catch (error) {
        console.error('[ENERGY] Error cargando datos por tipo:', error);
    }
}

/**
 * Cargar consumo por dia
 */
async function loadByDay() {
    try {
        const response = await fetch(`${getApiBase()}/api/energy?type=byDay&dias=7`);
        const data = await response.json();
        
        if (chartByDay && data.consumoByDay) {
            const labels = Object.keys(data.consumoByDay);
            const values = Object.values(data.consumoByDay).map(v => parseFloat(v));
            
            chartByDay.data.labels = labels;
            chartByDay.data.datasets[0].data = values;
            chartByDay.update();
        }
    } catch (error) {
        console.error('[ENERGY] Error cargando datos por dia:', error);
    }
}

/**
 * Cargar dispositivos activos
 */
async function loadActiveDevices() {
    try {
        const response = await fetch(`${getApiBase()}/api/energy?type=current`);
        const data = await response.json();
        
        const container = document.getElementById('activeDevicesList');
        const totalElement = document.getElementById('totalActiveWatts');
        
        if (!container) return;
        
        if (!data.dispositivosActivos || Object.keys(data.dispositivosActivos).length === 0) {
            container.innerHTML = '<div class="no-active">No hay dispositivos activos</div>';
            if (totalElement) totalElement.textContent = '0';
            return;
        }
        
        let html = '';
        for (const [deviceId, watts] of Object.entries(data.dispositivosActivos)) {
            html += `
                <div class="active-device-item">
                    <span class="device-dot" style="background: ${getColorForWatts(watts)}"></span>
                    <span class="device-name">${deviceId}</span>
                    <span class="device-watts">${watts.toFixed(0)}W</span>
                </div>
            `;
        }
        
        container.innerHTML = html;
        if (totalElement) totalElement.textContent = (data.totalWatts || 0).toFixed(0);
        
    } catch (error) {
        console.error('[ENERGY] Error cargando dispositivos activos:', error);
    }
}

/**
 * Cargar historial de eventos de energia
 */
async function loadEnergyLogs() {
    try {
        const limitSelect = document.getElementById('logsLimit');
        const limit = limitSelect ? limitSelect.value : 50;
        
        const response = await fetch(`${getApiBase()}/api/energy?type=logs&limit=${limit}`);
        const data = await response.json();
        
        const tbody = document.getElementById('logsBody');
        if (!tbody) return;
        
        if (!data.logs || data.logs.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="no-data">No hay eventos registrados</td></tr>';
            return;
        }
        
        let html = '';
        for (const log of data.logs) {
            const fecha = new Date(log.timestamp);
            const fechaStr = fecha.toLocaleDateString('es-MX', { 
                day: '2-digit', month: '2-digit', year: '2-digit',
                hour: '2-digit', minute: '2-digit'
            });
            
            // Duracion legible
            let duracionStr = '-';
            if (log.duration > 0) {
                const minutos = Math.floor(log.duration / 60000);
                const segundos = Math.floor((log.duration % 60000) / 1000);
                if (minutos > 0) {
                    duracionStr = minutos + 'm ' + segundos + 's';
                } else {
                    duracionStr = segundos + 's';
                }
            }
            
            // Clase de evento
            const eventClass = log.eventType === 'ON' ? 'event-on' : 
                              log.eventType === 'OFF' ? 'event-off' : 'event-action';
            
            html += `
                <tr>
                    <td>${fechaStr}</td>
                    <td>${log.deviceName}</td>
                    <td>${getTypeIcon(log.deviceType)} ${log.deviceType}</td>
                    <td><span class="event-badge ${eventClass}">${log.eventType}</span></td>
                    <td>${log.wattsConsumed?.toFixed(1) || 0}W</td>
                    <td>${duracionStr}</td>
                    <td>${log.kwhConsumed?.toFixed(4) || '0.0000'}</td>
                </tr>
            `;
        }
        
        tbody.innerHTML = html;
        
    } catch (error) {
        console.error('[ENERGY] Error cargando logs:', error);
    }
}

/**
 * Obtener color segun watts
 */
function getColorForWatts(watts) {
    if (watts < 50) return CHART_COLORS.primary;
    if (watts < 200) return CHART_COLORS.warning;
    return CHART_COLORS.danger;
}

/**
 * Obtener icono por tipo de dispositivo
 */
function getTypeIcon(type) {
    const icons = {
        'light': '&#128161;',
        'tv': '&#128250;',
        'speaker': '&#128266;',
        'camera': '&#128247;',
        'ac': '&#10052;',
        'door': '&#128682;',
        'washer': '&#129529;'
    };
    return icons[type] || '&#128268;';
}

// Inicializar cuando cargue la pagina
document.addEventListener('DOMContentLoaded', initEnergyModule);

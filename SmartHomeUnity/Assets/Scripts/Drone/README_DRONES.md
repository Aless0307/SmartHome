# 🚁 Sistema de Drones Multi-Cliente con Unity Render Streaming

Este sistema permite que múltiples usuarios controlen drones individuales en tiempo real a través de WebRTC, con streaming de video en alta resolución y baja latencia.

## 📋 Requisitos

- Unity 2020.3+ (recomendado 2022.3 LTS)
- Unity Render Streaming package (`com.unity.renderstreaming`)
- GPU con soporte para encoding por hardware (NVIDIA NVENC recomendado)
- Node.js (para el signaling server)

## 🎮 Archivos Creados

```
Assets/Scripts/Drone/
├── DroneController.cs          # Control de movimiento del dron
├── DroneSpawner.cs             # Sistema de spawn de drones
├── DroneStreamManager.cs       # Gestión de streams WebRTC
├── DroneStreamHandler.cs       # Handler individual por dron
├── MultiClientDroneStreaming.cs # Sistema principal multi-cliente
└── DroneConnectionHandler.cs   # Puente con Render Streaming

SmartHomeWeb/
└── drone.html                  # Página web para controlar el dron
```

## 🔧 Configuración en Unity

### Paso 1: Crear el Prefab del Dron

1. **Crear GameObject "DronePrefab"** con:
   - Modelo 3D del dron (puedes usar un cubo temporalmente)
   - Componente `DroneController` 
   - Cámara hijo con nombre "DroneCamera"
   - (Opcional) Luz puntual para identificar el dron

2. **Configurar la Cámara del Dron:**
   - Field of View: 90
   - Near Clip: 0.1
   - Far Clip: 1000
   - Clear Flags: Skybox

3. **Guardar como Prefab** en `Assets/Prefabs/DronePrefab.prefab`

### Paso 2: Configurar la Escena

1. **Crear GameObject vacío "DroneStreamingSystem"**

2. **Añadir componentes de Unity Render Streaming:**
   - `Render Streaming` (desde el menú Component > Render Streaming)
   - `Signaling Manager` - Configurar URL del signaling server

3. **Añadir nuestros componentes:**
   - `MultiClientDroneStreaming` - Asignar el prefab del dron
   - `DroneConnectionHandler`

4. **Configurar MultiClientDroneStreaming:**
   - Drone Prefab: Asignar DronePrefab
   - Stream Width: 1920 (o 2560 para 1440p)
   - Stream Height: 1080 (o 1440 para 1440p)
   - Frame Rate: 60
   - Spawn Center: Posición inicial de los drones
   - Min/Max Bounds: Límites del área de vuelo

### Paso 3: Configurar Render Streaming Settings

1. Ir a **Edit > Project Settings > Render Streaming**

2. Configurar:
   - **Signaling Type:** WebSocket
   - **Signaling URL:** ws://localhost:80
   - **Hardware Encoder:** Habilitado (usar GPU)

### Paso 4: Configurar Input System

El paquete requiere el nuevo Input System de Unity:

1. Ir a **Edit > Project Settings > Player**
2. En **Active Input Handling:** seleccionar "Both" o "Input System Package"
3. Si aparece el wizard, seleccionar "Fix All"

## 🌐 Configurar el Signaling Server

Unity Render Streaming incluye un servidor de signaling:

### Opción A: Descargar desde el Wizard

1. En Unity: **Window > Render Streaming > Render Streaming Wizard**
2. Click en "Download latest version web app"
3. Extraer el archivo descargado
4. Ejecutar el servidor:

```bash
# Windows
webrtc-webapp.exe

# Linux/Mac
./webrtc-webapp
```

### Opción B: Usar Node.js

```bash
# Clonar el repo de Unity Render Streaming
git clone https://github.com/Unity-Technologies/UnityRenderStreaming.git

# Ir al directorio del web server
cd UnityRenderStreaming/WebApp

# Instalar dependencias
npm install

# Ejecutar
npm run start
```

El servidor correrá en `http://localhost:80`

## 🎯 Uso

### Iniciar el Sistema

1. **Ejecutar el Signaling Server** (puerto 80)
2. **Ejecutar Unity** (Play mode)
3. **Abrir `drone.html`** en un navegador (o múltiples)
4. Click en **"Conectar"**
5. ¡Controla tu dron!

### Controles

| Tecla | Acción |
|-------|--------|
| W / ↑ | Avanzar |
| S / ↓ | Retroceder |
| A / ← | Izquierda |
| D / → | Derecha |
| Space | Subir |
| Shift | Bajar |
| Q | Rotar izquierda |
| E | Rotar derecha |

### Multi-Cliente

- Cada navegador que se conecte creará un **dron único**
- Cada dron tiene un **color diferente** para identificarse
- Los drones se pueden **ver entre sí** en el mundo 3D
- Cada cliente solo controla **su propio dron**

## 📊 Optimización de Rendimiento

### Para 2560x1440 @ 60fps sin lag:

1. **Usar GPU Encoding (NVENC):**
   - Project Settings > Render Streaming > Hardware Encoder: Enabled

2. **Ajustar Bitrate:**
   - Para 1440p60: 15-20 Mbps
   - Para 1080p60: 8-12 Mbps

3. **Usar VP9 o H.265 si está disponible:**
   - Mejor compresión = menos datos = menor latencia

4. **Red Local:**
   - WebRTC es P2P, si cliente y Unity están en la misma red, la latencia será mínima (~20-50ms)

### Monitorear Rendimiento

En la página `drone.html` verás:
- **Latencia**: Tiempo de ida y vuelta (RTT)
- **FPS**: Frames recibidos por segundo
- **Bitrate**: Datos por segundo
- **Resolución**: Resolución actual del stream

## 🐛 Troubleshooting

### "Video no se muestra"
- Verificar que el signaling server está corriendo
- Verificar la URL en drone.html (`CONFIG.signalingUrl`)
- Abrir consola del navegador para ver errores

### "Mucho lag"
- Verificar que Hardware Encoder está habilitado
- Reducir resolución temporalmente
- Verificar que la red soporta el bitrate

### "Dron no responde a controles"
- Verificar que el Data Channel está abierto
- Verificar en Unity que se reciben los inputs (logs)

### "Error de CORS"
- Usar el signaling server incluido, no un servidor HTTP estándar

## 🔮 Arquitectura del Sistema

```
┌─────────────────────────────────────────────────────────────┐
│                         UNITY                                │
│                                                              │
│   ┌──────────────────────────────────────────────────────┐  │
│   │           MultiClientDroneStreaming                   │  │
│   │  ┌─────────┐  ┌─────────┐  ┌─────────┐              │  │
│   │  │ Drone 1 │  │ Drone 2 │  │ Drone 3 │  ...         │  │
│   │  │ Camera  │  │ Camera  │  │ Camera  │              │  │
│   │  │  RT1    │  │  RT2    │  │  RT3    │              │  │
│   │  └────┬────┘  └────┬────┘  └────┬────┘              │  │
│   └───────┼────────────┼────────────┼────────────────────┘  │
│           │            │            │                        │
│   ┌───────▼────────────▼────────────▼────────────────────┐  │
│   │            Unity Render Streaming                     │  │
│   │     VideoStreamSender (H.264 NVENC encoding)         │  │
│   │     InputReceiver (keyboard/mouse from browser)       │  │
│   └───────────────────────┬──────────────────────────────┘  │
└───────────────────────────┼──────────────────────────────────┘
                            │ WebRTC (UDP/P2P)
                            │
┌───────────────────────────▼──────────────────────────────────┐
│                    SIGNALING SERVER                           │
│              (WebSocket - Negociación P2P)                    │
│                     localhost:80                              │
└───────────────────────────┬──────────────────────────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         │                  │                  │
         ▼                  ▼                  ▼
   ┌───────────┐      ┌───────────┐      ┌───────────┐
   │ Browser 1 │      │ Browser 2 │      │ Browser 3 │
   │ drone.html│      │ drone.html│      │ drone.html│
   │ (WebRTC)  │      │ (WebRTC)  │      │ (WebRTC)  │
   │  WASD ◄───│      │  WASD ◄───│      │  WASD ◄───│
   │   Video►  │      │   Video►  │      │   Video►  │
   └───────────┘      └───────────┘      └───────────┘
```

## 📝 Notas Importantes

1. **WebRTC es P2P**: Una vez establecida la conexión, el video va directamente Unity → Browser, sin pasar por el servidor.

2. **Hardware Encoding**: Es CRÍTICO para el rendimiento. Sin NVENC, el encoding será por CPU y será lento.

3. **Múltiples Cámaras**: Cada dron tiene su propia cámara y RenderTexture, esto escala bien hasta ~4-6 clientes dependiendo del hardware.

4. **Latencia típica**:
   - Red local: 20-50ms
   - Internet (mismo país): 50-100ms
   - Internet (internacional): 100-200ms

## 🎓 Para la Clase

Este sistema demuestra:
- **WebRTC**: Protocolo de comunicación en tiempo real
- **Streaming de Video**: Encoding, compresión, transmisión
- **Arquitectura Cliente-Servidor**: Signaling vs Data
- **Multi-threading**: GPU encoding paralelo
- **Networking**: P2P, NAT traversal (STUN/TURN)
- **Input Remoto**: Latencia compensation

¡Perfecto para impresionar en tu clase de Programación de Redes! 🚀

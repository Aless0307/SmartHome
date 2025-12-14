# 🏠 Smart Home Unity Client

Cliente visual en Unity para el sistema de domótica Smart Home.

## 📋 Requisitos

- Unity 2021.3 LTS o superior
- TextMeshPro (incluido en Unity)
- Servidor Java Smart Home ejecutándose

## 🚀 Configuración del Proyecto

### 1. Crear Proyecto Unity

1. Abrir Unity Hub
2. Crear nuevo proyecto 3D (o 3D URP)
3. Nombre: `SmartHomeUnity`

### 2. Importar Scripts

Copiar la carpeta `Assets/Scripts` a tu proyecto Unity.

### 3. Configurar Escena Principal

#### Crear GameObjects:

```
Hierarchy:
├── SmartHomeApp (Empty GameObject)
│   ├── SmartHomeClient
│   ├── DeviceManager
│   └── UIManager
├── House (Empty GameObject)
│   └── HouseController
├── Main Camera
│   └── CameraController
├── Directional Light
└── Canvas (UI)
    ├── LoginPanel
    │   ├── Title (TMP_Text)
    │   ├── UsernameInput (TMP_InputField)
    │   ├── PasswordInput (TMP_InputField)
    │   ├── LoginButton (Button)
    │   └── StatusText (TMP_Text)
    ├── MainPanel
    │   ├── Header
    │   │   ├── ConnectionStatus (TMP_Text)
    │   │   ├── UserName (TMP_Text)
    │   │   └── DisconnectButton (Button)
    │   └── DeviceList
    │       └── ScrollView
    │           └── Content (Vertical Layout Group)
    └── LoadingPanel
        └── LoadingText (TMP_Text)
```

### 4. Configurar Componentes

#### SmartHomeApp:
- Server IP: `127.0.0.1` (o IP del servidor)
- Server Port: `5000`
- Default Username: `admin`
- Default Password: `admin123`
- Auto Connect: ✓ (opcional)

#### UIManager:
- Asignar referencias a los paneles y elementos UI

#### DeviceCardPrefab:
Crear un prefab con:
- Image (Background)
- TMP_Text (Name)
- TMP_Text (Type)
- TMP_Text (Room)
- TMP_Text (Status)
- Button (Toggle)
- Slider (Value) - opcional

### 5. Crear Prefabs

#### DeviceCard Prefab:
```
DeviceCard (Panel)
├── Icon (Image)
├── NameText (TMP_Text)
├── TypeText (TMP_Text)
├── RoomText (TMP_Text)
├── StatusText (TMP_Text)
├── ToggleButton (Button)
├── ValueSlider (Slider) [opcional]
└── DeviceCardUI (Script)
```

## 🎮 Controles

### Cámara:
- **WASD / Flechas**: Mover
- **Click derecho + arrastrar**: Rotar
- **Scroll**: Zoom
- **R**: Reset cámara

### Dispositivos:
- **Click izquierdo**: Toggle encendido/apagado

## 📡 Protocolo de Comunicación

El cliente Unity se comunica con el servidor Java mediante TCP:

### Mensajes enviados:
```json
{"action": "LOGIN", "username": "admin", "password": "admin123"}
{"action": "GET_DEVICES"}
{"action": "DEVICE_CONTROL", "deviceId": "xxx", "command": "ON"}
{"action": "DEVICE_CONTROL", "deviceId": "xxx", "command": "OFF"}
{"action": "DEVICE_CONTROL", "deviceId": "xxx", "command": "TOGGLE"}
{"action": "DEVICE_CONTROL", "deviceId": "xxx", "command": "SET_VALUE", "value": "50"}
{"action": "DEVICE_CONTROL", "deviceId": "xxx", "command": "SET_COLOR", "color": "#FF0000"}
```

### Mensajes recibidos:
```json
{"action": "CONNECTED", "message": "..."}
{"action": "LOGIN_SUCCESS", "username": "admin", "role": "admin"}
{"action": "LOGIN_FAILED", "message": "..."}
{"action": "DEVICES_LIST", "devices": "[...]"}
{"action": "DEVICE_UPDATED", "device": "{...}"}
```

## 📁 Estructura de Scripts

```
Assets/Scripts/
├── SmartHomeApp.cs           # Inicialización principal
├── Network/
│   ├── SmartHomeClient.cs    # Cliente TCP
│   └── DeviceManager.cs      # Gestor de dispositivos
├── UI/
│   ├── UIManager.cs          # Gestor de UI
│   └── DeviceCardUI.cs       # Tarjeta de dispositivo
└── Visualization/
    ├── HouseController.cs    # Controlador de casa 3D
    ├── RoomController.cs     # Controlador de habitación
    ├── DeviceVisual.cs       # Visual de dispositivo 3D
    └── CameraController.cs   # Control de cámara
```

## 🔧 Solución de Problemas

### No conecta al servidor:
1. Verificar que el servidor Java esté ejecutándose
2. Verificar IP y puerto en SmartHomeApp
3. Verificar firewall

### No aparecen dispositivos:
1. Verificar login exitoso en consola
2. Verificar que hay datos en MongoDB

### Errores de UI:
1. Asegurarse de tener TextMeshPro instalado
2. Verificar referencias en UIManager

## 🏃 Ejecutar

1. Iniciar servidor Java:
```bash
cd SmartHomeServer
java -cp "bin:lib/*" com.smarthome.server.TcpServer
```

2. Play en Unity

3. Login con:
   - Usuario: `admin`
   - Contraseña: `admin123`

## 📝 Notas

- Los scripts están diseñados para funcionar sin dependencias externas
- El parsing JSON es manual para evitar dependencias
- La visualización 3D se genera automáticamente basada en los datos del servidor

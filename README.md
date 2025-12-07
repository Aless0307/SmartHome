# 🏠 Smart Home - Sistema de Domótica

Sistema completo de domótica que integra un servidor Java multi-protocolo con una visualización 3D en Unity.

![Java](https://img.shields.io/badge/Java-17+-orange)
![Unity](https://img.shields.io/badge/Unity-2021+-black)
![MongoDB](https://img.shields.io/badge/MongoDB-Atlas-green)
![License](https://img.shields.io/badge/License-MIT-blue)

## 📋 Descripción

Proyecto universitario para la materia **"Programación de Redes en Java"** que implementa un sistema de casa inteligente con:

- **Servidor Java** multi-protocolo (TCP, UDP, REST)
- **Cliente GUI Java** (Swing) para control de dispositivos
- **Visualización 3D en Unity** con casa interactiva
- **Base de datos MongoDB Atlas** para persistencia
- **Autenticación JWT** para seguridad

## 🏗️ Arquitectura

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Unity 3D      │     │   GUI Java      │     │   REST Client   │
│   (C#)          │     │   (Swing)       │     │   (curl/web)    │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         │ TCP:5000              │ TCP:5000              │ HTTP:8080
         │                       │                       │
         └───────────────┬───────┴───────────────────────┘
                         │
              ┌──────────▼──────────┐
              │   SERVIDOR JAVA     │
              │  ┌───────────────┐  │
              │  │ TCP Server    │  │ Puerto 5000
              │  │ UDP Server    │  │ Puerto 5001 (broadcast)
              │  │ REST Server   │  │ Puerto 8080
              │  └───────────────┘  │
              │         │           │
              │  ┌──────▼────────┐  │
              │  │   MongoDB     │  │
              │  │   Atlas       │  │
              │  └───────────────┘  │
              └─────────────────────┘
```

## 🚀 Características

### Protocolos Implementados
| Protocolo | Puerto | Uso |
|-----------|--------|-----|
| **TCP** | 5000 | Control principal de dispositivos |
| **UDP** | 5001 | Notificaciones broadcast en tiempo real |
| **REST** | 8080 | API HTTP para integración externa |

### Dispositivos Soportados
- 💡 **Luces inteligentes** - On/Off, intensidad, color RGB
- 📺 **TV motorizada** - Subir/bajar con animación
- 🚪 **Puerta de garage** - Abrir/cerrar animado
- 🌡️ **Aires acondicionados** - On/Off, temperatura
- 🔊 **Bocina inteligente (Echo Dot)** - Reproducir música, volumen
- 🧺 **Lavadora** - On/Off con animación

### Seguridad
- ✅ Autenticación por usuario/contraseña
- ✅ Tokens JWT con expiración de 24 horas
- ✅ Validación en cada request

## 📁 Estructura del Proyecto

```
SmartHome/
├── SmartHomeServer/          # Servidor Java
│   ├── src/main/java/com/smarthome/
│   │   ├── server/           # TCP, UDP, REST servers
│   │   ├── service/          # Lógica de negocio
│   │   ├── model/            # Entidades (Device, User, House)
│   │   ├── database/         # Conexión MongoDB
│   │   ├── protocol/         # Manejo de JSON
│   │   └── security/         # JWT Utils
│   └── lib/                  # Dependencias (MongoDB, JWT)
│
├── SmartHomeClient/          # Cliente GUI Java
│   ├── src/main/java/com/smarthome/client/
│   │   ├── SmartHomeClientGUI.java
│   │   └── TcpClient.java
│   └── SmartHomeClient.jar   # Ejecutable
│
└── SmartHomeUnity/           # Proyecto Unity 3D
    └── Assets/Scripts/
        ├── Network/          # SmartHomeClient, DeviceManager
        ├── Devices/          # SmartLight, TVLift, SideGate, etc.
        ├── UI/               # UIManager, DeviceCardUI
        └── DeviceBridge.cs   # Puente servidor-objetos 3D
```

## 🛠️ Requisitos

- **Java 17+** (OpenJDK o Oracle)
- **Unity 2021+** (para visualización 3D)
- **MongoDB Atlas** (cuenta gratuita)
- **Git** (para clonar)

## ⚡ Instalación y Ejecución

### 1. Clonar el repositorio
```bash
git clone https://github.com/Aless0307/SmartHome.git
cd SmartHome
```

### 2. Iniciar el Servidor
```bash
cd SmartHomeServer/bin
java -cp ".:../lib/*" com.smarthome.server.TcpServer
```

Verás:
```
═══════════════════════════════════════════════════════
  🏠 SMART HOME - Servidor Completo
  📡 TCP Puerto: 5000 (Control principal)
  📢 UDP Puerto: 5001 (Notificaciones broadcast)
  🌐 REST Puerto: 8080 (API HTTP)
  📱 Dispositivos: 16
  👥 Usuarios: 2
═══════════════════════════════════════════════════════
```

### 3. Ejecutar Cliente GUI
```bash
java -jar SmartHomeClient/SmartHomeClient.jar
```

O compilar desde fuente:
```bash
cd SmartHomeClient
javac -d bin src/main/java/com/smarthome/client/*.java
java -cp bin com.smarthome.client.SmartHomeClientGUI
```

### 4. Abrir Unity (opcional)
1. Abrir Unity Hub
2. Add Project → Seleccionar `SmartHomeUnity/`
3. Play para ver la casa 3D

## 🔑 Credenciales de Prueba

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| admin | admin123 | Administrador |
| test | test123 | Usuario |

## 📡 API REST

### Endpoints Disponibles

```bash
# Obtener dispositivos
GET http://localhost:8080/api/devices

# Obtener dispositivo específico
GET http://localhost:8080/api/device?id=xxx

# Login (obtener JWT)
POST http://localhost:8080/api/login
Body: {"username": "admin", "password": "admin123"}

# Controlar dispositivo
POST http://localhost:8080/api/control
Headers: Authorization: Bearer <JWT_TOKEN>
Body: {"deviceId": "xxx", "command": "toggle"}
```

### Comandos Disponibles
- `on` / `off` / `toggle` - Encender/apagar
- `set_value` - Establecer valor (intensidad, temperatura)
- `set_color` - Cambiar color (luces RGB)

## 🎮 Controles Unity

| Tecla | Acción |
|-------|--------|
| WASD | Mover cámara |
| Mouse | Rotar vista |
| R | Toggle TV |
| P | Toggle puerta garage |

## 📊 Base de Datos

El proyecto usa **MongoDB Atlas** con la siguiente estructura:

```javascript
// Colección: devices
{
  "_id": ObjectId,
  "name": "Luz Sala 1",
  "type": "light",      // light, door, tv, ac, speaker, appliance
  "room": "sala",
  "status": true,
  "value": 100,         // intensidad, temperatura, volumen
  "color": "#FFFFFF"    // para luces RGB
}

// Colección: users
{
  "_id": ObjectId,
  "username": "admin",
  "password": "hashed",
  "email": "admin@smarthome.com",
  "role": "admin",
  "houseId": "xxx"
}
```

## 🧪 Testing Multi-Cliente

Para probar sincronización en tiempo real:

1. Iniciar servidor
2. Abrir 2+ clientes GUI en diferentes terminales
3. Cambiar un dispositivo en un cliente
4. Ver actualización instantánea en los demás (via UDP broadcast)

## 👨‍💻 Autor

**Alessandro Atilano**
- GitHub: [@Aless0307](https://github.com/Aless0307)

## 📄 Licencia

Este proyecto está bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para detalles.

---

⭐ Si este proyecto te fue útil, ¡dale una estrella!

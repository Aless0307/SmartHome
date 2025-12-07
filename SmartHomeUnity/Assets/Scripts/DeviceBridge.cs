using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script puente que conecta objetos existentes de Unity con el servidor Smart Home
/// </summary>
public class DeviceBridge : MonoBehaviour
{
    [System.Serializable]
    public class DeviceMapping
    {
        [Tooltip("Nombre del dispositivo en el servidor (ej: Luz Principal, Puerta Principal)")]
        public string serverDeviceName;
        
        [Tooltip("El GameObject que contiene el script a controlar")]
        public GameObject targetObject;
        
        [Tooltip("Nombre del método para ENCENDER (ej: Open, TurnOn, Encender)")]
        public string onMethodName = "";
        
        [Tooltip("Nombre del método para APAGAR (ej: Close, TurnOff, Apagar)")]
        public string offMethodName = "";
        
        [Tooltip("Si no tienes métodos, marca esto para simular tecla")]
        public bool useKeySimulation = true;
        
        [Tooltip("Tecla que activa el objeto (solo si useKeySimulation = true)")]
        public KeyCode simulatedKey = KeyCode.None;
        
        // Estado interno
        [HideInInspector] public string deviceId;
        [HideInInspector] public bool currentStatus;
        [HideInInspector] public int currentValue;
        [HideInInspector] public string currentColor;
    }
    
    [Header("Mapeo de Dispositivos")]
    public List<DeviceMapping> deviceMappings = new List<DeviceMapping>();
    
    // Diccionario para búsqueda rápida
    private Dictionary<string, DeviceMapping> mappingById = new Dictionary<string, DeviceMapping>();
    private Dictionary<string, DeviceMapping> mappingByName = new Dictionary<string, DeviceMapping>();
    
    void Start()
    {
        // Indexar por nombre
        foreach (var mapping in deviceMappings)
        {
            if (!string.IsNullOrEmpty(mapping.serverDeviceName))
            {
                mappingByName[mapping.serverDeviceName.ToLower()] = mapping;
            }
        }
        
        // Suscribirse a eventos
        if (DeviceManager.Instance != null)
        {
            DeviceManager.Instance.OnDevicesLoaded.AddListener(OnDevicesLoaded);
            DeviceManager.Instance.OnDeviceUpdated.AddListener(OnDeviceUpdated);
        }
    }
    
    void OnDestroy()
    {
        if (DeviceManager.Instance != null)
        {
            DeviceManager.Instance.OnDevicesLoaded.RemoveListener(OnDevicesLoaded);
            DeviceManager.Instance.OnDeviceUpdated.RemoveListener(OnDeviceUpdated);
        }
    }
    
    /// <summary>
    /// Cuando se cargan los dispositivos del servidor
    /// </summary>
    private void OnDevicesLoaded(List<DeviceData> devices)
    {
        Debug.Log($"🔗 DeviceBridge: Vinculando {devices.Count} dispositivos...");
        
        foreach (var device in devices)
        {
            string key = device.name.ToLower();
            
            if (mappingByName.ContainsKey(key))
            {
                var mapping = mappingByName[key];
                mapping.deviceId = device.id;
                mapping.currentStatus = device.status;
                mappingById[device.id] = mapping;
                
                Debug.Log($"  ✅ Vinculado: {device.name} → {mapping.targetObject?.name ?? "NULL"}");
                
                // NO sincronizar estado inicial para evitar que se muevan al iniciar
                // ApplyDeviceState(mapping, device.status);
            }
        }
    }
    
    /// <summary>
    /// Cuando un dispositivo se actualiza desde el servidor
    /// </summary>
    private void OnDeviceUpdated(DeviceData device)
    {
        Debug.Log($"📨📨📨 DeviceBridge.OnDeviceUpdated: {device.name}, status={device.status}, value={device.value}, color={device.color}");
        
        // Verificar si tenemos mapping por ID
        if (mappingById.ContainsKey(device.id))
        {
            Debug.Log($"✅ Encontrado mapping por ID para: {device.name}");
            var mapping = mappingById[device.id];
            
            // Verificar si el estado cambió
            if (mapping.currentStatus != device.status)
            {
                Debug.Log($"🔄 {device.name}: {(device.status ? "ENCENDER" : "APAGAR")}");
                mapping.currentStatus = device.status;
                ApplyDeviceState(mapping, device.status);
            }
            
            // Verificar si el valor cambió (brillo) - permitir cualquier valor >= 0
            if (mapping.currentValue != device.value)
            {
                Debug.Log($"🔆 {device.name}: Valor = {device.value}");
                mapping.currentValue = device.value;
                ApplyDeviceValue(mapping, device.value);
            }
            
            // Verificar si el color cambió
            if (!string.IsNullOrEmpty(device.color) && mapping.currentColor != device.color)
            {
                Debug.Log($"🎨 {device.name}: Color = {device.color}");
                mapping.currentColor = device.color;
                ApplyDeviceColor(mapping, device.color);
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ Dispositivo {device.name} (id={device.id}) no tiene mapping");
        }
    }
    
    /// <summary>
    /// Aplicar valor (brillo) al objeto de Unity
    /// </summary>
    private void ApplyDeviceValue(DeviceMapping mapping, int value)
    {
        if (mapping.targetObject == null) return;
        
        // Llamar OnSmartHomeValue con el valor como string
        mapping.targetObject.SendMessage("OnSmartHomeValue", value.ToString(), SendMessageOptions.DontRequireReceiver);
    }
    
    /// <summary>
    /// Aplicar color al objeto de Unity
    /// </summary>
    private void ApplyDeviceColor(DeviceMapping mapping, string hexColor)
    {
        if (mapping.targetObject == null) return;
        
        // Verificar si es un comando de speaker (CMD:PLAY, CMD:NEXT, etc)
        if (hexColor.StartsWith("CMD:"))
        {
            string command = hexColor.Substring(4); // Quitar "CMD:"
            Debug.Log($"🔊 Comando de speaker: {command}");
            mapping.targetObject.SendMessage("OnSmartHomeCommand", command, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            // Es un color normal
            mapping.targetObject.SendMessage("OnSmartHomeColor", hexColor, SendMessageOptions.DontRequireReceiver);
        }
    }
    
    /// <summary>
    /// Aplicar estado al objeto de Unity
    /// </summary>
    private void ApplyDeviceState(DeviceMapping mapping, bool turnOn)
    {
        if (mapping.targetObject == null)
        {
            Debug.LogWarning($"⚠️ Target object es null para {mapping.serverDeviceName}");
            return;
        }
        
        // Opción 1: Usar Key Simulation - llamar TurnOn o TurnOff directamente
        if (mapping.useKeySimulation)
        {
            string methodName = turnOn ? "TurnOn" : "TurnOff";
            Debug.Log($"🎮 Llamando {methodName} en {mapping.targetObject.name}");
            mapping.targetObject.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
        }
        // Opción 2: Llamar método específico
        else if (!string.IsNullOrEmpty(turnOn ? mapping.onMethodName : mapping.offMethodName))
        {
            string methodName = turnOn ? mapping.onMethodName : mapping.offMethodName;
            Debug.Log($"🎮 Llamando {methodName} en {mapping.targetObject.name}");
            mapping.targetObject.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
        }
    }
    
    /// <summary>
    /// Simular presión de tecla llamando al Update del objeto
    /// </summary>
    private void SimulateKeyPress(DeviceMapping mapping)
    {
        // Enviamos un mensaje especial que los scripts pueden capturar
        mapping.targetObject.SendMessage("OnSmartHomeToggle", SendMessageOptions.DontRequireReceiver);
        
        // También intentamos con BroadcastMessage para objetos hijos
        mapping.targetObject.BroadcastMessage("OnSmartHomeToggle", SendMessageOptions.DontRequireReceiver);
    }
    
    /// <summary>
    /// Método público para activar un dispositivo desde UI o código
    /// </summary>
    public void ToggleDevice(string deviceName)
    {
        string key = deviceName.ToLower();
        if (mappingByName.ContainsKey(key))
        {
            var mapping = mappingByName[key];
            if (!string.IsNullOrEmpty(mapping.deviceId))
            {
                DeviceManager.Instance?.Toggle(mapping.deviceId);
            }
        }
    }
}

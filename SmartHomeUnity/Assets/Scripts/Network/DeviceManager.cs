using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestor de dispositivos del Smart Home
/// Mantiene el estado de todos los dispositivos y notifica cambios
/// </summary>
public class DeviceManager : MonoBehaviour
{
    // Singleton
    public static DeviceManager Instance { get; private set; }
    
    // Diccionario de dispositivos por ID
    private Dictionary<string, DeviceData> devices = new Dictionary<string, DeviceData>();
    
    // Diccionario de dispositivos por habitación
    private Dictionary<string, List<DeviceData>> devicesByRoom = new Dictionary<string, List<DeviceData>>();
    
    // Eventos
    [System.Serializable]
    public class DeviceEvent : UnityEvent<DeviceData> { }
    
    [System.Serializable]
    public class DeviceListEvent : UnityEvent<List<DeviceData>> { }
    
    public DeviceEvent OnDeviceAdded;
    public DeviceEvent OnDeviceUpdated;
    public DeviceListEvent OnDevicesLoaded;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        if (OnDeviceAdded == null) OnDeviceAdded = new DeviceEvent();
        if (OnDeviceUpdated == null) OnDeviceUpdated = new DeviceEvent();
        if (OnDevicesLoaded == null) OnDevicesLoaded = new DeviceListEvent();
    }
    
    void Start()
    {
        // Suscribirse a eventos del cliente
        if (SmartHomeClient.Instance != null)
        {
            SmartHomeClient.Instance.OnDevicesReceived += HandleDevicesReceived;
            SmartHomeClient.Instance.OnDeviceChanged += HandleDeviceChanged;
        }
    }
    
    void OnDestroy()
    {
        if (SmartHomeClient.Instance != null)
        {
            SmartHomeClient.Instance.OnDevicesReceived -= HandleDevicesReceived;
            SmartHomeClient.Instance.OnDeviceChanged -= HandleDeviceChanged;
        }
    }
    
    /// <summary>
    /// Manejar lista de dispositivos recibida del servidor
    /// </summary>
    private void HandleDevicesReceived(List<DeviceData> deviceList)
    {
        devices.Clear();
        devicesByRoom.Clear();
        
        foreach (var device in deviceList)
        {
            AddOrUpdateDevice(device);
        }
        
        Debug.Log($"📱 Cargados {devices.Count} dispositivos en {devicesByRoom.Count} habitaciones");
        OnDevicesLoaded?.Invoke(new List<DeviceData>(devices.Values));
    }
    
    /// <summary>
    /// Manejar cambio de dispositivo individual
    /// </summary>
    private void HandleDeviceChanged(DeviceData device)
    {
        if (devices.ContainsKey(device.id))
        {
            // Actualizar dispositivo existente
            devices[device.id] = device;
            UpdateRoomIndex(device);
            OnDeviceUpdated?.Invoke(device);
        }
        else
        {
            // Nuevo dispositivo
            AddOrUpdateDevice(device);
            OnDeviceAdded?.Invoke(device);
        }
    }
    
    /// <summary>
    /// Agregar o actualizar un dispositivo
    /// </summary>
    private void AddOrUpdateDevice(DeviceData device)
    {
        devices[device.id] = device;
        UpdateRoomIndex(device);
    }
    
    /// <summary>
    /// Actualizar índice por habitación
    /// </summary>
    private void UpdateRoomIndex(DeviceData device)
    {
        string room = string.IsNullOrEmpty(device.room) ? "Sin Habitación" : device.room;
        
        if (!devicesByRoom.ContainsKey(room))
        {
            devicesByRoom[room] = new List<DeviceData>();
        }
        
        // Remover si ya existe
        devicesByRoom[room].RemoveAll(d => d.id == device.id);
        
        // Agregar actualizado
        devicesByRoom[room].Add(device);
    }
    
    // ═══════════════════════════════════════════════════════════
    // CONSULTAS
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Obtener todos los dispositivos
    /// </summary>
    public List<DeviceData> GetAllDevices()
    {
        return new List<DeviceData>(devices.Values);
    }
    
    /// <summary>
    /// Obtener dispositivo por ID
    /// </summary>
    public DeviceData GetDevice(string id)
    {
        return devices.ContainsKey(id) ? devices[id] : null;
    }
    
    /// <summary>
    /// Obtener dispositivos por habitación
    /// </summary>
    public List<DeviceData> GetDevicesByRoom(string room)
    {
        return devicesByRoom.ContainsKey(room) ? devicesByRoom[room] : new List<DeviceData>();
    }
    
    /// <summary>
    /// Obtener dispositivos por tipo
    /// </summary>
    public List<DeviceData> GetDevicesByType(string type)
    {
        var result = new List<DeviceData>();
        foreach (var device in devices.Values)
        {
            if (device.type.ToLower() == type.ToLower())
            {
                result.Add(device);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Obtener lista de habitaciones
    /// </summary>
    public List<string> GetRooms()
    {
        return new List<string>(devicesByRoom.Keys);
    }
    
    /// <summary>
    /// Obtener número de dispositivos encendidos
    /// </summary>
    public int GetActiveDeviceCount()
    {
        int count = 0;
        foreach (var device in devices.Values)
        {
            if (device.status) count++;
        }
        return count;
    }
    
    // ═══════════════════════════════════════════════════════════
    // CONTROLES
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Encender dispositivo
    /// </summary>
    public void TurnOn(string deviceId)
    {
        SmartHomeClient.Instance?.TurnOn(deviceId);
    }
    
    /// <summary>
    /// Apagar dispositivo
    /// </summary>
    public void TurnOff(string deviceId)
    {
        SmartHomeClient.Instance?.TurnOff(deviceId);
    }
    
    /// <summary>
    /// Toggle dispositivo
    /// </summary>
    public void Toggle(string deviceId)
    {
        SmartHomeClient.Instance?.Toggle(deviceId);
    }
    
    /// <summary>
    /// Establecer valor de dispositivo
    /// </summary>
    public void SetValue(string deviceId, int value)
    {
        SmartHomeClient.Instance?.SetDeviceValue(deviceId, value);
    }
    
    /// <summary>
    /// Establecer color de dispositivo
    /// </summary>
    public void SetColor(string deviceId, Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGB(color);
        SmartHomeClient.Instance?.SetDeviceColor(deviceId, "#" + hex);
    }
    
    /// <summary>
    /// Encender todos los dispositivos de una habitación
    /// </summary>
    public void TurnOnRoom(string room)
    {
        var roomDevices = GetDevicesByRoom(room);
        foreach (var device in roomDevices)
        {
            TurnOn(device.id);
        }
    }
    
    /// <summary>
    /// Apagar todos los dispositivos de una habitación
    /// </summary>
    public void TurnOffRoom(string room)
    {
        var roomDevices = GetDevicesByRoom(room);
        foreach (var device in roomDevices)
        {
            TurnOff(device.id);
        }
    }
    
    /// <summary>
    /// Apagar todos los dispositivos
    /// </summary>
    public void TurnOffAll()
    {
        foreach (var device in devices.Values)
        {
            TurnOff(device.id);
        }
    }
}

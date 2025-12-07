using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Cliente TCP para conectar Unity al Smart Home Server (Java)
/// Este script maneja toda la comunicación de red
/// </summary>
public class SmartHomeClient : MonoBehaviour
{
    [Header("Configuración del Servidor")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5000;
    
    [Header("Credenciales")]
    public string username = "admin";
    public string password = "admin123";
    
    [Header("Estado")]
    public bool isConnected = false;
    public bool isLoggedIn = false;
    
    // Conexión TCP
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private volatile bool running = false;
    
    // Cola de mensajes para procesar en el hilo principal de Unity
    private Queue<string> messageQueue = new Queue<string>();
    private object queueLock = new object();
    
    // Eventos para notificar cambios
    public event Action<string> OnMessageReceived;
    public event Action<bool> OnConnectionChanged;
    public event Action<bool> OnLoginResult;
    public event Action<List<DeviceData>> OnDevicesReceived;
    public event Action<DeviceData> OnDeviceChanged;
    
    // Singleton
    public static SmartHomeClient Instance { get; private set; }
    
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
        }
    }
    
    void Update()
    {
        // Procesar mensajes en el hilo principal
        ProcessMessageQueue();
    }
    
    void OnDestroy()
    {
        Disconnect();
    }
    
    void OnApplicationQuit()
    {
        Disconnect();
    }
    
    /// <summary>
    /// Conectar al servidor TCP
    /// </summary>
    public void Connect()
    {
        if (isConnected) return;
        
        try
        {
            client = new TcpClient();
            client.Connect(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;
            running = true;
            
            // Iniciar hilo de recepción
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            Debug.Log($"✅ Conectado a {serverIP}:{serverPort}");
            OnConnectionChanged?.Invoke(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error conectando: {e.Message}");
            isConnected = false;
            OnConnectionChanged?.Invoke(false);
        }
    }
    
    /// <summary>
    /// Desconectar del servidor
    /// </summary>
    public void Disconnect()
    {
        running = false;
        isConnected = false;
        isLoggedIn = false;
        
        try
        {
            Send("{\"action\": \"DISCONNECT\"}");
            
            if (stream != null) stream.Close();
            if (client != null) client.Close();
            if (receiveThread != null && receiveThread.IsAlive)
            {
                receiveThread.Join(1000);
            }
        }
        catch { }
        
        Debug.Log("🔌 Desconectado");
        OnConnectionChanged?.Invoke(false);
    }
    
    /// <summary>
    /// Hilo de recepción de mensajes
    /// </summary>
    private void ReceiveLoop()
    {
        byte[] buffer = new byte[8192];
        StringBuilder messageBuilder = new StringBuilder();
        
        while (running && client != null && client.Connected)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        messageBuilder.Append(data);
                        
                        // Procesar líneas completas
                        string fullMessage = messageBuilder.ToString();
                        string[] lines = fullMessage.Split('\n');
                        
                        for (int i = 0; i < lines.Length - 1; i++)
                        {
                            string line = lines[i].Trim();
                            if (!string.IsNullOrEmpty(line))
                            {
                                EnqueueMessage(line);
                            }
                        }
                        
                        // Guardar el resto para la siguiente lectura
                        messageBuilder.Clear();
                        messageBuilder.Append(lines[lines.Length - 1]);
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                if (running)
                {
                    Debug.LogError($"Error recibiendo: {e.Message}");
                }
                break;
            }
        }
        
        isConnected = false;
    }
    
    /// <summary>
    /// Agregar mensaje a la cola
    /// </summary>
    private void EnqueueMessage(string message)
    {
        lock (queueLock)
        {
            messageQueue.Enqueue(message);
        }
    }
    
    /// <summary>
    /// Procesar mensajes en el hilo principal de Unity
    /// </summary>
    private void ProcessMessageQueue()
    {
        lock (queueLock)
        {
            while (messageQueue.Count > 0)
            {
                string message = messageQueue.Dequeue();
                HandleMessage(message);
            }
        }
    }
    
    /// <summary>
    /// Manejar mensaje recibido
    /// </summary>
    private void HandleMessage(string json)
    {
        Debug.Log($"← {json}");
        OnMessageReceived?.Invoke(json);
        
        try
        {
            var data = JsonHelper.ParseSimple(json);
            string action = data.GetValueOrDefault("action", "");
            
            switch (action)
            {
                case "CONNECTED":
                    Debug.Log("✅ Conectado al servidor");
                    // Auto-login
                    Login(username, password);
                    break;
                    
                case "LOGIN_SUCCESS":
                    isLoggedIn = true;
                    Debug.Log($"✅ Login exitoso: {data.GetValueOrDefault("username", "")}");
                    OnLoginResult?.Invoke(true);
                    // Obtener dispositivos
                    GetDevices();
                    break;
                    
                case "LOGIN_FAILED":
                    isLoggedIn = false;
                    Debug.LogWarning("❌ Login fallido");
                    OnLoginResult?.Invoke(false);
                    break;
                    
                case "DEVICES_LIST":
                    var devices = ParseDevices(json);
                    Debug.Log($"📱 Dispositivos recibidos: {devices.Count}");
                    OnDevicesReceived?.Invoke(devices);
                    break;
                    
                case "DEVICE_UPDATED":
                case "DEVICE_CHANGED":
                    string deviceJson = data.GetValueOrDefault("device", "");
                    if (!string.IsNullOrEmpty(deviceJson))
                    {
                        var device = ParseSingleDevice(deviceJson);
                        if (device != null)
                        {
                            Debug.Log($"🔄 Dispositivo actualizado: {device.name}");
                            OnDeviceChanged?.Invoke(device);
                        }
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parseando mensaje: {e.Message}");
        }
    }
    
    /// <summary>
    /// Enviar mensaje al servidor
    /// </summary>
    public void Send(string message)
    {
        if (!isConnected || stream == null) return;
        
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            stream.Write(data, 0, data.Length);
            stream.Flush();
            Debug.Log($"→ {message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error enviando: {e.Message}");
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // COMANDOS
    // ═══════════════════════════════════════════════════════════
    
    public void Login(string user, string pass)
    {
        Send($"{{\"action\": \"LOGIN\", \"username\": \"{user}\", \"password\": \"{pass}\"}}");
    }
    
    public void GetDevices()
    {
        Send("{\"action\": \"GET_DEVICES\"}");
    }
    
    public void GetRooms()
    {
        Send("{\"action\": \"GET_ROOMS\"}");
    }
    
    public void ControlDevice(string deviceId, string command)
    {
        Send($"{{\"action\": \"DEVICE_CONTROL\", \"deviceId\": \"{deviceId}\", \"command\": \"{command}\"}}");
    }
    
    public void SetDeviceValue(string deviceId, int value)
    {
        Send($"{{\"action\": \"DEVICE_CONTROL\", \"deviceId\": \"{deviceId}\", \"command\": \"SET_VALUE\", \"value\": \"{value}\"}}");
    }
    
    public void SetDeviceColor(string deviceId, string color)
    {
        Send($"{{\"action\": \"DEVICE_CONTROL\", \"deviceId\": \"{deviceId}\", \"command\": \"SET_COLOR\", \"color\": \"{color}\"}}");
    }
    
    public void TurnOn(string deviceId) => ControlDevice(deviceId, "ON");
    public void TurnOff(string deviceId) => ControlDevice(deviceId, "OFF");
    public void Toggle(string deviceId) => ControlDevice(deviceId, "TOGGLE");
    
    // ═══════════════════════════════════════════════════════════
    // PARSING
    // ═══════════════════════════════════════════════════════════
    
    private List<DeviceData> ParseDevices(string json)
    {
        var devices = new List<DeviceData>();
        
        // Encontrar el array de devices
        int start = json.IndexOf("\"devices\":\"");
        if (start == -1) return devices;
        start += 11;
        
        // Extraer el contenido del array
        int brackets = 0;
        int end = start;
        bool foundStart = false;
        
        for (int i = start; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '[' || (c == '\\' && i + 1 < json.Length && json[i + 1] == '['))
            {
                if (c == '\\') i++;
                brackets++;
                foundStart = true;
            }
            else if (c == ']' || (c == '\\' && i + 1 < json.Length && json[i + 1] == ']'))
            {
                if (c == '\\') i++;
                brackets--;
                if (foundStart && brackets == 0)
                {
                    end = i + 1;
                    break;
                }
            }
        }
        
        string devicesJson = json.Substring(start, end - start);
        devicesJson = devicesJson.Replace("\\\"", "\"").Replace("\\\\", "\\");
        
        // Parsear cada dispositivo
        int depth = 0;
        StringBuilder current = new StringBuilder();
        
        for (int i = 0; i < devicesJson.Length; i++)
        {
            char c = devicesJson[i];
            
            if (c == '{')
            {
                depth++;
                current.Append(c);
            }
            else if (c == '}')
            {
                depth--;
                current.Append(c);
                if (depth == 0)
                {
                    var device = ParseSingleDevice(current.ToString());
                    if (device != null)
                    {
                        devices.Add(device);
                    }
                    current.Clear();
                }
            }
            else if (depth > 0)
            {
                current.Append(c);
            }
        }
        
        return devices;
    }
    
    private DeviceData ParseSingleDevice(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        
        json = json.Replace("\\\"", "\"").Replace("\\\\", "\\");
        var data = JsonHelper.ParseSimple(json);
        
        if (!data.ContainsKey("id")) return null;
        
        return new DeviceData
        {
            id = data.GetValueOrDefault("id", ""),
            name = data.GetValueOrDefault("name", "Unknown"),
            type = data.GetValueOrDefault("type", "unknown"),
            room = data.GetValueOrDefault("room", ""),
            status = data.GetValueOrDefault("status", "false") == "true",
            value = int.TryParse(data.GetValueOrDefault("value", "0"), out int v) ? v : 0,
            color = data.GetValueOrDefault("color", "#FFFFFF")
        };
    }
}

/// <summary>
/// Datos de un dispositivo
/// </summary>
[System.Serializable]
public class DeviceData
{
    public string id;
    public string name;
    public string type;      // light, thermostat, door, camera, sensor
    public string room;
    public bool status;
    public int value;
    public string color;
}

/// <summary>
/// Helper para parsear JSON simple sin dependencias
/// </summary>
public static class JsonHelper
{
    public static Dictionary<string, string> ParseSimple(string json)
    {
        var data = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(json)) return data;
        
        json = json.Trim();
        if (json.StartsWith("{")) json = json.Substring(1);
        if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);
        
        int i = 0;
        while (i < json.Length)
        {
            // Saltar espacios
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length) break;
            
            // Buscar inicio de key
            if (json[i] != '"') { i++; continue; }
            i++;
            
            // Leer key
            StringBuilder key = new StringBuilder();
            while (i < json.Length && json[i] != '"')
            {
                key.Append(json[i]);
                i++;
            }
            i++;
            
            // Saltar hasta :
            while (i < json.Length && json[i] != ':') i++;
            i++;
            
            // Saltar espacios
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length) break;
            
            // Leer value
            StringBuilder value = new StringBuilder();
            char first = json[i];
            
            if (first == '"')
            {
                i++;
                while (i < json.Length)
                {
                    if (json[i] == '\\' && i + 1 < json.Length)
                    {
                        value.Append(json[i + 1]);
                        i += 2;
                    }
                    else if (json[i] == '"')
                    {
                        i++;
                        break;
                    }
                    else
                    {
                        value.Append(json[i]);
                        i++;
                    }
                }
            }
            else if (first == '[' || first == '{')
            {
                int depth = 0;
                char open = first;
                char close = first == '[' ? ']' : '}';
                do
                {
                    if (json[i] == open) depth++;
                    if (json[i] == close) depth--;
                    value.Append(json[i]);
                    i++;
                } while (i < json.Length && depth > 0);
            }
            else
            {
                while (i < json.Length && json[i] != ',' && json[i] != '}')
                {
                    value.Append(json[i]);
                    i++;
                }
            }
            
            string keyStr = key.ToString().Trim();
            string valueStr = value.ToString().Trim();
            if (!string.IsNullOrEmpty(keyStr))
            {
                data[keyStr] = valueStr;
            }
            
            while (i < json.Length && (json[i] == ',' || char.IsWhiteSpace(json[i]))) i++;
        }
        
        return data;
    }
    
    public static string GetValueOrDefault(this Dictionary<string, string> dict, string key, string defaultValue)
    {
        return dict.ContainsKey(key) ? dict[key] : defaultValue;
    }
}

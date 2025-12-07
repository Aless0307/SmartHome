using UnityEngine;

/// <summary>
/// Script principal de inicialización del Smart Home en Unity
/// Debe ser el primer script en ejecutarse
/// </summary>
[DefaultExecutionOrder(-100)]
public class SmartHomeApp : MonoBehaviour
{
    [Header("Configuración del Servidor")]
    [Tooltip("IP del servidor Java")]
    public string serverIP = "127.0.0.1";
    
    [Tooltip("Puerto TCP del servidor")]
    public int serverPort = 5000;
    
    [Header("Configuración de Usuario")]
    public string defaultUsername = "admin";
    public string defaultPassword = "admin123";
    
    [Header("Auto-Conexión")]
    public bool autoConnect = false;
    public float autoConnectDelay = 1f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    // Singleton
    public static SmartHomeApp Instance { get; private set; }
    
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
        
        Application.runInBackground = true;
        
        if (showDebugLogs)
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("   🏠 Smart Home Unity Client");
            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"   Servidor: {serverIP}:{serverPort}");
            Debug.Log("═══════════════════════════════════════");
        }
    }
    
    void Start()
    {
        // Configurar cliente
        if (SmartHomeClient.Instance != null)
        {
            SmartHomeClient.Instance.serverIP = serverIP;
            SmartHomeClient.Instance.serverPort = serverPort;
            SmartHomeClient.Instance.username = defaultUsername;
            SmartHomeClient.Instance.password = defaultPassword;
        }
        
        // Auto-conectar si está habilitado
        if (autoConnect)
        {
            Invoke(nameof(AutoConnect), autoConnectDelay);
        }
    }
    
    private void AutoConnect()
    {
        SmartHomeClient.Instance?.Connect();
    }
    
    void OnApplicationQuit()
    {
        SmartHomeClient.Instance?.Disconnect();
    }
    
    /// <summary>
    /// Conectar al servidor
    /// </summary>
    public void Connect()
    {
        SmartHomeClient.Instance?.Connect();
    }
    
    /// <summary>
    /// Desconectar del servidor
    /// </summary>
    public void Disconnect()
    {
        SmartHomeClient.Instance?.Disconnect();
    }
    
    /// <summary>
    /// Verificar si está conectado
    /// </summary>
    public bool IsConnected => SmartHomeClient.Instance?.isConnected ?? false;
    
    /// <summary>
    /// Verificar si está logueado
    /// </summary>
    public bool IsLoggedIn => SmartHomeClient.Instance?.isLoggedIn ?? false;
}

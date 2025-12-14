using UnityEngine;
using Unity.RenderStreaming;

/// <summary>
/// Script simple para probar Unity Render Streaming
/// Este script solo verifica que los componentes estén funcionando
/// </summary>
public class SimpleStreamTest : MonoBehaviour
{
    [Header("Componentes de Render Streaming")]
    public SignalingManager signalingManager;
    public Broadcast broadcast;
    public VideoStreamSender videoStreamSender;
    
    [Header("Configuración de prueba")]
    public Camera testCamera;
    
    [Header("Estado")]
    [SerializeField] private bool signalingConnected = false;
    [SerializeField] private int connectedClients = 0;
    
    void Start()
    {
        Debug.Log("========================================");
        Debug.Log("[SimpleStreamTest] 🚀 Iniciando prueba de streaming...");
        Debug.Log("========================================");
        
        // Buscar componentes si no están asignados
        if (signalingManager == null)
            signalingManager = FindFirstObjectByType<SignalingManager>();
        
        if (broadcast == null)
            broadcast = FindFirstObjectByType<Broadcast>();
            
        if (videoStreamSender == null)
            videoStreamSender = FindFirstObjectByType<VideoStreamSender>();
        
        // Verificar componentes
        VerifyComponents();
        
        // Si no hay cámara de prueba, usar la principal
        if (testCamera == null)
            testCamera = Camera.main;
            
        // Configurar la cámara del video sender
        ConfigureVideoSender();
    }
    
    void VerifyComponents()
    {
        Debug.Log("--- Verificación de Componentes ---");
        
        if (signalingManager != null)
        {
            Debug.Log($"✅ SignalingManager encontrado");
        }
        else
        {
            Debug.LogError("❌ SignalingManager NO encontrado - Asegúrate de agregar el componente");
        }
        
        if (broadcast != null)
        {
            Debug.Log($"✅ Broadcast encontrado");
        }
        else
        {
            Debug.LogError("❌ Broadcast NO encontrado - Asegúrate de agregar el componente");
        }
        
        if (videoStreamSender != null)
        {
            Debug.Log($"✅ VideoStreamSender encontrado");
        }
        else
        {
            Debug.LogError("❌ VideoStreamSender NO encontrado - Asegúrate de agregar el componente");
        }
        
        Debug.Log("-----------------------------------");
    }
    
    void ConfigureVideoSender()
    {
        if (videoStreamSender == null || testCamera == null) return;
        
        Debug.Log($"[SimpleStreamTest] Configurando VideoStreamSender con cámara: {testCamera.name}");
        
        // El VideoStreamSender debería configurarse desde el Inspector
        // pero podemos verificar su estado
        Debug.Log($"[SimpleStreamTest] VideoStreamSender.enabled = {videoStreamSender.enabled}");
    }
    
    void Update()
    {
        // Monitorear el estado del broadcast
        if (broadcast != null)
        {
            int clients = 0;
            var streams = broadcast.Streams;
            if (streams != null)
            {
                foreach (var s in streams)
                    clients++;
            }
            
            if (clients != connectedClients)
            {
                connectedClients = clients;
                Debug.Log($"[SimpleStreamTest] 👥 Clientes conectados: {connectedClients}");
            }
        }
    }
    
    void OnGUI()
    {
        // Mostrar información en pantalla
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.yellow;
        
        GUI.Label(new Rect(10, 10, 500, 30), "=== Stream Test ===", style);
        
        style.normal.textColor = signalingManager != null ? Color.green : Color.red;
        GUI.Label(new Rect(10, 40, 500, 30), $"SignalingManager: {(signalingManager != null ? "OK" : "MISSING")}", style);
        
        style.normal.textColor = broadcast != null ? Color.green : Color.red;
        GUI.Label(new Rect(10, 70, 500, 30), $"Broadcast: {(broadcast != null ? "OK" : "MISSING")}", style);
        
        style.normal.textColor = videoStreamSender != null ? Color.green : Color.red;
        GUI.Label(new Rect(10, 100, 500, 30), $"VideoStreamSender: {(videoStreamSender != null ? "OK" : "MISSING")}", style);
        
        style.normal.textColor = Color.cyan;
        GUI.Label(new Rect(10, 130, 500, 30), $"Clientes: {connectedClients}", style);
        
        GUI.Label(new Rect(10, 170, 600, 30), "Abre http://127.0.0.1:8888/receiver/ en el browser", style);
    }
}

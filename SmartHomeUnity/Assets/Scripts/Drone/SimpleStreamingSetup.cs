using UnityEngine;
using Unity.RenderStreaming;

/// <summary>
/// Configuración simple de streaming usando la API pública de Unity Render Streaming.
/// Añadir este script a un GameObject vacío.
/// </summary>
public class SimpleStreamingSetup : MonoBehaviour
{
    [Header("Componentes (se buscan automáticamente si están vacíos)")]
    public SignalingManager signalingManager;
    public Broadcast broadcast;
    
    [Header("Estado")]
    [SerializeField] private bool isRunning = false;
    [SerializeField] private int connectionCount = 0;
    
    void Start()
    {
        // Buscar componentes si no están asignados
        if (signalingManager == null)
            signalingManager = FindFirstObjectByType<SignalingManager>();
        
        if (broadcast == null)
            broadcast = FindFirstObjectByType<Broadcast>();
        
        if (signalingManager == null)
        {
            Debug.LogError("[SimpleStreaming] ❌ No se encontró SignalingManager");
            return;
        }
        
        if (broadcast == null)
        {
            Debug.LogError("[SimpleStreaming] ❌ No se encontró Broadcast");
            return;
        }
        
        Debug.Log("[SimpleStreaming] ✅ Componentes encontrados");
        Debug.Log($"[SimpleStreaming] SignalingManager: {signalingManager.gameObject.name}");
        Debug.Log($"[SimpleStreaming] Broadcast: {broadcast.gameObject.name}");
        
        // Suscribirse a eventos del Broadcast
        broadcast.OnAddReceiver += OnReceiverAdded;
        
        // El SignalingManager debería iniciarse automáticamente si está configurado
        // con "Run On Awake" en el Inspector
    }
    
    void OnReceiverAdded(string connectionId)
    {
        connectionCount++;
        Debug.Log($"[SimpleStreaming] 🎉 ¡NUEVO RECEPTOR CONECTADO! ID: {connectionId}");
        Debug.Log($"[SimpleStreaming] Total conexiones: {connectionCount}");
    }
    
    void Update()
    {
        // Verificar estado cada 5 segundos
        if (Time.frameCount % 300 == 0)
        {
            // Usar reflexión para verificar si está corriendo
            var runningField = typeof(SignalingManager).GetField("m_running", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (runningField != null)
            {
                isRunning = (bool)runningField.GetValue(signalingManager);
            }
            
            Debug.Log($"[SimpleStreaming] 📊 Running: {isRunning}, Conexiones: {connectionCount}");
        }
    }
    
    void OnGUI()
    {
        // Panel de estado simple
        GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 310, 200));
        GUILayout.BeginVertical("box");
        
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        GUILayout.Label("📡 STREAMING STATUS", titleStyle);
        
        GUIStyle greenStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green } };
        GUIStyle redStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };
        
        GUILayout.Label($"SignalingManager: {(signalingManager != null ? "✅" : "❌")}");
        GUILayout.Label($"Broadcast: {(broadcast != null ? "✅" : "❌")}");
        GUILayout.Label($"Running: {(isRunning ? "✅ YES" : "❌ NO")}", isRunning ? greenStyle : redStyle);
        GUILayout.Label($"Conexiones: {connectionCount}");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔄 Forzar Inicio"))
        {
            ForceStart();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    void ForceStart()
    {
        if (signalingManager == null || broadcast == null)
        {
            Debug.LogError("[SimpleStreaming] Faltan componentes");
            return;
        }
        
        Debug.Log("[SimpleStreaming] 🚀 Forzando inicio...");
        
        // Detener primero
        signalingManager.Stop();
        
        // Esperar un poco y luego iniciar
        Invoke(nameof(DoStart), 0.5f);
    }
    
    void DoStart()
    {
        // Iniciar el SignalingManager - esto usará la configuración del Inspector
        signalingManager.Run();
        Debug.Log("[SimpleStreaming] ✅ SignalingManager.Run() llamado");
    }
    
    void OnDestroy()
    {
        if (broadcast != null)
        {
            broadcast.OnAddReceiver -= OnReceiverAdded;
        }
    }
}

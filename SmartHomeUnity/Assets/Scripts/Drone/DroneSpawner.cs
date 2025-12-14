using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona el spawn de drones cuando nuevos clientes se conectan
/// Se integra con Unity Render Streaming para crear un dron por cliente
/// </summary>
public class DroneSpawner : MonoBehaviour
{
    [Header("Drone Prefab")]
    [Tooltip("Prefab del dron que se instanciará por cada cliente")]
    [SerializeField] private GameObject dronePrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Vector3 defaultSpawnPosition = new Vector3(0, 5, 0);
    [SerializeField] private float spawnRadius = 5f;
    [SerializeField] private float spawnHeight = 5f;
    
    [Header("Drone Colors")]
    [SerializeField] private Color[] droneColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        new Color(1f, 0.5f, 0f), // Orange
        new Color(0.5f, 0f, 1f)  // Purple
    };
    
    [Header("Boundaries")]
    [SerializeField] private Vector3 minBounds = new Vector3(-100, 1, -100);
    [SerializeField] private Vector3 maxBounds = new Vector3(100, 50, 100);
    
    // Diccionario de drones activos por ID de conexión
    private Dictionary<string, DroneInstance> activeDrones = new Dictionary<string, DroneInstance>();
    private int colorIndex = 0;
    
    // Singleton para acceso fácil
    public static DroneSpawner Instance { get; private set; }
    
    // Clase para almacenar información del dron
    [System.Serializable]
    public class DroneInstance
    {
        public string connectionId;
        public GameObject droneObject;
        public DroneController controller;
        public Camera droneCamera;
        public Color color;
        public System.DateTime spawnTime;
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (dronePrefab == null)
        {
            Debug.LogError("[DroneSpawner] ¡No hay prefab de dron asignado!");
        }
        
        Debug.Log("[DroneSpawner] Sistema de spawn de drones inicializado");
    }
    
    /// <summary>
    /// Crear un nuevo dron para un cliente
    /// </summary>
    /// <param name="connectionId">ID único de la conexión del cliente</param>
    /// <returns>La instancia del dron creado</returns>
    public DroneInstance SpawnDrone(string connectionId)
    {
        if (activeDrones.ContainsKey(connectionId))
        {
            Debug.LogWarning($"[DroneSpawner] Ya existe un dron para conexión: {connectionId}");
            return activeDrones[connectionId];
        }
        
        if (dronePrefab == null)
        {
            Debug.LogError("[DroneSpawner] No se puede crear dron - prefab no asignado");
            return null;
        }
        
        // Calcular posición de spawn
        Vector3 spawnPos = GetSpawnPosition();
        
        // Instanciar dron
        GameObject droneObj = Instantiate(dronePrefab, spawnPos, Quaternion.identity);
        droneObj.name = $"Drone_{connectionId}";
        
        // Configurar controlador
        DroneController controller = droneObj.GetComponent<DroneController>();
        if (controller == null)
        {
            controller = droneObj.AddComponent<DroneController>();
        }
        
        // Asignar color único
        Color droneColor = GetNextColor();
        controller.SetOwner(connectionId);
        controller.SetDroneColor(droneColor);
        controller.SetBoundaries(minBounds, maxBounds);
        
        // Obtener cámara
        Camera droneCamera = droneObj.GetComponentInChildren<Camera>();
        if (droneCamera == null)
        {
            // Crear cámara si no existe
            GameObject camObj = new GameObject("DroneCamera");
            camObj.transform.SetParent(droneObj.transform);
            camObj.transform.localPosition = new Vector3(0, 0.5f, -1f);
            camObj.transform.localRotation = Quaternion.Euler(15, 0, 0);
            droneCamera = camObj.AddComponent<Camera>();
            droneCamera.fieldOfView = 90;
            droneCamera.nearClipPlane = 0.1f;
            droneCamera.farClipPlane = 1000f;
        }
        
        // Desactivar AudioListener si hay más de un dron
        AudioListener audioListener = droneObj.GetComponentInChildren<AudioListener>();
        if (audioListener != null && activeDrones.Count > 0)
        {
            audioListener.enabled = false;
        }
        
        // Crear instancia
        DroneInstance instance = new DroneInstance
        {
            connectionId = connectionId,
            droneObject = droneObj,
            controller = controller,
            droneCamera = droneCamera,
            color = droneColor,
            spawnTime = System.DateTime.Now
        };
        
        activeDrones[connectionId] = instance;
        
        Debug.Log($"[DroneSpawner] Dron creado para {connectionId} - Color: {droneColor} - Total drones: {activeDrones.Count}");
        
        // Notificar a otros sistemas
        OnDroneSpawned?.Invoke(instance);
        
        return instance;
    }
    
    /// <summary>
    /// Destruir el dron de un cliente que se desconectó
    /// </summary>
    public void DespawnDrone(string connectionId)
    {
        if (!activeDrones.ContainsKey(connectionId))
        {
            Debug.LogWarning($"[DroneSpawner] No existe dron para conexión: {connectionId}");
            return;
        }
        
        DroneInstance instance = activeDrones[connectionId];
        
        // Notificar antes de destruir
        OnDroneDespawned?.Invoke(instance);
        
        // Destruir objeto
        if (instance.droneObject != null)
        {
            Destroy(instance.droneObject);
        }
        
        activeDrones.Remove(connectionId);
        
        Debug.Log($"[DroneSpawner] Dron destruido para {connectionId} - Total drones: {activeDrones.Count}");
    }
    
    /// <summary>
    /// Obtener el dron de un cliente específico
    /// </summary>
    public DroneInstance GetDrone(string connectionId)
    {
        if (activeDrones.TryGetValue(connectionId, out DroneInstance instance))
        {
            return instance;
        }
        return null;
    }
    
    /// <summary>
    /// Obtener todos los drones activos
    /// </summary>
    public Dictionary<string, DroneInstance> GetAllDrones()
    {
        return new Dictionary<string, DroneInstance>(activeDrones);
    }
    
    /// <summary>
    /// Obtener el número de drones activos
    /// </summary>
    public int GetDroneCount()
    {
        return activeDrones.Count;
    }
    
    /// <summary>
    /// Calcular posición de spawn
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        // Si hay spawn points definidos, usar uno aleatorio
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = activeDrones.Count % spawnPoints.Length;
            return spawnPoints[index].position;
        }
        
        // Si no, calcular posición en círculo alrededor del punto default
        float angle = activeDrones.Count * (360f / 8f) * Mathf.Deg2Rad;
        float x = defaultSpawnPosition.x + Mathf.Cos(angle) * spawnRadius;
        float z = defaultSpawnPosition.z + Mathf.Sin(angle) * spawnRadius;
        
        return new Vector3(x, spawnHeight, z);
    }
    
    /// <summary>
    /// Obtener el siguiente color de la lista
    /// </summary>
    private Color GetNextColor()
    {
        Color color = droneColors[colorIndex % droneColors.Length];
        colorIndex++;
        return color;
    }
    
    /// <summary>
    /// Respawnear un dron en una nueva posición
    /// </summary>
    public void RespawnDrone(string connectionId)
    {
        if (activeDrones.TryGetValue(connectionId, out DroneInstance instance))
        {
            Vector3 newPos = GetSpawnPosition();
            instance.controller.Teleport(newPos);
            Debug.Log($"[DroneSpawner] Dron {connectionId} respawneado en {newPos}");
        }
    }
    
    /// <summary>
    /// Destruir todos los drones
    /// </summary>
    public void DespawnAllDrones()
    {
        List<string> keys = new List<string>(activeDrones.Keys);
        foreach (string key in keys)
        {
            DespawnDrone(key);
        }
        colorIndex = 0;
    }
    
    // Eventos
    public delegate void DroneEvent(DroneInstance drone);
    public event DroneEvent OnDroneSpawned;
    public event DroneEvent OnDroneDespawned;
    
    void OnDestroy()
    {
        DespawnAllDrones();
    }
    
    /// <summary>
    /// Visualizar spawn points en el editor
    /// </summary>
    void OnDrawGizmos()
    {
        // Dibujar punto de spawn default
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(defaultSpawnPosition, 1f);
        
        // Dibujar radio de spawn
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        DrawCircle(defaultSpawnPosition, spawnRadius, 32);
        
        // Dibujar límites
        Gizmos.color = Color.yellow;
        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(center, size);
        
        // Dibujar spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                }
            }
        }
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}

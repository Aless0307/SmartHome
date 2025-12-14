using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador de la casa 3D
/// Gestiona las habitaciones y la visualización
/// </summary>
public class HouseController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform roomsContainer;
    public Camera mainCamera;
    
    [Header("Habitaciones")]
    public List<RoomController> rooms = new List<RoomController>();
    
    [Header("Configuración")]
    public bool autoCreateRooms = true;
    public Vector3 roomSize = new Vector3(5, 3, 5);
    public Material wallMaterial;
    public Material floorMaterial;
    
    // Singleton
    public static HouseController Instance { get; private set; }
    
    // Diccionario de habitaciones por nombre
    private Dictionary<string, RoomController> roomsByName = new Dictionary<string, RoomController>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Suscribirse a eventos
        if (DeviceManager.Instance != null)
        {
            DeviceManager.Instance.OnDevicesLoaded.AddListener(OnDevicesLoaded);
        }
        
        // Indexar habitaciones existentes
        foreach (var room in rooms)
        {
            if (room != null)
            {
                roomsByName[room.roomName.ToLower()] = room;
            }
        }
    }
    
    void OnDestroy()
    {
        if (DeviceManager.Instance != null)
        {
            DeviceManager.Instance.OnDevicesLoaded.RemoveListener(OnDevicesLoaded);
        }
    }
    
    /// <summary>
    /// Manejar dispositivos cargados
    /// </summary>
    private void OnDevicesLoaded(List<DeviceData> devices)
    {
        if (!autoCreateRooms) return;
        
        // Obtener habitaciones únicas
        HashSet<string> roomNames = new HashSet<string>();
        foreach (var device in devices)
        {
            if (!string.IsNullOrEmpty(device.room))
            {
                roomNames.Add(device.room);
            }
        }
        
        // Crear habitaciones que no existen
        int index = 0;
        foreach (string roomName in roomNames)
        {
            if (!roomsByName.ContainsKey(roomName.ToLower()))
            {
                CreateRoom(roomName, GetRoomPosition(index));
                index++;
            }
        }
        
        // Asignar dispositivos a habitaciones
        foreach (var device in devices)
        {
            AssignDeviceToRoom(device);
        }
    }
    
    /// <summary>
    /// Crear una habitación
    /// </summary>
    public RoomController CreateRoom(string name, Vector3 position)
    {
        GameObject roomObj = new GameObject($"Room_{name}");
        roomObj.transform.SetParent(roomsContainer != null ? roomsContainer : transform);
        roomObj.transform.localPosition = position;
        
        RoomController room = roomObj.AddComponent<RoomController>();
        room.roomName = name;
        room.roomSize = roomSize;
        room.Initialize();
        
        rooms.Add(room);
        roomsByName[name.ToLower()] = room;
        
        Debug.Log($"🏠 Creada habitación: {name}");
        return room;
    }
    
    /// <summary>
    /// Calcular posición de habitación
    /// </summary>
    private Vector3 GetRoomPosition(int index)
    {
        int cols = 3;
        int row = index / cols;
        int col = index % cols;
        
        return new Vector3(
            col * (roomSize.x + 1),
            0,
            row * (roomSize.z + 1)
        );
    }
    
    /// <summary>
    /// Obtener habitación por nombre
    /// </summary>
    public RoomController GetRoom(string name)
    {
        string key = name.ToLower();
        return roomsByName.ContainsKey(key) ? roomsByName[key] : null;
    }
    
    /// <summary>
    /// Asignar dispositivo a su habitación
    /// </summary>
    public void AssignDeviceToRoom(DeviceData device)
    {
        if (string.IsNullOrEmpty(device.room)) return;
        
        var room = GetRoom(device.room);
        if (room != null)
        {
            room.AddDevice(device);
        }
    }
    
    /// <summary>
    /// Enfocar cámara en una habitación
    /// </summary>
    public void FocusRoom(string roomName)
    {
        var room = GetRoom(roomName);
        if (room != null && mainCamera != null)
        {
            Vector3 targetPos = room.transform.position + Vector3.up * 10 + Vector3.back * 5;
            mainCamera.transform.position = targetPos;
            mainCamera.transform.LookAt(room.transform.position);
        }
    }
}

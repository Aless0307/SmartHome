using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador de una habitación
/// Gestiona los dispositivos visuales dentro de la habitación
/// </summary>
public class RoomController : MonoBehaviour
{
    [Header("Configuración")]
    public string roomName = "Room";
    public Vector3 roomSize = new Vector3(5, 3, 5);
    
    [Header("Referencias")]
    public Transform devicesContainer;
    public Light roomLight;
    public MeshRenderer floorRenderer;
    
    [Header("Prefabs")]
    public GameObject lightPrefab;
    public GameObject thermostatPrefab;
    public GameObject doorPrefab;
    public GameObject cameraPrefab;
    public GameObject sensorPrefab;
    
    [Header("Estado")]
    public bool isLightOn = false;
    public Color lightColor = Color.white;
    public float lightIntensity = 1f;
    
    // Dispositivos en la habitación
    private List<DeviceData> devices = new List<DeviceData>();
    private Dictionary<string, DeviceVisual> deviceVisuals = new Dictionary<string, DeviceVisual>();
    
    /// <summary>
    /// Inicializar la habitación
    /// </summary>
    public void Initialize()
    {
        // Crear contenedor de dispositivos
        if (devicesContainer == null)
        {
            GameObject container = new GameObject("Devices");
            container.transform.SetParent(transform);
            container.transform.localPosition = Vector3.zero;
            devicesContainer = container.transform;
        }
        
        // Crear estructura básica de la habitación
        CreateRoomStructure();
        
        // Crear luz de la habitación
        if (roomLight == null)
        {
            CreateRoomLight();
        }
        
        // Suscribirse a eventos
        if (DeviceManager.Instance != null)
        {
            DeviceManager.Instance.OnDeviceUpdated.AddListener(OnDeviceUpdated);
        }
    }
    
    void OnDestroy()
    {
        if (DeviceManager.Instance != null)
        {
            DeviceManager.Instance.OnDeviceUpdated.RemoveListener(OnDeviceUpdated);
        }
    }
    
    /// <summary>
    /// Crear estructura visual de la habitación
    /// </summary>
    private void CreateRoomStructure()
    {
        // Crear piso
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(transform);
        floor.transform.localPosition = new Vector3(0, -0.1f, 0);
        floor.transform.localScale = new Vector3(roomSize.x, 0.2f, roomSize.z);
        
        var floorMat = new Material(Shader.Find("Standard"));
        floorMat.color = new Color(0.6f, 0.5f, 0.4f); // Color madera
        floor.GetComponent<MeshRenderer>().material = floorMat;
        
        // Crear paredes (semi-transparentes para ver el interior)
        CreateWall("WallNorth", new Vector3(0, roomSize.y / 2, roomSize.z / 2), 
                   new Vector3(roomSize.x, roomSize.y, 0.1f));
        CreateWall("WallSouth", new Vector3(0, roomSize.y / 2, -roomSize.z / 2), 
                   new Vector3(roomSize.x, roomSize.y, 0.1f));
        CreateWall("WallEast", new Vector3(roomSize.x / 2, roomSize.y / 2, 0), 
                   new Vector3(0.1f, roomSize.y, roomSize.z));
        CreateWall("WallWest", new Vector3(-roomSize.x / 2, roomSize.y / 2, 0), 
                   new Vector3(0.1f, roomSize.y, roomSize.z));
        
        // Crear etiqueta con nombre
        CreateRoomLabel();
    }
    
    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(transform);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        
        var wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = new Color(0.9f, 0.9f, 0.85f, 0.5f); // Beige semi-transparente
        
        // Hacer semi-transparente
        wallMat.SetFloat("_Mode", 3);
        wallMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        wallMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        wallMat.SetInt("_ZWrite", 0);
        wallMat.DisableKeyword("_ALPHATEST_ON");
        wallMat.EnableKeyword("_ALPHABLEND_ON");
        wallMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        wallMat.renderQueue = 3000;
        
        wall.GetComponent<MeshRenderer>().material = wallMat;
    }
    
    private void CreateRoomLight()
    {
        GameObject lightObj = new GameObject("RoomLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = new Vector3(0, roomSize.y - 0.5f, 0);
        
        roomLight = lightObj.AddComponent<Light>();
        roomLight.type = LightType.Point;
        roomLight.color = Color.white;
        roomLight.intensity = 0;
        roomLight.range = roomSize.magnitude;
    }
    
    private void CreateRoomLabel()
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0, roomSize.y + 0.5f, 0);
        
        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = roomName;
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.1f;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = Color.white;
    }
    
    /// <summary>
    /// Agregar dispositivo a la habitación
    /// </summary>
    public void AddDevice(DeviceData device)
    {
        if (devices.Exists(d => d.id == device.id)) return;
        
        devices.Add(device);
        CreateDeviceVisual(device);
        UpdateRoomState();
    }
    
    /// <summary>
    /// Crear visual de un dispositivo
    /// </summary>
    private void CreateDeviceVisual(DeviceData device)
    {
        GameObject visualObj = null;
        Vector3 position = GetDevicePosition(device.type, devices.Count - 1);
        
        // Crear objeto según tipo
        switch (device.type?.ToLower())
        {
            case "light":
                visualObj = CreateLightVisual(device, position);
                break;
            case "thermostat":
                visualObj = CreateThermostatVisual(device, position);
                break;
            case "door":
                visualObj = CreateDoorVisual(device, position);
                break;
            case "camera":
                visualObj = CreateCameraVisual(device, position);
                break;
            case "sensor":
                visualObj = CreateSensorVisual(device, position);
                break;
            default:
                visualObj = CreateDefaultVisual(device, position);
                break;
        }
        
        if (visualObj != null)
        {
            DeviceVisual visual = visualObj.GetComponent<DeviceVisual>();
            if (visual == null)
            {
                visual = visualObj.AddComponent<DeviceVisual>();
            }
            visual.SetDevice(device);
            deviceVisuals[device.id] = visual;
        }
    }
    
    private Vector3 GetDevicePosition(string type, int index)
    {
        switch (type?.ToLower())
        {
            case "light":
                return new Vector3(0, roomSize.y - 0.3f, 0);
            case "thermostat":
                return new Vector3(roomSize.x / 2 - 0.2f, roomSize.y / 2, 0);
            case "door":
                return new Vector3(0, roomSize.y / 2, -roomSize.z / 2);
            case "camera":
                return new Vector3(roomSize.x / 2 - 0.3f, roomSize.y - 0.3f, roomSize.z / 2 - 0.3f);
            case "sensor":
                return new Vector3(-roomSize.x / 2 + 0.3f, 0.5f, -roomSize.z / 2 + 0.3f);
            default:
                return new Vector3(0, 0.5f, 0);
        }
    }
    
    private GameObject CreateLightVisual(DeviceData device, Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = $"Light_{device.name}";
        obj.transform.SetParent(devicesContainer);
        obj.transform.localPosition = position;
        obj.transform.localScale = Vector3.one * 0.3f;
        
        // Material emisivo
        var mat = new Material(Shader.Find("Standard"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", device.status ? Color.yellow : Color.gray);
        mat.color = device.status ? Color.yellow : Color.gray;
        obj.GetComponent<MeshRenderer>().material = mat;
        
        // Agregar luz puntual
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Color.yellow;
        light.intensity = device.status ? 1f : 0f;
        light.range = 5f;
        
        return obj;
    }
    
    private GameObject CreateThermostatVisual(DeviceData device, Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"Thermostat_{device.name}";
        obj.transform.SetParent(devicesContainer);
        obj.transform.localPosition = position;
        obj.transform.localScale = new Vector3(0.3f, 0.4f, 0.05f);
        
        var mat = new Material(Shader.Find("Standard"));
        mat.color = device.status ? new Color(0.2f, 0.6f, 1f) : Color.gray;
        obj.GetComponent<MeshRenderer>().material = mat;
        
        return obj;
    }
    
    private GameObject CreateDoorVisual(DeviceData device, Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"Door_{device.name}";
        obj.transform.SetParent(devicesContainer);
        obj.transform.localPosition = position;
        obj.transform.localScale = new Vector3(1f, 2f, 0.1f);
        
        var mat = new Material(Shader.Find("Standard"));
        mat.color = device.status ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.6f, 0.3f, 0.2f);
        obj.GetComponent<MeshRenderer>().material = mat;
        
        return obj;
    }
    
    private GameObject CreateCameraVisual(DeviceData device, Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = $"Camera_{device.name}";
        obj.transform.SetParent(devicesContainer);
        obj.transform.localPosition = position;
        obj.transform.localScale = new Vector3(0.15f, 0.1f, 0.15f);
        obj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        
        var mat = new Material(Shader.Find("Standard"));
        mat.color = device.status ? Color.green : Color.gray;
        obj.GetComponent<MeshRenderer>().material = mat;
        
        return obj;
    }
    
    private GameObject CreateSensorVisual(DeviceData device, Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        obj.name = $"Sensor_{device.name}";
        obj.transform.SetParent(devicesContainer);
        obj.transform.localPosition = position;
        obj.transform.localScale = Vector3.one * 0.2f;
        
        var mat = new Material(Shader.Find("Standard"));
        mat.color = device.status ? Color.cyan : Color.gray;
        obj.GetComponent<MeshRenderer>().material = mat;
        
        return obj;
    }
    
    private GameObject CreateDefaultVisual(DeviceData device, Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"Device_{device.name}";
        obj.transform.SetParent(devicesContainer);
        obj.transform.localPosition = position;
        obj.transform.localScale = Vector3.one * 0.3f;
        
        var mat = new Material(Shader.Find("Standard"));
        mat.color = device.status ? Color.green : Color.gray;
        obj.GetComponent<MeshRenderer>().material = mat;
        
        return obj;
    }
    
    /// <summary>
    /// Manejar actualización de dispositivo
    /// </summary>
    private void OnDeviceUpdated(DeviceData device)
    {
        if (device.room?.ToLower() != roomName.ToLower()) return;
        
        // Actualizar visual
        if (deviceVisuals.ContainsKey(device.id))
        {
            deviceVisuals[device.id].UpdateDevice(device);
        }
        
        // Actualizar dispositivo en lista
        int index = devices.FindIndex(d => d.id == device.id);
        if (index >= 0)
        {
            devices[index] = device;
        }
        
        UpdateRoomState();
    }
    
    /// <summary>
    /// Actualizar estado general de la habitación
    /// </summary>
    private void UpdateRoomState()
    {
        // Actualizar luz de la habitación basándose en las luces
        bool hasLightOn = false;
        Color avgColor = Color.white;
        
        foreach (var device in devices)
        {
            if (device.type == "light" && device.status)
            {
                hasLightOn = true;
                if (!string.IsNullOrEmpty(device.color))
                {
                    Color c;
                    if (ColorUtility.TryParseHtmlString(device.color, out c))
                    {
                        avgColor = c;
                    }
                }
                break;
            }
        }
        
        isLightOn = hasLightOn;
        lightColor = avgColor;
        
        if (roomLight != null)
        {
            roomLight.intensity = isLightOn ? lightIntensity : 0;
            roomLight.color = lightColor;
        }
    }
    
    /// <summary>
    /// Obtener todos los dispositivos de la habitación
    /// </summary>
    public List<DeviceData> GetDevices()
    {
        return new List<DeviceData>(devices);
    }
}

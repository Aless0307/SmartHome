using UnityEngine;
using Unity.RenderStreaming;
using Unity.WebRTC;
using System;
using System.Collections;

/// <summary>
/// Componente simple que recibe input para el dron
/// Se añade al mismo GameObject que el Broadcast/SignalingManager
/// </summary>
public class SimpleDroneInput : MonoBehaviour
{
    [Header("Referencia al Dron")]
    public DroneController targetDrone;
    
    [Header("Configuración")]
    public bool useKeyboardFallback = true;
    public KeyCode upKey = KeyCode.Space;
    public KeyCode downKey = KeyCode.LeftShift;
    
    // Estado actual
    private float h, v, e, r;
    private int frameCount;
    private bool receivingWebRTC = false;
    
    void Start()
    {
        if (targetDrone == null)
        {
            targetDrone = FindFirstObjectByType<DroneController>();
        }
        
        Debug.Log($"[SimpleDroneInput] Iniciado. Dron: {(targetDrone != null ? targetDrone.name : "NO ENCONTRADO")}");
        
        // Si hay teclado disponible, permitir control local para testing
        if (useKeyboardFallback)
        {
            Debug.Log("[SimpleDroneInput] Control por teclado habilitado: WASD + QE + Space/Shift");
        }
    }
    
    void Update()
    {
        // Fallback: Control por teclado local para testing
        if (useKeyboardFallback && !receivingWebRTC)
        {
            float keyH = 0, keyV = 0, keyE = 0, keyR = 0;
            
            // WASD para movimiento
            if (Input.GetKey(KeyCode.W)) keyV = 1;
            if (Input.GetKey(KeyCode.S)) keyV = -1;
            if (Input.GetKey(KeyCode.A)) keyH = -1;
            if (Input.GetKey(KeyCode.D)) keyH = 1;
            
            // QE para rotación
            if (Input.GetKey(KeyCode.Q)) keyR = -1;
            if (Input.GetKey(KeyCode.E)) keyR = 1;
            
            // Space/Shift para elevación
            if (Input.GetKey(upKey)) keyE = 1;
            if (Input.GetKey(downKey)) keyE = -1;
            
            if (keyH != 0 || keyV != 0 || keyE != 0 || keyR != 0)
            {
                ApplyInput(keyH, keyV, keyE, keyR);
            }
        }
    }
    
    /// <summary>
    /// Llamar este método desde cualquier fuente de input (WebRTC, WebSocket, etc.)
    /// </summary>
    public void ApplyInput(float horizontal, float vertical, float elevation, float rotation)
    {
        h = horizontal;
        v = vertical;
        e = elevation;
        r = rotation;
        frameCount++;
        
        if (targetDrone != null)
        {
            targetDrone.SetInput(horizontal, vertical, elevation, rotation);
        }
    }
    
    /// <summary>
    /// Procesar mensaje JSON de input
    /// </summary>
    public void ProcessInputMessage(string json)
    {
        receivingWebRTC = true;
        
        try
        {
            if (json.Contains("\"type\":\"input\""))
            {
                float h = ExtractFloat(json, "horizontal");
                float v = ExtractFloat(json, "vertical");
                float e = ExtractFloat(json, "elevation");
                float r = ExtractFloat(json, "rotation");
                
                ApplyInput(h, v, e, r);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SimpleDroneInput] Error: {ex.Message}");
        }
    }
    
    private float ExtractFloat(string json, string key)
    {
        string searchKey = $"\"{key}\":";
        int startIndex = json.IndexOf(searchKey);
        if (startIndex == -1) return 0;
        
        startIndex += searchKey.Length;
        int endIndex = json.IndexOfAny(new char[] { ',', '}' }, startIndex);
        if (endIndex == -1) return 0;
        
        string valueStr = json.Substring(startIndex, endIndex - startIndex).Trim();
        
        if (float.TryParse(valueStr, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        
        return 0;
    }
    
    void OnGUI()
    {
        // Panel pequeño en esquina
        GUILayout.BeginArea(new Rect(10, Screen.height - 120, 200, 110));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("🚁 Dron Input", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        
        string source = receivingWebRTC ? "WebRTC" : (useKeyboardFallback ? "Teclado" : "Ninguno");
        GUILayout.Label($"Fuente: {source}");
        GUILayout.Label($"H:{h:F1} V:{v:F1}");
        GUILayout.Label($"E:{e:F1} R:{r:F1}");
        GUILayout.Label($"Frames: {frameCount}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}

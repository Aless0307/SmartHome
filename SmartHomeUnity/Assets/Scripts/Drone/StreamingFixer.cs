using UnityEngine;
using Unity.RenderStreaming;
using Unity.RenderStreaming.Signaling;
using System.Reflection;
using System.Net.WebSockets;
using System.Threading;
using System;
using System.Collections;
using System.Linq;

public class StreamingFixer : MonoBehaviour
{
    [Header("Configuración Deseada")]
    public string targetUrl = "ws://127.0.0.1:8888";
    
    private SignalingManager signalingManager;
    private string statusLog = "Listo para diagnosticar.";
    private Vector2 scrollPos;
    
    void Start()
    {
        signalingManager = FindFirstObjectByType<SignalingManager>();
        if (signalingManager == null)
        {
            Log("❌ NO se encontró SignalingManager en la escena.");
        }
        else
        {
            Log("✅ SignalingManager encontrado.");
        }
    }

    void Update()
    {
        // Atajos de teclado
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            TestRawConnection();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            StartCoroutine(ForceConfiguration());
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            RestartSignaling();
        }
    }

    void OnGUI()
    {
    }

    void Log(string msg)
    {
        Debug.Log($"[StreamingFixer] {msg}");
        statusLog += $"\n[{DateTime.Now:HH:mm:ss}] {msg}";
    }

    async void TestRawConnection()
    {
        Log($"⏳ Probando conexión raw a {targetUrl}...");
        
        using (ClientWebSocket ws = new ClientWebSocket())
        {
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await ws.ConnectAsync(new Uri(targetUrl), cts.Token);
                Log("✅ ¡ÉXITO! Unity PUEDE conectar al servidor.");
                Log("   Esto confirma que NO es un problema de red/firewall.");
                Log("   El problema está en la configuración de Unity Render Streaming.");
                
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test", CancellationToken.None);
            }
            catch (Exception e)
            {
                Log($"❌ FALLÓ la conexión raw: {e.Message}");
                Log("   Posibles causas:");
                Log("   1. El servidor no está corriendo (revisa la terminal).");
                Log("   2. Firewall bloqueando Unity.");
                Log("   3. Puerto incorrecto.");
            }
        }
    }

    IEnumerator ForceConfiguration()
    {
        if (signalingManager == null)
        {
            Log("❌ No hay SignalingManager para configurar.");
            yield break;
        }

        Log("🔧 Iniciando reconfiguración forzada...");

        // 1. Detener
        var stopMethod = typeof(SignalingManager).GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
        if (stopMethod != null) stopMethod.Invoke(signalingManager, null);
        Log("   SignalingManager detenido.");

        yield return new WaitForSeconds(0.5f);

        // 2. Modificar campos privados usando reflexión
        var type = typeof(SignalingManager);
        
        // Desactivar Use Default Settings
        var useDefaultField = type.GetField("m_useDefaultSettings", BindingFlags.NonPublic | BindingFlags.Instance);
        if (useDefaultField != null)
        {
            useDefaultField.SetValue(signalingManager, false);
            Log("   'Use Default Settings' desactivado.");
        }

        // Quitar el Asset de settings (para evitar conflictos)
        var settingsAssetField = type.GetField("m_settings", BindingFlags.NonPublic | BindingFlags.Instance);
        if (settingsAssetField != null)
        {
            settingsAssetField.SetValue(signalingManager, null);
            Log("   'Signaling Settings Asset' eliminado (set to null).");
        }

        // Configurar la clase de signaling (WebSocketSignaling)
        // Nota: Esto es complicado porque es un TypeReference o similar en el inspector, 
        // pero intentaremos instanciar directamente el objeto de signaling si es posible.
        
        // En lugar de configurar los campos serializados, vamos a inyectar directamente la instancia de signaling
        // si el manager lo permite, o configurar los campos que usa el Run().

        // Intentemos configurar los campos que se ven en el inspector via reflexión
        // m_signalingClass parece ser el nombre del campo para el tipo
        
        // Vamos a intentar crear una instancia de WebSocketSignaling y asignarla si existe un campo para ello
        // O mejor, vamos a dejar que el manager lo cree pero asegurándonos que los parámetros son correctos.

        // Buscar el campo m_webSocketSettings o similar donde se guarda la URL
        // En versiones recientes, puede estar dentro de una clase de settings.
        
        // Vamos a intentar algo más directo: Crear el WebSocketSignaling nosotros y asignarlo a m_signaling
        
        try
        {
            // Buscar el tipo WebSocketSignaling
            Type wsType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                wsType = asm.GetType("Unity.RenderStreaming.Signaling.WebSocketSignaling");
                if (wsType != null) break;
            }

            if (wsType != null)
            {
                // Debug de constructores - Imprimir en bloque para fácil lectura en consola
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"[StreamingFixer] 🔍 ANÁLISIS DE TIPO: {wsType.FullName}");
                sb.AppendLine("[StreamingFixer] Constructores disponibles:");
                
                foreach (var ctor in wsType.GetConstructors())
                {
                    string paramsStr = "";
                    var parameters = ctor.GetParameters();
                    for(int i=0; i<parameters.Length; i++)
                    {
                        paramsStr += $"{parameters[i].ParameterType.Name} {parameters[i].Name}";
                        if(i < parameters.Length - 1) paramsStr += ", ";
                    }
                    sb.AppendLine($"[StreamingFixer]    👉 ({paramsStr})");
                }
                Debug.LogWarning(sb.ToString()); // Usar Warning para que resalte en amarillo

                // Intentar crear instancia basándonos en lo que encontremos
                object wsInstance = null;

                try 
                {
                    // NUEVA ESTRATEGIA: En lugar de crear SignalingSettings desde cero,
                    // vamos a usar el que ya tiene el SignalingManager configurado y solo modificar la URL
                    
                    // Buscar el SignalingSettings que ya existe en el manager
                    var settingsField = type.GetField("m_settings", BindingFlags.NonPublic | BindingFlags.Instance);
                    object existingSettings = settingsField?.GetValue(signalingManager);
                    
                    if (existingSettings != null)
                    {
                        Log("   ✅ Usando SignalingSettings existente del Manager.");
                        
                        // Modificar la URL en el settings existente
                        var settingsType = existingSettings.GetType();
                        var urlField = settingsType.GetField("m_url", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (urlField != null)
                        {
                            urlField.SetValue(existingSettings, targetUrl);
                            Log($"   ✅ URL actualizada a: {targetUrl}");
                        }
                        
                        // Crear WebSocketSignaling con los settings existentes
                        wsInstance = Activator.CreateInstance(wsType, new object[] { existingSettings, SynchronizationContext.Current });
                        Log("   ✅ WebSocketSignaling creado con settings existentes.");
                    }
                    else
                    {
                        // Si no hay settings, intentar cargar el asset configurado en Project Settings
                        Log("   ⚠️ No hay settings en el manager, buscando en Project Settings...");
                        
                        // Buscar RenderStreamingSettings (el singleton de Project Settings)
                        Type rsSettingsType = null;
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            rsSettingsType = asm.GetType("Unity.RenderStreaming.RenderStreamingSettings");
                            if (rsSettingsType != null) break;
                        }
                        
                        if (rsSettingsType != null)
                        {
                            // Obtener la instancia singleton
                            var instanceProp = rsSettingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                            if (instanceProp != null)
                            {
                                var rsSettings = instanceProp.GetValue(null);
                                if (rsSettings != null)
                                {
                                    // Obtener SignalingSettings del RenderStreamingSettings
                                    var sigSettingsProp = rsSettingsType.GetProperty("signalingSettings") ?? 
                                                          rsSettingsType.GetProperty("SignalingSettings");
                                    if (sigSettingsProp != null)
                                    {
                                        var sigSettings = sigSettingsProp.GetValue(rsSettings);
                                        if (sigSettings != null)
                                        {
                                            Log("   ✅ SignalingSettings obtenido de Project Settings.");
                                            
                                            // Modificar URL
                                            var urlField = sigSettings.GetType().GetField("m_url", BindingFlags.NonPublic | BindingFlags.Instance);
                                            if (urlField != null)
                                            {
                                                urlField.SetValue(sigSettings, targetUrl);
                                                Log($"   ✅ URL actualizada a: {targetUrl}");
                                            }
                                            
                                            wsInstance = Activator.CreateInstance(wsType, new object[] { sigSettings, SynchronizationContext.Current });
                                            Log("   ✅ WebSocketSignaling creado con Project Settings.");
                                        }
                                    }
                                }
                            }
                        }
                        
                        if (wsInstance == null)
                        {
                            Log("   ❌ No se pudo obtener SignalingSettings de ninguna fuente.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"   Error general creando instancia: {ex.Message}");
                    if (ex.InnerException != null) Log($"      Inner: {ex.InnerException.Message}");
                }

                if (wsInstance != null)
                {
                    // Asignar a m_signaling (el campo privado que guarda la instancia activa)
                    var signalingField = type.GetField("m_signaling", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (signalingField != null)
                    {
                        signalingField.SetValue(signalingManager, wsInstance);
                        Log("   ✅ Instancia inyectada en m_signaling.");
                    }
                    else
                    {
                        Log("   ⚠️ No se encontró el campo m_signaling.");
                    }
                }
                else
                {
                    Log("   ❌ No se pudo crear la instancia.");
                }
            }
            else
            {
                Log("   ❌ No se encontró el tipo WebSocketSignaling.");
            }
        }
        catch (Exception e)
        {
            Log($"   ⚠️ Error al inyectar signaling: {e.Message}");
        }

        yield return new WaitForSeconds(0.5f);

        // 3. Iniciar - Buscar todos los métodos Run y usar el correcto
        var runMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                             .Where(m => m.Name == "Run").ToArray();
        
        Log($"   Métodos Run encontrados: {runMethods.Length}");
        
        MethodInfo runMethod = null;
        foreach (var m in runMethods)
        {
            var pars = m.GetParameters();
            Log($"      Run({string.Join(", ", pars.Select(p => p.ParameterType.Name))})");
            if (pars.Length == 0)
            {
                runMethod = m;
                break;
            }
        }
        
        // Si no hay Run sin parámetros, usar el primero
        if (runMethod == null && runMethods.Length > 0)
        {
            runMethod = runMethods[0];
        }

        if (runMethod != null)
        {
            try {
                var parameters = runMethod.GetParameters();
                if (parameters.Length == 0)
                {
                    runMethod.Invoke(signalingManager, null);
                }
                else
                {
                    // Crear argumentos con valores por defecto
                    object[] args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].HasDefaultValue)
                            args[i] = parameters[i].DefaultValue;
                        else if (parameters[i].ParameterType.IsValueType)
                            args[i] = Activator.CreateInstance(parameters[i].ParameterType);
                        else
                            args[i] = null;
                    }
                    runMethod.Invoke(signalingManager, args);
                }
                Log("🚀 SignalingManager.Run() ejecutado.");
            }
            catch (Exception ex)
            {
                Log($"❌ Error ejecutando Run(): {ex.Message}");
                if (ex.InnerException != null) Log($"   Inner: {ex.InnerException.Message}");
            }
        }
        else
        {
            Log("⚠️ No se encontró método Run().");
        }
        
        Log("🏁 Reconfiguración terminada. Verifica la consola del servidor.");
    }

    void RestartSignaling()
    {
        if (signalingManager == null) return;
        Log("🔄 Reiniciando...");
        var stopMethod = typeof(SignalingManager).GetMethod("Stop", Type.EmptyTypes);
        var runMethod = typeof(SignalingManager).GetMethod("Run", Type.EmptyTypes);
        
        if (stopMethod != null) stopMethod.Invoke(signalingManager, null);
        
        StartCoroutine(RunAfterDelay(runMethod));
    }

    IEnumerator RunAfterDelay(MethodInfo runMethod)
    {
        yield return new WaitForSeconds(1.0f);
        if (runMethod != null) runMethod.Invoke(signalingManager, null);
        Log("✅ Reiniciado.");
    }
}

#!/bin/bash
# Script para lanzar Unity forzando el uso de la GPU NVIDIA RTX 4070
# Esto es necesario para Render Streaming ya que usa encoding por hardware

export __NV_PRIME_RENDER_OFFLOAD=1
export __VK_LAYER_NV_optimus=NVIDIA_only
export __GLX_VENDOR_LIBRARY_NAME=nvidia

# Ruta al editor de Unity 
UNITY_EDITOR="/home/alessandro/Unity/Hub/Editor/6000.2.14f1/Editor/Unity"

# Proyecto SmartHome
PROJECT_PATH="/home/alessandro/Documentos/Setup Guide In-Editor Tutorial"

echo "🚀 Lanzando Unity con GPU NVIDIA RTX 4070..."
echo "📁 Proyecto: $PROJECT_PATH"

# Lanzar Unity con el proyecto
"$UNITY_EDITOR" -projectpath "$PROJECT_PATH" &

echo "✅ Unity iniciado. Revisa que esté usando la GPU NVIDIA."
echo "💡 Puedes verificar con: nvidia-smi"

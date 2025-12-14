#!/bin/bash
# ============================================
# SMART HOME - Script de Inicio Completo
# ============================================
# Este script inicia todos los servicios necesarios
# para la demostración del proyecto SmartHome
# ============================================

echo "=========================================="
echo "   SMART HOME - Iniciando Servicios"
echo "=========================================="

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Directorio base
BASE_DIR="/home/alessandro/Documentos/SmartHome/SmartHome"

# Función para verificar si un puerto está en uso
check_port() {
    if ss -tlnp | grep -q ":$1 "; then
        return 0  # Puerto en uso
    else
        return 1  # Puerto libre
    fi
}

# Función para esperar a que un puerto esté disponible
wait_for_port() {
    local port=$1
    local max_attempts=30
    local attempt=0
    while ! check_port $port && [ $attempt -lt $max_attempts ]; do
        sleep 1
        ((attempt++))
    done
}

echo ""
echo -e "${BLUE}[1/5]${NC} Verificando servicios existentes..."

# Matar procesos anteriores si existen
pkill -f "webserver" 2>/dev/null
pkill -f "ngrok" 2>/dev/null
pkill -f "TcpServer" 2>/dev/null
sleep 2

echo -e "${BLUE}[2/5]${NC} Iniciando servidor Java (TCP/REST/WebSocket)..."
cd "$BASE_DIR/SmartHomeServer"
./run.sh &
JAVA_PID=$!
sleep 5

if check_port 8080; then
    echo -e "${GREEN}   [OK] Servidor Java iniciado (puertos 5000, 5002, 8080, 8081)${NC}"
else
    echo -e "${RED}   [ERROR] Error iniciando servidor Java${NC}"
fi

echo -e "${BLUE}[3/5]${NC} Iniciando Render Streaming WebServer..."
cd "$BASE_DIR/RenderStreamingServer"
./webserver -p 8888 &
RS_PID=$!
sleep 2

if check_port 8888; then
    echo -e "${GREEN}   [OK] Render Streaming WebServer iniciado (puerto 8888)${NC}"
else
    echo -e "${RED}   [ERROR] Error iniciando Render Streaming WebServer${NC}"
fi

echo -e "${BLUE}[4/5]${NC} Verificando nginx..."
if systemctl is-active --quiet nginx; then
    echo -e "${GREEN}   [OK] Nginx ya esta corriendo${NC}"
else
    echo -e "${YELLOW}   [WARN] Iniciando nginx...${NC}"
    sudo systemctl start nginx
    echo -e "${GREEN}   [OK] Nginx iniciado${NC}"
fi

echo -e "${BLUE}[5/5]${NC} Iniciando ngrok con dominio estático..."
ngrok http 80 --domain=dane-warm-secondly.ngrok-free.app &
NGROK_PID=$!
sleep 3

echo ""
echo -e "${GREEN}==========================================${NC}"
echo -e "${GREEN}   TODOS LOS SERVICIOS INICIADOS${NC}"
echo -e "${GREEN}==========================================${NC}"
echo ""
echo -e "${YELLOW}URLs de acceso:${NC}"
echo ""
echo -e "   ${BLUE}Web Publica (desde cualquier red):${NC}"
echo -e "      https://dane-warm-secondly.ngrok-free.app"
echo ""
echo -e "   ${BLUE}Web Local (misma red):${NC}"
echo -e "      http://$(hostname -I | awk '{print $1}')"
echo ""
echo -e "${YELLOW}Paginas disponibles:${NC}"
echo "      /                  - Login"
echo "      /dashboard.html    - Panel de control"
echo "      /drone.html        - Control del dron"
echo "      /cameras.html      - Camaras de seguridad"
echo "      /energy.html       - Consumo energetico"
echo "      /routines.html     - Rutinas automaticas"
echo ""
echo -e "${YELLOW}Para iniciar Unity con GPU NVIDIA:${NC}"
echo "      ./launch-unity-nvidia.sh"
echo ""
echo -e "${YELLOW}Credenciales de prueba:${NC}"
echo "      Usuario: admin"
echo "      Password: admin123"
echo ""
echo -e "${RED}Para detener todos los servicios: Ctrl+C${NC}"
echo ""

# Mantener el script corriendo y mostrar logs
trap "echo ''; echo 'Deteniendo servicios...'; pkill -f webserver; pkill -f ngrok; pkill -f TcpServer; echo 'Servicios detenidos.'; exit 0" SIGINT SIGTERM

# Esperar indefinidamente
wait

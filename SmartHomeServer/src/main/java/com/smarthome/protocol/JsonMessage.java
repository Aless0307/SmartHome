package com.smarthome.protocol;

import java.util.HashMap;
import java.util.Map;

/**
 * ═══════════════════════════════════════════════════════════════
 * Clase para parsear y crear mensajes JSON simples
 * Implementación sin librerías externas (Java puro)
 * 
 * Soporta JSON plano con valores string, number y boolean
 * ═══════════════════════════════════════════════════════════════
 */
public class JsonMessage {
    
    private Map<String, Object> data;
    
    public JsonMessage() {
        this.data = new HashMap<>();
    }
    
    /**
     * Crea un JsonMessage desde un string JSON
     */
    public static JsonMessage parse(String json) throws Exception {
        JsonMessage message = new JsonMessage();
        
        if (json == null || json.trim().isEmpty()) {
            throw new Exception("JSON vacío");
        }
        
        // Limpiar el JSON
        json = json.trim();
        
        // Verificar que empiece y termine con llaves
        if (!json.startsWith("{") || !json.endsWith("}")) {
            throw new Exception("JSON inválido: debe empezar con { y terminar con }");
        }
        
        // Quitar las llaves externas
        json = json.substring(1, json.length() - 1).trim();
        
        if (json.isEmpty()) {
            return message; // JSON vacío {}
        }
        
        // Parsear pares clave:valor
        // Usamos un approach simple que funciona para JSON plano
        int i = 0;
        while (i < json.length()) {
            // Saltar espacios y comas
            while (i < json.length() && (json.charAt(i) == ' ' || json.charAt(i) == ',' || json.charAt(i) == '\n' || json.charAt(i) == '\t')) {
                i++;
            }
            
            if (i >= json.length()) break;
            
            // Leer la clave (debe estar entre comillas)
            if (json.charAt(i) != '"') {
                throw new Exception("Se esperaba \" al inicio de la clave en posición " + i);
            }
            i++; // Saltar la comilla inicial
            
            // Encontrar el fin de la clave
            int keyStart = i;
            while (i < json.length() && json.charAt(i) != '"') {
                i++;
            }
            String key = json.substring(keyStart, i);
            i++; // Saltar la comilla final
            
            // Saltar espacios hasta encontrar ":"
            while (i < json.length() && json.charAt(i) == ' ') {
                i++;
            }
            
            if (i >= json.length() || json.charAt(i) != ':') {
                throw new Exception("Se esperaba : después de la clave");
            }
            i++; // Saltar ":"
            
            // Saltar espacios hasta el valor
            while (i < json.length() && json.charAt(i) == ' ') {
                i++;
            }
            
            // Leer el valor
            Object value;
            if (json.charAt(i) == '"') {
                // Valor string
                i++; // Saltar comilla inicial
                int valueStart = i;
                while (i < json.length() && json.charAt(i) != '"') {
                    if (json.charAt(i) == '\\' && i + 1 < json.length()) {
                        i++; // Saltar carácter escapado
                    }
                    i++;
                }
                value = json.substring(valueStart, i);
                i++; // Saltar comilla final
            } else if (json.charAt(i) == 't' || json.charAt(i) == 'f') {
                // Valor boolean
                if (json.substring(i).startsWith("true")) {
                    value = true;
                    i += 4;
                } else if (json.substring(i).startsWith("false")) {
                    value = false;
                    i += 5;
                } else {
                    throw new Exception("Valor inválido en posición " + i);
                }
            } else if (json.charAt(i) == 'n') {
                // Valor null
                if (json.substring(i).startsWith("null")) {
                    value = null;
                    i += 4;
                } else {
                    throw new Exception("Valor inválido en posición " + i);
                }
            } else if (Character.isDigit(json.charAt(i)) || json.charAt(i) == '-') {
                // Valor numérico
                int valueStart = i;
                while (i < json.length() && (Character.isDigit(json.charAt(i)) || json.charAt(i) == '.' || json.charAt(i) == '-')) {
                    i++;
                }
                String numStr = json.substring(valueStart, i);
                if (numStr.contains(".")) {
                    value = Double.parseDouble(numStr);
                } else {
                    value = Integer.parseInt(numStr);
                }
            } else {
                throw new Exception("Valor inválido en posición " + i);
            }
            
            message.data.put(key, value);
        }
        
        return message;
    }
    
    /**
     * Obtiene un valor string
     */
    public String getString(String key) {
        Object value = data.get(key);
        return value != null ? value.toString() : null;
    }
    
    /**
     * Obtiene un valor entero
     */
    public int getInt(String key, int defaultValue) {
        Object value = data.get(key);
        if (value instanceof Number) {
            return ((Number) value).intValue();
        }
        return defaultValue;
    }
    
    /**
     * Obtiene un valor boolean
     */
    public boolean getBoolean(String key, boolean defaultValue) {
        Object value = data.get(key);
        if (value instanceof Boolean) {
            return (Boolean) value;
        }
        return defaultValue;
    }
    
    /**
     * Verifica si tiene una clave
     */
    public boolean has(String key) {
        return data.containsKey(key);
    }
    
    /**
     * Establece un valor
     */
    public JsonMessage put(String key, Object value) {
        data.put(key, value);
        return this;
    }
    
    /**
     * Convierte a string JSON
     */
    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder("{");
        boolean first = true;
        
        for (Map.Entry<String, Object> entry : data.entrySet()) {
            if (!first) {
                sb.append(",");
            }
            first = false;
            
            sb.append("\"").append(entry.getKey()).append("\":");
            
            Object value = entry.getValue();
            if (value == null) {
                sb.append("null");
            } else if (value instanceof String) {
                sb.append("\"").append(escapeString((String) value)).append("\"");
            } else if (value instanceof Boolean || value instanceof Number) {
                sb.append(value);
            } else {
                sb.append("\"").append(escapeString(value.toString())).append("\"");
            }
        }
        
        sb.append("}");
        return sb.toString();
    }
    
    /**
     * Escapa caracteres especiales en strings
     */
    private String escapeString(String s) {
        return s.replace("\\", "\\\\")
                .replace("\"", "\\\"")
                .replace("\n", "\\n")
                .replace("\r", "\\r")
                .replace("\t", "\\t");
    }
    
    /**
     * Crea una respuesta de éxito
     */
    public static JsonMessage success(String message) {
        return new JsonMessage()
                .put("status", "OK")
                .put("message", message);
    }
    
    /**
     * Crea una respuesta de error
     */
    public static JsonMessage error(String message) {
        return new JsonMessage()
                .put("status", "ERROR")
                .put("message", message);
    }
}

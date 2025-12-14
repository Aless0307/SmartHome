package com.smarthome.security;

import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Rate limiter para prevenir ataques de fuerza bruta
 */
public class RateLimiter {
    
    // Configuracion por defecto
    private static final int MAX_ATTEMPTS = 5;
    private static final long BLOCK_TIME_MS = 60000; // 1 minuto
    private static final long CLEANUP_INTERVAL_MS = 300000; // 5 minutos
    
    // Almacena intentos por IP/usuario
    private final Map<String, AttemptInfo> attempts = new ConcurrentHashMap<>();
    private long lastCleanup = System.currentTimeMillis();
    
    // Singleton
    private static RateLimiter instance;
    
    public static synchronized RateLimiter getInstance() {
        if (instance == null) {
            instance = new RateLimiter();
        }
        return instance;
    }
    
    private RateLimiter() {}
    
    /**
     * Verifica si una IP/usuario esta bloqueado
     */
    public boolean isBlocked(String key) {
        cleanupIfNeeded();
        
        AttemptInfo info = attempts.get(key);
        if (info == null) {
            return false;
        }
        
        // Si paso el tiempo de bloqueo, resetear
        if (info.isExpired()) {
            attempts.remove(key);
            return false;
        }
        
        return info.isBlocked();
    }
    
    /**
     * Registra un intento fallido
     */
    public void recordFailedAttempt(String key) {
        AttemptInfo info = attempts.computeIfAbsent(key, k -> new AttemptInfo());
        info.incrementAttempts();
        
        System.out.println("[SECURITY] Intento fallido para: " + key + 
                          " (intentos: " + info.getAttempts() + "/" + MAX_ATTEMPTS + ")");
    }
    
    /**
     * Limpia los intentos exitosos
     */
    public void clearAttempts(String key) {
        attempts.remove(key);
    }
    
    /**
     * Obtiene los intentos restantes antes del bloqueo
     */
    public int getRemainingAttempts(String key) {
        AttemptInfo info = attempts.get(key);
        if (info == null) {
            return MAX_ATTEMPTS;
        }
        return Math.max(0, MAX_ATTEMPTS - info.getAttempts());
    }
    
    /**
     * Obtiene el tiempo restante de bloqueo en segundos
     */
    public long getBlockTimeRemaining(String key) {
        AttemptInfo info = attempts.get(key);
        if (info == null || !info.isBlocked()) {
            return 0;
        }
        long remaining = (info.getBlockedAt() + BLOCK_TIME_MS) - System.currentTimeMillis();
        return Math.max(0, remaining / 1000);
    }
    
    /**
     * Limpia entradas expiradas periodicamente
     */
    private void cleanupIfNeeded() {
        long now = System.currentTimeMillis();
        if (now - lastCleanup > CLEANUP_INTERVAL_MS) {
            attempts.entrySet().removeIf(entry -> entry.getValue().isExpired());
            lastCleanup = now;
        }
    }
    
    /**
     * Clase interna para almacenar info de intentos
     */
    private static class AttemptInfo {
        private int attempts = 0;
        private long blockedAt = 0;
        private long lastAttempt = System.currentTimeMillis();
        
        public void incrementAttempts() {
            attempts++;
            lastAttempt = System.currentTimeMillis();
            if (attempts >= MAX_ATTEMPTS) {
                blockedAt = System.currentTimeMillis();
            }
        }
        
        public int getAttempts() {
            return attempts;
        }
        
        public long getBlockedAt() {
            return blockedAt;
        }
        
        public boolean isBlocked() {
            return attempts >= MAX_ATTEMPTS && !isExpired();
        }
        
        public boolean isExpired() {
            if (blockedAt == 0) {
                // Si no esta bloqueado, expira despues de 5 minutos de inactividad
                return System.currentTimeMillis() - lastAttempt > CLEANUP_INTERVAL_MS;
            }
            // Si esta bloqueado, expira despues del tiempo de bloqueo
            return System.currentTimeMillis() - blockedAt > BLOCK_TIME_MS;
        }
    }
}

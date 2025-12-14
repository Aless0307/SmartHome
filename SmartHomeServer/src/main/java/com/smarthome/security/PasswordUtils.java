package com.smarthome.security;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.security.SecureRandom;
import java.util.Base64;

/**
 * Utilidad para hashear y verificar contraseñas usando SHA-256 con salt
 */
public class PasswordUtils {
    
    private static final int SALT_LENGTH = 16;
    private static final String ALGORITHM = "SHA-256";
    private static final String SEPARATOR = ":";
    
    /**
     * Genera un salt aleatorio
     */
    public static String generateSalt() {
        SecureRandom random = new SecureRandom();
        byte[] salt = new byte[SALT_LENGTH];
        random.nextBytes(salt);
        return Base64.getEncoder().encodeToString(salt);
    }
    
    /**
     * Hashea una contraseña con un salt dado
     */
    public static String hashPassword(String password, String salt) {
        try {
            MessageDigest md = MessageDigest.getInstance(ALGORITHM);
            String saltedPassword = salt + password;
            byte[] hash = md.digest(saltedPassword.getBytes(StandardCharsets.UTF_8));
            return Base64.getEncoder().encodeToString(hash);
        } catch (NoSuchAlgorithmException e) {
            throw new RuntimeException("Error al hashear password", e);
        }
    }
    
    /**
     * Crea un hash completo (salt:hash) para almacenar en BD
     */
    public static String createHash(String password) {
        String salt = generateSalt();
        String hash = hashPassword(password, salt);
        return salt + SEPARATOR + hash;
    }
    
    /**
     * Verifica si una contraseña coincide con un hash almacenado (salt:hash)
     */
    public static boolean verifyPassword(String password, String storedHash) {
        if (storedHash == null || !storedHash.contains(SEPARATOR)) {
            // Compatibilidad con contraseñas antiguas sin hash
            return password.equals(storedHash);
        }
        
        String[] parts = storedHash.split(SEPARATOR);
        if (parts.length != 2) {
            return false;
        }
        
        String salt = parts[0];
        String hash = parts[1];
        String computedHash = hashPassword(password, salt);
        
        return hash.equals(computedHash);
    }
    
    /**
     * Verifica si un hash almacenado es un hash moderno (salt:hash)
     */
    public static boolean isHashed(String storedPassword) {
        return storedPassword != null && storedPassword.contains(SEPARATOR) 
               && storedPassword.split(SEPARATOR).length == 2;
    }
}

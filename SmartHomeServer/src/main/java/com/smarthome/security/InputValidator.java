package com.smarthome.security;

import java.util.regex.Pattern;

/**
 * Utilidad para validar y sanitizar entradas de usuario
 */
public class InputValidator {
    
    // Patrones de validacion
    private static final Pattern USERNAME_PATTERN = Pattern.compile("^[a-zA-Z0-9_]{3,30}$");
    private static final Pattern EMAIL_PATTERN = Pattern.compile("^[A-Za-z0-9+_.-]+@[A-Za-z0-9.-]+$");
    private static final Pattern PASSWORD_PATTERN = Pattern.compile("^.{6,100}$");
    
    // Caracteres peligrosos para inyeccion
    private static final String[] DANGEROUS_CHARS = {"<", ">", "\"", "'", "&", ";", "--", "/*", "*/", "\\", "\0"};
    
    /**
     * Valida formato de username
     */
    public static boolean isValidUsername(String username) {
        if (username == null || username.isEmpty()) {
            return false;
        }
        return USERNAME_PATTERN.matcher(username).matches();
    }
    
    /**
     * Valida formato de email
     */
    public static boolean isValidEmail(String email) {
        if (email == null || email.isEmpty()) {
            return false;
        }
        return EMAIL_PATTERN.matcher(email).matches();
    }
    
    /**
     * Valida longitud de password (min 6 caracteres)
     */
    public static boolean isValidPassword(String password) {
        if (password == null || password.isEmpty()) {
            return false;
        }
        return PASSWORD_PATTERN.matcher(password).matches();
    }
    
    /**
     * Sanitiza una cadena removiendo caracteres peligrosos
     */
    public static String sanitize(String input) {
        if (input == null) {
            return null;
        }
        
        String result = input.trim();
        for (String dangerous : DANGEROUS_CHARS) {
            result = result.replace(dangerous, "");
        }
        return result;
    }
    
    /**
     * Valida que un ID de MongoDB tenga formato correcto (24 caracteres hex)
     */
    public static boolean isValidObjectId(String id) {
        if (id == null || id.length() != 24) {
            return false;
        }
        return id.matches("^[a-fA-F0-9]{24}$");
    }
    
    /**
     * Valida y retorna mensaje de error si hay problemas
     */
    public static String validateUserInput(String username, String password, String email) {
        if (!isValidUsername(username)) {
            return "Username invalido: debe tener 3-30 caracteres alfanumericos o _";
        }
        if (!isValidPassword(password)) {
            return "Password invalido: debe tener minimo 6 caracteres";
        }
        if (email != null && !email.isEmpty() && !isValidEmail(email)) {
            return "Email invalido";
        }
        return null; // Sin errores
    }
}

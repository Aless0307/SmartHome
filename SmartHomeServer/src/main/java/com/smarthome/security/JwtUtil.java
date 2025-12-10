package com.smarthome.security;

import io.jsonwebtoken.*;
import io.jsonwebtoken.security.Keys;
import java.security.Key;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;

/**
 * Utilidad para manejo de JSON Web Tokens (JWT)
 * Implementa creación, validación y parsing de tokens seguros
 * 
 * @author Alessandro Atilano
 */
public class JwtUtil {
    
    // Clave secreta para firmar los tokens (en producción usar variable de entorno)
    private static final String SECRET_KEY = "SmartHome2024SecretKeyForJWTMustBe256BitsLong!";
    private static final Key key = Keys.hmacShaKeyFor(SECRET_KEY.getBytes());
    
    // Tiempo de expiración: 24 horas
    private static final long EXPIRATION_TIME = 24 * 60 * 60 * 1000;
    
    /**
     * Genera un token JWT para un usuario
     * @param username Nombre de usuario
     * @param role Rol del usuario (admin, user)
     * @return Token JWT firmado
     */
    public static String generateToken(String username, String role) {
        Date now = new Date();
        Date expiration = new Date(now.getTime() + EXPIRATION_TIME);
        
        Map<String, Object> claims = new HashMap<>();
        claims.put("role", role);
        claims.put("iat", now);
        
        return Jwts.builder()
                .setClaims(claims)
                .setSubject(username)
                .setIssuedAt(now)
                .setExpiration(expiration)
                .setIssuer("SmartHomeServer")
                .signWith(key, SignatureAlgorithm.HS256)
                .compact();
    }
    
    /**
     * Valida un token JWT
     * @param token Token a validar
     * @return true si el token es válido
     */
    public static boolean validateToken(String token) {
        try {
            Jwts.parserBuilder()
                    .setSigningKey(key)
                    .build()
                    .parseClaimsJws(token);
            return true;
        } catch (ExpiredJwtException e) {
            System.err.println("[ERROR] Token expirado: " + e.getMessage());
        } catch (UnsupportedJwtException e) {
            System.err.println("[ERROR] Token no soportado: " + e.getMessage());
        } catch (MalformedJwtException e) {
            System.err.println("[ERROR] Token malformado: " + e.getMessage());
        } catch (SignatureException e) {
            System.err.println("[ERROR] Firma inválida: " + e.getMessage());
        } catch (IllegalArgumentException e) {
            System.err.println("[ERROR] Token vacío: " + e.getMessage());
        } catch (Exception e) {
            System.err.println("[ERROR] Error validando token: " + e.getMessage());
        }
        return false;
    }
    
    /**
     * Extrae el nombre de usuario del token
     * @param token Token JWT
     * @return Username o null si es inválido
     */
    public static String getUsername(String token) {
        try {
            Claims claims = Jwts.parserBuilder()
                    .setSigningKey(key)
                    .build()
                    .parseClaimsJws(token)
                    .getBody();
            return claims.getSubject();
        } catch (Exception e) {
            return null;
        }
    }
    
    /**
     * Extrae el rol del usuario del token
     * @param token Token JWT
     * @return Role o null si es inválido
     */
    public static String getRole(String token) {
        try {
            Claims claims = Jwts.parserBuilder()
                    .setSigningKey(key)
                    .build()
                    .parseClaimsJws(token)
                    .getBody();
            return (String) claims.get("role");
        } catch (Exception e) {
            return null;
        }
    }
    
    /**
     * Obtiene la fecha de expiración del token
     * @param token Token JWT
     * @return Fecha de expiración o null
     */
    public static Date getExpiration(String token) {
        try {
            Claims claims = Jwts.parserBuilder()
                    .setSigningKey(key)
                    .build()
                    .parseClaimsJws(token)
                    .getBody();
            return claims.getExpiration();
        } catch (Exception e) {
            return null;
        }
    }
    
    /**
     * Verifica si el token está expirado
     * @param token Token JWT
     * @return true si está expirado
     */
    public static boolean isTokenExpired(String token) {
        Date expiration = getExpiration(token);
        return expiration != null && expiration.before(new Date());
    }
    
    /**
     * Información detallada del token
     * @param token Token JWT
     * @return Mapa con info del token
     */
    public static Map<String, Object> getTokenInfo(String token) {
        Map<String, Object> info = new HashMap<>();
        try {
            Claims claims = Jwts.parserBuilder()
                    .setSigningKey(key)
                    .build()
                    .parseClaimsJws(token)
                    .getBody();
            
            info.put("username", claims.getSubject());
            info.put("role", claims.get("role"));
            info.put("issuedAt", claims.getIssuedAt());
            info.put("expiration", claims.getExpiration());
            info.put("issuer", claims.getIssuer());
            info.put("valid", true);
        } catch (Exception e) {
            info.put("valid", false);
            info.put("error", e.getMessage());
        }
        return info;
    }
}

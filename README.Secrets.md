# Configuración de secretos (no commitear credenciales)

Este proyecto espera que **no** se guarden credenciales en `appsettings*.json`.

## Variables de entorno

Configura estas variables antes de ejecutar:

- `INFORMIX_CONNECTION_STRING`
  - Ejemplo:
    - `Server=192.168.20.4:1580;Database=maindb;uid=usuario;pwd=clave`

- `SMTP_USER`
- `SMTP_PASS`

## Desarrollo local (alternativas)

- Usa variables de entorno en tu perfil/terminal.
- O usa `dotnet user-secrets` (si el proyecto se configura para ello).


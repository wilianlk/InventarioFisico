# Ajuste API `GET /api/GrupoConteo/disponibles`

## Resumen
Se ajustó la API para que, además de listar grupos `ACTIVO`, informe si cada grupo ya está asignado a un conteo abierto.

## Cambio de contrato
Se agregaron 2 campos en cada registro de grupo:

- `tieneConteoAbierto` (`boolean`)
- `operacionIdConteoAbierto` (`number | null`)

## Regla de negocio
- Si el grupo tiene al menos un registro en `operacion_conteo` con estado `ABIERTO`:
  - `tieneConteoAbierto = true`
  - `operacionIdConteoAbierto = <id de operación abierta>`
- Si no tiene conteo abierto:
  - `tieneConteoAbierto = false`
  - `operacionIdConteoAbierto = null`

## Ejemplo de respuesta
```json
[
  {
    "id": 12,
    "nombre": "GRUPO A",
    "estado": "ACTIVO",
    "fechaCreacion": "2026-02-20 09:10:11",
    "usuarioCreacion": 1001,
    "tieneConteoAbierto": true,
    "operacionIdConteoAbierto": 345
  },
  {
    "id": 18,
    "nombre": "GRUPO B",
    "estado": "ACTIVO",
    "fechaCreacion": "2026-02-20 10:40:02",
    "usuarioCreacion": 1001,
    "tieneConteoAbierto": false,
    "operacionIdConteoAbierto": null
  }
]
```

## Recomendación frontend
- Si `tieneConteoAbierto` es `true`, bloquear selección del grupo o mostrar etiqueta:
  - `Asignado a conteo abierto (Operación: {operacionIdConteoAbierto})`.

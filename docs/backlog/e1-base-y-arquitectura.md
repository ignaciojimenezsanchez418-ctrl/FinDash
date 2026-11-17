# E1 — Base y arquitectura

Issue de la épica: [#2](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/2)

Incluye la estructura de la solución, la configuración del backend, la base de datos, la autenticación y la conexión entre la aplicación y la API.

> Fuente de verdad: los issues de GitHub. Este archivo es una copia de referencia; el estado de cada historia se lleva en el tablero.

## Historias planificadas

| ID | Historia | Sprint | Prioridad | Puntos | Responsable | Rama |
|---|---|---|---|---|---|---|
| [US-100](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/8) | Spike técnico: API con SQL Server, CoinGecko, BCCR y app MAUI consumiendo un endpoint y mostrando una notificación | Sprint 1 | Must | 5 | Todos | `feature/US-100` |
| [US-103](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/9) | Solución en cuatro proyectos, inyección de dependencias, navegación con Shell y repositorio GitHub | Sprint 1 | Must | 3 | Jorge | `feature/US-103` |
| [US-101](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/10) | DbContext, siete tablas y migraciones de EF Core en SQL Server | Sprint 1 | Must | 5 | Jorge | `feature/US-101` |
| [US-107](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/11) | Registro e inicio de sesión con endpoints JWT y pantallas | Sprint 1 | Must | 5 | Jorge | `feature/US-107` |
| [US-104](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/14) | Tema visual: colores, tipografía, modo oscuro y plantillas de página | Sprint 2 | Should | 3 | David | `feature/US-104` |
| [US-108](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/20) | Cliente de API en la aplicación, manejo de token, reintentos y caché SQLite | Sprint 2 | Must | 5 | David | `feature/US-108` |

**Total planificado:** 26 puntos.

## Criterios de aceptación definidos en el documento

**US-100**

- La prueba se ejecuta en los emuladores de al menos dos integrantes.
- La aplicación consume la API local.
- Se comprueba la conexión con SQL Server, CoinGecko y el BCCR.
- Se muestra una notificación de prueba.

**US-107**

- Una solicitud sin token responde con HTTP 401.
- Un token válido permite acceder únicamente a los datos del usuario autenticado.

Las demás historias de esta épica no tienen criterios explícitos en el documento; están pendientes de definir por el Product Owner. El detalle completo de cada historia está en su issue.

# Backlog de FinDash CR

El backlog **vive en GitHub**: cada épica y cada historia es un issue, y el estado (Backlog, Todo, In Progress, In Review, Done) se lleva en el GitHub Project. Esta carpeta es una copia de referencia dentro del repositorio.

- Todos los issues: https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues
- Solo épicas: filtro `is:open label:épica`
- Solo historias: filtro `is:open label:historia`

Plan oficial: 4 sprints, 25 historias planificadas (97 puntos) y 3 historias fuera del alcance actual. Detalle por sprint e integrante en [`../planificacion.md`](../planificacion.md).

## Épicas

| Épica | Issue | Historias planificadas | Puntos | Fuera de alcance | Archivo |
|---|---|---|---|---|---|
| E1 — Base y arquitectura | [#2](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/2) | 6 | 26 | 0 | [e1-base-y-arquitectura.md](e1-base-y-arquitectura.md) |
| E2 — Precios e integraciones | [#3](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/3) | 5 | 19 | 1 | [e2-precios-e-integraciones.md](e2-precios-e-integraciones.md) |
| E3 — Portafolio | [#4](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/4) | 3 | 13 | 0 | [e3-portafolio.md](e3-portafolio.md) |
| E4 — Alertas | [#5](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/5) | 5 | 16 | 1 | [e4-alertas.md](e4-alertas.md) |
| E5 — Gráficas | [#6](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/6) | 2 | 7 | 1 | [e5-graficas.md](e5-graficas.md) |
| E6 — Cierre y entrega | [#7](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/7) | 4 | 16 | 0 | [e6-cierre-y-entrega.md](e6-cierre-y-entrega.md) |

## Historias por sprint

### Sprint 1 (26 puntos)

| ID | Historia | Sprint | Prioridad | Puntos | Responsable | Rama |
|---|---|---|---|---|---|---|
| [US-100](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/8) | Spike técnico: API con SQL Server, CoinGecko, BCCR y app MAUI consumiendo un endpoint y mostrando una notificación | Sprint 1 | Must | 5 | Todos | `feature/US-100` |
| [US-103](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/9) | Solución en cuatro proyectos, inyección de dependencias, navegación con Shell y repositorio GitHub | Sprint 1 | Must | 3 | Jorge | `feature/US-103` |
| [US-101](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/10) | DbContext, siete tablas y migraciones de EF Core en SQL Server | Sprint 1 | Must | 5 | Jorge | `feature/US-101` |
| [US-107](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/11) | Registro e inicio de sesión con endpoints JWT y pantallas | Sprint 1 | Must | 5 | Jorge | `feature/US-107` |
| [US-201](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/12) | PriceService: cliente de CoinGecko con reintentos, manejo de 429 y almacenamiento de PriceSnapshots | Sprint 1 | Must | 5 | Ignacio | `feature/US-201` |
| [US-206](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/13) | Tipo de cambio oficial USD/CRC desde el BCCR | Sprint 1 | Must | 3 | Ignacio | `feature/US-206` |

### Sprint 2 (25 puntos)

| ID | Historia | Sprint | Prioridad | Puntos | Responsable | Rama |
|---|---|---|---|---|---|---|
| [US-104](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/14) | Tema visual: colores, tipografía, modo oscuro y plantillas de página | Sprint 2 | Should | 3 | David | `feature/US-104` |
| [US-105](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/15) | Notificaciones locales en la aplicación y permiso de Android 13+ | Sprint 2 | Must | 3 | David | `feature/US-105` |
| [US-303](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/16) | PortfolioCalculator multimoneda CRC/USD con pruebas unitarias | Sprint 2 | Must | 3 | Ignacio | `feature/US-303` |
| [US-402](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/17) | AlertRuleEngine: detección de cruce y cooldown con pruebas unitarias | Sprint 2 | Must | 3 | Ignacio | `feature/US-402` |
| [US-204](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/18) | Búsqueda de activos y watchlist: endpoint y pantalla | Sprint 2 | Must | 5 | Jorge | `feature/US-204` |
| [US-203](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/19) | Endpoint /prices y refresco automático en la aplicación | Sprint 2 | Must | 3 | Ignacio | `feature/US-203` |
| [US-108](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/20) | Cliente de API en la aplicación, manejo de token, reintentos y caché SQLite | Sprint 2 | Must | 5 | David | `feature/US-108` |

### Sprint 3 (26 puntos)

| ID | Historia | Sprint | Prioridad | Puntos | Responsable | Rama |
|---|---|---|---|---|---|---|
| [US-202](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/21) | Modo sin conexión con hora de última actualización | Sprint 3 | Must | 3 | David | `feature/US-202` |
| [US-301](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/22) | Transacciones de compra y venta con validaciones: endpoint y formulario | Sprint 3 | Must | 5 | Jorge | `feature/US-301` |
| [US-401](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/23) | Crear, editar y eliminar alertas de precio y tipo de cambio | Sprint 3 | Must | 3 | Jorge | `feature/US-401` |
| [US-403](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/24) | AlertWorker: evalúa alertas cada minuto, crea AlertEvents y la aplicación notifica | Sprint 3 | Must | 5 | Ignacio | `feature/US-403` |
| [US-302](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/25) | Inicio con patrimonio total y ganancias o pérdidas en CRC y USD | Sprint 3 | Must | 5 | Ignacio | `feature/US-302` |
| [US-501](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/26) | Gráfica histórica por activo y tipo de cambio: 24h, 7d, 30d y 1 año | Sprint 3 | Must | 5 | David | `feature/US-501` |

### Sprint 4 (20 puntos)

| ID | Historia | Sprint | Prioridad | Puntos | Responsable | Rama |
|---|---|---|---|---|---|---|
| [US-601](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/27) | Pantalla de ajustes: moneda base, intervalo y cerrar sesión | Sprint 4 | Should | 3 | Jorge | `feature/US-601` |
| [US-604](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/28) | Publicar la API y la base de datos en la nube para la demostración | Sprint 4 | Should | 3 | Jorge | `feature/US-604` |
| [US-404](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/29) | Al tocar una notificación, abrir el detalle del activo | Sprint 4 | Could | 2 | David | `feature/US-404` |
| [US-503](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/30) | Gráfica de distribución del portafolio tipo dona | Sprint 4 | Could | 2 | David | `feature/US-503` |
| [US-602](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/31) | Pruebas del sistema, corrección de errores y pulido | Sprint 4 | Must | 5 | Todos | `feature/US-602` |
| [US-603](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/32) | Documentación final, manual de usuario, video de demostración y presentación | Sprint 4 | Must | 5 | Todos | `feature/US-603` |

### Fuera del alcance actual (Won't, ahora)

| ID | Historia | Sprint | Prioridad | Puntos | Responsable | Rama |
|---|---|---|---|---|---|---|
| [US-405](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/33) | Notificaciones push con la aplicación cerrada mediante Firebase Cloud Messaging | — | Won't (ahora) | 8 | Ignacio | `feature/US-405` |
| [US-502](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/34) | Gráfica en vivo que agrega un punto por cada actualización | — | Won't (ahora) | 3 | David | `feature/US-502` |
| [US-205](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/35) | Ticks en tiempo real mediante Binance WebSocket | — | Won't (ahora) | 5 | Ignacio | `feature/US-205` |

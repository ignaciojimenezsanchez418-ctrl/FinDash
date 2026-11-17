# FinDash CR · Planificación del proyecto

Plan oficial: **4 sprints, 25 historias planificadas y 97 puntos**, más 3 historias fuera del alcance actual. Cada historia es un issue del repositorio, enlazado como sub-issue de su épica.

## Épicas

| Épica | Issue | Alcance |
| --- | --- | --- |
| E1 — Base y arquitectura | #2 | Estructura de la solución, backend, base de datos, autenticación y conexión app–API. |
| E2 — Precios e integraciones | #3 | CoinGecko, precios de criptomonedas, tipo de cambio oficial del BCCR y actualización de datos. |
| E3 — Portafolio | #4 | Compras y ventas, patrimonio, ganancias y pérdidas, conversión CRC/USD. |
| E4 — Alertas | #5 | Crear, editar y eliminar alertas, evaluación de umbrales y notificaciones locales. |
| E5 — Gráficas | #6 | Gráficas históricas de activos y tipo de cambio, y distribución del portafolio. |
| E6 — Cierre y entrega | #7 | Configuración final, publicación en la nube, pruebas, errores, documentación y presentación. |

## Sprint 1 — Arquitectura y fundamentos (26 puntos)

Objetivo: tener la arquitectura funcionando, la base de datos configurada, la autenticación y la conexión con las fuentes externas.

| ID | Issue | Historia | Épica | Prioridad | Puntos | Responsable |
| --- | --- | --- | --- | --- | --- | --- |
| US-100 | #8 | Spike técnico: API con SQL Server, CoinGecko, BCCR y app MAUI consumiendo un endpoint y mostrando una notificación | E1 | Must | 5 | Todos |
| US-103 | #9 | Solución en cuatro proyectos, inyección de dependencias, navegación con Shell y repositorio GitHub | E1 | Must | 3 | Jorge |
| US-101 | #10 | DbContext, siete tablas y migraciones de EF Core en SQL Server | E1 | Must | 5 | Jorge |
| US-107 | #11 | Registro e inicio de sesión con endpoints JWT y pantallas | E1 | Must | 5 | Jorge |
| US-201 | #12 | PriceService: cliente de CoinGecko con reintentos, manejo de 429 y almacenamiento de PriceSnapshots | E2 | Must | 5 | Ignacio |
| US-206 | #13 | Tipo de cambio oficial USD/CRC desde el BCCR | E2 | Must | 3 | Ignacio |

## Sprint 2 — Interfaz, precios y lógica financiera (25 puntos)

Objetivo: conectar la aplicación con el backend y desarrollar las funcionalidades principales de consulta de precios y lógica financiera.

| ID | Issue | Historia | Épica | Prioridad | Puntos | Responsable |
| --- | --- | --- | --- | --- | --- | --- |
| US-104 | #14 | Tema visual: colores, tipografía, modo oscuro y plantillas de página | E1 | Should | 3 | David |
| US-105 | #15 | Notificaciones locales en la aplicación y permiso de Android 13+ | E4 | Must | 3 | David |
| US-303 | #16 | PortfolioCalculator multimoneda CRC/USD con pruebas unitarias | E3 | Must | 3 | Ignacio |
| US-402 | #17 | AlertRuleEngine: detección de cruce y cooldown con pruebas unitarias | E4 | Must | 3 | Ignacio |
| US-204 | #18 | Búsqueda de activos y watchlist: endpoint y pantalla | E2 | Must | 5 | Jorge |
| US-203 | #19 | Endpoint /prices y refresco automático en la aplicación | E2 | Must | 3 | Ignacio |
| US-108 | #20 | Cliente de API en la aplicación, manejo de token, reintentos y caché SQLite | E1 | Must | 5 | David |

## Sprint 3 — Portafolio y alertas (26 puntos)

Objetivo: completar las operaciones financieras, las alertas automáticas, el inicio de la aplicación y las gráficas históricas.

| ID | Issue | Historia | Épica | Prioridad | Puntos | Responsable |
| --- | --- | --- | --- | --- | --- | --- |
| US-202 | #21 | Modo sin conexión con hora de última actualización | E2 | Must | 3 | David |
| US-301 | #22 | Transacciones de compra y venta con validaciones: endpoint y formulario | E3 | Must | 5 | Jorge |
| US-401 | #23 | Crear, editar y eliminar alertas de precio y tipo de cambio | E4 | Must | 3 | Jorge |
| US-403 | #24 | AlertWorker: evalúa alertas cada minuto, crea AlertEvents y la aplicación notifica | E4 | Must | 5 | Ignacio |
| US-302 | #25 | Inicio con patrimonio total y ganancias o pérdidas en CRC y USD | E3 | Must | 5 | Ignacio |
| US-501 | #26 | Gráfica histórica por activo y tipo de cambio: 24h, 7d, 30d y 1 año | E5 | Must | 5 | David |

## Sprint 4 — Finalización y entrega (20 puntos)

Objetivo: integrar todo el sistema, realizar pruebas, corregir errores, publicar la solución y preparar la entrega universitaria.

| ID | Issue | Historia | Épica | Prioridad | Puntos | Responsable |
| --- | --- | --- | --- | --- | --- | --- |
| US-601 | #27 | Pantalla de ajustes: moneda base, intervalo y cerrar sesión | E6 | Should | 3 | Jorge |
| US-604 | #28 | Publicar la API y la base de datos en la nube para la demostración | E6 | Should | 3 | Jorge |
| US-404 | #29 | Al tocar una notificación, abrir el detalle del activo | E4 | Could | 2 | David |
| US-503 | #30 | Gráfica de distribución del portafolio tipo dona | E5 | Could | 2 | David |
| US-602 | #31 | Pruebas del sistema, corrección de errores y pulido | E6 | Must | 5 | Todos |
| US-603 | #32 | Documentación final, manual de usuario, video de demostración y presentación | E6 | Must | 5 | Todos |

## Fuera del alcance actual — Won't (ahora)

No forman parte de los 4 sprints. Quedan en el backlog, con el label `fuera de alcance`.

| ID | Issue | Historia | Épica | Puntos | Responsable |
| --- | --- | --- | --- | --- | --- |
| US-405 | #33 | Notificaciones push con la aplicación cerrada mediante Firebase Cloud Messaging | E4 | 8 | Ignacio |
| US-502 | #34 | Gráfica en vivo que agrega un punto por cada actualización | E5 | 3 | David |
| US-205 | #35 | Ticks en tiempo real mediante Binance WebSocket | E2 | 5 | Ignacio |

## Distribución del trabajo (puntos)

| Integrante | Sprint 1 | Sprint 2 | Sprint 3 | Sprint 4 | Total |
| --- | --- | --- | --- | --- | --- |
| Jorge | 13 | 5 | 8 | 6 | 32 |
| Ignacio | 8 | 9 | 10 | 0 | 27 |
| David | 0 | 11 | 8 | 4 | 23 |
| Compartidas (los tres) | 5 | 0 | 0 | 10 | 15 |
| **Total del sprint** | **26** | **25** | **26** | **20** | **97** |

### Ajustes respecto al documento original

El documento original tenía 4 integrantes (A–D). Con 3 integrantes, las historias se asignaron así:

- **A → Jorge** y **C → David**, sin cambios.
- **B → Ignacio**, excepto US-404, que pasó a **David** (notificaciones y navegación de la app son su área).
- **D (cuarto integrante) se redistribuyó:**
  - US-303, US-402 y US-302 → **Ignacio** (cálculos financieros y lógica de alertas).
  - US-301 y US-401 → **Jorge** (endpoints del backend).

Observaciones para el equipo:
- David no tiene historias individuales en el Sprint 1. Su trabajo en ese sprint es el spike compartido US-100.
- Ignacio no tiene historias individuales en el Sprint 4. Su trabajo en ese sprint son las historias compartidas US-602 y US-603.

## Versión de .NET

El documento original propone **.NET 9 (STS)** en la decisión D6, y él mismo indica que su soporte terminó el **10 de noviembre de 2026**, antes de la entrega del 7 de diciembre de 2026. Entregar con .NET 9 significa usar una versión sin parches de seguridad.

**Recomendación: .NET 10 (LTS)**, con soporte hasta noviembre de 2028. El repositorio **ya usa .NET 10**: `global.json` fija el SDK 10.0.100 y los proyectos apuntan a `net10.0` y `net10.0-android` (mínimo Android API 26). No hace falta cambiar código.

Implicaciones:
- Las máquinas de los tres integrantes necesitan el SDK de .NET 10, la carga de trabajo de MAUI y una versión de Visual Studio compatible con .NET 10.
- Los paquetes NuGet deben fijarse en versiones compatibles con .NET 10 (por ejemplo, EF Core 10). Las versiones se fijan al cerrar el spike US-100.
- Hay que actualizar la decisión D6 en el documento del proyecto para reflejar el cambio.

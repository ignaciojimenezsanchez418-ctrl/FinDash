# E2 — Precios e integraciones

Issue de la épica: [#3](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/3)

Incluye la conexión con CoinGecko, la obtención de precios de criptomonedas, el tipo de cambio oficial del BCCR y la actualización de datos.

> Fuente de verdad: los issues de GitHub. Este archivo es una copia de referencia; el estado de cada historia se lleva en el tablero.

## Historias planificadas

| ID | Historia | Sprint | Prioridad | Puntos | Responsable | Rama |
|---|---|---|---|---|---|---|
| [US-201](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/12) | PriceService: cliente de CoinGecko con reintentos, manejo de 429 y almacenamiento de PriceSnapshots | Sprint 1 | Must | 5 | Ignacio | `feature/US-201` |
| [US-206](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/13) | Tipo de cambio oficial USD/CRC desde el BCCR | Sprint 1 | Must | 3 | Ignacio | `feature/US-206` |
| [US-204](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/18) | Búsqueda de activos y watchlist: endpoint y pantalla | Sprint 2 | Must | 5 | Jorge | `feature/US-204` |
| [US-203](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/19) | Endpoint /prices y refresco automático en la aplicación | Sprint 2 | Must | 3 | Ignacio | `feature/US-203` |
| [US-202](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/21) | Modo sin conexión con hora de última actualización | Sprint 3 | Must | 3 | David | `feature/US-202` |

**Total planificado:** 19 puntos.

## Fuera del alcance actual (Won't, ahora)

| ID | Historia | Sprint | Prioridad | Puntos | Responsable | Rama |
|---|---|---|---|---|---|---|
| [US-205](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/35) | Ticks en tiempo real mediante Binance WebSocket | — | Won't (ahora) | 5 | Ignacio | `feature/US-205` |

## Criterios de aceptación definidos en el documento

**US-201**

- Existe un único PriceService compartido para todos los usuarios.
- Ante un error 429, espera y reintenta.
- No supera 30 llamadas por minuto.

**US-204**

- Buscar "bit" devuelve Bitcoin.
- Un activo agregado aparece en Inicio después del siguiente refresco.

Las demás historias de esta épica no tienen criterios explícitos en el documento; están pendientes de definir por el Product Owner. El detalle completo de cada historia está en su issue.

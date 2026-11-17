# E4 — Alertas

Issue de la épica: [#5](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/5)

Incluye la creación, edición y eliminación de alertas, la evaluación de umbrales y las notificaciones locales.

> Fuente de verdad: los issues de GitHub. Este archivo es una copia de referencia; el estado de cada historia se lleva en el tablero.

## Historias planificadas

| ID | Historia | Sprint | Prioridad | Puntos | Responsable | Rama |
|---|---|---|---|---|---|---|
| [US-105](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/15) | Notificaciones locales en la aplicación y permiso de Android 13+ | Sprint 2 | Must | 3 | David | `feature/US-105` |
| [US-402](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/17) | AlertRuleEngine: detección de cruce y cooldown con pruebas unitarias | Sprint 2 | Must | 3 | Ignacio | `feature/US-402` |
| [US-401](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/23) | Crear, editar y eliminar alertas de precio y tipo de cambio | Sprint 3 | Must | 3 | Jorge | `feature/US-401` |
| [US-403](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/24) | AlertWorker: evalúa alertas cada minuto, crea AlertEvents y la aplicación notifica | Sprint 3 | Must | 5 | Ignacio | `feature/US-403` |
| [US-404](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/29) | Al tocar una notificación, abrir el detalle del activo | Sprint 4 | Could | 2 | David | `feature/US-404` |

**Total planificado:** 16 puntos.

## Fuera del alcance actual (Won't, ahora)

| ID | Historia | Sprint | Prioridad | Puntos | Responsable | Rama |
|---|---|---|---|---|---|---|
| [US-405](https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues/33) | Notificaciones push con la aplicación cerrada mediante Firebase Cloud Messaging | — | Won't (ahora) | 8 | Ignacio | `feature/US-405` |

## Criterios de aceptación definidos en el documento

**US-402**

- La alerta se dispara únicamente cuando el precio cruza el umbral.
- No se repite antes de finalizar el cooldown.
- Se guarda en AlertEvents.
- La aplicación notifica una sola vez por evento.

**US-403**

- La alerta se dispara únicamente cuando el precio cruza el umbral.
- No se repite antes de finalizar el cooldown.
- Se guarda en AlertEvents.
- La aplicación notifica una sola vez por evento.

Las demás historias de esta épica no tienen criterios explícitos en el documento; están pendientes de definir por el Product Owner. El detalle completo de cada historia está en su issue.

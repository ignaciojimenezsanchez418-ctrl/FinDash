# Código fuente de FinDash CR

| Proyecto | Qué es | Responsable principal |
| --- | --- | --- |
| [`FinDash.Api`](FinDash.Api) | API REST en ASP.NET Core: endpoints, JWT, EF Core con SQL Server, PriceService y AlertWorker. | Jorge |
| [`FinDash.Core`](FinDash.Core) | Librería sin dependencias de web ni de MAUI: modelos, interfaces, PortfolioCalculator y AlertRuleEngine. | Ignacio |
| [`FinDashFrontEnd`](FinDashFrontEnd) | App Android en .NET MAUI (MVVM): pantallas, navegación, cliente de la API, caché SQLite, notificaciones y gráficas. | David |

Las pruebas xUnit están en [`../tests/FinDash.Tests`](../tests/FinDash.Tests).

## Backlog

Cada cambio en este código corresponde a una historia de usuario (`US-xxx`) y se trabaja en la rama `feature/US-xxx`.

- **Backlog del proyecto** (épicas e historias): [`../docs/backlog`](../docs/backlog/README.md)
- **Planificación por sprint e integrante:** [`../docs/planificacion.md`](../docs/planificacion.md)
- **Issues en GitHub** (fuente de verdad y estado): https://github.com/ignaciojimenezsanchez418-ctrl/FinDash/issues

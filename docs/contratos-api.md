# FinDash CR · Contratos entre capas

Versión 1 · 16 nov 2026 · Para aprobar a más tardar el miércoles 18 nov.

## 1. Reglas generales

| Tema | Acuerdo |
| --- | --- |
| Versión | .NET 10 (LTS) en todos los proyectos. `global.json` con SDK 10.0.100 y `rollForward: latestFeature`. Reemplaza D6 (.NET 9, sin soporte desde el 10 nov 2026). |
| JSON | camelCase, enums como texto (`"Buy"`, `"Above"`, `"CRC"`), montos como número, fechas ISO 8601 con zona (`2026-11-16T09:00:00-06:00`). En la API: `JsonStringEnumConverter`. En la app: las mismas opciones en el `HttpClient`. |
| Ids | La app genera el `Guid` de `Transaction` y `PriceAlert` (sus modelos ya lo hacen). La API lo acepta y responde igual si llega repetido. |
| Errores | `ProblemDetails` (RFC 9457). El mensaje para el usuario va en `detail`, en español. La app lo pasa a `OperationResult.Fail(detail)`. |
| Autenticación | `Authorization: Bearer <jwt>` en todo excepto `/api/auth/*`. Sin token: 401. El usuario se toma del claim `sub`, nunca del cuerpo. |
| Llaves | CoinGecko, BCCR y el secreto JWT solo en user-secrets (local) y en la configuración de App Service (Azure). Nunca en el repositorio ni en la app. |

## 2. Endpoints (mapeo 1 a 1 con `Services/IServices.cs` de la app)

### IAuthService

| Método de la app | HTTP | Respuesta |
| --- | --- | --- |
| `LoginAsync` | `POST /api/auth/login` `{ email, password }` | 200 `{ token, email, expiresAt }` · 401 |
| `RegisterAsync` | `POST /api/auth/register` `{ email, password }` | 201 `{ token, email, expiresAt }` · 409 "Ya hay una cuenta con ese correo." · 400 |
| `LogoutAsync` | (solo local: borra el token de SecureStorage) | — |

### IMarketService

| Método de la app | HTTP | Respuesta |
| --- | --- | --- |
| `Assets`, `GetAsset` | `GET /api/assets` | `Asset[]`. La app lo carga al iniciar y lo guarda en SQLite, porque estas dos propiedades son síncronas. |
| `SearchAsync` | `GET /api/assets/search?q=bit` | `Asset[]` (busca en la tabla Assets, no llama a CoinGecko) |
| `GetSnapshotAsync` | `GET /api/market/snapshot` | `MarketSnapshot` con `isOffline: false` |
| `GetHistoryAsync` | `GET /api/market/history/{assetId}?range=Day\|Week\|Month\|Year` | `PricePoint[]` |
| `GetWatchlistAsync` | `GET /api/watchlist` | `string[]` |
| `AddToWatchlistAsync` | `PUT /api/watchlist/{assetId}` | 204 |
| `RemoveFromWatchlistAsync` | `DELETE /api/watchlist/{assetId}` | 204 |

`forceRefresh` no obliga a la API a llamar a CoinGecko (RNF-02): solo hace que la app ignore su caché SQLite y pida `/api/market/snapshot`. Los precios los refresca PriceService en el servidor.

### IPortfolioService

| Método de la app | HTTP | Respuesta |
| --- | --- | --- |
| `GetSummaryAsync` | `GET /api/portfolio/summary` | `PortfolioSummary` con la forma de la app (cada `Holding` trae el `asset` completo) |
| `GetTransactionsAsync` | `GET /api/portfolio/transactions` | `Transaction[]`, más reciente primero |
| `GetQuantityAsync` | `GET /api/portfolio/quantity/{assetId}` | `decimal` |
| `AddTransactionAsync` | `POST /api/portfolio/transactions` | 201 · 422 "No se puede vender más de lo que se tiene." · 400 |

### IAlertService

| Método de la app | HTTP | Respuesta |
| --- | --- | --- |
| `GetAlertsAsync` | `GET /api/alerts` | `PriceAlert[]` |
| `GetAsync` | `GET /api/alerts/{id}` | `PriceAlert` · 404 |
| `SaveAsync` | `PUT /api/alerts/{id}` | 200 · 400 "El monto debe ser mayor a cero." |
| `DeleteAsync` | `DELETE /api/alerts/{id}` | 204 |
| `SetActiveAsync` | `PATCH /api/alerts/{id}/active` `{ isActive }` | 204 |
| `GetFiredAsync` | `GET /api/alerts/events` | `AlertEvent[]` con `deliveredAt` |
| (interno del cliente HTTP) | `POST /api/alerts/events/delivered` `{ ids: [] }` | 204 |

La interfaz de la app no tiene un método para marcar eventos como entregados. Para no cambiarla, el cliente HTTP de alertas, dentro de `GetFiredAsync`, notifica los eventos con `deliveredAt == null` y luego llama a `/events/delivered`. Así cada alerta se notifica una sola vez (RF-11, M4 y M5).

## 3. Interfaces internas del backend (FinDash.Core/Interfaces)

| Interfaz | La implementa | La usa |
| --- | --- | --- |
| `IMarketDataProvider` | Integraciones (CoinGeckoProvider y un FakeMarketDataProvider para desarrollo y demo) | PriceService y el endpoint de historial |
| `IExchangeRateProvider` | Integraciones (BccrProvider, con respaldo en el último valor guardado) | PriceService y el endpoint de historial |
| `IPortfolioService` | Backend (EF Core), usando `PortfolioCalculator` de Core | PortfolioController |
| `IAlertService` | Backend (EF Core) | AlertsController |

Los proveedores lanzan `HttpRequestException` si fallan. PriceService la registra en los logs y conserva el último snapshot.

`AlertRuleEngine` es una función pura: recibe la alerta, el precio nuevo y la hora, y devuelve la alerta actualizada y, si se disparó, el evento. AlertWorker solo lee y guarda.

## 4. Tablas que necesito del backend

**PriceSnapshots**, para que PriceService guarde cada ciclo:

| Columna | Tipo | Nota |
| --- | --- | --- |
| Id | bigint identity | PK |
| AssetId | nvarchar(50) | FK a Assets; `usd` para el tipo de cambio |
| PriceUsd | decimal(18,8) | 1 para `usd` |
| PriceCrc | decimal(18,8) | Para `usd` es el tipo de compra (317) |
| SellCrc | decimal(18,8) null | Solo para `usd`: tipo de venta (318) |
| Change24hPct | decimal(9,4) | |
| CapturedAt | datetimeoffset | Índice `(AssetId, CapturedAt DESC)` |

**PriceAlerts**: además de los campos de la app, necesita `State` (Active, Triggered, Rearmed), `LastPrice decimal(18,8) null` y `LastTriggeredAt datetimeoffset null`. Sin `LastPrice` no se puede detectar el cruce.

**AlertEvents**: `DeliveredAt datetimeoffset null`.

## 5. Reglas de cálculo acordadas

- La moneda de la comisión es la del precio (`PriceCurrency`), igual que en `MockPortfolioService`.
- El costo en colones de una compra en USD usa el `FxRate` guardado en la transacción. El valor actual usa el tipo de compra más reciente.
- Los montos en USD del resumen se calculan con el último tipo de cambio, como pide el documento.
- Para el dólar, la alerta se compara en CRC contra el tipo de compra. Para una cripto, se compara en la moneda de la alerta.

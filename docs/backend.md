# FinDash CR · Backend

API REST de FinDash (ASP.NET Core, .NET 10) que cumple `docs/contratos-api.md`.

```
src/FinDash.Core     Modelos, interfaces, PortfolioCalculator y AlertRuleEngine (sin web ni MAUI)
src/FinDash.Api      Controllers, EF Core, JWT, integraciones, PriceService y AlertWorker
tests/FinDash.Tests  xUnit: lógica, lectura de CoinGecko/BCCR y endpoints
```

## Correrlo

```bash
dotnet test tests/FinDash.Tests
```

```bash
dotnet run --project src/FinDash.Api
```

- La API queda en `http://localhost:5182`; desde el emulador de Android es `http://10.0.2.2:5182`.
- Swagger: `http://localhost:5182/swagger`.
- Sin configurar nada, usa precios **falsos** (los mismos de `MockBackend` de la app), crea
  `findash.db` y una llave JWT temporal.
- No uses `dotnet build FinDash.slnx` en Linux o macOS sin la carga de trabajo de MAUI: la
  solución incluye la app. El CI compila solo el backend.

## Llaves (nunca en el repositorio)

```bash
cd src/FinDash.Api
dotnet user-secrets set "Jwt:Key" "una-frase-larga-de-al-menos-32-caracteres"
dotnet user-secrets set "CoinGecko:ApiKey" "CG-..."
dotnet user-secrets set "Bccr:Email" "correo-registrado@ejemplo.com"
dotnet user-secrets set "Bccr:Token" "TOKEN-DEL-BCCR"
```

En App Service van como configuración: `Jwt__Key`, `CoinGecko__ApiKey`, `Bccr__Email` y `Bccr__Token`.
Fuera de Development la API no arranca sin `Jwt:Key`.

`Providers:Mode` decide las fuentes:
- `Auto`: la real si tiene su llave.
- `Real`: siempre las reales.
- `Fake`: siempre las falsas, para pruebas, CI y el plan B de la demo.

## Migraciones

La API aplica las migraciones al arrancar. Si se cambia una entidad:

```bash
dotnet tool restore
dotnet ef migrations add NombreDelCambio --project src/FinDash.Api --output-dir Data/Migrations
```

El CI falla si alguien cambia el modelo y olvida la migración.

## Cómo funciona

- **PriceService** pide todas las cripto del catálogo en UNA llamada a CoinGecko cada 5 min y
  el tipo de cambio del BCCR cada 6 h. Guarda cada ciclo en `PriceSnapshots` y en una caché en
  memoria compartida por todos los usuarios. `/api/market/snapshot` lee esa caché y nunca llama
  a CoinGecko (RNF-02).
- **CallBudget** no deja pasar de 25 llamadas por minuto a CoinGecko. Ante un 429 espera lo que
  pida la API y mientras tanto se sirve la caché.
- **Gráficas:** `/api/market/history` pide el historial a la fuente al abrir la gráfica y lo guarda
  en memoria (10 min para 24h, 1 h para el resto).
- **AlertWorker** evalúa cada minuto todas las alertas con `AlertRuleEngine`.
  - Dispara al cruzar el umbral (usa `LastPrice`).
  - Queda `Triggered` sin repetir hasta que el precio vuelve al otro lado (`Rearmed`).
  - Respeta un cooldown de 60 min (`Alerts:CooldownMinutes`).
- **Costo promedio:**
  - Las transacciones se procesan por fecha, no por orden de registro.
  - Una venta no cambia el promedio y deja ganancia realizada (`realizedPnlCrc` en el resumen).
  - Una venta que deja saldo negativo en su fecha, aunque sea con fecha anterior a la compra,
    responde 422.

## Decisiones para revisar en equipo

- **Base de datos: SQLite** para arrancar sin instalar nada y para el CI. El documento propone SQL
  Server (D14). Para cambiar:
  1. Paquete `Microsoft.EntityFrameworkCore.SqlServer` y `UseSqlServer`.
  2. Quitar el conversor de `DateTimeOffset` en `FinDashDbContext`.
  3. Borrar `Data/Migrations` y crear la migración de nuevo.
- **CoinGecko cada 5 min**, no cada 30–60 s (D2): además del límite por minuto, el plan Demo
  tiene un tope mensual de llamadas.
- **Transacciones en USD:** si la app manda `fxRate` > 0, se guarda ese. Si manda 0, el servidor
  pone el tipo de compra oficial de la fecha. Hoy `TransactionViewModel` manda el tipo de cambio
  de HOY aunque la fecha sea pasada, así que conviene mandar 0 cuando la fecha no es hoy.
- **Alertas creadas con la condición ya cumplida** avisan una vez en la siguiente evaluación.
  Editar el activo, la condición, el umbral o la moneda la deja como nueva. Editar no la enciende
  si estaba apagada.

## Pendiente

- Probar `BccrProvider` contra el servicio real cuando haya token. El formato que lee está
  probado con una respuesta guardada.
- Bloque 2: clientes HTTP en la app, token en SecureStorage, caché SQLite y notificaciones.

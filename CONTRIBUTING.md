# Cómo trabajamos en FinDash CR

## Equipo

| Integrante | Área |
| --- | --- |
| Jorge | Arquitectura y backend: estructura de la solución, ASP.NET Core Web API, SQL Server y EF Core, autenticación y autorización, endpoints. |
| Ignacio | Integraciones y lógica financiera: CoinGecko, BCCR, precios, cálculos del portafolio, lógica de alertas y sus pruebas unitarias. |
| David | Aplicación móvil: .NET MAUI, diseño e interfaz, navegación, cliente de la API, caché SQLite, notificaciones locales y gráficas. |

## Ramas

- `main` es la rama principal y está protegida: nadie hace push directo a `main`.
- Se crea una rama por historia de usuario desde `main`, con el ID de la historia:

  ```bash
  git checkout main
  git pull
  git checkout -b feature/US-301
  ```

- Las ramas son cortas: se integran en cuanto la historia está terminada.

## Commits

- Mensajes en español, en presente y con el ID de la historia al inicio:
  `US-301: valida que la venta no supere la cantidad en poder`.
- Commits pequeños y enfocados en un solo cambio.

## Pull Requests

1. Sube la rama y abre un Pull Request hacia `main`.
2. En la descripción escribe `Closes #<número del issue>` para que el issue se cierre al integrar.
3. Pide la revisión de al menos **otro integrante**.
4. El revisor revisa, prueba y aprueba. Si pide cambios, se corrigen en la misma rama.
5. Al integrar, usa **Squash and merge** para mantener un historial ordenado (un commit por historia).
6. Borra la rama después de integrar.

## Definición de terminado

- Cumple sus criterios de aceptación y corre en el emulador.
- Tiene pruebas unitarias si toca lógica de Core.
- El PR fue revisado y aprobado por otro integrante.
- `main` compila después del merge.
- Su estado está actualizado en el tablero.

## Seguridad

- Las llaves de CoinGecko y del BCCR, y el secreto JWT, viven solo en la configuración del servidor (user-secrets en local, variables de entorno en la nube). Nunca en el repositorio ni en la app.
- El token de sesión de la app se guarda en SecureStorage.

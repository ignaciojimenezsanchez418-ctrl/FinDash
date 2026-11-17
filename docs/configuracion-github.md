# Configuración de GitHub (pasos manuales)

Lo siguiente no se puede configurar desde el código del repositorio: lo hace el dueño del repositorio desde GitHub.

## 1. Proteger la rama `main`

> ⚠️ **Importante:** el repositorio es **privado**. En una cuenta personal **GitHub Free**, las reglas de protección de ramas **no se aplican** en repositorios privados. Para que funcionen, hay que hacer una de estas cosas:
> - Activar **GitHub Pro**, que es gratis con el **GitHub Student Developer Pack** (education.github.com/pack).
> - Hacer el repositorio público.

Pasos: **Settings → Rules → Rulesets → New ruleset → New branch ruleset**

1. **Ruleset name:** `Proteger main`. **Enforcement status:** Active.
2. **Target branches → Add target → Include default branch.**
3. Activa estas reglas:
   - **Restrict deletions**
   - **Block force pushes**
   - **Require a pull request before merging**
     - **Required approvals:** 1
     - **Dismiss stale pull request approvals when new commits are pushed**
   - **Require linear history** (historial ordenado)
4. **Create.**

## 2. Forma de integrar los Pull Requests

**Settings → General → Pull Requests**

- Deja activo solo **Allow squash merging** (un commit por historia en `main`).
- Activa **Automatically delete head branches** (borra la rama al integrar).

## 3. Colaboradores

**Settings → Collaborators → Add people**: invita a Jorge y a David con sus cuentas de GitHub. Después, en cada historia, cambia **Assignees** al responsable. Por ahora solo están asignadas las historias de Ignacio, porque es la única cuenta conocida.

## 4. GitHub Project

1. Crea el proyecto, o reutiliza el existente: **Your profile → Projects → New project → Table**.
2. **Status:** Settings del proyecto → Status → agrega las opciones **Backlog** e **In Review** y ordénalas: Backlog, Todo, In Progress, In Review, Done.
3. Si el proyecto ya tiene campos **Priority**, **Sprint**, **Epic** o **Story Points** con otras opciones u otro tipo, bórralos. El script los crea con los valores del plan.
4. Ejecuta el script, que crea los campos, agrega las 34 tarjetas y llena los valores:

   ```powershell
   winget install --id GitHub.cli
   gh auth login
   gh auth refresh -s project
   .\scripts\configurar-project.ps1 -ProjectNumber <número del proyecto>
   ```

5. **Vistas** (+ New view):
   - **Kanban:** Board, agrupado por Status.
   - **Backlog:** Table con Responsable, Priority, Story Points, Epic y Sprint.
   - **Sprints:** Board o Table → Group by: Sprint.
6. **Workflows** (botón Workflows del proyecto):
   - **Item closed** → Status: Done.
   - **Pull request merged** → Status: Done.
   - **Auto-add to project** → repositorio FinDash, filtro `is:issue`.

## 5. Labels antiguos

Estos labels quedaron de una configuración anterior y ya no se usan: `backend`, `servicios`, `movil`, `general`. Para borrarlos: **Issues → Labels → Delete**.

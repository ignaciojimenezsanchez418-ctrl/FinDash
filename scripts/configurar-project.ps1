<#
.SYNOPSIS
  Configura el GitHub Project de FinDash CR: crea los campos, agrega las épicas y
  las historias, y llena Status, Priority, Sprint, Epic, Story Points y Responsable.

.DESCRIPTION
  Requisitos:
    - GitHub CLI (gh) instalado:   winget install --id GitHub.cli
    - Sesión iniciada:             gh auth login
    - Permiso de Projects:         gh auth refresh -s project

  Uso (en PowerShell, desde la raíz del repositorio):
    .\scripts\configurar-project.ps1 -ProjectNumber 1

  El número del proyecto aparece en su URL: .../users/<usuario>/projects/<número>

  Se puede ejecutar varias veces: no duplica campos ni elementos.
#>
param(
    [Parameter(Mandatory = $true)] [int] $ProjectNumber,
    [string] $Owner = "ignaciojimenezsanchez418-ctrl",
    [string] $Repo  = "ignaciojimenezsanchez418-ctrl/FinDash"
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
$OutputEncoding = [System.Text.UTF8Encoding]::new()

# Función simple (sin [Parameter]) para que argumentos como --owner pasen tal cual a gh.
function Invoke-Gh {
    $salida = & gh @args
    if ($LASTEXITCODE -ne 0) {
        throw "Falló: gh $($args -join ' ')"
    }
    return ($salida -join "`n")
}

# --- Datos del plan (fuente: docs/planificacion.md) ---------------------------

$E1 = "E1 — Base y arquitectura"
$E2 = "E2 — Precios e integraciones"
$E3 = "E3 — Portafolio"
$E4 = "E4 — Alertas"
$E5 = "E5 — Gráficas"
$E6 = "E6 — Cierre y entrega"

$Epicas = @(
    @{ N = 2; Epic = $E1 }, @{ N = 3; Epic = $E2 }, @{ N = 4; Epic = $E3 },
    @{ N = 5; Epic = $E4 }, @{ N = 6; Epic = $E5 }, @{ N = 7; Epic = $E6 }
)

# Resp vacío = historia compartida por los tres (queda indicado en el issue).
$Historias = @(
    @{ N = 8;  Id = "US-100"; Epic = $E1; Pri = "Must";   Sprint = "Sprint 1"; SP = "5"; Resp = "";        Status = "Todo" },
    @{ N = 9;  Id = "US-103"; Epic = $E1; Pri = "Must";   Sprint = "Sprint 1"; SP = "3"; Resp = "Jorge";   Status = "Todo" },
    @{ N = 10; Id = "US-101"; Epic = $E1; Pri = "Must";   Sprint = "Sprint 1"; SP = "5"; Resp = "Jorge";   Status = "Todo" },
    @{ N = 11; Id = "US-107"; Epic = $E1; Pri = "Must";   Sprint = "Sprint 1"; SP = "5"; Resp = "Jorge";   Status = "Todo" },
    @{ N = 12; Id = "US-201"; Epic = $E2; Pri = "Must";   Sprint = "Sprint 1"; SP = "5"; Resp = "Ignacio"; Status = "Todo" },
    @{ N = 13; Id = "US-206"; Epic = $E2; Pri = "Must";   Sprint = "Sprint 1"; SP = "3"; Resp = "Ignacio"; Status = "Todo" },
    @{ N = 14; Id = "US-104"; Epic = $E1; Pri = "Should"; Sprint = "Sprint 2"; SP = "3"; Resp = "David";   Status = "Backlog" },
    @{ N = 15; Id = "US-105"; Epic = $E4; Pri = "Must";   Sprint = "Sprint 2"; SP = "3"; Resp = "David";   Status = "Backlog" },
    @{ N = 16; Id = "US-303"; Epic = $E3; Pri = "Must";   Sprint = "Sprint 2"; SP = "3"; Resp = "Ignacio"; Status = "Backlog" },
    @{ N = 17; Id = "US-402"; Epic = $E4; Pri = "Must";   Sprint = "Sprint 2"; SP = "3"; Resp = "Ignacio"; Status = "Backlog" },
    @{ N = 18; Id = "US-204"; Epic = $E2; Pri = "Must";   Sprint = "Sprint 2"; SP = "5"; Resp = "Jorge";   Status = "Backlog" },
    @{ N = 19; Id = "US-203"; Epic = $E2; Pri = "Must";   Sprint = "Sprint 2"; SP = "3"; Resp = "Ignacio"; Status = "Backlog" },
    @{ N = 20; Id = "US-108"; Epic = $E1; Pri = "Must";   Sprint = "Sprint 2"; SP = "5"; Resp = "David";   Status = "Backlog" },
    @{ N = 21; Id = "US-202"; Epic = $E2; Pri = "Must";   Sprint = "Sprint 3"; SP = "3"; Resp = "David";   Status = "Backlog" },
    @{ N = 22; Id = "US-301"; Epic = $E3; Pri = "Must";   Sprint = "Sprint 3"; SP = "5"; Resp = "Jorge";   Status = "Backlog" },
    @{ N = 23; Id = "US-401"; Epic = $E4; Pri = "Must";   Sprint = "Sprint 3"; SP = "3"; Resp = "Jorge";   Status = "Backlog" },
    @{ N = 24; Id = "US-403"; Epic = $E4; Pri = "Must";   Sprint = "Sprint 3"; SP = "5"; Resp = "Ignacio"; Status = "Backlog" },
    @{ N = 25; Id = "US-302"; Epic = $E3; Pri = "Must";   Sprint = "Sprint 3"; SP = "5"; Resp = "Ignacio"; Status = "Backlog" },
    @{ N = 26; Id = "US-501"; Epic = $E5; Pri = "Must";   Sprint = "Sprint 3"; SP = "5"; Resp = "David";   Status = "Backlog" },
    @{ N = 27; Id = "US-601"; Epic = $E6; Pri = "Should"; Sprint = "Sprint 4"; SP = "3"; Resp = "Jorge";   Status = "Backlog" },
    @{ N = 28; Id = "US-604"; Epic = $E6; Pri = "Should"; Sprint = "Sprint 4"; SP = "3"; Resp = "Jorge";   Status = "Backlog" },
    @{ N = 29; Id = "US-404"; Epic = $E4; Pri = "Could";  Sprint = "Sprint 4"; SP = "2"; Resp = "David";   Status = "Backlog" },
    @{ N = 30; Id = "US-503"; Epic = $E5; Pri = "Could";  Sprint = "Sprint 4"; SP = "2"; Resp = "David";   Status = "Backlog" },
    @{ N = 31; Id = "US-602"; Epic = $E6; Pri = "Must";   Sprint = "Sprint 4"; SP = "5"; Resp = "";        Status = "Backlog" },
    @{ N = 32; Id = "US-603"; Epic = $E6; Pri = "Must";   Sprint = "Sprint 4"; SP = "5"; Resp = "";        Status = "Backlog" },
    @{ N = 33; Id = "US-405"; Epic = $E4; Pri = "Won't";  Sprint = "";         SP = "8"; Resp = "Ignacio"; Status = "Backlog" },
    @{ N = 34; Id = "US-502"; Epic = $E5; Pri = "Won't";  Sprint = "";         SP = "3"; Resp = "David";   Status = "Backlog" },
    @{ N = 35; Id = "US-205"; Epic = $E2; Pri = "Won't";  Sprint = "";         SP = "5"; Resp = "Ignacio"; Status = "Backlog" }
)

$CamposSelect = [ordered]@{
    "Priority"     = @("Must", "Should", "Could", "Won't")
    "Sprint"       = @("Sprint 1", "Sprint 2", "Sprint 3", "Sprint 4")
    "Epic"         = @($E1, $E2, $E3, $E4, $E5, $E6)
    "Story Points" = @("2", "3", "5", "8")
    "Responsable"  = @("Jorge", "Ignacio", "David")
}
$StatusRequeridos = @("Backlog", "Todo", "In Progress", "In Review", "Done")

# --- 1. Proyecto y campos -----------------------------------------------------

Write-Host "Leyendo el proyecto $ProjectNumber de $Owner..."
$proyecto = Invoke-Gh project view $ProjectNumber --owner $Owner --format json | ConvertFrom-Json
$projectId = $proyecto.id

function Get-Campos {
    $json = Invoke-Gh project field-list $ProjectNumber --owner $Owner --format json --limit 100 | ConvertFrom-Json
    $mapa = @{}
    foreach ($f in $json.fields) { $mapa[$f.name] = $f }
    return $mapa
}

$campos = Get-Campos

# Status ya existe en todo proyecto; sus opciones se agregan a mano.
$faltanStatus = @()
foreach ($s in $StatusRequeridos) {
    if (-not ($campos["Status"].options | Where-Object { $_.name -eq $s })) { $faltanStatus += $s }
}
if ($faltanStatus.Count -gt 0) {
    throw "Al campo Status le faltan estas opciones: $($faltanStatus -join ', '). Agrégalas en Settings del proyecto > Status > Add option y vuelve a ejecutar el script."
}

foreach ($nombre in $CamposSelect.Keys) {
    $opciones = $CamposSelect[$nombre]
    $campo = $campos[$nombre]
    if ($null -eq $campo) {
        Write-Host "Creando el campo '$nombre'..."
        Invoke-Gh project field-create $ProjectNumber --owner $Owner --name $nombre `
            --data-type SINGLE_SELECT --single-select-options ($opciones -join ",") | Out-Null
        continue
    }
    if ($campo.type -ne "ProjectV2SingleSelectField") {
        throw "Ya existe un campo '$nombre' de otro tipo ($($campo.type)). Bórralo en Settings del proyecto y vuelve a ejecutar el script."
    }
    $faltan = $opciones | Where-Object { $o = $_; -not ($campo.options | Where-Object { $_.name -eq $o }) }
    if ($faltan) {
        throw "Al campo '$nombre' le faltan opciones: $($faltan -join ', '). Bórralo en Settings del proyecto (el script lo crea de nuevo) o agrega las opciones a mano."
    }
    Write-Host "El campo '$nombre' ya existe con las opciones correctas."
}

$campos = Get-Campos

function Set-Campo {
    param([string] $ItemId, [string] $Campo, [string] $Valor)
    if ([string]::IsNullOrEmpty($Valor)) { return }
    $f = $campos[$Campo]
    $opcion = $f.options | Where-Object { $_.name -eq $Valor }
    if ($null -eq $opcion) { throw "El campo '$Campo' no tiene la opción '$Valor'." }
    Invoke-Gh project item-edit --id $ItemId --project-id $projectId `
        --field-id $f.id --single-select-option-id $opcion.id | Out-Null
}

function Add-Issue {
    param([int] $Numero)
    $url = "https://github.com/$Repo/issues/$Numero"
    $item = Invoke-Gh project item-add $ProjectNumber --owner $Owner --url $url --format json | ConvertFrom-Json
    return $item.id
}

# --- 2. Épicas ----------------------------------------------------------------

foreach ($e in $Epicas) {
    Write-Host "Épica #$($e.N): $($e.Epic)"
    $id = Add-Issue $e.N
    Set-Campo $id "Epic" $e.Epic
}

# --- 3. Historias -------------------------------------------------------------

foreach ($h in $Historias) {
    Write-Host "$($h.Id) (#$($h.N))"
    $id = Add-Issue $h.N
    Set-Campo $id "Status"       $h.Status
    Set-Campo $id "Priority"     $h.Pri
    Set-Campo $id "Sprint"       $h.Sprint
    Set-Campo $id "Epic"         $h.Epic
    Set-Campo $id "Story Points" $h.SP
    Set-Campo $id "Responsable"  $h.Resp
}

Write-Host ""
Write-Host "Listo: 6 épicas y 28 historias en el proyecto $ProjectNumber."
Write-Host "Las historias compartidas (US-100, US-602, US-603) quedan sin Responsable: son de los tres."

using FinDash.Core.Services;

namespace FinDash.Api.Services;

/// <summary>
/// Mensajes de OperationResult que llevan un código HTTP distinto de 400 (contrato §2).
/// Los controllers los traducen con <see cref="StatusFor"/>.
/// </summary>
public static class ServiceErrors
{
    public const string Oversell = PortfolioCalculator.OversellError;
    public const string AssetNotFound = "Ese activo no existe.";
    public const string AlertNotFound = "Esa alerta no existe.";
    public const string IdInUse = "Ese id ya lo usa otro registro.";
    public const string FxUnavailable = "No hay tipo de cambio para esa fecha. Escríbalo en el formulario.";

    public static int StatusFor(string? error) => error switch
    {
        Oversell => StatusCodes.Status422UnprocessableEntity,
        AssetNotFound or AlertNotFound => StatusCodes.Status404NotFound,
        IdInUse => StatusCodes.Status409Conflict,
        FxUnavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status400BadRequest,
    };
}

using FinDash.Api.Auth;
using FinDash.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinDash.Api.Controllers;

/// <summary>Base de los controllers con sesión: todo pide token (contrato §1).</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid UserId => User.UserId();

    /// <summary>ProblemDetails con el mensaje para el usuario en "detail" (contrato §1).</summary>
    protected ObjectResult Error(string detail, int? status = null)
    {
        var code = status ?? ServiceErrors.StatusFor(detail);
        return Problem(detail: detail, statusCode: code, title: code switch
        {
            StatusCodes.Status401Unauthorized => "No autorizado",
            StatusCodes.Status404NotFound => "No encontrado",
            StatusCodes.Status409Conflict => "Conflicto",
            StatusCodes.Status422UnprocessableEntity => "Operación no permitida",
            StatusCodes.Status503ServiceUnavailable => "Datos no disponibles",
            _ => "Solicitud no válida",
        });
    }
}

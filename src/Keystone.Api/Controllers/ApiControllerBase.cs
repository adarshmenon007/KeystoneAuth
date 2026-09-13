using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Keystone.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult Problem(Error error)
    {
        return Problem(
            statusCode: error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            },
            title: error.Description);
    }
}

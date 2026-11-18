namespace Honeyslop.Trap.Handlers;

public interface ITrapHandler
{
    Task HandleAsync(HttpContext httpContext);
}

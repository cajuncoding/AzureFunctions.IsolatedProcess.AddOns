using System.Net;

namespace Functions.Worker.HttpResponseDataJsonMiddleware
{
    public delegate (HttpStatusCode HttpStatus, object? Result) JsonMiddlewareExceptionHandler(Exception ex);
}

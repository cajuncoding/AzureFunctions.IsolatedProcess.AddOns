using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Functions.Worker.HttpResponseDataJsonMiddleware
{
    public static class AzFuncHttpResponseDataJsonMiddlewareExtensions
    {
        public static IFunctionsWorkerApplicationBuilder UseJsonResponses(this IFunctionsWorkerApplicationBuilder app)
            => app.UseJsonResponses(exceptionHandler: null);

        public static IFunctionsWorkerApplicationBuilder UseJsonResponses(this IFunctionsWorkerApplicationBuilder app, bool handleExceptionsAutomatically = false)
            => handleExceptionsAutomatically switch
            {
                true => app.UseJsonResponses(exceptionHandler: (exc) => (HttpStatusCode.InternalServerError, exc.ToJsonErrorResultOrDefault())),
                _ => app.UseJsonResponses(exceptionHandler: null)
            };


        public static IFunctionsWorkerApplicationBuilder UseJsonResponses(this IFunctionsWorkerApplicationBuilder app, JsonMiddlewareExceptionHandler? exceptionHandler = null)
        {
            if(exceptionHandler is not null)
                app.Services.AddSingleton(exceptionHandler);

            return app.UseMiddleware<HttpResponseDataJsonMiddleware>();
        }

        public static object ToJsonErrorResultOrDefault(this Exception exc) => (
            new
            {
                isSuccessful = false,
                error = new
                {
                    code = exc.GetType().Name switch
                    {
                        "Exception" => "unhandled_exception",
                        var name => name.ToSnakeCase()
                    },
                    message = exc?.Message ?? "An unexpected/unhandled error occurred.",
                    timestamp = DateTime.UtcNow
                }
            }
        );



        //Boundary detection between tokens in Pascal/camel with acronym handling:
        // 1) Boundary between a lower/digit and upper:    "userID" -> "user_ID"
        // 2) Boundary in ALLCAPS followed by lower:       "HTTPServer" -> "HTTP_Server"
        private static readonly Regex wordBoundariesRegex = new(
            @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            matchTimeout: TimeSpan.FromSeconds(1)
        );

        private static readonly Regex _muiltiUnderscoresRegex = new(
            "_{2,}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            matchTimeout: TimeSpan.FromSeconds(1)
        );

        public static string ToSnakeCase(this string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var withUnderscores = wordBoundariesRegex.Replace(input, "_");
            
            // Normalize multiple underscores that might arise from odd inputs.
            withUnderscores = _muiltiUnderscoresRegex.Replace(withUnderscores , "_");
            
            return withUnderscores.ToLower(CultureInfo.InvariantCulture);
        }
    }
}

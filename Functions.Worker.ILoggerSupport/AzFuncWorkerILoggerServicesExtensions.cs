using Functions.Worker.ContextAccessor;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Functions.Worker.ILoggerSupport
{
    public static class AzFuncWorkerILoggerServicesExtensions
    {
        public static IServiceCollection AddFunctionILoggerSupport(this IServiceCollection services)
            => services
                .AddFunctionContextAccessor()
                .AddScoped<ILogger>(svc =>
                {
                    var functionContext = svc.GetRequiredService<IFunctionContextAccessor>()?.FunctionContext;
                    if (functionContext is null)
                        throw new InvalidOperationException(
                            $"The Function Context is required to initialize a valid ILogger instance." +
                            $" The {nameof(IFunctionContextAccessor)}.{nameof(IFunctionContextAccessor.FunctionContext)} is null or missing." +
                            $" Ensure that you have correctly enabled the Function Context Accessor middleware by calling UseFunctionContextAccessor() in the application startup process."
                        );

                    return functionContext.GetLogger(functionContext.FunctionDefinition.Name);
                });
    }
}

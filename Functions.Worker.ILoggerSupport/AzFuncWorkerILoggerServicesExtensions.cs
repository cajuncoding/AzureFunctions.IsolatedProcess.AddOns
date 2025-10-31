using Functions.Worker.ContextAccessor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Functions.Worker.ILoggerSupport
{
    public static class AzFuncWorkerILoggerServicesExtensions
    {
        public static IServiceCollection AddFunctionILoggerSupport(this IServiceCollection services)
            => services
                .AddFunctionContextAccessor()
                //NOTE: We use Transient lifetime to enable support within DI within any context including Singleton, Scoped, etc.
                //      just as the IFunctionContextAccessor is initialized. To mitigate any performance impace the ILogger instance
                //      is cached per function invocation using the FunctionContext Items dictionary...
                .AddTransient<ILogger>(svc =>
                {
                    var functionContext = svc.GetRequiredService<IFunctionContextAccessor>()?.FunctionContext;
                    if (functionContext is null)
                        throw new InvalidOperationException(
                            $"The Function Context is required to initialize a valid ILogger instance." +
                            $" The {nameof(IFunctionContextAccessor)}.{nameof(IFunctionContextAccessor.FunctionContext)} is null or missing." +
                            $" Ensure that you have correctly enabled the Function Context Accessor middleware by calling UseFunctionContextAccessor() in the application startup process."
                        );

                    return functionContext.GetILogger();
                });
    }
}

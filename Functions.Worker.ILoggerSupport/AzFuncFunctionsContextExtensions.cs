using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Functions.Worker.ILoggerSupport
{
    public static class AzFuncWorkerILoggerFunctionContextExtensions
    {
        public static readonly string ILoggerFunctionContextItemKey = "Functions.Worker.ILoggerSupport.ILoggerInstance";

        public static ILogger GetILogger(this FunctionContext functionContext)
        {
            //Use FunctionContext Items to cache the logger instance per function invocation...
            if (functionContext.Items.TryGetValue(ILoggerFunctionContextItemKey, out var logger))
                return (ILogger)logger;

            var newLogger = functionContext.GetLogger(functionContext.FunctionDefinition.Name);
            functionContext.Items[ILoggerFunctionContextItemKey] = newLogger;

            return newLogger;
        }
    }
}

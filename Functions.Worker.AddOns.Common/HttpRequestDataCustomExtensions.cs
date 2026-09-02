using Microsoft.Azure.Functions.Worker.Http;


namespace Functions.Worker.AddOns.Common
{
    public static class AzureFunctionsClaimTypes
    {
        public const string FunctionKeyId = "http://schemas.microsoft.com/2017/07/functions/claims/keyid";
    }

    public static class HttpRequestDataCustomExtensions
    {
        public static string? GetClaimValue(this HttpRequestData? httpRequestData, string claimType)
            => string.IsNullOrWhiteSpace(claimType) 
            ? null 
            : httpRequestData?.Identities
                .Select(identity => identity.FindFirst(claimType)?.Value)
                .FirstOrDefault(value => value is not null);

        public static string? GetFunctionKeyName(this HttpRequestData? httpRequestData)
            => httpRequestData.GetClaimValue(AzureFunctionsClaimTypes.FunctionKeyId);
    }       
}

using Microsoft.AspNetCore.Builder;

namespace Ocelot.ManualTest.Authentication
{
    public static class TwAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseTwAuthenticationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TwAuthenticationMiddleware>(builder);
        }
    }
}
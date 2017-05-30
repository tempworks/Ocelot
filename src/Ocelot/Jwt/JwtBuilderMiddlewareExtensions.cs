using Microsoft.AspNetCore.Builder;

namespace Ocelot.Jwt
{
    public static class JwtBuilderMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtBuilderMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtExchangeMiddleware>(builder);
        }
    }
}
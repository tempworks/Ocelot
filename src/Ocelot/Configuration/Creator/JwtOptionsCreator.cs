using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IJwtOptionsCreator
    {
        JwtOptions Create(FileReRoute fileReRoute);
    }

    public class JwtOptionsCreator : IJwtOptionsCreator
    {
        public JwtOptions Create(FileReRoute fileReRoute)
        {
            return new JwtOptionsBuilder()
                .WithProvider(fileReRoute.JwtOptions?.Provider)
                .WithProviderRootUrl(fileReRoute.JwtOptions?.ProviderRootUrl)
                .WithApiName(fileReRoute.JwtOptions?.ApiName)
                .WithRequireHttps(fileReRoute.JwtOptions.RequireHttps)
                .WithAllowedScopes(fileReRoute.JwtOptions?.AllowedScopes)
                .WithApiSecret(fileReRoute.JwtOptions?.ApiSecret)
                .WithGrantType(fileReRoute.JwtOptions?.GrantType)
                .Build();
        }
    }
}
using System.Collections.Generic;

namespace Ocelot.Configuration.Builder
{
    public class JwtOptionsBuilder
    {

        private string _provider;
        private string _providerRootUrl;
        private string _apiName;
        private string _apiSecret;
        private bool _requireHttps;
        private List<string> _allowedScopes;
        private string _grantType;

        public JwtOptionsBuilder WithProvider(string provider)
        {
            _provider = provider;
            return this;
        }

        public JwtOptionsBuilder WithProviderRootUrl(string providerRootUrl)
        {
            _providerRootUrl = providerRootUrl;
            return this;
        }

        public JwtOptionsBuilder WithApiName(string apiName)
        {
            _apiName = apiName;
            return this;
        }

        public JwtOptionsBuilder WithApiSecret(string apiSecret)
        {
            _apiSecret = apiSecret;
            return this;
        }

        public JwtOptionsBuilder WithRequireHttps(bool requireHttps)
        {
            _requireHttps = requireHttps;
            return this;
        }

        public JwtOptionsBuilder WithAllowedScopes(List<string> allowedScopes)
        {
            _allowedScopes = allowedScopes;
            return this;
        }

        public JwtOptionsBuilder WithGrantType(string grantType)
        {
            _grantType = grantType;
            return this;
        }

        public JwtOptions Build()
        {
            return new JwtOptions(_provider, _providerRootUrl, _apiName, _requireHttps, _allowedScopes, _apiSecret, _grantType);
        }
    }
}
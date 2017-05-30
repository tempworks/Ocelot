using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileJwtOptions
    {
        public FileJwtOptions()
        {
			AllowedScopes = new List<string>();
        }

        public string Provider { get; set; }
        public string ProviderRootUrl { get; set; }
        public string ApiName { get; set; }
        public bool RequireHttps { get; set; }
        public List<string> AllowedScopes { get; set; }
        public string ApiSecret { get; set; }
        public string GrantType { get; set; }
    }
}

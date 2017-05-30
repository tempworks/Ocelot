namespace Ocelot.Configuration
{
    public class ReRouteOptions
    {
        public ReRouteOptions(bool isAuthenticated, bool isAddJwtToRequest, bool isAuthorised, bool isCached, bool isQos, bool isEnableRateLimiting)
        {
            IsAuthenticated = isAuthenticated;
            IsAddJwtToRequest = isAddJwtToRequest;
            IsAuthorised = isAuthorised;
            IsCached = isCached;
            IsQos = isQos;
            EnableRateLimiting = isEnableRateLimiting;

        }
        public bool IsAuthenticated { get; private set; }
        public bool IsAddJwtToRequest { get; private set; }
        public bool IsAuthorised { get; private set; }
        public bool IsCached { get; private set; }
        public bool IsQos { get; private set; }
        public bool EnableRateLimiting { get; private set; }
    }
}
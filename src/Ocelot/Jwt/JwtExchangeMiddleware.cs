using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using Ocelot.Authentication.Handler.Factory;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Requester;

namespace Ocelot.Jwt
{
    public class JwtExchangeMiddleware : OcelotMiddleware
    {
        private static HttpClient _httpClient;
        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _app;
        private readonly IAuthenticationHandlerFactory _authHandlerFactory;
        private readonly IOcelotLogger _logger;

        public JwtExchangeMiddleware(RequestDelegate next,
            IApplicationBuilder app,
            IRequestScopedDataRepository requestScopedDataRepository,
            IAuthenticationHandlerFactory authHandlerFactory,
            IOcelotLoggerFactory loggerFactory)
            : base(requestScopedDataRepository)
        {
            _httpClient = new HttpClient();
            _next = next;
            _authHandlerFactory = authHandlerFactory;
            _app = app;
            _logger = loggerFactory.CreateLogger<JwtExchangeMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started jwt builder middleware");

            // does this reoute have jwt auth?
            if (IsJwtEnabledRoute(DownstreamRoute.ReRoute))
            {
                _logger.LogDebug("this route has instructions to exchange token for jwt");

                // get bearer token
                string authBearerToken = ParseAuthBearerTokenFromRequest(DownstreamRequest);
                string twToken = null;

                // if bearer not found, look for tw-token
                if (string.IsNullOrWhiteSpace(authBearerToken))
                {
                    twToken = ParseTwTokenFromRequest(DownstreamRequest);
                }

                // get jwt
                string jwt = null;

                if (!string.IsNullOrWhiteSpace(authBearerToken))
                {
                    _logger.LogDebug("found bearer reference token");

                    jwt = await ExchangeAuthBearerTokenForJwt(authBearerToken);

                    _logger.LogDebug("bearer reference token exchanged to jwt");
                }

                if (!string.IsNullOrWhiteSpace(twToken))
                {
                    _logger.LogDebug("found tw-token");

                    jwt = await ExchangeTwTokenForJwt(twToken);

                    _logger.LogDebug("tw-token exchanged to jwt");
                }

                // attach jwt to upstream request
                if (!string.IsNullOrWhiteSpace(jwt))
                {
                    _logger.LogDebug("setting jwt on request");

                    Request.HttpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", jwt);
                }
                else
                {
                    _logger.LogDebug("error exchanging jwt");
                }


            }

            _logger.LogDebug("calling next middleware");

            await _next.Invoke(context);

            _logger.LogDebug("succesfully called next middleware");
        }

        private static bool IsJwtEnabledRoute(ReRoute reRoute)
        {
            return reRoute.IsAddJwtToRequest;
        }

        private string ParseAuthBearerTokenFromRequest(HttpRequestMessage request)
        {
            request.Headers.TryGetValues("Authorization", out var authTokenHeader);
            var authHeader = authTokenHeader?.FirstOrDefault() ?? string.Empty;

            var authBearerToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring(7)
                : null;

            return authBearerToken;
        }

        private string ParseTwTokenFromRequest(HttpRequestMessage request)
        {
            request.Headers.TryGetValues("Authorization", out var authTokenHeader);
            var authHeader = authTokenHeader?.FirstOrDefault() ?? string.Empty;

            // look in header
            DownstreamRequest.Headers.TryGetValues("x-tw-token", out var twTokenHeader);
            var twTokenHeaderValue = twTokenHeader?.FirstOrDefault() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(twTokenHeaderValue))
            {
                return twTokenHeaderValue;
            }

            // look in auth basic header
            var authBasicToken = authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring(6)
                : null;

            if (!string.IsNullOrWhiteSpace(authBasicToken))
            {
                return authBasicToken;
            }

            // look in query string
            var twTokenQueryString = QueryHelpers.ParseQuery(DownstreamRequest.RequestUri.Query)
                ?.FirstOrDefault(q => q.Key.Equals("tw-token", StringComparison.OrdinalIgnoreCase));

            if (twTokenQueryString.HasValue)
            {
                return twTokenQueryString.Value.Value;
            }

            return null;
        }

        private async Task<string> ExchangeAuthBearerTokenForJwt(string authBearerToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{DownstreamRoute.ReRoute.JwtOptions.ProviderRootUrl}/connect/token")
            {
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "exchange_reference_token"),
                    new KeyValuePair<string, string>("client_id", DownstreamRoute.ReRoute.JwtOptions.ApiName),
                    new KeyValuePair<string, string>("client_secret", DownstreamRoute.ReRoute.JwtOptions.ApiSecret),
                    new KeyValuePair<string, string>("scope", "twapi3 allow-full-access"),
                    new KeyValuePair<string, string>("token", authBearerToken)
                })
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(json);

            var jwt = jobj.GetValue("access_token");
            return jwt.Value<string>();
        }

        private async Task<string> ExchangeTwTokenForJwt(string twToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{DownstreamRoute.ReRoute.JwtOptions.ProviderRootUrl}/connect/token")
            {
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "tw_token"),
                    new KeyValuePair<string, string>("client_id", DownstreamRoute.ReRoute.JwtOptions.ApiName),
                    new KeyValuePair<string, string>("client_secret", DownstreamRoute.ReRoute.JwtOptions.ApiSecret),
                    new KeyValuePair<string, string>("token", twToken)
                })
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(json);

            var jwt =  jobj.GetValue("access_token");
            return jwt.Value<string>();
        }

        public class BasicAuthenticationCredential
        {
            public string Id { get; set; }
            public string Secret { get; set; }
            public string Header { get; set; }

            public static BasicAuthenticationCredential Extract(string header)
            {
                if (string.IsNullOrEmpty(header)) throw new ArgumentNullException(nameof(header));

                var credential = new BasicAuthenticationCredential
                {
                    Header = header
                };

                string pair;
                try
                {
                    header = header.Replace("Basic ", "");
                    pair = Encoding.UTF8.GetString(
                        Convert.FromBase64String(header));//.Substring("Basic ".Length)));
                }
                catch (FormatException ex)
                {
                    throw new MalformedCredentialException(credential, ex);
                }
                catch (ArgumentException ex)
                {
                    throw new MalformedCredentialException(credential, ex);
                }

                var ix = pair.IndexOf(':');
                if (ix == -1)
                {
                    throw new MalformedCredentialException(credential);
                }

                credential.Id = pair.Substring(0, ix);
                credential.Secret = pair.Substring(ix + 1);

                return credential;
            }
        }

        public class MalformedCredentialException : Exception
        {
            private readonly BasicAuthenticationCredential _credential;

            public MalformedCredentialException() { }
            public MalformedCredentialException(BasicAuthenticationCredential credential) : base("Malformed credential") { _credential = credential; }
            public MalformedCredentialException(BasicAuthenticationCredential credential, Exception inner) : base("Malformed credential", inner) { _credential = credential; }
        }
    }


}

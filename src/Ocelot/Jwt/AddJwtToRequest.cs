using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Responses;

namespace Ocelot.Jwt
{
    public interface IAddJwtToRequest
    {
        Response SetJwtOnContext(List<ClaimToThing> claimsToThings, HttpContext context);
    }

    public class AddJwtToRequest : IAddJwtToRequest
    {
        private readonly IClaimsParser _claimsParser;

        public AddJwtToRequest(IClaimsParser claimsParser)
        {
            _claimsParser = claimsParser;
        }

        public Response SetJwtOnContext(List<ClaimToThing> claimsToThings, HttpContext context)
        {
            foreach (var config in claimsToThings)
            {
                var value = _claimsParser.GetValue(context.User.Claims, config.NewKey, config.Delimiter, config.Index);

                if (value.IsError)
                {
                    return new ErrorResponse(value.Errors);
                }

                var exists = context.User.Claims.FirstOrDefault(x => x.Type == config.ExistingKey);

                var identity = context.User.Identity as ClaimsIdentity;

                if (exists != null)
                {
                    identity?.RemoveClaim(exists);
                }

                identity?.AddClaim(new System.Security.Claims.Claim(config.ExistingKey, value.Data));
            }

            var response = new OkResponse();

            return response;
        }
    }
}

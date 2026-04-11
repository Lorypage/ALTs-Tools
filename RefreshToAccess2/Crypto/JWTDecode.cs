using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json.Linq;

namespace RefreshToAccess2.Crypto
{
    internal class JWTDecode
    {
        public static string GetDecodedJWTExpDate(string jwtString)
        {
            var token = new JwtSecurityToken(jwtEncodedString: jwtString);
            string expiryDate = token.Claims.First(c => c.Type == "exp").Value;
            return expiryDate;
        }
    }
}

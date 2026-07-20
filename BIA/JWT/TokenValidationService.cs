using BIA.BLL.BLLServices;
using BIA.Entity.ResponseEntity;
using BIA.Entity.ViewModel;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;

namespace BIA.JWT
{
    public class TokenValidationService
    {

        private readonly string _secretKey;

        private readonly BLLForbiddenWords _bllForbiddenWords = new BLLForbiddenWords();

        public TokenValidationService(string secretKey)
        {
            _secretKey = secretKey;
        }

        public ValidTokenResponse ValidateToken(string token)
        {
            ValidTokenResponse response = new ValidTokenResponse();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                SecurityToken validatedToken;
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                response.LoginProviderId = claimsPrincipal.FindFirst("loginProvider")?.Value;
                response.ChannelName = claimsPrincipal.FindFirst("channel_name")?.Value;
                response.UserName = claimsPrincipal.FindFirst("user_name")?.Value;
                response.DistributorCode = claimsPrincipal.FindFirst("distributor_code")?.Value;
                response.CenterCode = claimsPrincipal.FindFirst("center_code")?.Value;

                response.IsVallid = true;
                return response;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Contains("expired"))
                {
                    response.Message = "The session token is expired!";
                }
                else
                {
                    response.Message = ex.Message.ToString();
                }

                response.IsVallid = false;

                return response;

                //if (ex is SecurityTokenExpiredException)
                //{
                //    // Throw 401 Unauthorized
                //    throw new UnauthorizedAccessException("The session token is expired!");
                //}

                //throw new Exception("The session token is expired! " + ex.Message);
            }
        }

        public async Task<ValidTokenResponse> ValidateDBSSTokenV2(string token)
        {
            ValidTokenResponse response = new ValidTokenResponse();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,  // Set to true if issuer validation is required
                ValidateAudience = false,  // Set to true if audience validation is required
                ClockSkew = TimeSpan.Zero  // No tolerance for expiration time
            };

            try
            {
                List<ForbiddenWords> forbiddenWords = await _bllForbiddenWords.GetForbiddenWordsAsync();

                string originalToken = ReverseToken(token, forbiddenWords);

                SecurityToken validatedToken;
                var claimsPrincipal = tokenHandler.ValidateToken(originalToken, validationParameters, out validatedToken);

                var logindClaim = claimsPrincipal.FindFirst("loginProvider");
                var usernameClaim = claimsPrincipal.FindFirst("user_name");

                if (logindClaim != null)
                {
                    response.LoginProviderId = logindClaim.Value;
                }
                if (usernameClaim != null)
                {
                    response.UserName = usernameClaim.Value;
                }
                response.IsVallid = true;
                return response;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Contains("expired"))
                {
                    response.Message = "Invalid security token!";
                }
                else
                {
                    response.Message = ex.Message.ToString();
                }

                response.IsVallid = false;

                return response;

                //if (ex is SecurityTokenExpiredException)
                //{
                //    // Throw 401 Unauthorized
                //    throw new UnauthorizedAccessException("The session token is expired!");
                //}

                //throw new Exception("The session token is expired! " + ex.Message);
            }
        }

        public ValidTokenResponse ValidateTokenV2(string token)
        {
            ValidTokenResponse response = new ValidTokenResponse();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,  // Set to true if issuer validation is required
                ValidateAudience = false,  // Set to true if audience validation is required
                ClockSkew = TimeSpan.Zero  // No tolerance for expiration time
            };

            try
            {
                SecurityToken validatedToken;
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                var logindClaim = claimsPrincipal.FindFirst("loginProvider");
                var usernameClaim = claimsPrincipal.FindFirst("user_name");

                if (logindClaim != null)
                {
                    response.LoginProviderId = logindClaim.Value;
                }
                if (usernameClaim != null)
                {
                    response.UserName = usernameClaim.Value;
                }
                response.IsVallid = true;
                return response;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Contains("expired"))
                {
                    //response.Message = "The session token is expired!";
                    response.Message = "The session token is expired! Please logout and re-login";
                }
                else
                {
                    response.Message = ex.Message.ToString();
                }
                response.IsVallid = false;
                return response;
            }
        }


        public ValidTokenResponse ValidateDBSSToken(string token)
        {
            ValidTokenResponse response = new ValidTokenResponse();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,  // Set to true if issuer validation is required
                ValidateAudience = false,  // Set to true if audience validation is required
                ClockSkew = TimeSpan.Zero  // No tolerance for expiration time
            };

            try
            {
                SecurityToken validatedToken;
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                var logindClaim = claimsPrincipal.FindFirst("loginProvider");
                var usernameClaim = claimsPrincipal.FindFirst("user_name");

                if (logindClaim != null)
                {
                    response.LoginProviderId = logindClaim.Value;
                }
                if (usernameClaim != null)
                {
                    response.UserName = usernameClaim.Value;
                }
                response.IsVallid = true;
                return response;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToString().Contains("expired"))
                {
                    response.Message = "Invalid security token!";
                }
                else
                {
                    response.Message = ex.Message.ToString();
                }
                response.IsVallid = false;
                return response;
            }
        }


        #region Seesion token sanitization ridwan's solution
        public async Task<ValidTokenResponse> ValidateDBSSTokenV3(string token)
        {
            ValidTokenResponse response = new ValidTokenResponse();

            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                SecurityToken validatedToken;
                var principal = handler.ValidateToken(token, parameters, out validatedToken);

                response.UserName = DecodeBase64Url(principal.FindFirst("user_name")?.Value);
                response.LoginProviderId = DecodeBase64Url(principal.FindFirst("loginProvider")?.Value);
                response.CenterCode = DecodeBase64Url(principal.FindFirst("center_code")?.Value);
                response.DistributorCode = DecodeBase64Url(principal.FindFirst("distributor_code")?.Value);
                response.ChannelName = DecodeBase64Url(principal.FindFirst("channel_name")?.Value);
                response.Jti = DecodeBase64Url(principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value);
                response.IsVallid = true;
                return response;

            }
            catch (Exception ex)
            {
                response.IsVallid = false;
                return response;
            }
        }


        #region Seesion token sanitization ridwan's solution

        private string DecodeBase64Url(string encoded)
        {
            var bytes = Base64UrlEncoder.DecodeBytes(encoded);
            return Encoding.UTF8.GetString(bytes);
        }
        #endregion

        #endregion
        public string ReverseToken(string sanitizedToken, List<ForbiddenWords> forbiddenWords)
        {
            string originalToken = sanitizedToken;

            foreach (var fw in forbiddenWords)
            {
                if (!string.IsNullOrWhiteSpace(fw.Alternate))
                {
                    // Replace Alternate back with original Word
                    originalToken = ReplaceIgnoreCase(originalToken, fw.Alternate, fw.Word);
                }
            }

            return originalToken;
        }

        // Case-insensitive replace helper
        private string ReplaceIgnoreCase(string text, string search, string replacement)
        {
            return Regex.Replace(
                text,
                Regex.Escape(search),
                replacement
            );
        }

        public ValidTokenResponse ValidateExternalToken(string token)
        {
            ValidTokenResponse response = new ValidTokenResponse();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                SecurityToken validatedToken;
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);

                response.LoginProviderId = claimsPrincipal.FindFirst("loginProvider")?.Value;
                response.ChannelName = claimsPrincipal.FindFirst("channel_name")?.Value;
                response.UserName = claimsPrincipal.FindFirst("user_name")?.Value;

                response.IsVallid = true;
                return response;
            }
            catch (Exception ex)
            {
                if (ex is SecurityTokenExpiredException)
                {
                    throw new UnauthorizedAccessException("The session token is expired!");
                }

                throw new Exception("The session token issue! " + ex.Message);
            }
        }

    }
}

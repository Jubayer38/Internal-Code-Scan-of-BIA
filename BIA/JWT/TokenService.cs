using BIA.BLL.BLLServices;
using BIA.Entity.Collections;
using BIA.Entity.ResponseEntity;
using BIA.Entity.ViewModel;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BIA.JWT
{
    public class TokenService
    {
        private readonly string _secretKey;
        private static int sessionTokenExipreMinutes = SettingsValues.GetSessionTokenExiprationTime();
        private readonly BLLForbiddenWords _bllForbiddenWords = new BLLForbiddenWords();

        public TokenService(string secretKey)
        {
            _secretKey = secretKey;
        }

        public string GenerateToken(LoginUserInfoResponseRev loginUser, string loginprovider)
        {
            var claims = new[]
            {
            new Claim("loginProvider", loginprovider),
            new Claim("channel_name",loginUser.channel_name.ToString()),
            new Claim("user_name", loginUser.user_name.ToString()),
            new Claim("distributor_code",loginUser.distributor_code.ToString()),
            new Claim("center_code",loginUser.center_code.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // DateTime currentDateTime = DateTime.Now.Date;
            // DateTime expirationTime = currentDateTime.AddHours(24);
            DateTime currentDateTime = DateTime.Now;
            DateTime expirationTime = currentDateTime.AddMinutes(sessionTokenExipreMinutes);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expirationTime,  // Set token expiration
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateTokenV2(ResellerLoginUserInfoResponse loginUser, string loginprovider)
        {
            var claims = new[]
            {
            new Claim("loginProvider", loginprovider),
            new Claim("channel_name",loginUser.channel_name.ToString()),
            new Claim("user_name", loginUser.user_name.ToString()),
            new Claim("distributor_code",loginUser.distributor_code.ToString()),
            new Claim("center_code",loginUser.center_code.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // DateTime currentDateTime = DateTime.Now.Date;
            // DateTime expirationTime = currentDateTime.AddHours(24);
            DateTime currentDateTime = DateTime.Now;
            DateTime expirationTime = currentDateTime.AddMinutes(sessionTokenExipreMinutes);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expirationTime,  // Set token expiration
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateTokenV3(string user_name, string loginprovider)
        {
            var claims = new[]
            {
            new Claim("loginProvider", loginprovider),
            new Claim("user_name", user_name.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // DateTime currentDateTime = DateTime.Now.Date;
            // DateTime expirationTime = currentDateTime.AddHours(24);
            DateTime currentDateTime = DateTime.Now;
            DateTime expirationTime = currentDateTime.AddMinutes(sessionTokenExipreMinutes);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expirationTime,  // Set token expiration
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateTokenDBSS(LoginUserInfoResponse loginUser, string loginprovider)
        {
            List<ForbiddenWords> forbiddenWords = await _bllForbiddenWords.GetForbiddenWordsAsync();//_bllForbiddenWords.GetForbiddenWordsAsync().GetAwaiter().GetResult();//SettingsValues.GetForbiddenWords();
            string token;
            int retryCount = 0;

            do
            {
                retryCount++;
                loginprovider = Guid.NewGuid().ToString();
                token = CreateJwt(loginUser, loginprovider);
            }
            while (
            retryCount > 20 || forbiddenWords.Any(w => token.Contains(w.Word, StringComparison.OrdinalIgnoreCase))
            );

            return token;
        }

        public async Task<string> GenerateTokenDBSS_V2(LoginUserInfoResponse loginUser, string loginprovider)
        {
            List<ForbiddenWords> forbiddenWords = await _bllForbiddenWords.GetForbiddenWordsAsync();

            string token;
            int retryCount = 0;

            do
            {
                retryCount++;

                // Step 1: Generate new provider + token
                loginprovider = Guid.NewGuid().ToString();
                token = CreateJwt(loginUser, loginprovider);

                // -----------------------------------------------------
                // STEP 1: REPLACE WORDS WITH ALTERNATES (max 10 rounds)
                // -----------------------------------------------------
                for (int i = 0; i < 20; i++)
                {
                    bool replaced = false;

                    foreach (var fw in forbiddenWords)
                    {
                        if (!string.IsNullOrWhiteSpace(fw.Alternate))   // Only replace if Alternate exists
                        {
                            if (token.Contains(fw.Word, StringComparison.OrdinalIgnoreCase))
                            {
                                token = ReplaceIgnoreCase(token, fw.Word, fw.Alternate);
                                replaced = true;
                            }
                        }
                    }

                    if (!replaced) break; // No more replacements → exit early
                }

                // -----------------------------------------------------
                // STEP 2: IF STILL HAS FORBIDDEN WORDS (NO ALTERNATE), REGENERATE TOKEN
                // -----------------------------------------------------
            }
            while (
                retryCount <= 20 &&
                forbiddenWords.Any(w => token.Contains(w.Word, StringComparison.OrdinalIgnoreCase))
            );

            if (retryCount > 20)
            {
                return "";
            }

            return token;
        }

        #region Session token Sanitization Ridwan's Solution
        public async Task<string> GenerateTokenDBSS_V3(LoginUserInfoResponse loginUser, string loginprovider)
        {
            List<ForbiddenWords> forbiddenWords = await _bllForbiddenWords.GetForbiddenWordsAsync();

            int retryCount = 0;
            const int maxRetry = 10;

            string token = string.Empty;

            do
            {
                retryCount++;

                // 1️⃣ Encode claim values (for guaranteed safety)
                string encodedUser = EncodeBase64Url(loginUser.user_name);
                string encodedProvider = EncodeBase64Url(Guid.NewGuid().ToString());
                string encodedDistributor = EncodeBase64Url(loginUser.distributor_code);
                string encodedCenter = EncodeBase64Url(loginUser.center_code);
                string encodedChannel = EncodeBase64Url(loginUser.channel_name);


                // 2️⃣ Generate a fresh JWT
                token = CreateJwtSafe(encodedUser, encodedProvider, encodedDistributor, encodedCenter, encodedChannel);

                // 3️⃣ Check token for forbidden words
                if (!ContainsForbidden(token, forbiddenWords))
                {
                    return token;  // ✔ TOKEN IS 100% CLEAN
                }

            }
            while (retryCount < maxRetry);

            // If still dirty after retries → fail gracefully
            return string.Empty;
        }
        #endregion

        private bool ContainsForbidden(string token, List<ForbiddenWords> forbiddenWords)
        {
            foreach (var fw in forbiddenWords)
            {
                if (token.Contains(fw.Word, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private string EncodeBase64Url(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return Base64UrlEncoder.Encode(bytes);
        }



        private string CreateJwtSafe(string encodedUser, string encodedProvider, string encodedDistributor, string encodedCenter, string encodedChannel)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var claims = new[]
        {
         new Claim("user_name", encodedUser),
         new Claim("loginProvider", encodedProvider),
         new Claim("distributor_code", encodedDistributor),
         new Claim("center_code", encodedCenter),
         new Claim("channel_name", encodedChannel),
         new Claim(JwtRegisteredClaimNames.Jti, EncodeBase64Url(Guid.NewGuid().ToString()))
        };

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(descriptor);
            return tokenHandler.WriteToken(token);
        }


        private string ReplaceIgnoreCase(string text, string search, string replacement)
        {
            return Regex.Replace(
                text,
                Regex.Escape(search),
                replacement
            //RegexOptions.IgnoreCase
            );
        }

        public string GenerateTokenForRetailerLogin(ResellerLoginUserInfoResponse loginUser, string loginprovider)
        {
            var claims = new[]
            {
            new Claim("loginProvider", loginprovider),
            new Claim("channel_name",loginUser.channel_name.ToString()),
            new Claim("user_name", loginUser.user_name.ToString()),
            new Claim("distributor_code",loginUser.distributor_code.ToString()),
            new Claim("center_code",loginUser.center_code.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // DateTime currentDateTime = DateTime.Now.Date;
            // DateTime expirationTime = currentDateTime.AddHours(24);
            DateTime currentDateTime = DateTime.Now;
            DateTime expirationTime = currentDateTime.AddMinutes(sessionTokenExipreMinutes);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expirationTime,  // Set token expiration
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string CreateJwt(LoginUserInfoResponse loginUser, string loginprovider)
        {
            int sessionTokenExipreMinutes = SettingsValues.GetDbssSessionTokenExiprationTime();

            var claims = new[]
            {
                new Claim("loginProvider", loginprovider),
                new Claim("channel_name", loginUser.channel_name),
                new Claim("user_name", loginUser.user_name),
                new Claim("distributor_code", loginUser.distributor_code),
                new Claim("center_code", loginUser.center_code),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            DateTime expirationTime = DateTime.Now.AddMinutes(sessionTokenExipreMinutes);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expirationTime,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateTokenForExternal(ExternalUserValidationRespModel loginUser, string loginprovider)
        {
            var claims = new[]
            {
            new Claim("loginProvider", loginprovider),
            new Claim("channel_name",loginUser.channel_name.ToString()),
            new Claim("user_name", loginUser.user_name.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // DateTime currentDateTime = DateTime.Now.Date;
            // DateTime expirationTime = currentDateTime.AddHours(24);
            DateTime currentDateTime = DateTime.Now;
            DateTime expirationTime = currentDateTime.AddMinutes(SettingsValues.GetExternalLoginExiprationTime());

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expirationTime,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }
}

using BIA.Entity.Collections;
using BIA.Entity.ResponseEntity;
using BIA.JWT;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace BIA.Helper
{
    public class CustomAuthorizationFilter : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
            {
                if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var token))
                {
                    context.Result = new JsonResult(new { message = "Authorization token not found in request header." }) { StatusCode = 401 };
                    return;
                }
                string secreteKey = string.Empty;
                string prov_id = string.Empty;

                secreteKey = SettingsValues.GetJWTSequrityKey();
                ValidTokenResponse security = new ValidTokenResponse();
                TokenValidationService tokenService = new TokenValidationService(secreteKey);
                if (!String.IsNullOrEmpty(token))
                {
                    security = tokenService.ValidateExternalToken(token);
                }
                else
                {
                    context.Result = new JsonResult(new { message = "Token not found in request header." }) { StatusCode = 401 };
                    return;
                }                

                if (!security.IsVallid)
                {
                    context.Result = new JsonResult(new { message = "Unauthorized user" }) { StatusCode = 403 };
                    return;
                }
                else
                {
                    return;
                }
            }
        }
    }
}

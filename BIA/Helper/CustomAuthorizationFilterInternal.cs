using BIA.Entity.Collections;
using BIA.Entity.ResponseEntity;
using BIA.JWT;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace BIA.Helper
{
    public class CustomAuthorizationFilterInternal : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            try
            {
                if (!context.ActionDescriptor.EndpointMetadata
                    .OfType<AllowAnonymousAttribute>()
                    .Any())
                {
                    //------------------------------------------------
                    // AUTHORIZATION TOKEN
                    //------------------------------------------------
                    if (!context.HttpContext.Request.Headers
                        .TryGetValue("Authorization", out var token))
                    {
                        context.Result = new JsonResult(new
                        {
                            message = "Authorization token not found in request header."
                        })
                        {
                            StatusCode = 401
                        };

                        return;
                    }

                    //------------------------------------------------
                    // USER NAME HEADER
                    //------------------------------------------------
                    if (!context.HttpContext.Request.Headers
                        .TryGetValue("user_name", out var headerUserName))
                    {
                        context.Result = new JsonResult(new
                        {
                            message = "user_name not found in request header."
                        })
                        {
                            StatusCode = 401
                        };

                        return;
                    }

                    string secretKey = SettingsValues.GetJWTSequrityKey();

                    ValidTokenResponse security = new ValidTokenResponse();

                    TokenValidationService tokenService =
                        new TokenValidationService(secretKey);

                    //------------------------------------------------
                    // VALIDATE TOKEN
                    //------------------------------------------------
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        security = tokenService.ValidateToken(token);
                    }
                    else
                    {
                        context.Result = new JsonResult(new
                        {
                            message = "Token not found in request header."
                        })
                        {
                            StatusCode = 401
                        };

                        return;
                    }

                    //------------------------------------------------
                    // INVALID TOKEN
                    //------------------------------------------------
                    if (!security.IsVallid)
                    {
                        context.Result = new JsonResult(new
                        {
                            message = "Unauthorized user"
                        })
                        {
                            StatusCode = 403
                        };

                        return;
                    }

                    //------------------------------------------------
                    // USER NAME VALIDATION
                    //------------------------------------------------
                    string tokenUserName =
                        security.UserName?.Trim() ?? string.Empty;

                    string requestUserName =
                        headerUserName.ToString().Trim();

                    Log.ForContext("LogTag", "Authorization")
                        .Information(
                            "Authorization Validation Request: {@Request}",
                            new
                            {
                                request_user_name = requestUserName,
                                token_user_name = tokenUserName
                            });

                    if (!string.Equals(
                            tokenUserName,
                            requestUserName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Log.ForContext("LogTag", "Authorization")
                            .Warning(
                                "Unauthorized user. Header username and token username mismatch.");

                        context.Result = new JsonResult(new
                        {
                            message = "Unauthorized user. Username mismatch."
                        })
                        {
                            StatusCode = 403
                        };

                        return;
                    }

                    //------------------------------------------------
                    // AUTHORIZED
                    //------------------------------------------------
                    Log.ForContext("LogTag", "Authorization")
                        .Information(
                            "Authorization Success for user: {UserName}",
                            requestUserName);

                    return;
                }
            }
            catch (Exception ex)
            {
                Log.ForContext("LogTag", "Authorization")
                    .Error(ex, "CustomAuthorizationFilterInternal Exception");

                context.Result = new JsonResult(new
                {
                    message = "Authorization failed."
                })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
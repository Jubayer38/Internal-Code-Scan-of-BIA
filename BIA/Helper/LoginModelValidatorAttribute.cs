using BIA.Entity.CommonEntity;
using BIA.Entity.RequestEntity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BIA.Helper
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class LoginModelValidatorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var model = context.ActionArguments.Values.FirstOrDefault(v => v is LoginRequestsV2) as LoginRequestsV2;

            if (model != null)
            {
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(model.UserName))
                    errors.Add("Retailer Code is required, please allow app permission and Resubmit");

                if (string.IsNullOrWhiteSpace(model.Password))
                    errors.Add("Password is required, please allow app permission and Resubmit.");

                if (model.VersionCode == 0 || model.VersionCode < 0)
                    errors.Add("VersionCode is required, please allow app permission and Resubmit.");

                if (string.IsNullOrWhiteSpace(model.VersionName))
                    errors.Add("VersionName is required, please allow app permission and Resubmit.");                

                if (errors.Any())
                {
                    var errorMessage = errors.First();

                    context.Result = new OkObjectResult(new RACommonResponseRevamp
                    {
                        isError = true,
                        message = errorMessage,
                        data = new Datas
                        {
                            isEsim = 0,
                            request_id = " "
                        }
                    });

                    return;
                }
            }
            else
            {
                context.Result = new OkObjectResult(new RACommonResponseRevamp
                {
                    isError = true,
                    message = "Invalid request payload (data type mismatch). Please try again.",
                    data = new Datas
                    {
                        isEsim = 0,
                        request_id = " "
                    }
                });

                return;
            }
        }
    }
}

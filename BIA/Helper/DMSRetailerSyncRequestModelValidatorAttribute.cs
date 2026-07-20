using BIA.Entity.CommonEntity;
using BIA.Entity.RequestEntity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using System.Text;

namespace BIA.Helper
{
    public class DMSRetailerSyncRequestModelValidatorAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var errors = new List<string>();
            DMSRetailerSyncRequestModel? model = null;

            // Safe content type check
            var isForm = request.ContentType?.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true;

            if (isForm)
            {
                model = context.ActionArguments
                              .Values
                              .FirstOrDefault(v => v is DMSRetailerSyncRequestModel) as DMSRetailerSyncRequestModel;
            }
            else
            {
                request.EnableBuffering();
                request.Body.Position = 0;

                try
                {
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                    var bodyStr = await reader.ReadToEndAsync();
                    request.Body.Position = 0;

                    model = JsonSerializer.Deserialize<DMSRetailerSyncRequestModel>(bodyStr, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException)
                {
                    model = null;
                }
            }

            if (model == null)
            {
                context.Result = new BadRequestObjectResult(new RACommonResponseRevamp
                {
                    isError = true,
                    message = "Invalid or missing request payload. Please try again.",
                    data = new Datas { isEsim = 0, request_id = " " }
                });
                return;
            }

            // ✅ Unified validation logic (no duplication)
            ValidateModel(model, errors);

            if (errors.Any())
            {
                context.Result = new OkObjectResult(new RACommonResponseRevamp
                {
                    isError = true,
                    message = errors.First(),
                    data = new Datas { isEsim = 0, request_id = " " }
                });
                return;
            }

            await next(); // Proceed
        }

        private void ValidateModel(DMSRetailerSyncRequestModel model, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(model.userName))
                errors.Add("Technical error!!! UserName is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.password))
                errors.Add("Technical error!!! Password is required, please resubmit the request.");
        }
    }
}

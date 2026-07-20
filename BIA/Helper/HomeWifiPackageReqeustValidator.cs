using BIA.Entity.CommonEntity;
using BIA.Entity.RequestEntity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using System.Text;

namespace BIA.Helper
{
    public class HomeWifiPackageReqeustValidator : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var errors = new List<string>();
            PackagesFetchedRequestModel? model = null;

            // Safe content type check
            var isForm = request.ContentType?.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true;

            if (isForm)
            {
                model = context.ActionArguments
                              .Values
                              .FirstOrDefault(v => v is PackagesFetchedRequestModel) as PackagesFetchedRequestModel;
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

                    model = JsonSerializer.Deserialize<PackagesFetchedRequestModel>(bodyStr, new JsonSerializerOptions
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

        private void ValidateModel(PackagesFetchedRequestModel model, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(model.retailer_id))
        errors.Add("Technical error!!! Retailer Id is required, please resubmit the request.");

    if (string.IsNullOrWhiteSpace(model.channel_name))
        errors.Add("Technical error!!! Channel Name is required, please resubmit the request.");

    // Subscription & Session Validation (If applicable to your business logic)
    if (string.IsNullOrWhiteSpace(model.subscription_id))
        errors.Add("Technical error!!! Subscription Id is required, please resubmit the request.");

    // New Modality Validations
    if (string.IsNullOrWhiteSpace(model.initiator_channel))
        errors.Add("Technical error!!! Initiator Channel is required, please resubmit the request.");

    if (string.IsNullOrWhiteSpace(model.order_type))
        errors.Add("Technical error!!! Order Type is required, please resubmit the request.");

    if (string.IsNullOrWhiteSpace(model.subscription_type))
        errors.Add("Technical error!!! Subscription Type is required, please resubmit the request.");

    if (string.IsNullOrWhiteSpace(model.simkit_type))
        errors.Add("Technical error!!! Simkit Type is required, please resubmit the request.");



        }
    }
}

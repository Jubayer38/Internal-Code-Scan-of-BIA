using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System.Text;
using System.Text.Json;

namespace BIA.Helper
{
    public class HomeWifiReferSubmitOrderRequestValidatorAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            HomeWifiReferOrderRequest? model =
                context.ActionArguments.Values
                    .FirstOrDefault(v => v is HomeWifiReferOrderRequest)
                    as HomeWifiReferOrderRequest;

            if (model == null)
            {
                model = await ReadModelFromRequestBody(context);
            }

            if (model == null)
            {
                context.Result = BuildValidationResponse("Request body is required.");
                return;
            }

            string? validationMessage = ValidateModel(model);

            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                context.Result = BuildValidationResponse(validationMessage);
                return;
            }

            await next();
        }

        private async Task<HomeWifiReferOrderRequest?> ReadModelFromRequestBody(
            ActionExecutingContext context)
        {
            try
            {
                var request = context.HttpContext.Request;

                request.EnableBuffering();

                if (request.Body.CanSeek)
                {
                    request.Body.Position = 0;
                }

                using var reader = new StreamReader(
                    request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true
                );

                string body = await reader.ReadToEndAsync();

                if (request.Body.CanSeek)
                {
                    request.Body.Position = 0;
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<HomeWifiReferOrderRequest>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HomeWifiSubmitOrderRequestValidator ReadModelFromRequestBody Exception");
                return null;
            }
        }

        private string? ValidateModel(HomeWifiReferOrderRequest model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.customer_name))
                    return "Customer name is required.";

                if (string.IsNullOrWhiteSpace(model.plan_code))
                    return "plan code is required.";

                if (model.appointment_date.ToString() == null)
                    return "appoinment date is required.";

                if (string.IsNullOrWhiteSpace(model.retailer_id))
                    return "Retailer Id is required.";

                if (string.IsNullOrWhiteSpace(model.delivery_address))
                    return "delivery_address is required.";

                if (string.IsNullOrWhiteSpace(model.area_code))
                    return "Area Code is required.";

                if (string.IsNullOrWhiteSpace(model.channel_name))
                    return "Channel Name is required.";

                if (string.IsNullOrWhiteSpace(model.device_code))
                    return "Device Code is required.";

                if (string.IsNullOrWhiteSpace(model.district_code))
                    return "District Code is required.";

                if (string.IsNullOrWhiteSpace(model.mobile))
                    return "Mobile number is required.";

                if (string.IsNullOrWhiteSpace(model.plan_code))
                    return "plane code is required.";                                

                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HomeWifiReferOrderRequest ValidateModel Exception");
                return "Validation failed.";
            }
        }

        private IActionResult BuildValidationResponse(string message)
        {
            return new OkObjectResult(new HomeWifiCommonResponseModel
            {
                isError = true,
                message = message,
                data = null
            });
        }
    }
}

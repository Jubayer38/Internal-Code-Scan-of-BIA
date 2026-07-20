using BIA.Entity.CommonEntity;
using BIA.Entity.RequestEntity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using System.Text;

namespace BIA.Helper
{
    public class SIMReplacementAuthModelValidatorAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var errors = new List<string>();
            CorporateMSISDNCheckWithOTPRequest? model = null;

            // Safe content type check
            var isForm = request.ContentType?.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true;

            if (isForm)
            {
                model = context.ActionArguments
                              .Values
                              .FirstOrDefault(v => v is CorporateMSISDNCheckWithOTPRequest) as CorporateMSISDNCheckWithOTPRequest;
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

                    model = JsonSerializer.Deserialize<CorporateMSISDNCheckWithOTPRequest>(bodyStr, new JsonSerializerOptions
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

        private void ValidateModel(CorporateMSISDNCheckWithOTPRequest model, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(model.otp))
                errors.Add("Technical error!!! DBSS OTP is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.poc_msisdn_number))
                errors.Add("Technical error!!! POC MSISDN is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.purpose_number))
                errors.Add("Technical error!!! Purpose number is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.sim_number))
                errors.Add("Technical error!!! SIM number is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.mobile_number))
                errors.Add("Technical error!!! Mobile NUmber is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.purpose_number))
                errors.Add("Technical error!!! Purpose is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.retailer_id))
                errors.Add("Technical error!!! Retailer Id is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.channel_name))
                errors.Add("Technical error!!! Channel Name is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.session_token))
                errors.Add("Technical error!!! Session token is required, please resubmit the request.");

        }
    }
}

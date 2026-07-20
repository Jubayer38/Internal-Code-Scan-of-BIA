using BIA.Entity.CommonEntity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text;
using System.Text.Json;

namespace BIA.Helper
{
    public class CorpSIMReplacementOrderRequestValidatorAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var errors = new List<string>();
            CorpSimReplacementRequestModel? model = null;

            // Safe content type check
            var isForm = request.ContentType?.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true;

            if (isForm)
            {
                model = context.ActionArguments
                              .Values
                              .FirstOrDefault(v => v is CorpSimReplacementRequestModel) as CorpSimReplacementRequestModel;
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

                    model = JsonSerializer.Deserialize<CorpSimReplacementRequestModel>(bodyStr, new JsonSerializerOptions
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

        private void ValidateModel(CorpSimReplacementRequestModel model, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(model.purpose_number))
                errors.Add("Technical error!!! Purpose number is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.msisdn))
                errors.Add("Technical error!!! MSISDN is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.sim_number))
                errors.Add("Technical error!!! SIM number is required, please resubmit the request.");

            var hasAnyFingerprint =
                !string.IsNullOrWhiteSpace(model.dest_left_thumb) &&
                !string.IsNullOrWhiteSpace(model.dest_left_index) &&
                !string.IsNullOrWhiteSpace(model.dest_right_thumb) &&
                !string.IsNullOrWhiteSpace(model.dest_right_index);

            if (!hasAnyFingerprint)
                errors.Add("Technical error!!! Fingerprint is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.retailer_id))
                errors.Add("Technical error!!! Retailer ID is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.channel_name))
                errors.Add("Technical error!!! Channel name is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.session_token))
                errors.Add("Technical error!!! Session token is required, please resubmit the request.");
        }

    }
}

using BIA.Entity.RequestEntity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using BIA.Entity.CommonEntity;
using System.Text.Json;
using System.Text;

namespace BIA.Helper
{
    public class HomeWifiNewConnectionRequestValidator : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var errors = new List<string>();
            HomeWifiNewConnectionRequestModel? model = null;

            // Safe content type check
            var isForm = request.ContentType?.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true;

            if (isForm)
            {
                model = context.ActionArguments
                              .Values
                              .FirstOrDefault(v => v is HomeWifiNewConnectionRequestModel) as HomeWifiNewConnectionRequestModel;
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

                    model = JsonSerializer.Deserialize<HomeWifiNewConnectionRequestModel>(bodyStr, new JsonSerializerOptions
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


        private void ValidateModel(HomeWifiNewConnectionRequestModel model, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(model.purpose_number))
                errors.Add("Technical error!!! Purpose number is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.msisdn))
                errors.Add("Technical error!!! MSISDN is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.sim_number))
                errors.Add("Technical error!!! SIM number is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.subscription_code))
                errors.Add("Technical error!!! Subscription code is required, please resubmit the request.");
            if (model.is_paired == 0)
            {
                if (string.IsNullOrWhiteSpace(model.package_code))
                    errors.Add("Technical error!!! Package code is required, please resubmit the request.");
            }

            if (string.IsNullOrWhiteSpace(model.dest_nid))
                errors.Add("Technical error!!! NID is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.dest_dob))
                errors.Add("Technical error!!! DOB must be a valid date, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.customer_name))
                errors.Add("Technical error!!! Customer name is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.gender))
                errors.Add("Technical error!!! Gender is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.village))
                errors.Add("Technical error!!! Village is required, please resubmit the request.");

            var hasAnyFingerprint =
                !string.IsNullOrWhiteSpace(model.dest_left_thumb) &&
                !string.IsNullOrWhiteSpace(model.dest_left_index) &&
                !string.IsNullOrWhiteSpace(model.dest_right_thumb) &&
                !string.IsNullOrWhiteSpace(model.dest_right_index);

            if (!hasAnyFingerprint)
                errors.Add("Technical error!!! Fingerprint is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.retailer_id))
                errors.Add("Technical error!!! Retailer ID is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.division_name))
                errors.Add("Technical error!!! Division name is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.district_name))
                errors.Add("Technical error!!! District name is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.thana_name))
                errors.Add("Technical error!!! Thana name is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.channel_name))
                errors.Add("Technical error!!! Channel name is required, please resubmit the request.");

            if (string.IsNullOrWhiteSpace(model.order_number))
                errors.Add("Technical error!!! Order number is required, please resubmit the request.");
            if (string.IsNullOrWhiteSpace(model.initiator_channel))
                errors.Add("Technical error!!! Initiator channel is required, please resubmit the request.");
             if (string.IsNullOrWhiteSpace(model.order_type))
                errors.Add("Technical error!!! Order type is required, please resubmit the request.");
             if (string.IsNullOrWhiteSpace(model.subscription_type))
                errors.Add("Technical error!!! Subscription type is required, please resubmit the request.");
             if (string.IsNullOrWhiteSpace(model.simkit_type))
                errors.Add("Technical error!!! SIM kit type is required, please resubmit the request.");
        }
    }
}

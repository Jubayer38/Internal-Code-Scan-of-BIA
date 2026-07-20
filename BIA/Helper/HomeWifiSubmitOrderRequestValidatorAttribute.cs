using BIA.Entity.RequestEntity;
using BIA.Entity.ResponseEntity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System.Text;
using System.Text.Json;

namespace BIA.Helper
{
    public class HomeWifiSubmitOrderRequestValidatorAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            HomeWifiDEPOrderRequestModel? model =
                context.ActionArguments.Values
                    .FirstOrDefault(v => v is HomeWifiDEPOrderRequestModel)
                    as HomeWifiDEPOrderRequestModel;

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

        private async Task<HomeWifiDEPOrderRequestModel?> ReadModelFromRequestBody(
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

                return JsonSerializer.Deserialize<HomeWifiDEPOrderRequestModel>(
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

        private string? ValidateModel(HomeWifiDEPOrderRequestModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.order_number))
                    return "order_number is required.";

                if (string.IsNullOrWhiteSpace(model.retailer_code))
                    return "retailer_code is required.";

                // ------------------------------------------------
                // CANCEL FLOW VALIDATION
                // Same logic as previous controller validation.
                // ------------------------------------------------
                if (model.is_canceled == 1)
                {
                    if (string.IsNullOrWhiteSpace(model.cancelation_reason))
                        return "cancelation_reason is required when is_canceled is 1.";

                    if (model.cancelation_reason.ToUpper() == "OTHERS"
                        && string.IsNullOrWhiteSpace(model.remarks))
                    {
                        return "remarks is required when cancelation_reason is OTHERS.";
                    }

                    return null;
                }

                // ------------------------------------------------
                // NORMAL FLOW VALIDATION
                // Based on previous ValidateHomeWifiSubmitRequest method.
                // ------------------------------------------------
                if (string.IsNullOrWhiteSpace(model.mobile))
                    return "mobile is required.";

                //if (string.IsNullOrWhiteSpace(model.alternate_mobile))
                //    return "alternate_mobile is required.";

                if (string.IsNullOrWhiteSpace(model.customer_name))
                    return "customer_name is required.";

                //if (string.IsNullOrWhiteSpace(model.email))
                //    return "email is required.";

                //// Renamed from plan_name to offer_name
                //if (string.IsNullOrWhiteSpace(model.offer_name))
                //    return "offer_name is required.";

                //// Newly added from LeadList / LeadDetails response
                //if (string.IsNullOrWhiteSpace(model.offer_code))
                //    return "offer_code is required.";

                //if (string.IsNullOrWhiteSpace(model.device_name))
                //    return "device_name is required.";

                //// Newly added from LeadList / LeadDetails response
                //if (string.IsNullOrWhiteSpace(model.device_identifier))
                //    return "device_identifier is required.";

                //if (string.IsNullOrWhiteSpace(model.device_color))
                //    return "device_color is required.";

                //if (string.IsNullOrWhiteSpace(model.device_brand))
                //    return "device_brand is required.";

                //if (string.IsNullOrWhiteSpace(model.device_model))
                //    return "device_model is required.";

                if (string.IsNullOrWhiteSpace(model.delivery_address))
                    return "delivery_address is required.";

                //if (string.IsNullOrWhiteSpace(model.district))
                //    return "district is required.";

                //if (string.IsNullOrWhiteSpace(model.area))
                //    return "area is required.";

                if (string.IsNullOrWhiteSpace(model.payment_type))
                    return "payment_type is required.";

                if (model.total_amount == null)
                    return "total_amount is required.";

                //if (model.total_amount <= 0)
                //    return "total_amount must be greater than 0.";

                if (string.IsNullOrWhiteSpace(model.payment_status))
                    return "payment_status is required.";

                //if (string.IsNullOrWhiteSpace(model.order_date))
                //    return "order_date is required.";

                //if (!DateTime.TryParse(model.order_date, out _))
                //    return "Invalid order_date format.";

                //if (string.IsNullOrWhiteSpace(model.order_assigned_at))
                //    return "order_assigned_at is required.";

                //if (!DateTime.TryParse(model.order_assigned_at, out _))
                //    return "Invalid order_assigned_at format.";

                //if (string.IsNullOrWhiteSpace(model.appointment_date))
                //    return "appointment_date is required.";

                //if (!DateTime.TryParse(model.appointment_date, out _))
                //    return "Invalid appointment_date format.";

                //if (string.IsNullOrWhiteSpace(model.nw_assess_id))
                //    return "nw_assess_id is required.";

                //if (model.nw_assess_status == null)
                //    return "nw_assess_status is required.";

                if (string.IsNullOrWhiteSpace(model.order_type))
                    return "order_type is required.";

                if (string.IsNullOrWhiteSpace(model.order_status))
                    return "order_status is required.";

                if (string.IsNullOrWhiteSpace(model.initiator_channel))
                    return "initiator_channel is required.";

                //if (string.IsNullOrWhiteSpace(model.subscription_type))
                //    return "subscription_type is required.";

                if (string.IsNullOrWhiteSpace(model.simkit_type))
                    return "simkit_type is required.";

                // Renamed from wifi_router_msisdn
                if (string.IsNullOrWhiteSpace(model.ordered_msisdn))
                    return "ordered_msisdn is required.";

                if (!string.IsNullOrWhiteSpace(model.email))
                {
                    try
                    {
                        var mailAddress = new System.Net.Mail.MailAddress(model.email);

                        if (!string.Equals(
                                mailAddress.Address,
                                model.email,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return "Invalid email format.";
                        }
                    }
                    catch
                    {
                        return "Invalid email format.";
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "HomeWifiSubmitOrderRequestValidator ValidateModel Exception");
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
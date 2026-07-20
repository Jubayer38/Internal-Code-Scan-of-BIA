using Microsoft.AspNetCore.Http;

namespace BIA.Entity.RequestEntity
{
    public class HomeWifiLeadListRequestModel
    {
        public string retailer_code { get; set; } = string.Empty;
    }

    public class HomeWifiLeadDetailsRequestModel
    {
        public string retailer_code { get; set; } = string.Empty;
        public string order_number { get; set; } = string.Empty;
    }

    public class HomeWifiDEPOrderRequestModel
    {
        public string order_number { get; set; } = string.Empty;

        public string? mobile { get; set; }
        public string? alternate_mobile { get; set; }
        public string? customer_name { get; set; }
        public string? email { get; set; }

        public string? offer_name { get; set; }
        public string? offer_code { get; set; }

        public List<HomeWifiDeviceItemModel>? devices { get; set; }

        public string? delivery_address { get; set; }
        public string? district { get; set; }
        public string? area { get; set; }

        public string? payment_type { get; set; }
        public decimal? total_amount { get; set; }
        public string? payment_status { get; set; }

        public string? order_date { get; set; }
        public string? order_assigned_at { get; set; }
        public string? appointment_date { get; set; }

        public string? nw_assess_id { get; set; }
        public string? nw_assess_status { get; set; }

        public string? order_type { get; set; }
        public string? order_status { get; set; }

        public string? initiator_channel { get; set; }
        public string? subscription_type { get; set; }
        public string? simkit_type { get; set; }

        public string? remarks { get; set; }

        public string retailer_code { get; set; } = string.Empty;

        public string? cancelation_reason { get; set; }

        public int? is_activation_done { get; set; }
        public int? is_canceled { get; set; }
        public int? is_payslip_uploaded { get; set; }
        public int? is_imei_updated { get; set; }

        public string? ordered_msisdn { get; set; }

        // Used only for targeted device identifier update.
        // Do not use device_imei anymore.
        public string? old_identifier { get; set; }
        public string? new_identifier { get; set; }
        public string? imei_device_name { get; set; }
        public int? is_payment_method_changed { get; set; }
    }

    public class HomeWifiCancelOrderRequestModel
    {
        public string? retailer_code { get; set; }
        public string? order_number { get; set; }
        public string? cancelation_reason { get; set; }
    }

    public class HomeWifiNetworkAssessmentRequestModel
    {
        public string? order_number { get; set; }
        public string? retailer_code { get; set; }
        public string? nw_assess_id { get; set; }

        // Supports 1, 0, true, false, pass, fail, success, failed, timeout, etc.
        public object? nw_assess_status { get; set; }
        public string? order_type { get; set; }
    }

    public class HomeWifiPayslipUploadRequestModel
    {
        public string? order_number { get; set; }
        public string? retailer_code { get; set; }
        public IFormFile? payslip_image { get; set; }
    }

    public class HomeWifiPaymentMethodChangeRequestModel
    {
        public string? order_number { get; set; }
        public string? retailer_code { get; set; }
        public string? payment_type { get; set; }
    }

    public class HomeWifiIMEIUpdateRequestModel
    {
        public string? order_number { get; set; }
        public string? retailer_code { get; set; }

        // Current device identifier from APP/device list
        public string? old_identifier { get; set; }

        // New IMEI / new identifier input from APP
        public string? new_identifier { get; set; }

        // Used to identify the correct device when multiple devices exist
        public string? device_name { get; set; }

        public string? ordered_msisdn { get; set; }
    }

    public class HomeWifiDeviceItemModel
    {
        public string? sku { get; set; }
        public string? identifier { get; set; }
        public string? name { get; set; }
        public string? brand { get; set; }
        public string? model { get; set; }
        public string? color { get; set; }
        public string? offer_code { get; set; }
        public string? offer_name { get; set; }
    }

    public class DpeSessionTokenModel
    {
        public string access_token { get; set; } = string.Empty;
        public string token_type { get; set; } = "Bearer";
        public DateTime expires_at { get; set; }
    }
}
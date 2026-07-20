using BIA.Entity.CommonEntity;

namespace BIA.Entity.ResponseEntity
{
    /// <summary>
    /// B2C SIM replacement reponse type.
    /// </summary>
    public class IndividualSIMReplacementMSISDNCheckResponse : SIMReplacementMSISDNCheckResponse
    {
        /// <summary>
        /// Customer's SAF status.
        /// </summary>
        public bool saf_status { get; set; }
        /// <summary>
        /// DBSS customer id.
        /// </summary>
        public string customer_id { get; set; } = string.Empty;
    }

    public class CorporateSIMReplacementCheckResponseWithCustomerId : RACommonResponse
    {
        public string customer_id { get; set; } = string.Empty;
        public long dbss_subscription_id { get; set; }
        public string old_sim_number { get; set; } = string.Empty;
        public string old_sim_type { get; set; } = string.Empty;
    }

    /// <summary>
    /// B2B SIM replacement mobile number validation type. 
    /// </summary>
    public class SIMReplacementMSISDNCheckResponse : RACommonResponse
    {
        /// <summary>
        /// Customer's (parent) NID.
        /// </summary>
        public string? doc_id_number { get; set; } = string.Empty;
        /// <summary>
        /// Customer's (parent) DOB.
        /// </summary>
        public string? dob { get; set; } = string.Empty;
        /// <summary>
        /// Customer's () old SIM number.
        /// </summary>
        public string old_sim_number { get; set; } = string.Empty;
        /// <summary>
        /// Customer's () old SIM type (i.e. Prepaid = 1, Postpaid = 2).
        /// </summary>
        public string old_sim_type { get; set; } = string.Empty;
        /// <summary>
        /// DBSS subacription type.
        /// </summary>
        public long dbss_subscription_id { get; set; }
    }

    public class SIMReplacementMSISDNCheckResponseDataRev
    { 
        public bool isError { get; set; }
        public string message { get; set; } = string.Empty;

        public SIMReplacementMSISDNCheckResponseRev data { get; set; } = new SIMReplacementMSISDNCheckResponseRev();
    }
    public class SIMReplacementMSISDNCheckResponseRev
    {

        /// <summary>
        /// Customer's (parent) NID.
        /// </summary>
        public string doc_id_number { get; set; } = string.Empty;
        /// <summary>
        /// Customer's (parent) DOB.
        /// </summary>
        public string dob { get; set; } = string.Empty;
        /// <summary>
        /// Customer's () old SIM number.
        /// </summary>
        public string old_sim_number { get; set; } = string.Empty;
        /// <summary>
        /// Customer's () old SIM type (i.e. Prepaid = 1, Postpaid = 2).
        /// </summary>
        public string old_sim_type { get; set; } = string.Empty;
        /// <summary>
        /// DBSS subacription type.
        /// </summary>
        public long dbss_subscription_id { get; set; }
    }

    public class BioCalcelMSISDNCheckParseResponse
    {
        public string nid { get; set; } = string.Empty;
        public string dob { get; set; } = string.Empty;
    }

    public class MSISDNCheckResponse : RACommonResponse
    {

        /// <summary>
        /// Customer's (parent) NID.
        /// </summary>
        public string nid { get; set; } = string.Empty;
        /// <summary>
        /// Customer's (parent) DOB.
        /// </summary>
        public string dob { get; set; } = string.Empty;
        /// <summary>
        /// DBSS subacription type.
        /// </summary>
        public long dbss_subscription_id { get; set; }
        /// <summary>
        /// Customer's SAF status.
        /// </summary>
        public bool saf_status { get; set; }
        /// <summary>
        /// DBSS customer id.
        /// </summary>
        public string customer_id { get; set; } = string.Empty;
        /// <summary>
        /// dedicated_Ac_Id. 
        /// </summary>
        public string dedicated_Ac_Id { get; set; } = string.Empty;
        /// <summary> 
        /// DBSS customer id.
        /// </summary>
        public decimal amount { get; set; }
    }

    public class MSISDNCheckResponseV2 : RACommonResponse
    {

        /// <summary>
        /// Customer's (parent) NID.
        /// </summary>
        public string nid { get; set; } = string.Empty;
        /// <summary>
        /// Customer's (parent) DOB.
        /// </summary>
        public string dob { get; set; } = string.Empty;
        /// <summary>
        /// DBSS subacription type.
        /// </summary>
        public long dbss_subscription_id { get; set; }
        /// <summary>
        /// Customer's SAF status.
        /// </summary>
        public bool saf_status { get; set; }
        /// <summary>
        /// DBSS customer id.
        /// </summary>
        public string customer_id { get; set; } = string.Empty;

    }

    public class MSISDNCheckResponseRevamp
    {
        public bool isError { get; set; }
        /// <summary>
        /// Data contains api request result's message (i.e. "Success", "Security token invalid!")
        /// </summary>
        public string message { get; set; } = string.Empty;

        public MSISDNCheckResponseV2 data { get; set; } = new MSISDNCheckResponseV2();


    }

    public class IndividualSIMReplacementMSISDNCheckResponseRevamp
    {
        public bool isError { get; set; }
        /// <summary>
        /// Data contains api request result's message (i.e. "Success", "Security token invalid!")
        /// </summary>
        public string message { get; set; } = string.Empty;

        public IndividualSIMReplacementMSISDNCheckResponse data { get; set; } = new IndividualSIMReplacementMSISDNCheckResponse();


    }

    public class SIMReplacementMSISDNCheckResponseRevamp
    {
        public bool isError { get; set; }
        /// <summary>
        /// Data contains api request result's message (i.e. "Success", "Security token invalid!")
        /// </summary>
        public string message { get; set; } = string.Empty;

        public SIMReplacementMSISDNCheckResponse data { get; set; } = new SIMReplacementMSISDNCheckResponse();
    }
}

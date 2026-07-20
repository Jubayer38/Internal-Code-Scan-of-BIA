namespace BIA.Entity.ResponseEntity
{
    public class LoginUserInfoResponse
    {
        public string user_id { get; set; } = string.Empty;
        public string user_name { get; set; } = string.Empty;
        public string role_id { get; set; } = string.Empty;     
        public string role_name { get; set; } = string.Empty;
        public int? is_role_active { get; set; }
        public int? channel_id { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int? is_activedirectory_user { get; set; }
        public string role_access { get; set; } = string.Empty;
        public string distributor_code { get; set; } = string.Empty;
        public int inventory_id { get; set; }
        public string center_code { get; set; } = string.Empty;
        public string itopUpNumber { get; set; } = string.Empty;
        public int is_default_Password { get; set; }
        public string ExpiredDate { get; set; } = string.Empty;
        
    }

    public class LoginUserInfoResponseRev
    {
        public string user_id { get; set; } = string.Empty;
        public string user_name { get; set; } = string.Empty;
        public string role_id { get; set; } = string.Empty;
        public string role_name { get; set; } = string.Empty;
        public int is_role_active { get; set; }
        public int channel_id { get; set; }
        public string channel_name { get; set; } = string.Empty;
        public int is_activedirectory_user { get; set; }
        public string role_access { get; set; } = string.Empty;
        public string distributor_code { get; set; } = string.Empty;
        public int inventory_id { get; set; }
        public string center_code { get; set; } = string.Empty;
        public string itopUpNumber { get; set; } = string.Empty;
        public int is_default_Password { get; set; }
        public string ExpiredDate { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string designation { get; set; } = string.Empty; 
        public int isValidUser { get; set; }
        public int FWA_channel_id { get; set; }
        public string FWA_channel_name { get; set; } = string.Empty;

    }

}

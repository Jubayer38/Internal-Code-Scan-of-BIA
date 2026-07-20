using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.ResponseEntity
{
    public class MSISDNReservationResponse
    {
        public bool IsReserve { get; set; }
        public string Reservation_Id { get; set; } = string.Empty;
        public string MSISDN { get; set; } = string.Empty;      
        public string Error_message { get; set; } = string.Empty;

    }
}

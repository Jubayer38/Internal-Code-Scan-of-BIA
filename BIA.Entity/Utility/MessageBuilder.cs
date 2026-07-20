using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIA.Entity.Utility
{
    public class MessageBuilder
    { 
        public string GetInnerMessage(Exception ex)
        {
            string retString = "";
            try
            {
                if (ex.InnerException != null)
                {
                    retString = ex.InnerException.Message;
                }
                else
                {
                    retString += ex.Message;
                }
            }
            catch
            { }

            return retString;
        }
    }
}

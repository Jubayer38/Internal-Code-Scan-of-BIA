using Serilog.Core;
using Serilog.Events;
using System.ComponentModel.DataAnnotations;

namespace BIA.Middleware
{    
    public static class LogFilter
    {
        public static bool ShouldIgnoreApiLog(LogEvent e)
        {
            var message = e.RenderMessage();

            return message.ToLower().Contains(
                "secure and reliable biometric app management system",
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool ShouldIgnoreException(LogEvent e)
        {
            var ex = e.Exception;

            if (ex == null)
                return false;

            var msg = ex.Message?.ToLowerInvariant() ?? string.Empty;

            return msg.Contains("the session token is expired")
                || msg.Contains("msisdn not found")
                || msg.Contains("this field may not be blank")
                || msg.Contains("cannot insert null into");
        }
    }
}

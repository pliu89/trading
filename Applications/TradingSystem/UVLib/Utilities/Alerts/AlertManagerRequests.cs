using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Utilities.Alerts
{
    
    public class AlertManagerRequest : EventArgs
    {
        public Request Type;
        public AlertLevel Level = AlertLevel.Low;
        public string AlertString = string.Empty; 

        public enum Request
        {
            Start,
            Stop,
            SendEmail,
            SendSMS,
        }
    }
    
    public enum AlertLevel
    {
        Low,
        High
    }
}

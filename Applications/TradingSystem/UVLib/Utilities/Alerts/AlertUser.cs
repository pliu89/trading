using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Utilities.Alerts
{
    using UV.Lib.IO.Xml;

    public class AlertUser : IStringifiable
    {


        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //    
        public string UserEmail = string.Empty;                      // contact info for this alert user
        public AlertLevel UserAlertLevel = AlertLevel.Low;           // level of events user would like to recieve alerts for.  (and higher level from here)


        #endregion// members
        
        
        #region IStringifiable
        // *************************************************************
        // ****                     IStringifiable                  ****
        // *************************************************************
        string IStringifiable.GetAttributes()
        {
            return string.Empty;
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            AlertLevel alertLevel;
            foreach (KeyValuePair<string, string> keyVal in attributes)
            {
                if (keyVal.Key.ToUpper().Equals("EMAIL"))
                    UserEmail = keyVal.Value;
                else if (keyVal.Key.ToUpper().Equals("ALERTLEVEL") && Enum.TryParse<AlertLevel>(keyVal.Value, out alertLevel))
                    UserAlertLevel = alertLevel;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
        }
        #endregion//IStringifiable
    }
}

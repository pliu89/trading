using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Data
{
    public class DataHubRequest : EventArgs
    {
        // Members
        public RequestCode Request = RequestCode.None;  // request
        public List<object> Data = new List<object>();  // optional place to add bars or Data
        // 
        // ***      Constructor      ****
        public DataHubRequest()
        {
        }
        //
        // ***      Clear()     ****
        //
        /// <summary>
        /// Convenient way to clear object after recycling.
        /// </summary>
        public void Clear()
        {
            this.Request = RequestCode.None;
            this.Data.Clear();
        }
        //
        //
        // ***      ToString()     ****
        //
        public override string ToString() { return string.Format("Request: {0}", this.Request.ToString()); }

    }//end class

    #region Request Code
    //
    // Request code enum
    //
    public enum RequestCode
    {
        None = 0
        ,Connect = 1                       
        ,Shutdown = 2
        ,ServiceStateChange = 3            // Request a service state change
        ,RequestProductsToRecord = 4
    }
    //
    #endregion    

}

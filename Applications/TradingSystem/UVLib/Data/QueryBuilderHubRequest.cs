using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Data
{
    public class QueryBuilderHubRequest : EventArgs
    {
        // Members
        public QueryBuilderHubRequestCode Request = QueryBuilderHubRequestCode.None;  // request
        public List<object> Data = new List<object>();  // optional place to add bars or Data
        // 
        // ***      Constructor      ****
        public QueryBuilderHubRequest()
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
            this.Request = QueryBuilderHubRequestCode.None;
            this.Data.Clear();
        }
        //
        //
        // ***      ToString()     ****
        //
        public override string ToString() { return string.Format("Request: {0}", this.Request.ToString()); }

    }//end class

    public enum QueryBuilderHubRequestCode
    {
        None = 0,
        Stop = 1,
        RequestCheckDBInstrDetails = 2
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Strategies.StrategyHubs
{

    #region Strategy Hub Request
    // *****************************************************************
    // ****         Strategy Hub Request Event Args                 ****
    // *****************************************************************
    // 
    public class StrategyHubRequest : EventArgs
    {
        // Members
        public RequestCode Request = RequestCode.None;  // request
        //public ServiceState StateRequested = ServiceState.Unstarted;
        //public int ID = -1;
        public List<object> Data = new List<object>();  // optional place to add some data.

        // 
        // ***      Constructor      ****
        public StrategyHubRequest()
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
    #endregion//Event Handlers


    #region Request Code
    //
    // Request code enum
    //
    public enum RequestCode
    {
        None
        //,Connect                       
        ,Shutdown
        ,LoadStrategies                 // Load strategies
        ,ServiceStateChange             // Request a service state change
        //RequestState,                 // outside requests a state change
        ,StrategyReprice,               // reprice a strategy (as if a market instrument changed), user for parameter changes.
        //ForceStrategyReprice,       // outside request to reprice a strategy (as if a market instrument changed)
        //RequestEngineParameterSave, // request for all engines in all strategies to save their current values.
        //RequestEngineParameterLoad, // request for all engines parameters to be loaded from file.
        //SaveStrategyPosition,		// all strategies are asked to save their current position. 
        //
        //TimeEvent,					// a timer has triggered.
        //
        //

    }
    //
    //
    #endregion//Event Handlers
    

    #region Strategy Hub Stage
    // *****************************************************************
    // ****                 Strategy Hub Stage                      ****
    // *****************************************************************
    //
    #endregion//Stage


}
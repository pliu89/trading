using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies
{

    #region Request Code
    //
    // Request code enum
    //
    public enum RequestCode
    {
        None,
        Shutdown,
        LoadStrategies,                // Load strategies
        ServiceStateChange,            // Request a service state change
        StrategyReprice,               // reprice a strategy (as if a market instrument changed), user for parameter changes.
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
}

using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Application
{
    /// <summary>
    /// This describes the various modes in which the application can be run.
    /// </summary>
    public enum RunType
    {
        Debug = 0, 
        Sim, 
        Backtest, 
        Prod,
        Faux            // used for production simulated fills
    }
}

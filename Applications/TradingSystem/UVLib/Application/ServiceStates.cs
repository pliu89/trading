using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Application
{
    public enum ServiceStates 
    {
        // Start up
        None = 0,                   // Not allowed state for a service, but useful for usage for "pending states" etc.
        Unstarted = 1,              // internal thread unstarted.
        Started = 2,                    // internal thread is started.
        // Initialization
        Running = 3,


        // Paused = 100
        // Trading = 101

        // Shutdown
        Stopping = 1000,
        Stopped = 1001,
    }

    #region How to use Enum of Flags
    //
    // 
    //
    // States of a service.
    //[Flags]
    //public enum ServiceStates : int
    //{
    //    None = 0
    //    ,Connected          = 1 << 0    // Service is connected to outside resources needed for its job. 
    //                                    // For example, consider the Market service connected to an exchange API. 
    //                                    // Note: Since these are flags, being disconnected is just NOT Connected.
    //}
    // *********************************
    // ****         Usage           ****
    // *********************************
    // Usage:
    // 1)   To set a flag:  
    //      state |= ServiceState.Connected;            
    //
    // 2)   To unset a flag:
    //      state &= ~ServiceState.Connected;           
    //
    // Test: 
    //      if ( state.HasFlag( ServiceState.Connected ) )
    //          etc...
    #endregion//Flag explanation

}

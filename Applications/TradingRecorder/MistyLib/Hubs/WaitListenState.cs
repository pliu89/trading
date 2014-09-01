using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace Misty.Lib.Hubs
{

    // **************************************************
    // ****                 Enums                    ****
    // **************************************************
    //
    //
    public enum WaitListenState
    {
        ReadyToStart,   // constructed not yet started to listen - this is the "stopped" state too.
        Waiting,        // currently listening - running.
        Working,		// thread is currently busy executing code.		
        Stopping        // was listening, user state requesting a stop.
    }



}

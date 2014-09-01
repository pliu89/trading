using System;

namespace UV.Strategies.ExecutionHubs
{
    public enum RequestCode
    {
        // Hub control requests
        Nothing = 0,
        ServiceStateChange,     // Request start, stop etc.  Date[0]=ServiceState desired.
        //
        

    }
}

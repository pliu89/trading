using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Application.Managers
{
    /// <summary>
    /// Type of message object
    /// </summary>
    public enum MessageType
    {
        None = 0 
        , Credentials           // Message contains connection credentials
        , CreateServices        // Creation of services
        , ServiceAdded
        , ServiceRemoved
        , EngineEvent           // data contains engine event args.
    

    }

    public enum MessageState
    {
        None = 0, 
        Request = 1,
        Confirmed = 2,
        Failed = 3
    }



}

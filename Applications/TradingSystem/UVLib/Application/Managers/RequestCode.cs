using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace UV.Lib.Application.Managers
{
    /// <summary>
    /// Request codes for the Service Managers
    /// </summary>
    public enum RequestCode
    {
        None,
        //
        // Local requests
        //
        ServiceStateChange,            // Request a service state change
        //
        // Foreign service requests
        //
        ForeignServiceConnect           // attempt to connect


    }//end enum
}

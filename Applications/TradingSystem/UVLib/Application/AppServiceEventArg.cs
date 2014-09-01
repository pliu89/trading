using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Application
{
    /// <summary>
    /// </summary>
    public class AppServiceEventArg : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        public string ServiceName = string.Empty;
        public AppServiceEventType EventType = AppServiceEventType.ServiceAdded;


        #endregion// members




    }//end class



    #region Enum
    // *****************************************************************
    // ****                       Enum                             ****
    // *****************************************************************
    //
    public enum AppServiceEventType
    {
        ServiceAdded
        ,ServiceRemoved
    }

    #endregion//Enum

}

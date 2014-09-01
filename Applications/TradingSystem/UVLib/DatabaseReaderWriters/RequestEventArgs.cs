using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters
{
    public class RequestEventArgs : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        public RequestCode Request = RequestCode.None;


        #endregion// members



        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public RequestEventArgs(RequestCode requestCode)
        {
            this.Request = requestCode;
        }
        //
        //       
        #endregion//Constructors




       
    }


    public enum RequestCode
    {
        None = 0,
        BeginShutdown
    }


}

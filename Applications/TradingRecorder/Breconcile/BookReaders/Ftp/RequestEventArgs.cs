using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Breconcile.Ftp
{
    public class RequestEventArgs : EventArgs
    {
        //
        // Members
        //
        public RequestType Type = RequestType.None;
        public RequestStatus Status = RequestStatus.New;
        public DateTime GiveUpTime = DateTime.MinValue;             // (optional) keep trying to process request until this time, then we give up.

        // Returned data
        public List<object> Data = null;                            // data returned on success to user


        //
        // Constructors
        //
        public RequestEventArgs(RequestType type)
        {
            this.Type = type;
            
        }


        //
        //
        //




    }
}

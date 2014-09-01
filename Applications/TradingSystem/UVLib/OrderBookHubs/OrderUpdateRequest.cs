using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.OrderBookHubs
{
    public enum OrderRequestType
    {
        None,

        AddConfirm,
        AddRequest,
        AddReject,

        DeleteConfirm,
        DeleteRequest,
        DeleteReject,
        
        ChangeConfirm,
        ChangeRequest,
        ChangeReject,
         
        FillConfirm,
        
        Unknown,
        

    }
}

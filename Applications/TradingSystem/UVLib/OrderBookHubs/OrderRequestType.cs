using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.OrderBookHubs
{
    public enum OrderRequestType
    {
        None = 0,
        ForAdd = 1,
        ForDelete = 2,
        ForFill = 3,
        UpdateForReject = 4

    }
}

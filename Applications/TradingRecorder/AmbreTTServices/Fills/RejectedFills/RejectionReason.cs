using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Fills.RejectedFills
{
    public enum RejectionReason
    {
        Unknown = 0
        ,DuplicateKey                           // trade arrived previously
        ,ExcessiveLateness                      // trade came in way later than similar trades..
        //
        // 
        //
        ,ResubmissionRequestedByUser                       // user wants to resubmit this fill.

    }
}

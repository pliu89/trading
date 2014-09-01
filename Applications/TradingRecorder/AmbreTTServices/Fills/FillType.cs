using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Fills
{
    public enum FillType
    {

        New                        // a new live fill
        ,InitialPosition           // denotes our initial fill (to reconstitute a starting position)
        ,Historic                  // replay from today's trading session
        ,Adjustment                //  
        ,UserAdjustment            // adjustment made by trade in this application
        //
        //

    }
}

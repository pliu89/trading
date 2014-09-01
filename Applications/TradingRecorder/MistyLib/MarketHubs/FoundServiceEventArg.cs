using System;
using System.Collections.Generic;
using System.Text;

namespace Misty.Lib.MarketHubs
{
    public class FoundServiceEventArg : EventArgs
    {
        //
        // ****                 Members                     ****
        //
        public List<Products.Product> FoundProducts;
        public List<Products.InstrumentName> FoundInstruments;

    }//end class
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.OrderBookHubs
{

    using UV.Lib.Products;

    public class FillEventArgs : EventArgs
    {
        //
        // Members
        //
        public string FillKey = string.Empty;                           // foreign generated unique code ID for this fill event.
        public string AccountID = string.Empty;
        public Fill Fill;                                               // the fill
        public int OrderID;
        public int OrderBookID;
        public InstrumentName InstrumentName;           // instrument name associfate with the fill
        public bool isComplete = false;
        //
        // Constructors
        // 
        public FillEventArgs() { }
        public FillEventArgs(Fill theFill, int orderId, InstrumentName instrName)
        {
            this.Fill = theFill;
            this.OrderID = orderId;
            this.InstrumentName = instrName;
        }
        //
        //
        public FillEventArgs(Fill theFill, int orderId, InstrumentName instrName, bool isCompletelyFilled)
        {
            this.Fill = theFill;
            this.OrderID = orderId;
            this.InstrumentName = instrName;
            this.isComplete = isCompletelyFilled;
        }
        //
        // Public Methods
        // 
        public override string ToString()
        {
            return Fill.ToString();
        }



    }
}

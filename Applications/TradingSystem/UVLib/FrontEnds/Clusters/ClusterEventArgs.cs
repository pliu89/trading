using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.Clusters
{
    //
    //
    //
    public class ClusterEventArgs : EventArgs
    {
        #region Members
        // *********************************************
        // ****             Members                 ****
        // *********************************************
        //
        // Description of event:
        public EventArgs OriginalEventArgs = null;
        public Keys ModifierKey = Keys.None;
        public Keys KeyPressed = Keys.None;
        
        // Indentity of the event triggering object.
        public int ClusterEngineContainerID = -1;
        public int ClusterEngineID = -1;
        public int RowID = -1;
        public int BoxID = -1;
        public BoxRowType RowType = BoxRowType.None;

        // Data associated with event.
        public int BoxPriceValue = 0;  // Price associated with this box.
        public int BoxValue = 0;       // actual value displayed in this box.


        #endregion// members



         #region Enums
        // *********************************************
        // ****             Enums                 ****
        // *********************************************
        //
        /// <summary>
        /// This enum tells the Strategy that receives this event what type of "row" 
        /// in the associated cluster was clicked on.
        /// </summary>
        public enum BoxRowType 
        {
            PriceRow,
            BidQtyRow,
            AskQtyRow,
			OrderRow,
			FillsRow,
            None
        }
        

        #endregion// Enums


    }
}

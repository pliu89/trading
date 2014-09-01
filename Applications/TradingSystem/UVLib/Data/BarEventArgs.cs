using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Data
{
    /// <summary>
    /// Thread Safety Notes:
    /// In order to better manage memory, this event will create the Bar objects 
    /// and manage these itself, reusing them when this object is reused.  The idea
    /// is that we maintain a (thread-safe) shared queue of BarEventArgs to be used 
    /// by MrData.CreateBar() and the DatabaseWriter thread.
    /// Usage:
    ///		Bar myBar = barEventArgs.GetBar();		// get a bar object
    ///		myBar.mySqlID = 200321;					// set values
    ///		myBar.bid = 20.02;						// etc....
    ///		barEventArgs.EnqueueBar(myBar);			// Give this back to the event arg
    /// </summary>
    public class BarEventArgs : EventArgs
    {
        // Members
        public int unixTime;
        public Queue<Bar> BarList = new Queue<Bar>();

    }//BarEventArgs class
}

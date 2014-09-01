using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.BookHubs
{

    /// <summary>
    /// This event arg is launched from the BookHub whenever one of its book changes.
    /// It is this event that a typical trading strategy will received from the MarketHub.
    /// </summary>
    public class InstrumentChangeArgs : EventArgs
    {
        #region Members
        // *****************************************************
        // ****                 Members                     ****
        // *****************************************************
        //
        public object Sender = null;                                        // Object that last updated this event.        
        public Dictionary<int, InstrumentChange> ChangedInstruments = null; // instrument IDs, and description changes made to each instrument.
        
        
        #endregion//members


        #region Constructors
        // *****************************************************
        // ****             Constructors                    ****
        // *****************************************************
        //
        /// <summary>
        /// This constructor allows the caller to create an empty InstrumentChangeArgs and
        /// call AppendChangedInstrument as well as assign sender after construction.
        /// </summary>
        public InstrumentChangeArgs()
        {
            ChangedInstruments = new Dictionary<int, InstrumentChange>();
        }
        #endregion// Constructors


        #region Public methods 
        // *****************************************************
        // ****           Public methods                    ****
        // *****************************************************
        //
        public void AppendChangedInstrument(int instrID, List<int>[] depthsChanged)
        {
            if (!ChangedInstruments.ContainsKey(instrID))
            {
                InstrumentChange instrChange = new InstrumentChange();
                instrChange.InstrumentID = instrID;
                instrChange.MarketDepthChanged = depthsChanged;
                this.ChangedInstruments.Add(instrID, instrChange);
            }
            else
            { // we need to check the list of indexes changed to see if we need to add any
                for (int side = 0; side < MarketBase.NSides; side++)
                { // foreach side of the market
                    foreach(int levelChanged in depthsChanged[side])
                    { // foreach new level changed
                        if(!ChangedInstruments[instrID].MarketDepthChanged[side].Contains(levelChanged))
                        { // if our list doesn't have it yet add it.
                            ChangedInstruments[instrID].MarketDepthChanged[side].Add(levelChanged);
                        }
                    }
                    ChangedInstruments[instrID].MarketDepthChanged[side].Sort();       // we want to always be able to look at the top element and know that is the "best" change
                }
            }
        }
        //
        //
        public override string ToString()
        {
            StringBuilder msg = new StringBuilder("InstrChange: ");
            // for (int i = 0; i < this.ChangedInstruments.Length; i++)
            for (int i = 0; i < this.ChangedInstruments.Count; i++)
                msg.AppendFormat("{0} ", this.ChangedInstruments[i].ToString());
            //if (this.CustomEventArgs != null)
            //{
            //    msg.Append(" Custom");
            //}
            return msg.ToString();
        }// ToString().
        //
        //
        #endregion // public methods
    }

}

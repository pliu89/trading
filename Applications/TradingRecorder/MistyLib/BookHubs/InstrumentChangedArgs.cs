using System;
using System.Collections.Generic;
using System.Text;

namespace Misty.Lib.BookHubs
{

    //
    // *********************************************************
    // ****             Instrument Change Args              ****
    // *********************************************************
    public class InstrumentChangeArgs : EventArgs
    {


        #region Members
        //
        // Members
        //
        public object Sender = null;                    // Object that last updated this event.
        public List<int> ChangedInstruments = null;     // instrument IDs that have changed

        private EventArgs CustomEventArgs = null;
        private object CustomEventCreator = null;
        //
        #endregion//members


        #region Constructors
        //
        // Constructor
        //
        public InstrumentChangeArgs(object sender, List<int> changedInstr)
        {
            this.Sender = sender;           // store bookhub to identify the event origin.
            this.ChangedInstruments = changedInstr;
        }
        #endregion


        //
        //
        // ****     Properties      ****
        //
        public bool IsUserEvent
        {
            get { return (this.CustomEventArgs != null); }
        }

        #region Public Methods 
        //
        // ***      Methods         ****
        //
        //
        public void SetCustomEvent(object creator, EventArgs args)
        {
            this.CustomEventArgs = args;
            this.CustomEventCreator = creator;
        }
        public EventArgs GetCustomEvents()
        {
            return CustomEventArgs;
        }
        //
        public object GetCustomEventCreator()
        {
            return CustomEventCreator;
        }
        //
        public override string ToString()
        {
            StringBuilder msg = new StringBuilder("InstrChange: ");
            // for (int i = 0; i < this.ChangedInstruments.Length; i++)
            for (int i = 0; i < this.ChangedInstruments.Count; i++)
                msg.AppendFormat("{0} ", this.ChangedInstruments[i].ToString());
            if (this.CustomEventArgs != null)
            {
                msg.Append(" Custom");
            }
            return msg.ToString();
        }// ToString().
        //
        //
        #endregion // public methods
    }

}

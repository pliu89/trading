using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.OrderHubs
{
    using UV.Lib.Products;

    public class OrderBookEventArgs : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public EventTypes EventType;
        public InstrumentName Instrument; 
        public OrderHub ParentOrderHub;
        public Order Order = null;
        //
        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderBookEventArgs(OrderHub parentOrderHub, InstrumentName instrumentName, EventTypes orderBookEventType)
        {
            ParentOrderHub = parentOrderHub;
            Instrument = instrumentName;
            this.EventType = orderBookEventType;
        }
        //
        //       
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers



        public enum EventTypes
        {
            NewOrder,
            ChangedOrder,
            DeletedOrder,
            CreatedBook
        }









    }
}

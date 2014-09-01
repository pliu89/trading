using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Orders
{
    using Misty.Lib.OrderHubs;
    using Misty.Lib.Products;

    using TT = TradingTechnologies.TTAPI;

    public class OrderBookTT : OrderBook
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        public Dictionary<string, string> m_TagBre2TT = new Dictionary<string, string>();
        public Dictionary<string, string> m_TagTT2Bre = new Dictionary<string, string>();


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderBookTT(OrderHubTT parentOrderHub, InstrumentName instrumentName)
            : base(parentOrderHub, instrumentName)
        {

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

    }
}

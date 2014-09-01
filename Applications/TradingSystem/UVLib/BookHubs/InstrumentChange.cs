using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.BookHubs
{
    public class InstrumentChange
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Identification
        //
        public int InstrumentID = 0;

        //
        // Description of change
        //
        public List<int>[]  MarketDepthChanged = new List<int>[2];

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public InstrumentChange()
        {
            MarketDepthChanged[MarketBase.BidSide] = new List<int>();
            MarketDepthChanged[MarketBase.AskSide] = new List<int>();
        }
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

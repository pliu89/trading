﻿using System;
using System.Collections.Generic;
using System.Text;

namespace UV.TTServices.Fills.FrontEnds
{
    using InstrumentName = UV.Lib.Products.InstrumentName;
    using TradingTechnologies.TTAPI;

    public class InstrumentRowData
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // MarketHub information
        public InstrumentName InstrumentName;         
        public double Price = 0;
        public int MarketID = -1;
        public int MarketPriceDecimals = 3;
        public bool IsPriceChanged = false;

        public double CurrencyRate = 1.0;
        public DateTime ExpirationDate = DateTime.MinValue;
        public bool IsFoundInMarket = false;

        // FillHub information
        public int Position = 0;
        public double AverageCost = 0;
        public double StartingRealPnL = 0;
        public double RealPnL = 0;
        public double UnrealPnL = 0;

        #endregion// members

        #region Constructor
        // *****************************************************************
        // ****                     Constructor                         ****
        // *****************************************************************
        public InstrumentRowData(InstrumentName instrName)
        {
            this.InstrumentName = instrName;
        }
        //
        #endregion//Properties


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public double TotalPnL
        {
            get { return RealPnL + UnrealPnL; }
        }
        //
        #endregion//Properties






    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Fills
{
    using UV.Lib.Engines;

    /// <summary>
    /// </summary>
    public class SyntheticOrder : EngineEventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Internal identification
        //
        public int OrderId;        

        //
        // Trade target details
        //
        public int TradeSide;
        public int TargetQty;
        public double TargetPrice;

        //
        // Fills
        //




        #endregion// members


        #region Constructors & Creators 
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        protected SyntheticOrder() : base()
        {
            base.EngineID = -1;                         // Trades are considered "strategy-wide" not assoc with particular EngineId!
        }
        //
        // 
        // *****************************************
        // ****         RequestNewTrade         ****
        // *****************************************
        public static SyntheticOrder RequestNewTrade(string executionHubName, int strategyId, int tradeId)
        {
            SyntheticOrder e = new SyntheticOrder();
            e.EngineHubName = executionHubName;
            e.EngineContainerID = strategyId;
            // e.EngineId = -1;                 // always -1 for trade objects.
            e.OrderId = tradeId;
            e.MsgType = EngineEventArgs.EventType.SyntheticOrder;
            e.Status = EngineEventArgs.EventStatus.Request;
            return e;
        }//end Request AllControls
        //
        //
        //
        //
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public new SyntheticOrder Copy()
        {
            SyntheticOrder newArgs = new SyntheticOrder();
            this.CopyTo(newArgs);
            return newArgs;
        }
        //
        //
        //
        protected void CopyTo(SyntheticOrder newArg)
        {
            base.CopyTo(newArg);
            if ( newArg is SyntheticOrder)
            {
                SyntheticOrder newTradeArg = (SyntheticOrder) newArg;
                newTradeArg.OrderId = this.OrderId;
                newTradeArg.TradeSide = this.TradeSide;
                newTradeArg.TargetPrice = this.TargetPrice;
                newTradeArg.TargetQty = this.TargetQty;
            }
        }// CopyTo()
        //
        //        
        //
        public override string ToString()
        {
            StringBuilder msg = new StringBuilder();
            //msg.Append("EngineEvent ");
            msg.AppendFormat("SynthOrder {0}:{1} ", this.EngineHubName, this.EngineContainerID);
            msg.AppendFormat("[{0}]", this.Status.ToString());
            return msg.ToString();
        }
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

    }//end class
}

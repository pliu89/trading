using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace UV.Strategies.StrategyEngines
{
    using UV.Lib.Utilities;
    using UV.Lib.Engines;
    using UV.Lib.BookHubs;
    using UV.Lib.OrderBooks;
    using UV.Lib.Fills;
    using UV.Lib.Application;
    using UV.Lib.FrontEnds.GuiTemplates;

    using UV.Strategies.StrategyHubs;
    using UV.Strategies.ExecutionEngines.OrderEngines;

    /// <summary>
    /// 
    /// </summary>
    public class TradeEngine : Engine, IOrderEngineParameters
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // My local Engines
        //
        private StrategyHub m_StrategyHub = null;
        private Strategy m_Strategy = null;
        private int m_LastOrderId = -1;                             // TradeId is unique to each TradeEngine/Strategy

        // Execution Hub
        private string m_ExecutionHubName = "ExecutionHub";         // TODO: determine this somehow at Setup
        private IEngineHub m_ExecutionHub = null;                   // pointer to my remote IExecutionHub.
        private int m_DripQty = 0;
        private double m_QuoteTickSize = 1;
        private bool m_IsRiskCheckPassed = false;
        private bool m_IsUserTradingEnabled = false;

        //
        // Order controls
        //
        //  Note: Currently I store only one open trade on each side of
        //      market.  In future, do something more general.
        public SyntheticOrder[] m_ActiveOrders = new SyntheticOrder[2];    // each mkt side.
        private EngineEventArgs[] m_EngineEventArgsForOrders = new EngineEventArgs[2]; // each mkt side, reusable event args to send.
        private int[] m_QuoteQtyPrev = new int[2];
        private int[] m_QuoteIPricePrev = new int[2];
        private string[] m_QuoteReasonPrev = new string[2];
        private string m_DefaultAccount = null;
        private Dictionary<int, SyntheticOrder> m_FilledOrders = new Dictionary<int, SyntheticOrder>();

        #endregion// members


        #region Constructors & Engine Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public TradeEngine()
            : base()
        {

        }
        //
        //
        // *****************************************************
        // ****             SetupInitialize()               ****
        // *****************************************************
        protected override void SetupInitialize(IEngineHub engineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(engineHub, engineContainer, engineID, false);  //suppress gui creation.            
            //EngineGui engineGui = base.SetupGuiTemplates();
            //engineGui.HeaderControlFullName = string.Empty;                     // suppress popup gui.
            //engineGui.LowerHudFullName = typeof(Huds.TradeEngineHud).FullName;

            m_StrategyHub = (StrategyHub)engineHub;
            m_Strategy = (Strategy)engineContainer;

            // Get my associated execution hub.
            IService iservice;
            if (AppServices.GetInstance().TryGetService(this.m_ExecutionHubName, out iservice) && iservice is IEngineHub)
            {
                m_ExecutionHub = (IEngineHub)iservice;
            }
        }// SetupInitialize()
        //
        // 
        // *************************************************
        // ****             SetupBegin()                ****
        // *************************************************
        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {
            //EngineEventArgs e = EngineEventArgs.RequestAllParameters(m_ExecutionHubName, m_Strategy.EngineContainerID, -1);
            //SyntheticOrder synth = new SyntheticOrder(e);
            for (int i = 0; i < 2; i++)
            { // set up event args to reuse.
                m_EngineEventArgsForOrders[i] = new EngineEventArgs();
                m_EngineEventArgsForOrders[i].EngineHubName = m_ExecutionHubName;
                m_EngineEventArgsForOrders[i].EngineContainerID = m_Strategy.EngineContainerID;
                m_EngineEventArgsForOrders[i].MsgType = EngineEventArgs.EventType.SyntheticOrder;
                m_EngineEventArgsForOrders[i].Status = EngineEventArgs.EventStatus.Request;
                m_EngineEventArgsForOrders[i].DataObjectList = new List<object>();
            }

        }// SetupBegin()
        //       
        #endregion//Constructors & Engine Setup


        #region IOrderEnginePararameters
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public int DripQty
        {
            get { return m_DripQty; }
            set
            {

            }
        }
        public double QuoteTickSize
        {
            get { return m_QuoteTickSize; }
            set
            {
                m_QuoteTickSize = value;
            }
        }
        public bool IsRiskCheckPassed
        {
            get { return m_IsRiskCheckPassed; }
            set
            {
                throw new NotImplementedException();
            }
        }
        public bool IsUserTradingEnabled
        {
            get { return m_IsUserTradingEnabled; }
            set
            {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// 15 characters max. This allows us to filter based on TT!
        /// </summary>
        public string DefaultAccount
        {
            get { return m_DefaultAccount;  }
        }
        //
        //
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *****************************************
        // ****             Quote()             ****
        // *****************************************
        /// <summary>
        /// Method to request that an order be sent to the Execution hub at 
        /// a particular price and qty.
        /// </summary>
        /// <param name="orderSide"></param>
        /// <param name="price"></param>
        /// <param name="qty"></param>
        /// <param name="quoteReason"></param>
        /// <returns>TradeId</returns>
        public int Quote(int orderSide, double price, int qty, string quoteReason)
        {
            m_EngineEventArgsForOrders[orderSide].DataObjectList.Clear();  //clear before each use!

            // Locate TradeEventArg to update.
            SyntheticOrder syntheticOrder = null;
            if (m_ActiveOrders[orderSide] == null)
            {   // There is currently no active trade, so create one.
                syntheticOrder = SyntheticOrder.RequestNewOrder(++m_LastOrderId);
                syntheticOrder.Side = orderSide;
                syntheticOrder.TradeReason = quoteReason;
                m_ActiveOrders[orderSide] = syntheticOrder;           // store it as active trade
            }
            else
            {   // There is already an active trade, grab it.
                syntheticOrder = m_ActiveOrders[orderSide];
            }
            // Update quatities if they have changed.
            // TODO: implement various round-off schemes.
            int tradeSign = QTMath.MktSideToMktSign(orderSide);
            int iPrice = tradeSign * (int)System.Math.Floor(tradeSign * price / m_QuoteTickSize);    // integerized price!
            bool isChangedFromPrev = qty != m_QuoteQtyPrev[orderSide] || (iPrice != m_QuoteIPricePrev[orderSide]) || quoteReason != m_QuoteReasonPrev[orderSide];
            if (isChangedFromPrev)
            {
                m_QuoteQtyPrev[orderSide] = qty;
                m_QuoteIPricePrev[orderSide] = iPrice;
                m_QuoteReasonPrev[orderSide] = quoteReason;
                syntheticOrder.Price = iPrice * m_QuoteTickSize;
                syntheticOrder.Qty = qty;
                syntheticOrder.TradeReason = quoteReason;

                m_EngineEventArgsForOrders[orderSide].DataObjectList.Add(syntheticOrder);       // add to event arg that is already set up.
                m_ExecutionHub.HubEventEnqueue(m_EngineEventArgsForOrders[orderSide].Copy());   // send off to remote exeuction
                m_StrategyHub.Log.NewEntry(Lib.Hubs.LogLevel.Major, "TradeEngine: {0} sent {1} ", m_Strategy.Name, syntheticOrder);
            }
            // Exit
            if (syntheticOrder == null)
                return -1;
            else
                return syntheticOrder.OrderId;
        }// Quote()
        //
        // *****************************************
        // ****         CancelAllOrders         ****
        // *****************************************
        /// <summary>
        /// </summary>
        /// <param name="tradeSide">Market side to cancel (or -1 means both sides).</param>
        public void CancelAllOrders(int tradeSide = -1)
        {
            if (tradeSide < 0)
            {
                for (int side = 0; side < m_ActiveOrders.Length; ++side)
                    if (m_ActiveOrders[side] != null && m_ActiveOrders[side].Qty != 0)
                        Quote(side, m_ActiveOrders[side].Price, 0, "CancelAll");
            }
            else if (m_ActiveOrders[tradeSide] != null && m_ActiveOrders[tradeSide].Qty != 0)
                Quote(tradeSide, m_ActiveOrders[tradeSide].Price, 0, "CancelAll");
        }//CancelAllOrders()
        //
        //
        // *********************************************
        // ****     ProcessSyntheticOrder()         ****
        // *********************************************
        /// <summary>
        /// This function returns the "new" strategy-level synthetic fills.
        /// </summary>
        /// <param name="syntheticOrder"></param>
        /// <param name="syntheticFills"></param>
        /// <returns></returns>
        public bool ProcessSyntheticOrder(SyntheticOrder syntheticOrder, ref List<Fill> syntheticFills)
        {
            bool newFillsFound = false;
            SyntheticOrder prevOrder = null;
            if (m_FilledOrders.TryGetValue(syntheticOrder.OrderId, out prevOrder))
            { // we have seen this order before, so lets just find the new fills and save them
                int n = 0;
                if (prevOrder.m_SyntheticFills != null)
                    n = prevOrder.m_SyntheticFills.Count;
                for (int i = n; i < syntheticOrder.m_SyntheticFills.Count; ++i)
                    syntheticFills.Add(syntheticOrder.m_SyntheticFills[i]);
                m_FilledOrders[syntheticOrder.OrderId] = syntheticOrder.Copy();  // save the newest order update
                newFillsFound = n != syntheticOrder.m_SyntheticFills.Count;
            }
            else
            {   // We have never seen this order order before, add it to the dictionary.
                m_FilledOrders.Add(syntheticOrder.OrderId, syntheticOrder.Copy()); // this has to be a copy for us to be able to compare.
                foreach (SyntheticFill newSynthFill in syntheticOrder.m_SyntheticFills)
                    syntheticFills.Add(newSynthFill);
                newFillsFound = syntheticOrder.m_SyntheticFills.Count > 0;
            }
            return newFillsFound;
        }// ProcessSyntheticOrder()
        //
        //
        //
        // *****************************************
        // ****         Process Event()         ****
        // *****************************************
        /// <summary>
        /// This engine is the liason between the execution engines
        /// and the PricingEngine and Strategy, so there are no 
        /// </summary>
        /// <param name="eventArgs"></param>
        public override void ProcessEvent(EventArgs eventArgs)
        {
            if (eventArgs is EngineEventArgs)
            {   // Synthetic orders would come from the execution hub.                
                EngineEventArgs engineEventArgs = (EngineEventArgs)eventArgs;
                //SyntheticOrder syntheticOrder = (SyntheticOrder)engineEventArgs.DataObjectList[0];
                if (engineEventArgs.Status == EngineEventArgs.EventStatus.Request)
                {   // Execution hub would not send requests.
                    return;
                }
                else if (engineEventArgs.Status == EngineEventArgs.EventStatus.Confirm)
                {
                    switch (engineEventArgs.MsgType)
                    {
                        //
                        // ****         Parameter Value            ****
                        //
                        case EngineEventArgs.EventType.ParameterValue:
                            //for (int ptr = 0; ptr < eArgs.DataIntA.Length;++ptr)    // loop thru each param id.
                            //{
                            //    ParameterInfo pInfo = m_PInfo[ptr];
                            //}


                            break;
                        //
                        // ****         Parameter Change          ****
                        //
                        case EngineEventArgs.EventType.ParameterChange:
                            if (engineEventArgs.DataIntA == null)
                            {   // Here, cannot blindly change all parameter values, this should NOT be null.

                            }
                            else
                            {   // eArgs.DataIntA contains list of parameter IDs that user wants changed.
                                // eArgs.DataObjectList contains new values desired.
                                /*
                                for (int i = 0; i < eArgs.DataIntA.Length; ++i) //ith parameter in the list inside the event.
                                {
                                    int pID = eArgs.DataIntA[i];                // propertyID of parameter
                                    ParameterInfo pInfo = m_PInfo[pID];
                                    PropertyInfo property = this.GetType().GetProperty(pInfo.Name);
                                    if (property.CanWrite)
                                    {   // If writable, try to set value of this parameter in *this* object.
                                        try
                                        {
                                            property.SetValue(this, eArgs.DataObjectList[i], null);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                    // Regardless of result, get the current value
                                    object o = property.GetValue(this, null);
                                    eArgs.DataObjectList[i] = o;
                                }
                                // Set status to confirm signifying that the values in the message 
                                // are the correct, confirmed values from the engine.
                                eArgs.Status = EngineEventArgs.EventStatus.Confirm;
                                this.IsUpdateRequired = true;   // signal that we need updating!
                                */
                            }
                            break;
                        default:
                            //eArgs.Status = EngineEventArgs.EventStatus.Failed;
                            break;
                    }//switch msgType
                }
            }

        }// ProcessEvent()
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

        #region IStringify 
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        public override string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.Append(base.GetAttributes());
            s.AppendFormat(" ExecutionHubName={0}", m_ExecutionHubName);
            return s.ToString();
        }
        public override List<Lib.IO.Xml.IStringifiable> GetElements()
        {
            return base.GetElements();
        }
        public override void SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (KeyValuePair<string,string>kv in attributes)
                if (kv.Key.Equals("ExecutionHubName"))
                    m_ExecutionHubName = kv.Value;
            base.SetAttributes(attributes);
        }
        public override void AddSubElement(Lib.IO.Xml.IStringifiable subElement)
        {
            base.AddSubElement(subElement);
        }



        //
        #endregion//Private Methods

    }//end class        
}

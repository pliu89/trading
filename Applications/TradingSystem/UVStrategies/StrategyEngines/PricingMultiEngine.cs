using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.StrategyEngines
{
    using UV.Lib.IO.Xml;
    using UV.Lib.Engines;
    using UV.Lib.Hubs;
    using UV.Lib.BookHubs;

    //using UV.Lib.Products;
    
    //using UV.Lib.Fills;
    //using UV.Lib.OrderBooks;
    
    //using UV.Lib.DatabaseReaderWriters.Queries;     // Historic data handling
    //using UV.Lib.MarketHubs;
    //using UV.Lib.Utilities.Alarms;
    using UV.Strategies.StrategyHubs;

    /// <summary>
    /// Deprecated.... never fully implemented.
    /// </summary>
    public class PricingMultiEngine : PricingEngine, IStringifiable, ITimerSubscriber
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        //        
        protected Strategy ParentStrategy = null;
        protected List<IPricingEngine> m_PricingEngines = new List<IPricingEngine>();



        #endregion// members


        #region Constructors & Setup
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public PricingMultiEngine()
        {

        }
        //
        //
        // *************************************************
        // ****             SetupInitialize()           ****
        // *************************************************
        protected override void SetupInitialize(IEngineHub myEngineHub, IEngineContainer engineContainer, int engineID, bool setupGui)
        {
            base.SetupInitialize(myEngineHub, engineContainer, engineID, setupGui);
        }//SetupInitialize()
        //
        //
        // *************************************************
        // ****             SetupBegin()                 ****
        // *************************************************

        public override void SetupBegin(IEngineHub myEngineHub, IEngineContainer engineContainer)
        {

            base.SetupBegin(myEngineHub, engineContainer);
            
            
            // ParentStrategy.StrategyHub.SubscribeToTimer(ParentStrategy, this);             
            // ParentStrategy.m_OrderEngine.QuoteTickSize = m_QuoteTickSize; // set our quote tick size for proper rounding
        }
        //
        // *************************************************
        // ****             SetupComplete()             ****
        // *************************************************
        public override void SetupComplete()
        {
            base.SetupComplete();

            // Collect all other pricing engines.
            foreach (IEngine iEngine in base.ParentStrategy.GetEngines())
                if (iEngine is IPricingEngine && iEngine != this)
                    m_PricingEngines.Add( (IPricingEngine) iEngine);
            base.Log.NewEntry(LogLevel.Minor,"{0} PricingMultiEngine.SetupComplete() found {1} pricing engines.", base.ParentStrategy.Name, m_PricingEngines.Count);

            // 
            foreach (IPricingEngine iPricingEngine in m_PricingEngines)
            {
                // Unsubscribe Timers
                if (iPricingEngine is ITimerSubscriber)
                {
                    //if (ParentStrategy.StrategyHub.IsSubscribeToTimer((ITimerSubscriber)iPricingEngine))
                    {
                        // unsubscribe and collect its pointer so i can subscribe myself.
                    }
                }
            }


            //UV.Lib.Application.AppServices service = UV.Lib.Application.AppServices.GetInstance(); // get service and find user for reporting
            //m_User = service.User;
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

    }//end class
}

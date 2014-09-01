using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Orders
{
    using Misty.Lib.Hubs;
    using Ambre.TTServices;
    using TradingTechnologies.TTAPI;
    using TradingTechnologies.TTAPI.Tradebook;

    using Misty.Lib.IO.Xml;
    using Misty.Lib.Utilities;

    /// <summary>
    /// 
    /// </summary>
    public class OrderListener : IDisposable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        private Hub m_ParentHub = null;
        private LogHub Log = null;                                  // log I can write to.
        private string m_Name = string.Empty;                       // identifier for object and its thread.
        private bool m_isDisposing = false;

        private WorkerDispatcher m_Dispatcher = null;               // TT's WorkerDispatcher
        private TradeSubscription m_TradeSubsciption = null;
        private TTApiService m_TTService = null;

        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// Immediately starts the thread.  A TT Session must already exist when this is created.
        /// </summary>
        /// <param name="listenerName"></param>
        /// <param name="aLog"></param>
        public OrderListener(string listenerName, Hub parentHub)
        {
            m_ParentHub = parentHub;
            this.Log = m_ParentHub.Log;
            this.m_Name = listenerName;
            m_TTService = TTApiService.GetInstance();
        }
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        public void Start()
        {
            System.Threading.Thread thread = new System.Threading.Thread(InitializeThread);
            thread.Name = this.m_Name;
            thread.Start();
        }// Start()
        //
        //
        //
        public void Dispose()
        {
            if (m_isDisposing) return;
            m_isDisposing = true;

            m_Dispatcher.BeginInvoke(new Action(StopThread));
            m_Dispatcher.Run();

            m_TradeSubsciption.Dispose();

        }//Dispose()
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        /// <summary>
        /// This is the first method called by the FillListener's worker thread.
        /// To capture itself permanantly, it will call its own dispatcher and complete its
        /// initialization.
        /// </summary>
        private void InitializeThread()
        {
            m_Dispatcher = TradingTechnologies.TTAPI.Dispatcher.AttachWorkerDispatcher();
            m_Dispatcher.BeginInvoke(new Action(InitComplete));
            m_Dispatcher.Run();                                                 // this tells thread to do this and wait.
        }
        /// <summary>
        /// Called via my thread dispatcher to create the TradeSubscription object and start it.
        /// After which, the thread will sleep until further dispatcher actions are invoked.
        /// </summary>
        private void InitComplete()
        {
            this.Log.NewEntry(LogLevel.Major, "{0}: Trade listener started.", m_Name);
            // Subscribe to events.
            m_TradeSubsciption = new TradeSubscription(m_TTService.session, m_Dispatcher);
            m_TradeSubsciption.OrderFilled += new EventHandler<TradingTechnologies.TTAPI.OrderFilledEventArgs>(TT_OrderFilled);
            m_TradeSubsciption.OrderAdded += new EventHandler<TradingTechnologies.TTAPI.OrderAddedEventArgs>(TT_OrderAdded);
            m_TradeSubsciption.OrderDeleted += new EventHandler<TradingTechnologies.TTAPI.OrderDeletedEventArgs>(TT_OrderDeleted);
            m_TradeSubsciption.OrderBookDownload += new EventHandler<TradingTechnologies.TTAPI.OrderBookDownloadEventArgs>(TT_OrderBookDownload);
            m_TradeSubsciption.OrderRejected += new EventHandler<OrderRejectedEventArgs>(TT_OrderRejected);

            m_TradeSubsciption.Start();
        }//InitComplete()
        /// <summary>
        /// Called by the Dispose() function.
        /// </summary>
        private void StopThread()
        {
            if (m_Dispatcher != null && (!m_Dispatcher.IsDisposed))
                m_Dispatcher.Dispose();
            if (m_TradeSubsciption != null)
            {
                Log.NewEntry(LogLevel.Minor, "{0}: Shutting down TradeSubscription.", m_Name);
                m_TradeSubsciption.OrderFilled -= new EventHandler<TradingTechnologies.TTAPI.OrderFilledEventArgs>(TT_OrderFilled);
                m_TradeSubsciption.OrderAdded -= new EventHandler<TradingTechnologies.TTAPI.OrderAddedEventArgs>(TT_OrderAdded);
                m_TradeSubsciption.OrderDeleted -= new EventHandler<TradingTechnologies.TTAPI.OrderDeletedEventArgs>(TT_OrderDeleted);
                m_TradeSubsciption.OrderBookDownload -= new EventHandler<TradingTechnologies.TTAPI.OrderBookDownloadEventArgs>(TT_OrderBookDownload);
                m_TradeSubsciption.OrderRejected -= new EventHandler<OrderRejectedEventArgs>(TT_OrderRejected);
                m_TradeSubsciption.Dispose();
            }
        }//StopThread()
        //
        //
        //
        //
        //
        //
        #endregion // private methods



        #region TT Event Handlers
        // *********************************************************************************
        // ****                             Fills from TT                               ****
        // *********************************************************************************
        private void TT_OrderFilled(object sender, TradingTechnologies.TTAPI.OrderFilledEventArgs eventArgs)
        {
            m_ParentHub.HubEventEnqueue(eventArgs);
        }
        private void TT_OrderAdded(object sender, TradingTechnologies.TTAPI.OrderAddedEventArgs eventArgs)
        {
            m_ParentHub.HubEventEnqueue(eventArgs);
        }
        private void TT_OrderDeleted(object sender, TradingTechnologies.TTAPI.OrderDeletedEventArgs eventArgs)
        {
            m_ParentHub.HubEventEnqueue(eventArgs);
        }
        private void TT_OrderBookDownload(object sender, TradingTechnologies.TTAPI.OrderBookDownloadEventArgs eventArgs)
        {
            m_ParentHub.HubEventEnqueue(eventArgs);
        }
        private void TT_OrderRejected(object sender, OrderRejectedEventArgs eventArgs)
        {
            m_ParentHub.HubEventEnqueue(eventArgs);
        }
        private string Show(Order o)
        {
            return string.Format("[{1} {0} {2} {3} {4}]", o.Action, o.InstrumentKey,o.BuySell,o.LimitPrice.ToDouble(), o.WorkingQuantity.ToInt());
        }
        //
        //
        #endregion//Private Methods


        #region My Events and Triggers
        // ******************************************************************
        // ****                 My Events                                ****
        // ******************************************************************
        //
        //
        //public event EventHandler<Order> OrderAdded;
        //public event EventHandler<Order> OrderDeleted;
        //public event EventHandler<Order> OrderRejected;
        //
        //
        //
        #endregion//My Events

    }
}

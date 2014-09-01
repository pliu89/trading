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
    public class OrderListenerMsgr : IDisposable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
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
        public OrderListenerMsgr(string listenerName, LogHub aLog)
        {
            this.Log = aLog;
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
            //m_TradeSubsciption.OrderRejected
            //m_TradeSubsciption.OrderStatusUnknown
            //m_TradeSubsciption.OrderUpdated

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
            SendMessage("FilledOldOrder", eventArgs.OldOrder);
            SendMessage("FilledNewOrder", eventArgs.NewOrder);
        }
        private StringBuilder msg = new StringBuilder();
        private void TT_OrderAdded(object sender, TradingTechnologies.TTAPI.OrderAddedEventArgs eventArgs)
        {
            Order order = eventArgs.Order;
            SendMessage("OrderAdded", order);
        }
        private void TT_OrderDeleted(object sender, TradingTechnologies.TTAPI.OrderDeletedEventArgs eventArgs)
        {
            Order order = eventArgs.DeletedUpdate;
            SendMessage("OrderDeleted", order);
        }
        private void TT_OrderBookDownload(object sender, TradingTechnologies.TTAPI.OrderBookDownloadEventArgs eventArgs)
        {
            foreach (Order order in eventArgs.Orders)
            {
                InstrumentKey key = order.InstrumentKey;
                SendMessage("OrderBookDownload", order);
            }
        }
        private void SendMessage(string eventName, Order order)
        {
            InstrumentKey key = order.InstrumentKey;
            msg.Clear();
            msg.AppendFormat("{0}", eventName);
            msg.AppendFormat(",{0},{2}({1}),{3}", Enum.GetName(typeof(BuySell), order.BuySell), order.WorkingQuantity.ToInt(), order.LimitPrice.ToDouble(), order.FillQuantity.ToInt());
            msg.AppendFormat(",{0},{1},{2}", order.Status, order.Received.ToString(Strings.FormatDateTimeZone), order.OrderKey.ToString());
            OnMessage(key, msg.ToString());
        }
        //
        //
        #endregion//Private Methods


        #region My Events and Triggers
        // ******************************************************************
        // ****                 My Events                                ****
        // ******************************************************************
        //
        public event EventHandler Message;
        private void OnMessage(InstrumentKey key, string s)
        {

            if (Message != null)
            {
                Message(this, new MessageEventArgs(key, s));
            }
        }

        public class MessageEventArgs : EventArgs
        {
            public string Message;
            public InstrumentKey Key;
            public MessageEventArgs(InstrumentKey key, string msg)
            {
                this.Key = key;
                this.Message = msg;
            }
        }
        //
        //
        //
        #endregion//My Events

    }
}

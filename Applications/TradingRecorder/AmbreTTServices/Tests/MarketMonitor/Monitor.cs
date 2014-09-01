using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Ambre.TTServices.Tests.MarketMonitor
{
    using Misty.Lib.Utilities;
    using Misty.Lib.IO.Xml;
    using Misty.Lib.IO;
    using Misty.Lib.Hubs;

    //using InstrumentBase = Misty.Lib.Products.InstrumentBase;          // to distinguish from TT instrument class.
    using InstrumentName = Misty.Lib.Products.InstrumentName;

    using Ambre.TTServices.Markets;
    using Ambre.TTServices.Orders;
    using Ambre.TTServices.Fills;

    using TradingTechnologies.TTAPI;
    using TradingTechnologies.TTAPI.WinFormsHelpers;

    public partial class Monitor : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        //
        private bool m_IsShuttingDown = false;
        private TTApiService m_TTService = null;                                // Holds the TT API
        private PriceListener m_PriceListener = null;                           // my workerthread helper.
        private FillListener m_FillListener = null;
        private OrderListenerMsgr m_OrderListener = null;
        private LogHub Log = null;
        private DropQueueWriter m_Writer = null;


        // Lookup tables
        private ConcurrentDictionary<InstrumentName, InstrumentKey> m_Name2Key = new ConcurrentDictionary<InstrumentName, InstrumentKey>();
        private ConcurrentDictionary<InstrumentKey, InstrumentName> m_Key2Name = new ConcurrentDictionary<InstrumentKey, InstrumentName>();


        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public Monitor()
        {
            InitializeComponent();

            //Misty.Lib.Application.AppInfo appInfo = Misty.Lib.Application.AppInfo.GetInstance("Monitor",true);
            string currentPath = System.IO.Directory.GetCurrentDirectory();
            Log = new LogHub("Monitor", string.Format("{0}\\Logs\\", currentPath), true, LogLevel.ShowAllMessages);
            DateTime now = Log.GetTime();

            m_Writer = new DropQueueWriter(string.Format("{0}\\Logs\\",currentPath),string.Format("data_{0}_{1}.txt",now.ToString("yyyyMMdd"),now.ToString("HHmmss")),this.Log);
            m_Writer.Start();

            string s = string.Format("{0}\\user_female.ico",currentPath);
            if (System.IO.File.Exists(s))
                this.Icon =  Icon.ExtractAssociatedIcon(s);

            // Instantiate TT API.
            m_TTService = TTServices.TTApiService.GetInstance();
            m_TTService.ServiceStateChanged += new EventHandler(TTService_ServiceStateChanged);
            m_TTService.Start(true);


        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region Public Methods
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


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //

        //
        private void Shutdown()
        {
            m_IsShuttingDown = true;
            if (m_Writer != null)
            {
                m_Writer.RequestStop();
                m_Writer = null;
            }
            if (m_TTService != null)
            {
                m_TTService.Dispose();
                m_TTService = null;
            }
            if (m_PriceListener != null)
            {
                m_PriceListener.Dispose();
                m_PriceListener = null;
            }
            if (m_FillListener != null)
            {
                m_FillListener.Dispose();
                m_FillListener = null;
            }
            if (m_OrderListener != null)
            {
                m_OrderListener.Dispose();
                m_OrderListener = null;
            }
            if (Log != null)
            {
                Log.RequestStop();
                Log = null;
            }
        }
        //
        #endregion//Private Methods


        #region Service Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        private void TTService_ServiceStateChanged(object sender, EventArgs eventArgs)
        {
            if (m_IsShuttingDown) return;
            if (eventArgs is TTServices.TTApiService.ServiceStatusChangeEventArgs)
            {
                TTServices.TTApiService.ServiceStatusChangeEventArgs e = (TTServices.TTApiService.ServiceStatusChangeEventArgs)eventArgs;
                if (e.IsConnected)
                {
                    m_PriceListener = new PriceListener("PriceListener", Log);
                    m_PriceListener.InstrumentsFound += new EventHandler(PriceListener_InstrumentsFound);
                    m_PriceListener.ProcessPriceChangeEvents = new PriceListener.ProcessPriceChangeDelegate(PriceListener_ProcessMarketEvents);
                    m_PriceListener.Start();

                    m_FillListener = new FillListener("FillListener");
                    m_FillListener.Log = Log;
                    m_FillListener.Filled += new EventHandler(FillListener_Filled);
                    m_FillListener.Start();

                    m_OrderListener = new OrderListenerMsgr("OrderListener", Log);
                    m_OrderListener.Message += new EventHandler(OrderListener_Message);
                    m_OrderListener.Start();

                }
                else
                {
                    
                }
            }
        }// TTServices_ServiceStateChanged()
        //
        //
        //
        private object m_InstrumentLock = new object();
        private ConcurrentDictionary<InstrumentName, InstrumentDetails> m_InstrumentDetails = new ConcurrentDictionary<InstrumentName, InstrumentDetails>();
        private void PriceListener_InstrumentsFound(object sender, EventArgs eventArgs)
        {
            PriceListener.InstrumentsFoundEventArgs e = (PriceListener.InstrumentsFoundEventArgs)eventArgs;
            List<Misty.Lib.Products.InstrumentName> instrFound = new List<Misty.Lib.Products.InstrumentName>();
            lock (m_InstrumentLock)
            {
                foreach (InstrumentName name in e.InstrumentDetails.Keys)
                {
                    if (!m_InstrumentDetails.ContainsKey(name))
                    {
                        InstrumentDetails details = e.InstrumentDetails[name];
                        m_Name2Key.TryAdd(name, details.Key);
                        m_Key2Name.TryAdd(details.Key, name);
                        m_InstrumentDetails.TryAdd(name, details);
                        Market mkt = new Market(name);
                        m_Markets.TryAdd(name, mkt);
                    }
                    m_PriceListener.SubscribeTo(e.InstrumentDetails[name].Key, new PriceSubscriptionSettings(PriceSubscriptionType.InsideMarket));
                }
            }// lock
        }
        //
        //
        private class PriceQty
        {
            public double Price = 0;
            public int Qty = 0;
        }
        private class Market
        {
            public InstrumentName Name;
            public bool IsChanged = true;
            
            public PriceQty[] PQ = new PriceQty[5];
            public Market(InstrumentName name)
            {
                this.Name = name;
                for (int i = 0; i < PQ.Length; ++i) { PQ[i] = new PriceQty(); }
            }
            public string ToString(DateTime now)
            {                
                return string.Format("{0},{1},Mkt,{2}({3}),{4}({5}),{6}({7})", now.ToString(Strings.FormatDateTimeZone), Name
                    , PQ[0].Price, PQ[0].Qty
                    , PQ[1].Price, PQ[1].Qty
                    , PQ[2].Price, PQ[2].Qty   );
            }
        }
        private ConcurrentDictionary<InstrumentName, Market> m_Markets = new ConcurrentDictionary<InstrumentName, Market>();
        private void PriceListener_ProcessMarketEvents(ref List<EventArgs> eventArgs)
        {
            DateTime now = Log.GetTime();
            foreach (EventArgs eventArg in eventArgs)
            {
                if (eventArg is Misty.Lib.BookHubs.MarketUpdateEventArgs)
                {
                    Misty.Lib.BookHubs.MarketUpdateEventArgs e = (Misty.Lib.BookHubs.MarketUpdateEventArgs)eventArg;
                    Market mkt;
                    if (m_Markets.TryGetValue(e.Name, out mkt) )                    
                    {
                        mkt.PQ[e.Side].Price = e.Price;
                        mkt.PQ[e.Side].Qty = e.Qty;
                        mkt.IsChanged = true;
                    }
                }
                else if (eventArg is Misty.Lib.BookHubs.MarketStatusEventArgs)                
                {
                    Misty.Lib.BookHubs.MarketStatusEventArgs e = (Misty.Lib.BookHubs.MarketStatusEventArgs)eventArg;
                    string s = string.Format("{0},{1},Status,{2}", now.ToString(Strings.FormatDateTimeZone), e.InstrumentName, e.Status);
                    Console.WriteLine(s);
                    m_Writer.RequestEnqueue(s);
                }
            }
            //
            foreach (Market mkt in m_Markets.Values)
            {
                if (mkt.IsChanged)
                {
                    Console.WriteLine(mkt.ToString(now));
                    m_Writer.RequestEnqueue(mkt.ToString(now));
                    // REset trades to zero
                    mkt.PQ[2].Qty = 0;
                    mkt.IsChanged = false;
                }
            }

        }// PriceListener_ProcessMarketEvents
        //
        //
        private void FillListener_Filled(object sender, EventArgs eventArg)
        {
            if (eventArg is FillEventArgs)
            {
                DateTime now = Log.GetTime();
                FillEventArgs e = (FillEventArgs)eventArg;
                InstrumentName instrumentName;
                if (m_Key2Name.TryGetValue(e.TTInstrumentKey, out instrumentName))
                {
                    string s = string.Format("{0},{1},Fill,{2}", now.ToString(Strings.FormatDateTimeZone), instrumentName, e.Fill.ToString());
                    m_Writer.RequestEnqueue(s);
                    Console.WriteLine(s);
                }
            }
        }
        private void OrderListener_Message(object sender, EventArgs eventArg)
        {
            OrderListenerMsgr.MessageEventArgs e = (OrderListenerMsgr.MessageEventArgs)eventArg;
            InstrumentName name;
            DateTime now = Log.GetTime();
            if (m_Key2Name.TryGetValue(e.Key, out name) || e.Message.Contains("Download") )
            {
                string s = string.Format("{0},{1},{2}", now.ToString(Strings.FormatDateTimeZone), name, e.Message);
                m_Writer.RequestEnqueue(s);
                Console.WriteLine(s);
            }
        }
        //
        //
        //
        #endregion//Service Event Handlers


        #region Form Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        private void Monitor_FormClosing(object sender, FormClosingEventArgs e)
        {
            Shutdown();
        }
        private void Monitor_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.HasInstrumentKeys())
            {
                foreach (InstrumentKey key in e.Data.GetInstrumentKeys())                       // Loop thru each instr dropped.
                {
                    InstrumentName instrumentName;
                    if (m_Key2Name.TryGetValue(key, out instrumentName))
                    {
                        Log.NewEntry(LogLevel.Major, "DragDrop: Already subscribed to instrument key {0}",key);

                    }
                    else
                    {
                        m_PriceListener.SubscribeTo(key);
                    }
                    
                }//next instrumentKey
            }
        }
        //
        // ****                 Form1_DragOver()                ****
        /// <summary>
        /// Show dragover effects to let user know we will respond to his drop.
        /// </summary>
        private void Monitor_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.HasInstrumentKeys()){ e.Effect = DragDropEffects.Copy; }
        }
        //

        #endregion // form event handlers

    }
}

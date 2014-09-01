using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Windows.Forms;

namespace Ambre.AutoStackingTracker.FrontEnds
{
    using Misty.Lib.Application;
    using Misty.Lib.Products;
    using Misty.Lib.OrderHubs;
    using Misty.Lib.OrderHubs.FrontEnds;
    using Ambre.TTServices.Markets;         // need to load this into domain to find it using in appServices.
    using Ambre.TTServices;                 // need to load this into domain to find it using in appServices.

    public partial class Form2 : Form
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Local variables
        //
        private ConcurrentDictionary<InstrumentName, OrderBookDepthListView> m_OrderBookViews = new ConcurrentDictionary<InstrumentName, OrderBookDepthListView>();
        //private TTApiService m_TTService;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Form2()
        {
            InitializeComponent();

            typeof(Ambre.TTServices.Markets.MarketTTAPI).ToString();        // force this needed assembly to load.
            AppServices appServices = AppServices.GetInstance("AutoStackingTracker", true);  // Set application information - do this before hubs are instantiated.
            appServices.Info.RequestShutdownAddHandler(new EventHandler(this.RequestShutdownHandler));
            appServices.LoadServicesFromFile("AutoStackingConfig.txt");     // Creates all services.

            // Attach event handlers to events this GUI is interested in.
            foreach (IService service in AppServices.GetInstance().GetServices())
            {
                if (service is OrderHub)
                {
                    OrderHub hub = (OrderHub)service;
                    hub.BookCreated += new EventHandler(OrderHub_BookCreated);
                    hub.BookChanged += new EventHandler(OrderHub_BookChanged);
                    //hub.BookDeleted += new EventHandler(OrderHub_BookCreated);
                }
            }

            // Can use this to monitor xTrader
            //System.Diagnostics.Process[] procs2 = System.Diagnostics.Process.GetProcesses();
            //System.Diagnostics.Process[] procs = System.Diagnostics.Process.GetProcessesByName("x_trader");



            //             
            appServices.Start();                                            // Start thread hubs.
            appServices.Connect();


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
        //
        // ****         CreateOrderBookView()           ****
        //
        private void CreateOrderBookView(OrderBookEventArgs eventArg)
        {
            int nColumns = 2;

            OrderBookDepthListView view;
            if (OrderBookDepthListView.TryCreate(eventArg, out view) && m_OrderBookViews.TryAdd(eventArg.Instrument, view))
            {
                this.SuspendLayout();
                Size defaultSize = new Size(view.Size.Width, view.Size.Height);
                this.ClientSize = new Size(defaultSize.Width * nColumns, defaultSize.Height );

                // Place on screen 

                int nY = (int) Math.Ceiling((1.0*m_OrderBookViews.Count) / nColumns) - 1;
                int nX = (m_OrderBookViews.Count-1) % nColumns;

                view.Location = new Point(nX*defaultSize.Width,nY*defaultSize.Height);
                this.Controls.Add(view);


                this.ResumeLayout(true);
            }
        }// CreateOrderBookView()
        //
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        // ****         Request Shutdown Handler()          ****
        // 
        /// <summary>
        /// Some part of application has requested a shutdown.  Lets close the form.
        /// </summary>
        private void RequestShutdownHandler(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(RequestShutdownHandler), new object[] { sender, eventArgs });
            else
                this.Close();
        }
        //
        //
        // ****         OrderHub_BookCreated()          ****
        //
        private void OrderHub_BookCreated(object sender, EventArgs eventArgs)
        {
            if (this.InvokeRequired)
                this.Invoke(new EventHandler(OrderHub_BookCreated), new object[] { sender, eventArgs });
            else
            {
                OrderBookEventArgs eventArg = (OrderBookEventArgs)eventArgs;
                if (!m_OrderBookViews.ContainsKey(eventArg.Instrument))
                    CreateOrderBookView(eventArg);
            }
        }// OrderHub_BookCreated().
        //
        //
        // ****         OrderHub_BookChanged()          ****
        //
        private void OrderHub_BookChanged(object sender, EventArgs eventArgs)
        {
            OrderBookEventArgs eventArg = (OrderBookEventArgs)eventArgs;
            OrderBookDepthListView view;
            if (m_OrderBookViews.TryGetValue(eventArg.Instrument, out view))
                view.OrderHub_BookChanged(sender, eventArgs);
        }
        //
        //
        //
        #endregion//Event Handlers


        #region no Form Event Handlers
        // *****************************************************************
        // ****                Form  Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            AppServices.GetInstance().Shutdown();
        }
        //
        #endregion//Event Handlers

    }
}

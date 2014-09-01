using System;
using System.Collections.Generic;
using System.Text;

namespace UV.TTServices.Fills
{
    using UV.Lib.Hubs;
    using UV.TTServices;
    using TradingTechnologies.TTAPI;
    using TradingTechnologies.TTAPI.Tradebook;

    using UV.Lib.IO.Xml;

    /// <summary>
    /// Instantiates a FillSubscription() object.
    /// </summary>
    public class FillListener : IDisposable, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        public LogHub Log = null;                                   // log I can write to - owned by my parent FillHub, usually.
        public string Name = string.Empty;                          // identifier for object and its thread.
        private bool m_isDisposing = false;
        public bool m_LogAllFillPropertiesFromTT = false;           // flag controlling whether write the long fill message for each fill.

        private WorkerDispatcher m_Dispatcher = null;               // TT's WorkerDispatcher
        private TradeSubscription m_TradeSubsciption = null;        // The TT subscription object.
        private TTApiService m_TTService = null;

        private TradeSubscriptionFilter m_TradeFilter = null;       // Filter to apply to TT Trade subscritions
        private string m_TradeFilterArg = string.Empty;             // The key associated with the filter.
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
        public FillListener(string listenerName)
        {
            this.Name = listenerName;
            m_TTService = TTApiService.GetInstance();         

        }
        public FillListener()
        {
            this.Name = "FillListener";
            m_TTService = TTApiService.GetInstance();
        }
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public void Start()
        {
            // Initialize a new worker thread.
            System.Threading.Thread thread = new System.Threading.Thread(InitializeThread);
            thread.Name = this.Name;
            thread.Start();
            if (Log != null)
                Log.NewEntry(LogLevel.Major, "FillListener: {0} started {1}",Name, this);
        }
        //
        // ****             Dispose()               ****
        /// <summary>
        /// Called by an external thread, invoke the StopThread method.
        /// </summary>
        public void Dispose()
        {
            if (m_isDisposing) 
                return;
            m_isDisposing = true;

            try
            {
                m_Dispatcher.BeginInvoke(new Action(StopThread));
            }
            catch (Exception )
            {
            }

        }//Dispose()
        //
        //
        //
        /// <summary>
        /// After the Listener is instatiated, but BEFORE it is started, the user requests that this listener
        /// only listen to a specific account.
        /// </summary>
        /// <param name="accountName">name of account to listen to</param>
        public void FilterByAccount(string accountName)
        {
            TradeSubscriptionAccountFilter filter = new TradeSubscriptionAccountFilter(accountName, false, "Account");
            this.m_TradeFilter = filter;
            m_TradeFilterArg = accountName;
            if (Log!=null)
                Log.NewEntry(LogLevel.Minor, "FillListener.FilterByAccount: Filtering by account {0}", accountName);
        }
        public void FilterByInstrument(InstrumentKey instrumentKey)
        {
            TradeSubscriptionInstrumentFilter filter = new TradeSubscriptionInstrumentFilter(m_TTService.session, instrumentKey, false, instrumentKey.ToString());
            this.m_TradeFilter = filter;
            m_TradeFilterArg = instrumentKey.ToString();
            if (Log != null)
                Log.NewEntry(LogLevel.Minor, "FillListener.FilterByInstrument: Filtering by instrumentKey {0}",instrumentKey);
        }
        //
        public override string ToString()
        {
            if (m_TradeFilter == null)
                return Name;
            else if (m_TradeFilter.GetType() == typeof(TradeSubscriptionAccountFilter))
                return string.Format("{0} Acct={1}", this.Name, m_TradeFilterArg);
            else if (m_TradeFilter.GetType() == typeof(TradeSubscriptionInstrumentFilter))
                return string.Format("{0} Instr={1}", this.Name, m_TradeFilterArg);
            else
                return Name;
        }
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
            if (Log != null)
                Log.NewEntry(LogLevel.Major, "{0}: Listener thread started.",this);
            // Subscribe to events.
            m_TradeSubsciption = new TradeSubscription(m_TTService.session,m_Dispatcher);
            if ( m_TradeFilter != null )
                m_TradeSubsciption.SetFilter(m_TradeFilter);
            
            m_TradeSubsciption.OrderFilled += new EventHandler<TradingTechnologies.TTAPI.OrderFilledEventArgs>(TT_OrderFilled);            
            m_TradeSubsciption.Rollover += new EventHandler<RolloverEventArgs>(TT_Rollover);
            m_TradeSubsciption.FillRecordAdded += new EventHandler<FillAddedEventArgs>(TT_RecordedFill);
            m_TradeSubsciption.AdminFillAdded += new EventHandler<FillAddedEventArgs>(TT_AdminFill);
            m_TradeSubsciption.AdminFillDeleted += new EventHandler<FillDeletedEventArgs>(TT_AdminFillDeleted);

            m_TradeSubsciption.FillBookDownload += new EventHandler<TradingTechnologies.TTAPI.FillBookDownloadEventArgs>(TT_FillListDownLoad);
            m_TradeSubsciption.FillListStart += new EventHandler<TradingTechnologies.TTAPI.FillListEventArgs>(TT_FillListStart);
            m_TradeSubsciption.FillListEnd += new EventHandler<TradingTechnologies.TTAPI.FillListEventArgs>(TT_FillListEnd);

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
                if (Log != null)
                    Log.NewEntry(LogLevel.Minor, "FillListener: {0} shutting down TradeSubscription.", this);
                m_TradeSubsciption.OrderFilled -= new EventHandler<TradingTechnologies.TTAPI.OrderFilledEventArgs>(TT_OrderFilled);
                m_TradeSubsciption.Rollover -= new EventHandler<RolloverEventArgs>(TT_Rollover);
                m_TradeSubsciption.FillRecordAdded -= new EventHandler<FillAddedEventArgs>(TT_RecordedFill);
                m_TradeSubsciption.AdminFillAdded -= new EventHandler<FillAddedEventArgs>(TT_AdminFill);
                m_TradeSubsciption.AdminFillDeleted -= new EventHandler<FillDeletedEventArgs>(TT_AdminFillDeleted);

                m_TradeSubsciption.FillBookDownload -= new EventHandler<TradingTechnologies.TTAPI.FillBookDownloadEventArgs>(TT_FillListDownLoad);
                m_TradeSubsciption.FillListStart -= new EventHandler<TradingTechnologies.TTAPI.FillListEventArgs>(TT_FillListStart);
                m_TradeSubsciption.FillListEnd -= new EventHandler<TradingTechnologies.TTAPI.FillListEventArgs>(TT_FillListEnd);

                m_TradeSubsciption.Dispose();
                m_TradeSubsciption = null;
            }
        }//StopThread()
        //
        //
        //
        //
        private void WriteFillToLog(string fillEventName, Fill fill)
        {
            if (Log != null && Log.BeginEntry(LogLevel.Minor, "FillListener: {0} {1} ", this, fillEventName))
            {
                Log.AppendEntry("[InstrumentKey={0}]", fill.InstrumentKey.ToString());
                Log.AppendEntry("[{0} {1}@{2}]", Enum.GetName(typeof(BuySell), fill.BuySell), fill.Quantity.ToString(), fill.MatchPrice.ToDouble());

                System.Reflection.PropertyInfo[] properties = fill.GetType().GetProperties();

                // Non flags
                foreach (System.Reflection.PropertyInfo aProperty in properties)
                {
                    Type type = aProperty.PropertyType;
                    if (aProperty.CanRead && type != typeof(System.Boolean))
                    {
                        object o = aProperty.GetValue(fill);
                        if (o is DateTime)
                            Log.AppendEntry("[{0}={1}]", aProperty.Name, ((DateTime)o).ToString(UV.Lib.Utilities.Strings.FormatDateTimeZone));
                        else if (!string.IsNullOrEmpty(o.ToString()))
                            Log.AppendEntry("[{0}={1}]", aProperty.Name, o);
                    }
                }

                // Show flags
                Log.AppendEntry("[Flags");
                foreach (System.Reflection.PropertyInfo aProperty in properties)
                {
                    Type type = aProperty.PropertyType;
                    if (aProperty.CanRead && type == typeof(System.Boolean) && (bool)aProperty.GetValue(fill))
                        Log.AppendEntry(" {0}", aProperty.Name);        // if true, add name.
                }
                Log.AppendEntry("]");
                Log.EndEntry();
            }
        }// WriteFillToLog()
        //
        //
        // *************************************************************
        // ****             Create Fill EventArgs()                 ****
        // *************************************************************
        public static FillEventArgs CreateFillEventArg(FillType fillType, LogHub log, TradingTechnologies.TTAPI.Fill ttFill)
        {
            int qty = 0;
            if (ttFill.BuySell == BuySell.Buy)
                qty = ttFill.Quantity.ToInt();          // qty > 0 --> buy;
            else if (ttFill.BuySell == BuySell.Sell)
                qty = -ttFill.Quantity.ToInt();         // qty < 0 --> sell;
            UV.Lib.OrderHubs.Fill aFill = UV.Lib.OrderHubs.Fill.Create(qty, ttFill.MatchPrice.ToDouble(), log.GetTime(), ttFill.TransactionDateTime);
            FillEventArgs e = new FillEventArgs(ttFill.InstrumentKey, fillType, aFill);
            e.FillKey = ttFill.FillKey;
            e.AccountID = ttFill.AccountName;
            return e;
        }//CreateFillEventArg()
        //
        //
        //
        #endregion // private methods



        #region TT Event Handlers
        // *********************************************************************************
        // ****                             Fills from TT                               ****
        // *********************************************************************************
        /// <summary>
        /// These are live orders that have been filled.  These are orders directly associated with
        /// a specific order in the market.  (Leg fills associated with exchange-traded spreads at NOT here.)
        /// </summary>
        private void TT_OrderFilled(object sender, TradingTechnologies.TTAPI.OrderFilledEventArgs eventArgs)
        {
            if (m_LogAllFillPropertiesFromTT)
                WriteFillToLog(FillType.New.ToString(), eventArgs.Fill);

            OnFilled( CreateFillEventArg(FillType.New, this.Log, eventArgs.Fill) );
        }
        //
        /// <summary>
        /// These are fills from the exchange, but not associated directly with an order.
        /// </summary>
        private void TT_RecordedFill(object sender, TradingTechnologies.TTAPI.FillAddedEventArgs eventArgs)
        {
            OnFilled(CreateFillEventArg(FillType.New, this.Log, eventArgs.Fill));
        }
        private void TT_AdminFill(object sender, TradingTechnologies.TTAPI.FillAddedEventArgs eventArgs)
        {
            OnFilled(CreateFillEventArg(FillType.Adjustment, this.Log, eventArgs.Fill));
        }
        private void TT_AdminFillDeleted(object sender, TradingTechnologies.TTAPI.FillDeletedEventArgs eventArgs)
        {
            System.Text.StringBuilder msgText = new System.Text.StringBuilder("Received external admin fill deleted event. Accept fill?");
            string titleText = "Administrative fill deleted";
            System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.YesNo;
            System.Windows.Forms.MessageBoxDefaultButton defaultButton = System.Windows.Forms.MessageBoxDefaultButton.Button1;
            System.Windows.Forms.MessageBoxIcon icon = System.Windows.Forms.MessageBoxIcon.Question;
            System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(msgText.ToString(), titleText, buttons, icon, defaultButton);
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                if (Log!=null)
                    Log.NewEntry(LogLevel.Major, "FillListener: {0} delete administrated fill. NOT IMPLEMENTED YET.  Fill to delete follows: ",this);
                //WriteFillToLog("Admin Fill Delete",eventArgs.Fill);
            }
            else
            {
                if (Log != null)
                    Log.NewEntry(LogLevel.Major, "FillListener: {0} Delete administrated fill rejected. Doing nothing.  Fill to delete follows: ",this);
                //WriteFillToLog("Admin Fill Delete Reject",eventArgs.Fill);
            }
        }
        //

        //
        //
        // *********************************************************************************
        // ****                             Other events from TT                        ****
        // *********************************************************************************
        private List<FeedConnectionKey> m_DownloadFeeds = new List<FeedConnectionKey>();        // feeds currently loading.
        private List<FillEventArgs> m_HistoricFills = new List<FillEventArgs>();
        public bool IsFillDownloadComplete
        {
            get { return (m_FillDownloadsInProgress == 0); }
        }
        private int m_FillDownloadsInProgress = 0; 
        //
        /// <summary>
        /// Each fill feed will fire this event before it starts sending its FillBookDownLoad event, 
        /// and then trigger its End event afterword.  Also, they seem to fire even when there are no fills.
        /// Thus, I keep track of them and check them off.
        /// </summary>        
        private void TT_FillListStart(object sender, TradingTechnologies.TTAPI.FillListEventArgs eventArgs)
        {
            if (!m_DownloadFeeds.Contains(eventArgs.FeedConnectionKey))
            {
                m_FillDownloadsInProgress++;
                if (Log != null)
                    Log.NewEntry(LogLevel.Minor, "FillListener.FillListStart: {0} historic download started. {1}",Name, eventArgs.FeedConnectionKey);                
                m_DownloadFeeds.Add(eventArgs.FeedConnectionKey);
                if (IsFillDownloadComplete)
                    OnStatusChanged(new StatusChangedEventArgs(Status.DownLoadingStarted));
            }
        }
        private void TT_FillListDownLoad(object sender, TradingTechnologies.TTAPI.FillBookDownloadEventArgs eventArgs)
        {
            bool isWriteLog = false;
            if (Log != null)
                isWriteLog = Log.BeginEntry(LogLevel.Minor, "FillListener.TT_FillDownLoad: {0} {1} fills from {2}: ", Name, eventArgs.Fills.Count.ToString(), System.Threading.Thread.CurrentThread.Name);
            foreach (TradingTechnologies.TTAPI.Fill fill in eventArgs.Fills)
            {
                if (m_LogAllFillPropertiesFromTT)
                    WriteFillToLog(FillType.Historic.ToString(), fill);
                FillEventArgs e = CreateFillEventArg(FillType.Historic, this.Log, fill);
                if (isWriteLog)
                    Log.AppendEntry("[{0} {1} {2}]",e.TTInstrumentKey, e.Fill, e.FillKey);
                m_HistoricFills.Add(e);                             // alt approach: StatusChanged is triggered at end of download, HistoricFills passed to subscribers
                OnHistoricFill(e);                                  // otherwise, subscribers to this event can collect each historic fill.
                

            }
            if (Log != null)
                Log.EndEntry();
        }
        private void TT_FillListEnd(object sender, TradingTechnologies.TTAPI.FillListEventArgs eventArgs)
        {            
            if (m_DownloadFeeds.Contains(eventArgs.FeedConnectionKey))
            {
                m_DownloadFeeds.Remove(eventArgs.FeedConnectionKey);
                m_FillDownloadsInProgress--;
                if (Log != null)
                    Log.NewEntry(LogLevel.Minor, "FillListener.FillListEnd: {0} {1}. Historic download complete. Remaining downloads {2}", Name, eventArgs.FeedConnectionKey, m_FillDownloadsInProgress);

                if (IsFillDownloadComplete)
                {
                    StatusChangedEventArgs newEventArg = new StatusChangedEventArgs(Status.DownLoadsCompleted);
                    List<FillEventArgs> fillList = new List<FillEventArgs>(m_HistoricFills.Count);
                    fillList.AddRange(m_HistoricFills);
                    m_HistoricFills.Clear();                                            // clear our copy of fills
                    newEventArg.Data = fillList;
                    OnStatusChanged(newEventArg);
                }
            }
        }
        //
        private void TT_Rollover(object sender, TradingTechnologies.TTAPI.RolloverEventArgs eventArgs)
        {
            int sessionID = eventArgs.SessionId;
            FeedConnectionKey feedKey = eventArgs.FeedConnectionKey;

            if (Log != null && Log.BeginEntry(LogLevel.Major, "FillListener: {0} TT Rolloever ", Name))
            {
                Log.AppendEntry("Feed={0} ", feedKey.ToString());                
                foreach (Fill f in eventArgs.Fills)
                    Log.AppendEntry("[{0} {1} {2}@{3}]", f.InstrumentKey, f.BuySell, f.Quantity.ToInt(), f.MatchPrice.ToDouble());
                Log.EndEntry();
            }
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
        //
        // *****************************************************************
        // ****                         Filled                          ****
        // *****************************************************************
        public event EventHandler Filled;                               // Fired for each fill received (new or old).
        //
        //
        protected void OnFilled(FillEventArgs eventArgs)
        {
            Log.NewEntry(LogLevel.Major, "FillListener: {0}", eventArgs);
            if (Filled != null)
            {                
                Filled(this, eventArgs);
            }
        }// OnFilled()
        //
        // *****************************************************************
        // ****                    HistoricFill                         ****
        // *****************************************************************
        public event EventHandler HistoricFill;                               // Fired for each fill received (new or old).
        //
        //
        protected void OnHistoricFill(FillEventArgs eventArgs)
        {
            if (HistoricFill != null)
                HistoricFill(this, eventArgs);
        }// OnFilled()
        //
        //

        //
        //
        //
        //
        // *****************************************************************
        // ****                                                         ****
        // *****************************************************************
        //
        public event EventHandler StatusChanged;                            // market closings etc.
        //
        protected void OnStatusChanged(StatusChangedEventArgs e)
        {
            if (StatusChanged != null)
                StatusChanged(this,e);
        }
        //
        //
        //
        public class StatusChangedEventArgs : EventArgs
        {
            public Status NewStatus;
            public object Data = null;                                      // optional data argument
            public StatusChangedEventArgs(Status myStatus)
            {
                this.NewStatus = myStatus;
            }
            public override string ToString()
            {
                return this.NewStatus.ToString();
            }
        }
        //
        public enum Status
        {
            None = 0
            , DownLoadingStarted
            , DownLoadsCompleted
        }
        //
        //
        #endregion//My Events


        #region IStringifiable
        public string GetAttributes()
        {
            StringBuilder msg = new StringBuilder();
            msg.AppendFormat("Name={0}",Name);
            // Trade filter controls
            if (m_TradeFilter != null)
            {
                if (this.m_TradeFilter.GetType() == typeof(TradeSubscriptionAccountFilter))
                    msg.AppendFormat(" FilterAccount={0}", m_TradeFilterArg);
                else if (this.m_TradeFilter.GetType() == typeof(TradeSubscriptionInstrumentFilter))
                    msg.AppendFormat(" FilterInstrumentKey={0}", m_TradeFilterArg);
            }
            // Log Flags
            msg.AppendFormat(" LogAllFills={0}", m_LogAllFillPropertiesFromTT);

            return msg.ToString();
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            InstrumentKey instrumentKey;
            bool boolValue = false;
            foreach (string key in attributes.Keys)
            {
                if (key.Equals("Name"))
                    Name = attributes[key];
                else if (key.Equals("FilterInstrumentKey") && TTConvert.TryCreateInstrumentKey(attributes[key], out instrumentKey))
                    this.FilterByInstrument(instrumentKey);
                else if (key.Equals("FilterAccount"))
                    this.FilterByAccount(attributes[key]);
                else if (key.Equals("LogAllFills") && bool.TryParse(attributes[key], out boolValue))
                    this.m_LogAllFillPropertiesFromTT = boolValue;
            }
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }

        public void AddSubElement(IStringifiable subElement)
        {
            
        }
        #endregion // IStringifiable



    }
}

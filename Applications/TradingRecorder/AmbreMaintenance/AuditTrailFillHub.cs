using System;
using System.Collections.Generic;

namespace AmbreMaintenance
{
    using Ambre.TTServices.Fills;
    using Ambre.TTServices.Markets;

    using Misty.Lib.Hubs;

    using TradingTechnologies.TTAPI;

    public class AuditTrailFillHub : FillHub
    {
        #region Members
        private List<InstrumentKey> m_NeededCheckKeys = null;
        public event EventHandler BooksCreated;
        #endregion

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public AuditTrailFillHub(string name, bool isLogViewVisible)
            : base(name, isLogViewVisible)
        {
            m_NeededCheckKeys = new List<InstrumentKey>();
        }
        #endregion//Constructors


        #region Properties
        // Need waiting list to operate.
        public Dictionary<TradingTechnologies.TTAPI.InstrumentKey, SortedList<DateTime, FillEventArgs>> InitialWaitDictionary
        {
            get { return m_FillsWaitList; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// In the time to not connect to TT, the developer may choose this method to create books.
        /// </summary>
        public void CreateBooksStatic(MarketTTAPI marketTTAPI)
        {
            // Create books for all the instruments without connecting to TT and using start method.
            MarketHub = marketTTAPI;

            foreach (BookLifo book in m_FillBooks.Values)
            {
                PositionBookChangedEventArgs eventArgs = new PositionBookChangedEventArgs();
                eventArgs.Instrument = book.Name;
                eventArgs.Sender = this;
                OnPositionBookCreated(eventArgs);
                MarketHub.RequestInstruments(m_BookNameMap[book.Name].Key);
            }
        }

        /// <summary>
        /// This function checks the existence of a fill by exchange time. Only used by audit trail fill hub.
        /// </summary>
        /// <param name="exchangeTime"></param>
        /// <returns></returns>
        public bool TryCheckFillByExchangeTime(InstrumentKey ttKey, DateTime exchangeTime)
        {
            if (!m_FillBooks.ContainsKey(ttKey))
                return false;
            else
            {
                BookLifo book = m_FillBooks[ttKey];
                bool status = book.IsFillExistByExchangeTime(exchangeTime);
                return status;
            }
        }

        /// <summary>
        /// At the end when the program shut down, it should substract these event handlers.
        /// </summary>
        public void RequestSubstractEventHandler()
        {
            if (MarketHub != null)
            {
                MarketHub.FoundResource -= new EventHandler(this.HubEventEnqueue);
                MarketHub.MarketStatusChanged -= new EventHandler(this.HubEventEnqueue);
            }
        }
        #endregion

        #region Hub Event Handler and Processing
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        // ****                 Hub Event Handler()                     ****
        /// <summary>
        /// Called by this hub thread.
        /// </summary>
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            base.HubEventHandler(eventArgList);
            if (eventArgList == null)
                return;

            // This overriding method only contains new methods related audit trail events.
            foreach (EventArgs eventArg in eventArgList)
            {
                if (eventArg == null) continue;
                Type eventType = eventArg.GetType();
                try
                {
                    if (eventType == typeof(AuditTrailEventArgs))
                        ProcessAuditTrailEvent((AuditTrailEventArgs)eventArg);
                }
                catch (Exception ex)
                {
                    this.Log.Flush();
                    System.Windows.Forms.DialogResult result = Misty.Lib.Application.ExceptionCatcher.QueryUserTakeAction(ex, this, eventArg);
                }
            }
        }//HubEventHandler()
        //
        //
        //
        // *************************************************************************
        // ***                      Process Request                             ****
        // *************************************************************************
        /// <summary>
        /// These are request for fill hub resources.  Some can not be completed yet, and these
        /// will be stored in a Queue for retrying later.
        /// </summary>
        private void ProcessAuditTrailEvent(AuditTrailEventArgs eventArg)
        {
            if (eventArg == null)
                return;

            if (eventArg.auditTrailEventType == AuditTrailEventType.LoadAuditTrailFills)
            {
                if (m_Listener != null)
                {
                    // It does not need the TT to start fill listening.
                    m_IsInitializingBooks = false;
                    Log.NewEntry(LogLevel.Minor, "Connected to audit trail file.");
                    m_Listener.Filled += new EventHandler(HubEventEnqueue);
                    AuditTrailPlayer auditTrailReader = (AuditTrailPlayer)eventArg.Data[0];
                    AuditTrailFillHub auditTrailFillHub = (AuditTrailFillHub)eventArg.Data[1];
                    DateTime auditTrailReadingStartDateTime = (DateTime)eventArg.Data[2];
                    DateTime auditTrailPlayingEndDateTime = (DateTime)eventArg.Data[3];
                    LogHub log = (LogHub)eventArg.Data[4];
                    m_Listener.Log = log;

                    // Get the variables from GUI thread to load audit trail fills and update initial state of fill hub.
                    if (auditTrailReader.TryReadAuditTrailFills(auditTrailReadingStartDateTime, auditTrailPlayingEndDateTime, auditTrailFillHub))
                    {
                        m_NeededCheckKeys.Clear();
                        m_NeededCheckKeys.AddRange(auditTrailReader.m_NeededBookInstrumentList);
                        Log.NewEntry(LogLevel.Minor, "Successfully load fills from audit trail file.");
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Major, "Failed to load fills from audit trail file.");
                        return;
                    }
                }
            }
            else if (eventArg.auditTrailEventType == AuditTrailEventType.PlayAuditTrailFills)
            {
                AuditTrailPlayer auditTrailReader = (AuditTrailPlayer)eventArg.Data[0];
                AuditTrailFillHub auditTrailFillHub = (AuditTrailFillHub)eventArg.Data[1];
                DateTime auditTrailReadingStartDateTime = (DateTime)eventArg.Data[2];
                DateTime auditTrailPlayingEndDateTime = (DateTime)eventArg.Data[3];
                LogHub log = (LogHub)eventArg.Data[4];

                // Get the variables from GUI thread to play the audit trail fills.
                if (auditTrailReader.TryPlayAuditTrailFillsForFillHub(auditTrailFillHub, auditTrailReadingStartDateTime, auditTrailPlayingEndDateTime, out auditTrailFillHub))
                {
                    Log.NewEntry(LogLevel.Minor, "Successful in playing the audit trail file.");
                }
                else
                {
                    Log.NewEntry(LogLevel.Major, "Failed to play the audit trail file.");
                    return;
                }
            }
        }// ProcessRequest()
        #endregion

        #region Private Methods
        /// <summary>
        /// In this block, it should check whether all the books are created for the new ones.
        /// </summary>
        protected override void UpdatePeriodic()
        {
            base.UpdatePeriodic();
            if (m_NeededCheckKeys.Count == 0)
            {
                if (BooksCreated != null)
                    BooksCreated(this, EventArgs.Empty);
            }
            if (!m_IsInitializingBooks && m_NeededCheckKeys.Count > 0)
            {
                int ptr = m_NeededCheckKeys.Count - 1;
                while (ptr >= 0)
                {
                    if (TryCheckExistenceOfInstrumentKey(m_NeededCheckKeys[ptr]))
                        m_NeededCheckKeys.RemoveAt(ptr);
                    ptr--;
                }
                if (m_NeededCheckKeys.Count == 0)
                {
                    m_IsInitializingBooks = true;
                    if (BooksCreated != null)
                        BooksCreated(this, EventArgs.Empty);
                }
            }

            // To here, we shoud have finished creating the books. This block is to wait for creating books.
            m_IsInitializingBooks = false;
        }
        #endregion
    }
}

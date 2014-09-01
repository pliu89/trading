using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Breconcile.Reconciler
{
    using Misty.Lib.Application;
    using Misty.Lib.Hubs;

    /// <summary>
    /// This is now defunct. Use ReconcilerTaskHub class.
    /// </summary>
    public class ReconcilerHub : Hub
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Services
        //
        private AppInfo m_AppInfo = null;
        private Ftp.SimpleFtpReader m_FtpReader = null;
        private bool m_IsShuttingDown = false;


        // Working requests
        private List<RequestEventArg> m_WorkingRequests = new List<RequestEventArg>();
        private System.Timers.Timer m_Timer = null;


        // Directory names
        private readonly string m_StatementPath;
        private readonly string m_DropPath;

        // Report parameters
        private int[] Col_Width = new int[]{32,48,64};

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public ReconcilerHub()
            : base("Reconciler", AppInfo.GetInstance().LogPath, true, LogLevel.ShowAllMessages)
        {
            m_AppInfo = AppInfo.GetInstance();
            m_FtpReader = new Ftp.SimpleFtpReader(this.Log);

            // Create names
            m_StatementPath = string.Format("{0}{1}", m_AppInfo.UserPath, "Statements\\");
            //m_DropPath = string.Format("{0}{1}", m_AppInfo.UserPath, "Drops\\");
            m_DropPath = string.Format("{0}{1}", m_AppInfo.UserPath, "Seth\\Drops\\");


            // Task-manager variables.
            double minutesInterval = 1;
            m_Timer = new System.Timers.Timer(1000.0 * 60.0 * minutesInterval);
            m_Timer.AutoReset = false;
            m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            
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


        
        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public bool Request(RequestEventArg newRequest)
        {
            return this.HubEventEnqueue(newRequest);
        }
        //
        //
        public override void Start()
        {
            m_Timer.Start();
            base.Start();
        }
        public override void RequestStop()
        {
            this.HubEventEnqueue(new RequestEventArg(RequestType.Stop));
        }        
        //
        //
        //
        //
        //
        #endregion//Public Methods



        #region HubEventHandler & Processing Methods
        // *****************************************************
        // ****         HubEventHandler                     ****
        // *****************************************************
        /// <summary>
        /// The main event handler, called by the hub thread.
        /// </summary>
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            foreach (EventArgs eventArg in eventArgList)
            {
                Type eventType = eventArg.GetType();
                if (eventType == typeof(RequestEventArg))
                    ProcessRequest((RequestEventArg)eventArg);
                else
                    Log.NewEntry(LogLevel.Warning, "HubEventHandler: Event type {0} not implemented.", eventArg);
            }
        }// HubEventHandler()
        //
        //
        // *****************************************************
        // ****             ProcessRequest()                ****
        // *****************************************************
        /// <summary>
        /// Processes job requests from user.
        /// </summary>
        private void ProcessRequest(RequestEventArg eventArg)
        {
            bool triggerRequestCompletedEvent = false;
            
            //
            // Multiple Sequential Requests
            //
            if (eventArg.Type == RequestType.MultipleSequentialRequests)        // this contains one or more child tasks to complete.
            {
                RequestEventArg currentWorkingRequest = null;                   // the child-request that we are currently working on.
                if (eventArg.TryGetNextUnsuccessfulChild(out currentWorkingRequest))
                {
                    eventArg.Status = RequestStatus.ContinueWorking;            // set parent as "working"
                    if (currentWorkingRequest.Status == RequestStatus.Success)  // Usually, the "current" child is not finished successfully, 
                    {                                                           // but if its the last child, then it signals the end of the task.
                        eventArg.Status = RequestStatus.Success;                // Mark parent for success...
                        triggerRequestCompletedEvent = true;
                    }
                    else if (currentWorkingRequest.Status == RequestStatus.Failed)  // the last non-successful child says its failed...
                    {
                        eventArg.Status = RequestStatus.Failed;                     // done. Failed request
                        triggerRequestCompletedEvent = true;
                    }
                    else
                    {
                        Log.BeginEntry(LogLevel.Minor, "ProcessRequest: MultipleSequentialRequests {0}", eventArg);
                        Log.AppendEntry(" ----> {0}",currentWorkingRequest);        // output the current child to work.
                        Log.EndEntry();

                        ProcessRequest(currentWorkingRequest);                      // continue trying to work this task.
                        
                        // TODO: fix this.  If the currentWorkingREquest is complete, we might consider processing the
                        // next request immediately, or not based on its allowed starting time.
                        if (!m_WorkingRequests.Contains(eventArg))                  // make sure that we will revist this parent event again...
                        {
                            Log.NewEntry(LogLevel.Minor, "ProcessRequest: MultipleSequentialRequests. Adding to waiting queue.");
                            m_WorkingRequests.Add(eventArg);                       
                        }
                        else
                            Log.NewEntry(LogLevel.Minor, "ProcessRequest: MultipleSequentialRequests. Already in waiting queue.");
                    }
                }
            }
            //
            // Monitor Copy New Files
            //
            else if (eventArg.Type == RequestType.MonitorCopyNewFiles)
            {
                Log.BeginEntry(LogLevel.Minor, "ProcessRequest: {0}", eventArg);
                List<string> localFileNamesCopied;                              // names of files discovered on FTP site.
                if (m_FtpReader.TryCopyNewRemoteFilesToLocal(m_StatementPath, out localFileNamesCopied))
                {   // Success making FTP connection!
                    if ( localFileNamesCopied.Count > 0 )                       // we consider a success that the files we waiting for are there now.
                    {
                        eventArg.Status = RequestStatus.Success;                // update request state
                        eventArg.Data = new List<object>(localFileNamesCopied); // update data
                        RequestEventArg parentRequest;
                        if (eventArg.TryGetParent(out parentRequest))           
                            this.HubEventEnqueue(parentRequest);                // strobe the parent to attempt next task...
                        else
                            triggerRequestCompletedEvent = true;                       // otherwise, we are done, trigger event.
                    }
                    else
                    {   // There were no new files yet.
                        // Test that we dont already have a latest file... this would happen only if we ran
                        // the code twice in one day... perform some check here.
                        if ( DateTime.Now.CompareTo(eventArg.GiveUpTime) > 0 )
                        {
                            eventArg.Status = RequestStatus.Failed;
                            RequestEventArg parentRequest;
                            if (eventArg.TryGetParent(out parentRequest))           
                                this.HubEventEnqueue(parentRequest);                // strobe the parent to attempt next task...
                            else
                                triggerRequestCompletedEvent = true;                       // otherwise, we are done, trigger event.
                        }
                        else 
                        {
                            eventArg.Status = RequestStatus.ContinueWorking;        // We will try this again.
                            if (! eventArg.IsChild && ! m_WorkingRequests.Contains(eventArg) )
                                m_WorkingRequests.Add(eventArg);
                        }
                    }// if there are new files                                        
                    
                }
                else
                {   // Failed to event connect properly to FTP
                    Log.AppendEntry(" FTP Connection failed.");
                    if (DateTime.Now.CompareTo(eventArg.GiveUpTime) > 0)
                    {
                        eventArg.Status = RequestStatus.Failed;
                        RequestEventArg parentRequest;
                        if (eventArg.TryGetParent(out parentRequest))
                            this.HubEventEnqueue(parentRequest);                // strobe the parent to attempt next task...
                        else
                            triggerRequestCompletedEvent = true;                       // otherwise, we are done, trigger event.
                    }
                    else
                    {
                        eventArg.Status = RequestStatus.ContinueWorking;        // We will try this again.
                        if (!eventArg.IsChild && !m_WorkingRequests.Contains(eventArg))
                            m_WorkingRequests.Add(eventArg);
                    }
                }
                Log.EndEntry();
            }
            //
            // Reconcile Statement
            //
            else if (eventArg.Type == RequestType.ReconcileStatement)
            {
                bool isSuccessful = true;                                       // track our success in reconciling.


                //
                // Read statement
                //
                RCG.StatementReader statement;
                DateTime settlementDateTime;
                settlementDateTime = new DateTime(2013, 04, 8, 16, 15, 0);      // DEBUG
                TryReadRCGStatement(settlementDateTime,out statement);          // read RCG statements - date is settlement date.
                string statementFileName = statement.FilePathForPosition.Substring(statement.FilePathForPosition.LastIndexOf('_')+1); // keep everything after "_"
                int n = statementFileName.LastIndexOf('.');
                statementFileName = statementFileName.Substring(0, n);

                DateTime statementTimeStamp;
                if (!DateTime.TryParseExact(statementFileName, "yyyyMMdd", System.Globalization.DateTimeFormatInfo.CurrentInfo, System.Globalization.DateTimeStyles.None, out statementTimeStamp))
                {   // Failed to interpret date on file!
                    isSuccessful = false;                    
                }

                //
                // Read drops
                //               
                if (isSuccessful)
                {
                    StringBuilder report = new StringBuilder();                 // Full report
                    StringBuilder currentItem = new StringBuilder();
                    foreach (string acctNumber in statement.m_PortfolioPosition.Keys)      // for each account number found in the statement...
                    {                        
                        string baseFileName = string.Format("FillBooks_828{0}", acctNumber);
                        BookReaders.EventPlayer eventPlayer = new BookReaders.EventPlayer(m_DropPath, baseFileName, settlementDateTime);
                        if (eventPlayer.SeriesList.Count == 0)                  // No information about this account.
                            continue;                                           // next account

                        report.AppendFormat("Acct# {0}\n", acctNumber);          // RCG acct numbers from statement are missing first 3 chars.
                        const string fmt = "    {0,-32}{1,8}\t{2,-24}\n";                                               
                        foreach (string rcgInstrDescr in statement.m_PortfolioPosition[acctNumber].Keys)    // loop thru instrDescriptions - items in statement
                        {
                            currentItem.Clear();                    // clear this line.                            

                            Misty.Lib.Products.InstrumentName rcgInstrumentName;
                            Misty.Lib.Products.Product mistyProduct;
                            if (statement.m_InstrDescrToInstrName.TryGetValue(rcgInstrDescr, out rcgInstrumentName))
                            {
                                int ourQty = 0;                         // qty computed from drop files
                                if (statement.m_RcgToBreProduct.TryGetValue(rcgInstrumentName.Product, out mistyProduct))
                                {
                                    Misty.Lib.Products.InstrumentName mistyInstrumentName = new Misty.Lib.Products.InstrumentName(mistyProduct, rcgInstrumentName.SeriesName);
                                    BookReaders.EventSeries series;
                                    if (eventPlayer.SeriesList.TryGetValue(mistyInstrumentName, out series))
                                    {
                                        Misty.Lib.OrderHubs.Fill fill;
                                        if (series.TryGetStateAt(settlementDateTime, out fill))
                                        {   
                                            ourQty = fill.Qty;
                                            currentItem.AppendFormat(fmt, mistyInstrumentName, ourQty, fill.LocalTime);
                                        }
                                        else
                                        {

                                        }
                                        
                                        
                                    }
                                        
                                }
                                else
                                {   // Unknown product mapping between RCG and TT!
                                    currentItem.AppendFormat(fmt, "unknown", "-", "-");                                    
                                }

                                //
                                // Statement qty
                                //
                                int statementQty = 0;                   // qty from statement
                                List<Misty.Lib.OrderHubs.Fill> fills = statement.m_PortfolioPosition[acctNumber][rcgInstrDescr];
                                foreach (Misty.Lib.OrderHubs.Fill aFill in fills)
                                    statementQty += aFill.Qty;
                                currentItem.AppendFormat(fmt, rcgInstrDescr, statementQty, statementTimeStamp.ToShortDateString());
                                
                                if (statementQty != ourQty)
                                {
                                    currentItem.Remove(0, 1);
                                    currentItem.Insert(0, "*");
                                }

                            }// try get rcgInstrumentName
                            
                            report.Append(currentItem);
                        }//next rcgInstrDescr

                    }//next acctNumber
                    Log.NewEntry(LogLevel.Major, "ReconcileStatement: \r\n{0}", report.ToString()); // write report to Log file.
                }
            
                //
                // Exit
                //
                if (isSuccessful)
                    eventArg.Status = RequestStatus.Success;
                else
                    eventArg.Status = RequestStatus.Failed;

                RequestEventArg parentRequest;
                if (eventArg.TryGetParent(out parentRequest))
                    this.HubEventEnqueue(parentRequest);                        // strobe the parent to attempt next task...
                else
                    triggerRequestCompletedEvent = true;                       // otherwise, we are done, trigger event.
            }
            //
            // Debug Test
            //
            else if (eventArg.Type == RequestType.DebugTest)
            {
                Log.BeginEntry(LogLevel.Minor, "ProcessRequest: {0}", eventArg);
                if (eventArg.Data == null)
                {
                    Log.AppendEntry(" First failure.");
                    eventArg.Data = new List<object>();
                    eventArg.Data.Add(1);                   // first failure
                    eventArg.Status = RequestStatus.ContinueWorking;
                }
                else if (((int)eventArg.Data[0]) > 2)
                {   // Success
                    Log.AppendEntry(" Success!");
                    eventArg.Status = RequestStatus.Success;
                    RequestEventArg parent;
                    if (eventArg.TryGetParent(out parent))
                    {
                        this.HubEventEnqueue(parent);
                    }
                }
                else
                {   // another failure
                    int n = (int)eventArg.Data[0];
                    eventArg.Data[0] = n + 1;
                    Log.AppendEntry(" {0} failures, try again.", n);
                    eventArg.Status = RequestStatus.ContinueWorking;        // We will try this again.
                    if (!eventArg.IsChild && !m_WorkingRequests.Contains(eventArg))
                        m_WorkingRequests.Add(eventArg);
                }
                Log.EndEntry();
            }
            else if (eventArg.Type == RequestType.Stop)
            {
                Shutdown();
                base.Stop();
            }
            else
            {
                Log.BeginEntry(LogLevel.Minor, "ProcessRequest: {0}", eventArg);
                Log.AppendEntry(" request type not implemented.");
                Log.EndEntry();
            }
            
            // Exit
            if (triggerRequestCompletedEvent)
                OnRequestCompleted(eventArg);
        }//ProcessRequest()
        //
        //
        //
        //
        #endregion//Public Methods



        #region Private Task Working Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //  
        //
        //
        //
        //
        private void TryReadRCGStatement(DateTime date, out RCG.StatementReader reader)
        {
            reader = null;
            List<string> fileNames = new List<string>(System.IO.Directory.GetFiles(m_StatementPath));
            string fileName;
            int n = 0;
            while (n < fileNames.Count)                                 // loop thru each file we find...
            {
                bool isKeeper = false;                                  // keep the ones that match the desired patterns.
                fileName = fileNames[n].Substring(fileNames[n].LastIndexOf('\\') + 1);
                string strPattern = m_FtpReader.m_FilePatterns[0];      // keep only first file pattern right now.
                if (fileName.Contains(strPattern))
                    isKeeper = true;
                if (isKeeper)
                    n++;
                else
                    fileNames.RemoveAt(n);
            }
            fileNames.Sort();                                           // Sort by dates encoded in file names

            string desiredFile = string.Empty;
            int ptr = fileNames.Count - 1;                              // point to last filename (usually we want the last).
            while (string.IsNullOrEmpty(desiredFile) && ptr >= 0)
            {
                // Extract date stamp
                n = fileNames[ptr].LastIndexOf('_');
                fileName = fileNames[ptr].Substring(n + 1);
                fileName = fileName.Substring(0, fileName.LastIndexOf('.'));
                DateTime fileDate;
                if (DateTime.TryParseExact(fileName, "yyyyMMdd", System.Globalization.DateTimeFormatInfo.CurrentInfo, System.Globalization.DateTimeStyles.None, out fileDate))
                {   // extracted the date from file name.
                    if (fileDate.CompareTo(date.Date) == 0)
                        desiredFile = fileNames[ptr];
                    else if (fileDate.CompareTo(date.Date) < 0)         // the user's date is already passed.
                    {
                        desiredFile = fileNames[ptr];
                    }
                }
                else
                {   // failed to extract date time.
                    Log.AppendEntry(" Failed to extract date from Statement {0}.", fileNames[ptr]);
                }
                ptr--;                                                  // decrement the counter
            }// wend
            
            // Read statement
            reader = new RCG.StatementReader(desiredFile);

        }// ReadStatement()
        //
        //
        #endregion // Task-working methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //  
        //
        //
        //
        //
        // ****                     Shutdown()                          *****
        private void Shutdown()
        {
            if (!m_IsShuttingDown)
            {
                m_IsShuttingDown = true;
                if (m_Timer != null)
                {
                    m_Timer.Stop();
                    m_Timer.Dispose();
                    m_Timer = null;
                }
            }
        }//Shutdown()
        //
        //
        //
        #endregion//Private Methods


        #region External Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        /// <summary>
        /// This event handler is called by a pool thread; keep it threadsafe!
        /// </summary>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs eventArgs)
        {
            Log.NewEntry(LogLevel.Minor, "Timer_Elapsed: Tick. ");
            if (m_IsShuttingDown)
                return;
            m_Timer.Enabled = false;
            // Check for working requests to resubmit.
            List<EventArgs> requestsToResubmit = null;
            lock (m_WorkingRequests)
            {
                if (m_WorkingRequests.Count > 0)
                {
                    Log.NewEntry(LogLevel.Minor, "Timer_Elapsed: Working request queue contains {0} waiting requests.  Will resubmit them.",m_WorkingRequests.Count);
                    requestsToResubmit = new List<EventArgs>();
                }
                while (m_WorkingRequests.Count > 0)
                {
                    requestsToResubmit.Add(m_WorkingRequests[0]);
                    m_WorkingRequests.RemoveAt(0);
                }
            }
            if (requestsToResubmit != null)
                this.HubEventEnqueue(requestsToResubmit);

            m_Timer.Enabled = true;
        }// Timer_Elapsed()
        //
        //
        #endregion//External event handlers


        #region Events
        // *****************************************************************
        // ****                         Events                          ****
        // *****************************************************************
        //
        public event EventHandler RequestCompleted;
        //
        //
        private void OnRequestCompleted(RequestEventArg completedRequest)
        {
            Log.NewEntry(LogLevel.Minor, "OnRequestCompleted: {0}", completedRequest);

            if (m_WorkingRequests.Contains(completedRequest))
            {
                Log.NewEntry(LogLevel.Minor, "OnRequestCompleted: Removing from Working queue/", completedRequest);
                m_WorkingRequests.Remove(completedRequest);
            }

            if (RequestCompleted != null)
                RequestCompleted(this,completedRequest);
        }
        //
        #endregion//Event Handlers

    }
}

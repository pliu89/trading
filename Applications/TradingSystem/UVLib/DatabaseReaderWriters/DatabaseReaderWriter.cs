using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters
{
    using MySql.Data.MySqlClient;

    using UV.Lib.IO.Xml;
    using UV.Lib.Application;
    using UV.Lib.Hubs;

    using System.Threading;

    /// <summary>
    /// This is a hub that provides on-demand services to read/write to database.
    /// </summary>
    public class DatabaseReaderWriter : Hub, IService, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private AppServices m_Services = null;
        private ServiceStates m_ServiceState = ServiceStates.Unstarted;

        //
        // Database write controls
        //
        private int WritePeriodSecs = 60;				        // Wait time (in secs) between writes to db.
        private DatabaseInfo m_Database = DatabaseInfo.Create(DatabaseInfo.DatabaseLocation.custom); // default to custom 
        //private MySqlConnection m_MySqlBarTableConnection = null;

        //
        // Internal variables
        //
        private string m_ErrorFileName = string.Empty;          // unique filename user for failed queries.

        // Sync request
        private ConcurrentQueue<EventWaitHandle> m_WaitHandleFactory = new ConcurrentQueue<EventWaitHandle>();
        private ConcurrentDictionary<int, EventWaitHandle> m_WaitHandles = new ConcurrentDictionary<int, EventWaitHandle>();

        //
        // Q's and workspace
        //
        private ConcurrentQueue<Queries.QueryBase> m_WriteQueryQueue = new ConcurrentQueue<Queries.QueryBase>(); // place to aggregate queries.

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        /// <summary>
        /// Simplified auto-starting constructor used by applications that just want
        /// to create a single DatabaseReaderWriter and use it.  No need to call "Start()"
        /// </summary>
        /// <param name="dbInfo"></param>
        public DatabaseReaderWriter(DatabaseInfo dbInfo)
            : this()           // call basic constructor too
        {
            m_Database = dbInfo;
            Start();
        }//Constructor
        //
        //
        /// <summary>
        /// IStringifiable constructor.  After construction, this object will 
        /// remain unstarted until Start() is called explicity.  Before this, m_Database MUST be set.
        /// </summary>
        public DatabaseReaderWriter()
            : base("DatabaseReaderWriter", UV.Lib.Application.AppInfo.GetInstance().LogPath, false, LogLevel.ShowAllMessages)
        {
            m_Services = AppServices.GetInstance();

        }
        //
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
        public override void Start()
        {
            // Validate that we can start.
            if (m_Database == null)
            {
                Log.NewEntry(LogLevel.Error, "Start: No database info provided.  Can't start. Requesting shutdown");
                m_Services.Shutdown();
                return;
            }
            if (m_ServiceState != ServiceStates.Unstarted)
            {
                Log.NewEntry(LogLevel.Major, "Start: Already starting.  Ignoring.");
                return;
            }

            // 
            // Start up now
            //
            m_ServiceState = ServiceStates.Started;                     // immediately set this to avoid double-starting.
            base.m_WaitListenUpdatePeriod = WritePeriodSecs * 1000;		// convert to miliseconds.

            // Create unique ErrorFileName - will contain chronic failure queries.
            //  Name is created here, but will only be created when there is a problem.
            //  This is problematic, since multiple hub could create such files later, which 
            //  can mess up the uniqueness of the names of these files.
            StringBuilder s = new StringBuilder();
            s.AppendFormat("Log_{0}_{1}_Error", Log.m_StartTime.ToString("HHmm"), base.m_HubName);
            s.Insert(0, m_Services.Info.LogPath);
            m_ErrorFileName = string.Format("{0}.txt", s.ToString());
            int dupeFileID = 0;
            while (System.IO.File.Exists(m_ErrorFileName))		// Make sure the file name is unique.
            {
                m_ErrorFileName = string.Format("{0}_{1}.txt", s.ToString(), dupeFileID.ToString());
                dupeFileID++;							// increase index until we find a non-used file name.
            }

            // Start thread now.  He will move us to Running state                                                                      
            base.Start();
            return;
        }// Start()
        //
        //
        // *****************************************
        // ****         SubmitAsync()           ****
        // *****************************************
        /// <summary>
        /// This is the asynchronous query.  The requesting thread is
        /// immediately released after pushing the query request onto the 
        /// hub queue, and call back is made with a QueryResponse event.
        /// The results as placed withing the query object, so the calling 
        /// thread should not keep it.
        /// </summary>
        /// <param name="request"></param>
        public void SubmitAsync(Queries.QueryBase request)
        {
            if (!((int)m_ServiceState >= (int)ServiceStates.Stopping))
            {   // Regardless of the type of query (read or write), the request
                // is simply pushed onto the queue.
                this.HubEventEnqueue((EventArgs)request);
            }
            else
            {
                request.Status = Queries.QueryStatus.Failed;
                request.ErrorMessage = "DatabaseReaderWriter shutting down.";
                OnQueryResponse((EventArgs)request);
            }
        }//SubmitAsync()
        //
        //
        //
        //
        // *****************************************
        // ****         SubmitSync()           ****
        // *****************************************
        /// <summary>
        /// Here, the query is made synchronously and the requesting
        /// thread waits until the query is completed, the results are
        /// added to the query and returned to the calling thread.
        /// No event is triggered in this case.
        /// 
        /// NOTE: This only works correctly with read queries. Write queries are always queued.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public bool SubmitSync(Queries.QueryBase request)
        {
            bool isSuccessful = false;
            if ((int)m_ServiceState >= (int)ServiceStates.Stopping)
            {
                request.Status = Queries.QueryStatus.Failed;
                request.ErrorMessage = "DatabaseReaderWriter shutting down.";
                return false;
            }
            // 
            if (request.IsRead)
            {
                // This request is a query, which will have data returned to it.
                // All such requests will be done be the main thread for this hub. 
                // (This keeps all query work spaces lock free for the main thread, but the 
                // caller will need to wait a bit more.)
                // Now, create a waithandle to block the calling thread until data is ready.
                EventWaitHandle wh = null;
                if (!m_WaitHandleFactory.TryDequeue(out wh))
                    wh = new EventWaitHandle(false, EventResetMode.AutoReset);
                m_WaitHandles.TryAdd(request.QueryID, wh);              // leave my phone number here, with the query I am waiting for.

                this.HubEventEnqueue((EventArgs)request);               // submit request

                wh.WaitOne();                                           // wait for callback here.

                EventWaitHandle wh2;                                    // Try to recycle my wait handle.
                if (m_WaitHandles.TryRemove(request.QueryID, out wh2))  // They can been removed by other threads in bad cases; e.g., ShutdownNow()
                    m_WaitHandleFactory.Enqueue(wh2);                   // Usually, we get our own wait handle (same as wh above), except during shutdowns, etc.
                isSuccessful = (request.Status != Queries.QueryStatus.Failed);
            }
            else
            {   // This is a write, which the thread can do for himself, if he likes.

            }
            // Exit
            return isSuccessful;
        }//SubmitAsync()
        //
        //
        //
        // *****************************************
        // ****         Request Stop()          ****
        // *****************************************
        public override void RequestStop()
        {
            this.HubEventEnqueue(new RequestEventArgs(RequestCode.BeginShutdown));

        }// RequestStop()
        //
        //
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Hub Event Handler Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        // *************************************************
        // ****             HubEvent Handler()          ****
        // *************************************************
        /// <summary>
        /// This is the main processing routine.
        /// </summary>
        /// <param name="eventArgsList"></param>
        protected override void HubEventHandler(EventArgs[] eventArgsList)
        {
            if (m_ServiceState == ServiceStates.Unstarted || m_ServiceState == ServiceStates.Started)
            {
                m_ServiceState = ServiceStates.Running;
                OnServiceStateChanged(ServiceStates.Unstarted, ServiceStates.Started);
                OnServiceStateChanged(ServiceStates.Started, ServiceStates.Running);
            }
            //
            //
            //
            foreach (EventArgs eventArg in eventArgsList)
            {
                if (eventArg == null || eventArg == EventArgs.Empty)
                    continue;
                if (eventArg is Queries.QueryBase)
                {
                    Queries.QueryBase query = (Queries.QueryBase)eventArg;
                    if (query.QueryID == 6)
                    { }
                    if (query.Status == Queries.QueryStatus.New)
                    {   // This is a new query request, for either read or write.
                        // We will process this query here.
                        if (query.IsRead)
                        {   // If the user has requested a "query" then he probably 
                            // expects to receive an answer, and so this we try to carry out immediately.
                            ProcessReadQuery(query);
                        }
                        else
                        {   // User wants to write data.
                            m_WriteQueryQueue.Enqueue(query);
                        }
                    }
                    else
                    {   // This is a query that we have already processed.
                        // It has been pushed back onto the queue to be returned to the caller.
                        EventWaitHandle wh = null;
                        if (m_WaitHandles.TryRemove(query.QueryID, out wh))
                        {   // If this queryID has a wait handle, then the creator has made a synchronized
                            // call and is waiting for the response at the wait handle.
                            wh.Set();                           // Wake the caller now (he has a copy of the query).
                            m_WaitHandleFactory.Enqueue(wh);    // No need to trigger an event in this case, recycle the wait handle.
                        }
                        else
                            OnQueryResponse((EventArgs)query);  // Here, the caller has made async call.  So we must trigger event for him.

                    }
                }
                else if (eventArg.GetType() == typeof(RequestEventArgs))
                {
                    RequestEventArgs request = (RequestEventArgs)eventArg;
                    if (request.Request == RequestCode.BeginShutdown)
                    {
                        OnServiceStateChanged(m_ServiceState, ServiceStates.Stopping);
                        m_ServiceState = ServiceStates.Stopping;

                        // Purge all write queues now.

                        // If all things are completed, then shutdown completely now.
                        // Otherwise, we will have to check after each periodic update to see when all work 
                        // has been completed.
                        bool isReadyToStop = true;
                        if (isReadyToStop)
                        {
                            m_ServiceState = ServiceStates.Stopped;
                            OnServiceStateChanged(ServiceStates.Stopping, ServiceStates.Stopped);
                            base.Stop();
                        }
                    }
                }


            }
        }//HubEventHandler()
        //
        //
        // *************************************************
        // ****         ProcessQuery(iQuery)            ****
        // *************************************************
        private void ProcessReadQuery(Queries.QueryBase query)
        {
            MySqlConnection mySqlConnection = null;
            // Try to connect
            if (!m_Database.IsTryToConnect(ref mySqlConnection))
            {   // Failed to connect.
                Log.NewEntry(LogLevel.Major, "ProcessQuery: Failed to connect to {0}.", m_Database);
                query.Status = Queries.QueryStatus.Failed;
                OnQueryResponse((EventArgs)query);
            }

            //
            MySqlDataReader reader = null;
            try
            {
                string cmdString = query.GetQuery(m_Database);
                Log.NewEntry(LogLevel.Minor, "ProcessQuery: {0}", cmdString);
                if (string.IsNullOrEmpty(cmdString))
                {
                    Log.NewEntry(LogLevel.Major, "ProcessQuery: Query provided is empty.");
                    query.Status = Queries.QueryStatus.Failed;
                    OnQueryResponse((EventArgs)query);
                }
                MySqlCommand cmd = new MySqlCommand(cmdString, mySqlConnection);

                reader = cmd.ExecuteReader();
                int fieldCount = reader.FieldCount;
                List<string> fieldNames = new List<string>();       // place to store column names
                List<object> values = new List<object>();           // place to store values.  Count will be (fieldCount * nRows)
                for (int i = 0; i < fieldCount; ++i)
                    fieldNames.Add(reader.GetName(i));
                while (reader.Read())
                {   // Reading the next row

                    for (int i = 0; i < fieldCount; ++i)
                    {   // Read the ith column in this row.
                        if (reader.IsDBNull(i))
                        {
                            values.Add(null);
                            continue;
                        }
                        Type type = reader.GetFieldType(i);
                        if (type == typeof(int))
                        {
                            values.Add(reader.GetInt32(i));
                        }
                        else if (type == typeof(double))
                        {
                            values.Add(reader.GetDouble(i));
                        }
                        else
                        {
                            object o = reader.GetValue(i);
                            values.Add(o);
                        }
                    }//next ith column.
                }
                // Give the data to the query.
                query.Status = query.AcceptData(m_Database, values, fieldNames);
            }
            catch (Exception e)
            {
                Log.NewEntry(LogLevel.Major, "ProcessQuery: {1} exception: {0}", e.Message, query);
                query.ErrorMessage = e.Message;
                query.Status = Queries.QueryStatus.Failed;
            }
            if (reader != null && reader.IsClosed == false)
            {
                try
                {
                    reader.Close();
                }
                catch (Exception e)
                {
                    Log.NewEntry(LogLevel.Major, "ProcessQuery: Closing reader {1} exception: {0}", e.Message, query);
                }
            }
            if (mySqlConnection != null)
            {
                mySqlConnection.Close();
            }

            //
            // This is complete.  Push onto the queue.
            //
            this.HubEventEnqueue(query);

        }//ProcessQuery()
        //
        private void ProcessWriteQuery(Queries.QueryBase query, MySqlConnection mySqlConnection)
        {
            try
            {
                string cmdString = query.GetQuery(m_Database);
                Log.NewEntry(LogLevel.Minor, "ProcessQuery: {0}", cmdString);
                if (string.IsNullOrEmpty(cmdString))
                {
                    Log.NewEntry(LogLevel.Major, "ProcessQuery: Query provided is empty.");
                    query.Status = Queries.QueryStatus.Failed;
                    OnQueryResponse((EventArgs)query);
                }
                MySqlCommand cmd = new MySqlCommand(cmdString, mySqlConnection);
                int nRows = cmd.ExecuteNonQuery();

                query.Status = Queries.QueryStatus.Completed;
            }
            catch (Exception e)
            {
                Log.NewEntry(LogLevel.Major, "ProcessQuery: {1} exception: {0}", e.Message, query);
                query.ErrorMessage = e.Message;
                query.Status = Queries.QueryStatus.Failed;
            }
            //
            // This is complete.  Push onto the queue.
            //
            this.HubEventEnqueue(query);
        }//ProcessQuery()
        //
        //
        /// <summary>
        /// Caller would like to attemp to wite a query string to the connection provided.
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="mySqlConnection"></param>
        /// <returns></returns>
        private bool TryProcessWriteQueryString(string queryString, MySqlConnection mySqlConnection)
        {
            try
            {
                Log.NewEntry(LogLevel.Minor, "TryProcessWriteQueryString: {0}", queryString);
                if (string.IsNullOrEmpty(queryString))
                {
                    Log.NewEntry(LogLevel.Major, "TryProcessWriteQueryString: Query provided is empty.");
                    return false;
                }
                MySqlCommand cmd = new MySqlCommand(queryString, mySqlConnection);
                int nRows = cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Log.NewEntry(LogLevel.Major, "TryProcessWriteQueryString: exception: {0}", e.Message);
                return false;
            }
        }
        //
        //
        // *****************************************************
        // ****             Update Periodic()               ****
        // *****************************************************
        //
        //
        // Query Workspaces for aggregation
        private Dictionary<Type, List<Queries.QueryBase>> m_QueryTypeToQueries = new Dictionary<Type, List<Queries.QueryBase>>();
        private StringBuilder m_AggregatedQueryString = new StringBuilder();
        //
        //
        /// <summary>
        /// Called periodically to check state and if we have pending queries to write.
        /// 
        /// Note: For this to work every query object must correctly override base class methods for Suffix and Prefix
        /// </summary>
        protected override void UpdatePeriodic()
        {

            if ((int)m_ServiceState >= (int)ServiceStates.Stopping)          // Check for shutdown conditions met.
            {
                bool isReadyToShutdown = true;
                // TODO: Insert checks here
                if (isReadyToShutdown)
                    ShutdownNow();
            }

            DateTime startTimeWriteProcess = Log.GetTime();
            int nSuccessfulQueries = 0;
            //
            // Aggregate all queries into bins by type
            //

            foreach (KeyValuePair<Type, List<Queries.QueryBase>> KeyValuePair in m_QueryTypeToQueries) // clear all query lists!
                KeyValuePair.Value.Clear();
            Queries.QueryBase query;

            while (m_WriteQueryQueue.TryDequeue(out query))
            {// remove from threadsafe queue
                Type queryType = query.GetType();
                List<Queries.QueryBase> queryList;
                if (m_QueryTypeToQueries.TryGetValue(queryType, out queryList))
                { // we have a list for this type!
                    queryList.Add(query);
                }
                else
                { // create new list and add our query to it
                    m_QueryTypeToQueries.Add(queryType, new List<Queries.QueryBase>());
                    m_QueryTypeToQueries[queryType].Add(query);
                }
            }

            //
            // Now we have all of our queries to write aggregated by type and can combine them into a single insert that should be much faster!
            //

            MySqlConnection mySqlConnection = null;
            bool isConnected = m_Database.IsTryToConnect(ref mySqlConnection);

            foreach (KeyValuePair<Type, List<Queries.QueryBase>> KeyValuePair in m_QueryTypeToQueries)
            { // for each bin
                if (KeyValuePair.Value.Count == 0)
                    continue;
                TryCreateAggregatedQuery(KeyValuePair.Value, ref m_AggregatedQueryString);      // create on big query!
                if (m_AggregatedQueryString.ToString() == string.Empty)
                { // there is no query. this is an error!
                    Log.NewEntry(LogLevel.Major, "ProcessQuery: Query provided is empty.");
                    foreach (Queries.QueryBase failedQuery in KeyValuePair.Value)
                    {
                        failedQuery.Status = Queries.QueryStatus.Failed;
                        OnQueryResponse((EventArgs)failedQuery);
                    }
                }
                else if (isConnected && TryProcessWriteQueryString(m_AggregatedQueryString.ToString(), mySqlConnection))
                { // we are connected and have a succesful write
                    foreach (Queries.QueryBase successfulQuery in KeyValuePair.Value)
                    {
                        successfulQuery.Status = Queries.QueryStatus.Completed;
                        this.HubEventEnqueue(successfulQuery);
                        nSuccessfulQueries++;
                    }
                }
                else
                { // we aren't succesful in writing
                    foreach (Queries.QueryBase unsuccessfulQuery in KeyValuePair.Value)
                        unsuccessfulQuery.Status = Queries.QueryStatus.Failed;
                }
            }

            if (mySqlConnection != null)
            { // if the connection is still up close it!
                mySqlConnection.Close();
            }
            DateTime endTimeWriteProcess = Log.GetTime();
            TimeSpan processingTime = endTimeWriteProcess - startTimeWriteProcess;
            Log.NewEntry(LogLevel.Major, "DatabaseReaderWriter UpdatePeriodic: Successfully completed writing {0} queries in {1} ms", nSuccessfulQueries, processingTime.TotalMilliseconds);
        }//UpdatePeriodic()
        //
        //
        #endregion//Private Hub Event Handler Methods



        #region Private Methods
        // *****************************************************
        // ****				Private Methods				    ****
        // *****************************************************
        //
        //
        //
        // *****************************************************
        // ****				Is Try To Connect()				****
        // *****************************************************
        /*
        private bool IsTryToConnect(MySqlConnection myConnection)
        {
            try
            {
                if (myConnection == null)
                {
                    string connStr = string.Format("Data Source={0};User Id={1};Password={2}"
                        , m_Database.ServerIP, m_Database.UserName, m_Database.UserPW);
                    Log.NewEntry(LogLevel.Major, "IsTryToConnect: Trying to connect to database.  {0}", connStr);
                    myConnection = new MySqlConnection(connStr);
                }
                if (myConnection.State != System.Data.ConnectionState.Open)
                {
                    myConnection.Open();
                }
            }
            catch (Exception ex)
            {
                Log.NewEntry(LogLevel.Error, "IsTryToConnect: Database exception = {0}", ex.Message);
            }
            // Validate and exit.
            bool isOpen = myConnection.State == System.Data.ConnectionState.Open;
            if (!isOpen)
            {
                Log.NewEntry(LogLevel.Major, "IsTryToConnect: Failed to connect to {0}, table={1}.", m_Database.ToString(), m_Database.Bars.TableNameFull);
                if (myConnection != null)		// for moment, I have to assume the connection is bad.
                {
                    myConnection.Close();
                    myConnection = null;
                }
            }
            return isOpen;
        }// IsTryToConnect()
        //
        */
        //
        //
        // *********************************************
        // ****         ShutdownNow()               ****
        // *********************************************
        private void ShutdownNow()
        {
            // Release wait handle resources.
            // Note: here I am removing them from list myself! Any blocked thread will not be able to find
            //      the handle in the list now.
            List<int> idList = new List<int>(m_WaitHandles.Keys);
            EventWaitHandle wh;
            foreach (int id in idList)
                if (m_WaitHandles.TryRemove(id, out wh))
                {
                    wh.Set();
                    wh.Dispose();
                }
            while (m_WaitHandleFactory.TryDequeue(out wh))
                wh.Dispose();
            // Stop
            base.Stop();
        }//ShutdownNow()
        //
        //
        //
        private bool TryCreateAggregatedQuery(List<Queries.QueryBase> queryList, ref StringBuilder queryStringBuilder)
        {
            queryStringBuilder.Clear();
            if (queryList.Count == 0)
                return false;

            Type queryType = queryList[0].GetType();            // all must be the same type!

            queryStringBuilder.AppendFormat("{0} {1}", queryList[0].GetWriteQueryPrefix(m_Database), queryList[0].GetWriteQuerySuffix(m_Database)); // start of command!

            for (int i = 1; i < queryList.Count; i++)
            { // for all existing entires start appending values
                if (queryList[i].GetType().Equals(queryType))
                { // it has to be the same type! if not something went wrong, return false
                    string s = queryList[i].GetWriteQuerySuffix(m_Database);
                    if (string.IsNullOrEmpty(s))
                        return false;
                    queryStringBuilder.AppendFormat(", {0}", s);
                }
                else
                    return false;
            }

            queryStringBuilder.Append(";");
            return true;
        }
        #endregion//Private Methods


        #region Events
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        //
        // ****         Query Response          ****
        //
        public event EventHandler QueryResponse;
        //
        //
        public void OnQueryResponse(EventArgs eventArgs)
        {
            if (QueryResponse != null)
            {
                QueryResponse(this, eventArgs);
            }
        }//OnQueryResponse()
        //
        //
        //
        // ****         NonQuery Response      ****
        //
        /// <summary>
        /// This provides only write success/failure responses for non query 
        /// transactions.
        /// </summary>
        public event EventHandler NonQueryResponse;
        //
        //
        public void ONonnQueryResponse(EventArgs eventArgs)
        {
            if (NonQueryResponse != null)
            {
                QueryResponse(this, eventArgs);
            }
        }//OnQueryResponse()


        #endregion//Events


        #region IService
        public string ServiceName
        {
            get { return m_HubName; }
        }
        public void Connect()
        {
        }
        //
        //
        public event EventHandler ServiceStateChanged;
        //
        private void OnServiceStateChanged(ServiceStates prevState, ServiceStates currState)
        {
            if (ServiceStateChanged != null)
            {
                ServiceStateEventArgs e = new ServiceStateEventArgs(this, currState, prevState);
                ServiceStateChanged(this, e);
            }
        }// OnServiceStateChanged()
        //
        //       
        #endregion//IService

        #region IStringifiable
        string IStringifiable.GetAttributes()
        {
            throw new NotImplementedException();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            throw new NotImplementedException();
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            DatabaseInfo.DatabaseLocation dbLoc;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key.ToUpper() == "LOCATION" && Enum.TryParse<DatabaseInfo.DatabaseLocation>(attr.Value, true, out dbLoc))
                    m_Database = DatabaseInfo.Create(dbLoc);
                if (attr.Key.ToUpper() == "SERVERIP")
                    m_Database.ServerIP = attr.Value;
                if (attr.Key.ToUpper() == "USERNAME" && m_Database != null)
                    m_Database.UserName = attr.Value;
                if (attr.Key.ToUpper() == "PASSWORD" && m_Database != null)
                    m_Database.UserPW = attr.Value;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {
            throw new NotImplementedException();
        }
        #endregion//IStrinigifiable

    }
}

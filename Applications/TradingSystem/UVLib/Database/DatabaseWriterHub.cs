using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UV.Lib.Database
{
    using UV.Lib;
    using MySql.Data.MySqlClient;
    using UV.Lib.Hubs;
    using UV.Lib.FrontEnds;
    public class DatabaseWriterHub : Hub
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Global variables
        private FrontEndServices m_Services;
        //
        // Database write controls
        //
        public int WritePeriodSecs = 30;				// Wait time (in secs) between writes to db.
        private bool m_IsBeginningExitSequence = false;	// flag set to true when we are shutting down.
        private DatabaseInfo m_Database;				//
        private MySqlConnection m_MySqlBarTableConnection = null;

        //
        // Database writing queues
        //
        private Utilities.RecycleFactory<DatabaseWriterEventArgs> m_Factory = new Utilities.RecycleFactory<DatabaseWriterEventArgs>();
        private Queue<DatabaseWriterEventArgs> m_NewQueries = new Queue<DatabaseWriterEventArgs>();	// storage of new unwritten queries.
        private Queue<DatabaseWriterEventArgs> m_FailedQueries = new Queue<DatabaseWriterEventArgs>();// storage of failed queries.
        private Queue<DatabaseWriterEventArgs> m_ChronicQueries = new Queue<DatabaseWriterEventArgs>();// storage of failed queries.

        // 
        // Email
        //
        private bool IsEmailSendOn = true;
        private Queue<DatabaseWriterEventArgs> m_NewEmails = new Queue<DatabaseWriterEventArgs>();	// outbox for new emails.
        private string m_EmailSubjectPrefix = String.Empty;						// optional prefix for emails
        private bool IsEmailSendOnException = true;

        // Error files
        private int m_VerboseCount = 0;
        private int m_VerboseCountMax = 10;
        private string m_ErrorFileName = String.Empty;

        // Non-Query execution command out file
        private string m_DropCopyFileName = String.Empty;				// output file for storing all non-query commands.

        //
        // My events
        //
        public event EventHandler WriteCompleted;

        //        
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public DatabaseWriterHub(DatabaseInfo DBInfo)
            : base("DatabaseWriter", UV.Lib.Application.AppInfo.GetInstance().LogPath, true, LogLevel.ShowAllMessages)
        {
            m_Services = FrontEndServices.GetInstance();

            if ((!System.IO.Directory.Exists(m_Services.LogPath)) && (!System.IO.Directory.CreateDirectory(m_Services.LogPath).Exists))
            {	// Failed to create the directory for logs.
                Log.NewEntry(LogLevel.Error, "Failed to create directory {0}.", m_Services.LogPath);
            }

            m_Database = DBInfo;
            m_EmailSubjectPrefix = m_Services.RunName.ToString();

            base.m_WaitListenUpdatePeriod = WritePeriodSecs * 1000;		// convert to miliseconds.
            Initialize();
        }//Constructor
        //
        //
        private void Initialize()
        {
            // Create non-query command dump file - place for all sql commands.
            string fileName = "Drop_MySql.txt";
            string outputDirName = UV.Lib.Application.AppInfo.GetInstance().LogPath;
            if (String.IsNullOrEmpty(m_Services.LogPath))
                outputDirName = Directory.GetCurrentDirectory() + "\\";
            else if (!Directory.Exists(outputDirName))
                Directory.CreateDirectory(outputDirName);
            string timeString = Log.m_StartTime.ToString("HHmm_");
            m_DropCopyFileName = outputDirName + "Log_" + timeString + fileName;	// basic name.
            int dupeFileID = 0;
            while (File.Exists(m_DropCopyFileName))		// Make sure the file name is unique.
            {
                m_DropCopyFileName = outputDirName + "Log_" + timeString + string.Format("{0}_", dupeFileID.ToString()) + fileName;
                dupeFileID++;							// increase index until we find a non-used file name.
            }

            // Create unique ErrorFileName - will contain chronic failure queries.
            StringBuilder s = new StringBuilder();
            s.AppendFormat("Log_{0}_{1}_Error", Log.m_StartTime.ToString("HHmm"), base.m_HubName);
            s.Insert(0, outputDirName);
            m_ErrorFileName = string.Format("{0}.txt", s.ToString());
            dupeFileID = 0;
            while (System.IO.File.Exists(m_ErrorFileName))		// Make sure the file name is unique.
            {
                m_ErrorFileName = string.Format("{0}_{1}.txt", s.ToString(), dupeFileID.ToString());
                dupeFileID++;							// increase index until we find a non-used file name.
            }

        }// Initialize().
        //
        //       
        #endregion//Constructors

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****				Send Email Message				****
        //
        /// <summary>
        /// This method provides a simple way to send emails, constructing the appropriate
        /// DBWRequest object for a send email request.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="aMsg"></param>
        /// <returns></returns>
        public bool SendEmailMessage(string subject, string aMsg)
        {
            if (IsEmailSendOn)
            {
                DatabaseWriterEventArgs eventArg = this.GetEventArg(DatabaseWriterRequests.SendEmail);
                eventArg.QueryBase.Append(subject);
                eventArg.QueryValues.Append(aMsg);
                return this.HubEventEnqueue(eventArg);
            }
            else
                return false;
        }// SendEmailMessage().
        //
        //
        //
        // ****				InsertIntoTable()				****
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="data">Dictionary object loaded directly into event.  Not affected by this routine.</param>
        /// <param name="logMessage"></param>
        /// <returns>String of outgoing query, suitable to add to your log.</returns>
        public bool InsertIntoTable(string tableName, ref Dictionary<string, string> data, out string logMessage)
        {
            bool isSuccessful = true;
            StringBuilder msg = new StringBuilder();
            msg.AppendFormat("DatabaseWriterHub.InsertIntoTable {0} ", tableName);

            //
            // Create query
            //
            DatabaseWriterEventArgs dbEntry = GetEventArg();
            // base query
            dbEntry.QueryBase.AppendFormat("INSERT INTO {0} (", tableName);
            string[] keys = new string[data.Keys.Count];
            data.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; ++i)
            {
                dbEntry.QueryBase.AppendFormat(" {0}", keys[i]);
                if (i != keys.Length - 1) dbEntry.QueryBase.Append(",");
            }
            dbEntry.QueryBase.Append(") VALUES ");
            // add the values for query
            dbEntry.QueryValues.Append("(");
            for (int i = 0; i < keys.Length; ++i)
            {
                dbEntry.QueryValues.AppendFormat(" \'{0}\'", data[keys[i]]);
                if (i != keys.Length - 1) dbEntry.QueryValues.Append(",");
            }
            dbEntry.QueryValues.Append(");");
            isSuccessful = this.HubEventEnqueue(dbEntry);

            // Write Log.
            if (isSuccessful)
                msg.Append("Success writing: ");
            else
                msg.Append("Failed writing: ");
            foreach (string s in data.Keys) { msg.AppendFormat("[{0}={1}]", s, data[s]); }
            this.Log.NewEntry(LogLevel.Major, msg.ToString());

            // Exit;
            logMessage = msg.ToString();
            return isSuccessful;
        }//InsertIntoTable().
        //
        //
        // ****						Execute NonQuery()						****
        /// <summary>
        /// User has provided a non-query command for us to process.  We do nothing but
        /// send off his request.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool ExecuteNonQuery(string msg)
        {
            bool isSuccessful = true;
            // Create query
            DatabaseWriterEventArgs dbEntry = GetEventArg();
            dbEntry.QueryBase.Append(msg);
            dbEntry.QueryValues.Remove(0, dbEntry.QueryValues.Length);	// double check this is clear.
            isSuccessful = this.HubEventEnqueue(dbEntry);
            // Write Log.
            if (!isSuccessful)
                Log.NewEntry(LogLevel.Warning, "NonQueryExecution: Failed to enqueue {0}.", msg);
            return isSuccessful;
        }//Execute
        //
        //
        //
        //
        public override void RequestStop()
        {
            this.HubEventEnqueue(this.GetEventArg(DatabaseWriterRequests.Stop));	// Will call base.Stop() by internal thread.
        }
        
        //
        //
        //
        #endregion//public methods

        #region Private Email Methods
        // *****************************************************************
        // ****                Private Email Methods                    ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// This method compiles all the messages intended 
        /// </summary>
        private void SendEmailNow()
        {

            string subject = String.Empty;
            StringBuilder msg = new StringBuilder();
            DateTime thisTime = Log.GetTime();
            msg.AppendFormat("TimeStamp: {0}  {1} \n", thisTime.ToShortDateString(), thisTime.ToString("HH:mm:ss.fff"));

            //
            // Compile multiple email messages into one.
            //
            int nEmails = 1;
            while (m_NewEmails.Count > 0)
            {
                DatabaseWriterEventArgs eventArg = m_NewEmails.Dequeue();
                // TODO: Confirm these mesages for same receivers, and same subject,
                // if so, then append their bodies.  Now I assume everyone gets every email message.
                if (String.IsNullOrEmpty(subject))
                    subject = eventArg.QueryBase.ToString();
                else if (!subject.Contains(eventArg.QueryBase.ToString()))
                    subject = String.Format("{0},{1}", subject, eventArg.QueryBase.ToString());

                // Append messages.
                msg.AppendFormat("\n     --------------------------     Message #{0:#0}\n", nEmails.ToString());
                msg.AppendFormat("{0}\n", eventArg.QueryValues.ToString());
                nEmails++;
            }//wend

            //
            // Divide recipents into groups of same domains.  (Multiple domains yields exception on send.)
            //
            Dictionary<string, List<string>> emailRecp = new Dictionary<string, List<string>>();
            foreach (string recp in m_Services.EmailRecipients)
            {
                string[] s = recp.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length == 2)
                {   // This seems like a good address.
                    if (!emailRecp.ContainsKey(s[1]))
                        emailRecp.Add(s[1], new List<string>());    // make an entry for this domain name.
                    emailRecp[s[1]].Add(recp);                      // add email address to list for this domain name.
                }
                else
                    Log.NewEntry(LogLevel.Error, "DatabaseWriterHub.SendEmailNow() failed to split off domain name from email recipient {0}.", recp);
            }//next email recpient

            foreach (string domainName in emailRecp.Keys)
            {
                //
                // Create email
                //
                System.Net.Mail.MailMessage email = new System.Net.Mail.MailMessage();
                email.From = new System.Net.Mail.MailAddress(m_Services.AppEmailAddress, m_Services.AppName);//"Tramp@bgtradingllc.com", "Tramp");
                //foreach (string recp in m_Services.EmailRecipients)
                foreach (string recp in emailRecp[domainName])
                    email.To.Add(recp);
                if (String.IsNullOrEmpty(m_EmailSubjectPrefix))
                    email.Subject = string.Format("{0}", subject);
                else
                    email.Subject = string.Format("{0}-{1}", m_EmailSubjectPrefix, subject);
                email.Body = msg.ToString();


                //
                // Send message.
                //
                DateTime dt = Log.GetTime();
                bool isSuccessful = true;
                System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential("UVTrading1@gmail.com", "DVTr@ding1");
                client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                try
                {
                    client.Send(email);
                }
                catch (Exception ex)
                {
                    isSuccessful = false;
                    Log.NewEntry(LogLevel.Error, "SendEmailNow: Exception={0}", ex.Message);
                }
                TimeSpan ts = Log.GetTime().Subtract(dt);
                Log.NewEntry(LogLevel.Minor, "SendEmailNow: Success = {0}. Appended {1} messages. ElapsedTime = {2:0.000} ", isSuccessful.ToString(), nEmails.ToString(), ts.TotalSeconds);
            }// next domain name to send email.

        }//SendEmailNow().
        //
        //
        //
        #endregion // private email

        #region Private Database Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // ****					Get EventArg()				****
        /// <summary>
        /// The most-basic DBWRequests event arg is passed back to the caller.
        /// It is assumed to be of type database "Write".
        /// </summary>
        /// <returns></returns>
        private DatabaseWriterEventArgs GetEventArg()
        {
            return GetEventArg(DatabaseWriterRequests.Write);
        }
        private DatabaseWriterEventArgs GetEventArg(DatabaseWriterRequests request)
        {
            DatabaseWriterEventArgs dbwEvent = m_Factory.Get();
            dbwEvent.Clean();
            dbwEvent.Request = request;
            return dbwEvent;
        }// GetEventArg()
        //
        //
        //
        // ****				Write Now()			****
        //
        /// <summary>
        /// Here we do actual writing the data to the database.
        /// </summary>
        private void WriteNow()
        {
            if (!IsTryToConnect())
            {
                Log.NewEntry(LogLevel.Warning, "WriteNow:  {0} queries waiting. Connection failed. Skipping WriteNow.", m_NewQueries.Count.ToString());
                return;
            }
            DateTime time1 = Log.GetTime();


            System.IO.StreamWriter dropStreamWriter = null;
            try
            {
                if (!string.IsNullOrEmpty(m_DropCopyFileName))
                    dropStreamWriter = new System.IO.StreamWriter(m_DropCopyFileName, true);
            }
            catch (Exception e)
            {
                Log.NewEntry(LogLevel.Warning, "WriteNow:  Failed to open drop copy writer. Exception {0}", e.Message);
                if (dropStreamWriter != null)
                {
                    dropStreamWriter.Dispose();
                    dropStreamWriter = null;				// signals that we won't make drop copy.
                }
            }

            //
            // Execute queries
            //
            int nRowsWritten = 0;
            bool areExecutionsGood = true;					// flag for errors.			
            using (MySqlCommand cmd = new MySqlCommand())
            {
                while (m_NewQueries.Count > 0)
                {
                    DatabaseWriterEventArgs queryItem = m_NewQueries.Dequeue();

                    if (dropStreamWriter != null)
                        dropStreamWriter.WriteLine(queryItem.Query);

                    // Try to execute the non-query
                    if (areExecutionsGood)
                    {
                        cmd.CommandText = queryItem.Query;
                        cmd.Connection = m_MySqlBarTableConnection;
                        try
                        {
                            nRowsWritten += cmd.ExecuteNonQuery();
                            m_Factory.Recycle(queryItem);	//this.RecycleEventArg(queryItem);			// return this to heap.
                        }
                        catch (Exception ex)
                        {
                            Log.NewEntry(LogLevel.Error, "WriteNow: Exception = {0}.  Query = {1}", ex.Message, queryItem.Query);
                            areExecutionsGood = false;					// on first failure, we will cease writing.
                            m_FailedQueries.Enqueue(queryItem);			// store failed query.							
                            if (m_MySqlBarTableConnection != null)		// for moment, I have to assume the connection is bad.
                            {
                                m_MySqlBarTableConnection.Close();
                                m_MySqlBarTableConnection = null;
                            }
                            if (IsEmailSendOnException)					// Send email warnings on failure!
                            {
                                string msg = string.Format("WriteNow: Exception = {0}.  Query = {1}", ex.Message, queryItem.Query);
                                SendEmailMessage(string.Format("{0} {1}", m_HubName, m_Database.Location.ToString()), msg);
                            }
                        }
                    }
                    else
                    {
                        m_FailedQueries.Enqueue(queryItem);			// move to the list of failed queries.
                    }

                }//while new qeuries remain.
            }// using MySqlCommand

            if (dropStreamWriter != null)
            {
                dropStreamWriter.Flush();
                dropStreamWriter.Close();
                dropStreamWriter.Dispose();
            }

            TimeSpan timespan = Log.GetTime().Subtract(time1);
            Log.NewEntry(LogLevel.Minor, "WriteNow: {0} rows written to {1}. Completed={2}. FailedQueryQueue={3}.  Elapsed time = {4}s.", nRowsWritten.ToString(), m_Database.Location.ToString(), areExecutionsGood.ToString(), m_FailedQueries.Count.ToString(), timespan.TotalSeconds.ToString("0.000"));

            //
            // Inform subscribers of write
            //
            if (this.WriteCompleted != null)
            {
                WriteStatusEventArgs e = new WriteStatusEventArgs();
                e.Message = string.Format("{0} rows written to {1}. Completed={2}. FailedQueryQueue={3}.  Elapsed time = {4}s.", nRowsWritten.ToString(), m_Database.Location.ToString(), areExecutionsGood.ToString(), m_FailedQueries.Count.ToString(), timespan.TotalSeconds.ToString("0.000")); ;
                WriteCompleted(this, e);
            }

            //
            // Clean up.
            //
            if ((areExecutionsGood && m_FailedQueries.Count > 0) || m_FailedQueries.Count > 1000)
            {
                Log.NewEntry(LogLevel.Warning, "Attempting to Process {0} failed queries", m_FailedQueries.Count);
                ProcessFailedQueries();
            }
            if (m_ChronicQueries.Count > 0)
            {
                Log.NewEntry(LogLevel.Warning, "Attempting to Process {0} chronic queries", m_ChronicQueries.Count);
                ProcessChronicQueries();
            }
        }//WriteNow().
        //
        //
        //
        //
        //
        public class WriteStatusEventArgs : EventArgs
        {
            public string Message;
        }


        //
        // *****************************************************
        // ****				ProcessFailQueries()			****
        // *****************************************************
        /// <summary>
        /// Queries in the FailQueriesQueue have failed to be written in the past.
        /// It might have been they that threw the write exception, or another query.
        /// Here, we try to write them again.  If they fail several more times, they are pushed
        /// onto the Chronic queue for later study.
        /// </summary>
        private void ProcessFailedQueries()
        {
            MySqlCommand cmd = new MySqlCommand();
            int nFailedRowsWritten = 0;
            Log.NewEntry(LogLevel.Warning, "WriteNow: Retry failed {0} queries.", m_FailedQueries.Count.ToString());
            Queue<DatabaseWriterEventArgs> failedAgain = new Queue<DatabaseWriterEventArgs>();

            while (m_FailedQueries.Count > 0)
            {
                DatabaseWriterEventArgs f = m_FailedQueries.Dequeue();
                if (f.NFails < 3)
                {
                    cmd.CommandText = f.Query;
                    cmd.Connection = m_MySqlBarTableConnection;
                    try
                    {
                        nFailedRowsWritten += cmd.ExecuteNonQuery();
                        m_Factory.Recycle(f);		//this.RecycleEventArg(f);	
                    }
                    catch (Exception ex)
                    {
                        f.NFails++;							// increment failure counter.
                        if (failedAgain.Count < 2) { Log.NewEntry(LogLevel.Warning, "WriteNow: Query failed {1} times. Exception = {0} Query = {1}.", ex.Message, f.NFails.ToString(), f.Query); }
                        failedAgain.Enqueue(f);				// store failed query.
                    }
                }
                else
                {	// Mark this query as chronic.
                    if (m_ChronicQueries.Count < 2) Log.NewEntry(LogLevel.Warning, "WriteNow: Chronic queries have been found.");
                    m_ChronicQueries.Enqueue(f);
                }
            }
            // Push those that failed again onto the queue.
            if (failedAgain.Count > 0)
            {
                Log.NewEntry(LogLevel.Warning, "WriteNow: A total of {0} queries failed again.", failedAgain.Count.ToString());
                while (failedAgain.Count > 0)
                {
                    m_FailedQueries.Enqueue(failedAgain.Dequeue());	// push back onto failed queue.
                }
            }
        }// ProcessFailedQueries().
        //
        //
        // 
        // *****************************************************
        // ****				ProcessChronicQueries()			****
        // *****************************************************
        /// <summary>
        /// These queries have failed many times already.  There are many possible reasons.
        /// This method will make some attempt to correct these reasons, otherwise write these
        /// chronic queries to a file for the user to figure out later.
        /// </summary>
        private void ProcessChronicQueries()
        {
            Log.NewEntry(LogLevel.Warning, "ProcessChronicQueries: attempting to fix chronically failed queries.");
            Queue<DatabaseWriterEventArgs> queriesToDump = new Queue<DatabaseWriterEventArgs>();
            Queue<string> valuesList = new Queue<string>();
            while (m_ChronicQueries.Count > 0)
            {
                DatabaseWriterEventArgs chronicQuery = m_ChronicQueries.Dequeue();
                const string valueKey = "values";
                int valueStart = chronicQuery.Query.IndexOf(valueKey, StringComparison.CurrentCultureIgnoreCase);
                string headerString = chronicQuery.Query.Substring(0, valueStart);
                string valueString = chronicQuery.Query.Substring(valueStart + valueKey.Length);
                if (valueString.Contains("),("))
                {	// This query has compound rows, try to split them out.
                    string s3 = valueString.Trim(';', ' ');			// remove trailing ';'
                    string[] s2 = s3.Split(new string[] { "),(" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in s2)			// strip out silly entries.
                    {
                        string element = s.Trim(')', '(', ' ');
                        if (!String.IsNullOrEmpty(element))
                            valuesList.Enqueue(element);
                    }
                    // Process elements
                    if (valuesList.Count > 1)
                    {
                        Log.NewEntry(LogLevel.Warning, "ProcessChronicQueries: Breaking up chronic query into {0} pieces.", valuesList.Count.ToString());
                        while (valuesList.Count > 0)
                        {
                            string element = valuesList.Dequeue();
                            if (!String.IsNullOrEmpty(element) && element.Length > 1)
                            {
                                DatabaseWriterEventArgs f = this.GetEventArg(DatabaseWriterRequests.Write);
                                f.QueryBase.Append(headerString);
                                f.QueryValues.AppendFormat("VALUES ({0});", element);			// reconstituting trailing semicolon!
                                m_FailedQueries.Enqueue(f);			// try this again.
                            }
                        }
                    }
                    else
                        queriesToDump.Enqueue(chronicQuery);
                }
                else
                {
                    queriesToDump.Enqueue(chronicQuery);
                }
            }
            if (queriesToDump.Count > 0) DumpChronicQuery(queriesToDump);
        }// ProcessChronicQueries()
        //
        //
        //
        // *****************************************************
        // ****				DumpChronicQuery(	)			****
        // *****************************************************
        private void DumpChronicQuery(Queue<DatabaseWriterEventArgs> queriesToDump)
        {
            System.IO.StreamWriter streamWriter = null;
            int nWritten = 0;
            try
            {
                streamWriter = new System.IO.StreamWriter(m_ErrorFileName, true);

                while (queriesToDump.Count > 0)
                {
                    DatabaseWriterEventArgs query = queriesToDump.Dequeue();
                    streamWriter.WriteLine(query.Query);
                    nWritten++;
                    m_Factory.Recycle(query);		// this.RecycleEventArg(query);	
                }
                streamWriter.Flush();
            }
            catch (Exception e)
            {
                Log.NewEntry(LogLevel.Error, "DumpChronicQuery: Failed to write. Exception = {0}", e.Message);
                while (queriesToDump.Count > 0)
                {
                    DatabaseWriterEventArgs query = queriesToDump.Dequeue();
                    Log.NewEntry(LogLevel.Error, "DumpChronicQuery: FailedQuery = {0}", query.Query);
                }
            }
            finally
            {
                if (streamWriter != null) streamWriter.Close();
            }
            Log.NewEntry(LogLevel.Warning, "DumpChronicQuery: Dumped {0} chronic queries to error file = {1}.", nWritten.ToString(), m_ErrorFileName);

        }//DumpChronicQuery()
        //
        //
        //
        //
        //
        //
        //
        // *****************************************************
        // ****				Is Try To Connect()				****
        // *****************************************************
        private bool IsTryToConnect()
        {
            try
            {
                if (m_MySqlBarTableConnection == null)
                {
                    string connStr = string.Format("Data Source={0};User Id={1};Password={2}"
                        , m_Database.ServerIP, m_Database.UserName, m_Database.Password);
                    Log.NewEntry(LogLevel.Major, "IsTryToConnect: Trying to connect to database.  {0}", connStr);
                    m_MySqlBarTableConnection = new MySqlConnection(connStr);
                }
                if (m_MySqlBarTableConnection.State != System.Data.ConnectionState.Open)
                {
                    m_MySqlBarTableConnection.Open();
                }
            }
            catch (Exception ex)
            {
                Log.NewEntry(LogLevel.Error, "IsTryToConnect: Database exception = {0}", ex.Message);
            }
            // Validate and exit.
            bool isOpen = m_MySqlBarTableConnection.State == System.Data.ConnectionState.Open;
            if (!isOpen)
            {
                Log.NewEntry(LogLevel.Major, "IsTryToConnect: Failed to connect to {0}, table={1}.", m_Database.ToString(), m_Database.Bars.TableNameFull);
                if (m_MySqlBarTableConnection != null)		// for moment, I have to assume the connection is bad.
                {
                    m_MySqlBarTableConnection.Close();
                    m_MySqlBarTableConnection = null;
                }
            }
            return isOpen;
        }// IsTryToConnect()
        //
        //
        private void WriteVerbose()
        {
            Log.NewEntry(LogLevel.Minor, "Verbose: Events={0}", m_Factory.ToString());
        } //WriteVerbose
        //
        //
        #endregion//Private Methods

        #region HubEvent Handlers
        // *****************************************************************
        // ****                 HubEvent Handlers	                    ****
        // *****************************************************************
        //
        protected override void HubEventHandler(EventArgs[] eArgList)
        {
            //
            // Extract and separate eventArgs we process.
            //
            foreach (EventArgs eArg in eArgList)
            {
                // DBW Requests
                if (eArg is DatabaseWriterEventArgs)
                {
                    DatabaseWriterEventArgs dbwEvent = (DatabaseWriterEventArgs)eArg;
                    if (dbwEvent.Request == DatabaseWriterRequests.Stop)
                        m_IsBeginningExitSequence = true;			// signal our desired to shutdown.
                    else if (dbwEvent.Request == DatabaseWriterRequests.Write)
                        m_NewQueries.Enqueue(dbwEvent);				// add to outgoing write queue.
                    else if (dbwEvent.Request == DatabaseWriterRequests.SendEmail)
                        m_NewEmails.Enqueue(dbwEvent);				// add to outgoing emails.
                    else
                        Log.NewEntry(LogLevel.Warning, "Unknown DBWEventArg request received. EventArg = {0}.", dbwEvent.ToString());
                }
                else
                    Log.NewEntry(LogLevel.Warning, "Unknown EventArg received. EventArg = {0}.", eArg.ToString());
            }//next event
            //
            // Test for exit condition.
            //
            if (m_IsBeginningExitSequence)
            {
                base.m_WaitListenUpdatePeriod = 1500;
                if (m_NewEmails.Count > 0) SendEmailNow();
                if (m_NewQueries.Count > 0) WriteNow();
                base.Stop();
            }
        }//HubEventHandler
        //
        // *****************************************************************
        // ****						Update Periodic()					****
        // *****************************************************************
        protected override void UpdatePeriodic()
        {
            // Write to database
            if (m_NewQueries.Count > 0) WriteNow();

            // Send emails
            if (m_NewEmails.Count > 0) SendEmailNow();

            // Write verbose output
            m_VerboseCount++;
            if (m_VerboseCount > m_VerboseCountMax)
            {
                m_VerboseCount = 0;
                WriteVerbose();
            }
        }//UpdatePeriodic
        //
        //
        #endregion//Event Handlers
    }
}

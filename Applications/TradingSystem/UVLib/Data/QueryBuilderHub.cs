using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Data
{
    using UV.Lib.Hubs;
    using UV.Lib.Database;
    using UV.Lib.FrontEnds;
    using UV.Lib.Products;
    /// <summary>
    /// This is a helper class for the DataHub
    /// </summary>
    public class QueryBuilderHub : Hub
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Other hubs
        private DataHub m_DataHub = null;
        private DatabaseWriterHub m_DatabaseWriter = null;

        // Output database
        private DatabaseInfo m_DatabaseInfo;
        public string DatabaseTableNameOverride = string.Empty;
        private StringBuilder m_Body = new StringBuilder();				// mysql body workspace.
        private StringBuilder m_Header = new StringBuilder();			// mysql header

        // Output storage and control
        private bool m_IsShuttingDown = false;
        private Queue<BarEventArgs> m_EventList = new Queue<BarEventArgs>();	// Events to write out.
        public int WriteBlockSize = 60;

        //
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public QueryBuilderHub(DataHub dataHub, DatabaseWriterHub dbWriter)
            : base("QueryBuilder", UV.Lib.Application.AppInfo.GetInstance().LogPath, true, LogLevel.ShowAllMessages)
        {
            m_DataHub = dataHub;
            m_DatabaseWriter = dbWriter;
            FrontEndServices m_Services = FrontEndServices.GetInstance();
            m_Services.EmailRecipients = new string[1];
            m_Services.EmailRecipients[0] = m_DataHub.m_EmailAddr;
            Log.AllowedMessages = LogLevel.ShowAllMessages;

            // Output database			
            m_DatabaseInfo = m_DataHub.m_DataBaseInfo;
            this.Start();

        }//constructor
        //
        //       
        #endregion//Constructors

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public override void RequestStop()
        {	// Request gentle shut down
            QueryBuilderHubRequest e = new QueryBuilderHubRequest();
            e.Request = QueryBuilderHubRequestCode.Stop;
            this.HubEventEnqueue(e);
        }
        //
        //
        /// <summary>
        /// Called by external thread to request that the hub thread create a 
        /// query to check to make sure out database has the 
        /// instrument details supplied 
        /// correctly in the database.
        /// </summary>
        /// <param name="instrDetails"></param>
        public void RequestCheckInstrDBDetails(InstrumentDetails instrDetails)
        {
            QueryBuilderHubRequest e = new QueryBuilderHubRequest();
            e.Request = QueryBuilderHubRequestCode.RequestCheckDBInstrDetails;
            e.Data.Add(instrDetails);
            this.HubEventEnqueue(e);
        }


        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        /// <summary>
        /// Creates the queury
        /// </summary>
        private void ProcessQueryBuildEvent()
        {
            Log.NewEntry(LogLevel.Minor, "ProcessQueryBuildEvent: Processing {0} rows.", m_EventList.Count.ToString());
            while (m_EventList.Count > 0)					        // while there are events to process...
            {
                m_Body.Remove(0, m_Body.Length);			        // clear entire content of last command
                m_Header.Remove(0, m_Header.Length);
                int rowCount = 0;

                // Process all events that share *same* header. (Same table name).
                string tableName = string.Empty;			        // we will check that this is empty, signals new write query.				
                while (m_EventList.Count > 0)
                {
                    // Peek at next event to process.  We will write as many as we can that share a common
                    // header.  (That is, common table name.)
                    BarEventArgs barArg = m_EventList.Peek();       // this contains many instruments, but with one common timestamp!
                    string tbName = m_DatabaseInfo.Bars.TableNameFull;
                    if (string.IsNullOrEmpty(tableName))
                    {	// First time we came thru here, or we just breaked out from last 
                        tableName = tbName;
                        m_Header.AppendFormat(Bar.QueryHeader, tableName);
                    }
                    else if (!tableName.Equals(tbName))
                        break;								        // we must not add these bars to current query.
                    //
                    // Write the queries for this event
                    //
                    barArg = m_EventList.Dequeue();			        // dequeue the barEventArg we will NOW process!
                    string timeStamp = barArg.unixTime.ToString();	// create unix time stamp
                    while (barArg.BarList.Count > 0)		        // load all bars in this event.
                    {
                        Bar abar = barArg.BarList.Dequeue();        // extract bars from the eventArg!
                        if (rowCount == 0)					        // first value for this query.
                            m_Body.Append("(");
                        else
                            m_Body.Append(",(");
                        m_Body.Append(abar.GetQueryValues(timeStamp));		// add values for entire instr. bar.
                        m_Body.Append(")");
                        m_DataHub.m_BarFactory.Recycle(abar);       // we are done with the bar now, recycle
                        rowCount++;							        // each set of values will lead to a new row.
                    }
                    
                    m_DataHub.m_BarEventFactory.Recycle(barArg);	// done with this event, recycle it 
                }//while
                m_Body.Append(";");							        // finish query construction.
                m_DatabaseWriter.ExecuteNonQuery(string.Format("{0} {1}", m_Header.ToString(), m_Body.ToString()));
            }//while events to process.

        }// ProcessQueryBuildEvent()
        //
        //
        //
        /// <summary>
        /// called by the hub thread to process a request to create a query for instrument details
        /// </summary>
        /// <param name="requestArg"></param>
        private void ProcessRquestDBInstrDetails(QueryBuilderHubRequest requestArg)
        {
            Log.NewEntry(LogLevel.Minor, "ProcessCheckDBInstrDetails: Processing Request");
            foreach (object obj in requestArg.Data)
            {
                InstrumentDetails instrDetails = (InstrumentDetails)obj;
                // TODO CALL DB INSTRUMENT METHODS FROM HERE
            }
        }
        //
        //
        //
        #endregion//Private Methods

        #region Hub Event Handler
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        protected override void HubEventHandler(EventArgs[] eArgList)
        {
            foreach (EventArgs e in eArgList)
            {
                if (e is BarEventArgs)
                {	// This is a bar event.  It was pushed onto me from the data hub.
                    if (!m_IsShuttingDown)
                        m_EventList.Enqueue((BarEventArgs)e);
                }
                else if (e is QueryBuilderHubRequest)
                { // this is an request to do something besides baring data!
                    QueryBuilderHubRequest requestArg = (QueryBuilderHubRequest)e;
                    switch (requestArg.Request)
                    {
                        case QueryBuilderHubRequestCode.Stop:
                            m_IsShuttingDown = true;
                            break;
                        case QueryBuilderHubRequestCode.RequestCheckDBInstrDetails:
                            ProcessRquestDBInstrDetails(requestArg);        // not fully implemented
                            break;
                        default:
                            Log.NewEntry(LogLevel.Error, "HubEventHandler : QueryBuilderHub {0} not implemented", requestArg.ToString());
                            break;
                    }
                }
                else
                {
                    Log.NewEntry(LogLevel.Error, "Unknown event type: {0}", e.ToString());
                }
            }
            // Process Queries
            if (m_IsShuttingDown || m_EventList.Count >= WriteBlockSize)
            {
                ProcessQueryBuildEvent();
            }
            if (m_IsShuttingDown)
            {
                base.Stop();
            }


        }//HubEventHandler()
        //
        //
        //
        #endregion//Hub Event Handlers

    }//end class
}

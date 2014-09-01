using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Misty.Lib.Hubs
{
    //
    //
    //
    // *************************************************************************
    // ****                         LogHub class                            ****
    // *************************************************************************
    /// <summary>
    /// An external thread passes a string (and other info) by calling one of the 
    /// public methods NewEntry(), BeginEntry(), AppendEntry(), etc which loads the
    /// message into a LogEventArg object.
    /// This is pushed onto the local HubBase event queue, via the usual HubEventEnqueue(). 
    /// for later processing and subsequent pushing onto an outgoing message queue.
    /// Thereafter, the HubBase thread awakens and passes the eventArg back up to 
    /// LogHub.HubEventHandler( EventArg ) which unpacks the string message, and processes it.
    /// </summary>
    public class LogHub : HubBase
    {

        #region Members
        // *********************************************************************
        // ****                         Members                             ****
        // *********************************************************************
        //
        // Out-going message queues
        //
        private Queue<string> m_OutQueue;
        private int m_QueueSize = 500;							// initial size of queue.
        private double m_WriteThreshold = 0.80;                 // Write when OutQueue is more than n% filled.
        private int m_MessageID = 0;							// serial msg id number,. 

        // LogEvent storage
        private Queue<LogEventArgs> m_EventStorage;
        private int m_EventStorageSize = 16;
        private object m_EventStorageLock = new object();


        private volatile LogLevel m_AllowedMessageMask = LogLevel.ShowErrorMessages;


        // New attempt at more accurate time stamps.
        public Stopwatch m_StopWatch = new Stopwatch();
        public DateTime m_StartTime;

        //
        // Message monitor
        //
        private FrontEnds.GuiCreator m_GuiCreator = null;     // used to create a LogViewer
        protected LogViewer m_LogViewer = null;
        protected bool m_IsViewActive = false;

        //
        // Warning/error message monitor
        //
        public LogViewer m_ErrorView = null;				// Warning/errors monitor
        public object m_ErrorViewLock = new object();
        private static LogViewCreator m_ViewCreator = null;
        private static object m_ViewCreatorLock = new object();

        //
        // File & logname.
        //
        public string LogName;                                 // This is the label put on each outgoing msg.
        //private System.IO.StreamWriter m_FileStream = null;     
        private string m_OutputFileName = string.Empty;			// Out file full path
        private string m_OutputDirName = string.Empty;			// base directory path for Logs

        //
        // Working entry.
        //
        private Dictionary<int, StringBuilder> m_WorkingMessage = new Dictionary<int, StringBuilder>();
        private Dictionary<int, bool> m_IsWorkingMessage = new Dictionary<int, bool>();
        private Dictionary<int, LogLevel> m_WorkingMessageLevel = new Dictionary<int, LogLevel>();

        //
        // Copy the log files to one centralized place.
        //
        static private object m_CopyDirectoryLock;                  // Directory lock
        #endregion // members


        #region Properties
        // *************************************************************
        // ****                     Properties                      ****
        // *************************************************************
        //
        //
        //
        // ****             Allowed Message                 ****
        //
        public LogLevel AllowedMessages
        {
            get { return m_AllowedMessageMask; }
            set
            {
                m_AllowedMessageMask = value;
                if (m_LogViewer != null)
                {
                    m_LogViewer.AddMessage(String.Empty);
                }
            }
        }
        //
        //
        // ****             Is View Active                  ****
        //
        public bool IsViewActive
        {
            get { return m_IsViewActive; }
            set
            {
                m_IsViewActive = value;
                if (m_LogViewer != null)
                {
                    if (m_IsViewActive)
                    {   // view is active
                        m_LogViewer.AddMessage(String.Empty);
                        m_LogViewer.WindowState = System.Windows.Forms.FormWindowState.Normal;
                    }
                    else
                    {   // view is not active.
                        m_LogViewer.WindowState = System.Windows.Forms.FormWindowState.Minimized;
                    }
                }
                else if (m_IsViewActive == true)
                {	// Can try to create new view.  New Gui thread to create it tho.					
                    CreateLogViewer();
                    //m_LogViewer = new LogViewer(this);
                    //m_LogViewer.Show();				// does this handle non-window threads? Does it automatically invoke?					
                    
                }
            }
        }
        //
        //
        //
        // ****             EntryHeader                  ****
        //
        //public string EntryHeader
        //{
        //    get{ return LogName; }
        //    //set{ lock (m_InQueueSyncLock) { LogName = value; } }
        //    set { LogName = value;  }   // protect this from multithreading!
        //}//end EntryHeader
        //
        //
        // ****             Is Working Message              ****
        //
        public bool IsWorkingMessage
        {
            get
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                bool isworking = false;
                if (m_IsWorkingMessage.TryGetValue(id, out isworking))
                    return isworking;
                else
                    return false;
            }
        }
        //
        // ****			Queue Size				****
        //
        public int QueueSize
        {
            get { return m_QueueSize; }
            set
            {
                if (value > 0)
                    m_QueueSize = value;
            }
        }
        //
        //
        //
        //
        #endregion // properties


        #region Constructors
        // *********************************************************************
        // ****                      Constructor()                          ****
        // *********************************************************************
        /// <summary>
        /// Main constructor.
        /// </summary>
        public LogHub(string logName, string logDirectoryPathName, bool isViewerDesired,
             LogLevel initialLevel)
            : base(logName + "Log")
        {
            LogName = logName;                              // this is the name of this log.          

            base.m_WaitListenUpdatePeriod = 60 * 1000;      // flush period

            // Create directory lock.
            if (m_CopyDirectoryLock == null)
                m_CopyDirectoryLock = new object();

            if (isViewerDesired)
                CreateLogViewer();  //m_LogViewer = new LogViewer(this);
            m_IsViewActive = isViewerDesired;
            AllowedMessages = initialLevel;

            // Create the base Log directory path name.
            m_OutputDirName = logDirectoryPathName;
            if (!m_OutputDirName.EndsWith("\\")) { m_OutputDirName = m_OutputDirName + "\\"; }

            Initialize();
        }//end constructor.
        //
        //
        //
        // *************************************************************
        // ****                 Initialize()                        ****
        // *************************************************************
        /// <summary>
        /// Called by main GUI Thread via a constructor.  Allows creation of form, if desired.
        /// </summary>
        private void Initialize()
        {
            // Create a log viewer form.
            if (m_LogViewer != null)
            {
                m_LogViewer.Show();
                if (!m_IsViewActive)
                    m_LogViewer.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            }

            // ViewCreator
            lock (m_ViewCreatorLock)
            {
                if (m_ViewCreator == null)
                {
                    m_ViewCreator = new LogViewCreator();	// must produce on UI thread.
                    m_ViewCreator.Show();
                    m_ViewCreator.Hide();
                }
            }

            // initialize our queues
            m_ThreadPriority = ThreadPriority.Lowest;       // Reset Hub thread to lowest priority.
            m_OutQueue = new Queue<string>(m_QueueSize);

            m_EventStorage = new Queue<LogEventArgs>(m_EventStorageSize);
            for (int i = 0; i < m_EventStorageSize; ++i)
                m_EventStorage.Enqueue(new LogEventArgs());


            // Start our clock.
            string clockOffsetMessage;
            TimeSpan offset;
            Misty.Lib.Utilities.NistServices nist = null;
            bool isConnectToNIST = false;
            try
            {
                nist = Misty.Lib.Utilities.NistServices.GetInstance(isConnectToNIST);
            }
            catch (Exception e)
            {
                Console.WriteLine("LogHub: " + e.Message);
            }
            if (nist != null && nist.IsGood)
            {
                offset = nist.SystemTimeOffset;
                clockOffsetMessage = String.Format("Using NIST-corrected system time, with offset = {0} for start time.", offset.ToString());
            }
            else
            {
                offset = new TimeSpan(0);
                clockOffsetMessage = String.Format("Using local system time for start time.", offset.ToString());
            }
            m_StopWatch.Start();
            m_StartTime = DateTime.Now.Add(offset);

            // Send a starting message to log.
            string msg = string.Format("{0} started at {2} on {1}. ", LogName, m_StartTime.ToLongDateString(), m_StartTime.ToLongTimeString());
            HubEventEnqueue(GetNewLogEvent(LogRequest.NewMessage, msg, LogLevel.Major, m_StopWatch.Elapsed));
            HubEventEnqueue(GetNewLogEvent(LogRequest.NewMessage, clockOffsetMessage, LogLevel.Major, m_StopWatch.Elapsed));

            // Send nist messages.
            string[] nistMsgs = nist.Messages;
            foreach (string msg2 in nistMsgs)
                HubEventEnqueue(GetNewLogEvent(LogRequest.NewMessage, msg2, LogLevel.Major, m_StopWatch.Elapsed));


            base.Start();   // starts thread.
        }//end Initialize().
        //
        //
        //
        //
        //
        //		
        //
        //
        //
        #endregion//end constructors


        #region Public Methods
        // *********************************************************************
        // ****                      Public Methods                         ****
        // *********************************************************************
        //
        //
        //
        // *************************************
        // ****         NewEntry()          ****
        // *************************************
        /// <summary>
        /// The most basic way to create a single, new log entry.
        /// </summary>
        /// <param name="msgType"></param>
        /// <param name="formatString"></param>
        /// <param name="args"></param>
        public void NewEntry(LogLevel msgLevel, string formatString, params object[] args)
        {
            if ((msgLevel & m_AllowedMessageMask) == msgLevel)	// check msgLevel threshold
            {
                string msg = string.Format(formatString, args);
                //#if (DEBUG)
                //Console.WriteLine(msg);
                //#endif
                HubEventEnqueue(GetNewLogEvent(LogRequest.NewMessage, msg, msgLevel, m_StopWatch.Elapsed));
            }
        }
        public void NewEntry(LogLevel msgLevel, string fmt, object arg)
        {
            if ((msgLevel & m_AllowedMessageMask) == msgLevel)
            {
                string msg = string.Format(fmt, arg);
                //#if (DEBUG)
                //    Console.WriteLine(msg);
                //#endif
                HubEventEnqueue(GetNewLogEvent(LogRequest.NewMessage, msg, msgLevel, m_StopWatch.Elapsed));
            }
        }//NewEntry().
        //
        //
        //
        // *************************************
        // ****         BeginEntry()        ****
        // *************************************
        /// <summary>
        /// This method is called when the user wants to create a compound message in the Log.
        /// First this method is called, followed by AppendEntry() repeatedly, and then EndEntry()
        /// completes the message and submits it.
        /// This is thread safe since each caller thread has its own working message space.
        /// </summary>
        /// <param name="msgType"></param>
        /// <returns></returns>
        public bool BeginEntry(LogLevel msgType)
        {            
            if ((msgType & m_AllowedMessageMask) == msgType)
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                if (!m_WorkingMessage.ContainsKey(id))
                {													// The first time thread called this method...
                    m_WorkingMessage.Add(id, new StringBuilder(512));  // ... create a work space for this thread.
                    m_IsWorkingMessage.Add(id, true);
                }
                m_IsWorkingMessage[id] = true;
                return true;
            }
            return false;
        }//end BeginEntry().
        //
        public bool BeginEntry(LogLevel msgType, string message)
        {
            if ((msgType & m_AllowedMessageMask) == msgType)
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                if (!m_WorkingMessage.ContainsKey(id))
                {													// The first time thread called this method...
                    m_WorkingMessage.Add(id, new StringBuilder(512));  // ... create a work space for this thread.
                    m_IsWorkingMessage.Add(id, true);
                }
                m_IsWorkingMessage[id] = true;
                // Now start message.
                m_WorkingMessage[id].Append(message);
                return true;
            }
            return false;
        }//end BeginEntry().
        //
        public bool BeginEntry(LogLevel msgType, string format, params object[] args)
        {
            if ((msgType & m_AllowedMessageMask) == msgType)
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                if (!m_WorkingMessage.ContainsKey(id))
                {													// The first time thread called this method...
                    m_WorkingMessage.Add(id, new StringBuilder(512));  // ... create a work space for this thread.
                    m_IsWorkingMessage.Add(id, true);
                }
                m_IsWorkingMessage[id] = true;
                // Now start message.
                m_WorkingMessage[id].AppendFormat(format, args);
                return true;
            }
            return false;
        }//end BeginEntry().
        //
        public bool BeginEntry(LogLevel msgType, string format, object arg)
        {
            if ((msgType & m_AllowedMessageMask) == msgType)
            {
                int id = Thread.CurrentThread.ManagedThreadId;
                if (!m_WorkingMessage.ContainsKey(id))
                {													    // The first time thread called this method...
                    m_WorkingMessage.Add(id, new StringBuilder(512));  // ... create a work space for this thread.
                    m_IsWorkingMessage.Add(id, true);
                }
                m_IsWorkingMessage[id] = true;
                // Now start message.
                m_WorkingMessage[id].AppendFormat(format, arg);
                return true;
            }
            return false;
        }//end BeginEntry().
        //
        //
        //
        // *************************************
        // ****         AppendEntry()        ****
        // *************************************
        public void AppendEntry(string format, params object[] args)
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            if (m_IsWorkingMessage.ContainsKey(id) && m_IsWorkingMessage[id] && m_WorkingMessage.ContainsKey(id))
                m_WorkingMessage[id].AppendFormat(format, args);
        }
        //
        public void AppendEntry(string format, object arg)
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            if (m_IsWorkingMessage.ContainsKey(id) && m_IsWorkingMessage[id] && m_WorkingMessage.ContainsKey(id))
                m_WorkingMessage[id].AppendFormat(format, arg);
        }
        //
        public void AppendEntry(string message)
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            if (m_IsWorkingMessage.ContainsKey(id) && m_IsWorkingMessage[id] && m_WorkingMessage.ContainsKey(id))
                m_WorkingMessage[id].Append(message);
        }
        //
        // *************************************
        // ****        InsertEntry()        ****
        // *************************************
        /// <summary>
        /// This method inserts a string at front of the working message.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void InsertEntry(string format, params object[] args)
        {
            this.InsertEntry(0, format, args);
        }//InsertEntry()
        public void InsertEntry(int startIndex, string format, params object[] args)
        {
            string msg = string.Format(format, args);
            int id = Thread.CurrentThread.ManagedThreadId;
            if (m_IsWorkingMessage.ContainsKey(id) && m_IsWorkingMessage[id] && m_WorkingMessage.ContainsKey(id))
                m_WorkingMessage[id].Insert(startIndex, msg);
        }//InsertEntry()
        //
        //
        // *************************************
        // ****         EndEntry()          ****
        // *************************************
        /// <summary>
        /// This is called after BeingEntry() and possibly multiple AppendEntry() methods
        /// have been called to finalize the compound message and finally push onto thread queue
        /// for processing.
        /// </summary>
        /// <param name="acceptEntry"></param>
        public void EndEntry(bool acceptEntry)
        {
            int id = Thread.CurrentThread.ManagedThreadId;	// id of calling thread.
            if (m_IsWorkingMessage.ContainsKey(id) && m_IsWorkingMessage[id] && m_WorkingMessage.ContainsKey(id))
            {
                if (acceptEntry && m_WorkingMessage[id].Length > 0)
                {   // User wants to accept this entry for publication.
                    LogLevel level = LogLevel.Minor;
                    m_WorkingMessageLevel.TryGetValue(id, out level);
                    HubEventEnqueue(GetNewLogEvent(LogRequest.NewMessage, m_WorkingMessage[id].ToString(), level, m_StopWatch.Elapsed));
                }
                m_WorkingMessage[id].Remove(0, m_WorkingMessage[id].Length);	// Remove old msg.
                m_IsWorkingMessage[id] = false;									// flag as no longer working on a msg.
            }
        }//EndEntry().
        public void EndEntry()
        {
            EndEntry(true);			// default behavior is to accept entry.
        }
        public void Flush()
        {
            HubEventEnqueue(GetNewLogEvent(LogRequest.FlushNow, string.Empty, LogLevel.None, m_StopWatch.Elapsed));
        }
        //
        //
        //
        // *****************************************
        // ****			GetEntry()				****
        // *****************************************
        /// <summary>
        /// Allows user to get a copy of full msg being constructed.
        /// </summary>
        /// <returns></returns>
        public string GetEntry()
        {
            string message = String.Empty;
            int id = Thread.CurrentThread.ManagedThreadId;
            if (m_IsWorkingMessage.ContainsKey(id) && m_IsWorkingMessage[id] && m_WorkingMessage.ContainsKey(id))
            {
                message = m_WorkingMessage[id].ToString();
            }
            return message;
        }//GetEntry().
        //
        //
        // *****************************************
        // ****			GetTime()				****
        // *****************************************	
        public DateTime GetTime()
        {
            return m_StartTime.Add(m_StopWatch.Elapsed);
        }// GetTime()
        
        public void ProcessCopyTo(string targetDir)
        {
            LogEventArgs e;
            e = GetNewLogEvent(LogRequest.CopyTo, targetDir, LogLevel.Major, m_StopWatch.Elapsed);
            HubEventEnqueue(e);
        }
        //
        //
        //
        //
        // *************************************************
        // ****                 Stop()                  ****
        // *************************************************
        /// <summary>
        /// This method is called by an external thread.
        /// Since all file handling is done asynchronously, we have to request a stop
        /// to the Log keeping service.
        /// </summary>
        protected override void Stop()
        {
            LogEventArgs e;
            e = GetNewLogEvent(LogRequest.Stop, string.Empty, LogLevel.Major, m_StopWatch.Elapsed);
            HubEventEnqueue(e);
        }//end Stop().
        //
        public override void RequestStop()
        {
            LogEventArgs e;
            e = GetNewLogEvent(LogRequest.Stop, string.Empty, LogLevel.Major, m_StopWatch.Elapsed);
            HubEventEnqueue(e);
        }//end Stop().
        //
        //
        /// <summary>
        /// TODO: This does not seem to be thread-safe!!  Can't we be writing to LogViewer after
        /// user has closed and destroyed object?
        /// </summary>
        /// <param name="viewerToRemove"></param>
        public void RemoveLogViewer(LogViewer viewerToRemove)
        {
            if (viewerToRemove == m_LogViewer)
            {
                m_IsViewActive = false;
                m_LogViewer = null;			// release this viewer.
                return;
            }
            lock (m_ErrorViewLock)
            {
                if (viewerToRemove == m_ErrorView)
                    m_ErrorView = null;		// release this viewer
            }
        }
        //
        //
        //
        // *************************************************
        // ****                 ToString()              ****
        // *************************************************
        public override string ToString()
        {
            StringBuilder msg = new StringBuilder();
            msg.AppendFormat("{0}", m_HubName);
            return msg.ToString();
        }//ToString()
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *********************************************************************
        // ****                     Private Methods                         ****
        // *********************************************************************
        //
        //
        //
        /// <summary>
        /// Copy the file to target directory.
        /// </summary>
        /// <param name="targetDirectory"></param>
        private void CopyFileToTarget(string targetDirectory)
        {
            string targetFileName;
            string fileName;

            // Try to create log directory in a centralized place to user directory.
            lock (m_CopyDirectoryLock)
            {
                if (!System.IO.Directory.Exists(targetDirectory))
                    System.IO.Directory.CreateDirectory(targetDirectory);

                // Try to create log directory in user directory.
                targetDirectory = string.Format("{0}{1}\\", targetDirectory, "Logs");
                if (!System.IO.Directory.Exists(targetDirectory))
                    System.IO.Directory.CreateDirectory(targetDirectory);

                // Save file to the target directory.
                fileName = m_OutputFileName.Substring(m_OutputFileName.LastIndexOf("\\") + 1,
                    m_OutputFileName.Length - (m_OutputFileName.LastIndexOf("\\") + 1));
                string date = DateTime.Now.ToString("yyyyMMdd");
                targetDirectory = string.Format("{0}{1}\\", targetDirectory, date);

                if (!System.IO.Directory.Exists(targetDirectory))
                    System.IO.Directory.CreateDirectory(targetDirectory);

                targetFileName = string.Format("{0}{1}", targetDirectory, fileName);
            }

            // Copy files and overwrite if possible.
            System.IO.File.Copy(m_OutputFileName, targetFileName, true);
        }

        // *************************************************************
        // ****					Create OutputFileName				****
        // *************************************************************
        /// <summary>
        /// Creates a unique output file name based on the hub name, the startup time, 
        /// and makes sure its unique.
        /// Called by the local hub thread.
        /// </summary>
        private void CreateOutputFileName()
        {
            string fileName = LogName.Replace(' ', '_') + ".txt";	// Create filename based on hub name.
            if (String.IsNullOrEmpty(m_OutputDirName))
                m_OutputDirName = Directory.GetCurrentDirectory() + "\\";
            else if (!Directory.Exists(m_OutputDirName))
                Directory.CreateDirectory(m_OutputDirName);
            string timeString = m_StartTime.ToString("HHmm_");
            m_OutputFileName = m_OutputDirName + "Log_" + timeString + fileName;	// basic name.
            int dupeFileID = 0;
            while (File.Exists(m_OutputFileName))		// Make sure the file name is unique.
            {
                m_OutputFileName = m_OutputDirName + "Log_" + timeString + string.Format("{0}_", dupeFileID.ToString()) + fileName;
                dupeFileID++;							// increase index until we find a non-used file name.
            }
        }//end CreateOutputFileName().
        //
        //
        // *************************************************************
        // ****                 Open OutputFile()                   ****
        // *************************************************************
        /// <summary>
        /// Opens an output log file, named in m_OutputFileName.  
        /// This method should make multiple attempts to open the output file before giving up.
        /// Called by local thread.
        /// </summary>
        private bool TryOpenOutputFile(out StreamWriter streamWriter)
        {
            // Validate our fileName
            if (string.IsNullOrEmpty(m_OutputFileName))
                CreateOutputFileName();	// create a new unique name if basic one already exists.
            try
            {
                streamWriter = new StreamWriter(m_OutputFileName, true);	// Attempt to open 		
            }
            catch (Exception e)
            {
                Console.WriteLine("LogHub exception. {0}", e.Message);
                streamWriter = null;
                return false;
            }
            return true;
        }//end OpenOutFile().
        //
        //
        // *************************************************
        // ****             Flush OutQueue()            ****
        // *************************************************
        /// <summary>
        /// This private method is called by the HubThread whenever we want to 
        /// write the current contents of the out buffer to the out file.
        /// </summary>
        protected void FlushOutQueue()
        {
            StreamWriter stream;
            if (TryOpenOutputFile(out stream))
            {	// Flush queue, write all messages in queue.
                foreach (string aLine in m_OutQueue)
                    stream.WriteLine(aLine);
                stream.Close();
                m_OutQueue.Clear();
            }
        }//end FlushOutQueue().
        //
        //
        // *************************************************
        // ****             ProcessStopRequest()        ****
        // *************************************************
        /// <summary>
        /// Called by the LogHub thread when a request to shut down the log
        /// has been received.  This method will do a friendly shut down.
        /// </summary>
        protected void ProcessStopRequest()
        {
            /*
            string msg = string.Format("{0} stopping.", LogName);
            LogEventArgs eventArgs = GetNewLogEvent(LogRequest.NewMessage, msg, LogLevel.Major, m_StopWatch.Elapsed);
            m_OutQueue.Enqueue(BuildMessage(eventArgs));
            FlushOutQueue();
            m_StopWatch.Stop();
            // Exit.
            base.Stop();
             */ 
        }//end ProcessStopRequest().
        //
        //
        //
        // *************************************************
        // ****             Build Message()             ****
        // *************************************************
        /// <summary>
        /// In new approach, the LogEventArg contains a timespan computed 
        /// using the StopWatch when the event was created - since thats the time the
        /// event was triggered.
        /// This is called by the internal hub thread.  As such, it is completely threadsafe.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>Completed message </returns>
        protected string BuildMessage(LogEventArgs e)
        {
            DateTime time = m_StartTime.Add(e.TimeElapsedFromStart);
            int fracOfSec = (time.Millisecond);    // most significant decimial - 
            //string outMsg = String.Format("{0}:{1}:{2}.{3} {4}({5}): {6}",
            //	time.Hour.ToString("00"), time.Minute.ToString("00"),
            //	time.Second.ToString("00"), fracOfSec.ToString("000")
            //	,LogName, (++MessageID).ToString(), e.Message);  
            //*
            string outMsg = String.Format("{0}:{1}:{2}.{3} ({4}) {5}",
                time.Hour.ToString("00"), time.Minute.ToString("00"),
                time.Second.ToString("00"), fracOfSec.ToString("000")
                , (++m_MessageID).ToString(), e.Message);
            //*/
            /*
            string outMsg = String.Format("{0}:{1}:{2}.{3} ({4} {6}): {5}",
                time.Hour.ToString("00"), time.Minute.ToString("00"),
                time.Second.ToString("00"), fracOfSec.ToString("000")
                , (++m_MessageID).ToString(), e.Message , e.LastCount.ToString());  
            */
            // Exit.
            return outMsg;
        }//end BuildMessage().
        //
        //
        //        
        //
        //
        // *************************************************************
        // ****					Process Out Message()				****
        // *************************************************************
        protected void ProcessOutMessage(LogEventArgs e)
        {
            // Extract message from event.
            string msg = BuildMessage(e);

            // Process the message.
            if (!String.IsNullOrEmpty(msg))
            {
                m_OutQueue.Enqueue(msg);								// push non-empty messages to outQutu
                if (m_IsViewActive == true && m_LogViewer != null)		// Output to log viewer.
                    m_LogViewer.AddMessage(msg);   // push onto gui.

                // Process errors and warnings.
                //if ((e.Level & LogLevel.ShowWarningMessages) != 0x0000 )
                if ((e.Level & LogLevel.ShowErrorMessages) != 0x0000)	// popup window for errors only.
                {	// Open ErrorViewer and display this message.
                    lock (m_ErrorViewLock)
                    {
                        if (m_ErrorView == null)
                        {
                            if (m_ViewCreator != null)
                                m_ViewCreator.CreateInvoke(this, msg);
                        }
                        else
                            m_ErrorView.AddMessage(msg);		// Thread-safety: if user is exiting window, we may lose this message! But shouldn't crash.
                    }
                }


            }
            // Optionally flush the queue now.
            int nMsgs = m_OutQueue.Count;
            if (nMsgs > m_WriteThreshold * m_QueueSize && nMsgs % 10 == 0)
                FlushOutQueue();					// write to file, when buffer full, and only on every tenth message.
        }// ProcessOutMessage()
        //
        //
        // *************************************************************
        // ****					Get New LogEvent()					****
        // *************************************************************
        /// <summary>
        /// When the outside thread wants to make a request, he calls this method which 
        /// rather than creating  new event, simply takes the next one from off a stack of event args. 
        /// The hub thread returns used eventArgs back to the stack after ProcessOutMessage().
        /// </summary>
        /// <param name="req"></param>
        /// <param name="message"></param>
        /// <param name="timeElapsed"></param>
        /// <returns></returns>
        protected LogEventArgs GetNewLogEvent(LogRequest req, string message, LogLevel level, TimeSpan timeElapsed)
        {
            // Get an empty eventArg object
            LogEventArgs eventArg;
            int n;
            lock (m_EventStorageLock)
            {
                n = m_EventStorage.Count;
                if (n > 0)
                    eventArg = m_EventStorage.Dequeue();
                else
                    eventArg = new LogEventArgs();
            }
            // Load the eventArg object
            eventArg.Request = req;
            eventArg.Level = level;
            eventArg.Message = message;
            eventArg.TimeElapsedFromStart = timeElapsed;
            eventArg.LastCount = n;		// number of eventARgs in storage. For debugging.
            return eventArg;
        }//
        //
        //
        // *****************************************************
        // ****             CreateLogViewer()               ****
        // *****************************************************
        /// <summary>
        /// 
        /// </summary>
        private void CreateLogViewer()
        {
            if (m_LogViewer == null && m_GuiCreator == null)
            {
                m_GuiCreator = FrontEnds.GuiCreator.Create(typeof(LogViewer), this);
                m_GuiCreator.FormCreated += new EventHandler(GuiCreator_FormCreated);
                m_GuiCreator.Start();
            }
        }// CreateLogViewer()
        //
        //
        private void GuiCreator_FormCreated(object sender, EventArgs eventArgs)
        {
            m_GuiCreator = null;            // let go of this gui creator.
            if (m_LogViewer == null)
            {
                FrontEnds.GuiCreator.CreateFormEventArgs e = (FrontEnds.GuiCreator.CreateFormEventArgs)eventArgs;
                if (e.CreatedForm is LogViewer)
                {
                    m_LogViewer = (LogViewer)e.CreatedForm;
                    this.IsViewActive = true;
                }
            }
        }// CreateLogViewer()
        //
        //
        //
        #endregion//private methods


        #region HubBase overrides
        // *********************************************************
        // ****                 HubEvent Handler                ****
        // *********************************************************
        /// <summary>
        /// This method is called by the LogHub thread to process a new log message 
        /// or a special log request (such as "flush buffers" etc).
        /// </summary>
        /// <param name="e"></param>
        protected override void HubEventHandler(EventArgs[] eventArgArray)
        {
            if (eventArgArray == null) { return; }  // this can happen as the thread is dying. See HubBase.WaitListen()
            foreach (EventArgs eventArg in eventArgArray)
            {
                if ((eventArg != null) && (eventArg != EventArgs.Empty))
                {
                    LogEventArgs logArg = (LogEventArgs)eventArg;
                    switch (logArg.Request)
                    {
                        case LogRequest.FlushNow:
                            FlushOutQueue();
                            break;
                        case LogRequest.Stop:
                            //ProcessStopRequest();
                            string msg = string.Format("{0} stopping.", LogName);
                            LogEventArgs eventArgs = GetNewLogEvent(LogRequest.NewMessage, msg, LogLevel.Major, m_StopWatch.Elapsed);
                            m_OutQueue.Enqueue(BuildMessage(eventArgs));
                            FlushOutQueue();
                            m_StopWatch.Stop();
                            if (m_LogViewer != null)
                                m_LogViewer.LogClose(this, null);
                            LogHub.m_ViewCreator = null;		// disconnect myself.
                            // Close
                            bool needToCloseErrorView = false;
                            lock (m_ErrorViewLock)
                            {
                                if (m_ErrorView != null)
                                    needToCloseErrorView = true;
                            }
                            if (needToCloseErrorView)
                                m_ErrorView.LogClose(this, EventArgs.Empty);

                            base.Stop();
                            break;
                        case LogRequest.NewMessage:
                            ProcessOutMessage(logArg);
                            break;
                        case LogRequest.CopyTo:
                            FlushOutQueue();

                            string targetPath = logArg.Message;
                            if (!targetPath.EndsWith("\\"))
                                targetPath += "\\";
                            CopyFileToTarget(targetPath);
                            break;
                        default:
                            break;
                    }//end switch.
                    lock (m_EventStorageLock)
                    {
                        m_EventStorage.Enqueue(logArg);			// push back into the storage area.
                    }
                }
            }//next eventArg.
        }//end HubEventHander().
        //
        //
        protected override void UpdatePeriodic()
        {
            if ( m_OutQueue.Count > 0 )
                this.FlushOutQueue();
        }
        //
        #endregion//end HubBase overrides.


        #region Event Objects
        // *********************************************************
        // ****                 Event Objects                   ****
        // *********************************************************
        //
        //
        // ****             Log EventArgs               ****
        //
        protected class LogEventArgs : EventArgs
        {
            public LogRequest Request;
            public LogLevel Level = LogLevel.None;
            public string Message;
            public TimeSpan TimeElapsedFromStart = TimeSpan.Zero;
            public int LastCount;
            //public object ObjectPtr = null;

            public LogEventArgs()
            {
                
            }
        }//end Log Event Args
        //
        //
        // ****             Log Event Types             ****
        //
        protected enum LogRequest
        {
            FlushNow = 0
            ,NewMessage = 1
            ,Stop = 2
            ,CopyTo =3
        }
        //
        #endregion//end Event Objects

    }//end class.
}//end Namespace.

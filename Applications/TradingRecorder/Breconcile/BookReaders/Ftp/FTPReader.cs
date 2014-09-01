using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Breconcile.Ftp
{
    using Misty.Lib.Application;
    using Misty.Lib.Hubs;

    using Renci.SshNet;
    using Renci.SshNet.Sftp;

    public class FtpReader : Hub
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Services
        private AppInfo m_AppInfo = null;

        // sFTP control variables
        private const int m_PortID = 34022;
        private const string m_Host = "66.150.110.84";
        private const string m_UserName = "bretrading";
        private string m_PrivateKeyFileName = "bretrading.priv";
        private PrivateKeyFile m_PrivateKeyFile = null;                     // key file pointer

        // Target file patterns - sync files that contain these keywords.
        public string[] m_FilePatterns = new string[] { "POS" };           // desired file name patterns.
        public string[] m_FileNameSuffixes = new string[] { "CSV" };       // desired file extensions

        // Failed requests
        private System.Timers.Timer m_Timer = null;
        private List<RequestEventArgs> m_WaitingRequests = new List<RequestEventArgs>();    // time provided is give-up time.
        

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FtpReader(bool isLogViewDesired)
            : base("FTPCollector", AppInfo.GetInstance().LogPath, isLogViewDesired, LogLevel.ShowAllMessages)
        {
            m_AppInfo = AppInfo.GetInstance();


            double delayMinutes = 15;
            m_Timer = new System.Timers.Timer(1000.0 * 60 * delayMinutes);
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
        public bool Request( RequestEventArgs request )
        {
            return this.HubEventEnqueue(request);
        }
        public override void RequestStop()
        {
            this.HubEventEnqueue(new RequestEventArgs(RequestType.Stop));
        }

        //
        //
        //
        //
        //
        #endregion//Public Methods



        #region Hub Event Handler 
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        protected override void HubEventHandler(EventArgs[] eventArgList)
        {
            foreach (EventArgs e in eventArgList)
            {
                Type eventType = e.GetType();
                if (eventType == typeof(RequestEventArgs))
                    ProcessRequest((RequestEventArgs)e);
                else
                    Log.NewEntry(LogLevel.Major, "HubEventHandler: Unknown event type {0}. event = {1}", eventType, e);
            } 
        }// HubEventHandler()
        //
        //
        // ****             ProcessRequest()                    ****
        //
        private void ProcessRequest(RequestEventArgs eventArg)
        {
            if (eventArg.Type == RequestType.GetNewFiles)
            {

                List<string> filesFound;
                if (TryCopyNewFilesToLocal(out filesFound))
                {   // Success!
                    eventArg.Status = RequestStatus.Success;
                    eventArg.Data = new List<object>(filesFound);               // pass the file names copied back to subscribers.
                    lock (m_WaitingRequests)
                    {
                        if (m_WaitingRequests.Contains(eventArg))
                            m_WaitingRequests.Remove(eventArg);
                    }
                }
                else
                {   // Failure
                    if (DateTime.Now.CompareTo(eventArg.GiveUpTime) > 0)
                    {   // Give up!
                        eventArg.Status = RequestStatus.Failed;
                        lock (m_WaitingRequests)
                        {
                            if (m_WaitingRequests.Contains(eventArg))
                                m_WaitingRequests.Remove(eventArg);
                        }
                    }
                    else
                    {   // Keep trying
                        eventArg.Status = RequestStatus.StillWorking;
                        lock (m_WaitingRequests)
                        {
                            if (!m_WaitingRequests.Contains(eventArg))
                                m_WaitingRequests.Add(eventArg);
                        }
                    }
                }
                // Report results
                OnRequestCompleted(eventArg);
                Log.NewEntry(LogLevel.Major, "ProcessRequest: {0} {1}.", eventArg, eventArg.Status);
                
            }
            else if (eventArg.Type == RequestType.Stop)
            {
                if (m_Timer != null)
                {
                    m_Timer.Stop();
                    m_Timer.Elapsed -= new System.Timers.ElapsedEventHandler(Timer_Elapsed);
                    m_Timer = null;
                }
                base.Stop();
            }
            else
                Log.NewEntry(LogLevel.Major, "ProcessRequest: RequestType {0} not implemented.", eventArg.Type);
        }// ProcessRequest()
        //
        //
        //
        //
        //
        //
        // ****                     Timer_Elapsed()                 ****
        //
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs eventArgs)
        {
            m_Timer.Enabled = false;            // stop the timer - if any 
            lock (m_WaitingRequests)
            {
                while (m_WaitingRequests.Count > 0)
                {
                    RequestEventArgs request = m_WaitingRequests[0];
                    m_WaitingRequests.RemoveAt(0);
                    this.HubEventEnqueue(request);
                }                
            }
            m_Timer.Enabled = true;
        }// Timer_Elapsed()
        //
        //
        //
        #endregion//Event Handlers



        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        /// <summary>
        /// This 
        /// </summary>
        /// <returns>true if new files were found.</returns>
        private bool TryCopyNewFilesToLocal(out List<string> localFileNamesCopied)
        {
            localFileNamesCopied = null;

            // Prepare for ftp connection.
            if (!TryInitialize())
                return false;

            // Get list of files on ftp server.
            List<SftpFile> ftpFileList = null;
            if (!TryGetFileNames(out ftpFileList))
                return false;

            // Read local files.
            string statementPath = string.Format("{0}{1}", m_AppInfo.UserPath, "Statements\\");
            if (!System.IO.Directory.Exists(statementPath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(statementPath);
                }
                catch (Exception ex)
                {
                    Log.NewEntry(LogLevel.Error, "Cannot find statement directory {0}.  Exception {1}", statementPath, ex.Message);
                    return false;
                }
            }
            List<string> localFileList = new List<string>();
            string[] fileName = System.IO.Directory.GetFiles(statementPath);
            foreach (string s in fileName)
            {
                int nPtr = s.LastIndexOf('\\');
                localFileList.Add(s.Substring(nPtr + 1, s.Length - (nPtr + 1)));            // keep file name only.   
            }

            //
            // Determine files we are missing.
            //
            List<SftpFile> filesWeWant = new List<SftpFile>();
            Log.BeginEntry(LogLevel.Minor, "Discovered");
            foreach (SftpFile ftpFile in ftpFileList)                                       // loop thru files on ftp server...
            {
                string ftpFileName = ftpFile.Name;
                bool isFileOfInterest = true;
                if (isFileOfInterest && m_FilePatterns.Length > 0)                          // test filename prefix.
                {
                    isFileOfInterest = false;
                    foreach (string prefix in m_FilePatterns)
                        isFileOfInterest = isFileOfInterest || ftpFileName.ToUpper().Contains(prefix);
                }
                if (isFileOfInterest && m_FileNameSuffixes.Length > 0)                      // test filename suffix
                {
                    isFileOfInterest = false;
                    foreach (string suffix in m_FileNameSuffixes)
                        isFileOfInterest = isFileOfInterest || ftpFileName.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase);
                }
                if (isFileOfInterest && !localFileList.Contains(ftpFileName))
                {
                    Log.AppendEntry(" {0}", ftpFile.Name);
                    filesWeWant.Add(ftpFile);
                }
            }// next ftpFile
            Log.AppendEntry(". {0} files.", filesWeWant.Count);
            Log.EndEntry();

            //
            // Try to get from FTP server the files we are missing.
            //
            localFileNamesCopied = new List<string>();
            List<string> linesRead = new List<string>();
            SftpClient client;
            using (client = new SftpClient(m_Host, m_PortID, m_UserName, m_PrivateKeyFile))
            {
                try
                {
                    client.Connect();
                    foreach (SftpFile file in filesWeWant)
                    {
                        // Read ftp file.
                        linesRead.Clear();
                        linesRead.AddRange(client.ReadLines(file.Name));
                        // Write local file.
                        string localFileName = string.Format("{0}{1}", statementPath, file.Name);
                        System.IO.File.WriteAllLines(localFileName, linesRead);
                        Log.NewEntry(LogLevel.Minor, "Downloaded file {0}.", localFileName);
                        localFileNamesCopied.Add(localFileName);
                    }
                    client.Disconnect();
                }
                catch (Exception ex)
                {
                    Log.NewEntry(LogLevel.Major, "Failed to read. {0}", ex.Message);
                    return false;
                }
            }//using ftpclient
            Log.NewEntry(LogLevel.Minor, "Finished getting all files.");
            return true;
        } // TryCopyNewFilesToLocal()
        //
        //
        //
        // *************************************************************
        // ****                 Try Initialize()                    ****
        // *************************************************************
        /// <summary>
        /// Confirms existance and sets up private key file.
        /// </summary>
        /// <returns></returns>
        private bool TryInitialize()
        {
            // Create the private key file.
            string privateKeyFilePath = string.Format("{0}{1}", m_AppInfo.UserPath, m_PrivateKeyFileName);
            if (!System.IO.File.Exists(privateKeyFilePath))
            {
                Log.NewEntry(LogLevel.Warning, "Failed to locate the private key file at {0}.", privateKeyFilePath);
                return false;
            }
            m_PrivateKeyFile = new PrivateKeyFile(privateKeyFilePath);
            return true;
        }// TryInitialize()
        //
        //
        //
        //
        // ****             TryGetFiles()               ****
        //
        /// <summary>
        /// Returns a list of all files found on the ftp server.
        /// </summary>
        private bool TryGetFileNames(out List<Renci.SshNet.Sftp.SftpFile> fileList)
        {
            fileList = null;
            SftpClient client;
            using (client = new SftpClient(m_Host, m_PortID, m_UserName, m_PrivateKeyFile))
            {
                try
                {
                    client.Connect();
                    Log.NewEntry(LogLevel.Major, "Connected to {0}", m_Host);
                    string workingDir = client.WorkingDirectory;
                    var listDirectory = client.ListDirectory(workingDir);
                    fileList = new List<Renci.SshNet.Sftp.SftpFile>(client.ListDirectory(workingDir));
                    client.Disconnect();
                }
                catch (Exception ex)
                {
                    Log.NewEntry(LogLevel.Major, "Failed to connect. {0}", ex.Message);
                    return false;
                }
            }
            // Exit
            return true;
        } // TryConnect()
        //
        //
        //
        //
        #endregion//Private Methods



        #region Event Triggers
        // *****************************************************************
        // ****                     Event Triggers                      ****
        // *****************************************************************
        public event EventHandler RequestCompleted;
        //
        private void OnRequestCompleted(EventArgs e)
        {
            if (RequestCompleted != null)
                RequestCompleted(this, e);
        }
        //
        //
        //
        #endregion// Event Triggers


    }
}

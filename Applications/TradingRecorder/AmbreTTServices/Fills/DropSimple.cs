using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Ambre.TTServices.Fills
{
    using InstrumentName = Misty.Lib.Products.InstrumentName;
    using Misty.Lib.Application;
    using Misty.Lib.Hubs;
    using Misty.Lib.IO.Xml;                         // for Istringifiable
    using Misty.Lib.IO;                             // for DropQueue

    using Ambre.TTServices;
    using Ambre.TTServices.Markets;
    using TradingTechnologies.TTAPI.Tradebook;

    /// <summary>
    /// TODO: This is the simplified, single threaded (single DropQueueWriter) writing of drop files.
    /// This version only writes a single archived/dated drop file to the Drops/yyyyMMdd directory.
    /// </summary>
    public class DropSimple : IDisposable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // External services
        private FillHub m_FillHub = null;
        private DropQueueWriter m_ArcBookWriter = null;             // for writing books + fills to archive.
        public bool IsBookWriterRunning = false;


        // Used to create drop file name.
        private string UniqueUserName;                              // unique identifier to distiguish between multiple fillhubs (accts)
        private DateTime DropFileStartDateTime = DateTime.MinValue; // time the current file was started.

        // Drop timing controls
        public DateTime LastBookDrop = DateTime.MinValue;           // last time we dropped the books.
        private DateTime m_LocalTimeLast = DateTime.MinValue;       // last time something was enqueued for writing (a fill usually).
        public TimeSpan BookDropPeriod = new TimeSpan(0, 60, 0);
        public TimeSpan FillDelayPeriod = new TimeSpan(0, 1, 0);

        private Dictionary<Type, string[]> m_StringifyOverrideTable = null;

        //
        // File & directory formatting etc.
        //
        public static string DropType_FillBook = "FillBooks";
        public static string FileSuffix = "txt";
        private bool PushToRepository = true;                       // will we copy to repository?
        //public static string BreRepositoryPath = "\\\\fileserver\\Users\\dv_bre\\Ambre\\Drops\\";
        //public static string BreRepositoryPath = "\\\\fileserver\\Users\\DV_Ambre\\AmbreUsers\\";
        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// After constructing the drop rules, one can load a previous file or change
        /// parameters before writing.  The writers are created only after calling "Start()".
        /// </summary>
        /// <param name="fillHub"></param>
        public DropSimple(FillHub fillHub)
        {
            // Create the unique user name.
            TTApiService api = TTApiService.GetInstance();
            this.UniqueUserName = string.Format("{0}_{1}", api.LoginUserName, fillHub.Name);
            this.m_FillHub = fillHub;
            //this.DropFileStartDateTime = fillHub.Log.GetTime();               // time stamp "label" used for archiving current drop file.

            m_StringifyOverrideTable = new Dictionary<Type, string[]>();
            m_StringifyOverrideTable.Add(m_FillHub.GetType(), new string[] { string.Empty, "GetAttributesDrop", "GetElementsDrop" });

            this.LastBookDrop = fillHub.Log.GetTime().Subtract(this.BookDropPeriod).AddMinutes(30);
            this.m_LocalTimeLast = this.LastBookDrop;
        }
        //
        //       
        //
        //
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // *****************************************************
        // ****                 Start()                     ****
        // *****************************************************
        /// <summary>
        /// Starts the drop service that will periodically write whatever is in its queue.
        /// The user might want to load the old drop file for positions before starting
        /// to write and update a new one.  No new drop files are created until Start() is called.
        /// </summary>
        public void Start()
        {
            // Acrhival writer
            //DropFileStartDateTime = m_FillHub.Log.GetTime();            // This is time stamp of the next archive file to start.
            //string archivalPath = DropSimple.GetArchivePath(DropFileStartDateTime, Misty.Lib.Application.AppInfo.GetInstance().DropPath);// Creates starting time stamp (now's date) in arcPath name.
            //string fileName = this.GetArchiveFileName(DropFileStartDateTime, DropType_FillBook);
            //this.m_FillHub.Log.NewEntry(LogLevel.Major, "DropRules.Start(): Starting drop to dir: {0}  filename: {1}", archivalPath, fileName);

            if (DropFileStartDateTime.CompareTo(new DateTime(2000, 1, 1)) < 0)
            {   // If DropFilStartTime has been changed from MinDateTime, then it points to the drop file
                // We have loaded already.  If its still MinDateTime, then set it to NOW.
                DropFileStartDateTime = m_FillHub.Log.GetTime();            // This is time stamp of the next archive file to start.
            }

            string archivalPath = DropSimple.GetArchivePath(DropFileStartDateTime, Misty.Lib.Application.AppInfo.GetInstance().DropPath);// Creates starting time stamp (now's date) in arcPath name.
            string fileName = this.GetArchiveFileName(DropFileStartDateTime, DropType_FillBook);
            this.m_FillHub.Log.NewEntry(LogLevel.Major, "DropRules.Start(): Starting drop to dir: {0}  filename: {1}", archivalPath, fileName);
            m_ArcBookWriter = new DropQueueWriter(archivalPath, fileName, m_FillHub.Log);
            m_ArcBookWriter.Stopping += new EventHandler(ArcBookWriter_Stopping);
            m_ArcBookWriter.Start();
            IsBookWriterRunning = true;

            //this.m_FillHub.Log.EndEntry();
        }// Start()
        //
        //
        //
        // *********************************************************
        // ****                     Enqueue()                   ****
        // *********************************************************
        /// <summary>
        /// Dropping snapshots of the fillHub or fills.
        /// </summary>
        public void Enqueue(IStringifiable iStringfiableObject)
        {
            string s = Stringifiable.Stringify(iStringfiableObject, m_StringifyOverrideTable);
            m_ArcBookWriter.RequestEnqueue(s);
            m_LocalTimeLast = m_FillHub.Log.GetTime();                              // save time stamp of this latest entry.
        }
        //
        //
        // *********************************************************
        // ****             TryPeriodicBookDrop()               ****
        // *********************************************************
        /// <summary>
        /// This implements the periodic auto-drop rule.  Given the current time, decides if its safe to save a snapshot.
        /// When the (LastBookDrop + BookDropPeriod) is greater than now, and our last fill time (LocalLastTime),
        /// we will DropFillBooks.
        /// </summary>
        /// <returns></returns>
        public bool TryPeriodicBookDrop()
        {
            DateTime now = m_FillHub.Log.GetTime();
            if (now.CompareTo(LastBookDrop.Add(BookDropPeriod)) > 0)        // We require enough time passed since last book snapshot.
            {
                if (now.CompareTo(m_LocalTimeLast.Add(FillDelayPeriod)) > 0)// and require some quiet time since last fill.
                {
                    StartNewDropArchive();
                    return true;
                }
                else
                    return false;
            }
            else
                return false;                                               // not yet time for periodic drop.
        } // TryPeriodicBookDrop()
        //
        //
        // *************************************************************
        // ****             StartNewDropArchive()                   ****
        // *************************************************************
        /// <summary>
        /// Create a new complete snapshot and drop in into file.
        /// Called by the FillHub thread, so we know no fills will be being added while we are here.
        /// </summary>
        public void StartNewDropArchive()
        {
            m_FillHub.Log.NewEntry(LogLevel.Major, "Running drop files for account/instrument {0} for {1}!", m_FillHub.Name, AppServices.GetInstance().m_AmbreUserName);
            
            if (m_FillHub.ListenerTradeSubscriptionFilter == null)
            {
                m_FillHub.Log.NewEntry(LogLevel.Warning, "*****Notice***** The account or instrument filter becomes corrupted because fill listener filter is null now");
                if (!string.IsNullOrEmpty(m_FillHub.m_LastListenAccountFilter))
                    m_FillHub.ResetFillListener();
            }
            else
            {
                if (m_FillHub.ListenerTradeSubscriptionFilter.GetType() != typeof(TradeSubscriptionAccountFilter) && m_FillHub.ListenerTradeSubscriptionFilter.GetType() != typeof(TradeSubscriptionInstrumentFilter))
                {
                    m_FillHub.Log.NewEntry(LogLevel.Warning, "*****Notice***** The account or instrument filter becomes corrupted because fill listener filter has different types from account or instrument filter");
                    if (!string.IsNullOrEmpty(m_FillHub.m_LastListenAccountFilter))
                        m_FillHub.ResetFillListener();
                }
            }
            m_FillHub.CheckMakeUpFilterArgs(m_FillHub.m_LastListenAccountFilter);
            string fillHubSnapshot = Stringifiable.Stringify(m_FillHub, m_StringifyOverrideTable);

            if (m_ArcBookWriter != null)
            {
                // Wrap up the current working file, and archive it.
                m_ArcBookWriter.RequestFlushNow();                  // Following procedure as above, make current file up to date...                         
                if (PushToRepository)                               // Now its up-to-date, push it to the repository group-shared drive.
                {
                    string droppath = string.Format("\\\\fileserver\\Users\\DV_Ambre\\AmbreUsers\\{0}\\Drops\\", AppServices.GetInstance().m_AmbreUserName);
                    string repoPath = DropSimple.GetArchivePath(DropFileStartDateTime, droppath);
                    m_FillHub.Log.NewEntry(LogLevel.Minor, "Sending request to copy to new drop directory:{0}.", repoPath);
                    m_ArcBookWriter.RequestCopyTo(repoPath, string.Empty);  // push current file to new repository now, keep same filename.
                    //m_ArcBookWriter.RequestCopyAllFiles(repoPath, "*FillBooks*");// try to sync all 

                    // Also copy the files to old path that is \\\\fileserver\\Users\\dv_bre\\Ambre\\Drops\\ told by Mike.
                    // Temp fix to allow a smooth transition from the old approach to the new approach.
                    droppath = "\\\\fileserver\\Users\\dv_bre\\Ambre\\Drops\\";
                    repoPath = DropSimple.GetArchivePath(DropFileStartDateTime, droppath);
                    m_FillHub.Log.NewEntry(LogLevel.Minor, "Sending request to copy to old drop directory:{0}.", repoPath);
                    m_ArcBookWriter.RequestCopyTo(repoPath, string.Empty);
                }
                // Set new starting time stamp for the new archival file.
                DropFileStartDateTime = m_FillHub.Log.GetTime();    // This is time stamp of the next archive file to start.
                string archivalPath = DropSimple.GetArchivePath(DropFileStartDateTime, Misty.Lib.Application.AppInfo.GetInstance().DropPath);// Creates starting time stamp (now's date) in arcPath name.
                m_ArcBookWriter.RequestChangeFileName(archivalPath, GetArchiveFileName(DropFileStartDateTime, DropType_FillBook));// Change current output filename to a new name (thereby ending our writes to the last one).
                m_ArcBookWriter.RequestEnqueue(fillHubSnapshot);    // initialize the new file with new snapshot
                m_ArcBookWriter.RequestFlushNow();                  // flush it just in case, so at least some entry is present in file if we crash.
            }
            LastBookDrop = m_FillHub.Log.GetTime();                 // store the time we last did a snapshot.
        } // DropFillBooks()
        //
        //
        //
        //
        // *********************************************************
        // ****                 TryLoadBooks()                  ****
        // *********************************************************
        /// <summary>
        /// Reads the local drop file at the GetLocalPath() location named
        /// GetLocalFileName(), and obtains a list of IStringifiable.Nodes in that file.
        /// This list of nodes is examined (in reverse order) until the most recent FillHub
        /// node object is found.  
        /// This is called before Start().
        /// The fillhub node is broken down into its parts, which are actually created and 
        /// store in another list.  The remaining nodes are also created and loading into 
        /// the list as well.  (These are usually fills that came after the fillhub snapshot.)
        /// Version 4:  This is improved because it relies on read-methods from Stringifiable namespace.
        /// </summary>
        /// <returns>True upon success</returns>
        public bool TryLoadBooks()
        {
            string filePath = string.Empty;                 // place to store the desired book to load.

            // Find most recent book that matches unique name.
            List<string> dirPathList = new List<string>(System.IO.Directory.GetDirectories( Misty.Lib.Application.AppInfo.GetInstance().DropPath,"20*")); // presumes all fills in 21st century!
            dirPathList.Sort();
            int dirPtr = dirPathList.Count - 1;             // point to last entry.
            List<string> filePathList = new List<string>();
            string pattern = string.Format("*{0}*",this.GetArchiveFileNameBase(DropType_FillBook));
            while (string.IsNullOrEmpty(filePath) && dirPtr >= 0)
            {
                filePathList.Clear();
                string currentDirPath = dirPathList[dirPtr];
                filePathList.AddRange(System.IO.Directory.GetFiles( currentDirPath, pattern));
                if (filePathList.Count > 0)
                {
                    filePathList.Sort();
                    int filePtr = filePathList.Count -1;    // point to last entry
                    while (string.IsNullOrEmpty(filePath) && filePtr >= 0)
                    {   // We can set to checking here and validation, or use a slightly earlier drop, etc.
                        System.IO.FileInfo info = new System.IO.FileInfo(filePathList[filePtr]);
                        bool isGood = info.Length > 0;      // TODO:  Should we perform more checks that this is the file we want?
                        if (isGood)
                        {
                            filePath = filePathList[filePtr];
                            int indexDir = currentDirPath.LastIndexOf('\\');
                            int indexPath = filePath.LastIndexOf('\\');
                            string fileName = filePath.Substring(indexPath + 1, filePath.Length - (indexPath + 1));
                            string fileTime = fileName.Substring(0, fileName.IndexOf("_"));
                            string fileDate = currentDirPath.Substring(indexDir + 1, currentDirPath.Length - (indexDir + 1));
                            string fileDateTime = string.Format("{0}{1}", fileDate, fileTime);

                            // Try to parse the date time out to the Drop File Start Date Time which defines the specific paths.
                            if (!DateTime.TryParseExact(fileDateTime, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out DropFileStartDateTime))
                            {
                                DropFileStartDateTime = DateTime.MinValue;
                                m_FillHub.Log.NewEntry(LogLevel.Major, "Failed to find the exact date time for the most recent fill book. This is a new user.", fileDateTime);
                            }
                        }
                        else
                            filePtr--;
                    }
                }
                else
                    dirPtr--;
            }// while file not found.
            if (string.IsNullOrEmpty(filePath))
            {
                m_FillHub.Log.BeginEntry(LogLevel.Major, "DropRules.TryLoadBooks: No drop file found for pattern {0}. Searched directories: ", pattern);
                foreach (string s in dirPathList)
                {
                    int n = s.LastIndexOf('\\');
                    m_FillHub.Log.AppendEntry("{0} ", s.Substring(n + 1, s.Length - (n + 1)));
                }
                m_FillHub.Log.EndEntry();
                return false;
            }

            
            //
            // Load the drop file.
            //
            this.m_FillHub.Log.NewEntry(LogLevel.Major, "DropRules.TryLoadBooks: {0}", filePath);
            List<Node> nodeList;
            try
            {
                using (StringifiableReader reader = new StringifiableReader(filePath))
                {
                    nodeList = reader.ReadNodesToEnd();
                    reader.Close();
                }
            }
            catch (Exception e)
            {
                this.m_FillHub.Log.NewEntry(LogLevel.Error, "DropRules.TryLoadBooks: StrigifiableReader exception: {0}", e.Message);
                return false;
            }
            // Go thru the nodes, looking for the last FillHub node.
            Node hubNode = null;
            string fillHubTypeName = m_FillHub.GetType().FullName;
            int ptr = nodeList.Count - 1;                   // point to last element
            while (ptr >= 0)
            {
                if (hubNode != null)
                {                                           // We read backwards, and once we've passed the hub
                    nodeList.RemoveAt(ptr);                 // From here on out, we want to dump everthing!
                }
                else if (nodeList[ptr].Name == fillHubTypeName)
                {
                    hubNode = nodeList[ptr];
                    nodeList.RemoveAt(ptr);                 // remove the hub also; we already have a pointer to it.
                }
                ptr--;                                      // move backwards thru list
            }//wend ptr

            // Extract the info from the FillHub node.
            List<IStringifiable> objectList = new List<IStringifiable>();
            ((IStringifiable)m_FillHub).SetAttributes(hubNode.Attributes);  // initialize this hub!
            foreach (IStringifiable subElem in hubNode.SubElements)
            {
                IStringifiable obj = Stringifiable.Create((Node)subElem);
                objectList.Add(obj);                        // keep all the sub elements (below) we load them in specific order!
            }
            // Now create the objects we found after the hub - these are usually additional fills not in the fill hub.
            foreach (Node anode in nodeList)
            {
                IStringifiable obj = Stringifiable.Create(anode);
                objectList.Add(obj);
            }

            // Load objects we found.
            if (objectList != null && objectList.Count > 0)
            {
                foreach (IStringifiable obj in objectList)
                    if (obj is InstrumentMapEntry)                      // load all InstrumentMapEntries first; needed to create fill books
                        ((IStringifiable)m_FillHub).AddSubElement(obj);
                foreach (IStringifiable obj in objectList)
                    if (!(obj is InstrumentMapEntry))                   // load everything else now.
                        ((IStringifiable)m_FillHub).AddSubElement(obj);
                return true;
            }
            else
                return false;

        }//TryLoadBooks()
        //
        //
        //
        // *****************************************************
        // ****                 Dispose()                   ****
        // *****************************************************
        public void Dispose()
        {
            if (m_ArcBookWriter != null)
            {
                m_ArcBookWriter.RequestStop();
                m_ArcBookWriter = null;
            }            
        }
        //
        //
        // *****************************************************
        // ****                 Stop()                      ****
        // *****************************************************
        public void Stop()
        {
            if (m_ArcBookWriter != null)
            {
                m_ArcBookWriter.SetLog(null);                       // Disconnect my fillHub's log, since we are shutting down (and the writer may persist for a long time).
                m_ArcBookWriter.RequestFlushNow();                  // Following procedure as above, make current file up to date...                         
                if (PushToRepository)                               // Now its up-to-date, push it to the repository group-shared drive.
                {
                    string droppath = string.Format("{0}{1}{2}", "\\\\fileserver\\Users\\DV_Ambre\\AmbreUsers\\", AppServices.GetInstance().m_AmbreUserName, "\\Drops\\");
                    string repoPath = DropSimple.GetArchivePath(DropFileStartDateTime, droppath); //GetRepositoryPath(DropFileStartDateTime);
                    m_ArcBookWriter.RequestCopyTo(repoPath, string.Empty);// push current file to repository now, keep same filename.
                    m_ArcBookWriter.RequestCopyAllFiles(repoPath, string.Format("*{0}", GetArchiveFileNameBase(DropType_FillBook)) );   // just a precaution.
                }
                m_ArcBookWriter.RequestStop();                
            }
        }//Dispose()
        //
        //
        // ****                 ArcWriterStopping()                 ****
        //
        private void ArcBookWriter_Stopping(object sender, EventArgs eventArgs)
        {
            m_ArcBookWriter.Stopping -= new EventHandler(ArcBookWriter_Stopping);
            m_ArcBookWriter = null;
            IsBookWriterRunning = false;
        }
        //
        //
        #endregion//Public Methods


        #region Private Utilities
        // *************************************************************************
        // ****                         Private Utilities                       ****
        // *************************************************************************
        private string GetLocalPath()
        {
            return Misty.Lib.Application.AppInfo.GetInstance().DropPath;
        }
        private string GetLocalFileName()
        {
            if (string.IsNullOrEmpty(this.UniqueUserName))
                return string.Format("{0}.{1}", DropRules.DropType_FillBook, FileSuffix);
            else
                return string.Format("{1}_{0}.{2}", this.UniqueUserName, DropRules.DropType_FillBook, FileSuffix);
        }
        //private string GetArchivePath(DateTime date)
        //{
        //    return DropSimple.GetArchiveDir(date,Misty.Lib.Application.AppInfo.GetInstance().DropPath);
        //}
        public static string GetArchivePath(DateTime date, string dropBasePath)
        {
            return string.Format("{0}{1:yyyyMMdd}\\", dropBasePath, date);
        }
        //private string GetRepositoryPath(DateTime date)
        //{
        //    return DropSimple.GetArchivePath(date, DropRules.BreRepositoryPath);
        //    //return string.Format("{0}{1:yyyyMMdd}\\", DropRules.BreRepositoryPath, date);
        //}
        //
        // File name elements
        //
        private string GetArchiveFileName(DateTime date, string bookType)
        {
            //return string.Format("{0:HHmmss}_{1}",date,GetArchiveFileNameBase(bookType));
            return DropSimple.GetArchiveFileName(date, bookType, this.UniqueUserName, FileSuffix);
        }
        private string GetArchiveFileNameBase(string bookType)
        {
            return DropSimple.GetArchiveFileBaseName(bookType, this.UniqueUserName, FileSuffix);
            //return string.Format("{0}_{1}.{2}",bookType,this.UniqueUserName,FileSuffix);
        }
        // Static versions:
        public static string GetArchiveFileBaseName(string bookType, string userName, string fileSuffix = "txt")
        {
            return string.Format("{0}_{1}.{2}", bookType, userName, fileSuffix);
        }
        public static string GetArchiveFileName(DateTime date, string bookType, string userName, string fileSuffix="txt")
        {
            return string.Format("{0:HHmmss}_{1}", date, GetArchiveFileBaseName(bookType,userName,fileSuffix));
        }
        //
        #endregion//private utilities
    }
}

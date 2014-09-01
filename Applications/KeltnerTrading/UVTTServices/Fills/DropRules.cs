using System;
using System.Collections.Generic;
using System.Text;

namespace UV.TTServices.Fills
{
    using InstrumentName = UV.Lib.Products.InstrumentName;
    using UV.Lib.Hubs;
    using UV.Lib.IO.Xml;                         // for Istringifiable
    using UV.Lib.IO.Drops;                             // for DropQueue

    using UV.TTServices;
    using UV.TTServices.Markets;

    /// <summary>
    /// Notes:
    /// 1. Whenever a new drop file is started it is started with a snapshot of the entire fillHub.
    /// 2. however, this is NOT guarenteed, if at startup we receive fills right away, that archival file may start with fills, 
    ///     and then when a new archive is started, no snapshot will ever appear in that file.  This can't happen in the 
    ///     drop file used to load from.... (hopefully) since its always loaded from, appended to and then re-initialized (with snapshot).
    /// </summary>
    public class DropRules : IDisposable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // External services
        private FillHub m_FillHub = null;
        private DropQueueWriter m_LocalBookWriter = null;           // for books + fills to local file.
        private DropQueueWriter m_ArcBookWriter = null;             // for writing books + fills to archive.
        private DropQueueWriter m_FillWriter = null;                // raw fill dropping

        // Used to create drop file name.
        private string UniqueUserName;                              // unique identifier to distiguish between multiple fillhubs (accts)
        private DateTime DropFileStartDateTime;                     // time the current file was started.

        // Drop timing controls
        public DateTime LastBookDrop = DateTime.MinValue;           // last time we dropped the books.
        private DateTime m_LocalTimeLast = DateTime.MinValue;       // last time something was enqueued for writing (a fill usually).
        public TimeSpan BookDropPeriod = new TimeSpan(0, 60, 0);
        public TimeSpan FillDelayPeriod = new TimeSpan(0, 1, 0);

        //public DateTime NextResetDateTime = DateTime.Now;           // Next time to reset PnL.
        //public TimeSpan ResetTimeOfDay = new TimeSpan(16, 30, 00);  // 4:30 PM local time each day.

        private Dictionary<Type, string[]> m_StringifyOverrideTable = null;


        //
        // File & directory formatting etc.
        //
        public static string DropType_FillBook = "FillBooks";
        public static string DropType_Fills = "Fills";
        public static string FileSuffix = "txt";


        private bool PushToRepository = true;                       // will we copy to repository?
        public static string BreRepositoryPath = "\\\\fileserver\\Users\\dv_bre\\Ambre\\Drops\\";
            
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
        public DropRules(FillHub fillHub)
        {
            // Create the unique user name.
            // UserLoginID + FillHubName (usually acct#)
            //
            TTApiService api = TTApiService.GetInstance();
            this.UniqueUserName = string.Format("{0}_{1}", api.LoginUserName, fillHub.Name);
            //this.UniqueUserName = fillHub.Name;
            this.m_FillHub = fillHub;
            this.DropFileStartDateTime = fillHub.Log.GetTime();               // time stamp "label" used for archiving current drop file.

            m_StringifyOverrideTable = new Dictionary<Type, string[]>();
            m_StringifyOverrideTable.Add(m_FillHub.GetType(), new string[] { string.Empty, "GetAttributesDrop", "GetElementsDrop" });

           
            //#if (DEBUG)
            //BookDropPeriod = new TimeSpan(0, 5, 0);
            //PushToRepository = false;
            //#endif

            this.LastBookDrop = fillHub.Log.GetTime().Subtract(this.BookDropPeriod).AddMinutes(5.0);
            this.m_LocalTimeLast = this.LastBookDrop;
            //this.NextResetDateTime = this.NextResetDateTime.Subtract(this.NextResetDateTime.TimeOfDay).Add(ResetTimeOfDay);

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
        /// The user might want to load the old drop file for positions before starting
        /// to write and update a new one.  No drop file will be created until Start() is called.
        /// </summary>
        public void Start()
        {
            // Create drop writers
            // Local writer
            string path = this.GetLocalPath();
            string fileName = this.GetLocalFileName();
            this.m_FillHub.Log.BeginEntry(LogLevel.Major, "DropRules: Dropping to {0}{1}", path, fileName);
            m_LocalBookWriter = new DropQueueWriter(path, fileName, m_FillHub.Log);
            m_LocalBookWriter.Start();

            // Acrhival writer
            DateTime now = DateTime.Now;
            path = this.GetArchivePath(now);
            fileName = this.GetArchiveFileName(now,DropType_FillBook);
            this.m_FillHub.Log.AppendEntry(", archived to {0}{1}", path, fileName);
            m_ArcBookWriter = new DropQueueWriter(path, fileName, m_FillHub.Log);
            m_ArcBookWriter.Start();


            // Raw Fill drops 
            fileName = this.GetArchiveFileName(now, DropType_Fills);
            this.m_FillHub.Log.AppendEntry(", and fills archived {1}.", path, fileName);
            m_FillWriter = new DropQueueWriter(path, fileName,m_FillHub.Log);
            m_FillWriter.Start();

            this.m_FillHub.Log.EndEntry();
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
            m_LocalBookWriter.RequestEnqueue(s);
            m_LocalTimeLast = m_FillHub.Log.GetTime();                              // save time stamp of this latest entry.
        }
        /// <summary>
        /// Dropping for all Raw fill events.
        /// </summary>
        public void EnqueueArchiveFill(FillEventArgs fillEventArg)
        {
            m_FillWriter.RequestEnqueue(Stringifiable.Stringify(fillEventArg, m_StringifyOverrideTable));
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
        /// This is called by the PeriodicUpdate() method of the owner Hub.
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
                    // This is where I used to reset the PnL.  Now this is done in the FillHub itself.
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
            string fillHubSnapshot = Stringifiable.Stringify(m_FillHub, m_StringifyOverrideTable);

            // Copy and reinitialize the local file
            if (m_LocalBookWriter != null)
            {
                m_LocalBookWriter.RequestFlushNow();                // Flush queue into the current file, making it up-to-date.
                string fileName = GetLocalFileName();               // Local drop-file name
                string path = string.Format("{0}Previous\\", GetLocalPath());
                m_LocalBookWriter.RequestMoveTo(path, fileName);    // Move the current drop file to a safe, backup area.
                m_LocalBookWriter.RequestEnqueue(fillHubSnapshot);  // Create a new local drop file, and initialize it w/ snapshot
                m_LocalBookWriter.RequestFlushNow();                // I like to push out the first snapshot immediately...
            }

            // Archive and restart new archival files.
            if (m_ArcBookWriter != null)
            {
                m_ArcBookWriter.RequestFlushNow();                  // Following procedure as above, make current file up to date...                         
                if (PushToRepository)                               // Now its up-to-date, push it to the repository group-shared drive.
                {
                    string repoPath = GetRepositoryPath(DropFileStartDateTime);
                    m_ArcBookWriter.RequestCopyTo(repoPath, string.Empty);          // push current file to repository now, keep same filename.
                    m_ArcBookWriter.RequestCopyAllFiles(repoPath,  string.Format("*{0}",GetArchiveFileNameBase(DropType_FillBook)) );    // try to sync all 
                }
                // Set new starting time stamp for the new archival file.
                DropFileStartDateTime = m_FillHub.Log.GetTime();    // This is time stamp of the next archive file to start.
                string archivalPath = GetArchivePath(DropFileStartDateTime);// Creates starting time stamp (now's date) in arcPath name.
                m_ArcBookWriter.RequestChangeFileName(archivalPath, GetArchiveFileName(DateTime.Now,DropType_FillBook));// Change current output filename to a new name (thereby ending our writes to the last one).
                m_ArcBookWriter.RequestEnqueue(fillHubSnapshot);    // initialize the new file with new snapshot
                m_ArcBookWriter.RequestFlushNow();                  // flush it just in case.
            }

            // Archive
            if (m_FillWriter != null)
            {
                m_FillWriter.RequestFlushNow();
                string archivalPath = GetArchivePath(DropFileStartDateTime);// Creates starting time stamp (now's date) in arcPath name.
                m_FillWriter.RequestChangeFileName(archivalPath, GetArchiveFileName(DropFileStartDateTime, DropType_Fills));                
            }

            // Store the time we did this snapshot.
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
        /// The fillhub node is broken down into its parts, which are actually created and 
        /// store in another list.  The remaining nodes are also created and loading into 
        /// the list as well.  (These are usually fills that came after the fillhub snapshot.)
        /// Version 4:  This is improved because it relies on read-methods from Stringifiable namespace.
        /// </summary>
        /// <returns>True upon success</returns>
        public bool TryLoadBooks(string loadFileName = "")
        {
            string filePath = string.Empty;
            if (string.IsNullOrEmpty(loadFileName))
                filePath = string.Format("{0}{1}", GetLocalPath(), GetLocalFileName());
            else
                filePath = string.Format("{0}{1}", GetLocalPath(), loadFileName);
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
                this.m_FillHub.Log.NewEntry(LogLevel.Error, "DropRules.TryLoadBooks: StrigifiableReader exception: {0}",e.Message);
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
            if (m_FillWriter != null)
            {
                m_FillWriter.RequestStop();            // this will purge any drop lines in buffer.
                m_FillWriter = null;
            }
            if (m_LocalBookWriter != null)
            {
                m_LocalBookWriter.RequestStop();
                m_LocalBookWriter = null;
            }
            if (m_ArcBookWriter != null)
            {
                m_ArcBookWriter.RequestFlushNow();                  // Following procedure as above, make current file up to date...                         
                if (PushToRepository)                               // Now its up-to-date, push it to the repository group-shared drive.
                {
                    string repoPath = GetRepositoryPath(DropFileStartDateTime);
                    m_ArcBookWriter.RequestCopyTo(repoPath, string.Empty);// push current file to repository now, keep same filename.
                    m_ArcBookWriter.RequestCopyAllFiles(repoPath, string.Format("*{0}", GetArchiveFileNameBase(DropType_FillBook)) );                
                }

                m_ArcBookWriter.RequestStop();
                m_ArcBookWriter = null;
            }
        }
        #endregion//Public Methods


        #region Private Utilities
        // *************************************************************************
        // ****                         Private Utilities                       ****
        // *************************************************************************
        private string GetLocalPath()
        {
            return UV.Lib.Application.AppInfo.GetInstance().DropPath;
        }
        private string GetLocalFileName()
        {
            if (string.IsNullOrEmpty(this.UniqueUserName))
                return string.Format("{0}.{1}",DropRules.DropType_FillBook,FileSuffix);
            else
                return string.Format("{1}_{0}.{2}", this.UniqueUserName, DropRules.DropType_FillBook,FileSuffix);
        }
        private string GetArchivePath(DateTime date)
        {
            return string.Format("{0}{1:yyyyMMdd}\\", UV.Lib.Application.AppInfo.GetInstance().DropPath, date);
        }
        private string GetRepositoryPath(DateTime date)
        {
            return string.Format("{0}{1:yyyyMMdd}\\",DropRules.BreRepositoryPath, date);
        }
        /// <summary>
        /// Utility function to construct the file names for drop files. 
        /// There may be multiple drop file types in an application, like "Fills" etc, the type
        /// string will be embedded as part of the drop file name.  
        /// The drop file format is:    HHmmss_dropType_UniqueUserName.txt
        /// This function returns the name "Base"; that is, the part that follows the time stamp.
        /// </summary>
        /// <param name="dropType">string identifying type of data found in drop file.</param>
        /// <returns>base file name.</returns>
        private string GetArchiveFileNameBase(string dropType)
        {
            return string.Format("{0}_{1}.{2}", dropType, this.UniqueUserName, FileSuffix);
        }
        /// <summary>
        /// This returns the full name of the file = timestamp + basename
        /// </summary>
        /// <param name="date">date from which timestamp is extracted</param>
        /// <param name="dropType">string identifying type of data found in drop file.</param>
        /// <returns>full file name.</returns>
        private string GetArchiveFileName(DateTime date, string dropType)
        {
            return string.Format("{0:HHmmss}_{1}", date, GetArchiveFileNameBase(dropType));
        }
        #endregion//private utilities


        #region Loading Methods Version 3 - now defunct
        // *********************************************************************************
        // ****                 Load Most Recently Saved Fill Book ()                   ****
        // *********************************************************************************
        //
        //
        /*
        public bool TryLoadBooks_Original()
        {
            List<IStringifiable> nodeList = ReadLocalFillBook();
            string fillHubTypeName = m_FillHub.GetType().FullName;


            // Now convert these nodes into real objects.
            List<IStringifiable> objectList = new List<IStringifiable>();
            foreach (IStringifiable iNode in nodeList)
            {
                Node node = (Node)iNode;
                if (node.Name.Equals(fillHubTypeName))
                {   // We need to take the sub elements of the FillHub, and pull them out.
                    ((IStringifiable)m_FillHub).SetAttributes(node.Attributes);
                    foreach (IStringifiable subElem in node.SubElements)
                    {
                        IStringifiable obj = Stringifiable.Create((Node)subElem);
                        objectList.Add(obj);
                    }
                }
                else
                {   // These are fill events and other things.
                    IStringifiable obj = Stringifiable.Create((Node)iNode);
                    objectList.Add(obj);
                }
            }
            // 
            if (objectList != null && objectList.Count > 0)
            {
                foreach (IStringifiable obj in objectList)                          // load all InstrumentMapEntries first - needed to create books
                    if (obj is InstrumentMapEntry)
                        ((IStringifiable)m_FillHub).AddSubElement(obj);
                foreach (IStringifiable obj in objectList)                          // load everything else now.
                    if (!(obj is InstrumentMapEntry))
                        ((IStringifiable)m_FillHub).AddSubElement(obj);
                return true;
            }
            else
                return false;

        }// TryLoadBooks()
        //
        /// <summary>
        /// Reads local file for current position.
        /// </summary>
        /// <returns></returns>
        private List<IStringifiable> ReadLocalFillBook()
        {            
            string fillHubTypeName = m_FillHub.GetType().FullName;

            string filePath = string.Format("{0}{1}", GetLocalPath(), GetLocalFileName());
            this.m_FillHub.Log.NewEntry(LogLevel.Major, "DropRules.ReadLocalFillBook: {0}",filePath);
            List<IStringifiable> nodes = ReadNodes(filePath);           // nodes in this file.

            return nodes;
        }// LoadLocalFillBook().
        //
        //
        private List<IStringifiable> ReadNodes(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                return new List<IStringifiable>();              // return empty list.

            // 1.  Read the file backwards until we find the last startTag for the FillBook XML.
            // Search for the last block for the object.
            List<string> lines = new List<string>();
            string objectName = m_FillHub.GetType().FullName;
            string startTag = string.Format("<{0}", m_FillHub.GetType().FullName);  // leave trailing parts off to allow for attributes.
            using (BackwardReader br = new BackwardReader(filePath))
            {
                bool isContinuing = true;
                while (!br.SOF && isContinuing)
                {
                    string aLine = br.Readline();
                    if (aLine.Contains(startTag))
                    {
                        m_FillHub.Log.NewEntry(LogLevel.Major, "DropRules.Load: Loading fill hub {0}", aLine);
                        isContinuing = false;
                        // Keep everything after the startTag, dump everything on the line
                        string[] truncatedLineParts = aLine.Split(new string[] { startTag }, StringSplitOptions.RemoveEmptyEntries);
                        string truncatedLine = string.Format("{0}{1}", startTag, truncatedLineParts[truncatedLineParts.Length - 1]);
                        lines.Add(truncatedLine);
                    }
                    else
                        lines.Add(aLine);
                }//wend
            }//using reader
            lines.Reverse();                                            // since I read backwards, reverse the lines now.

            // 2. Now, create a string stream of the block, create nodes.
            StringBuilder msg = new StringBuilder();
            foreach (string aLine in lines)
                msg.Append(aLine);
            byte[] byteBuffer = ASCIIEncoding.ASCII.GetBytes(msg.ToString());
            System.IO.MemoryStream stream = new System.IO.MemoryStream(byteBuffer);
            StringifiableReader reader = new StringifiableReader(stream);
            List<IStringifiable> nodeList = reader.ReadToEnd(true);
            return nodeList;
        } // ReadNodes
        //
        */ 
        //
        //
        #endregion // load methods


   
    }
}

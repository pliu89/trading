using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Fills
{
    using InstrumentName = Misty.Lib.Products.InstrumentName;
    using Misty.Lib.Hubs;
    using Misty.Lib.IO.Xml;                         // for Istringifiable

    using Ambre.TTServices;
    using Ambre.TTServices.Markets;

    public class DropRules2 : IDisposable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // External services
        private FillHub m_FillHub = null;
        private DropQueueWriter m_DropWriter = null;                // for books, etc.

        // Fills
        private DropQueueWriter m_DropFills = null;                 // pure fill drops


        // Used to create drop file name.
        private string UniqueUserName = string.Empty;
        private DateTime DropFileStartDateTime;                     // time the current file was started.

        // Drop timing controls
        public DateTime LastBookDrop = DateTime.MinValue;           // last time we dropped the books.
        private DateTime m_LocalTimeLast = DateTime.MinValue;       // last time something was enqueued for writing (a fill usually).
        public TimeSpan BookDropPeriod = new TimeSpan(0, 30, 0);
        public TimeSpan FillDelayPeriod = new TimeSpan(0, 1, 0);

        public DateTime NextResetDateTime = DateTime.Now;      // Next time to reset PnL.
        public TimeSpan ResetTimeOfDay = new TimeSpan(16, 30, 00);  // 4:30 PM local time each day.

        //
        // File & directory formatting etc.
        //
        public static string FillBook_Base = "FillBooks";
        public static string FileSuffix = "txt";


        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public DropRules2(FillHub fillHub)
        {
            this.UniqueUserName = fillHub.Name;
            this.m_FillHub = fillHub;
            this.DropFileStartDateTime = fillHub.Log.GetTime();               // time stamp "label" used for archiving current drop file.


#if (DEBUG)
            BookDropPeriod = new TimeSpan(0, 5, 0);
#endif

            this.LastBookDrop = fillHub.Log.GetTime().Subtract(this.BookDropPeriod).AddMinutes(5.0);
            this.m_LocalTimeLast = this.LastBookDrop;
            this.NextResetDateTime = this.NextResetDateTime.Subtract(this.NextResetDateTime.TimeOfDay).Add(ResetTimeOfDay);

            string path = this.GetLocalPath();
            string fileName = this.GetLocalFileName();
            this.m_FillHub.Log.NewEntry(LogLevel.Major, "DropRules: Dropping to {0}{1}", path, fileName);
            m_DropWriter = new DropQueueWriter(path, fileName);

            // Raw Fill drops 
            string dropDirPath = this.GetArchivePath(DateTime.Now);
            string dropFileName = this.GetArchiveFileName(DateTime.Now, "Fills");
            m_DropFills = new DropQueueWriter(dropDirPath, dropFileName);

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
            m_DropWriter.Start();
            m_DropFills.Start();
        }
        // *********************************************************
        // ****                     Enqueue()                   ****
        // *********************************************************
        public void Enqueue(IStringifiable iStringfiableObject)
        {
            m_DropWriter.RequestEnqueue(Stringifiable.Stringify(iStringfiableObject));
            m_LocalTimeLast = m_FillHub.Log.GetTime();                              // save time stamp of this latest entry.
        }
        //
        //
        // *********************************************************
        // ****             TryPeriodicBookDrop()               ****
        // *********************************************************
        /// <summary>
        /// This implements the drop rule.  Given the current time, decides if its safe to save a snapshot.
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
                    DropFillBooks();
                    if (now.CompareTo(NextResetDateTime) > 0)
                    {   // Lets reset the PnL if we passed the NextResetDateTime.
                        // We will request the fill hub to reset the PnL, then it will also request another drop.
                        DateTime nextDay = NextResetDateTime.AddDays(1.0);
                        NextResetDateTime = nextDay.Subtract(nextDay.TimeOfDay).Add(ResetTimeOfDay);  // time of next reset, tomorrow
                        m_FillHub.Request(new Misty.Lib.OrderHubs.OrderHubRequest(Misty.Lib.OrderHubs.OrderHubRequest.RequestType.RequestRealPnLReset));
                    }
                    return true;
                }
                else
                    return false;
            }
            else
                return false;                                               // not yet time for periodic drop.
        } // TryPeriodicBookDrop()
        //
        // *************************************************************
        // ****                   Drop Raw Fill()                   ****
        // *************************************************************
        public void DropFillEvent(FillEventArgs fillEventArg)
        {
            m_DropFills.RequestEnqueue(Misty.Lib.IO.Xml.Stringifiable.Stringify(fillEventArg));
        }
        //
        // *************************************************************
        // ****                 Drop Fill Books()                   ****
        // *************************************************************
        /// <summary>
        /// Create a new complete snapshot and drop in into file.
        /// Called by the FillHub thread.
        /// </summary>
        public void DropFillBooks()
        {
            if (m_DropWriter != null)
            {
                // Drop a summary in the current file.
                string s = Stringifiable.Stringify(m_FillHub);
                m_DropWriter.RequestEnqueue(s);
                m_DropWriter.RequestFlushNow();

                // Tell the writer to copy its current file to archive directory.
                string pathName = GetArchivePath(this.DropFileStartDateTime);
                double n = 1;
                string fileName = GetArchiveFileName(this.DropFileStartDateTime);   // name of place to store this file.
                while (n < 20 && System.IO.File.Exists(string.Format("{0}{1}", pathName, fileName)))
                {
                    fileName = GetArchiveFileName(this.DropFileStartDateTime.AddSeconds(n));
                    n++;
                }
                if (n < 20)
                {
                    m_FillHub.Log.BeginEntry(LogLevel.Minor, "DropRule.DropFillBooks: Archiving drops to {0}{1}.", pathName, fileName);
                    m_DropWriter.RequestCopyTo(pathName, fileName);            // this will copy drop file to this new name.                
                }

                // Create a snapshot to start off the new drop.
                this.DropFileStartDateTime = m_FillHub.Log.GetTime();               // new start time for current drop file.                
                m_DropWriter.RequestEnqueue(s);
                m_DropWriter.RequestFlushNow();
                LastBookDrop = this.DropFileStartDateTime;

                m_FillHub.Log.EndEntry();
            }
        } // DropFillBooks()
        //
        //
        //
        //
        // *********************************************************
        // ****                 TryLoadBooks()                  ****
        // *********************************************************
        public bool TryLoadBooks()
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
                    Dictionary<string, string> unused = new Dictionary<string, string>();
                    ((IStringifiable)m_FillHub).SetAttributes(node.Attributes, ref unused);
                    foreach (IStringifiable subElem in node.SubElements)
                    {
                        IStringifiable obj = Stringifiable.DeStringify((Node)subElem);
                        objectList.Add(obj);
                    }
                }
                else
                {   // These are fill events and other things.
                    IStringifiable obj = Stringifiable.DeStringify((Node)iNode);
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
        //
        //
        public void Dispose()
        {
            if (m_DropWriter != null)
            {
                m_DropWriter.Stop();            // this will purge any drop lines in buffer.
                m_DropWriter = null;
            }
            if (m_DropFills != null)
            {
                m_DropFills.Stop();
                m_DropFills = null;
            }
        }
        #endregion//Public Methods


        #region Private Utilities
        //
        private string GetLocalPath()
        {
            return Misty.Lib.Application.AppInfo.GetInstance().DropPath;
        }
        private string GetLocalFileName()
        {
            if (string.IsNullOrEmpty(this.UniqueUserName))
                return string.Format("{0}.{1}", DropRules.FillBook_Base, FileSuffix);
            else
                return string.Format("{1}_{0}.{2}", this.UniqueUserName, DropRules.FillBook_Base, FileSuffix);
        }
        private string GetArchivePath(DateTime date)
        {
            return string.Format("{0}{1:yyyyMMdd}\\", Misty.Lib.Application.AppInfo.GetInstance().DropPath, date);
        }
        private string GetArchiveFileName(DateTime date, string fileTypeName)
        {
            if (string.IsNullOrEmpty(this.UniqueUserName))
                return string.Format("{0:HHmmss}_{2}.{3}", date, this.UniqueUserName, fileTypeName, FileSuffix);
            else
                return string.Format("{0:HHmmss}_{2}_{1}.{3}", date, this.UniqueUserName, fileTypeName, FileSuffix);
        }
        private string GetArchiveFileName(DateTime date)
        {
            return GetArchiveFileName(date, DropRules.FillBook_Base);
        }
        //
        #endregion//private utilities


        #region Loading Methods Version 3
        // *********************************************************************************
        // ****                 Load Most Recently Saved Fill Book ()                   ****
        // *********************************************************************************
        /// <summary>
        /// Reads local file for current position.
        /// </summary>
        /// <returns></returns>
        private List<IStringifiable> ReadLocalFillBook()
        {
            string fillHubTypeName = m_FillHub.GetType().FullName;

            string filePath = string.Format("{0}{1}", GetLocalPath(), GetLocalFileName());
            this.m_FillHub.Log.NewEntry(LogLevel.Major, "DropRules.ReadLocalFillBook: {0}", filePath);
            List<IStringifiable> nodes = ReadNodes(filePath);           // nodes in this file.

            return nodes;
        }// LoadLocalFillBook().
        //
        //
        #endregion // load methods


        #region Loading Methods Version 2
        // *********************************************************************************
        // ****                 Load Most Recently Saved Fill Book ()                   ****
        // *********************************************************************************
        private List<IStringifiable> LoadMostRecentlySavedFillBookLoad()
        {
            List<IStringifiable> objectList = new List<IStringifiable>();
            string fillHubTypeName = m_FillHub.GetType().FullName;


            // Obtain a list of all drop directories, go thru them by date.
            string filePath = string.Empty;                             // place to store book.
            List<string> dirList = new List<string>(System.IO.Directory.GetDirectories(Misty.Lib.Application.AppInfo.GetInstance().DropPath));
            DateTime dropDate = DateTime.Now;
            string dirPath = GetArchivePath(dropDate);
            const int nLookBackDays = 30;
            while (string.IsNullOrEmpty(filePath) && (DateTime.Now.Subtract(dropDate).Days < nLookBackDays))
            {
                while (!dirList.Contains(dirPath) && (DateTime.Now.Subtract(dropDate).Days < nLookBackDays))
                {
                    dropDate = dropDate.AddDays(-1.0);
                    dirPath = GetArchivePath(dropDate);
                }
                // Found a directory, search its contents
                List<string> fileList = new List<string>(System.IO.Directory.GetFiles(dirPath));
                int n = fileList.Count - 1;
                while (n >= 0)
                {
                    if (!fileList[n].Contains(DropRules.FillBook_Base))
                        fileList.RemoveAt(n);                           // keep only fill book files.
                    n--;
                }
                while (fileList.Count > 0 && string.IsNullOrEmpty(filePath))
                {
                    fileList.Sort();                                    // sort them by their names (time stamps)
                    List<IStringifiable> nodes = ReadNodes(fileList[fileList.Count - 1]);// nodes in this file.
                    // Must insert these into my master list, since they are earlier than anything else that may be
                    // in the list already, insert them into the front.
                    int ptr = nodes.Count - 1;                          // point to last new node to insert, 
                    while (ptr >= 0)
                    {
                        objectList.Insert(0, nodes[ptr]);               // keep inserting this into the front position
                        if (nodes[ptr] is Node && ((Node)nodes[ptr]).Name.Equals(fillHubTypeName))
                            filePath = fileList[fileList.Count - 1];    // signals we are done, found a snapshot in this file!
                        ptr--;
                    }
                    fileList.RemoveAt(fileList.Count - 1);      // remove last file, and keep reading                    
                }
            }

            // Exit
            return objectList;
        } // LoadMostRecentlySavedFillBookLoad()
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
        #endregion // Loading methods


        #region Loading Methods version 1
        // *****************************************************************
        // ****                    Private Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        private string GetMostRecentlyFile()
        {
            string filePath = string.Empty;

            // Find most recent drop directory
            List<string> dirList = new List<string>(System.IO.Directory.GetDirectories(Misty.Lib.Application.AppInfo.GetInstance().DropPath));
            DateTime dropDate = DateTime.Now;
            string dirPath = GetArchivePath(dropDate);
            const int nLookBackDays = 30;
            while (string.IsNullOrEmpty(filePath) && (DateTime.Now.Subtract(dropDate).Days < nLookBackDays))
            {
                while (!dirList.Contains(dirPath) && (DateTime.Now.Subtract(dropDate).Days < nLookBackDays))
                {
                    dropDate = dropDate.AddDays(-1.0);
                    dirPath = GetArchivePath(dropDate);
                }
                // Found a directory, search its contents
                List<string> fileList = new List<string>(System.IO.Directory.GetFiles(dirPath));
                int n = fileList.Count - 1;
                while (n >= 0)
                {
                    if (!fileList[n].Contains(DropRules.FillBook_Base))
                        fileList.RemoveAt(n);                           // keep only fill book files.
                    n--;
                }
                if (fileList.Count > 0)
                {
                    fileList.Sort();                                    // sort them by their names (time stamps)                    
                    filePath = fileList[fileList.Count - 1];            // get largest (most recent) file time stamp.
                }
            }
            return filePath;
        } // GetMostRecentDropFile()
        //
        //
        //
        //
        private List<IStringifiable> Load2(string filePath)
        {
            List<IStringifiable> objectList = null;
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





            // 3. Now convert these nodes into real objects.
            objectList = new List<IStringifiable>();
            foreach (IStringifiable iNode in nodeList)
            {
                Node node = (Node)iNode;
                if (node.Name.Contains("FillHub"))
                {   // We need the sub elements of the FillHub
                    foreach (IStringifiable subElem in node.SubElements)
                    {
                        IStringifiable obj = Stringifiable.DeStringify((Node)subElem);
                        objectList.Add(obj);
                    }
                }
                else
                {   // These are fill events and other things.
                    IStringifiable obj = Stringifiable.DeStringify((Node)iNode);
                    objectList.Add(obj);
                }
            }

            return objectList;
        }// Load2()
        //
        //
        //
        /// <summary>
        /// Original version - less robust
        /// </summary>
        /// <returns></returns>
        public List<IStringifiable> Load1()
        {
            List<IStringifiable> objectList = null;
            /*            
            if (IsFileExists)
            {
                // Search for the last entry for the object.
                List<string> lines = new List<string>();
                string objectName = m_FillHub.GetType().FullName;
                string startTag = string.Format("<{0}", m_FillHub.GetType().FullName);  // leave trailing parts off to allow for attributes.
                string endTag = string.Format("</{0}", m_FillHub.GetType().FullName);
                Dictionary<string, string> attributes = new Dictionary<string, string>();
                int level = -1;         // default is not found.
                using (BackwardReader br = new BackwardReader(this.FileNamePath))
                {
                    bool isContinuing = true;
                    while (!br.SOF && isContinuing)
                    {
                        string aLine = br.Readline();
                        if (aLine.Contains(endTag))         // We found an endTag, we are enter a desired block (or a sub block).                        
                        {
                            level++;
                        }
                        else if (aLine.Contains(startTag))  // we will skip the FillHub tags - assume they are on their own lines!!
                        {
                            level--;
                            if (level < 0)
                            {
                                m_FillHub.Log.NewEntry(LogLevel.Major, "DropRules.Load: Loading fill hub {0}", aLine);
                                isContinuing = false;
                            }
                        }
                        else
                            lines.Add(aLine);
                    }//wend
                }//using reader
                lines.Reverse();

                // Now load the string.
                StringBuilder msg = new StringBuilder();
                foreach (string s in lines)
                    msg.AppendFormat("{0}\r\n", s);
                objectList = Stringifiable.Destringify(msg.ToString());
            }
            */
            return objectList;
        }// Load1
        #endregion//Private Methods

    }
}

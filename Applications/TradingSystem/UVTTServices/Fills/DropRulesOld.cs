using System;
using System.Collections.Generic;
using System.Text;

namespace UV.TTServices.Fills
{
    using TradingTechnologies.TTAPI;

    using InstrumentName = UV.Lib.Products.InstrumentName;
    using UV.Lib.Hubs;
    using UV.Lib.Utilities;
    using UV.Lib.IO.Xml;                         // for Istringifiable
    

    using UV.TTServices;
    using UV.TTServices.Markets;


    public class DropRulesOld
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // External services
        private FillHub m_FillHub = null;
        private UV.Lib.IO.Drops.DropQueueWriter m_DropWriter = null;
        private LogHub Log = null;

        //


        private DateTime m_DropSnapshotLastTime = DateTime.MinValue;            // Actual last time we wrote the drop file.
        private DateTime m_DropSnapshotNextTime;                                // next scheduled snapshot drop        
        private TimeSpan m_DropSnapshotWait = new TimeSpan(0, 15, 0);           // time betweeen snapshots.      
        private TimeSpan m_DropSnapshotPostFillWait = new TimeSpan(0, 0, 30);   // required time between last fill and now.        
        private DateTime m_LoadedTransactionTime = DateTime.MinValue;           // time we demand new fills come in after.

        private List<UV.Lib.OrderHubs.Fill> m_DropFillList = new List<UV.Lib.OrderHubs.Fill>();   // workspace
     
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public DropRulesOld(FillHub fillHub, UV.Lib.IO.Drops.DropQueueWriter dropWriter)
        {
            m_FillHub = fillHub;
            Log = m_FillHub.Log;
            m_DropWriter = dropWriter;

        }
        //
        //       
        #endregion//Constructors



        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        // ****                 Update Drop Snapshot()                    ****
        //
        public void UpdateDropSnapshot(DateTime localTimeLastFill)
        {
            // Criteria for dropping a snapshot:
            // 1)  Some significant time has elapsed since last update:             now - (SnapshotLastTime + WaitTime) > 0
            //          here, SnapshotLastTime+WaitTime ---> SnapshotNextTime       this way we can wait even longer.
            // 2)  but, the fills aren't too recent:                                now - (LastFillTime + DeadTime) > 0
            //      If we pass these events, then we test the final test:
            //      3) there have been fills since the last update:                 LastFillTime - (SnapshotLastTime) > 0
            //      If true, we drop now, and set SnapshotNextTime = now + WaitTime
            //      If false, things are slow, no reason to keep checking... set SnapshotNextTime = now + WaitTime.  
            //
            // Note that the lastFill is the server transaction time, NOT our local time.  The following will cause trouble 
            // if our clocks are more than the amount m_DropSnapshotPostFillWait.
            DateTime now = Log.GetTime();
            int n1 = now.CompareTo(m_DropSnapshotNextTime);                         // is now > SnapshotNextTime
            if (n1 > 0)                                                             // has significant time past since last snapshot?
            {
                int n2 = now.CompareTo(localTimeLastFill.Add(m_DropSnapshotPostFillWait));// is now > LastFillTime + DeadTime
                if (n2 > 0)
                {
                    int n3 = localTimeLastFill.CompareTo(m_DropSnapshotLastTime);
                    if (n3 > 0)
                    {                                                               // There has been a fill since the last snapshot.
                        //DropSnapshot();
                    }
                    m_DropSnapshotNextTime = now.Add(m_DropSnapshotWait);           // Wait at least this much time before dropping again.
                }
            }
        }//UpdateDropSnapshot
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        public bool LoadPositionFromDrop(string dropFilePathName, out DateTime localTimeLastFill)
        {
            m_DropSnapshotNextTime = Log.GetTime().Add(m_DropSnapshotWait);
            if (!System.IO.File.Exists(dropFilePathName))
            {                                                           // this is fine
                Log.NewEntry(LogLevel.Major, "LoadPositionFromDrop:  No fill file found at {0}.", dropFilePathName);
                localTimeLastFill = new DateTime();
                return false;
            }


            UV.Lib.IO.BackwardReader backReader = new UV.Lib.IO.BackwardReader(dropFilePathName);
            bool isMoreToRead = true;
            bool foundMostRecentSnapshot = false;
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            List<string> singles = new List<string>();
            char[] lineDelimiters = new char[] { ',' };                    // what separates each term in a Line/
            char[] pairDelimiters = new char[] { '=' };                    // what separates keys and values.
            DateTime lastSnapshotTime = DateTime.MinValue;
            List<FillEventArgs> dropFillsFound = new List<FillEventArgs>();

            while (isMoreToRead)
            {
                if (backReader.SOF)
                {
                    isMoreToRead = false;
                    break;                                              // wend
                }
                string aLine = backReader.Readline();
                if (string.IsNullOrEmpty(aLine))
                    continue;                                           // skip blank lines.
                pairs.Clear();
                singles.Clear();
                GetKeyValues(aLine, lineDelimiters, pairDelimiters, ref pairs, ref singles);

                // These lines are specific to application.
                // Could be a delegate:   bool AnalyzeLine(ref singles, ref pairs) return true if we should continue....
                //  problem is the use of foundMostRecentSnapshot to stop reading.
                if (singles.Contains("Fill"))                         // found a fill event in the drop file.
                {
                    if (!foundMostRecentSnapshot)
                        dropFillsFound.Add(CreateFillEvent(pairs));     // These fills came in after the last snapshot, so keep them.
                    else
                        isMoreToRead = false;                           // we have read past the most recent snapshot, can stop now.
                }
                else if (singles.Contains("Snapshot"))
                {
                    string s;
                    if (!foundMostRecentSnapshot)
                    {
                        foundMostRecentSnapshot = true;                 // First time we found a snapshot entry!
                        if (pairs.TryGetValue("Time", out s))
                        {
                            lastSnapshotTime = Convert.ToDateTime(s);
                            Log.NewEntry(LogLevel.Major, "LoadPositionFromDrop: Most recent snapshot found for time: {0}.", lastSnapshotTime.ToString(Strings.FormatDateTimeZone));
                        }
                        else
                            Log.NewEntry(LogLevel.Error, "LoadPositionFromDrop: No time stamp found in line = {0}", aLine);
                    }
                    if (pairs.TryGetValue("Time", out s) && Convert.ToDateTime(s).Equals(lastSnapshotTime))
                    {
                        Log.NewEntry(LogLevel.Major, "LoadPositionFromDrop: Loading snapshot = {0} ", aLine);
                        m_FillHub.HubEventEnqueue(CreateFillEvent(pairs));
                    }
                    else
                        isMoreToRead = false;

                }
                else if (singles.Contains("InstrumentDetail"))          // We can load details of insrtruments with snapshots, if desired.
                {
                    string s;
                    if (foundMostRecentSnapshot && pairs.TryGetValue("Time", out s) && Convert.ToDateTime(s).Equals(lastSnapshotTime))
                    {
                        Log.NewEntry(LogLevel.Major, "InstrumentDetail = {0} ", aLine);// Only need to keep these for the TT Keys, if we remove them from the fills.                        
                    }
                    else
                        isMoreToRead = false;
                }
                //
                //
                //
            }//wend isMoreToRead
            m_LoadedTransactionTime = lastSnapshotTime;
            m_DropSnapshotLastTime = lastSnapshotTime;
            // Now, if we discovered any fills after the last drop time stamp, process these.
            foreach (FillEventArgs e in dropFillsFound)
            {
                if (e.Fill.LocalTime.CompareTo(m_LoadedTransactionTime) > 0)
                    m_LoadedTransactionTime = e.Fill.LocalTime;                            // Update the initial time, since these are part of our snapshot, in a way.
                m_FillHub.HubEventEnqueue(e);
            }
            localTimeLastFill = m_LoadedTransactionTime;
            return true;
        }// LoadFrom()
        //       
        //
        //
        private void GetKeyValues(string aLine, char[] elementDelim, char[] keyPairDelim, ref Dictionary<string, string> keyValuePairs, ref List<string> singleArguments)
        {
            string[] parts = aLine.Split(elementDelim, StringSplitOptions.RemoveEmptyEntries);
            foreach (string aPart in parts)
            {
                string[] elements = aPart.Split(keyPairDelim, StringSplitOptions.RemoveEmptyEntries);
                if (elements.Length == 1 && !singleArguments.Contains(elements[0].Trim()))
                    singleArguments.Add(elements[0].Trim());
                else if (elements.Length == 2 && !keyValuePairs.ContainsKey(elements[0].Trim()))
                    keyValuePairs.Add(elements[0].Trim(), elements[1].Trim());
            }

        }
        /// <summary>
        /// Creates a fill event from the pair-wise info in drop.  
        /// Allowed keys:
        ///     Time - the time the drop was made.
        ///     LocalFillTime - local time fill was received.
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns></returns>
        private FillEventArgs CreateFillEvent(Dictionary<string, string> pairs)
        {

            string sQty;
            string sPrice;
            if (pairs.TryGetValue("Qty", out sQty) && pairs.TryGetValue("Price", out sPrice))
            {
                UV.Lib.OrderHubs.Fill aFill = UV.Lib.OrderHubs.Fill.Create();
                aFill.Qty = Convert.ToInt32(sQty);
                aFill.Price = Convert.ToDouble(sPrice);

                // Extract fill times.
                if (pairs.ContainsKey("LocalFillTime"))
                    aFill.LocalTime = Convert.ToDateTime(pairs["LocalFillTime"]);                    // use fill time, if available.
                else
                    aFill.LocalTime = Convert.ToDateTime(pairs["Time"]);                             // else use last drop time- legacy approach

                // Extract TT's instrument key.
                InstrumentKey key;
                string sForeignKey;
                if (pairs.TryGetValue("ForeignKey", out sForeignKey) && TTConvert.TryCreateInstrumentKey(sForeignKey, out key))
                {
                    FillEventArgs e = new FillEventArgs(key,FillType.InitialPosition, aFill);
                    return e;   // success!
                }
                else
                {
                    Log.NewEntry(LogLevel.Error, "Failed to recreate instrument key from {0}.", sForeignKey);
                    return null;
                }
            }
            else
            {
                Log.NewEntry(LogLevel.Error, "Failed to create a fill event");
                return null;
            }
        }// CreateFillEvent()
        //
        // 
        #endregion//Private Methods



    }
}

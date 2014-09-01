using System;
using System.Collections.Generic;
using System.Text;
using Google.GData.Client;
using Google.GData.Spreadsheets;
using Google.GData.Extensions;

namespace Ambre.Breconcile.Reconciler
{
    using Misty.Lib.Application;
    using Misty.Lib.Hubs;
    //       
    //
    // *********************************************
    // ****             BreUserTags             ****
    // *********************************************
    public class BreUserTags
    {
        public string Main = string.Empty;                                              // main owner
        public string Tag = string.Empty;                                               // descriptive tag
        public string Number = string.Empty;                                            // account id# substring (not necessarily the complete acct number).
        public string SpoofStr = string.Empty;                                          // TT spoof ID.
        private static LogHub Log = null;
        private const string targetFileName = "AccountTags";

        public BreUserTags(string main, string tag, string number, string spoofString)
        {
            this.Main = main;
            this.Tag = tag;
            this.Number = number;
            this.SpoofStr = spoofString.Trim();
            //string[] elems = spoofString.Split(' ');
            //this.SpoofList = new List<string>(elems.Length);
            //foreach (string elem in elems)
            //this.SpoofList.Add(elem.Trim());
        }
        public override string ToString()
        {
            return string.Format("{0}-{1}", this.Main, this.Tag);
        }
        //
        // *************************************************
        // ****             Try Create()                ****
        // *************************************************
        /// <summary>
        /// Try to create a list of BreUserTags objects for each clearing firm found in the file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="createdTags"></param>
        /// <returns></returns>
        public static bool TryCreate(string fileName, out Dictionary<string, List<BreUserTags>> createdTags, string googleName, string googlePassword, LogHub log)
        {
            if (Log == null)
                Log = log;

            createdTags = null;

            // Read the entire file once.
            List<string> fileRows = new List<string>();
            string filePath = fileName;
            try
            {
                string aLine = string.Empty;

                if (!string.IsNullOrEmpty(googleName) && googlePassword != null)
                {
                    SpreadsheetsService myService = new SpreadsheetsService("exampleCo-exampleApp-1");
                    myService.setUserCredentials(googleName, googlePassword);
                    SpreadsheetQuery query = new SpreadsheetQuery();
                    SpreadsheetFeed feed = myService.Query(query);
                    Log.NewEntry(LogLevel.Minor, "Loading account tags file from the google drive");

                    foreach (SpreadsheetEntry entry in feed.Entries)
                    {
                        if (!entry.Title.Text.Equals(targetFileName))
                            continue;

                        Log.NewEntry(LogLevel.Minor, entry.Title.Text);
                        WorksheetFeed wsFeed = entry.Worksheets;
                        WorksheetEntry worksheet = (WorksheetEntry)wsFeed.Entries[0];
                        AtomLink listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);
                        ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
                        ListFeed listFeed = myService.Query(listQuery);
                        string header = "RCG.number,ABN.number,main,tag,RCG.spoof,ABN.spoof";
                        fileRows.Add(header);
                        foreach (ListEntry row in listFeed.Entries)
                        {
                            string tagString = string.Empty;
                            foreach (ListEntry.Custom element in row.Elements)
                            {
                                if (tagString != string.Empty)
                                    tagString += ",";
                                tagString += element.Value;
                                //fileRows.Add(element.Value);
                            }
                            fileRows.Add(tagString);
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                    {
                        using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
                        {
                            while ((aLine = reader.ReadLine()) != null)
                            {
                                aLine = aLine.Replace("\"", "");
                                aLine = aLine.Trim();
                                if (!string.IsNullOrEmpty(aLine))
                                    fileRows.Add(aLine);
                            }
                            reader.Close();
                        }
                    }
                    else
                    {
                        Log.NewEntry(LogLevel.Warning, "The filePath is null or empty:{0} and it may not exist on the file server", filePath);
                        return false;
                    }
                }

            }
            catch (Exception e)
            {
                Log.NewEntry(LogLevel.Warning, "There is an error in loading account tags file, the error information is {0}", e);
                return false;
            }
            //
            // Read headers collecting all clearing firm names.
            //
            string[] elems = fileRows[0].Split(',');
            Dictionary<string, int[]> columnIDs = new Dictionary<string, int[]>();
            int[] universalColumnIDs = new int[2];
            for (int col = 0; col < elems.Length; ++col)
            {
                if (elems[col].Contains(".number"))                         // this is item #0
                {
                    int n = elems[col].IndexOf('.');
                    string s = elems[col].Substring(0, n).Trim();       // this is a clearing firm name.
                    if (!columnIDs.ContainsKey(s))
                        columnIDs.Add(s, new int[2]);
                    columnIDs[s][0] = col;
                }
                else if (elems[col].Contains(".spoof"))                     // this is item #1
                {
                    int n = elems[col].IndexOf('.');
                    string s = elems[col].Substring(0, n).Trim();       // this is a clearing firm name.
                    if (!columnIDs.ContainsKey(s))
                        columnIDs.Add(s, new int[2]);
                    columnIDs[s][1] = col;
                }
                else if (elems[col].Contains("main"))
                    universalColumnIDs[0] = col;
                else if (elems[col].Contains("tag"))
                    universalColumnIDs[1] = col;
            }
            // Create return dictionaies, a list of rows for each clearing firm.
            createdTags = new Dictionary<string, List<BreUserTags>>(columnIDs.Count);
            foreach (string clearingName in columnIDs.Keys)
                createdTags.Add(clearingName, new List<BreUserTags>());

            //
            // Load each row
            //
            for (int row = 1; row < fileRows.Count; ++row)
            {
                elems = fileRows[row].Split(',');
                string main = elems[universalColumnIDs[0]].Trim();
                string tag = elems[universalColumnIDs[1]].Trim();

                foreach (string clearingName in columnIDs.Keys)
                {
                    BreUserTags bre = new BreUserTags(main, tag, elems[columnIDs[clearingName][0]].Trim(), elems[columnIDs[clearingName][1]].Trim());
                    createdTags[clearingName].Add(bre);
                }
            }

            return true;
        }//TryCreate()
        //
        //
        //
        // *************************************************
        // ****     GetUserNamesForThisClearing         ****
        // *************************************************
        public static void GetUserNamesForThisClearing(string clearingAcctNumber, List<BreUserTags> allUserTags, List<string> allUserNames,
            out BreUserTags resultingUserTag, out List<string> resultingUserNames)
        {
            resultingUserTag = null;
            resultingUserNames = null;

            //
            // Search thru AllUserTags looking for information about this clearing Account number.
            //                
            int n = 0;
            while (n < allUserTags.Count && resultingUserTag == null)
            {
                int minLen = Math.Min(allUserTags[n].Number.Length, clearingAcctNumber.Length);
                string s1 = allUserTags[n].Number.Substring(allUserTags[n].Number.Length - minLen, minLen).ToUpper();     // clearing number of this userTag
                string s2 = clearingAcctNumber.Substring(clearingAcctNumber.Length - minLen, minLen).ToUpper();         // clearing account number we are considering.
                if (s1.Equals(s2))
                    resultingUserTag = allUserTags[n];
                n++;
            }
            resultingUserNames = new List<string>();
            if (resultingUserTag == null)
                resultingUserTag = new BreUserTags("?", "?", clearingAcctNumber, string.Empty);// We have NOT found anything about this clearing account.
            else
            {   // We have found a userTag that corresponds to this clearing account number.
                // Look for matching userNames in the list, if found add them to the output list.
                foreach (string name in allUserNames)
                    if (!string.IsNullOrEmpty(resultingUserTag.SpoofStr) && resultingUserTag.SpoofStr.Contains(name))      // If clearing acct number appears in the ambre-file name, we assume a match!
                        resultingUserNames.Add(name);
            }
            // Look for generic match based on similar account numbers.
            foreach (string name in allUserNames)
                if (name.Contains(clearingAcctNumber) && (!resultingUserNames.Contains(name)))
                    resultingUserNames.Add(name);
        }
    }//end BreUserTag class
    //
    //
}

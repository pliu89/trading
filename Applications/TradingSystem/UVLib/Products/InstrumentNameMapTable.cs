using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace UV.Lib.Products
{
    /// <summary>
    /// This class represents a table to map InstrumentNames (Products) from one
    /// API to another.  For example, it has been used to reconcile the Ambre application 
    /// to RCG's clearing.  In this case, Ambre InstrumentNames are based on the names used
    /// in TT API.  Since RCG has their own naming convention for instruments and products, 
    /// we created an instance of this class to load itself from a file, and provide the 
    /// mapping from RCG to TTApi names.
    /// Features:
    ///     1) It is able to load itself from a comma-delimited file.
    ///     2) It allows addition pairs to be added to the table during runtime.
    ///     3) It is able to save itself to a comma-delimited file.
    ///     4) I plan to allow the mappings to be many-to-many.  This happens in the case
    ///         when two Ambre users trade the ten-year future on TTApi where the same instr
    ///         has two names CME.ZN (Future) and CBOT.ZN (Future).  (Clearing firms seem to 
    ///         have only ONE name for each instrument.)
    /// </summary>
    public class InstrumentNameMapTable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        // Control Variables
        //
        public string FilePath = string.Empty;


        //
        // Internal variables
        private List<InstrumentName> m_KeyList = new List<InstrumentName>();
        private List<InstrumentName> m_ValueList = new List<InstrumentName>();
        private int m_SavedInstrumentCount = 0;                   // keep track of the number of instruments originally loaded.
       
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath">Full path of file with table.</param>
        public InstrumentNameMapTable(string filePath)
        {
            this.FilePath = filePath;
            ReadTable();
        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Indicates whether there are new entries since we loaded/saved table to file.
        /// </summary>
        public bool IsNewEntries
        {
            get { return m_KeyList.Count > m_SavedInstrumentCount; }         
        }
        //
        //
        public int Count
        {
            get { return m_KeyList.Count; }
        }
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        public bool TryGetValue(InstrumentName key, ref List<InstrumentName> matchingValues, bool useExactMatch=true)
        {
            return this.TryFindMappings(key, ref matchingValues, ref m_KeyList, ref m_ValueList, useExactMatch);
        }
        public bool TryGetKey(InstrumentName aValue, ref List<InstrumentName> matchingKeys, bool useExactMatch=true)
        {
            return this.TryFindMappings(aValue, ref matchingKeys, ref m_ValueList, ref m_KeyList, useExactMatch);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memberOfList1"></param>
        /// <param name="matchingMembersOfList2"></param>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <param name="useExactMatch">false -> matching product part only</param>
        /// <returns></returns>
        private bool TryFindMappings(InstrumentName memberOfList1, ref List<InstrumentName> matchingMembersOfList2, ref List<InstrumentName> list1, ref List<InstrumentName> list2
                ,bool useExactMatch=true)
        {
            int nCount = matchingMembersOfList2.Count;
            for (int i = 0; i < list1.Count; ++i)
            {
                if (memberOfList1.Equals(list1[i]))                     // we have located a matching in list1 at this index i.
                {   
                    InstrumentName mappedInstrument = list2[i];         // this is the output of that entry of list1.
                    matchingMembersOfList2.Add(mappedInstrument);       // add it to the outgoing list.
                }
                else if (!useExactMatch)                                // alternatively, the user may only want partial matches.
                {   // Look for near matches.                           // These have two restrictions, mapping is a product entry (not specific instr).
                    if (list1[i].IsProduct && memberOfList1.Product.Equals(list1[i].Product))// and the products must match exactly.
                    {   // The entry in list1 seems to be a product that matches users instr.
                        InstrumentName mappedInstrument = list2[i];     // So, grab the output of the map, and store it.
                        matchingMembersOfList2.Add(mappedInstrument);
                    }
                }
            }
            // Exit
            return (matchingMembersOfList2.Count > nCount);
        }// TryFindMatch()
        //
        //
        // ****                     Add()                   ****
        //
        public void Add(InstrumentName key, InstrumentName value)
        {
            m_KeyList.Add(key);
            m_ValueList.Add(value);

        }// Add()
        //
        //
        //
        //
        //
        // ****             TryGetNewEntries()              ****
        //
        /// <summary>
        /// If there are new entries in the mapping, then a true is returned and the new entries 
        /// are returned. 
        /// </summary>
        /// <param name="newEntries1"></param>
        /// <param name="newEntries2"></param>
        /// <returns></returns>
        public bool TryGetNewEntries(out List<InstrumentName> newEntries1, out List<InstrumentName> newEntries2)
        {
            newEntries1 = null;
            newEntries2 = null;
            if (IsNewEntries)
            {
                newEntries1 = new List<InstrumentName>();
                newEntries2 = new List<InstrumentName>();
                for (int i = m_SavedInstrumentCount; i < m_KeyList.Count; ++i)
                {
                    newEntries1.Add(m_KeyList[i]);
                    newEntries2.Add(m_ValueList[i]);
                }// next i
                return true;
            }
            else
                return false;
        }// TryGetNewEntries()
        //
        //
        //
        // ****             Save Table()                ****
        //
        // Product table format:   breProductName, RcgProductName
        public void SaveTable(string newFilePath = "")
        {
            string filePath;
            if (string.IsNullOrEmpty(newFilePath))
                filePath = this.FilePath;
            else
                filePath = newFilePath;

            // Lets alphabetize this table
            List<InstrumentName> sortingList = new List<InstrumentName>(m_KeyList);
            sortingList.Sort(new InstrumentNameComparer());
            int searchFromIndex = 0;
            InstrumentName lastInstrName = new InstrumentName();
            using (System.IO.StreamWriter stream = new System.IO.StreamWriter(filePath, false))
            {
                foreach (InstrumentName instrName in sortingList)
                {
                    if (!instrName.Equals(lastInstrName))
                        searchFromIndex = 0;                                // instrName is different from last one, so start search from beginning index.
                    lastInstrName = instrName;                              // Remember this instrName.
                    // Search for entry.
                    int n1 = m_KeyList.IndexOf(instrName, searchFromIndex);   // location of this instrName entry.
                    searchFromIndex = n1 + 1;
                    // Write the entries now.
                    InstrumentName instr1 = m_KeyList[n1];
                    InstrumentName instr2 = m_ValueList[n1];
                    stream.WriteLine("{0,32},{1,32}", InstrumentName.Serialize(instr1).Trim(), InstrumentName.Serialize(instr2).Trim());
                }// next instrName
                stream.Write("// END");
                stream.Close();
                m_SavedInstrumentCount = sortingList.Count;               // update instrument count
            }
        }// SaveTable
        //
        //
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        // ****             Read Product Table()                ****
        //
        // Product table format:   breProductName, RcgProductName
        private void ReadTable()
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(this.FilePath))
            {
                string aLine;
                bool continueReading = true;
                while (continueReading)
                {
                    aLine = reader.ReadLine();
                    if (aLine==null)
                        break;
                    aLine = aLine.Trim();
                    if (string.IsNullOrWhiteSpace(aLine))
                        continue;                                   // skip blank lines
                    if (aLine.StartsWith("// END", StringComparison.CurrentCultureIgnoreCase)) // this signals end of file at the moment.
                    {
                        continueReading = false;
                        continue;
                    }
                    else if (aLine.Contains("//"))
                    {
                        int n = aLine.IndexOf("//");
                        if (n == 0)
                            continue;
                        else if (n > 0)
                            aLine = aLine.Substring(0, n);
                    }
                        
                    //
                    // Extract table entries
                    //
                    string[] elements = aLine.Split(',');                   
                    if (elements.Length >= 2)
                    {   // Need two elements
                        InstrumentName instr1;
                        InstrumentName instr2;
                        if (InstrumentName.TryDeserialize(elements[0].Trim(),out instr1) && InstrumentName.TryDeserialize(elements[1].Trim(),out instr2)) 
                        {
                            m_KeyList.Add(instr1);
                            m_ValueList.Add(instr2);
                        }
                    }
                }//wend
                reader.Close();
                m_SavedInstrumentCount = m_KeyList.Count;           // update the instrument Count.
            }
        }// ReadTable()
        //
        //
        //
        //
        //
        //
        #endregion//Private Methods


        #region Static Functions
        // *****************************************************************
        // ****                  Static Functions                        ****
        // *****************************************************************
        //
        //
        public static bool TryExtractExpiryFromSeriesName(InstrumentName name1, out DateTime expiryDate)
        {
            expiryDate = DateTime.MinValue;
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
            if (DateTime.TryParseExact(name1.SeriesName, "MMMyy", ci, System.Globalization.DateTimeStyles.None, out expiryDate))
                return true;
            else if (DateTime.TryParseExact(name1.SeriesName, "yyyy/MM", ci, System.Globalization.DateTimeStyles.None, out expiryDate))
                return true;
            else if (DateTime.TryParseExact(name1.SeriesName, "MM/yyyy", ci, System.Globalization.DateTimeStyles.None, out expiryDate))
                return true;
            else if (DateTime.TryParseExact(name1.SeriesName, "ddMMMyy", ci, System.Globalization.DateTimeStyles.None, out expiryDate))
                return true;
            else
                return false;
        }//
        //
        //
        //
        #endregion//Event Handlers

    }
}

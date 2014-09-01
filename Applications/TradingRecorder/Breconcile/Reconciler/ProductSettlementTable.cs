using System;
using System.Collections.Generic;
using System.Text;


namespace Ambre.Breconcile.Reconciler
{
    using Misty.Lib.Products;

    // *****************************************************************************
    // ****                         ProductSettlementTable                      ****
    // *****************************************************************************
    /// <summary>
    /// This class represents a table that holds the settlement timing information 
    /// for all products and exchanges.
    /// Exchanges and products are loaded as general rules, and exceptions to those, 
    /// with specific rules override more general rules for the same product.
    /// </summary>
    public class ProductSettlementTable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        private List<ProductSettlementEntry> Rows = new List<ProductSettlementEntry>();
        public bool IgnoreCaseOfNames = true;                       // Comparisons between exchanges, products etc ignore case.


        #endregion// members

        #region Constructors & Creators
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        private ProductSettlementTable()
        {
        }
        //
        /// <summary>
        /// Method used to create a Settlement table from a file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="newTable"></param>
        /// <returns>true if table is created</returns>
        public static bool TryCreate(string filePath, out ProductSettlementTable newTable, DateTime settlement)
        {
            newTable = null;
            //
            // Load file, remove comments.
            //
            string[] fileLines = null;
            try
            {
                fileLines = System.IO.File.ReadAllLines(filePath);     // raw lines from file.
            }
            catch(Exception)
            {
                return false;
            }
            List<string> cleanLines = new List<string>();                   // lines cleaned of comments etc.
            foreach (string aLine in fileLines)
            {
                int n = aLine.IndexOf("//");
                string s = aLine;
                if (n > -1)
                    s = aLine.Substring(0, n);
                s = s.Trim();
                if (!string.IsNullOrEmpty(s))
                    cleanLines.Add(s);
            }
            //
            // Create table.
            //
            newTable = new ProductSettlementTable();
            foreach (string aLine in cleanLines)
            {
                ProductSettlementEntry newEntry;
                if (ProductSettlementEntry.TryCreate(aLine, out newEntry, settlement))
                {
                    newEntry.IgnoreCaseOfNames = newTable.IgnoreCaseOfNames;
                    newTable.Rows.Add(newEntry);
                    // Report
                    StringBuilder s = new StringBuilder();
                    TimeZoneInfo tzLocal = TimeZoneInfo.Local;
                                        
                    DateTime settle = DateTime.Now.Date;
                    DateTime foreignSettle = new DateTime(settle.Year, settle.Month, settle.Day, 0, 0, 0, DateTimeKind.Unspecified);
                    foreignSettle = foreignSettle.Add(newEntry.SettleTime);
                    TimeZoneInfo foreignTZ = newEntry.TZInfo;
                    try
                    {
                        DateTime settleLocal = TimeZoneInfo.ConvertTime(foreignSettle, foreignTZ, tzLocal);
                        s.AppendFormat("{0} ---> Local Settlement: {1}", newEntry, settleLocal);
                    }
                    catch (Exception e)
                    {
                        s.AppendFormat("{0}", e.Message);
                    }                    
                    Console.WriteLine(s.ToString());

                }
            }
            newTable.Rows.Sort(ProductSettlementEntry.CompareEntriesBySpecificity);
            return true;
        }//TryCreate()
        //
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
        // 
        //
        // *************************************************
        // ****         GetSettlementOffsets()          ****
        // *************************************************
        /// <summary>
        /// Compiles a list of all unique hour offsets for settlements.
        /// </summary>
        /// <returns></returns>
        public List<int> GetSettlementOffsets()
        {
            List<int> offsets = new List<int>();
            foreach (ProductSettlementEntry entry in this.Rows)
            {
                if (!offsets.Contains(entry.MinuteOffset))
                    offsets.Add(entry.MinuteOffset);
            }
            return offsets;
        }// GetSettlementOffsets()
        //
        //
        // *************************************************
        // ****                  ****
        // *************************************************
        public bool TryFindMatchingEntry(InstrumentName instrumentName, out ProductSettlementEntry foundEntry)
        {
            foundEntry = null;
            foreach (ProductSettlementEntry entry in this.Rows)
            {
                if (entry.IsMatching(instrumentName))
                {
                    foundEntry = entry;         // since entries sorted from MOST specific to least, as soon as we find a match, we break.
                    return true;
                }
            }
            return false;
        }// TryFindMatchingEntry()
        //
        //
        // *************************************************
        // ****            ToString()                   ****
        // *************************************************
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            foreach (ProductSettlementEntry entry in this.Rows)
            {
                s.AppendFormat("    {0}\r\n",entry.ToString());
            }
            return s.ToString();
        }//ToString().

        //
        //
        #endregion//Public Methods

        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods

    }//end product settlement table class



    // *****************************************************************************
    // ****                         ProductSettlementEntry                      ****
    // *****************************************************************************
    public class ProductSettlementEntry
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Public members:
        //
        public string Exchange = string.Empty;
        public string Product = string.Empty;
        public ProductTypes Type = ProductTypes.Unknown;

        // Local time offset from midnight.
        public int MinuteOffset = 0;                                    // Minutes from Local-Midnight time this instr will settle.

        // Foreign time zone information.
        public TimeZoneInfo TZInfo = null;
        public TimeSpan SettleTime;

        
        public bool IgnoreCaseOfNames = true;

        // Private
        private const int NumberOfFields = 4;                           // the string that is passed to creator must have this many fields!!
        private const int MinutesPerHour = 60;
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        private ProductSettlementEntry()
        {
        }
        //
        public static bool TryCreate(string aLine, out ProductSettlementEntry newEntry, DateTime settmentDate)
        {
            newEntry = null;
            string[] elements = aLine.Split(',');
            if (elements.Length < ProductSettlementEntry.NumberOfFields)
                return false;            
            newEntry = new ProductSettlementEntry();
            // Load - Exchange name
            int elem = 0;
            if (!string.IsNullOrWhiteSpace(elements[elem]))
                newEntry.Exchange = elements[elem].Trim();
            elem++;
            // Load - Product name
            if (!string.IsNullOrWhiteSpace(elements[elem]))
                newEntry.Product = elements[elem].Trim();
            elem++;
            // Load - Product type       
            if (!string.IsNullOrWhiteSpace(elements[elem]))
            {
                ProductTypes type;
                if (Enum.TryParse<ProductTypes>(elements[elem].Trim(), out type))
                    newEntry.Type = type;
            }
            elem++;
            //
            // Load - Timezone name
            //
            if (!string.IsNullOrWhiteSpace(elements[elem]))
            {
                TimeZoneInfo tzInfo = null;
                try
                {
                    tzInfo = TimeZoneInfo.FindSystemTimeZoneById(elements[elem].Trim());
                    newEntry.TZInfo = tzInfo;
                }
                catch (Exception)
                {
                    return false;
                }                
            }
            elem++;
            // Load - settlement time in local time of exchange.            
            if (!string.IsNullOrWhiteSpace(elements[elem]))
            {
                TimeSpan time;
                if (TimeSpan.TryParse(elements[elem].Trim(), out time))
                {
                    //newEntry.MinuteOffset = (int)(MinutesPerHour * x);
                    newEntry.SettleTime = time;//.Subtract(time.TimeOfDay);
                }
                else
                    return false;                       // This is necessary!
            }
            elem++;


            // Exit.
            return newEntry.TryInitialize(settmentDate); ;
        }//TryCreate()
        //
        //
        // *************************************************
        // ****             Initialize()                ****
        // *************************************************
        public bool TryInitialize(DateTime settlementDate)
        {
            // Compute offset date
            

            // Compute the offset minutes.
            DateTime foreignSettle = new DateTime(settlementDate.Year, settlementDate.Month, settlementDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
            foreignSettle = foreignSettle.Add(this.SettleTime);                 // this is settlement time (in foreign timezone).
            try
            {
                DateTime settleLocal = TimeZoneInfo.ConvertTime(foreignSettle, this.TZInfo, TimeZoneInfo.Local);    // local (Ambre) time we should settle this instrument.
                int dayOffset = settleLocal.Date.Subtract(foreignSettle.Date).Days;
                this.MinuteOffset = settleLocal.Hour * 60 + settleLocal.Minute + dayOffset * (24*60);                
            }
            catch (Exception)
            {
                this.MinuteOffset = 0;
                return false;                
            }
            return true;
        }//Initialize()
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
        //
        //
        //
        // ****             IsMatching()            ****
        //
        /// <summary>
        /// Compares each field that is defined in this entry against the InstrumentName fields.
        /// As soon as one doesn't match, we can reject the instrument as NOT matching.
        /// Otherwise, we return true.
        /// </summary>
        /// <param name="instrument"></param>
        /// <returns></returns>
        public bool IsMatching(InstrumentName instrument)
        {            
            StringComparison sc = StringComparison.CurrentCultureIgnoreCase;
            if (! IgnoreCaseOfNames)
                sc = StringComparison.CurrentCulture;
            // Test this instrument.
            if (!string.IsNullOrWhiteSpace(this.Product) && !string.Equals(this.Product, instrument.Product.ProductName, sc))
                return false;
            if (!string.IsNullOrWhiteSpace(this.Exchange) && !string.Equals(this.Exchange, instrument.Product.Exchange, sc))
                return false;
            if (this.Type != ProductTypes.Unknown && this.Type != instrument.Product.Type)
                return false;
            // Failed to find a reason to fail this instrument, so they match!
            return true;
        }// IsMatching()
        //
        //
        // ****             ToString()              ****
        public override string ToString()
        {
            string name = string.Format(m_Format0,
                string.IsNullOrWhiteSpace(this.Exchange) ? "*" : this.Exchange,
                string.IsNullOrWhiteSpace(this.Product) ? "*" : this.Product,
                this.Type == ProductTypes.Unknown ? "*" : this.Type.ToString());
            return string.Format(m_Format1,name,this.SettleTime,this.TZInfo.BaseUtcOffset.TotalHours,(this.MinuteOffset / 60.0));
            //return string.Format("[{0}.{1} ({2}) {3}]",this.Exchange,this.Product,this.Type.ToString(),this.HourOffset);
        }
        private const string m_Format0 = "{0}.{1} ({2})";
        private const string m_Format1 = "    {0,30} -> {1,12}({2}) settle:{3,5} local";
        //
        //
        #endregion // Public methods



        #region Public Static Methods
        // *****************************************************************
        // ****              Public Static Methods                      ****
        // *****************************************************************
        //
        /// <summary>
        /// Used for sorting, returns something like sign(x - y) = {+1,-1,0}.
        /// Earlier items are "smaller", so we want to make more specific entries "smaller" 
        /// so that it looping thru we stop as soon as there is a match.
        /// Most specific entries define all (Exchange,ProductType,Product) properties have smalles values.
        /// then Exchange,ProductType, then Exchange, then empty.
        /// The null entry says nothing, so its the largest object.  As such, this comparison views 
        /// the entries as the "size" of their Venn diagrams.
        /// </summary>
        /// <returns>+1,-1, 0 = sign(x-y)</returns>
        public static int CompareEntriesBySpecificity(ProductSettlementEntry x, ProductSettlementEntry y)
        {
            if (x == null)
            {
                if (y == null)
                    return 0;       // both are equal
                else
                    return +1;      // x is null, so its biggest.
            }
            else
            {
                if (y == null)
                    return -1;      // y is null, so its the biggest.
                else
                {   // Neither are null, must really compare them.
                    // Product names first:
                    if (string.IsNullOrWhiteSpace(x.Product))
                    {   // x has no product field, it looks pretty general (big).
                        if (!string.IsNullOrWhiteSpace(y.Product))
                            return +1;                                  // y is more specific, smaller!
                    }
                    else
                    {   // x has a product field.
                        if (string.IsNullOrWhiteSpace(y.Product))
                            return -1;                                  // y is more general, its bigger!
                        else
                            return string.Compare(x.Product, y.Product);// both very specific, user alphabetical order.
                    }
                    // ProductType test:
                    // Last test was not conclusive, (neither have product name).
                    if (x.Type == Misty.Lib.Products.ProductTypes.Unknown)
                    {   // x has no type!
                        if (y.Type != Misty.Lib.Products.ProductTypes.Unknown)
                            return +1;                                  // y is more specific, smaller.
                    }
                    else
                    {   // x has a type!
                        if (y.Type == Misty.Lib.Products.ProductTypes.Unknown)
                            return -1;                                  // y is more general, bigger.
                        else
                            return x.Type.CompareTo(y.Type);
                    }
                    // Exchange test:
                    if (string.IsNullOrWhiteSpace(x.Exchange))
                    {
                        if (string.IsNullOrWhiteSpace(y.Exchange))
                            return +1;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(y.Exchange))
                            return -1;                                  // y is more general, bigger.
                        else
                            return string.Compare(x.Exchange, y.Exchange, true);
                    }
                    // No test could distinguish between x,y; they are equal.
                    return 0;
                }// neither x,y is null.
            }
        }//CompareEntriesBySpecificity()
        //
        //
        //
        //
        //
        #endregion//Public static Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods




    }//end product settlement table class



}

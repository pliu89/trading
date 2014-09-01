using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Breconcile.RCG
{
    using Misty.Lib.OrderHubs;
    using Misty.Lib.Products;

    using Reconciler;

    /// <summary>
    /// 
    /// This version creates InstrumentName objects for each of the description names 
    /// found in the statement. Note that these are not identical to those used in Ambre,
    /// only the format/object is similar.
    /// Version: 27 May 2013
    /// </summary>
    public class StatementReader2 : IClearingStatementReader
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        public readonly string m_FilePathForPosition;
        public readonly string m_FilePathForFills;

        // Resulting portfolio.
        public Dictionary<string, Dictionary<InstrumentName, List<Fill>>> m_PortfolioPosition = new Dictionary<string, Dictionary<InstrumentName, List<Fill>>>(); // [acct][instrDescr string] --> List<Fills>
        public Dictionary<string, Dictionary<InstrumentName, List<Fill>>> m_PortfolioFills = new Dictionary<string, Dictionary<InstrumentName, List<Fill>>>(); // [acct][instrDescr string] --> List<Fills>
        public Dictionary<string, Misty.Lib.Products.InstrumentName> m_InstrDescrToInstrName = new Dictionary<string, Misty.Lib.Products.InstrumentName>();
        public List<string> m_AccountMasterList = new List<string>();



        // Tables
        //public Dictionary<Misty.Lib.Products.Product, Misty.Lib.Products.Product> m_RcgToBreProduct = new Dictionary<Misty.Lib.Products.Product, Misty.Lib.Products.Product>();

        // Error messages
        public StringBuilder m_ErrorMessage = new StringBuilder();

        //
        // constants
        private char[] Delims = new char[] { ',' };
        private char[] DelimSpace = new char[] { ' ' };
        public static string[] FileNamePatterns = new string[] { "POS", "ST4" };       // names of important RCG files.
        private static string ProductTableName = "ProductTable.txt";                   // name of AMbre --> Rcg product names.
        private InstrumentNameMapTable m_ProductMapping = null;



        // POS & ST4 file:
        // Columns - zero based 
        private const int ST4_ENTRYTYPE     = 0;        // Indicates whether is "trade" or not.
        private const int COL_ACCT          = 4;        // COL denotes all statements use this common column.
        private const int COL_BUYSELL       = 9;
        private const int COL_UNSIGNEDQTY   = 10;
        private const int COL_INSTR         = 11;

        private const int COL_QUOTEPRICE    = 12;
        private const int POS_DECIMALPRICE  = 30;       // column for POS statement.
        private const int ST4_DECIMALPRICE  = 32;       // for ST4 trade statement.

        private const int COL_TRADEDATE     = 13;
        private const int COL_OPTIONFLAG    = 14;       // "O"ptions
        private const int COL_CONTRACTEXP   = 15;

        private const int COL_INSTRTYPE     = 17;       // "F"=future flag
        private const int COL_CALLPUT       = 18;       // "C"all / "P"ut flag?

        private const int POS_EXPIRATION    = 24;
        private const int POS_EXCHPRODNAME  = 27;       // Exchange "short" name
        private const int ST4_EXCHPRODNAME  = 29;       // Exchange "short" name



        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public StatementReader2(string positionFilePath, string fillsFillPath)
        {
            m_FilePathForPosition = positionFilePath;
            m_FilePathForFills = fillsFillPath;

            string filePath = string.Format("{0}{1}", Misty.Lib.Application.AppInfo.GetInstance().UserPath, ProductTableName);
            m_ProductMapping = new InstrumentNameMapTable(filePath);

            if ( ! string.IsNullOrEmpty(positionFilePath) )
                ReadStatement(m_FilePathForPosition,true);       // Loads "PortfolioPosition"            
            if ( ! string.IsNullOrEmpty(fillsFillPath) )
                ReadStatement(m_FilePathForFills,false);       // Loads "PortfolioFills" 

        }
        //       
        #endregion//Constructors


        #region Properties
        public Dictionary<string, Dictionary<InstrumentName, List<Fill>>> Position
        {
            get
            {
                return m_PortfolioPosition;
            }
        }
        public Dictionary<string, Dictionary<InstrumentName, List<Fill>>> Fills
        {
            get
            {
                return m_PortfolioFills;
            }
        }
        //public Dictionary<Product, Product> Clearing2BreProductMap
        //{
        //    get
        //    {
        //        return m_RcgToBreProduct;
        //    }
        //}
        public string PositionFilePath
        {
            get { return m_FilePathForPosition; }
        }
        public InstrumentNameMapTable InstrumentNameMap
        {
            get { return m_ProductMapping; }
        }
        //
        #endregion


        #region Public Methods
        // *****************************************************************
        // ****                  Public  Methods                        ****
        // *****************************************************************
        //
        //
        // *************************************************************
        // ****             Try Read Rcg Statement()                ****
        // *************************************************************
        /// <summary>
        /// Given the desired reconcilliation date, and a directory path,  
        /// we search for the appropriate statements, and if they exist, 
        /// we create a StatementReader object, and load it with the data.
        /// </summary>
        /// <param name="reconcilliationDate"></param>
        /// <param name="reader">new statement reader object</param>
        public static bool TryReadStatement(DateTime reconcilliationDate, string statementPath, 
            out RCG.StatementReader2 reader)
        {
            reader = null;
            string reconcilliationDateStr = reconcilliationDate.ToString("yyyyMMdd");   // This pattern appears in the desired files.

            //
            // Get list of reconcilliation reports.
            //
            List<string> desiredFilePathNames = new List<string>();
            List<string> allFilePathNames = new List<string>(System.IO.Directory.GetFiles(statementPath));
            foreach (string filePathName in allFilePathNames)                  // loop thru each filename until we find what we want.
            {
                bool isKeeper = false;                                  // keep the ones that match the desired patterns.
                string fileName = filePathName.Substring(filePathName.LastIndexOf('\\') + 1);
                isKeeper = fileName.Contains(reconcilliationDateStr);   // the date pattern appears in the file.
                if (isKeeper)
                {   // Now search for the patterns of interest as well.
                    foreach (string strPattern in FileNamePatterns)
                        isKeeper = isKeeper || fileName.Contains(strPattern);// must match ANY of the patterns...
                    if (isKeeper)
                        desiredFilePathNames.Add(filePathName);                  // keep this filename.
                }
            }//next filePathName

            // Read statement
            if (desiredFilePathNames.Count > 0)
            {
                string posFileName = string.Empty;
                string fillFileName = string.Empty;
                foreach (string s in desiredFilePathNames)
                {
                    if (s.Contains("POS"))
                        posFileName = s;
                    else if (s.Contains("ST4"))
                        fillFileName = s;
                }
                reader = new RCG.StatementReader2(posFileName, fillFileName);

                // Create a super-list of all rcg account names
                reader.m_AccountMasterList.AddRange( reader.m_PortfolioPosition.Keys );
                foreach (string acct in reader.m_PortfolioFills.Keys)
                    if (!reader.m_AccountMasterList.Contains(acct))
                        reader.m_AccountMasterList.Add(acct);

            }
            return (reader != null);
        }// TryReadRCGStatement()
        //
        #endregion


        #region Private Methods
        // *****************************************************************
        // ****                  Private Methods                        ****
        // *****************************************************************
        //
        //
        //
        // *********************************************************
        // ****             Read Product Table()                ****
        // *********************************************************
        // Product table format:   breProductName, RcgProductName
        /*
        private void ReadProductTable()
        {
            string filePath = string.Format("{0}{1}", Misty.Lib.Application.AppInfo.GetInstance().UserPath, ProductTableName);
            using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
            {
                string aLine;
                bool continueReading = true;
                while (continueReading)
                {
                    aLine = reader.ReadLine();
                    if (aLine == null || aLine.StartsWith("// END",StringComparison.CurrentCultureIgnoreCase) ) // this signals end of file at the moment.
                    {
                        continueReading = false;
                        continue;
                    }
                    if (aLine.StartsWith("//"))
                        continue;                                   // signifies a comment line.
                    if (string.IsNullOrWhiteSpace(aLine))
                        continue;                                   // skip blank lines
                   
                    // Extract 
                    string[] elements = aLine.Split(',');
                    Misty.Lib.Products.Product breProduct;
                    Misty.Lib.Products.Product rcgProduct;
                    if (elements.Length >= 2)
                    {
                        if (Misty.Lib.Products.Product.TryDeserialize(string.Format("{0} (Future)", elements[0]), out breProduct)  && 
                            Misty.Lib.Products.Product.TryDeserialize( string.Format("{0} (Future)",elements[1]), out rcgProduct) )                            
                        {
                            m_RcgToBreProduct.Add(rcgProduct,breProduct);
                        }                        
                    }
                }//wend
                reader.Close();
            }
        }// ReadProductTable()
        //
        //
        //
        // *************************************************************
        // ****             Save New Product Table()                ****
        // *************************************************************
        // Product table format: Bre, Rcg 
        public void SaveNewProductTable(Dictionary<Misty.Lib.Products.Product, Misty.Lib.Products.Product> table, string newFileName = "")
        {
            string filePath;
            if ( string.IsNullOrEmpty(newFileName) )
                filePath = string.Format("{0}{1}", Misty.Lib.Application.AppInfo.GetInstance().UserPath, ProductTableName);
            else if (newFileName.Contains(".") )
                filePath = string.Format("{0}{1}", Misty.Lib.Application.AppInfo.GetInstance().UserPath, newFileName);
            else
                filePath = string.Format("{0}{1}.txt", Misty.Lib.Application.AppInfo.GetInstance().UserPath, newFileName);
            // Lets alphabetize this table
            SortedDictionary<string, string> tableStr = new SortedDictionary<string, string>();
            foreach (Misty.Lib.Products.Product rcgProduct in table.Keys)
            {
                string breStr = Misty.Lib.Products.Product.Serialize(table[rcgProduct]);
                string rcgStr = Misty.Lib.Products.Product.Serialize(rcgProduct);                // rcg name
                if ( ! tableStr.ContainsKey( breStr ) )
                    tableStr.Add(breStr,rcgStr); 
            }
            using (System.IO.StreamWriter stream = new System.IO.StreamWriter(filePath,false))
            {
                foreach (string sBreName in tableStr.Keys)
                    stream.WriteLine("{0,32},{1,32}",sBreName,tableStr[sBreName]);  // RCG ---> BRE               
                stream.WriteLine("// END");
                stream.Close();
            }
        }// SaveNewProductTable
        */
        //
        //
        //
        // *****************************************************************
        // ****                 Read Statements()                       ****
        // *****************************************************************
        private void ReadStatement(string filePath, bool isPositionStatement)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
            {
                string aLine;
                while ((aLine = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(aLine))
                        continue;
                    string[] elements = aLine.Split(Delims, StringSplitOptions.None);
                    //
                    //
                    string acctName = string.Empty;
                    string instrDesc = string.Empty;
                    string instrType = string.Empty;
                    string prodSymbol = string.Empty;
                    Fill newFill = null;
                    try
                    {                        
                        string priceStr;
                        if (isPositionStatement)
                        {   // 
                            prodSymbol = elements[POS_EXCHPRODNAME];
                            priceStr = elements[POS_DECIMALPRICE].Trim();
                        }
                        else
                        {   // Fill statement
                            if (!elements[ST4_ENTRYTYPE].Equals("T"))
                                continue;
                            prodSymbol = elements[ST4_EXCHPRODNAME];
                            priceStr = elements[ST4_DECIMALPRICE].Trim();
                        }
                        acctName = elements[COL_ACCT];
                        instrDesc = elements[COL_INSTR];
                        instrType = string.Format("{0}{1}{2}", elements[COL_OPTIONFLAG], elements[COL_INSTRTYPE], elements[COL_CALLPUT]);  // concatenate all flags

                        // Extract effective fill
                        string sBuySell = elements[COL_BUYSELL];
                        int qtySign = Convert.ToInt32(3 - 2 * Convert.ToDouble(sBuySell));
                        int signedQty = Convert.ToInt32(qtySign * Convert.ToDouble(elements[COL_UNSIGNEDQTY]));
                        double price = 0;
                        price = Convert.ToDouble(priceStr);

                        newFill = new Fill();
                        DateTime tradeDate;
                        if (DateTime.TryParseExact(elements[COL_TRADEDATE], "yyyyMMdd", new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out tradeDate))
                        {   // Specific trade time is not supplied by clearing report.
                            newFill.LocalTime = tradeDate;
                            newFill.ExchangeTime = tradeDate;
                        }
                        newFill.Qty = signedQty;
                        newFill.Price = price;
                    }
                    catch (Exception e)
                    {
                        m_ErrorMessage.AppendFormat("Exception {0} extracting info from elements: {1}", e.Message, aLine);   // Write to a log file.
                        m_ErrorMessage.AppendLine();
                    }

                    //
                    // Create InstrumentName for this item
                    //
                    InstrumentName instrumentName;
                    if ( ! m_InstrDescrToInstrName.TryGetValue(instrDesc, out instrumentName) )
                    {   // This is the first encounter with this instrument Description.
                        // We store the table, so we have only to look up an instrument once.
                        Product product;
                        if (TryCreateInstrumentName(instrType, instrDesc, prodSymbol, out product, out instrumentName))
                            m_InstrDescrToInstrName.Add(instrDesc, instrumentName);
                    }

                    //
                    // Add position to lists
                    //
                    Dictionary<InstrumentName, List<Fill>> instrumentPortfolio;                 // Get a portfolio for this acctName
                    if (isPositionStatement)
                    {
                        if (!m_PortfolioPosition.TryGetValue(acctName, out instrumentPortfolio))
                        {
                            instrumentPortfolio = new Dictionary<InstrumentName, List<Fill>>();
                            m_PortfolioPosition.Add(acctName, instrumentPortfolio);
                        }
                    }
                    else
                    {
                        if (!m_PortfolioFills.TryGetValue(acctName, out instrumentPortfolio))
                        {
                            instrumentPortfolio = new Dictionary<InstrumentName, List<Fill>>();
                            m_PortfolioFills.Add(acctName, instrumentPortfolio);
                        }
                    }
                    List<Fill> fillList;
                    if (!instrumentPortfolio.TryGetValue(instrumentName, out fillList))
                    {
                        fillList = new List<Fill>();
                        instrumentPortfolio.Add(instrumentName, fillList);
                    }
                    if ( newFill != null)       // null can happen on exceptions
                        fillList.Add(newFill);

                }//wend
                reader.Close();
            }//using reader
        }// ReadStatements()
        //
        //
        //
        // *****************************************************************
        // ****                 Create InstrumentName()                 ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeCodeStr"></param>
        /// <param name="instrDescStr"></param>
        /// <param name="instrSymbolStr"></param>
        /// <param name="rcgProduct"></param>
        /// <param name="rcgInstrumentName"></param>
        /// <returns></returns>
        private bool TryCreateInstrumentName(string typeCodeStr, string instrDescStr, string instrSymbolStr, out Misty.Lib.Products.Product rcgProduct, out Misty.Lib.Products.InstrumentName rcgInstrumentName)
        {
            int n;

            if (typeCodeStr.Contains("O") || typeCodeStr.Contains("C") || typeCodeStr.Contains("P"))
            {
                ProductTypes mistyProductType = ProductTypes.Option;
                string exchStr = "Option";
                string instrStr = instrDescStr;
                string seriesName = instrDescStr;
                rcgProduct = new Product(exchStr, instrStr, mistyProductType);
                rcgInstrumentName = new InstrumentName(rcgProduct, seriesName);

            }
            else if (string.IsNullOrEmpty(typeCodeStr) || typeCodeStr.Contains("F"))
            {   // futures format = "DEC 13 TOCOM GOLD"
                ProductTypes mistyProductType = ProductTypes.Future;
                string[] elements;
                try
                {
                    elements = instrDescStr.Split(DelimSpace, StringSplitOptions.RemoveEmptyEntries);
                    int nextPtr = 0;
                    //
                    // Instrument date extraction
                    //
                    string seriesName;
                    if (Int32.TryParse(elements[0], out n))
                    {   // Seems to be "dd MMM yy" format, since first element is integer n = "dd"
                        string s = elements[1].Trim();              // extract month part
                        string monthName = string.Format("{0}{1}", s.Substring(0, 1).ToUpper(), s.Substring(1, 2).ToLower());   // MAY --> May
                        seriesName = string.Format("{0:00}{1}{2}", n, monthName, elements[2].Trim());                          // 08May13 for example
                        seriesName = "CA 3M";
                        nextPtr = 3;                                // ptr to next element. 
                    }
                    else
                    {   // Seems to be "MMM yy" format
                        string s = elements[0].Trim();              // extract month part
                        string monthName = string.Format("{0}{1}", s.Substring(0, 1).ToUpper(), s.Substring(1, 2).ToLower());   // MAY --> May
                        seriesName = string.Format("{0}{1}", monthName, elements[1].Trim());
                        nextPtr = 2;                                // ptr to next element. 
                    }
                    string exchStr;
                    string instrStr;
                    int remainingElements = elements.Length - nextPtr;
                    if (remainingElements == 1)
                    {   // No obvious delineation between product and exch?
                        // Assume a 3-character exchange code!  I believe RCG uses fixed length fields...
                        string s = elements[nextPtr].Trim();
                        exchStr = s.Substring(0, 3);                // First 3 chars
                        instrStr = s.Substring(3);                  // remaining symbol
                        nextPtr++;
                    }
                    else
                    {
                        exchStr = elements[nextPtr].Trim();         // presume exch name is ONE-word long
                        nextPtr++;
                        instrStr = elements[nextPtr].Trim();
                        nextPtr++;
                        while (nextPtr < elements.Length)
                        {
                            instrStr = string.Format("{0} {1}", instrStr, elements[nextPtr].Trim());
                            nextPtr++;
                        }
                    }
                    rcgProduct = new Product(exchStr, instrStr, mistyProductType);
                    rcgInstrumentName = new InstrumentName(rcgProduct, seriesName);
                }
                catch (Exception)
                {
                    // TODO: Write error message to log, and continue.
                    rcgProduct = new Product();
                    rcgInstrumentName = new InstrumentName();
                    return false;
                }
            }
            else
            {
                rcgProduct = new Product();
                rcgInstrumentName = new InstrumentName();
            }
            // Exit
            return true;
        }// CreateInstrumentName()
        //
        //
        //
        #endregion // Private Methods

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Breconcile.ABN
{
    using Misty.Lib.OrderHubs;
    using Misty.Lib.Products;

    using Reconciler;

    /// <summary>
    /// Update 27 May 2013:
    /// This version creates InstrumentName objects for each of the description names 
    /// found in the statement. Note that these are not identical to those used in Ambre,
    /// only the format/object is similar.
    /// Update 31 May 2013:
    /// This version generalizes for RCG and ABN usage.
    /// </summary>
    public class StatementReader : IClearingStatementReader
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Resulting portfolios.
        public Dictionary<string, Dictionary<InstrumentName, List<Fill>>> m_PortfolioPosition = new Dictionary<string, Dictionary<InstrumentName, List<Fill>>>(); // [acct][instrDescr string] --> List<Fills>
        public Dictionary<string, Dictionary<InstrumentName, List<Fill>>> m_PortfolioFills = new Dictionary<string, Dictionary<InstrumentName, List<Fill>>>(); // [acct][instrDescr string] --> List<Fills>
        private string m_FilePathForPosition = string.Empty;

        // Product Tables
        //public Dictionary<Product, Product> m_RcgToBreProduct = new Dictionary<Product,Product>();
        private string m_ProductTableName = string.Empty;

        private InstrumentNameMapTable m_ProductMapping;


        // Error messages
        public StringBuilder m_ErrorMessage = new StringBuilder();

        //
        // constants
        private char[] Delims = new char[] { ',' };
        private char[] DelimSpace = new char[] { ' ' };

        //
        // ABN
        //
        // Columns - zero based 
        private const int COL_TRANSACTION       = 1;
        private const int COL_ACCT              = 10;               // COL denotes all statements use this common column.
        private const int COL_EXCHANGENAME      = 11;
        private const int COL_PRODUCTNAME       = 12;
        private const int COL_PRODUCTNAMEALT    = 43;
        private const int COL_PRODUCTTYPESYMBOL = 35;               // E=equity, F=future (or use 15? Security type?)
        private const int COL_CONTRACTYRMO      = 16;               // yyyyMM
        private const int COL_CONTRACTDAY       = 17;               // dd

        private const int COL_BUYSELL           = 20;
        private const int COL_UNSIGNEDQTY       = 21;
        private const int COL_DECIMALPRICE      = 25;    
        private const int COL_TRADEDATE         = 18;               // yyyyMMdd
        private const int COL_ORDERTIME         = 54;               // Hmmss
        private const int COL_CALLPUT           = 13;               // "C"all / "P"ut flag

        


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        private StatementReader() { }
        private StatementReader(string positionFilePath, string fillsFillPath, string productTableFileName)
        {
            m_FilePathForPosition = positionFilePath;
            m_ProductTableName = productTableFileName;

            string filePath = string.Format("{0}{1}", Misty.Lib.Application.AppInfo.GetInstance().UserPath, m_ProductTableName);
            m_ProductMapping = new InstrumentNameMapTable(filePath);
            //string newName = m_ProductTableName.Replace(".txt", ".csv");
            //instrMap.SaveTable(string.Format("{0}{1}", Misty.Lib.Application.AppInfo.GetInstance().UserPath, newName));            
            
            
            //ReadProductTable();                                  // loads m_RcgToBreProduct, product naming maps.

            if (!string.IsNullOrEmpty(positionFilePath))
                ReadStatement(positionFilePath, true);       // Loads "PortfolioPosition"            
            if (!string.IsNullOrEmpty(fillsFillPath))
                ReadStatement(fillsFillPath, false);         // Loads "PortfolioFills" 
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
            out IClearingStatementReader reader)
        {
            reader = null;
            string reconcilliationDateStr = reconcilliationDate.ToString("yyyyMMdd");   // This pattern appears in the desired files.
            string posFileName = string.Format("{0}futpos_{1}.csv", statementPath, reconcilliationDateStr);
            //string fillFileName = string.Empty;// string.Format("{0}futtran_{1}.csv", statementPath, reconcilliationDateStr);
            string fillFileName = string.Format("{0}futtran_{1}.csv", statementPath, reconcilliationDateStr);
            reader = new StatementReader(posFileName, fillFileName, "ProductTableABN.txt");            
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
            string filePath = string.Format("{0}{1}", Misty.Lib.Application.AppInfo.GetInstance().UserPath, m_ProductTableName);
            if (!System.IO.File.Exists(filePath))
                return;
            using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
            {
                string aLine;
                bool continueReading = true;
                while (continueReading)
                {
                    aLine = reader.ReadLine();
                    if (aLine == null || aLine.StartsWith("// END", StringComparison.CurrentCultureIgnoreCase)) // this signals end of file at the moment.
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
                        if (Misty.Lib.Products.Product.TryDeserialize(string.Format("{0} (Future)", elements[0]), out breProduct) &&
                            Misty.Lib.Products.Product.TryDeserialize(string.Format("{0} (Future)", elements[1]), out rcgProduct))
                        {
                            m_RcgToBreProduct.Add(rcgProduct, breProduct);
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
            if (string.IsNullOrEmpty(newFileName))
                filePath = string.Format("{0}{1}", Misty.Lib.Application.AppInfo.GetInstance().UserPath, m_ProductTableName);
            else if (newFileName.Contains("."))
                filePath = string.Format("{0}{1}", Misty.Lib.Application.AppInfo.GetInstance().UserPath, newFileName);
            else
                filePath = string.Format("{0}{1}.txt", Misty.Lib.Application.AppInfo.GetInstance().UserPath, newFileName);
            // Lets alphabetize this table
            SortedDictionary<string, string> tableStr = new SortedDictionary<string, string>();
            foreach (Misty.Lib.Products.Product rcgProduct in table.Keys)
            {
                string breStr = Misty.Lib.Products.Product.Serialize(table[rcgProduct]);
                string rcgStr = Misty.Lib.Products.Product.Serialize(rcgProduct);                // rcg name
                if (!tableStr.ContainsKey(breStr))
                    tableStr.Add(breStr, rcgStr);
            }
            using (System.IO.StreamWriter stream = new System.IO.StreamWriter(filePath, false))
            {
                foreach (string sBreName in tableStr.Keys)
                    stream.WriteLine("{0,32},{1,32}", sBreName, tableStr[sBreName]);  // RCG ---> BRE               
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
            Dictionary<string, InstrumentName> m_InstrDescrToInstrName = new Dictionary<string, InstrumentName>();
            using (System.IO.StreamReader reader = new System.IO.StreamReader(filePath))
            {
                string aLine;
                aLine = reader.ReadLine();                          // read header line.
                while ((aLine = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(aLine))
                        continue;
                    string[] elements = aLine.Split(Delims, StringSplitOptions.None);
                    try
                    {
                        if (elements[0].Trim().StartsWith("#"))
                            continue;
                        if (isPositionStatement)
                        {

                        }
                        else
                        {   // Fill statement
                            if (!elements[COL_TRANSACTION].Equals("T"))
                                continue;
                        }
                        // Product identification.
                        string acctName = elements[COL_ACCT].Trim();
                        string exchange = elements[COL_EXCHANGENAME].Trim();
                        string prodSymbol = elements[COL_PRODUCTNAME].Trim();
                        int n;
                        //if (acctName.Contains("W1077") && exchange.Contains("12") )
                        //    acctName = acctName;
                        if (int.TryParse(prodSymbol, out n) && !string.IsNullOrEmpty(elements[COL_PRODUCTNAMEALT].Trim()))
                            prodSymbol = elements[COL_PRODUCTNAMEALT].Trim();
                        string prodType = elements[COL_PRODUCTTYPESYMBOL].Trim();
                        string contractExpYrMo = elements[COL_CONTRACTYRMO].Trim();
                        string contractExpDay = elements[COL_CONTRACTDAY].Trim();

                        string instrDesc = string.Format("{0}({1}){2}.{3}{4}", exchange, prodType, prodSymbol, contractExpYrMo, contractExpDay);
                        InstrumentName instrumentName;
                        if (!m_InstrDescrToInstrName.TryGetValue(instrDesc, out instrumentName))
                        {   // First encounter with this instrument, extract it, and store it so we have only to look up instrument once.
                            Product product;
                            if (TryExtractInstrumentName(exchange, prodType, prodSymbol, contractExpYrMo, contractExpDay, out product, out instrumentName))
                                m_InstrDescrToInstrName.Add(instrDesc, instrumentName);
                        }

                        string priceStr;
                        if (isPositionStatement)
                            priceStr = elements[COL_DECIMALPRICE].Trim();
                        else
                            priceStr = elements[COL_DECIMALPRICE].Trim();
                        string sBuySell = elements[COL_BUYSELL];
                        int qtySign = Convert.ToInt32(3 - 2 * Convert.ToDouble(sBuySell)); // Buy,Sell = 1,2
                        int signedQty = Convert.ToInt32(qtySign * Convert.ToDouble(elements[COL_UNSIGNEDQTY]));
                        double price = 0;
                        price = Convert.ToDouble(priceStr);

                        Fill newFill = null;
                        newFill = new Fill();
                        DateTime tradeDate;
                        if (DateTime.TryParseExact(elements[COL_TRADEDATE], "yyyyMMdd", new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out tradeDate))
                            newFill.LocalTime = tradeDate;
                        DateTime tradeTime;
                        if (DateTime.TryParseExact(elements[COL_ORDERTIME], "Hmmss", new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out tradeTime))
                            newFill.LocalTime.Add(tradeTime.TimeOfDay);
                        newFill.Qty = signedQty;
                        newFill.Price = price;

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
                        if (newFill != null)       // null can happen on exceptions
                            fillList.Add(newFill);


                    }
                    catch (Exception e)
                    {
                        m_ErrorMessage.AppendFormat("Exception {0} extracting info from elements: {1}", e.Message, aLine);   // Write to a log file.
                        m_ErrorMessage.AppendLine();
                    }


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
        private bool TryExtractInstrumentName(string exchangeStr, string prodTypeStr, string prodSymbolStr, string contractExpYrMo, string contractExpDay,out Product product, out InstrumentName instrumentName)
        {
            product = new Product();
            instrumentName = new InstrumentName();
            if (prodTypeStr.Contains("E"))
            {   // Equity type instrument
                //ProductTypes productType = ProductTypes.Equity;
            }
            else if (prodTypeStr.Contains("F"))
            {   // Futures type instrument   
                product = new Product(exchangeStr, prodSymbolStr, ProductTypes.Future);

                // Instrument date extraction
                string seriesName = string.Empty ;
                int expiryDay = 0;
                if (!string.IsNullOrEmpty(contractExpDay) && int.TryParse(contractExpDay, out expiryDay))
                {

                }
                int expiryYear = 0;
                int expiryMonth = 0;
                if (!string.IsNullOrEmpty(contractExpYrMo) && int.TryParse(contractExpYrMo.Substring(0, 4), out expiryYear))
                {
                    if (int.TryParse(contractExpYrMo.Substring(4, 2), out expiryMonth))
                    {
                        if (expiryDay < 1)
                        {   // This is a monthly contract - normal futures contract.
                            expiryDay = 1;
                            DateTime dt = new DateTime(expiryYear, expiryMonth, expiryDay);
                            seriesName = string.Format("{0:MMM}{0:yy}", dt);
                        }
                        else if (product.Exchange.Equals("12") && product.ProductName.Equals("CP"))
                        {   // This is a daily rolling forward contract.
                            seriesName = "CA 3M";   // for two products to be the "same", the series names need to agree.
                            // We want to add all of these daily rolling copper contracts into one instrument.
                        }
                        else
                        {
                            DateTime dt = new DateTime(expiryYear, expiryMonth, expiryDay);
                            seriesName = string.Format("{0:dd}{0:MMM}{0:yy}", dt);
                        }
                    }
                    instrumentName = new InstrumentName(product,seriesName);
                }
                if (string.IsNullOrEmpty(seriesName))
                    return false;
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

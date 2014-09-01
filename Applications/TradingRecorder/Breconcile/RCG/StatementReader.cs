using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.Breconcile.RCG
{
    using Misty.Lib.OrderHubs;


    public class StatementReader
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        public readonly string FilePathForPosition;
        public readonly string FilePathForFills;

        // Resulting portfolio.
        public Dictionary<string,Dictionary<string,List<Fill>>> m_PortfolioPosition = new Dictionary<string,Dictionary<string,List<Fill>>>(); // [acct][instrDescr string] --> List<Fills>
        public Dictionary<string, Dictionary<string, List<Fill>>> m_PortfolioFills = new Dictionary<string, Dictionary<string, List<Fill>>>(); // [acct][instrDescr string] --> List<Fills>
        public Dictionary<string, Misty.Lib.Products.InstrumentName> m_InstrDescrToInstrName = new Dictionary<string, Misty.Lib.Products.InstrumentName>();


        // Tables
        public Dictionary<Misty.Lib.Products.Product, Misty.Lib.Products.Product> m_RcgToBreProduct = new Dictionary<Misty.Lib.Products.Product, Misty.Lib.Products.Product>();

        // Error messages
        public StringBuilder m_ErrorMessage = new StringBuilder();

        //
        // constants
        private char[] Delims = new char[] { ',' };
        private char[] DelimSpace = new char[] { ' ' };

        private string ProductTableName = "ProductTable.txt";

        // POS & ST4 file:
        // Columns - zero based 
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

        private const int POS_EXPIRATION = 24;
        private const int POS_EXCHPRODNAME  = 27;       // Exchange "short" name
        private const int ST4_EXCHPRODNAME  = 29;       // Exchange "short" name


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public StatementReader(string positionFilePath="", string fillsFillPath="")
        {
            FilePathForPosition = positionFilePath;
            FilePathForFills = fillsFillPath;

            ReadProductTable();             // loads m_RcgToBreProduct, product naming maps.

            if ( ! string.IsNullOrEmpty(positionFilePath) )
                ReadStatement(FilePathForPosition,true);       // Loads "PortfolioPosition"
            
            if ( ! string.IsNullOrEmpty(fillsFillPath) )
                ReadStatement(FilePathForFills,false);       // Loads "PortfolioFills" 

        }
        //       
        #endregion//Constructors




        #region Private Methods
        // *****************************************************************
        // ****                  Private Methods                        ****
        // *****************************************************************
        //
        //
        // Product table format:   breProductName, RcgProductName
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
                        if (
                            Misty.Lib.Products.Product.TryDeserialize(string.Format("{0} (Future)", elements[0]), out breProduct) 
                            && 
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
        //
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
                        acctName = elements[COL_ACCT];
                        instrDesc = elements[COL_INSTR];
                        instrType = elements[COL_INSTRTYPE];
                        string priceStr;
                        if (isPositionStatement)
                        {
                            prodSymbol = elements[POS_EXCHPRODNAME];
                            priceStr = elements[POS_DECIMALPRICE].Trim();
                        }
                        else
                        {
                            prodSymbol = elements[ST4_EXCHPRODNAME];
                            priceStr = elements[ST4_DECIMALPRICE].Trim();
                        }






                        // Extract effective fill
                        string sBuySell = elements[COL_BUYSELL];
                        int qtySign = Convert.ToInt32(3 - 2 * Convert.ToDouble(sBuySell));
                        int signedQty = Convert.ToInt32(qtySign * Convert.ToDouble(elements[COL_UNSIGNEDQTY]));
                        double price = 0;
                        price = Convert.ToDouble(priceStr);
                        newFill = new Fill();                        
                        DateTime tradeDate;
                        if (DateTime.TryParseExact(elements[COL_TRADEDATE], "yyyyMMdd", new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out tradeDate))
                            newFill.LocalTime = tradeDate;
                        newFill.Qty = signedQty;
                        newFill.Price = price;
                    }
                    catch (Exception e)
                    {
                        m_ErrorMessage.AppendFormat("Exception {0} extracting info from elements: {1}", e.Message, aLine);   // Write to a log file.
                        m_ErrorMessage.AppendLine();
                    }

                    //
                    // Add position to lists
                    //
                    Dictionary<string, List<Fill>> instrumentPortfolio;                 // Get a portfolio for this acctName
                    if (isPositionStatement)
                    {
                        if (!m_PortfolioPosition.TryGetValue(acctName, out instrumentPortfolio))
                        {
                            instrumentPortfolio = new Dictionary<string, List<Fill>>();
                            m_PortfolioPosition.Add(acctName, instrumentPortfolio);
                        }
                    }
                    else
                    {
                        if (!m_PortfolioFills.TryGetValue(acctName, out instrumentPortfolio))
                        {
                            instrumentPortfolio = new Dictionary<string, List<Fill>>();
                            m_PortfolioFills.Add(acctName, instrumentPortfolio);
                        }
                    }
                    List<Fill> fillList;
                    if (!instrumentPortfolio.TryGetValue(instrDesc, out fillList))
                    {
                        fillList = new List<Fill>();
                        instrumentPortfolio.Add(instrDesc, fillList);
                    }
                    if ( newFill != null)       // null can happen on exceptions
                        fillList.Add(newFill);

                    //
                    // Create InstrumentName for this item
                    //
                    if (!m_InstrDescrToInstrName.ContainsKey(instrDesc))
                    {   // This is the first encounter with this instrument Description.
                        Misty.Lib.Products.Product product;
                        Misty.Lib.Products.InstrumentName instrumentName;
                        string callPutFlag = elements[COL_CALLPUT];
                        if (CreateInstrumentName(instrType,callPutFlag,instrDesc, prodSymbol, out product, out instrumentName))
                        {
                            m_InstrDescrToInstrName.Add(instrDesc, instrumentName);
                        }
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
        private bool CreateInstrumentName(string typeCodeStr, string callPutFlag, string instrDescStr, string instrSymbolStr, out Misty.Lib.Products.Product rcgProduct, out Misty.Lib.Products.InstrumentName rcgInstrumentName)
        {
            int n;
            bool isOption = false;
            if (callPutFlag.Equals("C"))
                isOption = true;
            else if (callPutFlag.Equals("P"))
                isOption = true;

            if (isOption)
            {
                Misty.Lib.Products.ProductTypes mistyProductType = Misty.Lib.Products.ProductTypes.Option;
                string exchStr = string.Empty;
                string instrStr = instrDescStr;
                string seriesName = instrDescStr;
                rcgProduct = new Misty.Lib.Products.Product(exchStr, instrStr, mistyProductType);
                rcgInstrumentName = new Misty.Lib.Products.InstrumentName(rcgProduct, seriesName);


            }
            else if (string.IsNullOrEmpty(typeCodeStr) || typeCodeStr.Equals("F"))
            {   // futures format = "DEC 13 TOCOM GOLD"
                Misty.Lib.Products.ProductTypes mistyProductType = Misty.Lib.Products.ProductTypes.Future;
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
                    rcgProduct = new Misty.Lib.Products.Product(exchStr, instrStr, mistyProductType);
                    rcgInstrumentName = new Misty.Lib.Products.InstrumentName(rcgProduct, seriesName);
                }
                catch (Exception)
                {
                    // TODO: Write error message to log, and continue.
                    rcgProduct = new Misty.Lib.Products.Product();
                    rcgInstrumentName = new Misty.Lib.Products.InstrumentName();
                    return false;
                }
            }
            else
            {
                rcgProduct = new Misty.Lib.Products.Product();
                rcgInstrumentName = new Misty.Lib.Products.InstrumentName();
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

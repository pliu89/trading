using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace UV.Lib.Database
{
    using MySql.Data.MySqlClient;
    using UV.Lib.Products;
    using UV.Lib.Utilities;
    /// <summary>
    /// Class for reading instruments from a UV style database.
    /// </summary>
    public static class DBInstrument
    {
        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
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
        /// <summary>
        /// Try to find an instrument in the database, check it's data, and if 
        /// incorrect or missing fill in with new data.
        /// 
        /// </summary>
        /// <param name="dataBase"></param>
        /// <param name="instrDetails"></param>
        /// <param name="queryToExecute">will return non empty string to be pushed to db writer if data in the table needs to be written</param>
        /// <returns>true if instrument was found and checked</returns>
        public static bool TryCheckMySQLInstrumentDetails(DatabaseInfo dataBase, InstrumentDetails instrDetails, out string queryToExecute)
        {
            bool isSuccess = true;
            queryToExecute = string.Empty;                                                          // query to execute if instrument details are different!
            if (dataBase.Instruments == null | dataBase.Products == null)                           // make sure we have an instrument table and product table.
                return false;

            int mySQLInstrId;
            if (!TryGetMySQLInstrumentId(dataBase, instrDetails.InstrumentName, out mySQLInstrId))   // make sure we can find this instrument
                return false;

            //
            // read instrument details
            //
            MySqlConnection conn = null;
            if (dataBase.IsTryToConnect(ref conn))
            {
                double smallTickTT = double.NaN;
                double unitTT = double.NaN;
                double calendarTickTT = double.NaN;
                double tickTT = double.NaN;                                                         // turn everything to NAN to start
                DateTime expirationDate = new DateTime();

                MySqlDataReader reader = null;
                StringBuilder query = new StringBuilder();                                          // create our query
                query.AppendFormat("select {0}, {1}, {2}, {3}, {4} from {5} where {6} = \'{7}\';",
                    dataBase.Instruments.unitTT,                    // 0
                    dataBase.Instruments.tickTT,                    // 1
                    dataBase.Instruments.smallTickTT,               // 2
                    dataBase.Instruments.calendarTickTT,            // 3
                    dataBase.Instruments.ExpirationDate,            // 4
                    dataBase.Instruments.TableNameFull,             // 5
                    dataBase.Instruments.InstrumentID,              // 6
                    mySQLInstrId);                                  // 7
                try
                {
                    MySqlCommand cmd = new MySqlCommand(query.ToString(), conn);                    // execute our query
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {

                        if (!TryReadInstrumentDetails(reader, out unitTT, out tickTT,
                                                              out smallTickTT, out calendarTickTT,
                                                              out expirationDate))
                        { // if we are not able to read instrument details we have to fail
                            dataBase.Errors.Enqueue("TryCheckMySQLInstrumentDetails(): Failed to read instrument details.");
                            isSuccess = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    dataBase.Errors.Enqueue(string.Format("Instrument.Create(): MySql exception {0}", e.Message));
                    isSuccess = false;
                }
                finally
                {
                    if (reader != null && !reader.IsClosed) { reader.Close(); }
                    conn.Close();
                }
                //
                // compare instrument details
                //
                if (!QTMath.IsNearEqual(smallTickTT, instrDetails.TickSize * instrDetails.Multiplier, .0001) |
                    !QTMath.IsNearEqual(tickTT, instrDetails.TickSize * instrDetails.Multiplier, .0001) |
                    !QTMath.IsNearEqual(calendarTickTT, instrDetails.TickSize * instrDetails.Multiplier, .0001) |
                    !QTMath.IsNearEqual(unitTT, instrDetails.Multiplier, .0001) |
                    expirationDate != instrDetails.ExpirationDate)
                { // we have different data, so we should create a query
                    query.Clear();
                    // update instrumentInfo set smallTickTT = x, tickTT = y, etc, where instrumentID = mysqlID;
                    query.AppendFormat("update {0} set {1} = {2}, {3} = {4}, {5} = {6}, {7} = {8}, {9} = \'{10}\' where {11} = \'{12}\' limit 1;",
                        dataBase.Instruments.TableNameFull,                             // 0
                        dataBase.Instruments.smallTickTT,                               // 1
                        instrDetails.TickSize * instrDetails.Multiplier,                // 2
                        dataBase.Instruments.tickTT,                                    // 3
                        instrDetails.TickSize * instrDetails.Multiplier,                // 4
                        dataBase.Instruments.unitTT,                                    // 5
                        instrDetails.Multiplier,                                        // 6
                        dataBase.Instruments.calendarTickTT,                            // 7 
                        instrDetails.TickSize * instrDetails.Multiplier,                // 8
                        dataBase.Instruments.ExpirationDate,                            // 9
                        instrDetails.ExpirationDate.ToString("yyyy-MM-dd HH:mm:ss"),    // 10
                        dataBase.Instruments.InstrumentID,                              // 11
                        mySQLInstrId);                                                  // 12
                    queryToExecute = query.ToString();
                }
            }//if connected
            return isSuccess;
        }
        //
        //
        /// <summary>
        /// Try and find the uniquie Instrument ID from a mySQL database for a given InstrumentName.
        /// Warning : This function is extremely dangerous and written very poorly.  
        /// </summary>
        /// <param name="dataBase"></param>
        /// <param name="instrument"></param>
        /// <param name="mySQLInstrumentID"></param>
        /// <returns></returns>
        public static bool TryGetMySQLInstrumentId(DatabaseInfo dataBase, InstrumentName instrument, out int mySQLInstrumentID)
        {
            // Validate	
            
            mySQLInstrumentID = -1;
            if (dataBase.Instruments == null)
                return false;

            Dictionary<int, string> possibleMySQLInstrumendIds = new Dictionary<int, string>();
            TableInfo.InstrumentsTableInfo instrumentTable = dataBase.Instruments;
            TableInfo.ExchangesTableInfo exchangeTable = dataBase.Exchanges;

            MySqlConnection conn = null;
            bool isSuccess = true;
            string expiryCode;      // ie Z3 or H4
            if (dataBase.IsTryToConnect(ref conn) && UV.Lib.Utilities.QTMath.TryConvertMonthYearToCodeY(instrument.SeriesName, out expiryCode))
            {
                MySqlDataReader reader = null;
                StringBuilder query = new StringBuilder();
                query.AppendFormat("SELECT {0},{1} FROM {2} ", instrumentTable.InstrumentID, instrumentTable.SpreadComposition, dataBase.Instruments.TableNameFull);
                // add restrictions here. (example below)
                /*                                               0                      1                2                3             4         5         6          7          8
                 * select instrID from instrumentInfo where exchangeId in (select exchangeId from exchangeInfo where exchangeNameTT ='CME') and prodNameTT = "CL"  and prodExpiry='X2';
                */
                query.AppendFormat("WHERE {0} in (select {1} from {2} where {3} =\'{4}\') and {5} =\'{6}\' and {7} =\'{8}\' and {9} = \'{10}\'",
                                    instrumentTable.ExchangeID,     // 0
                                    instrumentTable.ExchangeID,     // 1
                                    exchangeTable.TableNameFull,    // 2
                                    exchangeTable.ExchangeName,     // 3
                                    instrument.Product.Exchange,    // 4
                                    instrumentTable.Product,        // 5
                                    instrument.Product.ProductName, // 6
                                    instrumentTable.ExpirySymbol,   // 7
                                    expiryCode,                     // 8
                                    instrumentTable.ProductType,    // 9
                                    instrument.Product.Type.ToString().ToLower()); // 10


                #region BAD CODE FIX ME!
                if (instrument.Product.Type == ProductTypes.Spread)
                { // handling special cases here in a BAD WAY!
                    if (instrument.Product.ProductName == "KEZW" || instrument.Product.ProductName == "MWEZW" || instrument.Product.ProductName == "MWEKE")
                    { // these are ICS spreads and we only want the "Matched" expirations
                        string composition = instrument.SeriesName;
                        string[] compSplit = composition.Split(new char[] { ':' });
                        List<string> expiryCodes = new List<string>();

                        string matchExpiries = @"[a-zA-Z]{3}[0-9]{2}";
                        foreach (string partialString in compSplit)
                            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(partialString, matchExpiries))
                                expiryCodes.Add(match.Value);

                        if (expiryCodes.Count == 2 && expiryCodes[0] == expiryCodes[1])
                        {
                            
                        }
                        else
                        {
                            isSuccess = false;
                            return isSuccess;
                        }
                    }
                }
                #endregion

                query.AppendFormat("ORDER BY {0};", instrumentTable.InstrumentID);
                try
                {
                    MySqlCommand cmd = new MySqlCommand(query.ToString(), conn);
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {

                        int Id = -1;
                        string spreadComposition;
                        if (!TryReadMySQLInstrumentID(reader, out Id, out spreadComposition))
                        {
                            dataBase.Errors.Enqueue("TryGetMySQLInstrumentId(): Failed to get mySQL instrument.");
                            isSuccess = false;
                            break;
                        }
                        possibleMySQLInstrumendIds.Add(Id, spreadComposition);
                    }
                }
                catch (Exception e)
                {
                    dataBase.Errors.Enqueue(string.Format("Instrument.Create(): MySql exception {0}", e.Message));
                    isSuccess = false;
                }
                finally
                {
                    if (reader != null && !reader.IsClosed) { reader.Close(); }
                    conn.Close();
                }

                if (possibleMySQLInstrumendIds.Count > 1)
                {// we need to find out which instrument id is correct, very simliar instruments!
                    // Warning: This is horrible code.  I am not sure yet how to create something more generic dealing with 
                    // a place were were keep similiar spreads as different products and TT believes them to be  instruments.  
                    // This section is VERY specific and not general in any way. It will probably break.  Need to come up
                    // with a much better well though out solution

                    #region  //Bad Code!
                    Match match = Regex.Match(instrument.SeriesName, ": [0-9] x [0-9]");            // make sure we know how to deal with these instruments
                    if (match.Success)
                    {
                        // find our ratios from our TT product to compare to our database product
                        string[] stringRatios = Regex.Split(match.Value.Substring(2), "x");        // split these ratios into their components so we can compare     
                        int[] intRatios = new int[stringRatios.Length];
                        for(int i =0; i<stringRatios.Length; i++)
                        {
                            int ratio;
                            if (int.TryParse(stringRatios[i], out ratio))                           // create them as ints and save
                            {
                                intRatios[i] = ratio;
                            }
                        }
                        
                        // compare with each database product to find the correct match
                        
                        foreach (int id in possibleMySQLInstrumendIds.Keys)     
                        {
                            bool isMatch = false;
                            string spreadCompString = possibleMySQLInstrumendIds[id];
                            MatchCollection allMatches = Regex.Matches(spreadCompString, "[0-9]x[A-Z]");
                            if(match.Success)
                            {
                                int ratio;
                                int count = 0;
                                
                                foreach (Match newMatch in allMatches)
                                {
                                    if (int.TryParse(newMatch.Value.Substring(0, 1), out ratio))    // parse out the ints to compare
                                    {
                                        if (ratio == intRatios[count])
                                            isMatch = true;
                                        else
                                            isMatch = false;
                                    }
                                    else
                                        isMatch = false;
                                    count++;
                                }
                            }
                            if (isMatch)
                            { // we found a ratio match so assign the correct id and move on
                                mySQLInstrumentID = id;
                                break;
                            }
                        }
                    }
                }
                #endregion // Bad Code!

                else if (possibleMySQLInstrumendIds.Count == 1)
                    foreach (int id in possibleMySQLInstrumendIds.Keys)
                        mySQLInstrumentID = id;
                else
                    isSuccess = false;

                // exit
            }//if connected
            return isSuccess && mySQLInstrumentID > -1;
        }//TryCreate().
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Helper function to read a instrument ID from a database, returning both its 
        /// id and its composition if a spread.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="mySQLInstrumentID"></param>
        /// <param name="spreadComposition"></param>
        /// <returns></returns>
        private static bool TryReadMySQLInstrumentID(MySqlDataReader reader, out int mySQLInstrumentID, out string spreadComposition)
        {
            bool isSuccesful = true;
            mySQLInstrumentID = -1;
            spreadComposition = string.Empty;

            string[] fieldNames = new string[reader.FieldCount];
            for (int i = 0; i < fieldNames.Length; i++)
                fieldNames[i] = reader.GetName(i);
            TableInfo.InstrumentsTableInfo table = new TableInfo.InstrumentsTableInfo();
            try
            {
                mySQLInstrumentID = reader.GetInt32(table.InstrumentID);
                if (reader[table.SpreadComposition] != DBNull.Value)
                    spreadComposition = reader.GetString(table.SpreadComposition);
            }
            catch (Exception)
            {
                isSuccesful = false;
            }
            return isSuccesful;
        }
        //
        //
        //
        /// <summary>
        /// Helper function for reading instrument details from a instrumentTable.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="unitTT"></param>
        /// <param name="tickTT"></param>
        /// <param name="smallTickTT"></param>
        /// <param name="calendarTickTT"></param>
        /// <param name="expirationDate"></param>
        /// <returns></returns>
        private static bool TryReadInstrumentDetails(MySqlDataReader reader, out double unitTT, out double tickTT,
            out double smallTickTT, out double calendarTickTT, out DateTime expirationDate)
        {
            bool isSuccseful = true;
            smallTickTT = unitTT = calendarTickTT = tickTT = double.NaN;                                               // turn everything to NAN to start
            expirationDate = new DateTime();
            string[] fieldNames = new string[reader.FieldCount];
            for (int i = 0; i < fieldNames.Length; i++)
                fieldNames[i] = reader.GetName(i);
            TableInfo.InstrumentsTableInfo table = new TableInfo.InstrumentsTableInfo();
            try
            { // if the value is not null assign it to our variable casting it as a double.
                if (reader[table.unitTT] != DBNull.Value)
                    unitTT = reader.GetDouble(table.unitTT);
                if (reader[table.tickTT] != DBNull.Value)
                    tickTT = reader.GetDouble(table.tickTT);
                if (reader[table.smallTickTT] != DBNull.Value)
                    smallTickTT = reader.GetDouble(table.smallTickTT);
                if (reader[table.calendarTickTT] != DBNull.Value)
                    calendarTickTT = reader.GetDouble(table.calendarTickTT);
                if (reader[table.ExpirationDate] != DBNull.Value)
                    expirationDate = reader.GetDateTime(table.ExpirationDate);
            }
            catch (Exception)
            {
                isSuccseful = false;
            }
            return isSuccseful;
        }

        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers
    }
}

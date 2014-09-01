using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Database
{
    using MySql.Data.MySqlClient;
    /// <summary>
    /// Currently unused.  This class was originally going to be a way of getting
    /// mySQL exhange ID's.  
    /// </summary>
    public class ExchangeNameToID
    {

        #region no Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        private Dictionary<string, int> m_ExhchangeNameToID = new Dictionary<string, int>();
        private Dictionary<int, string> m_ExchangeIDToName = new Dictionary<int, string>();     // for backward lookup
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //
        public ExchangeNameToID(DatabaseInfo databaseInfo)
        {
            if (databaseInfo.Exchanges == null)
            {
                TryCreateLookUpTable(databaseInfo);
            }
        }
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
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
        private bool TryCreateLookUpTable(DatabaseInfo databaseInfo)
        {
            MySqlConnection conn = null;
            bool isSuccess = databaseInfo.IsTryToConnect(ref conn);
            if (isSuccess)
            {
                MySqlDataReader reader = null;
                StringBuilder query = new StringBuilder();
                query.AppendFormat("SELECT * FROM {0};", databaseInfo.Exchanges.TableNameFull);
                try
                {
                    MySqlCommand cmd = new MySqlCommand(query.ToString(), conn);
                    reader = cmd.ExecuteReader();
                    string ExchangeName;
                    int ExchangeID;
                    while (reader.Read())
                    {
                        if (TryCreateExchange(reader, out ExchangeName, out ExchangeID))
                        {
                            m_ExhchangeNameToID.Add(ExchangeName, ExchangeID);
                            m_ExchangeIDToName.Add(ExchangeID, ExchangeName);
                        }
                        else
                        {
                            databaseInfo.Errors.Enqueue("TryCreateLookUpTable(): Failed to create exchange lookups");
                            isSuccess = false;
                        }
                    }
                }
                catch (Exception exc)
                {
                    databaseInfo.Errors.Enqueue(string.Format("Product.Create(): MySql exception {0}", exc.Message));
                    isSuccess = false;
                }
                finally
                {
                    if (reader != null && !reader.IsClosed) { reader.Close(); }
                    conn.Close();
                }
            }
            return isSuccess;
        }
        //
        //
        //
        private static bool TryCreateExchange(MySqlDataReader reader, out string ExchangeName, out int ExchangeID)
        {
            bool isSuccesful = true;
            ExchangeName = null;
            ExchangeID = -1;

            string[] fieldNames = new string[reader.FieldCount];
            for (int i = 0; i < fieldNames.Length; i++) 
                fieldNames[i] = reader.GetName(i);
            TableInfo.ExchangesTableInfo table = new TableInfo.ExchangesTableInfo();
            try
            {
                ExchangeName = reader.GetString(table.ExchangeName);
                ExchangeID = reader.GetInt32(ExchangeID);
            }
            catch (Exception)
            {
                isSuccesful = false;
            }
            return isSuccesful;
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

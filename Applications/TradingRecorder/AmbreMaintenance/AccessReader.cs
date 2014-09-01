using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace AuditTrailReading
{
    public class AccessReader
    {
        private static string m_EmptyString = string.Empty;
        private const string m_OleDbProviderString = "Microsoft.ACE.OLEDB.12.0"; // Microsoft.Jet.OLEDB.4.0

        /// <summary>
        /// This function reads the raw data from the data base file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="tableName"></param>
        /// <param name="objData"></param>
        /// <returns></returns>
        public static bool TryReadAccessFile(string filePath, string tableName, out List<List<object>> objData)
        {
            objData = null;

            // Create and open connection.
            OleDbConnection oleDbConnection = new OleDbConnection(string.Format(@"Provider={0};Data Source={1};", m_OleDbProviderString, filePath));
            try
            {
                oleDbConnection.Open();

                // Create query language.
                string queryCommand = string.Format("select * from {0};", tableName);
                OleDbCommand oleDbCommand = new OleDbCommand(queryCommand, oleDbConnection);
                OleDbDataReader oleDbReader = oleDbCommand.ExecuteReader();

                // Read line by line to get the information.
                objData = new List<List<object>>();
                while (oleDbReader.Read())
                {
                    // All relevant columns needed.
                    object localTimeStamp = new object();
                    object exchangeName = new object();
                    object orderStatus = new object();
                    object orderAction = new object();
                    object orderSide = new object();
                    object orderQty = new object();
                    object product = new object();
                    object contract = new object();
                    object orderPrice = new object();
                    object accountName = new object();
                    object userName = new object();
                    object exchangeTime = new object();
                    object exchangeDate = new object();
                    object tradeSource = new object();
                    object ttOrderKey = new object();
                    object ttSeriesKey = new object();
                    List<object> objectList = new List<object>();

                    // Get the value if the data base on that cell is not null.
                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.LocalTimeStamp))
                        localTimeStamp = oleDbReader.GetValue(AuditTrailTableFields.LocalTimeStamp);
                    else
                        continue;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.ExchangeName))
                        exchangeName = oleDbReader.GetValue(AuditTrailTableFields.ExchangeName);
                    else
                        exchangeName = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.OrderStatus))
                        orderStatus = oleDbReader.GetValue(AuditTrailTableFields.OrderStatus);
                    else
                        orderStatus = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.OrderAction))
                        orderAction = oleDbReader.GetValue(AuditTrailTableFields.OrderAction);
                    else
                        orderAction = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.OrderSide))
                        orderSide = oleDbReader.GetValue(AuditTrailTableFields.OrderSide);
                    else
                        orderSide = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.OrderQty))
                        orderQty = oleDbReader.GetValue(AuditTrailTableFields.OrderQty);
                    else
                        orderQty = 0;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.Product))
                        product = oleDbReader.GetValue(AuditTrailTableFields.Product);
                    else
                        product = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.Contract))
                        contract = oleDbReader.GetValue(AuditTrailTableFields.Contract);
                    else
                        contract = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.OrderPrice))
                        orderPrice = oleDbReader.GetValue(AuditTrailTableFields.OrderPrice);
                    else
                        orderPrice = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.AccountName))
                        accountName = oleDbReader.GetValue(AuditTrailTableFields.AccountName);
                    else
                        accountName = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.UserName))
                        userName = oleDbReader.GetValue(AuditTrailTableFields.UserName);
                    else
                        userName = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.ExchangeTime))
                        exchangeTime = oleDbReader.GetValue(AuditTrailTableFields.ExchangeTime);
                    else
                        exchangeTime = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.ExchangeDate))
                        exchangeDate = oleDbReader.GetValue(AuditTrailTableFields.ExchangeDate);
                    else
                        exchangeDate = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.TradeSource))
                        tradeSource = oleDbReader.GetValue(AuditTrailTableFields.TradeSource);
                    else
                        tradeSource = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.TTOrderKey))
                        ttOrderKey = oleDbReader.GetValue(AuditTrailTableFields.TTOrderKey);
                    else
                        ttOrderKey = m_EmptyString;

                    if (!oleDbReader.IsDBNull(AuditTrailTableFields.TTSeriesKey))
                        ttSeriesKey = oleDbReader.GetValue(AuditTrailTableFields.TTSeriesKey);
                    else
                        ttSeriesKey = m_EmptyString;

                    // Create object list and add them to final output.
                    objectList.Add(localTimeStamp);
                    objectList.Add(exchangeName);
                    objectList.Add(orderStatus);
                    objectList.Add(orderAction);
                    objectList.Add(orderSide);
                    objectList.Add(orderQty);
                    objectList.Add(product);
                    objectList.Add(contract);
                    objectList.Add(orderPrice);
                    objectList.Add(accountName);
                    objectList.Add(userName);
                    objectList.Add(exchangeTime);
                    objectList.Add(exchangeDate);
                    objectList.Add(tradeSource);
                    objectList.Add(ttOrderKey);
                    objectList.Add(ttSeriesKey);
                    objData.Add(objectList);
                }

                // Sort the object list by its utc time stamp.
                objData.Sort(CompareRowFunction);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
            finally
            {
                oleDbConnection.Close();
            }
        }

        /// <summary>
        /// This function helps sort the row data in the object list.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        private static int CompareRowFunction(List<object> A, List<object> B)
        {
            DateTime dt1;
            DateTime dt2;
            string sdt1 = (string)A[0];
            string sdt2 = (string)B[0];

            if (string.IsNullOrEmpty(sdt1) || string.IsNullOrEmpty(sdt2))
            {
                Console.WriteLine("One date time is empty or null.{0},{1}.", sdt1, sdt2);
                return 0;
            }


            if (!DateTime.TryParseExact(sdt1, "HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out dt1) || !DateTime.TryParseExact(sdt2, "HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out dt2))
            {
                Console.WriteLine("One date time is parsed wrongly.{0},{1}.", sdt1, sdt2);
                return 0;
            }

            if (dt1 > dt2)
                return 1;

            if (dt1 < dt2)
                return -1;

            return 0;
        }
    }
}

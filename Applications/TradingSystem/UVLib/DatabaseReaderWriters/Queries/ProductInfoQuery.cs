using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace UV.Lib.DatabaseReaderWriters.Queries
{
    /// <summary>
    /// This is a query object to load the entire ProductInfo table 
    /// into a list of ProductInfoItems (one for each row).
    /// </summary>
    public class ProductInfoQuery : QueryBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //   
        public List<ProductInfoItem> m_Results = null;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        public override string GetQuery(DatabaseInfo databaseInfo)
        {
            TableInfo.ProductsTableInfo table = databaseInfo.Products;
            string desiredFields = "*";

            // Create the query.
            StringBuilder query = new StringBuilder();
            query.AppendFormat("SELECT {0} FROM {1}", desiredFields, table.TableNameFull);
            query.Append(";");
            return query.ToString();
        }
        //
        //
        //
        public override QueryStatus AcceptData(DatabaseInfo dbInfo, List<object> values, List<string> fieldNames)
        {
            // Load all exchange rows.
            int nColumns = fieldNames.Count;
            if (m_Results == null)
                m_Results = new List<ProductInfoItem>();
            else
                m_Results.Clear();

            int ptr = 0;
            int fptr = 0;
            int n;
            Products.ProductTypes productType;
            ProductInfoItem item = new ProductInfoItem();
            while (ptr < values.Count)
            {
                object o = values[ptr];
                if (o != null)
                {
                    // Extract data
                    string s = o.ToString();
                    string fieldName = fieldNames[fptr];
                    if (fieldName.Equals(dbInfo.Exchanges.ExchangeID) && int.TryParse(s, out n))
                        item.ExchId = n;
                    else if (fieldName.Equals(dbInfo.Products.ProductID) && int.TryParse(s, out n))
                        item.ProdFamilyId = n;
                    else if (fieldName.Equals(dbInfo.Products.Product))
                        item.ProdName = s;
                    else if (fieldName.Equals(dbInfo.Products.ProductType) && Enum.TryParse<Products.ProductTypes>(s, true, out productType))
                        item.ProdType = productType;
                    else if (fieldName.Equals(dbInfo.Products.ProductTT))
                        item.ProdNameTT = s;
                }
                // Increment pointers
                ptr++;
                fptr = (fptr + 1) % nColumns;
                if (fptr == 0)                      // We have read all fields.. we have completed this row.
                {                                   
                    m_Results.Add(item);            // save this current row, 
                    item = new ProductInfoItem();   // create a new object to hold the next row.
                }
            }//next value

            // Exit
            return QueryStatus.Completed;
        }// AcceptData()
        //
        //
        //
        //
        //
        #endregion//Public Methods

    }//end class


    #region ProductInfo Item Class
    // *****************************************************************
    // ****                     Product Item                        ****
    // *****************************************************************
    //
    /// <summary>
    /// Small object to hold a row from the ProductInfo table.
    /// </summary>
    public class ProductInfoItem
    {
        // UV variables
        public int ProdFamilyId = -1;
        public int ExchId = -1;

        public string ProdName = string.Empty;
        public string ProdNameTT = string.Empty;
        //public string ProdNameReuters = string.Empty;
        public Products.ProductTypes ProdType = Products.ProductTypes.Unknown;


        //public string ProdDescription = string.Empty;
        //public TimeZoneInfo ProdTimeZoneInfo = null;
        //public string Currency;
        //public string MonthCycle;

        //public string StartSessionT;
        //public string EndSessionT;
        //public string StartSessionT2;
        //public string EndSessionT2;
        //public string StartSessionOutcryT;
        //public string EndSessionOutcryT;



    }
    //
    //
    #endregion//Event Handlers


}

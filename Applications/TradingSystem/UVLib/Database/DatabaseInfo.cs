using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Database
{
    using MySql.Data.MySqlClient;
    using UV.Lib.IO.Xml;

    public class DatabaseInfo : IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Database server
        private DatabaseLocation m_Location;	    
        public string ServerIP = String.Empty;							// server address		
        public string UserName = "uvclient";
        public string Password = "uv172pwd";

        //
        // Tables available at this server.
        //
        public TableInfo.BarsTableInfo Bars = null;
        public TableInfo.ExchangesTableInfo Exchanges = null;
        public TableInfo.ProductsTableInfo Products = null;
        public TableInfo.InstrumentsTableInfo Instruments = null;

        public TableInfo.TrampStrategiesTableInfo TrampStrategies = null;
        public TableInfo.TrampFillsTableInfo TrampFills = null;
        public TableInfo.TrampModelSignalsTableInfo TrampModelSignals = null;
        public TableInfo.TrampOrdersTableInfo TrampOrders = null;
        public TableInfo.TrampModelPositionsTableInfo TrampPositions = null;

        public TableInfo.BloombergEconomicEvents EconomicEvents = null;
        public TableInfo.BloombergEconomicEventTypes EconomicEventTypes = null;
        public TableInfo.BloombergImportantEconomicEvents EconomicEventImportance = null;


        // Status
        public Queue<string> Errors = new Queue<string>();			// place to store error messages.

        //
        #endregion// members

        #region Public Enums
        // *****************************************************************
        // ****                     Enums                               ****
        // *****************************************************************
        // 
        public enum DatabaseLocation
        {
            apastor,
            uv1,
            cermakDV,  // cermak linux server reachable from DV Network
            cermakRCG,  // cermak linux server (same as above, reachable from RCG network)
            custom
        }
        //
        //
        #endregion//enums

        #region Creators
        // *****************************************************************
        // ****                     Creators                            ****
        // *****************************************************************
        public DatabaseInfo() 
        {

        }		
        //
        //
        #endregion // Creators

        #region Properties
        public DatabaseLocation Location
        {
            get { return m_Location; }
            set
            {
                m_Location = value;
                //
                // Create default information.
                //
                Bars = new TableInfo.BarsTableInfo();
                Exchanges = new TableInfo.ExchangesTableInfo();
                Products = new TableInfo.ProductsTableInfo();
                Instruments = new TableInfo.InstrumentsTableInfo();

                TrampStrategies = new TableInfo.TrampStrategiesTableInfo();
                TrampFills = new TableInfo.TrampFillsTableInfo();
                TrampModelSignals = new TableInfo.TrampModelSignalsTableInfo();
                TrampOrders = new TableInfo.TrampOrdersTableInfo();
                TrampPositions = new TableInfo.TrampModelPositionsTableInfo();

                EconomicEvents = new TableInfo.BloombergEconomicEvents();
                EconomicEventTypes = new TableInfo.BloombergEconomicEventTypes();
                EconomicEventImportance = new TableInfo.BloombergImportantEconomicEvents();

                //
                // Update information that depends on specific server selected.
                //		Servers may offer different table definitions; if so, fix below.
                switch (m_Location)
                {
                    case DatabaseLocation.apastor:
                        ServerIP = "10.10.100.74";				// clearly each server has unique IP address.
                        break;
                    case DatabaseLocation.uv1:
                        ServerIP = "10.10.100.28";
                        break;
                    case DatabaseLocation.cermakRCG:                // RCG network only
                        ServerIP = "10.64.18.90";
                        break;
                    case DatabaseLocation.cermakDV:                 // DV network only.
                        ServerIP = "10.10.101.9";
                        break;
                    default:
                        break;
                }
                // Exit
            }
        }
        #endregion 

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        public bool IsTryToConnect(ref MySqlConnection connection)
        {
            bool isError = false;
            try
            {
                if (connection == null)
                {
                    string connStr = string.Format("Data Source={0};User Id={1};Password={2}"
                        , this.ServerIP, this.UserName, this.Password);	// table name is supplied later.
                    connection = new MySqlConnection(connStr);
                }
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
            }
            catch (Exception ex)
            {
                Errors.Enqueue(string.Format("IsTryToConnect: Database exception = {0}", ex.Message));
                isError = true;
            }
            // Validate and exit.
            bool isOpen = (connection.State == System.Data.ConnectionState.Open) && (!isError);
            if (!isOpen)
            {
                if (connection != null) connection.Close();
                connection = null;
            }
            return isOpen;
        }// IsConnected()
        //
        //
        //
        //
        //
        //
        //
        public override string ToString()
        {
            return this.m_Location.ToString();
        }
        //
        //
        #endregion//Public Methods

        #region Istringifiable Methods
        // *****************************************************************
        // ****               Istringifiable implementation             ****
        // *****************************************************************
        public string GetAttributes()
        {
            return string.Empty;
        }

        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            DatabaseInfo.DatabaseLocation dbLocation;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "Location" && Enum.TryParse<DatabaseInfo.DatabaseLocation>(attr.Value, true, out dbLocation))
                    Location = dbLocation;
                else if (attr.Key == "Username" || attr.Key == "UserName" || attr.Key == "username")
                    UserName = attr.Value;
                else if (attr.Key == "Password" || attr.Key == "password" || attr.Key == "PassWord")
                    Password = attr.Value;
                else if (attr.Key == "ServerIP")
                    ServerIP = attr.Value;
            }   
        }
        public void AddSubElement(IStringifiable subElement)
        {
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.DatabaseReaderWriters
{
    using MySql.Data.MySqlClient;
    public class DatabaseInfo
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Database server
        public DatabaseLocation Location = DatabaseLocation.apastor;	// server for this database.
        public string ServerIP = String.Empty;							// server address		
        public string UserName = "uvclient";
        public string UserPW = "uv172pwd";

        //
        // Tables available at this server.
        //
        public TableInfo.BarsTableInfo Bars = null;
        public TableInfo.ExchangesTableInfo Exchanges = null;
        public TableInfo.ProductsTableInfo Products = null;
        public TableInfo.InstrumentsTableInfo Instruments = null;

        public TableInfo.UVStrategiesTableInfo UVStrategies = null;
        public TableInfo.UVStrategyEnginesTableInfo UVStrategyEngines = null;
        public TableInfo.UVSignalsTableInfo UVSignals = null;
        public TableInfo.UVFillsTableInfo UVFills = null;

        //public TableInfo.TrampStrategiesTableInfo TrampStrategies = null;
        public TableInfo.TrampFillsTableInfo TrampFills = null;
        public TableInfo.TrampModelSignalsTableInfo TrampModelSignals = null;
        public TableInfo.TrampOrdersTableInfo TrampOrders = null;
        public TableInfo.TrampModelPositionsTableInfo TrampPositions = null;

        public TableInfo.EconomicDataTableInfo EconomicEvents = null;
        public TableInfo.EconomicTickersTableInfo EconomicTickers = null;
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
            cermakRCG,  // cermak liniux server (same as above, reachable from RCG network)
            bredev, // dv local server for de work
            custom
        }
        //
        //
        #endregion//enums

        #region Creators
        // *****************************************************************
        // ****                     Creators                            ****
        // *****************************************************************
        protected DatabaseInfo() { }		// default constructor is private.
        //
        //
        // ****				Creator()				****
        public static DatabaseInfo Create(DatabaseLocation location)
        {
            //
            // Create default information.
            //
            DatabaseInfo info = new DatabaseInfo();
            info.Location = location;

            info.Bars = new TableInfo.BarsTableInfo();
            info.Exchanges = new TableInfo.ExchangesTableInfo();
            info.Products = new TableInfo.ProductsTableInfo();
            info.Instruments = new TableInfo.InstrumentsTableInfo();

            info.UVStrategies = new TableInfo.UVStrategiesTableInfo();
            info.UVStrategyEngines = new TableInfo.UVStrategyEnginesTableInfo();
            info.UVSignals = new TableInfo.UVSignalsTableInfo();
            info.UVFills = new TableInfo.UVFillsTableInfo();

            //info.TrampStrategies = new TableInfo.TrampStrategiesTableInfo();
            info.TrampFills = new TableInfo.TrampFillsTableInfo();
            info.TrampModelSignals = new TableInfo.TrampModelSignalsTableInfo();
            info.TrampOrders = new TableInfo.TrampOrdersTableInfo();
            info.TrampPositions = new TableInfo.TrampModelPositionsTableInfo();

            info.EconomicEvents = new TableInfo.EconomicDataTableInfo();
            info.EconomicTickers = new TableInfo.EconomicTickersTableInfo();
            info.EconomicEventTypes = new TableInfo.BloombergEconomicEventTypes();
            info.EconomicEventImportance = new TableInfo.BloombergImportantEconomicEvents();

            //
            // Update information that depends on specific server selected.
            //		Servers may offer different table definitions; if so, fix below.
            switch (location)
            {
                case DatabaseLocation.apastor:
                    info.ServerIP = "10.10.100.74";				// clearly each server has unique IP address.
                    break;
                case DatabaseLocation.uv1:
                    info.ServerIP = "10.10.100.28";
                    break;
                case DatabaseLocation.cermakRCG:                // RCG network only
                    info.ServerIP = "10.64.18.90";
                    break;
                case DatabaseLocation.cermakDV:                 // DV network only.
                    info.ServerIP = "10.10.101.9";
                    break;
                case DatabaseLocation.bredev:                   // bre db for dev work
                    info.ServerIP = "10.10.100.79";
                    break;
                case DatabaseLocation.custom:                   // user will supply their own IP
                    break;
                default:
                    info = null; break;
            }
            // Exit
            return info;
        }// Create()
        //
        //
        //
        //
        //
        //
        //
        //
        //
        #endregion // Creators

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
                    string connStr = string.Format("Data Source={0};User Id={1};Password={2};Convert Zero DateTime=True"
                        , this.ServerIP, this.UserName, this.UserPW);	// table name is supplied later.
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
            return this.Location.ToString();
        }

        //
        //
        #endregion//Public Methods

    }
}

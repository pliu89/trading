using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Data
{
    public class Bar
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        public int mysqlID = -1;            // Instrument ID from mySQL db.
        public double bidPrice;
        public double askPrice;
        public int bidQty;
        public int askQty;

        public double lastTradePrice;       // last trade price
        public int sessionVolume;           // traded volume since sessiosn start as report by TT
        public int longVolume;              // volume on the long side from TimeAndSales
        public int shortVolume;             // volume on the short side form TimeAndSales  
        public int totalVolume;             // volume on the uknown side + short + long form TimeAndSales 

        public int sessionCode;             // 1=trading, 0=not trading
        #endregion// members

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        public override string ToString()
        {
            return string.Format("[{0}|{1}] | [{2}|{3}] last = {4}, vol = {5}", bidQty, bidPrice, askPrice, askQty, lastTradePrice, sessionVolume);
        }
        //
        //
        public const string QueryHeader = "INSERT IGNORE INTO {0} (instrumentID,unixTime,bidPrice,bidQty,askPrice,askQty,lastTradePrice,sessionVolume,longVolume,shortVolume,totalVolume,sessionCode) VALUES ";
        //
        //
        public string GetQueryValues(string timeStamp)
        {
            return string.Format(" {0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                mysqlID,         // 0
                timeStamp,       // 1
                bidPrice,        // 2
                bidQty,          // 3
                askPrice,        // 4    
                askQty,          // 5
                lastTradePrice,  // 6
                sessionVolume,   // 7
                longVolume,      // 8
                shortVolume,     // 9
                totalVolume,     // 10
                sessionCode);    //11

        }
        #endregion//Public Methods

    }
}

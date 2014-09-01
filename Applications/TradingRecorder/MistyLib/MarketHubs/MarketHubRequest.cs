using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Misty.Lib.MarketHubs
{
    public class MarketHubRequest : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        public RequestType Request;
        public List<object> Data = new List<object>();

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public MarketHubRequest()
        {
        }
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


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region Enums
        //
        public enum RequestType
        {
            None = 0
            ,RequestServers = 1
            ,RequestProducts = 2
            ,RequestInstruments = 3
            ,RequestInstrumentSubscription = 4
            ,RequestShutdown = 5                            // starts the shutdown sequence

        }//
        //
        //
        //public enum RequestStatus
        //{
        //    New = 0
        //    ,Success = 1
        //    ,Failed = 2
        //}
        //
        //
        #endregion//Enums

    }
}

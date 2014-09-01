using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.MarketHubs
{
    public class MarketHubRequest : EventArgs, IEquatable<MarketHubRequest>
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        public RequestCode Request;
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


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        /// <summary>
        /// Compares to MarketHubRequests to see if they are the same in terms of their
        /// requests for underlying instruments or products
        /// </summary>
        /// <param name="marketHubRequest"></param>
        /// <returns>false if different</returns>
        public bool Equals(MarketHubRequest marketHubRequest)
        {
            if (marketHubRequest.Request.Equals(this.Request) && marketHubRequest.Data.Count == this.Data.Count)
            {
                for (int i = 0; i < this.Data.Count; i++)
                {
                    if (this.Data[i].Equals(marketHubRequest.Data[i]))
                        continue;
                    else
                        return false;
                }
                return true;
            }
            else
                return false;
        }
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
        /*
        public enum RequestCode
        {
            None = 0
            ,RequestServers = 1
            ,RequestProducts = 2
            ,RequestInstruments = 3
            ,RequestInstrumentSubscription = 4
            ,RequestShutdown = 5                            // starts the shutdown sequence

        }//
        */
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

using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.OrderHubs
{
    public class OrderHubRequest : EventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        public RequestType Request;
        public object[] Data = null;
        //        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderHubRequest(OrderHubRequest.RequestType type)
        {
            this.Request = type;
        }
        //
        //       
        #endregion//Constructors


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public override string ToString()
        {
            if ( Data != null && Data.Length > 0 )
                return string.Format("{0} {1}", this.Request,Data[0]);
            else
                return string.Format("{0} ", this.Request);
        }
        //
        //       
        #endregion//Public Methods


        #region Enums
        // *****************************************************************
        // ****                     Enums                               ****
        // *****************************************************************
        //
        public enum RequestType
        {
            None
            ,RequestConnect                     // connect to order server.
            ,RequestShutdown
            ,RequestDropCopyNow                 // Request to output a hard copy of fills etc.
            ,RequestDropCopyArchive             // Split off the current drop copy, rename it, and start a new one.
            ,RequestRealPnLReset                // Request PnLs in fill books be set to zero. Good before starting a new trading session.
            //
            // Specific instr requests
            ,RequestCreateBook                  // request an order book.
            ,RequestDeleteBook                  // request to delete order book.
            ,RequestCompleteBookReset               // Deletes and cleans all books.

        }
        //
        #endregion//Enums

    }
}

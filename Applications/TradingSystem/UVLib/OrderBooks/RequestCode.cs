using System;

namespace UV.Lib.OrderBooks
{
    /// <summary>
    /// This enum provides internal request codes that should be processed 
    /// by any class implementing OrderBookHub.
    /// </summary>
    public enum RequestCode
    {
            None
            ,ServiceStateChange             // Request a change to Service State - requires use provide desired state.
            //,DropCopyNow                  // Request to output a hard copy of fills etc.
            //,DropCopyArchive              // Split off the current drop copy, rename it, and start a new one.
            //,RealPnLReset                 // Request PnLs in fill books be set to zero. Good before starting a new trading session.
            //
            // Specific instr requests
            ,CreateBook                     // request an order book.
            //,DeleteBook                   // request to delete order book.
            //,CompleteBookReset            // Deletes and cleans all books.
    }
}

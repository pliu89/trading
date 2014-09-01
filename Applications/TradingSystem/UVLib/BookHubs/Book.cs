using System;
using System.Collections.Generic;
using System.Threading;

using UV.Lib.Products;

namespace UV.Lib.BookHubs
{
    public class Book
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // Instrument Book
        public ReaderWriterLockSlim Lock;
        //public List<string> LockHolders = new List<string>();
        public Dictionary<int, Market> Instruments;   // instruments described by this book.


        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public Book()
        {
            Instruments = new Dictionary<int, Market>();
            Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
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
        // ****                 CopyTo()                    ****
        //
        /// <summary>
        /// After the book hub obtains the write lock for the next writable book, 
        /// it can copy this book to that one.  
        /// Note: In practice, it doesn't need to get a read lock on this book, if 
        /// its the only thread that can get a write lock.
        /// Called by the BookHub only.
        /// </summary>
        /// <param name="aWriteLockedBook">A book already locked for writing</param>
        public virtual void CopyTo(Book aWriteLockedBook)
        {
            foreach (int id in this.Instruments.Keys)
            {
                Market myContract = this.Instruments[id];     // my instrument
                Market aContract;                             // instrument in book to copy over.
                if (!aWriteLockedBook.Instruments.TryGetValue(id, out aContract))
                {
                    aContract = Market.CreateCopy(myContract);
                    aWriteLockedBook.Instruments.Add(id, aContract);
                }
                else
                {
                    myContract.CopyTo(aContract);
                }
            }
        }//end CopyTo().
        //
        //
        //
        public override string ToString()
        {
            return String.Format("Book: {0} Instruments", Instruments.Count);
        }
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers


        #region no Book Event Args
        // *****************************************************************
        // ****                     Event Args                          ****
        // *****************************************************************
        //
        /*
        public class BookEventArgs : EventArgs
        {
            // Members
            public readonly BookEventType Type;
            public Instrument m_Instrument = null;

            // Contructors
            public BookEventArgs(BookEventType type)
            {
                this.Type = type;           // store the type of event this is.
            }
        } //end BookEventArgs     
        //
        //
        //
        // *************************************************************
        // ****                 Book Event Type                     ****
        // *************************************************************
        public enum BookEventType
        {
            AddInstrumentToBook = 1
        }
        */
        //
        #endregion//Event Handlers



    }
}

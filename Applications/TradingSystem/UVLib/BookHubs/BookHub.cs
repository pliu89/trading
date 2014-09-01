using System;
using System.Collections.Generic;
using System.Text;

using UV.Lib.Hubs;

namespace UV.Lib.BookHubs
{
    /// <summary>
    /// This class is a hub that manages and updates books for multiple readers (on different threads).
    /// Only the local hub thread is allowed to write to these books. Many readers, but a single writer thread.
    /// Usage:
    ///     Each Book contains multiple instruments each accessed by an ID#.
    ///     Users subscribe to InstrumentChanged event, which returns the instr ID#s that changed.
    /// Internal procedure: 
    ///     This hub manages a collection of duplicate books.  Only one book is designated
    /// as the "current" or "published" book at any given moment that is accessible to the many readers.
    /// As new events come in, if the "current" or published book is being read by another thread,
    /// then the next book in the collection is updated and prepared, and then published, 
    /// with events sent out to subscribers to look at the new current book.
    /// </summary>
    public abstract class BookHub : Hub
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // Market Books:
        //
        private const int MaxNBooks = 16;
        protected int m_NBooks = 0;                         // number of books currently created.
        protected Book[] m_Book = new Book[MaxNBooks];      // book copies        
        protected volatile int m_CurrentBook = -1;          // in [0 , NBooks]
        private int m_NextInstrID = 0;						// number of instruments in each book.
        protected int m_EnterBookReadTimeOut = 1;			// milisecs to hold thread.

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public BookHub(string name, string logDirectoryName, bool isShowLog)
            : base(name, logDirectoryName, isShowLog, LogLevel.ShowAllMessages)
        {
            /*
            // Create our first book.
            m_Book[0] = new Book();
            m_CurrentBook = -1;            
            m_NBooks = 1;
            */

            //this.LogBookState(string.Empty);
        }//end constructor
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
        // ****             Enter Read Book()               ****
        //
        /// <summary>
        /// Requests a read lock on the current book and receives a pointer to it.
        /// Called by an external thread.
        /// </summary>
        /// <returns>Current book object.</returns>
        public bool TryEnterReadBook(out Book aBook)
        {
            aBook = null;
            if (m_CurrentBook < 0)
                return false;
            int nFailures = 0;
            while (nFailures < 3)		                                    // Thread tries several times to get Book!
            {
                aBook = m_Book[m_CurrentBook];
                bool isSuccess = aBook.Lock.TryEnterReadLock(m_EnterBookReadTimeOut);	    // Timeout
                if (aBook.Lock.IsReadLockHeld)
                    return true;					                        // successfully EXIT obtained Read Lock.
                else
                    aBook = null;                                           // Failed to get Read Lock on current book!
                nFailures++;
            }//while.
            LogBookState("{0} failed to acquire read lock for book {1}.", System.Threading.Thread.CurrentThread.Name, m_CurrentBook.ToString());   
            return false;
        }//EnterReadLock().
        //
        // 
        // ****             Exit Read Book()                ****
        //
        /// <summary>
        /// Returns the read-lock for this book.  This should be called as soon
        /// as the reader no longer needs to read the book.
        /// Called by an external thread.
        /// </summary>
        /// <param name="aBook">Book to release lock.</param>
        public void ExitReadBook(Book aBook)
        {
            aBook.Lock.ExitReadLock();
            aBook = null;
        }//ExitReadLock().
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        // ****             AddInstrumentToBook()               ****
        /// <summary>
        /// Adds an additional instrument to the next writable book.
        /// Called by local bookhub thread.
        /// </summary>
        protected int AddInstrumentToBook(Market instr)
        {
            int bookID = EnterWriteLock();              // get a writer lock.    
            instr.ID = m_NextInstrID;					// take next available instrID.
            m_NextInstrID++;							// increment global instrID. (Ensures uniqueness!)
            m_Book[bookID].Instruments.Add(instr.ID, instr);
            Log.NewEntry(LogLevel.Minor, "Added new {2}th instrument {0} to book {1}", instr.Name, bookID.ToString(), instr.ID.ToString());
            if (m_CurrentBook < 0)
                m_CurrentBook = bookID;
            if (bookID != m_CurrentBook)
                m_Book[m_CurrentBook].CopyTo(m_Book[bookID]);   // no reader lock here, since I am only writer, so i can freely read.
            ExitWriteLock(bookID);
            return instr.ID;
        }//end AddInstrumentToBook()
        //
        //
        //
        //
        //
        //
        //
        // ****               Enter Write Lock()           ****
        // 
        /// <summary>
        /// Called by local bookhub thread.   Returns the next book that is free for
        /// the Writer thread (me).
        /// </summary>
        /// <returns>ID book available for updating.</returns>
        protected int EnterWriteLock()
        {
            int i = 0;
            int lockedBookID = -1;								// place to store bookID we can lock!
            while (i < m_NBooks && lockedBookID < 0)			// search thru each of NBooks in list.
            {
                int bookID = (i + m_CurrentBook) % m_NBooks;    // start at the current book, mod NBooks.
                // Try to get a writer's lock on this bookID.  Skip if readers are inside it, or are waiting.
                if (m_Book[bookID].Lock.WaitingReadCount == 0 && m_Book[bookID].Lock.TryEnterWriteLock(0))
                    lockedBookID = bookID;                      // We have obtained this book's lock
                else
                    i += 1;                                     // we can not obtain this book's lock, increment and try again.
            }
            if (lockedBookID < 0)
            {                                                   // We could not get the writer-lock of any book,
                lockedBookID = CreateNewBook();                 // so make new book, lock it, add to our collection.
            }
            // Log report on all books.
            if (lockedBookID != m_CurrentBook)
                LogBookState(string.Empty);
            //LogBookState("Switch to book {0}.", lockedBookID.ToString());
            return lockedBookID;
        }// Enter Write Lock().
        //
        //
        //
        // ****             Exit Write Lock()                  ****
        /// <summary>
        /// Release the currently writer-locked book.
        /// </summary>
        protected void ExitWriteLock(int writeLockedBookID)
        {
            Book aWriteLockedBook = m_Book[writeLockedBookID];
            if (aWriteLockedBook.Lock.IsWriteLockHeld)
                aWriteLockedBook.Lock.ExitWriteLock();
            else
                Log.NewEntry(LogLevel.Error, "ExitBookLock: Lock not locked BookID={0}", writeLockedBookID.ToString());
        }//end ExitBookLock().
        //
        //
        //
        // ****                 Create New Book()               ****
        //
        /// <summary>
        /// Creates a new book and gets a writer lock on it.  The lock is obtained
        /// prior to making its existance public to avoid any race condition.
        /// Called by the hub thread.
        /// </summary>
        /// <returns>ID of new book.</returns>
        protected int CreateNewBook()
        {
            int newBookID = -1;
            if (m_NBooks < m_Book.Length)
            {   // Create a book at the end of our list.
                newBookID = m_NBooks;								// ID of new book.                      
                Book newBook = new Book();							// create the new book.
                newBook.Lock.EnterWriteLock();                      // 2012 - improvement
                if (m_CurrentBook > -1)
                    m_Book[m_CurrentBook].CopyTo(newBook);		    // completely copy current book, if this isn't the first book.               
                m_Book[newBookID] = newBook;                        // Add the locked book to our collection of books.
                m_NBooks++;
            }
            else
            {   // No more space error!!!
                Log.NewEntry(LogLevel.Error, "No more book space!");
                throw (new Exception("BookHub.CreateNewBook(). Have run out of book space."));
            }
            Log.NewEntry(LogLevel.Major, "Created the {0}th new book.", m_NBooks.ToString());
            return newBookID;
        }//end CreateNewBook().
        //
        //
        //
        private void LogBookState(string format, params object[] args)
        {
            if (Log.BeginEntry(LogLevel.Minor))
            {
                if (!String.IsNullOrEmpty(format))
                {
                    Log.AppendEntry(format, args);
                    Log.AppendEntry(" ");
                }
                // Version two:
                Log.AppendEntry("Books:");
                Log.AppendEntry(" read(wait) =");
                for (int ii = 0; ii < m_NBooks; ++ii)
                {
                    if (m_Book[ii].Lock.IsWriteLockHeld)
                        Log.AppendEntry(" {0}[{1}]", m_Book[ii].Lock.CurrentReadCount.ToString(), m_Book[ii].Lock.WaitingReadCount.ToString());
                    else
                        Log.AppendEntry(" {0}({1})", m_Book[ii].Lock.CurrentReadCount.ToString(), m_Book[ii].Lock.WaitingReadCount.ToString());
                    if (m_CurrentBook == ii) Log.AppendEntry("*");
                }
                Log.AppendEntry(" ");
                Log.EndEntry();
            }
        }//LogBookState().
        //
        //
        //
        #endregion//Private Methods


        #region Event Processing for Books
        // *****************************************************************
        // ****                 Event Processing for Books              ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        // ****                 ProcessBookEvents()                   ****
        //
        /// <summary>
        /// Once an event in the queue is determined to be a market change event, or
        /// any event changing the "market book" of an instrument, this method is 
        /// called to update the market instrument books.
        /// Called by hub thread only.  
        /// </summary>
        /// <param name="eArgList">A list of market change events.</param>
        protected void ProcessBookEvents(List<EventArgs> eArgList)
        {
            int updateBookID = EnterWriteLock();                                            // step 0.0            
            BringBookUpToDate(updateBookID);  // Bring this book up to current date.        // step 1, 2, not 3, 4, and 5.            

            // Process new events by calling super class (since this depends on particular exchange)!            
            InstrumentChangeArgs eventArgs = ProcessBookEventsForABook(updateBookID, eArgList); // step 3            

            // Finalize book that was updated.
            ExitWriteLock(updateBookID);
            m_CurrentBook = updateBookID;         // Now publish book as current book!      // step 6.0  
            //this.LogBookState(string.Empty);

            /*
            Book aBook;
            if (TryEnterReadBook(out aBook) )
            {
                if (Log.BeginEntry(LogLevel.Minor, "ProcessBookEvents: "))
                {
                    foreach (int id in updatedInstrList)
                    {
                        Market mkt = aBook.Instruments[id];
                        Log.AppendEntry(mkt.ToString());
                    }
                    Log.EndEntry(updatedInstrList.Count > 0);
                }
                ExitReadBook(aBook);
            }
            */ 

            // Trigger events for updated instruments.

            if (eventArgs != null)
            {
                eventArgs.Sender = this;
                OnInstrumentChange(eventArgs);           // step 7.0
            }
        }//end HubEventHandler().
        //
        //
        // ****             BringBookUpToDate()             ****
        //
        /// <summary>
        /// Version 1:
        /// Example: Assume 3 books, current book is #2, and new events "F" come in to system.  
        ///     GetNextBookLock() will return either #2 (if no one currently looking at it) or #3.
        ///     Book ID:        1           2           3
        ///     eventList:      D           E           C  
        ///     currentBook:                *
        ///     
        ///     CASE 1: 
        ///     0.0 GetNextUpdateBook ---> updatebook = 3, which is not the current book.
        ///     1.0 stopAtBook = current + 1  ---> "3"
        ///     2.1 (updateBook+1) ---> "1", so process(events#1 = D)
        ///     2.2 (updateBook+2) ---> "2", so process(events#2 = E)
        ///     2.3 (updateBook+3) ---> "3", is the stopAtBook, so stop.
        ///     3.0 process new events "F".
        ///     4.0 if ( currentBook != updateBook ) clear events in updateBook (delete C).
        ///     5.0 Add "F" to events#3.
        ///     6.0 Publish book.
        ///     CASE 2: 
        ///     0.0 GetNextUpdateBook --->  updateBook = 2, which IS the currentBook!    
        ///     1.0 stopAtBook = current + 1 ---> "3"
        ///     2.1 (updateBook+1) ---> "3", is the stopAtBook, so stop.
        ///     3.0 process new events "F".
        ///     4.0 since ( currentBook == updateBook ) don't clear events in updateBook (don't delete E)
        ///     5.0 Add "F" to events#2 (updateBook events)
        ///     6.0 Publish book.
        /// Version 2:
        ///     If the current != updateBook, copy all mkt values to updateBook.
        /// </summary>
        private void BringBookUpToDate(int updateBookID)
        {
            /*
            // Version 1: Bring this book up to date.                        
            int stopAtBook = m_NextBook[m_CurrentBook];     // step 1.0 (stopAtBook = current + 1).
            int iBook = m_NextBook[updateBookID];             // step 2.1 booki = (updateBook + 1)
            while (iBook != stopAtBook)
            {
                ProcessBookEventsForABook(updateBookID, m_Book[iBook].EventList);
                iBook = m_NextBook[iBook];                 // step 2.i booki = (updateBook + 1)
            }
            // Update the processed-event list in book.
            if (m_CurrentBook != updateBookID) { m_Book[updateBookID].EventList.Clear(); }    // step 4.0
            m_Book[updateBookID].EventList.AddRange(eArgList);                                  // setep 5.0
            */
            // Version #: Bring this book up to date. 
            if (m_CurrentBook != updateBookID) m_Book[m_CurrentBook].CopyTo(m_Book[updateBookID]);

        }//BringBookUpToDate()
        //
        //
        // ****             Process Book Events For A Book()            ****
        //
        /// <summary>
        /// Give a particular book ID, and the events, this abstract method actually does the 
        /// book updating.  As this is dependent on both the detailed form of the messages in 
        /// the events AND the particular type of instrument in the book, it must be implemented
        /// by the super class.
        /// </summary>
        /// <param name="bookID"></param>
        /// <param name="eArgList"></param>
        /// <returns>InstrumentChangeArgs list of changed instruments</returns>
        protected abstract InstrumentChangeArgs ProcessBookEventsForABook(int bookID, List<EventArgs> eArgList);
        //
        //
        #endregion


        #region BookHub Events:  "InstrumentChange"
        // *****************************************************************
        // ****                   Instrument Change                     ****
        // *****************************************************************
        //
        public event EventHandler InstrumentChanged;
        //
        //
        //
        /// <summary>
        /// Here, we already have the event object (perhaps someone else created it).  Before
        /// triggering event, we mark that we are the senders, but note, if someone else created
        /// it, "CustomEventCreator" could be a different object (say, the object that requested
        /// something from this book). 
        /// </summary>        
        protected void OnInstrumentChange(InstrumentChangeArgs args)
        {
            args.Sender = this;                     // update the sender before triggering event
            if (this.InstrumentChanged != null)
            {
                InstrumentChanged(this, args);
            }
        }//OnInstrumentChange()
        //
        //
        //
        //
        #endregion//BookHub Events


    }//end BookHub class
}

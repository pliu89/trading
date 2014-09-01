using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace Misty.Lib.OrderHubs
{
    using Misty.Lib.Hubs;
    using Misty.Lib.Application;

    using Misty.Lib.Products;

    /// <summary>
    /// TODO: Since orderbooks depend on the implementation, we might consider using Generics for m_Books.
    /// </summary>
    public class OrderHub : Hub, IService
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Internal order books
        //
        protected ConcurrentDictionary<InstrumentName, OrderBook> m_Books = new ConcurrentDictionary<InstrumentName, OrderBook>();
        private int m_WriteLockTimeout = -1;                             // timeout in miliseconds.
        //private int m_ReadLockTimeout = -1;                              // timeout in miliseconds.

        //
        // Internal services
        //protected EventWaitQueue m_EventWaitQueue = null;
        
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public OrderHub(string hubName)
            : base(hubName, AppInfo.GetInstance().LogPath, false, LogLevel.ShowAllMessages)
        {
            //m_EventWaitQueue = new EventWaitQueue(this.Log);
            //m_EventWaitQueue.ResubmissionReady += new EventHandler(this.HubEventEnqueue);
            this.Stopping += new EventHandler(Hub_Stopping);

        }
        //
        //       
        #endregion//Constructors


        #region Properties
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
        public virtual void Connect()
        {
        }
        //
        //
        //
        /*
        public bool TryReadBook(InstrumentName name, out OrderBook)
        {
            OrderBook book;
            if ( m_Books.TryGetValue(name,out book) )
            {
                
            }
        }
        */ 
        //
        // *************************************************************
        // ****                 Try Enter Book Write()              ****
        // *************************************************************
        protected bool TryEnterBookWrite(InstrumentName name, out OrderBook book)
        {
            book = null;
            if ( m_Books.TryGetValue(name,out book) && book.Lock.TryEnterWriteLock(m_WriteLockTimeout) )  
                return true;
            else 
                return false;
        }// TryEnterBookWrite()
        //
        // ****                 Exit Book Write()                   ****
        //
        protected void ExitBookWrite(OrderBook book)
        {
            book.Lock.ExitWriteLock();
        } // ExitBookWrite()
        //
        //
        //
        // *************************************************************
        // ****                 Requests()                          ****
        // *************************************************************
        /// <summary>
        /// This is called once after, the hub is started and the application ready 
        /// for conections to exchanges or APIs are available.  The inheriting class
        /// will override this to make those connections.
        /// </summary>
        public virtual void RequestConnect()
        {
            this.HubEventEnqueue(new OrderHubRequest(OrderHubRequest.RequestType.RequestConnect));
        }
        public override void RequestStop()
        {
            this.HubEventEnqueue(new OrderHubRequest(OrderHubRequest.RequestType.RequestShutdown));
        }
        //
        #endregion//Public Methods


        #region Private Hub Event Handlers 
        // *****************************************************************
        // ****                  Hub Event Handlers                     ****
        // *****************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventArgs"></param>
        override protected void HubEventHandler(EventArgs[] eventArgs)
        {
            throw new NotImplementedException("OrderHub base class event handler not implemented.");
        }// HubEventHander()
        //
        //
        //
        //
        #endregion//Private  Hub Event Handlers



        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        // *********************************************
        // ****             Hub_Stopping()          ****
        // *********************************************
        /// <summary>
        /// Called when the base hub triggers the Stopping event.
        /// Shuts down its services nicely.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected void Hub_Stopping(object sender, EventArgs eventArgs)
        {
            /*
            if (m_EventWaitQueue != null)
            {
                m_EventWaitQueue.Dispose();
                m_EventWaitQueue = null;
            }
            */ 
            

        }//Hub_Stopping()
        //
        //
        //
        #endregion//Private Methods


        #region Event Triggers 
        // *****************************************************************
        // ****                     Event Triggers                     ****
        // *****************************************************************
        //
        //
        //
        //
        //
        // *****************************************************************
        // ****                 Book Created                            ****
        // *****************************************************************
        public event EventHandler BookCreated;
        //
        protected void OnBookCreated(EventArgs eventArgs)
        {
            if (BookCreated != null)
                BookCreated(this, eventArgs);
        }
        //       
        // *****************************************************************
        // ****                Book Deleted                             ****
        // *****************************************************************
        public event EventHandler BookDeleted;
        //
        protected void OnBookDeleted(EventArgs eventArgs)
        {
            if (BookDeleted != null)
                BookDeleted(this, eventArgs);
        }
        //       
        //
        //
        // *****************************************************************
        // ****                Book Changed                             ****
        // *****************************************************************
        public event EventHandler BookChanged;
        //
        protected void OnBookChanged(EventArgs eventArgs)
        {
            if (BookChanged != null)
                BookChanged(this, eventArgs);
        }
        //
        //
        //
        //
        //
        //
        #endregion//Event Triggers



        #region IService implementation
        // *****************************************************
        // ****                 IService                    ****
        // *****************************************************
        //
        public string ServiceName
        {
            get { return this.HubName; }
        }
        //
        //
        private void OnServiceStateChanged()
        {
            if (ServiceStateChanged != null)
                ServiceStateChanged(this, EventArgs.Empty);
        }
        public event EventHandler ServiceStateChanged;
        //
        //
        //
        #endregion//IService implementation




    }
}

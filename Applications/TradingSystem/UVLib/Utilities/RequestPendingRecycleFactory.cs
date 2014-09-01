using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
//using System.Linq;
//using System.Text;

namespace UV.Lib.Hubs
{
    using UV.Lib.Utilities;

    /// <summary>
    /// Many hubs implementations usse a great deal of internal messaging, 
    /// its convenient to have recycling and a place to store requests that
    /// are waiting to be resubmitted later.
    /// This utility class creates new RequestEventArg and manages a pending
    /// list for those the hub wants to store for later submission.
    /// 
    /// Features:
    ///     1) Takes enum type "T".  
    ///         The enum should describe each of the specific requests that
    ///         the implementing hub requires. 
    ///         Usually these RequestEventArgs are private to the implementing hub.
    ///         
    ///     2) Recycles RequestEventArgs.
    ///         * Everytime the hub has completed a request, the RequestEventArg should
    ///             be retuned to this object using the Recycle() method.
    ///         * When a RequestEventArg is needed, call method Get() and either 
    ///             a new one is created or one is taken from the recycled queue and 
    ///             returned to be reused.  
    ///         * Prior to being returned to user, each RequestEventArg is cleaned
    ///             completely using the RequestEventArg.Clear().  This returns it
    ///             to its default new state.
    ///             
    ///     3) Threadsafe.
    ///         * Multiple threads can call Get() and Recycle() freely.
    /// </summary>
    public class RequestPendingRecycleFactory<T> : RecycleFactory<RequestEventArg<T>> where T : struct, IConvertible
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //        
        // Pending controls
        private int m_MaxWaitTicks = 30;                // default ticks (usually each tick is 1 second long).
        private int m_LastTick = 1;
        private object m_PendingQueueLock = new object();
        private Dictionary<int, Queue<RequestEventArg<T>>> m_PendingQueues = new Dictionary<int, Queue<RequestEventArg<T>>>();

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public RequestPendingRecycleFactory()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maximumWaitTicks">Maximum number of ticks allowed to wait for any pending request.</param>
        public RequestPendingRecycleFactory(int maximumWaitTicks)
        {
            m_MaxWaitTicks = maximumWaitTicks;
        }
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        /// <summary>
        /// Maximum number of clock ticks a Request can be placed into the future.
        /// </summary>
        public int MaximumPendingTime
        {
            get {return m_MaxWaitTicks;}
            set 
            {
                m_MaxWaitTicks = value;
                // Now move all pending queues for larger times into new largest bin.
                    
            }
        }
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        // *****************************************
        // ****             Get()               ****
        // *****************************************
        public override RequestEventArg<T> Get()
        {
            RequestEventArg<T> request = base.Get();
            request.Clear();
            return request;
        }// Get()
        //
        public virtual RequestEventArg<T> Get(T code)
        {
            RequestEventArg<T> request = Get(code,null);
            return request;
        }// Get()
        //
        /// <summary>
        /// This overload allows for data to be added.
        /// </summary>
        public virtual RequestEventArg<T> Get(T code, object arg)
        {
            RequestEventArg<T> request = base.Get();
            request.Clear();
            request.RequestType = code;
            if (arg != null)
                request.Data.Add(arg);
            return request;
        }// Get()
        //
        //
        //
        //
        // *****************************************
        // ****         AddPending()            ****
        // *****************************************
        public void AddPending(int waitTicks, RequestEventArg<T> pendingRequest)
        {
            lock (m_PendingQueueLock)
            {
                // Compute time for these new pending items.
                int insertTime = (m_LastTick + waitTicks) % m_MaxWaitTicks;     // increment is here also!
                
                // Look for their place in the item list.
                Queue<RequestEventArg<T>> queue;
                if (! m_PendingQueues.TryGetValue(insertTime, out queue) )
                {
                    queue = new Queue<RequestEventArg<T>>();
                    m_PendingQueues.Add(insertTime, queue);
                }
                // Add them to the correct queue.
                queue.Enqueue(pendingRequest);
            }
        }//AddPending()
        //
        //
        // *****************************************
        // ****     TickAndTryGetPending()      ****
        // *****************************************
        /// <summary>
        /// Increments the current clock tick, and returns any pending items.
        /// The reason this takes a EventArgs list (and not RequestEventArg list), is
        /// so that the added pending requests can be passed directly to the Hub.HubEventEnqueue( List ) method.
        /// </summary>
        /// <param name="pendingRequest"></param>
        /// <returns></returns>
        public bool TickAndTryGetPending(ref List<EventArgs> pendingRequests)
        {
            bool isPendingEventsFound = false;

            lock (m_PendingQueueLock)
            {                
                // Increment the tick
                m_LastTick = (m_LastTick + 1) % m_MaxWaitTicks;
            
                // Search for those requests that are ready for resubmission.
                Queue<RequestEventArg<T>> queue;
                if (m_PendingQueues.TryGetValue(m_LastTick, out queue) && queue.Count > 0)
                {   // There is a queue at this time, and its NOT empty!
                    isPendingEventsFound = true;
                    while (queue.Count > 0)
                        pendingRequests.Add(queue.Dequeue());           // load the callers list with items.
                }
            }
            // Exit
            return isPendingEventsFound;
        }//TickAndTryGetPending()
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


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

    }//end class
}

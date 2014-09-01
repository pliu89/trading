using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace UV.Lib.Utilities
{


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
    ///         
    /// </summary>
    public class RequestFactory<T> : RecycleFactory<RequestEventArg<T>> where T : struct, IConvertible
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        /// <summary>
        /// Deafult Constructor for Request Factory
        /// </summary>
        public RequestFactory()
            : this(0)
        {
        }//constructor
        //
        /// <summary>
        /// Constructor allowing for intial creation of requests
        /// </summary>
        /// <param name="initialNumberOfItems"></param>
        public RequestFactory(int initialNumberOfItems)
        {
            int count = 0;
            lock (m_StorageLock)
            {
                while (count < initialNumberOfItems)
                {
                    RequestEventArg<T> eventArg = new RequestEventArg<T>();
                    m_Storage.Enqueue(eventArg);
                    count++;
                }
            }
        }
        //
        //       
        #endregion//Constructors




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
            RequestEventArg<T> request = Get();
            request.RequestType = code;
            return request;
        }// Get()
        //
        /// <summary>
        /// This overload is the usual way that one or more arguments
        /// are passed all at once.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="args">array of objects to be added to Data.</param>
        /// <returns></returns>
        public virtual RequestEventArg<T> Get(T code, params object[] args)
        {
            RequestEventArg<T> request = base.Get();
            request.Clear();
            request.RequestType = code;
            if (args != null)
            {
                foreach (object arg in args)
                    request.Data.Add(arg);
            }
            return request;
        }
        //
        //
        /// <summary>
        /// Overload copies objects from list.  List is not kept, 
        /// but the objects in it are taken and added to Data[].
        /// </summary>
        /// <param name="code"></param>
        /// <param name="objList">List of objects to load into Data</param>
        /// <returns></returns>
        public virtual RequestEventArg<T> Get(T code, List<object> objList)
        {
            RequestEventArg<T> request = base.Get();
            request.Clear();
            request.RequestType = code;
            if (objList != null)
            {
                request.Data.AddRange(objList);
            }
            return request;
        }
        #endregion//Public Methods





    }//end class
}

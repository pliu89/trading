using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Utilities
{
    /// <summary>
    /// Most hubs use an internal system of request eventArgs to push and process tasks.
    /// That is, external users will make requests of the hub by push an EventArg onto its queue
    /// which is then processed by the hub thread later.
    /// Typically, the tasks are enumerated by an enum, task like Startup, Shutdown, AddInstrument, etc.
    /// So, for convenience, these messages can be created using this specific factory.  
    /// Usage:
    ///     1) The user need only provide its specific enum, and instantiate this recycling factory.
    ///     2) Then, the RequestEventArgs are automatically created using the Get() method.
    ///     3) When processing is complete, RequestEventArgs can be recycled using the Recycle() method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RequestEventArg<T>: EventArgs, IEquatable<RequestEventArg<T>> where T : struct, IConvertible
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // 
        // Request type and data.
        //
        public T RequestType = default(T);                  // enum that defines the request code
        public List<object> Data = new List<object>();      // place for data

        //
        // Internal statistics
        //
        // TODO: add internal counter for failures?

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
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
        //
        //
        // *************************************************
        // ****                 Equals                  ****
        // *************************************************
        /// <summary>
        /// Compares two requests to see if they are the same in terms of their
        /// request type and data components.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>false if different</returns>
        public bool Equals(RequestEventArg<T> request)
        {
            if (request.RequestType.Equals(this.RequestType) && request.Data.Count == this.Data.Count)
            {
                for (int i = 0; i < this.Data.Count; i++)
                {
                    if (this.Data[i].Equals(request.Data[i]))
                        continue;
                    else
                        return false;
                }
                return true;
            }
            else
                return false;
        }//Equals()
        //
        //
        //
        // *****************************************
        // ****             Clear()             ****
        // *****************************************
        /// <summary>
        /// A nice way to clean out the data. Useful when recycling these objects.
        /// </summary>
        public void Clear()
        {
            this.RequestType = default(T);
            this.Data.Clear();
        }// Clear()
        //
        //
        //
        //
        // *****************************************
        // ****         ToString()              ****
        // *****************************************
        public override string ToString()
        {
            return this.RequestType.ToString();
        }// ToString()
        //
        //
        //
        #endregion//Public Methods



    }
}

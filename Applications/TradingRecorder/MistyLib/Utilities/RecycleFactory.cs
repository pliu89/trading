using System;
using System.Collections.Generic;
//using System.Collections.Concurrent;

namespace Misty.Lib.Utilities
{
    /// <summary>
    /// Usage:
    /// Improvement Notes:
    ///     1) Upgrade storage to Concurrent.Queue, is that enough to make this thread-safe?
    ///         Allowing me to remove the StorageLock mutex.
    ///     2) Perhaps include a lambda expression at construction that knows how to clear a recycled
    ///         object for new user.
    /// </summary>
    /// <typeparam name="T"></typeparam>
	public class RecycleFactory<T> where T : new()
	{

		#region Members
		// *****************************************************************
		// ****                     Members                             ****
		// *****************************************************************
		//
		// Internal storage
		//		
		private Queue<T> m_Storage = new Queue<T>();
		private object m_StorageLock = new object();
		private int m_ItemsCreated = 0;
		private int m_StoragePeak = 0;   
        private int m_DupeAddAttemps = 0;               // times that a user has tried to add the same object more than once.
                                                        // This non-zero suggests error in owner of this object.    
		#endregion// members


		#region no Constructors
		// *****************************************************************
		// ****                     Constructors                        ****
		// *****************************************************************
		//
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
		// ****				GetBar()				****
		/// <summary>
		/// Recover a previously used Bar object from storage, or create a new Bar.
		/// </summary>
		/// <returns></returns>
		public T Get()
		{
			T item = default(T);
            bool isRecycledItemAvailable;
			lock (m_StorageLock)
			{
                if (m_Storage.Count > 0)
                {
                    item = m_Storage.Dequeue();
                    isRecycledItemAvailable = true;
                }
                else
                    isRecycledItemAvailable = false;
			}
            //if (object.Equals(item,default(T)))     
            if (! isRecycledItemAvailable)              // This bool is used so we can store non-nullable objects as well.
            {
                item = new T();                         // Ok, so create new one instead.
                m_ItemsCreated++;
            }
            //if (!m_Storage.TryDequeue(out item))//Concurrent version is good!
            //{
            //    item = new T();
            //    System.Threading.Interlocked.Increment(ref m_ItemsCreated);                
            //}
			return item;
		}// Get()
		//
		//
		// ****				Recycle()				****
		//
		/// <summary>
		/// Put previously used item into store for recycling.
		/// </summary>
		/// <param name="aBar"></param>
		public void Recycle(T item)
		{
			lock (m_StorageLock)
			{
                if (m_Storage.Contains(item))
                    System.Threading.Interlocked.Increment(ref m_DupeAddAttemps);   // User shouldn't add same object to list more than once!
                else
				{
					m_Storage.Enqueue(item);
					m_StoragePeak = Math.Max(m_StoragePeak, m_Storage.Count);
				}
			}
		}//RecycleBar()
		//
		//
		//
		public override string ToString()
		{
			Type t = typeof(T);
            int n = 0;
            lock (m_StorageLock)
			{
				n = m_Storage.Count;
            }
			return string.Format("Created {1} {0} instances. Peak stored was {2}. Current stored {3}. DuplicateAdds = {4}.", t.Name, m_ItemsCreated, m_StoragePeak,n,m_DupeAddAttemps);
		}
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

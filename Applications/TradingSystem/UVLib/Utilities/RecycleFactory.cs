using System;
using System.Collections.Generic;

namespace UV.Lib.Utilities
{
	public class RecycleFactory<T> where T : new()
	{

		#region Members
		// *****************************************************************
		// ****                     Members                             ****
		// *****************************************************************
		//
		// Internal storage
		//		
		protected Queue<T> m_Storage = new Queue<T>();
		protected object m_StorageLock = new object();
		private int m_ItemsCreated = 0;                     // keeps some info about items created
		private int m_StoragePeak = 0;                      // keeps some info about max items in recycle bin.
		#endregion// members


        #region Constructors
        // *****************************************************************
        // ****                    Constructors                         ****
        // *****************************************************************
        //
        //
        //
        // *****************************************
        // ****         Constructor             ****
        // *****************************************
        //
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RecycleFactory() : this(0)
        {

        }
        //
        //
        // *****************************************
        // ****         Constructor             ****
        // *****************************************
        /// <summary>
        /// Constructor that allows initial creation of storage of objects.
        /// </summary>
        /// <param name="initialNumberOfItems"></param>
        public RecycleFactory(int initialNumberOfItems)
        {
            int count = 0;
            lock (m_StorageLock)
            {
                while (count < initialNumberOfItems)
                {
                    T item;
                    item = new T();
                    m_Storage.Enqueue(item);
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
		// ****				Get()				****
        // *****************************************
		/// <summary>
		/// Recover a previously used object from storage, or create a new one.
        /// This version breaks the process into two steps:
        ///     1. Try to get a recycled object from storage, or if that fails
        ///     2. Create a new object.
        /// This way, each step in the process can be overriden by a subclass.
		/// </summary>
		/// <returns>T object ready for use.</returns>
        public virtual T Get()
		{
            T item;
			if (! TryGetFromStorage(out item) )
			{
				item = new T();
				m_ItemsCreated++;
			}
            else if (item is IRecyclable)
            {
                ((IRecyclable)item).Clear();   
            }
			return item;
		}// Get()
        //
        //
        // *********************************************
        // ****     Try Get From Storage()          ****
        // *********************************************
        protected virtual bool TryGetFromStorage(out T item)
        {
            item = default(T);
			lock (m_StorageLock)
			{
				if (m_Storage.Count > 0)
					item = m_Storage.Dequeue();
			}
            return (item != null);
        }//TryGetFromStorage()
        //
        /*
        // Original version.
		public virtual T Get()
		{
			T item = default(T);
			lock (m_StorageLock)
			{
				if (m_Storage.Count > 0)
					item = m_Storage.Dequeue();
			}
			if (item == null)
			{
				item = new T();
				m_ItemsCreated++;
			}
			return item;
		}// Get()
        */ 
		//
		// *********************************************
		// ****				Recycle()				****
        // *********************************************
		/// <summary>
		/// Put previously used item into store for recycling.
		/// </summary>
        /// <param name="item"></param>
		public virtual void Recycle(T item)
		{
			lock (m_StorageLock)
			{
				if ( ! m_Storage.Contains(item))		// temp extra test.
				{
					m_Storage.Enqueue(item);
					m_StoragePeak = Math.Max(m_StoragePeak, m_Storage.Count);
				}
				//else
				//	Log.NewEntry(LogLevel.Error, "PutBarIntoStore: Attempt to save a duplicate bar object!");
			}
		}//RecycleBar()
		//
		//
		//
        // *********************************************
        // ****             ToString()              ****
        // *********************************************
		public override string ToString()
		{
			//Type t = typeof(T);
			return string.Format("Created {0} items. Peak stored was {1}.",m_ItemsCreated.ToString(), m_StoragePeak.ToString());
		}
		//
		#endregion//Public Methods



	}//end class

    public interface IRecyclable
    {
        void Clear();
    }


}

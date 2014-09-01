using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Fills.RejectedFills
{
    using Misty.Lib.IO.Xml;

    public class RecentKeyList : IStringifiable
    {



        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public int MaxStorage = 256;
        private int m_OldestItem = 0;
        private List<object> m_List = null;
        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public RecentKeyList()
        {
            m_List = new List<object>(MaxStorage);
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
        public bool Contains(object key)
        {
            return m_List.Contains(key);
        }
        public void Add(object key)
        {
            if ( ! m_List.Contains(key) )
            {
                if (m_List.Count < MaxStorage)
                    m_List.Add(key);
                else
                {
                    m_List[m_OldestItem] = key;                     // over-write oldest entry.
                    m_OldestItem = (m_OldestItem + 1) % MaxStorage; // point to next-to-oldest entry.
                }
            }
        }
        //
        //
        //
        //
        //
        #endregion//Public Methods

        #region IStringifiable interface
        public string GetAttributes()
        {
            StringBuilder msg = new StringBuilder();
            msg.AppendFormat("MaxStorage={0} ", this.MaxStorage);
            if (m_List.Count > 0)
            {
                msg.AppendFormat("Keys=");
                foreach (object o in m_List)
                    msg.AppendFormat("{0} ", o);
                msg.Remove(msg.Length - 1, 1);      // remove trailing space.
            }

            return msg.ToString();
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            int n;
            foreach (string key in attributes.Keys)
                if (key.Equals("Keys"))
                {
                    string[] keys = attributes[key].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in keys)
                        m_List.Add(s.Trim());
                    MaxStorage = Math.Max(MaxStorage, m_List.Count);
                }
                else if (key.Equals("MaxStorage") && Int32.TryParse(attributes[key], out n))
                    this.MaxStorage = n;                
        }
        public void AddSubElement(IStringifiable subElement)
        {            
        }
        #endregion//IStringifiable

    }
}

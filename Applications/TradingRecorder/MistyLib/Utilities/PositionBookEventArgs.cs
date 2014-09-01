using System;
using System.Collections.Generic;
using System.Text;

namespace Misty.Lib.Utilities
{
    public class PositionBookEventArgs
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public RequestType EventType = RequestType.CurrentState;

        // Data
        public List<int> QtyList = new List<int>();
        public List<double> PriceList = new List<double>();
        //public string SerializedBook = string.Empty;
        //
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
        //
        //
        //
        // ****					ToString()				****
        //
        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("PositionBookEventArg: {0}", EventType.ToString());
            for (int i = 0; i < PriceList.Count; ++i)
                s.AppendFormat(" {0}@{1}", QtyList[i].ToString(), PriceList[i].ToString());
            return s.ToString();
        }//ToString()
        //
        //
        /*
        public string GetAllFills() // use process event args instead
        {
            StringBuilder s = new StringBuilder();           
            for (int i = 0; i < PriceList.Count; ++i)
                s.AppendFormat("{0}@{1}/", QtyList[i].ToString(), PriceList[i].ToString());
            return s.ToString();
        }//ToString()
        */
        //
        #endregion//Public Methods


        #region Enums
        // *****************************************************************
        // ****                     Enums			                     ****
        // *****************************************************************
        //
        public enum RequestType
        {
            //DeSerialize,	// Update the position book using the string.
            CurrentState,	// data reflects the content of the associate position book.
            Add,			// a request to add fills into the book.
            Remove			// a request to remove fills from the book.
        }
        //
        #endregion//Private Methods


    }//end class
}

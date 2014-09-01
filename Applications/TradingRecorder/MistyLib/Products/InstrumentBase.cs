using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace Misty.Lib.Products
{
    public class InstrumentBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        public InstrumentName Name;


        //public string Name = string.Empty;
        public DateTime ExpirationDate = DateTime.MaxValue;                // default is never expires.

        public object ForeignKey = null;                                    // foreign unique key for outside API
        //public object ForeignInfo = null;                                   // other information about this instrument from API.

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public Product Product
        {
            get { return this.Name.Product; }
        }
        public string FullName
        {
            get { return this.Name.FullName; }
        }
        public string SeriesName
        {
            get { return this.Name.SeriesName; }
        }
        //
        #endregion//Properties


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public override string ToString()
        {
            return FullName;
        }
        //
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

    }
}

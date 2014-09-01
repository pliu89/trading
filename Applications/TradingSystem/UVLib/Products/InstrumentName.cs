using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Products
{
    /// <summary>
    /// This represents the name of a UNIQUE instrument.  
    /// Since this is a struct, we can not distiguish between two different instances of this object if
    /// they have the same Product and KeyName.
    /// Why is this useful?
    ///     A: This struct is ideal for use in lookup tables since its will match another key (a different instance)
    ///     as long as the product and KeyName are the same!
    /// </summary>
    public struct InstrumentName : IEquatable<InstrumentName>
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public Product Product;
        public string SeriesName;


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public InstrumentName(Product prod, string seriesUniqueName)
        {
            this.SeriesName = seriesUniqueName;
            this.Product = prod;
        }
        //
        //       
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        /// <summary>
        /// Returns true if this Instrument is the empty instrument.
        /// </summary>
        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(SeriesName) && this.Product.IsEmpty; }
        }
        /// <summary>
        /// Returns true if the Instrument is trivially just its own product.
        /// This happens for Instruments that are alone in their product family, like perhaps a stock.
        /// Or, when we are using the InstrumentName to denote a whole family of instruments.
        /// </summary>
        public bool IsProduct
        {
            get { return string.IsNullOrEmpty(SeriesName) && !this.Product.IsEmpty; }
        }
        //
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        public string FullName
        {
            get { return string.Format("{0} {1}", this.Product.FullName, this.SeriesName); }
        }
        //
        //
        public static string Serialize(InstrumentName instrument)
        {
            return string.Format("{0} {1}",Product.Serialize(instrument.Product),instrument.SeriesName).Trim();
        }
        //
        //
        public static bool TryDeserialize(string serialStr, out InstrumentName newInstrument)
        {                                                   // the empty product
            // Format is:  "product & exchange stuff (productType) Name"
            int n1 = serialStr.LastIndexOf(')');            
            if (n1 > 0 && n1 < serialStr.Length)
            {
                Product product;
                string productString = serialStr.Substring(0, n1 + 1).Trim();        // all stuff including ')'
                if (Product.TryDeserialize(productString, out product))
                {
                    try
                    {
                        if (n1 + 1 < serialStr.Length)
                        {
                            string instrumentName = serialStr.Substring(n1 + 1, serialStr.Length - (n1 + 1)).Trim();
                            newInstrument = new InstrumentName(product, instrumentName);
                        }
                        else
                            newInstrument = new InstrumentName(product, string.Empty);
                        return true;
                    }
                    catch (Exception)
                    {                       
                    }
               }                
            }
            newInstrument = new InstrumentName();
            return false;
        }// TryDeserialize()
        //
        //
        //
        #endregion//public methods



        #region Public Override Methods
        // *****************************************************************
        // ****             Public Override Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        // ****             ToString()              ****
        //
        public override string ToString()
        {
            return string.Format("{0} {1}", this.Product.ToString(),this.SeriesName);
        }//ToString()
        //
        //
        //
        //
        // ****                 Equals                  ****
        //
        public override bool Equals(object obj)
        {
            if (obj is InstrumentName)
            {
                InstrumentName other = (InstrumentName)obj;
                bool isEqual = this.Product.Equals(other.Product) && this.SeriesName.Equals(other.SeriesName);
                return isEqual;
            }
            else
                return false;
        }
        public bool Equals(InstrumentName other)
        {
            bool isEqual = this.Product.Equals(other.Product) && this.SeriesName.Equals(other.SeriesName);
            return isEqual;
        }
        public static bool operator ==(InstrumentName a, InstrumentName b)
        {
            //if (System.Object.ReferenceEquals(a, b))
            //    return true;
            if ((object)a == null)
                return false;
            return a.Equals(b);
        }
        public static bool operator !=(InstrumentName a, InstrumentName b)
        {
            return !(a == b);
        }
        public override int GetHashCode()
        {
            return this.FullName.GetHashCode();
        }
        //
        //

        //
        //
        #endregion//Public Methods



    }
}

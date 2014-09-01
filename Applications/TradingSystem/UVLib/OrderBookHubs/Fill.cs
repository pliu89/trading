using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using UV.Lib.Utilities;
using UV.Lib.IO.Xml;             // for IStringifiable

namespace UV.Lib.OrderBookHubs
{
    public class Fill : IStringifiable , ICloneable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Basic fill information    
        public int Qty = 0;                                 // signed quantity.
        public double Price;
        public DateTime LocalTime;                          // time fill was received by local application.
        public DateTime ExchangeTime;                       // time the exchange filled us (their time stamp).

        #endregion// members

        #region Creators
        // *********************************************
        // ****             Create()                ****
        // *********************************************
        public Fill()
        {
        }
        protected Fill(int qty, double price, DateTime localTime, DateTime exchangeTime)
        {
            this.Qty = qty;
            this.Price = price;
            this.LocalTime = localTime;
            this.ExchangeTime = exchangeTime;
        }
        public static Fill Create()
        {
           return new Fill();
        }// Create()
        //
        public static Fill Create(Fill fillToCopy)
        {
            // copy each value to this.
            Fill newFill = new Fill();
            newFill.Qty = fillToCopy.Qty;
            newFill.Price = fillToCopy.Price;
            newFill.LocalTime = fillToCopy.LocalTime;
            newFill.ExchangeTime = fillToCopy.ExchangeTime;

            return newFill;
        }// Create()
        //
        public static Fill Create(int qty, double price, DateTime localTime, DateTime exchangeTime)
        {
            // copy each value to this.
            Fill newFill = new Fill();
            newFill.Qty = qty;
            newFill.Price = price;
            newFill.LocalTime = localTime;
            newFill.ExchangeTime = exchangeTime;

            return newFill;
        }// Create()

        //
        //
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public override string ToString()
        {
            return string.Format("{0} @ {1} {2}",this.Qty.ToString("+0;-0;0"), this.Price.ToString(),ExchangeTime.ToString(Strings.FormatTime));
            //return string.Format("{0} @ {1}", this.Qty.ToString("+0;-0;0"), this.Price.ToString());
        }        
        //
        public bool IsSameAs(Fill other)
        {
            if (!LocalTime.Equals(other.LocalTime))
                return false;
            if (!ExchangeTime.Equals(other.ExchangeTime))
                return false;
            if (!this.Price.Equals(other.Price))
                return false;
            if (!this.Qty.Equals(other.Qty))
                return false;
            return true;
        }// IsSameAs()
        //
        // 
        //
        //
        //
        //
        #endregion//Public Methods

        #region IStringifiable Implementation
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        string IStringifiable.GetAttributes()
        {
            return string.Format("Qty={0} Price={1} LocalTime={2} ExchangeTime={3}", Qty, Price,
                LocalTime.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"), ExchangeTime.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
        }
        List<IStringifiable> IStringifiable.GetElements(){  return null;}
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)//, ref Dictionary<string, string> unusedAttributes)
        {
            double x;
            int i;
            DateTime dt;
            foreach (string key in attributes.Keys)
                if (key.Equals("Qty") && Int32.TryParse(attributes[key], out i) )
                    this.Qty = i;
                else if (key.Equals("Price") && Double.TryParse(attributes[key], out x) )
                    this.Price = x;
                else if (key.Equals("LocalTime") && DateTime.TryParse(attributes[key], out dt) )
                    this.LocalTime = dt;
                else if (key.Equals("ExchangeTime") && DateTime.TryParse(attributes[key], out dt) )
                    this.ExchangeTime = dt;
                //else 
                //    unusedAttributes.Add(key, attributes[key]);
        }
        void IStringifiable.AddSubElement(IStringifiable subElement) {   }
        //
        //
        #endregion // IStringifiable

        #region ICloneable implementation
        //
        //
        object ICloneable.Clone()
        {
            return this.Clone();
        }
        /// <summary>
        /// Shallow clone method for fill object
        /// </summary>
        /// <returns>shallowly cloned Fill Object</returns>
        public Fill Clone()
        {
            return (Fill)this.MemberwiseClone();
        }
        //
        #endregion ICloneable implementation

    }
}

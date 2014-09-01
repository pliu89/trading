using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.MarketHubs
{
    using UV.Lib.Products;
    using UV.Lib.IO.Xml;

    /// <summary>
    /// This very elementary object is simply a nice way to store an instrument name
    /// with its price multiplier and weight.  Its used by PricingEngines to define its
    /// implied multi-legged market.  But, it can be used by other objects to organize 
    /// multiple instruments.
    /// </summary>
    public class PriceLeg : IStringifiable, ICloneable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Definition of a leg
        //
        public InstrumentName InstrumentName;
        public double PriceMultiplier = 1.0;
        public double Weight = 1.0;

        // Internal market variables
        public bool IsPriceEngineLeg = true;    // Should this PriceLeg be interpreted by PricingEngine base class as part of its implied market.
        public int MarketID = -1;               // Id number of instrument in market book.

        // User defined tags.                   // Ignored by UV base classes, including the PriceEngine base class.

        public string UserTag = string.Empty;   // Users can put whatever they want in here, ignored by system.


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public PriceLeg()
        {
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
        //
        //
        //
        //
        //
        // ****         ToString()          ****
        //
        public override string ToString()
        {
            return string.Format("Leg: {0} {1} {2}", this.InstrumentName, this.PriceMultiplier, this.Weight);
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


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        //
        //
        public string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("InstrumentName={0}", InstrumentName.Serialize(this.InstrumentName));
            s.AppendFormat(" PriceMultiplier={0}", this.PriceMultiplier);
            s.AppendFormat(" Weight={0}", this.Weight);
            s.AppendFormat(" IsPriceEngineLeg={0}", this.IsPriceEngineLeg);
            if (!string.IsNullOrEmpty(this.UserTag))
            {
                if (this.UserTag.Contains("="))
                    throw new Exception("PriceLeg UserTag contains illegal characters.");
                s.AppendFormat(" UserTag={0}", this.UserTag);
            }
            return s.ToString();
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            double x;
            bool b;
            InstrumentName newInstr;
            foreach (KeyValuePair<string, string> attr in attributes)
            {
                if (attr.Key == "InstrumentName" && InstrumentName.TryDeserialize(attr.Value, out newInstr))
                    this.InstrumentName = newInstr;
                else if (attr.Key == "PriceMultiplier" && double.TryParse(attr.Value, out x))
                    this.PriceMultiplier = x;
                else if (attr.Key == "Weight" && double.TryParse(attr.Value, out x))
                    this.Weight = x;
                else if (attr.Key == "IsPriceEngineLeg" && bool.TryParse(attr.Value,out b))
                    this.IsPriceEngineLeg = b;
                else if (attr.Key == "UserTag")
                    this.UserTag = attr.Value;
            }
        }
        public void AddSubElement(IStringifiable subElement)
        {
        }
        //
        //
        #endregion//IStringifiable


        #region ICloneable 
        // *************************************************
        // ****             ICloneable                  ****
        // *************************************************
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
        public PriceLeg Clone()
        {
            return (PriceLeg)this.MemberwiseClone();
        }
        //
        #endregion // ICloneable


    }//end class
}

using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Strategies.Engines
{
    using UV.Lib.Products;
    using UV.Lib.IO.Xml;

    /// <summary>
    /// </summary>
    public class PriceLeg : IStringifiable
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

        // Internal variables
        public int MarketID = -1;                   // Id number of instrument in market book.
        public bool IsLegDefined = false;           // true, after mkt acknowledges leg has market. Remains true then on.

  


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
            s.AppendFormat("InstrumentName={0}", this.InstrumentName);
            s.AppendFormat("PriceMultiplier={0}", this.PriceMultiplier);
            s.AppendFormat("Weight={0}", this.Weight);
            return s.ToString();
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            double x;
            InstrumentName newInstr;
            foreach (KeyValuePair<string,string> attr in attributes)
            {
                if (attr.Key == "InstrumentName" && InstrumentName.TryDeserialize(attr.Value, out newInstr))
                    this.InstrumentName = newInstr;
                else if (attr.Key == "PriceMultiplier" && double.TryParse(attr.Value, out x))
                    this.PriceMultiplier = x;
                else if (attr.Key == "Weight" && double.TryParse(attr.Value, out x))
                    this.Weight = x;
            }
        }
        public void AddSubElement(IStringifiable subElement)
        {            
        }
        //
        //
        #endregion//IStringifiable

    }//end class
}

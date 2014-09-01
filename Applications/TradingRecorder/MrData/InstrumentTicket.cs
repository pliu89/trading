using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MrData
{
    using Misty.Lib.Products;
    using Misty.Lib.IO.Xml;

    /// <summary>
    /// Simple controls telling MrData how to record this instrument.
    /// </summary>
    public class InstrumentTicket: IStringifiable
    {
        public InstrumentName InstrumentName;
        public int LeadingContracts = 1;                        // number of leading contracts to load (if InstrumentName.IsProduct = true)




        #region IStringifiable
        // *********************************************************
        // ****                 IStringifiable                  ****
        // *********************************************************
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("InstrumentName={0}", InstrumentName.Serialize(this.InstrumentName));
            s.AppendFormat("Leading={0}", this.LeadingContracts);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            InstrumentName instr;
            int n;
            foreach (string key in attributes.Keys)
            {
                if (key.Equals("InstrumentName") && InstrumentName.TryDeserialize(attributes[key], out instr))
                    this.InstrumentName = instr;
                else if (key.Equals("Leading") && int.TryParse(attributes[key], out n))
                    this.LeadingContracts = n;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {            
        }
        #endregion//IStringifiable



    }
}

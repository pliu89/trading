using System;
using System.Collections.Generic;
using System.Text;

namespace UV.TTServices.Markets
{

    using TradingTechnologies.TTAPI;
    using UV.Lib.Products;
    using UV.Lib.IO.Xml;

    public class InstrumentMapEntry : IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        public InstrumentName Name;
        public InstrumentKey Key;


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public InstrumentMapEntry()
        {
        }
        public InstrumentMapEntry(InstrumentName name, InstrumentKey key)
        {
            this.Name = name;
            this.Key = key;
        }
        //
        //       
        #endregion//Constructors



        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public override string ToString()
        {
            return string.Format("{0} <==> {1}",this.Name, this.Key);
        }
        //
        //
        //
        //
        //
        #endregion//Public Methods



        #region IStringifiable
        public string GetAttributes()
        {
            return string.Format("Name={0} Key={1}", InstrumentName.Serialize(this.Name), Key.ToString());
        }
        public List<IStringifiable> GetElements()
        {
            return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            InstrumentName name;
            InstrumentKey ttkey;
            foreach (string key in attributes.Keys)
                if (key.Equals("Name") && InstrumentName.TryDeserialize(attributes["Name"],out name))
                    this.Name = name;
                else if (key.Equals("Key") && TTConvertNew.TryCreateInstrumentKey(attributes["Key"], out ttkey))
                    this.Key = ttkey;
        }
        public void AddSubElement(IStringifiable subElement)
        {            
        }
        #endregion//IStringifiable
    }
}

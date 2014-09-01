using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Fills
{

    using Misty.Lib.IO.Xml;



    public class FillEventArgs : EventArgs, IStringifiable
    {
        //
        // Members
        //
        public FillType Type;                                               // type of fill
        public TradingTechnologies.TTAPI.InstrumentKey TTInstrumentKey;     // foreign API code used to id instrument assoc with fill.
        public string FillKey = string.Empty;                               // foreign generated unique code ID for this fill event.
        public string AccountID = string.Empty;
        public Misty.Lib.OrderHubs.Fill Fill;                               // the fill

        //
        // Constructors
        // 
        public FillEventArgs() { }
        public FillEventArgs(TradingTechnologies.TTAPI.InstrumentKey ttKey, FillType fillType, Misty.Lib.OrderHubs.Fill theFill)
        {
            this.TTInstrumentKey = ttKey;
            this.Type = fillType;
            this.Fill = theFill;
        }
        //
        // Public Methods
        // 
        public override string ToString()
        {
            return string.Format("[{2} {1} {0}]", Fill.ToString(), Type, TTInstrumentKey);
        }
        /// <summary>
        /// True if both instances seem to refer to the same fill event.
        /// </summary>
        /// <returns></returns>
        public bool IsSameAs(FillEventArgs other)
        {
            if (other == null)
                return false;
            if (string.IsNullOrEmpty(this.FillKey) || string.IsNullOrEmpty(other.FillKey))
            {   // One or both are missing a unique fill key - bummer!
                if (!this.TTInstrumentKey.Equals(other.TTInstrumentKey))
                    return false;
                if (this.Type != other.Type)
                    return false;
                if (!this.Fill.IsSameAs(other.Fill))
                    return false;
                // They seem to be same!
                return true;
            }
            else
                return this.FillKey.Equals(other.FillKey);
        }



        #region IStringifiable
        // *********************************************************************************
        // ****                             IStringifiable                              ****
        // *********************************************************************************
        //
        //
        public string GetAttributes()
        {
            return string.Format("Type={0} InstrumentKey={1} FillKey={3} {2}", this.Type, this.TTInstrumentKey, ((IStringifiable)this.Fill).GetAttributes(), this.FillKey);
        }
        public List<IStringifiable> GetElements() { return null; }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            this.Fill = new Misty.Lib.OrderHubs.Fill();
            ((IStringifiable)this.Fill).SetAttributes(attributes);

            FillType type;
            TradingTechnologies.TTAPI.InstrumentKey ttInstrumentKey;
            foreach (string key in attributes.Keys)
            {
                if (key == "Type" && Enum.TryParse<FillType>(attributes[key], out type))
                    this.Type = type;
                else if (key == "InstrumentKey" && TTConvert.TryCreateInstrumentKey(attributes[key], out ttInstrumentKey))
                    this.TTInstrumentKey = ttInstrumentKey;
                else if (key == "FillKey")
                    this.FillKey = attributes[key];
            }
        }
        public void AddSubElement(IStringifiable subElement) { }
        #endregion // IStringifiable



    }
}

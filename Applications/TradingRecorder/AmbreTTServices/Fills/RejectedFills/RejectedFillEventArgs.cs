using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Fills.RejectedFills
{
    using Ambre.TTServices.Fills;
    using Misty.Lib.IO.Xml;
    using Misty.Lib.Utilities;

    using Misty.Lib.Products;

    public class RejectedFillEventArgs : EventArgs , IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private InstrumentName m_Name;
        public RejectionReason Reason = RejectionReason.Unknown;
        public string Message = string.Empty;
        public FillEventArgs OriginalFillEventArg;

        

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public RejectedFillEventArgs() { }                                      // default constructor for IStringifiable
        public RejectedFillEventArgs(InstrumentName name, FillEventArgs rejectedFill, RejectionReason reason, string rejectionMessage)
        {
            this.m_Name = name;
            this.Reason = reason;
            this.Message = rejectionMessage;
            this.OriginalFillEventArg = rejectedFill;

        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                    Properties                           ****
        // *****************************************************************
        //
        public InstrumentName Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }
        public string TT_InstrumentKey
        {
            get { return OriginalFillEventArg.TTInstrumentKey.ToString(); }
        }
        //
        public string FillQtyPrice
        {
            get { return string.Format("{0} @ {1}", OriginalFillEventArg.Fill.Qty, OriginalFillEventArg.Fill.Price); }
        }
        public string FillExchangeTime
        {
            get { return string.Format("{0}", OriginalFillEventArg.Fill.ExchangeTime.ToString(Strings.FormatDateTimeZone)); }
        }
        public string FillLocalTime
        {
            get { return string.Format("{0}", OriginalFillEventArg.Fill.LocalTime.ToString(Strings.FormatDateTimeZone)); }
        }
        //
        //
        public RejectionReason RejectionReason
        {
            get { return this.Reason; }
        }

        public string RejectionMessage
        {
            get { return this.Message; }
        }
        //
        //
        #endregion


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        public override string ToString()
        {
            if (OriginalFillEventArg != null)
                return string.Format("{0} {1} Fill={2}",this.Reason,this.Message,this.OriginalFillEventArg);
            else
                return string.Format("{0} {1} Fill=none",this.Reason,this.Message);
        }// ToString()
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region IStringifiable Implementation 
        // *****************************************************************
        // ****                    IStringifiable                       ****
        // *****************************************************************
        //
        public string GetAttributes()
        {
            return string.Format("InstrumentName={2} Reason={0} Message={1}",this.Reason,this.Message,InstrumentName.Serialize(m_Name));
        }
        public List<IStringifiable> GetElements()
        {
            List<IStringifiable> elements = new List<IStringifiable>();
            elements.Add(this.OriginalFillEventArg);
            return elements;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            RejectionReason reason;
            InstrumentName name;
            foreach (string aKey in attributes.Keys)
            {
                if (aKey.Equals("Reason") && Enum.TryParse<RejectionReason>(attributes[aKey], out reason))
                    this.Reason = reason;
                else if (aKey.Equals("Message"))
                    this.Message = attributes[aKey];
                else if (aKey.Equals("InstrumentName") && InstrumentName.TryDeserialize(attributes[aKey], out name))
                    this.m_Name = name;
            }
        }
        public void AddSubElement(IStringifiable subElement)
        {
            Type type = subElement.GetType();
            if (type == typeof(FillEventArgs))
                this.OriginalFillEventArg = (FillEventArgs)subElement;
        }
        //
        #endregion // IStringifiable

    }
}

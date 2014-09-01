using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices.Fills
{
    using System.Threading;

    using Misty.Lib.Products;
    using Misty.Lib.OrderHubs;
    using Misty.Lib.IO.Xml;
    using Misty.Lib.Utilities;

    using TradingTechnologies.TTAPI;


    public class BookLifo : FillBookLifo, IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Services
        //
        private ReaderWriterLockSlim m_Lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        //
        // Fill rejection variables
        //
        private RejectedFills.RecentKeyList m_RecentKeys = new RejectedFills.RecentKeyList();
        private List<RejectedFills.RejectedFillEventArgs> m_RejectedFills = new List<RejectedFills.RejectedFillEventArgs>();
        private int DaysToStoreRejections = 2;
        //private TimeSpan MaxAllowedFillLatency = new TimeSpan(0, 30, 0);      // minutes

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public BookLifo() { }                                                 // needed for IStringifiable 
        //
        public BookLifo(double smallestFillPriceIncrement, double dollarAmtOfSmallestPriceIncrement, InstrumentName name)
            : base(smallestFillPriceIncrement, dollarAmtOfSmallestPriceIncrement, name)
        {

        }
        //
        //       
        #endregion//Constructors


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        public new ReaderWriterLockSlim Lock
        {
            get { return m_Lock; }
        }
        public new InstrumentName Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Try Add()                           ****
        // *****************************************************************
        /// <summary>
        /// A fill event is presented to the book for acceptance.  The fill event is checked for validity, 
        /// and if good added to the book. If not, a rejection event argument is created for caller.
        /// </summary>
        /// <param name="eventArg">fill event argument</param>
        /// <param name="rejection">rejection event create if false is returned</param>
        /// <returns>true if fill can be accepted, false if not.</returns>
        public new bool TryAdd(FillEventArgs eventArg, out RejectedFills.RejectedFillEventArgs rejection)          // Add a key workd override.
        {
            rejection = null;
            InstrumentKey key = eventArg.TTInstrumentKey;

            if (this.IsFillNew(eventArg, out rejection))
            {
                // Add fill to book
                Misty.Lib.OrderHubs.Fill aFill = eventArg.Fill;
                base.Add(aFill);
                // Update fill identification tables
                if (m_RecentKeys == null)
                    m_RecentKeys = new RejectedFills.RecentKeyList();
                if (!string.IsNullOrEmpty(eventArg.FillKey))
                    m_RecentKeys.Add(eventArg.FillKey);
                // Process rejections that are now accepted.
                if (eventArg.Type == FillType.UserAdjustment)
                {
                    int n = 0;
                    while (n < m_RejectedFills.Count)
                    {
                        if (m_RejectedFills[n].OriginalFillEventArg.IsSameAs(eventArg))
                        {
                            m_RejectedFills.RemoveAt(n);
                            break;
                        }
                        n++;
                    }
                }

                return true;
            }
            else
                return false;


            /*
            if (eventArg.Type == FillType.Historic || eventArg.Type == FillType.InitialPosition)
            {   // If a fill is historic or initial, it needs to be verified that we have not already
                // incorporated this fill into our book in the past.  
                // Other fills are blindly accepted since they should not have been seen before.
                if (this.IsFillNew(eventArg, out rejection))
                {
                    base.Add(aFill);
                    if (m_RecentKeys == null)
                        m_RecentKeys = new RejectedFills.RecentKeyList();
                    m_RecentKeys.Add(eventArg.FillKey);
                }
                else
                {
                    return false;
                }
            }
            else
            {   // Other kinds of fills; including "New" and "UserAdjustments" are always accepted as they 
                // come in.
                base.Add(aFill);
                if (m_RecentKeys == null)
                    m_RecentKeys = new RejectedFills.RecentKeyList();
                m_RecentKeys.Add(eventArg.FillKey);
            }         
            return true;
            */
        } // TryAdd()
        //
        //
        //
        // *************************************************************
        // ****                     Is Fill New()                   ****
        // *************************************************************
        /// <summary>
        /// This method examines a fill event and tries to determine whether or not the fill 
        /// is known to this fill book, or is new.  If its not new, an explanation is returned as 
        /// an RejectedFillEventArg.
        /// </summary>
        /// <param name="fillEventArgs"></param>
        /// <param name="rejectedEventArgs">Rejected fill: null when true, but can be null/not null when false</param>
        /// <returns>True if this fill seems to be new to our book.</returns>
        public new bool IsFillNew(FillEventArgs fillEventArgs, out RejectedFills.RejectedFillEventArgs rejectedEventArgs)
        {
            rejectedEventArgs = null;

            // Test purpose:(This should never appeared in the production code.
            //DateTime localAcceptDateTime = DateTime.Now.Date.AddHours(12);
            //if (fillEventArgs.Fill.LocalTime <= localAcceptDateTime)
            //{
            //    string msg = string.Format("Test purpose date time accepted only after {0}.", localAcceptDateTime);
            //    rejectedEventArgs = TriggerRejectionEvent(fillEventArgs, RejectedFills.RejectionReason.ExcessiveLateness, msg);
            //    return false;
            //}

            // Accept user adjusted fills always.
            if (fillEventArgs.Type == FillType.UserAdjustment)
                return true;

            // Accept adjusted fills always.
            if (fillEventArgs.Type == FillType.Adjustment)
                return true;

            // Exchange time stamp events.  (Note some fills like UserAdjusted fills don't have Exchange time stamps.)
            if (fillEventArgs.Fill.ExchangeTime.CompareTo(DateTime.MinValue) > 0)
            {
                // Reject if we have actually seen fill key before. We don't do this test right off because its slow.
                if (fillEventArgs.FillKey != null && m_RecentKeys.Contains(fillEventArgs.FillKey))
                {
                    string msg = string.Format("{0} already in book {1}.", fillEventArgs.FillKey, this.Name);
                    rejectedEventArgs = TriggerRejectionEvent(fillEventArgs, RejectedFills.RejectionReason.DuplicateKey, msg);
                    return false;
                }

                TimeSpan ts = (fillEventArgs.Fill.ExchangeTime).Subtract(this.ExchangeTimeLast);    // time since last fill.
                if (ts.TotalMilliseconds >= 0)                           // fills after the last fill are always reasonable.
                    return true;
                else
                {
                    // Reject if fill is old.  That is, our book has more recent fills.
                    string msg = string.Format("Bad fill time {0}. Book last {1}. Earlier by {2} hours.", fillEventArgs.Fill.ExchangeTime.ToString(Strings.FormatDateTimeZone), base.ExchangeTimeLast.ToString(Strings.FormatDateTimeZone), ts.TotalHours.ToString("0.0"));
                    rejectedEventArgs = TriggerRejectionEvent(fillEventArgs, RejectedFills.RejectionReason.ExcessiveLateness, msg);
                    return false;
                }
            }
            else
            {
                // Reject if fill does not have valid exchange time.
                string msg = string.Format("Invalid fill time {0}.", fillEventArgs.Fill.ExchangeTime.ToString(Strings.FormatDateTimeZone));
                rejectedEventArgs = TriggerRejectionEvent(fillEventArgs, RejectedFills.RejectionReason.ExcessiveLateness, msg);
                return false;
            }

            // Can't find any reason to reject.
            //return true;
        }// IsFillAcceptable()
        //
        //
        //
        //
        //
        public void GetRejectedFills(ref List<RejectedFills.RejectedFillEventArgs> rejectedFills)
        {
            if (m_RejectedFills == null) { return; }
            rejectedFills.AddRange(m_RejectedFills);
        }
        //
        //
        //
        //
        public bool IsFillExistByExchangeTime(DateTime exchangeTime)
        {
            foreach (Misty.Lib.OrderHubs.Fill mistyFill in m_FillsOpen)
                if (mistyFill.ExchangeTime == exchangeTime)
                    return true;
            return false;
        }//IsFillExistByExchangeTime()
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        private RejectedFills.RejectedFillEventArgs TriggerRejectionEvent(FillEventArgs rejectedFill, RejectedFills.RejectionReason reason, string rejectionMessage)
        {
            // Make sure this rejection is new.
            bool isDuplicate = false;
            int n = 0;
            while ((!isDuplicate) && n < m_RejectedFills.Count)
            {
                isDuplicate = rejectedFill.IsSameAs(m_RejectedFills[n].OriginalFillEventArg);
                n++;
            }

            RejectedFills.RejectedFillEventArgs rejectedEventArgs = new RejectedFills.RejectedFillEventArgs(this.Name, rejectedFill, reason, rejectionMessage);
            if (!isDuplicate)
            {   // Don't event if this is a duplicate rejection (previously rejected that is).
                //RejectedFills.RejectedFillEventArgs rejectedEventArgs = new RejectedFills.RejectedFillEventArgs(this.Name, rejectedFill, reason, rejectionMessage);
                this.m_RejectedFills.Add(rejectedEventArgs);    // The storing of this rejection notification, is like triggering event.
                return rejectedEventArgs;
            }
            else
                return rejectedEventArgs;                       // this is a rejection that we have already seen (in previous run, probably).
        }
        //
        #endregion//Private Methods


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        //public new string GetAttributes()
        public new List<IStringifiable> GetElements()
        {
            List<IStringifiable> elements = base.GetElements();             // includes the fills
            if (m_RecentKeys != null)
                elements.Add(m_RecentKeys);
            if (m_RejectedFills != null && m_RejectedFills.Count > 0)       // include rejections
            {
                DateTime now = DateTime.Now;
                foreach (RejectedFills.RejectedFillEventArgs rejectedFill in m_RejectedFills)
                {
                    DateTime localFillTime = rejectedFill.OriginalFillEventArg.Fill.LocalTime;
                    int n = DaysToStoreRejections;                          // after this many (business) days, we dump rejection notices.
                    while (localFillTime.DayOfWeek == DayOfWeek.Saturday || localFillTime.DayOfWeek == DayOfWeek.Sunday || (n--) > 0)
                        localFillTime = localFillTime.AddDays(+1.0);        // push this n days ahead
                    if (now.CompareTo(localFillTime) < 0)
                        elements.Add(rejectedFill);                         // keep this rejection for some time more.
                }
            }
            return elements;
        }
        //public new void SetAttributes(Dictionary<string, string> attributes, ref Dictionary<string, string> unusedAttributes)
        public new void AddSubElement(IStringifiable subElement)
        {
            Type type = subElement.GetType();
            if (type == typeof(RejectedFills.RecentKeyList))
                m_RecentKeys = (RejectedFills.RecentKeyList)subElement;
            else if (type == typeof(RejectedFills.RejectedFillEventArgs))
            {
                RejectedFills.RejectedFillEventArgs reject = (RejectedFills.RejectedFillEventArgs)subElement;
                bool isDuplicate = false;
                int n = 0;
                while ((!isDuplicate) && (n < m_RejectedFills.Count))
                {
                    isDuplicate = reject.OriginalFillEventArg.IsSameAs(m_RejectedFills[n].OriginalFillEventArg);
                    n++;
                }
                if (!isDuplicate)
                {
                    reject.Name = this.Name;// Temp- to add information
                    m_RejectedFills.Add(reject);
                }
            }
            else
                base.AddSubElement(subElement);
        }
        #endregion//IStringifiable
    }
}

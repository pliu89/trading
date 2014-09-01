using System;
using System.Collections.Generic;
using System.Text;

namespace UV.Lib.Products
{
    public class InstrumentDetails
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //      
        public InstrumentName InstrumentName;
        public readonly string Currency;                            // ISO string for currency
        public readonly double TickSize;                            // Smallest price change possible in isntrumnet
        public readonly double TickValue;                           // Currency value of a TickSize change
        /////////////////////// Explanation of Multiplier ////////////////////
        //      Multiplier * currenct price = notional value of contract    //
        //      Multiplier * TickSize = Tick Value                          //
        //      Multiplier = TickValue / TickSize                           //
        //////////////////////////////////////////////////////////////////////
        public readonly double Multiplier;                          
        public readonly double ExecutableTickSize;                  // For spreads, sometimes the legs can come across in smaller tick values than the spread.
        //public readonly double Unit;                                // unit: by how much value of the contract changes in unit of currency if price changes by 1 (andreism)
        // standard instruments have a single expiry.  This is meant to flag strips, and packs that some exchanges include as "futures" which makes it very confusing.
        public readonly bool isStandard = true;                           
        public readonly DateTime ExpirationDate;
        public readonly ProductTypes Type;

        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        /// <summary>
        /// Create Instrument Details for a given instrument.
        /// </summary>
        /// <param name="instr"></param>
        /// <param name="currency"></param>
        /// <param name="tickSize">Smallest price change possible of intsrument</param>
        /// <param name="multiplier">The multiplier * current price = notional value of contract.  (Multiplier = TickValue / TickSize)</param>
        /// <param name="executableTickSize"> Smallest possible increment to be filled in</param>
        /// <param name="expires"></param>
        /// <param name="type"></param>
        public InstrumentDetails(InstrumentName instr, string currency, double tickSize, double executableTickSize, double multiplier, DateTime expires, ProductTypes type)
        {
            this.InstrumentName = instr;
            this.Currency = currency;               
            this.TickSize = tickSize;                       // smallest increment price can fluctuate by
            this.ExecutableTickSize = executableTickSize;
            this.Multiplier = multiplier;                   // See above for explanation of multipilier
            this.ExpirationDate = expires;          
            this.Type = type;               
            this.TickValue = multiplier * tickSize;         // Native Currency Value of a Tick
            if (type == ProductTypes.Future | type == ProductTypes.Spread)
                this.isStandard = IsStandardInstrument(instr);
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
        public override string ToString()
        {
            return this.InstrumentName.ToString();
        }
        //
        //
        //
        //
        #endregion//Public Methods

        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Called for futures only where we expect the series name to have some sort of date in it.
        /// This is mainly a way of filtering out instrument like "e-Brent Cal 14" and "e-Brent Q1"
        /// which are getting denoted as futures but have multiple instruments within.
        /// </summary>
        /// <param name="instr"></param>
        /// <returns></returns>
        private bool IsStandardInstrument(InstrumentName instr)
        {
            DateTime dateTime;
            if(DateTime.TryParse(instr.SeriesName, out dateTime))
                return true;
            return false;
        }

        #endregion // Private Methods

    }
}

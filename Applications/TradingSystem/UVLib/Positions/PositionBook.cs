using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Positions
{
    using UV.Lib.Products;
    using UV.Lib.Fills;
    using UV.Lib.Utilities;
    /// <summary>
    /// This book is meant to organize positions by price and side for a single instrument.
    /// Positions are meant to be added and removed as needed.  This is different than the standard fill book
    /// because it will not aggregate positions from different prices. This allows the user full control of which positions
    /// cancel. So if a user desires to view the positions seperately they are able to do so.  The only exception to this
    /// is position from the same price, those are viewed as cancelling each other automatically.
    /// 
    /// Price's are oganized by integerized IPrice.  The book can accept fills but will extract qty and price and not store 
    /// the actual fill, to allow for easier seperation of fills
    /// /// </summary>
    public class PositionBook
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //     
        public InstrumentName m_Instrument;
        public double m_TickSize;
        private Dictionary<int, int> m_IPriceToPosition = new Dictionary<int, int>();                               // map IPrice to current position to be scratched
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public PositionBook(InstrumentName instrName, double tickSize)
        {
            m_Instrument = instrName;
            m_TickSize = tickSize;
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
        // **************************************************
        // ****               AddPosition()              ****
        // **************************************************
        /// <summary>
        /// Caller would like to add a position to the position book for this instrument
        /// </summary>
        /// <param name="afill"></param>
        /// <returns>Net Position at IPrice</returns>
        public int AddPosition(Fill afill)
        {
            int iPrice = (int)(afill.Price / m_TickSize);                                   // convert to iPrice
            return this.AddPosition(iPrice, afill.Qty);
        }
        //
        //
        //
        // *********************************************************
        // ****                     AddPosition                 ****
        // *********************************************************
        /// <summary>
        /// Caller would like to add a position.
        /// </summary>
        /// <param name="iPrice"></param>
        /// <param name="qty"></param>
        /// <returns>Net Position At Price</returns>
        public int AddPosition(int iPrice, int qty)
        {
            int previousPos;
            if (m_IPriceToPosition.TryGetValue(iPrice, out previousPos))
            {
                previousPos += qty;
                m_IPriceToPosition[iPrice] = previousPos;
                return previousPos;
            }
            else
                m_IPriceToPosition[iPrice] = qty;
            return qty;
        }
        //
        //
        //
        // **************************************************
        // ****              GetPositionAtIPrice         ****
        // **************************************************
        /// <summary>
        /// Caller would like to get the current position at a IPrice
        /// </summary>
        /// <param name="iPrice"></param>
        /// <returns></returns>
        public int GetPositionAtIPrice(int iPrice)
        {
            int posAtIPrice;
            if (m_IPriceToPosition.TryGetValue(iPrice, out posAtIPrice))
                return posAtIPrice;
            else
                return 0;
        }
        //
        //
        /// <summary>
        /// This overload allows for a caller to pass in a fill to use the 
        /// price in the fill to find the current position at the fill price.
        /// NOTE: This does nothing with the qty of the fill
        /// This is meant to be used typically to find out the position prior 
        /// to adding the fill.
        /// </summary>
        /// <param name="fill"></param>
        /// <returns></returns>
        public int GetPositionAtIPrice(Fill fill)
        {
            int iPrice = (int)(fill.Price / m_TickSize);
            return GetPositionAtIPrice(iPrice);
        }
        #endregion//Public Methods


        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods


        #region no Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        //
        #endregion//Event Handlers

    }
}

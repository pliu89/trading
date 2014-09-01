using System;
using System.Collections.Generic;
using System.Text;

namespace UV.TTServices
{
    using UV.Lib.Products;
    using UV.Lib.Utilities;
    using UVProduct = UV.Lib.Products.Product;
    using UVProductTypes = UV.Lib.Products.ProductTypes;
    using UVInstrDetails = UV.Lib.Products.InstrumentDetails;
    using UVOrderType = UV.Lib.OrderBooks.OrderType;
    using UVOrderTIF = UV.Lib.OrderBooks.OrderTIF;
    using UVOrderState = UV.Lib.OrderBooks.OrderState;
    using UVOrder = UV.Lib.OrderBooks.Order;

    //using UV.TTServices.OrderBooks;

    using TradingTechnologies.TTAPI;
    using TTProduct = TradingTechnologies.TTAPI.Product;
    using TTProductKey = TradingTechnologies.TTAPI.ProductKey;
    using TTInstrumentDetails = TradingTechnologies.TTAPI.InstrumentDetails;
    using TradingTechnologies.TTAPI.Tradebook;
    using TTOrderType = TradingTechnologies.TTAPI.OrderType;

    /// <summary>
    /// Static functions that convert TT Products and instruments to UV products and instruments.
    /// </summary>
    public static class TTConvertNew
    {

        #region Product Conversions
        // *****************************************************************
        // ****              Product Conversions Methods                ****
        // *****************************************************************
        //
        //
        public static bool TryConvert(TTProduct ttProduct, out UVProduct UVProduct)
        {
            //UVProduct = null;
            ProductTypes productType = GetProductType(ttProduct.Type);
            if (productType == ProductTypes.Unknown)
            {
                UVProduct = new UVProduct();
                return false;
            }

            //UVProduct = new Product(ttProduct.Market.Name, ttProduct.Name, productType, ttProduct.Market.Name);
            UVProduct = new UVProduct(ttProduct.Market.Name, ttProduct.Name, productType);

            return true;
        }//TryConvert()       
        //
        /// <summary>
        /// Tries to naively create a TT PRoduct Key based on a UVProduct.
        /// </summary>
        /// <returns></returns>
        public static bool TryConvert(UVProduct UVProduct, out TTProductKey ttProduct)
        {
            TradingTechnologies.TTAPI.ProductType ttType = GetProductType(UVProduct.Type);
            ttProduct = new TTProductKey(UVProduct.Exchange, ttType, UVProduct.ProductName);
            return true;
        }//TryConvert()       
        //
        //
        public static bool TryConvert(TTProductKey ttProductKey, out UVProduct UVProduct)
        {
            ProductTypes productType = GetProductType(ttProductKey.Type);
            if (productType == ProductTypes.Unknown)
            {
                UVProduct = new UVProduct();
                return false;
            }

            //UVProduct = new Product(ttProductKey.MarketKey.Name, ttProductKey.Name, productType, ttProductKey.MarketKey.Name);
            UVProduct = new UVProduct(ttProductKey.MarketKey.Name, ttProductKey.Name, productType);
            //UVProduct.ForeignKey = ttProductKey;
            return true;
        }//TryConvert()
        //
        //
        // *****************************************************************************
        // ****                         Get Product Type()                          ****
        // *****************************************************************************       
        /// <summary>
        /// Given a TT product type, returns the UV Product type, or Unknown - if that type is not implemented yet.
        /// </summary>
        /// <param name="ttProductType">A TradingTechnologies ProductType</param>
        /// <returns>Corresponding UV ProductType</returns>
        public static ProductTypes GetProductType(TradingTechnologies.TTAPI.ProductType ttProductType)
        {
            // Determine the product type. TODO: add more
            ProductTypes productType = ProductTypes.Unknown;
            if (ttProductType == TradingTechnologies.TTAPI.ProductType.Future)
                productType = ProductTypes.Future;
            else if (ttProductType == TradingTechnologies.TTAPI.ProductType.Spread)
                productType = ProductTypes.Spread;
            else if (ttProductType == TradingTechnologies.TTAPI.ProductType.Stock)
                productType = ProductTypes.Equity;
            else if (ttProductType == TradingTechnologies.TTAPI.ProductType.Bond)
                productType = ProductTypes.Bond;
            else if (ttProductType == TradingTechnologies.TTAPI.ProductType.AutospreaderSpread)
                productType = ProductTypes.AutoSpreaderSpread;
            else if (ttProductType == TradingTechnologies.TTAPI.ProductType.Algo)
                productType = ProductTypes.Synthetic;
            else if (ttProductType == TradingTechnologies.TTAPI.ProductType.Strategy)
                productType = ProductTypes.Synthetic;
            //else if (ttProductType == TradingTechnologies.TTAPI.ProductType.Strategy)
            //else if (ttProductType == TradingTechnologies.TTAPI.ProductType.Index)
            return productType;
        }
        //
        //
        //
        //
        // *****************************************************************************
        // ****                         Get Product Type()                          ****
        // *****************************************************************************       
        /// <summary>
        /// Given a TT product type, returns the UV Product type, or Unknown - if that type is not implemented yet.
        /// </summary>
        /// <param name="productType">A TradingTechnologies ProductType</param>
        /// <returns>Corresponding UV ProductType</returns>
        public static TradingTechnologies.TTAPI.ProductType GetProductType(ProductTypes productType)
        {
            if (productType == ProductTypes.Bond)
                return TradingTechnologies.TTAPI.ProductType.Bond;
            else if (productType == ProductTypes.Equity)
                return TradingTechnologies.TTAPI.ProductType.Stock;
            else if (productType == ProductTypes.Spread)
                return TradingTechnologies.TTAPI.ProductType.Spread;
            else if (productType == ProductTypes.Future)
                return TradingTechnologies.TTAPI.ProductType.Future;
            else if (productType == ProductTypes.AutoSpreaderSpread)
                return TradingTechnologies.TTAPI.ProductType.AutospreaderSpread;
            else
            {
                throw new Exception("Unknown product type");

            }
        }
        #endregion Product Conversions End

        #region Instrument Conversions
        // *****************************************************************
        // ****              Instrument Conversions Methods             ****
        // *****************************************************************
        //
        //
        public static bool TryConvert(TradingTechnologies.TTAPI.Instrument ttInstr, out InstrumentName UVInstrumentName)
        {
            UVProduct UVProduct;
            if (TryConvert(ttInstr.Product, out UVProduct))                          // Determine the product type first. 
            {
                string ttName = string.Copy(ttInstr.Name);
                int ptr = ttName.IndexOf(UVProduct.Exchange, 0);                     // locate exchange name
                if (ptr >= 0 && ptr < ttName.Length)
                {   // Found exchange name.
                    ptr += UVProduct.Exchange.Length;                              // move pointer to end of exchange name
                    int ptr2 = ttName.IndexOf(UVProduct.ProductName, ptr);
                    if ((ptr2 >= 0 && ptr2 < ttName.Length) && ((ptr2 - ptr) < 3))
                    {   // Found product family
                        ptr2 += UVProduct.ProductName.Length;

                        string niceName = ttName.Substring(ptr2).Trim();
                        UVInstrumentName = new InstrumentName(UVProduct, niceName);
                        return true;
                    }
                    else
                    {   // The product name doesn't seem to be embedded in the 
                        string niceName = ttName.Substring(ptr).Trim();
                        UVInstrumentName = new InstrumentName(UVProduct, niceName);
                        return true;
                    }
                }
            }
            // Failure exit.
            UVInstrumentName = new InstrumentName();
            return false;
        }//TryConvert()
        //
        //
        //
        public static bool TryCreateInstrumentKey(string keyString, out TradingTechnologies.TTAPI.InstrumentKey key)
        {
            key = new TradingTechnologies.TTAPI.InstrumentKey();
            // Assume key has form:   "XXXX PP PPPPP (TYPE) SSSSSSS"
            // Where: 
            //  exchange name "XXXX" has NO embedded spaces; 
            //  product name "PP PPPPP" CAN have embedded spaces (like "IPE e-Gas Oil");
            //  there are NO extra parentheses, apart from those wrapping the instrument "TYPE"
            string[] parts = keyString.Split(new char[] { ')', '(' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                return false;
            }
            // Now we should have something like:  ["ICE_IPE IPE e-Gas Oil ","FUTURE"," 198755"]
            // Extract instrument type and series name from the last two terms.
            string typeStr = parts[1].Trim();
            string seriesName = parts[2].Trim();
            // Extract product and exchange.
            int n = parts[0].IndexOf(' ');                      // locate the FIRST space, assumed to be after the exchange name.
            if (n < 0 || n >= parts[0].Length)
            {
                return false;
            }
            string exchange = parts[0].Substring(0, n).Trim();
            string productName = parts[0].Substring(n + 1, parts[0].Length - (n + 1)).Trim();
            key = new TradingTechnologies.TTAPI.InstrumentKey(exchange, typeStr, productName, seriesName);
            return true;
        }//TryCreateInstrumentKey()
        //
        //
        //
        public static string ToString(TradingTechnologies.TTAPI.InstrumentKey ttKey)
        {
            return string.Format("{0} ({1}) {2}", ttKey.ProductKey.Name, ttKey.ProductKey.Type, ttKey.SeriesKey);
        }
        //
        //
        // *****************************************************************
        // ****            CreateUVInstrumentDetails()                  ****
        // *****************************************************************
        /// <summary>
        /// Create UV Instrument Details from TT Instrument Details 
        /// </summary>
        /// <param name="instrName"></param>
        /// <param name="instrDetails"></param>
        public static UVInstrDetails CreateUVInstrumentDetails(UV.Lib.Products.InstrumentName instrName, TTInstrumentDetails instrDetails)
        {
            UVProductTypes instrType;                                                 // finds the correct product type 
            if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Future))
                instrType = UVProductTypes.Future;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Spread))
                instrType = UVProductTypes.Spread;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Option))
                instrType = UVProductTypes.Option;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.AutospreaderSpread))
                instrType = UVProductTypes.AutoSpreaderSpread;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Bond))
                instrType = UVProductTypes.Bond;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Stock))
                instrType = UVProductTypes.Equity;
            else if (instrDetails.Key.ProductKey.Type.Equals(ProductType.Strategy))
                instrType = UVProductTypes.Synthetic;
            else
                instrType = UVProductTypes.Unknown;

            UV.Lib.Products.InstrumentDetails uvInstrDetails;
            uvInstrDetails = new UVInstrDetails(instrName,                                                                       // create the UV Instr details
                                                           instrDetails.Currency.Code,
                                                           TTConvertNew.ToUVTickSize(instrDetails),
                                                           TTConvertNew.ToUVExecTickSize(instrDetails),
                                                           TTConvertNew.ToUVMultiplier(instrDetails),
                                                           instrDetails.ExpirationDate.ToDateTime(),
                                                           instrType);
            return uvInstrDetails;
        }
        //
        //
        #endregion// Instrument Conversions

        #region Order Conversions
        // *****************************************************************
        // ****                Order Conversions Methods                ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Converts a TT Trade Direction object into a market side integer
        /// </summary>
        /// <param name="tradeDirection"></param>
        /// <returns></returns>
        public static int ToUVMarketSide(TradeDirection tradeDirection)
        {
            if (tradeDirection == TradeDirection.Take)
                return UVOrder.BuySide;
            else if (tradeDirection == TradeDirection.Hit)
                return UVOrder.SellSide;
            else if (tradeDirection == TradeDirection.Unknown)
                return UVOrder.UnknownSide;
            return UVOrder.UnknownSide;
        }
        //
        //
        public static BuySell ToBuySell(int signedQty)
        {
            if (signedQty > 0)
                return BuySell.Buy;
            else if (signedQty < 0)
                return BuySell.Sell;
            else
                return BuySell.Unknown;
        }//ToBuySell()
        //
        //
        /// <summary>
        /// Converts TT Buy Sell into a market side integer.a
        /// </summary>
        /// <param name="buySell"></param>
        /// <returns></returns>
        public static int ToMarketSide(BuySell buySell)
        {
            if (buySell == BuySell.Buy)
                return 0;
            else if (buySell == BuySell.Sell)
                return 1;
            else
                return 2;
        }
        //
        //
        //
        public static TTOrderType ToOrderType(UVOrderType uvOrderType)
        {
            switch (uvOrderType)
            {
                case UVOrderType.LimitOrder:
                    return TTOrderType.Limit;
                case UVOrderType.StopLimitOrder:            // TT doesn't distinguish between a stop limit and a limit order
                    return TTOrderType.Limit;
                case UVOrderType.MarketOrder:
                    return TTOrderType.Market;
                default:
                    return TTOrderType.Limit;
            }//switch()
        }//ToOrderType
        //
        //
        //
        /// <summary>
        /// Caller would like to take a TT time in force object and translate it into
        /// a UV time in force code.
        /// </summary>
        /// <param name="timeInForce"></param>
        /// <returns></returns>
        public static UVOrderTIF ToUVTimeinForce(TimeInForce timeInForce)
        {
            switch (timeInForce.Code)
            {
                case TimeInForceCode.GoodTillCancel:
                    return UVOrderTIF.GTC;
                case TimeInForceCode.GoodTillDay:
                    return UVOrderTIF.GTD;
                default:
                    return UVOrderTIF.GTD;
            }
        }
        //
        //
        /// <summary>
        /// Caller would like to conver a UV time in force to a TT time in force code.
        /// </summary>
        /// <param name="timeInForce"></param>
        /// <returns></returns>
        public static TimeInForceCode ToTimeInForce(UVOrderTIF timeInForce)
        {
            switch (timeInForce)
            {
                case UVOrderTIF.GTC:
                    return TimeInForceCode.GoodTillCancel;
                case UVOrderTIF.GTD:
                    return TimeInForceCode.GoodTillDay;
                default:
                    return TimeInForceCode.GoodTillDay;
            }
        }
        //
        //
        /// <summary>
        /// Caller would like to take a TT Order state and translate it to a UV order state.
        /// </summary>
        /// <param name="tradeState"></param>
        /// <returns></returns>
        public static UVOrderState ToUVOrderState(TradeState tradeState)
        {
            switch (tradeState)
            {
                case TradeState.Working:
                    return UVOrderState.Submitted;
                case TradeState.PartiallyFilled:
                    return UVOrderState.Submitted;
                case TradeState.FullyFilled:
                    return UVOrderState.Dead;
                case TradeState.Deleted:
                    return UVOrderState.Dead;
                case TradeState.Cancelled:
                    return UVOrderState.Dead;
                default:
                    return UVOrderState.Unsubmitted;
            }
        }
        //
        //
        /// <summary>
        /// Given a ttOrder try and create a UV style order. This overload instantiates
        /// a new order each time.
        /// </summary>
        /// <param name="ttOrder"></param>
        /// <param name="uvOrder"></param>
        /// <returns></returns>
        public static bool TryConvert(Order ttOrder, out UVOrder uvOrder)
        {
            bool isSuccess = true;
            try
            {
                uvOrder = new UVOrder();
                if (ttOrder.BuySell == BuySell.Buy)
                {
                    uvOrder.OriginalQtyConfirmed = ttOrder.OrderQuantity;
                    uvOrder.ExecutedQty = ttOrder.FillQuantity;
                }
                else
                {
                    uvOrder.OriginalQtyConfirmed = ttOrder.OrderQuantity * -1;
                    uvOrder.ExecutedQty = ttOrder.FillQuantity * -1;
                }
                uvOrder.OrderTIF = ToUVTimeinForce(ttOrder.TimeInForce);
                uvOrder.IPriceConfirmed = ttOrder.LimitPrice.ToTicks() / ttOrder.InstrumentDetails.SmallestTickIncrement;
                uvOrder.TickSize = ToUVTickSize(ttOrder.InstrumentDetails);
                uvOrder.OrderStateConfirmed = ToUVOrderState(ttOrder.TradeState);
            }
            catch (Exception)
            {
                isSuccess = false;
                uvOrder = null;
            }
            return isSuccess;
        }
        //
        //
        /// <summary>
        /// Given a ttOrder try and create a UV style order attempting to use 
        /// a recycle order each time.
        /// </summary>
        /// <param name="ttOrder"></param>
        /// <param name="orderRecycler"></param>
        /// <param name="uvOrder"></param>
        /// <returns></returns>
        public static bool TryConvert(Order ttOrder, RecycleFactory<UVOrder> orderRecycler, out UVOrder uvOrder)
        {
            bool isSuccess = true;
            try
            {
                uvOrder = orderRecycler.Get();                      // try and use a recycled order
                if (ttOrder.BuySell == BuySell.Buy)
                {
                    uvOrder.OriginalQtyConfirmed = ttOrder.OrderQuantity;
                    uvOrder.ExecutedQty = ttOrder.FillQuantity;
                    uvOrder.Side = UVOrder.BuySide;
                }
                else
                {
                    uvOrder.OriginalQtyConfirmed = ttOrder.OrderQuantity * -1;
                    uvOrder.ExecutedQty = ttOrder.FillQuantity * -1;
                    uvOrder.Side = UVOrder.SellSide;
                }
                uvOrder.OrderTIF = ToUVTimeinForce(ttOrder.TimeInForce);
                uvOrder.IPriceConfirmed = ttOrder.LimitPrice.ToTicks() / ttOrder.InstrumentDetails.SmallestTickIncrement;
                uvOrder.TickSize = ToUVTickSize(ttOrder.InstrumentDetails);
                uvOrder.OrderStateConfirmed = ToUVOrderState(ttOrder.TradeState);
            }
            catch (Exception)
            {
                isSuccess = false;
                uvOrder = null;
            }
            return isSuccess;
        }
        //
        //
        /// <summary>
        /// provided with TT instrdetails object this method returns
        /// what UV commonly calls TickSize. Which is the smallest increment
        /// the price in the instrument could actually change by.
        /// </summary>
        /// <param name="instrDetails"></param>
        /// <returns></returns>
        public static double ToUVTickSize(TTInstrumentDetails instrDetails)
        {
            double uvTickSize;
            uvTickSize = Convert.ToDouble(instrDetails.TickSize.Numerator) /
                         Convert.ToDouble(instrDetails.TickSize.Denominator) *
                         Convert.ToDouble(instrDetails.SmallestTickIncrement);
            return uvTickSize;
        }
        //
        //
        /// <summary>
        /// provided with TT instrdetails object this method returns
        /// what UV commonly calls Executable TickSize. Which is the 
        /// smallest possible unit fills could come across in.
        /// </summary>
        /// <param name="instrDetails"></param>
        /// <returns></returns>
        public static double ToUVExecTickSize(TTInstrumentDetails instrDetails)
        {
            double uvExecTickSize;
            uvExecTickSize = Convert.ToDouble(instrDetails.TickSize.Numerator) /
                             Convert.ToDouble(instrDetails.TickSize.Denominator);
            return uvExecTickSize;
        }
        //
        //
        /// <summary>
        /// provided with TT instrument details object this method returns
        /// what UV commonly calls the multiplier. Which is the value you could multiply
        /// a price by to get the native currency value of.  Additionally, this multplied by
        /// the tick size would equal the tick value.
        /// </summary>
        /// <param name="instrDetails"></param>
        /// <returns></returns>
        public static double ToUVMultiplier(TTInstrumentDetails instrDetails)
        {
            double uvMultiplier = instrDetails.TickValue / ToUVTickSize(instrDetails) *
                                   Convert.ToDouble(instrDetails.SmallestTickIncrement);
            return uvMultiplier;
        }
        #endregion // end Order Conversions
    }
}

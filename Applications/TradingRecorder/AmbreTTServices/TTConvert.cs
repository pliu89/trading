using System;
using System.Collections.Generic;
using System.Text;

namespace Ambre.TTServices
{
    using Misty.Lib.Products;

    /// <summary>
    /// Static functions that convert TT Products and instruments to Misty products and instruments.
    /// </summary>
    public static class TTConvert
    {

        public static bool TryConvert(TradingTechnologies.TTAPI.Product ttProduct, out Product mistyProduct)
        {
            //mistyProduct = null;
            ProductTypes productType = GetProductType(ttProduct.Type);
            if (productType == ProductTypes.Unknown)
            {
                mistyProduct = new Product();
                return false;
            }

            //mistyProduct = new Product(ttProduct.Market.Name, ttProduct.Name, productType, ttProduct.Market.Name);
            mistyProduct = new Product(ttProduct.Market.Name, ttProduct.Name, productType);

            return true;
        }//TryConvert()       
        public static bool TryConvert(TradingTechnologies.TTAPI.ProductKey ttProductKey, out Product mistyProduct)
        {
            ProductTypes productType = GetProductType(ttProductKey.Type);
            if (productType == ProductTypes.Unknown)
            {
                mistyProduct = new Product();
                return false;
            }

            //mistyProduct = new Product(ttProductKey.MarketKey.Name, ttProductKey.Name, productType, ttProductKey.MarketKey.Name);
            mistyProduct = new Product(ttProductKey.MarketKey.Name, ttProductKey.Name, productType);
            //mistyProduct.ForeignKey = ttProductKey;
            return true;
        }//TryConvert()
        /*
        public static bool TryConvert(TradingTechnologies.TTAPI.Instrument ttInstr, out InstrumentBase mistyInstrument)
        {
            // Determine the product type first. 
            Product mistyProduct;
            if (TryConvert( ttInstr.Product, out mistyProduct))
            {
                mistyInstrument = new InstrumentBase();
                mistyInstrument.ExpirationDate = ttInstr.InstrumentDetails.ExpirationDate.ToDateTime();
                mistyInstrument.Name = ttInstr.Name;
                mistyInstrument.ForeignKey = ttInstr.InstrumentDetails.Key;         // store unique foreign name                
                InstrumentName instrName = new InstrumentName(mistyProduct,ttInstr.Name);   // THIS MAY NOT ALWAYS BE UNIQUE IN TT.. TRY FOR NOW.                
                mistyInstrument.KeyName = instrName;
                return true;
            }
            else
            {   // Failed to convert the product
                mistyInstrument = null;
                return false;
            }
        }//TryConvert()
        */ 
        //
        //
        public static bool TryConvert(TradingTechnologies.TTAPI.Instrument ttInstr, out InstrumentName mistyInstrumentName)
        {
            Product mistyProduct;
            if (TryConvert(ttInstr.Product, out mistyProduct))                          // Determine the product type first. 
            {
                string ttName = string.Copy(ttInstr.Name);
                int ptr = ttName.IndexOf(mistyProduct.Exchange,0);                     // locate exchange name
                if ( ptr>=0 && ptr<ttName.Length)
                {   // Found exchange name.
                    ptr += mistyProduct.Exchange.Length;                              // move pointer to end of exchange name
                    int ptr2 = ttName.IndexOf(mistyProduct.ProductName, ptr);
                    if ((ptr2 >= 0 && ptr2 < ttName.Length) && ((ptr2 - ptr) < 3) )
                    {   // Found product family
                        ptr2 += mistyProduct.ProductName.Length;

                        string niceName = ttName.Substring(ptr2).Trim();
                        mistyInstrumentName = new InstrumentName(mistyProduct, niceName);
                        return true;
                    }
                    else
                    {   // The product name doesn't seem to be embedded in the 
                        string niceName = ttName.Substring(ptr).Trim();
                        mistyInstrumentName = new InstrumentName(mistyProduct, niceName);
                        return true;
                    }
                }
            }
                // Failure exit.
                mistyInstrumentName = new InstrumentName();
                return false;

            /* 
            Product mistyProduct;
            if (TryConvert(ttInstr.Product, out mistyProduct))                          // Determine the product type first. 
            {
            string ttName = string.Copy(ttInstr.Name);
            string[] elems = ttInstr.Name.Split(new string[] { ttInstr.Product.Alias }, StringSplitOptions.None);
            if (elems.Length > 1)
            {
                string niceName = elems[elems.Length - 1].Trim();
                mistyInstrumentName = new InstrumentName(mistyProduct, niceName);  // NOTE: THIS MAY NOT ALWAYS BE UNIQUE IN TT.. TRY FOR NOW.               
                return true;
            }
            else
            {
                mistyInstrumentName = new InstrumentName();
                return false;
            }
             * }
            //else
            //{   // Failed to convert
            //    mistyInstrumentName = new InstrumentName();
            //    return false;
            //}
             * */
        }//TryConvert()
        //
        //
        // *****************************************************************************
        // ****                         Get Product Type()                          ****
        // *****************************************************************************       
        /// <summary>
        /// Given a TT product type, returns the misty Product type, or Unknown - if that type is not implemented yet.
        /// </summary>
        /// <param name="ttProductType">A TradingTechnologies ProductType</param>
        /// <returns>Corresponding Misty ProductType</returns>
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
                productType = ProductTypes.Strategy;
            else if (ttProductType == TradingTechnologies.TTAPI.ProductType.Strategy)
                productType = ProductTypes.Strategy;
            //else if (ttProductType == TradingTechnologies.TTAPI.ProductType.Strategy)
            //else if (ttProductType == TradingTechnologies.TTAPI.ProductType.Index)
            return productType;
        }

        //
        //
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

        /// <summary>
        /// Check whether the instrument key already exist.
        /// </summary>
        /// <param name="instrumentKey"></param>
        /// <returns></returns>
        public static bool CheckExistenceOfInstrumentKey(List<TradingTechnologies.TTAPI.InstrumentKey> keyList, TradingTechnologies.TTAPI.InstrumentKey instrumentKey)
        {
            foreach (TradingTechnologies.TTAPI.InstrumentKey key in keyList)
                if (IsTwoInstrumentEqual(key, instrumentKey))
                    return true;

            return false;
        }

        /// <summary>
        /// This method checks whether the two instrument keys are equal.
        /// </summary>
        /// <param name="key1"></param>
        /// <param name="key2"></param>
        /// <returns></returns>
        public static bool IsTwoInstrumentEqual(TradingTechnologies.TTAPI.InstrumentKey key1, TradingTechnologies.TTAPI.InstrumentKey key2)
        {
            string keyString1 = TTConvert.ToString(key1);
            string keyString2 = TTConvert.ToString(key2);
            return keyString1.Equals(keyString2);
        }
    }
}

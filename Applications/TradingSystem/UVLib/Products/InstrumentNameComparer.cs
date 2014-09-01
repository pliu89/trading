using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace UV.Lib.Products
{

    public class InstrumentNameComparer : IComparer<InstrumentName>
    {

        /// <summary>
        /// Orders instruments by: ProductType, Exchange, Product name, finally by Series name.
        /// </summary>
        /// <param name="nameA"></param>
        /// <param name="nameB"></param>
        /// <returns></returns>
        public int Compare(InstrumentName nameA, InstrumentName nameB)
        {
            // TODO: To allow for other methods of sorting, simply create an enum
            // with the details and hand it to the constructor.  Then branch the following
            // code with different ordering rules.

            // Alphabetical.
            // Assumes Exchange and product name are NOT null.  But SeriesName may be null.
            // A null SeriesName is like a product, so its listed with lower value.
            int compare = nameA.Product.Type.CompareTo(nameB.Product.Type);
            if (compare == 0)
            {
                compare = nameA.Product.Exchange.CompareTo(nameB.Product.Exchange);
                if (compare == 0)
                {
                    compare = nameA.Product.ProductName.CompareTo(nameB.Product.ProductName);
                    if (compare == 0)
                    {   // I allow null SeriesNames as a stand in for products 
                        // Nulls are listed first.
                        if (string.IsNullOrWhiteSpace(nameA.SeriesName))
                        {   // A is a product.
                            if (string.IsNullOrWhiteSpace(nameB.SeriesName))
                                return 0;       // they are both equal products.
                            else
                                return -1;      // B is an instrument from A's product family, list A < B.  
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(nameB.SeriesName))
                                return +1;
                            else
                                return nameA.SeriesName.CompareTo(nameB.SeriesName);
                        }
                    }
                }
            }
            return compare;            
        }// Compare()




    }
}

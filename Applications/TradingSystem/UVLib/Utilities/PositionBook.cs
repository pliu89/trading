using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;

namespace UV.Lib.Utilities
{
    /// <summary>
    /// This object simply holds a list of the current fills for a *single contract* (or strategy, etc)
    /// and the associated fill prices.  The difference between this object and a simple "fill" book, 
    /// is that while a "fill book" might containt all fills (both longs and shorts) that were collected 
    /// throughout a day, this "position book" will cancel positions on opposite sides of the mkt; 
    /// at any given moment, this book will contain only long positions, or only short positions or be empty.
    /// For example, when it contains a collection of shorts, and the user calls the "Add" method with a 
    /// positive (long) fill qty, this long position will be cancelled against the pre-existing short positions.
    /// 
    /// This book only contains the current "OPEN" positions, their quantities at each fill price. 
    /// The net position is summarized in TotalQty.
    /// 
    /// Some of the public methods in this class take a "PriceIndex" argument.  This is different from
    /// then internal index (referred to herein as the List Index).  The Price index is a zero-based index
    /// where priceIndex = 0 is the WORST fill in the book, or "fill that is closest to the other side";  
    /// that is, for long positions, priceIndex=0 is the highest fill price!  For short position, it is 
    /// the lowest fill price.
    /// </summary>
    public class PositionBookOLD // deprecate for now
    {
        #region Member variables
        // *************************************************************************
        // ****							Member Variables						****
        // *************************************************************************
        private System.Collections.SortedList m_List;

        public int TotalQty = 0;							// net position at current moment.
        public int TotalTraded = 0;							// total traded, both long and shorts.
        public double m_RealizedGains = 0.0d;				// realized gains from closed positions.

        //
        // Properties that must be set by the user.
        //
        public double SmallestPriceIncrement = 1.0;			// smallest exchange price unit. (0.5 for eurodollars)
        public double DollarPerPoint = 1.0;                 // Dollar value of this contract.
        #endregion//end Members


        #region Properties
        // *****************************************************************************
        // ****								Properties								****
        // *****************************************************************************
        //
        //
        // ****						Count						****
        //
        /// <summary>Gets the total number of unique positions.</summary>
        public int Count
        {
            get { return m_List.Count; }
        }
        //
        //
        //
        // ****                     Average Price               ****
        //
        public double AveragePrice
        {
            get
            {
                double avePrice = 0.0d;
                double price = 0.0;
                int qty = 0;
                for (int i = 0; i < m_List.Count; ++i)
                {
                    GetFillPriceQty(i, out qty, out price);
                    avePrice += price * qty;
                }// for each fill i.
                // Exit.
                if (TotalQty != 0) return avePrice / Convert.ToDouble(TotalQty);
                else return 0.0d;
            }
        }//end AveragePrice
        //
        //
        // ****             Realized Dollar Gains           ****
        //
        /// <summary>
        /// This property gets the dollar amt of gains realized thus far.
        /// This value depends on the variable "DollarPerPoint" being properly set.
        /// </summary>
        public double RealizedDollarGains
        {
            get { return m_RealizedGains * DollarPerPoint; }
        }
        #endregion//end Properties


        #region Constructor
        // *************************************************************************
        // ****						Constructors and destructor					****
        // *************************************************************************
        /// <summary>
        /// 
        /// </summary>
        /// <param name="smallestPriceIncrement">The quoted size of a "tick"; for ED
        ///     this is 0.50, for equities its 0.01 (penny).</param>
        /// <param name="dollarAmtOfSmallestPriceIncrement">The dollar amount of a trading UNIT 
        ///     (not tick); for ED the dollar value per trading unit is $25, while for
        ///     stocks the dollar value per trading unit (or "point") is $1.</param>
        public PositionBookOLD(double smallestPriceIncrement, double dollarAmtOfSmallestPriceIncrement)
        {
            m_List = new System.Collections.SortedList(32);
            SmallestPriceIncrement = smallestPriceIncrement;
            DollarPerPoint = dollarAmtOfSmallestPriceIncrement / smallestPriceIncrement;
        }//end constructor.	
        //
        //
        #endregion//end Constructor


        #region Book manipulation methods
        // *************************************************************************
        // ****							Public Methods 							****
        // *************************************************************************
        //
        // ****					Add()					****
        //
        /// <summary>
        ///		Add a new fill to the list.
        /// Note:
        ///		Under normal circumstances price/SmallestPriceIncrement must be an integer.  
        /// However, in a simulator we might assign slippage trade costs that are *smaller*
        /// than the SmallestPriceIncrement.  In this case, we replace this fractional price
        /// fill with TWO fills at different prices such that the (average price) * qty is 
        /// unchanged.  In particular, a fill price p, qty q is filled as:
        /// 
        ///		q * p = q *(1-a) * Fl(p)  + q * a * Ce(p)			a is fraction of fill given to higher price.
        ///
        /// then, a = ( p - Fl(p) ) / ( Fl(p) - Ce(p) ),
        /// 
        /// where Fl(p) and Ce(p) are the floor and ceiling prices around p.
        /// The denominator above is simply SmallestPriceIncrement, p - Fl(p) / SmallestPriceIncrement
        /// is called the excessTickPrice.  This fill is thus broken down according to the group addition rule:
        /// 
        ///		{p,q} -->   {Fl(p), (1-a)*q}	(+)		{Ce(p), a*q}
        ///		
        /// TODO:  Please confirm that the following works even for prices that are negative!!
        /// </summary>
        /// <param name="price">exchange-quoted price of fill.</param>
        /// <param name="quantity">Quantity filled at this price.</param>
        /// <returns>realized gain (in dollars), if any.</returns>
        public double Add(int quantity, double price)
        {	//
            // Convert to exhange-traded tick-quantized trades.
            //
            double tickPrice = price / SmallestPriceIncrement;				// rarely this is a fraction, see above note.
            if (Math.Abs(tickPrice) > Int32.MaxValue) { tickPrice = 0; }
            int floorTickPrice = Convert.ToInt32(Math.Min(Int32.MaxValue, Math.Max(Int32.MinValue, Math.Floor(tickPrice))));	// this is always an integer.
            double excessTickPrice = tickPrice - floorTickPrice;			// this is remaining amt, also this is 
            // the fraction of fill to be shared with ceiling price.
            double gain = 0.0;
            int ceilingQty = Convert.ToInt32(Math.Round(quantity * excessTickPrice));
            if (ceilingQty != 0)
            {	// fill higher price.
                int ceilingTickPrice = (floorTickPrice + 1);
                gain += Convert.ToDouble(Add(ceilingQty, ceilingTickPrice)) * SmallestPriceIncrement;
            }
            if (ceilingQty != quantity)
            {	// fill lower price (or exact price).
                gain += Convert.ToDouble(Add(quantity - ceilingQty, floorTickPrice)) * SmallestPriceIncrement;
            }

            /* original version
            int tickPrice = Convert.ToInt32(Math.Round(price / SmallestPriceIncrement));
            double gain = Convert.ToDouble(Add( quantity, tickPrice)) * SmallestPriceIncrement;	// realized gains in user's units.
            */

            m_RealizedGains += gain;					// update gains.
            TotalTraded += Math.Abs(quantity);			// update total contracts traded.
            return gain;
        }//end Add()		
        //
        //
        //
        // ****					Add(string)					****
        //
        /// <summary>
        /// Overloading that takes a string in the format: qty @ fillPrice; that is, 
        /// "+10 @ 35.5" for example.
        /// </summary>
        /// <param name="fillString"></param>
        /// <returns>realized gain (in dollars), if any.</returns>
        public double Add(string fillString)
        {
            int fillQty = 0;
            double fillPrice = 0.0d;
            double gain = 0.0d;
            if (FromString(fillString, out fillQty, out fillPrice))
            {	// fillString is valid!
                gain = Add(fillQty, fillPrice);		// add the qty and price to our fill list.
            }

            // Exit.
            return gain;
        }//end Add()
        //
        //
        //
        // ****					AddAll(string)					****
        //
        /// <summary>
        /// This takes the whole serialized string for this position book.
        /// It splits the position into individual fill strings and adds them to the book
        /// </summary>
        /// <param name="allPositionString"></param>
        /// <returns>realized gain (in dollars), if any.</returns>
        public double AddAll(string allPositionString)
        {
            string[] fillString = allPositionString.Split("/".ToCharArray());
            double gain = 0.0;				// compute any gain that results from this add all.
            for (int iFill = 0; iFill < fillString.Length; iFill++)
            {
                gain += Add(fillString[iFill].Trim());
            }
            return gain;
        }//end AddAll()
        //
        //
        //
        //
        // ****					Remove At					****
        //
        /// <summary>
        /// Allows the user to delete the position at his chosen index.
        /// </summary>
        /// <param name="priceIndex"></param>
        /// <returns></returns>
        public bool RemoveAt(int priceIndex)
        {
            int listIndex = this.PriceIndexToListIndex(priceIndex);	// get corresponding internal index.
            if (listIndex < 0) { return false; }

            // Remove the position.
            int qty = (int)m_List.GetByIndex(listIndex);
            TotalQty -= qty;					// reduce our total position by that fill.
            TotalTraded -= Math.Abs(qty);
            m_List.RemoveAt(listIndex);		// remove that fill.

            // Exit.
            return true;						// success!
        }//DeletePositionAt
        //
        //
        //
        //
        // ****					Remove At					****
        //
        public bool RemoveAt(double price, int qtyToRemove)
        {
            // Check validity of price.
            int tickPrice = Convert.ToInt32(Math.Round(price / SmallestPriceIncrement));
            if (!m_List.ContainsKey(tickPrice)) return false;

            // Get index value corresponding this price.
            int listIndex = m_List.IndexOfKey(tickPrice);
            if (listIndex < 0 || listIndex >= m_List.Count) return false;

            return RemoveAtListIndex(listIndex, qtyToRemove);
        }//end RemoveAt()
        //
        //
        //
        // ****					Remove At					****
        //
        public bool RemoveAt(int priceIndex, int qtyToRemove)
        {
            int listIndex = this.PriceIndexToListIndex(priceIndex);	// get corresponding internal index.
            if (listIndex < 0) { return false; }						// invalid priceIndex!

            return RemoveAtListIndex(listIndex, qtyToRemove);
        }//end RemoveAt().
        //
        //
        //
        // ****					RemoveAtListIndex			****
        //
        /// <summary>
        /// This is a private utility.
        /// </summary>
        /// <param name="listIndex"></param>
        /// <param name="qtyToRemove"></param>
        /// <returns></returns>
        private bool RemoveAtListIndex(int listIndex, int qtyToRemove)
        {
            // Analyze old position, and 
            int oldQty = (int)m_List.GetByIndex(listIndex);			// qty currently in book.
            if (qtyToRemove * oldQty <= 0) { return false; }			// cannot *remove* an opposite position!
            if (Math.Abs(qtyToRemove) > Math.Abs(oldQty)) { return false; }	// cannot remove more than you have!

            // remove it.			
            TotalQty -= qtyToRemove;			// reduce our total position by that fill.
            TotalTraded -= Math.Abs(qtyToRemove);
            if (qtyToRemove == oldQty)
            {	// user wants to delete this entire level.
                m_List.RemoveAt(listIndex);		// remove everything that fill.
            }
            else
            {	// user wants to reduce the qty at this price level.
                m_List.SetByIndex(listIndex, (oldQty - qtyToRemove));
            }
            // Exit.
            return true;
        }//end RemoveAtListIndex().
        //
        //
        //
        //
        // ****					MakeCopy()						****
        //
        /// <summary>
        /// This makes a deep copy of this instance of Position Book.
        /// </summary>
        /// <returns></returns>
        public PositionBookOLD MakeCopy()
        {
            double[] weight = new double[1] { 1.0 };
            PositionBookOLD[] book = new PositionBookOLD[1] { this };
            return MakeNewCombinedBook(book, weight);
        }//end MakeCopy().
        //
        //
        //
        // ****					Make New Combined Book				****
        //
        /// <summary>
        /// This static function creates a new Book by "combining" the fills of two or more other books.
        /// This is done by matching fills, one at a time from the worst price level up to the best.
        /// The new book is created and its pointer is returned to the caller.
        /// </summary>
        /// <param name="book">Array of PositionBook objects.  These are not affected by this routine.</param>
        /// <param name="weight">double array containing relative weights to be assigned to fills.
        ///		For example; a spread of two books would have weights of (+1.0,-1.0). </param>
        /// <returns>new PositionBook that represents the implied position. </returns>
        public static PositionBookOLD MakeNewCombinedBook(PositionBookOLD[] book, double[] weight)
        {
            int nBooks = book.Length;
            if (nBooks < 1) { return null; }	// return null if we passed fewer than one book.
            if (nBooks != weight.Length) { return null; }	// weight and book array length must be same.

            //		
            // Set tick size of new book.
            //
            double newTickSize = book[0].SmallestPriceIncrement;
            for (int i = 1; i < nBooks; ++i)
            {
                newTickSize = Math.Min(newTickSize, book[i].SmallestPriceIncrement);
            }
            // This is very naive, and wrong when the two books being combined are
            // very different instruments.  Fix me.
            double newDollarValuePerPoint = book[0].DollarPerPoint;
            PositionBookOLD newBook = new PositionBookOLD(newTickSize, newDollarValuePerPoint);			// create new blank position book.



            newBook.CombineBooks(book, weight, false);
            return newBook;

        }//end MakeNewCombinedBook
        //
        //
        //
        // ****						Combine Books						***
        //
        /// <summary>
        /// This method first clears all entries in this book instance, then reloads it with fills implied
        /// by combining the array of books[] passed to it.  This overloading does not touch the array of
        /// input books; they are left unchanged.  This overloading is useful for making "implied" books 
        /// from 
        /// </summary>
        /// <param name="book"></param>
        /// <param name="weight"></param>
        public bool CombineBooks(PositionBookOLD[] book, double[] weight)
        {
            return CombineBooks(book, weight, false);
        }//end CombineBooks.
        //
        //
        //
        // ****						Combine Books						***
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="book">array of books to be combined.</param>
        /// <param name="weight">weights to use when combining books.</param>
        /// <param name="removeFillsFromBooks">If true this method removes the fills from the original 
        ///		books that were combined into the final book.  In this way, the sum of the input books
        ///		and the resulting combined book are equivalent to the total number of fills.
        ///		</param>
        /// <param name="combineBestFillsFirst">
        ///		If true then the best fills are combined together to form the new book.  The usual behavior
        ///		is false, that the worst fills from both books are combined to form a bad fill.
        ///		</param>
        public bool CombineBooks(PositionBookOLD[] book, double[] weight, bool removeFillsFromBooks
            , bool combineBestFillsFirst)
        {
            //
            // Delete entries if my book is purely-implied book.
            //
            // If the flag is set to NOT remove the fills combined into the resulting book, 
            // the implication is that this book instance is a shadow or purely implied book.
            // As such, the purely-implied book is completely cleared of its previous fills first.
            if (!removeFillsFromBooks) this.ClearAll();


            //
            // Validate input variables.
            //
            bool isBookChanged = false;		// default is to assume that books were not combined.
            int nBooks = book.Length;
            if (nBooks < 1) { return isBookChanged; }
            if (nBooks != weight.Length) { return isBookChanged; }

            //
            // Validate signs of books with weights.
            //
            int positionSign = Math.Sign(book[0].TotalQty / weight[0]);	// sign of position adding to new book.
            // Check that all books have signs consistent with weights.
            for (int i = 0; i < nBooks; ++i)
            {
                // Check: each book needs to be able to contribute at least 1 unit of the implied strategy.
                if (Math.Abs(Math.Round(book[i].TotalQty / weight[i])) < 1.0) { return isBookChanged; }
                // Check: each book must have a consistent sign wrt other books.
                if ((positionSign * book[i].TotalQty / weight[i]) <= 0.0) { return isBookChanged; }
            }//ith book.

            // If we make it here, there should be some combining possible.
            // Set flag indicating that the final book has changed.
            isBookChanged = true;


            //
            // Load qty/prices from all books.
            //
            int[][] levelQty = new int[nBooks][];
            double[][] levelPrice = new double[nBooks][];
            for (int i = 0; i < nBooks; ++i) { book[i].GetPriceQtyArrays(out levelQty[i], out levelPrice[i], combineBestFillsFirst); }

            //
            // Load new book one complete price-level at a time. 
            //
            int[] level = new int[nBooks];					// price level pointer for each book.
            bool shouldContinue = true;						// flag indicated whether or not we should continue building fills.
            while (shouldContinue)
            {
                //
                // Find largest implied qty (at this price level) for new combined book.
                //
                int qty = Convert.ToInt32(Math.Round(Math.Abs(levelQty[0][level[0]] / weight[0])));	// use this as initial guess.
                for (int i = 0; i < nBooks; ++i)
                {	// Loop thru all books, and check validity of levelQty[], then update qty if needed.
                    double x = Math.Abs(levelQty[i][level[i]] / weight[i]);	// implied qty for ith book (at this level).
                    if (x < 1.0)
                    {	// implied qty is less than 1.0
                        level[i]++;							// point to next price level.
                        if (level[i] >= levelQty[i].Length) { shouldContinue = false; }// Stop if no next price level.
                        qty = 0;
                        break;								// stop looping thru books.
                    }
                    // Update implied qty for this level. 
                    qty = Math.Min(qty, Convert.ToInt32(Math.Round(x)));	// maximum implied qty
                }//ith book.

                //
                // Add any non-zero implied quantities to the book.
                //
                if (qty > 0)
                {
                    // Determine price at this level, subtract qty from each sub-book.
                    double price = 0.0d;						// initialize dummy price to zero.
                    for (int i = 0; i < nBooks; ++i)				// loop thru each sub-book.
                    {
                        int qtyUsed = Convert.ToInt32(Math.Round(qty * weight[i]));	// unsigned qty to use from this book.
                        price += qtyUsed * levelPrice[i][level[i]];						// calculate implied price.
                        levelQty[i][level[i]] -= qtyUsed * positionSign;				// remove qty from this book used to make fill.
                    }//ith book.
                    this.Add(qty * positionSign, price / qty);		// add to implied PositionBook.
                }//if implied qty is non-zero.
            }//end while (shouldContinue)

            //
            // Remove fills from input books, if desired.
            //
            if (removeFillsFromBooks)
            {
                for (int i = 0; i < nBooks; ++i)
                {
                    int[] levelQtyOriginal;
                    double[] levelPriceOriginal;
                    book[i].GetPriceQtyArrays(out levelQtyOriginal, out levelPriceOriginal, combineBestFillsFirst);
                    for (int iLevel = 0; iLevel < levelQtyOriginal.Length; ++iLevel)
                    {
                        //int qtyToAdd = ( levelQty[i][iLevel] - levelQtyOriginal[iLevel] ); // initial - final
                        //book[i].Add( qtyToAdd, levelPrice[i][iLevel] );
                        int qtyToRemove = (levelQtyOriginal[iLevel] - levelQty[i][iLevel]);
                        if (qtyToRemove != 0) book[i].RemoveAt(levelPriceOriginal[iLevel], qtyToRemove);
                    }//price level
                }//i
            }//if removeFillsFromBooks().

            // 
            // Exit.
            //
            return isBookChanged;
        }//end CombineBooks()
        //
        //
        public bool CombineBooksAtSideandPrice(PositionBookOLD[] book, double[] weight, bool removeFillsFromBooks
    , bool combineBestFillsFirst, double BuyPrice91, double SellPrice91)
        {
            //
            // Delete entries if my book is purely-implied book.
            //
            // If the flag is set to NOT remove the fills combined into the resulting book, 
            // the implication is that this book instance is a shadow or purely implied book.
            // As such, the purely-implied book is completely cleared of its previous fills first.
            if (!removeFillsFromBooks) this.ClearAll();


            //
            // Validate input variables.
            //
            bool isBookChanged = false;		// default is to assume that books were not combined.
            int nBooks = book.Length;
            if (nBooks < 1) { return isBookChanged; }
            if (nBooks != weight.Length) { return isBookChanged; }

            //
            // Validate signs of books with weights.
            //
            int positionSign = Math.Sign(book[0].TotalQty / weight[0]);	// sign of position adding to new book.
            // Check that all books have signs consistent with weights.
            for (int i = 0; i < nBooks; ++i)
            {
                // Check: each book needs to be able to contribute at least 1 unit of the implied strategy.
                if (Math.Abs(Math.Round(book[i].TotalQty / weight[i])) < 1.0) { return isBookChanged; }
                // Check: each book must have a consistent sign wrt other books.
                if ((positionSign * book[i].TotalQty / weight[i]) <= 0.0) { return isBookChanged; }
            }//ith book.

            // If we make it here, there should be some combining possible.
            // Set flag indicating that the final book has changed.
            //isBookChanged = true;


            //
            // Load qty/prices from all books.
            //
            int[][] levelQty = new int[nBooks][];
            double[][] levelPrice = new double[nBooks][];
            for (int i = 0; i < nBooks; ++i) { book[i].GetPriceQtyArrays(out levelQty[i], out levelPrice[i], combineBestFillsFirst); }

            //
            // Load new book one complete price-level at a time. 
            //
            int[] level = new int[nBooks];					// price level pointer for each book.
            bool shouldContinue = true;						// flag indicated whether or not we should continue building fills.
            while (shouldContinue)
            {
                //
                // Find largest implied qty (at this price level) for new combined book.
                //
                int qty = Convert.ToInt32(Math.Round(Math.Abs(levelQty[0][level[0]] / weight[0])));	// use this as initial guess.
                for (int i = 0; i < nBooks; ++i)
                {	// Loop thru all books, and check validity of levelQty[], then update qty if needed.
                    double x = Math.Abs(levelQty[i][level[i]] / weight[i]);	// implied qty for ith book (at this level).
                    if (x < 1.0)
                    {	// implied qty is less than 1.0
                        level[i]++;							// point to next price level.
                        if (level[i] >= levelQty[i].Length) { shouldContinue = false; }// Stop if no next price level.
                        qty = 0;
                        break;								// stop looping thru books.
                    }
                    // Update implied qty for this level. 
                    qty = Math.Min(qty, Convert.ToInt32(Math.Round(x)));	// maximum implied qty
                }//ith book.

                //
                // Add any non-zero implied quantities to the book.
                //
                if (qty > 0)
                {
                    // Determine price at this level, subtract qty from each sub-book.
                    double price = 0.0d;						// initialize dummy price to zero.
                    for (int i = 0; i < nBooks; ++i)				// loop thru each sub-book.
                    {
                        int qtyUsed = Convert.ToInt32(Math.Round(qty * weight[i]));	// unsigned qty to use from this book.
                        price += qtyUsed * levelPrice[i][level[i]];						// calculate implied price.
                    }//ith book.
                    /*
                    if (this.TotalQty < 0)   // if we have short position 
                    {
                        if (positionSign > 0) // if the incoming fill is long position fill
                        {
                            // now add this fill to the position book
                            this.Add(qty * positionSign, price / qty);		// add to implied PositionBook.
                            isBookChanged = true;                           // now we can say that the book has changed
                            for (int i = 0; i < nBooks; ++i)				// loop thru each sub-book.
                            {
                                int qtyUsed = Convert.ToInt32(Math.Round(qty * weight[i]));	// unsigned qty to use from this book.
                                levelQty[i][level[i]] -= qtyUsed * positionSign;				// remove qty from this book used to make fill.
                            }
                        }
                        else  // If the incoming fill is a short position fill
                        {
                            if ((price / qty) >= SellPrice91)  // see if the price at which we are getting the fill is at price better than the sell price specified
                            {
                                // now add this fill to the position book
                                this.Add(qty * positionSign, price / qty);		// add to implied PositionBook.
                                isBookChanged = true;                           // now we can say that the book has changed
                                for (int i = 0; i < nBooks; ++i)				// loop thru each sub-book.
                                {
                                    int qtyUsed = Convert.ToInt32(Math.Round(qty * weight[i]));	// unsigned qty to use from this book.
                                    levelQty[i][level[i]] -= qtyUsed * positionSign;				// remove qty from this book used to make fill.
                                }
                            }
                        }
                    }
                    else if (this.TotalQty > 0)  // if we have a long postion
                    {
                        if (positionSign < 0) // If the incoming fill is a short position fill
                        {
                            // now add this fill to the position book
                            this.Add(qty * positionSign, price / qty);		// add to implied PositionBook.
                            isBookChanged = true;                           // now we can say that the book has changed
                            for (int i = 0; i < nBooks; ++i)				// loop thru each sub-book.
                            {
                                int qtyUsed = Convert.ToInt32(Math.Round(qty * weight[i]));	// unsigned qty to use from this book.
                                levelQty[i][level[i]] -= qtyUsed * positionSign;				// remove qty from this book used to make fill.
                            }
                        }
                        else // if the incoming fill is long position fill
                        {
                            if ((price / qty) <= BuyPrice91)
                            {
                                // now add this fill to the position book
                                this.Add(qty * positionSign, price / qty);		// add to implied PositionBook.
                                isBookChanged = true;                           // now we can say that the book has changed
                                for (int i = 0; i < nBooks; ++i)				// loop thru each sub-book.
                                {
                                    int qtyUsed = Convert.ToInt32(Math.Round(qty * weight[i]));	// unsigned qty to use from this book.
                                    levelQty[i][level[i]] -= qtyUsed * positionSign;				// remove qty from this book used to make fill.
                                }
                            }
                        }
                    }
                    else  // if we do not have any position
                    {
                        if (positionSign > 0)   // if the incoming fill is long position fill
                        {
                            if ((price / qty) <= BuyPrice91) // see if the price at which we are getting the fill is at price better than the Buy price specified
                            {
                                // now add this fill to the position book
                                this.Add(qty * positionSign, price / qty);		// add to implied PositionBook.
                                isBookChanged = true;                           // now we can say that the book has changed
                                for (int i = 0; i < nBooks; ++i)				// loop thru each sub-book.
                                {
                                    int qtyUsed = Convert.ToInt32(Math.Round(qty * weight[i]));	// unsigned qty to use from this book.
                                    levelQty[i][level[i]] -= qtyUsed * positionSign;				// remove qty from this book used to make fill.
                                }
                            }
                        }
                        else if (positionSign < 0) // If the incoming fill is a short position fill
                        {
                            if ((price / qty) >= SellPrice91) // see if the price at which we are getting the fill is at price better than the sell price specified
                            {
                                // now add this fill to the position book
                                this.Add(qty * positionSign, price / qty);		// add to implied PositionBook.
                                isBookChanged = true;                           // now we can say that the book has changed
                                for (int i = 0; i < nBooks; ++i)				// loop thru each sub-book.
                                {
                                    int qtyUsed = Convert.ToInt32(Math.Round(qty * weight[i]));	// unsigned qty to use from this book.
                                    levelQty[i][level[i]] -= qtyUsed * positionSign;				// remove qty from this book used to make fill.
                                }
                            }
                        }
                    }
                    */
                    if (positionSign > 0)   // if the incoming fill is long position fill
                    {
                        if ((price / qty) <= BuyPrice91) // see if the price at which we are getting the fill is at price better than the Buy price specified
                        {
                            // now add this fill to the position book
                            this.Add(qty * positionSign, price / qty);		// add to implied PositionBook.
                            isBookChanged = true;                           // now we can say that the book has changed
                            for (int i = 0; i < nBooks; ++i)				// loop thru each sub-book.
                            {
                                int qtyUsed = Convert.ToInt32(Math.Round(qty * weight[i]));	// unsigned qty to use from this book.
                                levelQty[i][level[i]] -= qtyUsed * positionSign;				// remove qty from this book used to make fill.
                            }
                        }
                    }
                    else if (positionSign < 0) // If the incoming fill is a short position fill
                    {
                        if ((price / qty) >= SellPrice91) // see if the price at which we are getting the fill is at price better than the sell price specified
                        {
                            // now add this fill to the position book
                            this.Add(qty * positionSign, price / qty);		// add to implied PositionBook.
                            isBookChanged = true;                           // now we can say that the book has changed
                            for (int i = 0; i < nBooks; ++i)				// loop thru each sub-book.
                            {
                                int qtyUsed = Convert.ToInt32(Math.Round(qty * weight[i]));	// unsigned qty to use from this book.
                                levelQty[i][level[i]] -= qtyUsed * positionSign;				// remove qty from this book used to make fill.
                            }
                        }
                    }

                }//if implied qty is non-zero.
            }//end while (shouldContinue)

            //
            // Remove fills from input books, if desired.
            //
            if ((removeFillsFromBooks) && (isBookChanged))  // go through this only if the book has changed 
            {
                for (int i = 0; i < nBooks; ++i)
                {
                    int[] levelQtyOriginal;
                    double[] levelPriceOriginal;
                    book[i].GetPriceQtyArrays(out levelQtyOriginal, out levelPriceOriginal, combineBestFillsFirst);
                    for (int iLevel = 0; iLevel < levelQtyOriginal.Length; ++iLevel)
                    {
                        //int qtyToAdd = ( levelQty[i][iLevel] - levelQtyOriginal[iLevel] ); // initial - final
                        //book[i].Add( qtyToAdd, levelPrice[i][iLevel] );
                        int qtyToRemove = (levelQtyOriginal[iLevel] - levelQty[i][iLevel]);
                        if (qtyToRemove != 0) book[i].RemoveAt(levelPriceOriginal[iLevel], qtyToRemove);
                    }//price level
                }//i
            }//if removeFillsFromBooks().

            // 
            // Exit.
            //
            return isBookChanged;
        }//end CombineBooks()
        //
        public bool CombineBooks(PositionBookOLD[] book, double[] weight, bool removeFillsFromBooks)
        {
            return CombineBooks(book, weight, removeFillsFromBooks, false);

        }//end CombineBooks().
        //
        //
        //
        // ****					Clear All					****
        //
        /// <summary>
        /// Clears all entries in this instance of Position Book.
        /// </summary>
        public void ClearAll()
        {
            m_List.Clear();
            TotalQty = 0;
            TotalTraded = 0;
            m_RealizedGains = 0.0;
        }//end ClearAll()
        //
        //
        //
        #endregion// end book manipulation


        #region Book viewing methods
        // *************************************************************************
        // ****						Book Viewing Methods						****
        // *************************************************************************
        //
        //
        //
        // ****                     Unrealized Dollar Gains                 ****
        //
        /// <summary>
        /// Returns the current portfolio liquidation value marked-to-market assuming
        /// the liquidation is done actively (hitting/lifting market) and perfect 
        /// liquidity (elasticity).  
        /// </summary>
        /// <param name="activeMarketPrices">ActivePrice[0,1] two-vector. Price to fill liquidation at for BUYSIDE=0 and SELLSIDE=1.</param>
        /// <returns>Market value of our current position.</returns>
        public double UnrealizedDollarGains(double[] activeMarketPrices)
        {
            double gain = 0.0;
            if (TotalQty != 0)
            {
                //
                // Determine liquidation price.
                //
                int tradeSign = Math.Sign(-TotalQty);  // if totalQty>0, we are long --> need to sell (sign = -1).
                int tradeSide = Utilities.QTMath.MktSignToMktSide(tradeSign);
                double tradePrice = activeMarketPrices[tradeSide];

                //
                // sum gain from each current fill.
                //
                int[] qtyArray;
                double[] priceArray;
                this.GetPriceQtyArrays(out qtyArray, out priceArray, false);
                for (int i = 0; i < qtyArray.Length; ++i)
                {
                    gain += (tradePrice - priceArray[i]) * qtyArray[i];
                }
            }//if we have a position.
            // Exit.
            return (gain * DollarPerPoint);
        }//end GetUnrealizedDollarGains().
		/// <summary>
		/// This computes unrealized gains assuming a simple, single exit price, usually a midprice, 
		/// for all the positions.  This is useful for quick estimates, and for strategies whose PnL
		/// is small compared to the bid/ask spread.
		/// </summary>
		/// <param name="midPrices"></param>
		/// <returns></returns>
		public double UnrealizedDollarGains(double midPrices)
		{
			double gain = 0.0;
			if (TotalQty != 0)
			{
				//
				// sum gain from each current fill.
				//
				int[] qtyArray;
				double[] priceArray;
				this.GetPriceQtyArrays(out qtyArray, out priceArray, false);
				for (int i = 0; i < qtyArray.Length; ++i)
				{
					gain += (midPrices - priceArray[i]) * qtyArray[i];
				}
			}//if we have a position.
			// Exit.
			return (gain * DollarPerPoint);
		}//end GetUnrealizedDollarGains().
        //
        //
        //
        //
        // ****						Get Price Qty Arrays						****
        //
        /// <summary>
        ///		Converts the entire book into two out arrays.  These arrays are
        ///	ordered in the usual manner with the "worst" price located at index 0.
        ///		
        /// </summary>
        public void GetPriceQtyArrays(out int[] qtyArray, out double[] priceArray)
        {
            this.GetPriceQtyArrays(out qtyArray, out priceArray, false);

        }//end GetPriceQtyArrays()
        //
        //
        //
        /// <summary>
        /// Main entry point for this method.
        /// </summary>
        /// <param name="qtyArray"></param>
        /// <param name="priceArray"></param>
        /// <param name="orderBestFillFirst">true and the arrays are order so that the best fill
        ///		(lowest buy / highest sale) is listed at the start of the array.  Otherwise, the worst
        ///		fill is listed first. 
        ///		</param>
        public void GetPriceQtyArrays(out int[] qtyArray, out double[] priceArray, bool orderBestFillFirst)
        {
            // Initialize output arrays.
            int length = m_List.Count;
            qtyArray = new int[length];
            priceArray = new double[length];
			int orderingOfFills;
			if (orderBestFillFirst)
				orderingOfFills = -1;		// reverse order of fills.
			else
				orderingOfFills = +1;		// usual ordering of fills - worst is first.

            //
            // Loop thru (and load) each entry in position book.
            //
            for (int priceIndex = 0; priceIndex < length; ++priceIndex)
            {
                int qty;
                double price;
                int listIndex = this.PriceIndexToListIndex(priceIndex,orderingOfFills);
                //if (orderBestFillFirst) { listIndex = (length - 1) - listIndex; }	// reverse order of listIndex.

                this.GetFillPriceQty(listIndex, out qty, out price);
                priceArray[priceIndex] = price;
                qtyArray[priceIndex] = qty;
            }//i
        }//end GetPriceQtyArrays().
        //
		// ****				GetPriceQtyLists				****
		//
		public void GetPriceQtyLists(ref List<int> qtyList, ref List<double> priceList, bool orderBestFillFirst)
		{
			int orderingOfFills;
			if (orderBestFillFirst)
				orderingOfFills = -1;		// reverse order of fills.
			else
				orderingOfFills = +1;		// usual ordering of fills - worst is first.

			//
			// Loop thru (and load) each entry in position book.
			//
			for (int priceIndex = 0; priceIndex < m_List.Count; ++priceIndex)
			{
				int qty;
				double price;
				int listIndex = this.PriceIndexToListIndex(priceIndex, orderingOfFills);
				this.GetFillPriceQty(listIndex, out qty, out price);
				priceList.Add( price );
				qtyList.Add( qty );
			}//priceIndex
		}//end GetPriceQtyLists().
		//
        //
        // ****						Get Qty()						****
        //
        /// <summary>
        /// Gets the signed qty of the position at the price index.
        /// </summary>
        /// <param name="priceIndex">position from inside the mkt.</param>
        /// <returns>signed quantity of position at this index in book.</returns>
        public int GetQty(int priceIndex)
        {
            int listIndex = this.PriceIndexToListIndex(priceIndex);
            if (listIndex < 0) { return 0; }		// handle possible errors.
            return (this.GetQtyAt(listIndex));
        }//end GetQty()
        //
        //
        // ****						Get Price()						****
        //
        /// <summary>
        /// See GetQty() for explanation.  This method returns the price associated with the
        /// qty returned by GetQty().
        /// </summary>
        /// <param name="priceIndex">See GetQty().</param>
        /// <returns>price of fill.</returns>
        public double GetPrice(int priceIndex)
        {
            int listIndex = this.PriceIndexToListIndex(priceIndex);
            if (listIndex < 0) { return 0; }		// handle possible errors.
            return (this.GetPriceAt(listIndex));
        }//end GetPrice()
        //
        //
        //
        //
        // ****						GetQtyByPrice()					****
        // 
        public int GetQtyByPrice(double price)
        {
            int tickPrice = Convert.ToInt32(Math.Round(price / SmallestPriceIncrement));
            int qty;
            if (m_List.ContainsKey(tickPrice))
            {
                int tickIndex = m_List.IndexOfKey(tickPrice);
                qty = (int)m_List.GetByIndex(tickIndex);
            }
            else
            {
                qty = 0;
            }
            return qty;
        }//end GetQtyByPrice().
        //
        //
        //
        // ****					GetQty ByPrice OrWorse()			****
        //
        /// <summary>
        /// Returns the sum of quantities at the target price, and all
        /// worse price levels.  That is, for a long position the quatity
        /// returned is the sum of fills at the price level "price" and 
        /// all HIGHER price levels.
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        public int GetQtyByPriceOrWorse(double price)
        {
            int qtyByPriceOrWorse = 0;			// qty at price or worse.
            int positionSign = Math.Sign(TotalQty);	// sign of position.
            // Loop thru each fill from worst to best.
            for (int priceIndex = 0; priceIndex < m_List.Count; ++priceIndex)
            {
                int listIndex = PriceIndexToListIndex(priceIndex);		// list index corresponding to this price index.
                double currentPrice = GetPriceAt(listIndex);			// price at this location.
                double p = Math.Round((currentPrice - price) / SmallestPriceIncrement) * positionSign;	// non-negative if price worse (or equal).
                if (p >= 0)
                {	// Current price level is worse than (or equal to) target price.
                    qtyByPriceOrWorse += GetQtyAt(listIndex);			// add onto qty.
                }
                else
                {	// Current price is better than target.
                    break;		// quit our search now.
                }
            }//priceIndex
            return qtyByPriceOrWorse;
        }//	GetQtyByPriceOrWorse().
        //
        //
        //
        // ****						ToString()						****
        //
        /// <summary>
        /// This returns a string representing the fill at the ith price level, 
        /// where i = priceIndex is counted from the worst fill price (priceLevel=0)
        /// to the best price priceLevel = (number of entries - 1).
        /// Output format is like "+5 @ 9645.5"
        /// </summary>
        /// <param name="priceIndex">User's index of a particular entry in the book.</param>
        /// <returns>a string denoting a fill and price.</returns>
        public string ToString(int priceIndex)
        {
            //
            // Convert priceIndex to internal index value.
            // Goal here is to label the worst fill with priceIndex of 0.
            // Worst fill is the "lowest short" or the "highest valued long" position.
            int listIndex = this.PriceIndexToListIndex(priceIndex);
            if (listIndex < 0) { return " * "; }	// on error return " * ".

            //
            // Build response.
            //
            System.Text.StringBuilder msg = new System.Text.StringBuilder(32);
            msg.AppendFormat("{0}@{1}", this.GetQtyAt(listIndex).ToString(), this.GetPriceAt(listIndex).ToString("+0.0##;-0.0##;0"));
            return msg.ToString();
        }//end ToString().
        //
        //
        //
        // ****						ToString()						****
        //
        /// <summary>
        /// This returns a string that appends all fills in book.
        /// This string is the correct format to be passed into the .AddAll(string) method.
        /// </summary>
        /// <returns>a string denoting all fills and prices in list separated by verticle bars "|".</returns>
        override public string ToString()
        {
            // Build response.
            System.Text.StringBuilder msg = new System.Text.StringBuilder(32 * (m_List.Count + 1));
            for (int iPriceIndex = 0; iPriceIndex < m_List.Count; iPriceIndex++)
            {
                msg.Append(this.ToString(iPriceIndex));
                if (iPriceIndex < m_List.Count - 1) { msg.Append(" / "); } // seperate entries with a verticle bar.
            }
            return msg.ToString();
        }//end ToString().
        //
        //
        //
        // ****						From String()						****
        //
        /// <summary>
        /// This converts string to a fill qty and price in format "+20 @ 9555.5", where
        /// the first number is an integer and the second is a double.  They are separated
        /// by an " @ ", note the blank spaces around the @-symbol.
        /// </summary>
        /// <returns></returns>
        public bool FromString(string fillString, out int fillQty, out double fillPrice)
        {
            //
            // Determine the position of @ sign. 
            //
            // "+10 @ 5.5" --> in this case, @-sign has a location of 4.
            //string[] fillInfo = fillString.Split("@".ToCharArray());
			string[] fillInfo = fillString.Split('@');
            bool isSuccess = true;
            fillQty = 0;
            fillPrice = 0;
            if (fillInfo.Length == 2)
            {
                try
                {
                    fillQty = int.Parse(fillInfo[0].Trim());
                    fillPrice = double.Parse(fillInfo[1].Trim());
                }
                catch
                {	// conversion failed.					
                    isSuccess = false;
                }
            }

            //
            // Exit
            //
            return isSuccess;
        }//end FromString()
        //
        //
		//
		//
		// ****					GetEventArgs				****
		//
		/// <summary>
		/// Returns the state of the position book.
		/// </summary>
		/// <returns></returns>
		public PositionBookEventArgs GetEventArgs()
		{
			PositionBookEventArgs eventArgs = new PositionBookEventArgs();
			this.GetPriceQtyLists(ref eventArgs.QtyList, ref eventArgs.PriceList, false);
			return eventArgs;
		}// GetEventArgs()
        //
        //
		//
		// ****					ProcessEventArgs()				****
		//
		public void ProcessEventArgs(PositionBookEventArgs eventArgs)
		{
			switch (eventArgs.EventType)
			{
				case PositionBookEventArgs.RequestType.Add:
					for (int i = 0; i < eventArgs.PriceList.Count; ++i)
						this.Add(eventArgs.QtyList[i], eventArgs.PriceList[i]);
					break;
				case PositionBookEventArgs.RequestType.Remove:
					for (int i = 0; i < eventArgs.PriceList.Count; ++i)
						this.RemoveAt(eventArgs.PriceList[i],eventArgs.QtyList[i]);
					break;
				//case PositionBookEventArgs.RequestType.DeSerialize:
				//	this.AddAll(eventArgs.SerializedBook);
				//	break;
				default:
					break;
			}//switch
		}//ProcessEventArgs()
		//
		//
		//
		//
        #endregion//end book view methods.


        #region Private Utilities
        // *************************************************************************************
        // ****								Private Utilities								****
        // *************************************************************************************
        //
        //
        //
        // ****					Add( ticks )				****
        //
        /// <summary>
        /// Internal utility for adding a qty/price position to the list.  Here, the price must be given 
        /// in terms of ticks, not dollars.
        /// </summary>
        /// <param name="quantity"></param>
        /// <param name="tickPrice"></param>
        /// <returns></returns>
        private int Add(int quantity, int tickPrice)
        {
            int qty = quantity;								// Qty at this key price.
            int gain = 0;									// realized gain in ticks.

            //
            // Handle position annihilations.
            //
            if (qty * TotalQty < 0)
            {	// Fill on opposite side as current position 
                // We are closing our position.  Annihilation needed.
                // Convention:  Always cancel with worst price (highest long, cheapest short).
                while (Math.Abs(qty) > 0 && (m_List.Count > 0))
                {
                    int oldIndex = 0;								// ptr to lowest short price.
                    if (TotalQty > 0) oldIndex = m_List.Count - 1;		// ptr to highest long price.
                    int oldQty = (int)m_List.GetByIndex(oldIndex);	// old position at (worst) price.
                    if ((oldQty + qty) * qty > 0)
                    {	// qty is bigger than oldQty.
                        // Calculate realized gain. (note signs!!)
                        // gain = - sum_{i} Q_i P_i.
                        gain -= oldQty * ((int)m_List.GetKey(oldIndex) - tickPrice);

                        // Annihilate oldQty fills (entire) price level, 
                        // adjust remaining qty, and continue to annhilate.
                        m_List.RemoveAt(oldIndex);		// completely remove this old fill.
                        qty += oldQty;						// qty remaining to annihilate.
                    }
                    else
                    {	// qty is (equal to or) smaller than oldQty.
                        // Calculate realized gain. (note signs!!)
                        gain += qty * ((int)m_List.GetKey(oldIndex) - tickPrice);

                        // Partially annihilate this level and stop.
                        oldQty += qty;
                        m_List.SetByIndex(oldIndex, oldQty);
                        qty = 0;
                        // if oldQty is identically zero, just remove it completely!
                        if (oldQty == 0) m_List.RemoveAt(oldIndex);
                    }
                }//while end
            }//if annihilation

            //
            // Add remaining fills to list.
            //
            if (qty != 0)
            {
                int index = m_List.IndexOfKey(tickPrice);		// See if already a fill at this price.
                if (index > -1)
                {	// Increment already existing entry at this price.
                    qty += (int)m_List.GetByIndex(index);
                    m_List.SetByIndex(index, qty);
                }
                else
                {	// add a new entry for this price.
                    m_List.Add(tickPrice, qty);
                }
            }

            //
            // Update current total position and exit.
            //
            TotalQty += quantity;
            return gain;
        }//end Add()
        //
        //
        // ****					PriceIndex to ListIndex			****
        //
        /// <summary>
        /// This internal private utility takes an worst index and converts it to our internal index.
        /// </summary>
		/// <param name="priceIndex">Usually this runs from 0 (worst fill) to N-1 (best fill).</param>
		/// <param name="fillOrdering">+1 indicates usual ordering from worst to best fill, -1 indicates reverse order.</param>
        /// <returns>corresponding internal list index, or -1 if error.</returns>
        private int PriceIndexToListIndex(int priceIndex, int fillOrdering)
        {	//
            // Convert priceIndex to internal list index value.
            // Goal here is to label the worst fill with priceIndex of 0 - when fillOrder = +1
            // Worst fill is the "lowest short" or the "highest valued long" position.
            // That is, the "priceIndex" of 0 is closest to (or deepest into) the other-side of the mkt.

            //
            // Validate price Index value.
            //
            if (TotalQty == 0)
            {	// there are no positions in this book at all.
                return -1;				// return error value.
            }
            if ((priceIndex >= m_List.Count) || (priceIndex < 0))
            {	// 
                return -1;				// return ERROR value.
            }

            // 
            // Determine listIndex value associated with this price index.
            //
            int listIndex = 0;
			if (TotalQty * fillOrdering > 0)
            {	// we have a long position.
                listIndex = (m_List.Count - 1) - priceIndex;
            }
            else
            {	// we have a short position.
                listIndex = priceIndex;
            }
            return listIndex;
        }//end WorstToIndex()
		//
		//
		private int PriceIndexToListIndex(int priceIndex)
		{
			return PriceIndexToListIndex(priceIndex, +1);
		}
        //
        //
        //
        // ****						Get Fill PriceQty()						****
        /// <summary>
        /// Returns the price of the ith fill.
        /// </summary>
        /// <param name="listIndex">index (base 0) of the fill to be returned.</param>
        /// <param name="qty">Qty to of the ith fill.</param>
        /// <param name="price"></param>
        /// <returns>price of the ith fill.</returns>
        private bool GetFillPriceQty(int listIndex, out int qty, out double price)
        {
            // Validate arguments.
            qty = 0;
            price = 0.0d;
            if (listIndex < 0 || listIndex > (m_List.Count - 1)) { return false; }

            // Load values and exit.
            price = Convert.ToDouble((int)m_List.GetKey(listIndex)) * SmallestPriceIncrement;
            qty = (int)m_List.GetByIndex(listIndex);
            return true;

        }//end GetFillPriceQty
        //
        //
        //
        // ****					Get Price At				****
        //
        private double GetPriceAt(int listIndex)
        {
            return Convert.ToDouble((int)m_List.GetKey(listIndex)) * SmallestPriceIncrement;
        }//end GetPriceAt()
        //
        //
        // ****					Get Qty At					****
        //
        private int GetQtyAt(int listIndex)
        {
            return ((int)m_List.GetByIndex(listIndex));
        }//end GetQtyAt()
        //
        //
        //
        //
        #endregion//end private utilities



    }//end PositionBook class




}//namespace

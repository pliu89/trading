using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace UV.Lib.Utilities
{
    public class QTMath
    {
        /// <summary>
        /// Empty default constructor -- not used in a static class.
        /// </summary>
        public QTMath() { }

        #region Basic Static Functions
        //
        //
        // *************************************************************
        // ****					    IsNearEqual   					****
        // *************************************************************
        /// <summary>
        /// simple way to compare two doubles within an acceptable fractional
        /// tolerance.  Typically something very small like .0001.
        /// </summary>
        /// <param name="dbll"></param>
        /// <param name="dbl2"></param>
        /// <param name="fractionalTolerance"></param>
        /// <returns></returns>
        public static bool IsNearEqual(double dbll, double dbl2, double fractionalTolerance)
        {
            double allowableDiff = Math.Abs(fractionalTolerance * dbll);
            return Math.Abs(dbll - dbl2) <= allowableDiff;
        }
        //
        //
        //
        // *************************************************************
        // ****					Power(a,n) = a^n					****
        // *************************************************************
        public static double Power(int x, int exponent)
        {
            if (exponent == 0)
            {
                return 1;
            }
            else if (exponent < 0)
            {
                return (1.0 / Power(x, Math.Abs(exponent)));
            }
            else
            {
                double result = x * Power(x, (exponent - 1));
                return result;
            }
        }//end Power().
        //
        public static double Power(double x, int exponent)
        {
            if (exponent == 0)
            {
                return 1;
            }
            else if (exponent < 0)
            {
                return (1.0 / Power(x, Math.Abs(exponent)));
            }
            else
            {
                double result = x * Power(x, (exponent - 1));
                return result;
            }
        }//end Power().
        //
        //
        //
        //
        //
        // *************************************************************
        // ****					Heaviside Function					****
        // *************************************************************
        /// <summary>
        ///		The Heaviside "Theta" function:   
        ///			Theta(x) = +1	if x > 0, 
        ///						0	otherwise.		
        ///						
        ///		Note: I choose to use Theta(x=0) = 0, so that the support 
        ///     for this function is the *open* set, x > 0.  This is a critical
        ///     property if one is to use this function for a logical switch.
        ///		The form I use is
        ///		
        ///		Theta(x) = ( sign(x)^2  + sign(x) )/2
        ///						
        ///		Derivation: Recall the function Math.sign(x) = {sign of x, or zero when x = 0}.
        ///		Using the fact that s(x)^2 = +0 if x = 0, and +1 otherwise, we can write
        ///			Theta(x) = (1 + s(x))/2 - (1 - s(x)^2 ) / 2  
        ///					 =  1 + ( s(x) - s(x)^2 )/2 .
        /// </summary>
        /// <param name="x">Independent variable.</param>
        /// <returns>The heaviside Theta ("step") function; 
        ///     f(x) = +1, if x > 0, and f(x) = 0, for all other x.</returns>
        public static int Heaviside(double x)
        {
            int s = Math.Sign(x);
            return (s + s * s) / 2;
        }
        public static int Heaviside(decimal x)
        {
            int s = Math.Sign(x);
            //return ( 1 + s*(1 - s)/2 );
            return (s + s * s) / 2;
        }
        public static int Heaviside(int x)
        {
            int s = Math.Sign(x);
            //return ( 1 + s*(1 - s)/2 );
            return (s + s * s) / 2;
        }
        //
        // *********************************************************
        // ****             Generalized Heaviside               ****
        // *********************************************************
        //
        /// <summary>
        /// This generalization of the the Canonical Heaviside function, is 
        /// defined as
        ///         Theta(x, w) =   0       if x is less than or equal to zero, 
        ///                         +1      if x >= w, 
        ///                         x/w     if x is on (0,w) domain.
        /// Thus, its a step function with a linear transition period between x=0 
        /// and +w.
        /// 
        /// </summary>
        /// <param name="x">function argument</param>
        /// <param name="width">width of transition region, zero or non-zero.
        ///     For zero width, the result is the canonical heaviside function.</param>
        /// <returns>Heaviside generalization, with finite width.</returns>
        public static double Heaviside(double x, double width)
        {
            // Validate width argument.
            double zeta = (x / width);      // convert to dimless qty.
            int heavisideBase = Heaviside(x);
            if (double.IsNaN(zeta)) { return heavisideBase; } // if width = zero -> return basic Heaviside.
            // Compute the function.
            double O = Math.Min(1.0, Math.Abs(zeta));
            return (heavisideBase * O);
        }//end Heaviside().
        //
        //
        // *********************************************************
        // ****             Generalized Heaviside               ****
        // *********************************************************
        //
        /*
        /// <summary>
        /// This is one possible generalization of the heaviside function, expanding
        /// its usage while maintaining some important features I often rely on.
        /// First, its definition is:
        /// 
        /// Theta(x, x_HW, a_{-}, a_{+}, a_{0}) = a_{sgn(x)} * min(1, |x|/x_HW) + a_{0},
        /// 
        /// where x is the real-valued argument, x_HW is the half-width of the step
        /// transition, a_{+/-} is the function limit at positive/negative values of x, 
        /// and a_{0} is the value of the function at the origin (the "shift amount").
        /// 
        /// Properties:
        ///     Limits:
        ///     1)  lim_{x -> -infty} Theta(x) =  a_{0} + a_{-}
        ///     2)  lim_{x -> +infty} Theta(x) =  a_{0} + a_{+}
        ///     3)  lim_{x -> 0     } Theta(x) =  a_{0}
        /// 
        ///     4) Is monotonic whenever a_{+}, a_{-} have different signs!
        ///         If they have same sign, then Theta(x) has weird looking dip at origin.
        /// </summary>
        /// <param name="x">Independent variable of the function.</param>
        /// <param name="halfWidth">Non-zero half-width of the transition region around origin.</param>
        /// <param name="a_minus">Desired negative limit of function.</param>
        /// <param name="a_plus">Desired positive limit of function.</param>
        /// <param name="a_shift">Overall shift in function.</param>
        /// <returns>A generalization of the Heaviside (or "step") function.</returns>
        
        public static double Heaviside(double x, double halfWidth,
            double a_minus, double a_plus, double a_shift)
        {
            int E = Heaviside(x);         
            double coeff = a_plus * E + a_minus * (1 - E);
            double Theta = a_shift + Math.Min(1, Math.Abs(x / halfWidth)) * coeff;
            return Theta;
        }//end Heaviside().
         */
        //
        //
        // *************************************************
        // ****				TruncateDomain()			****
        // *************************************************
        /// <summary>
        /// This function returns x if x in the domain [a,b]. 
        /// If x is below this domain, function returns lower.
        /// If x is above this domain, function returns upper; that is, 
        /// 
        ///						a		if x less than a, 
        ///	T(x,a,b)	=		x		if x is greater than a, less than b, 
        ///						b		if x is greater than b.   
        /// 
        ///		Note: Returns upper when upper is less than lower. (This is junk)
        /// </summary>
        /// <param name="x">value to be truncated</param>
        /// <param name="lower">lower bound of domain</param>
        /// <param name="upper">upper bound of domain</param>
        /// <returns>new value x' that is in [lower,upper].</returns>
        public static double TruncateToDomain(double x, double lower, double upper)
        {
            x = Math.Max(x, lower);
            x = Math.Min(x, upper);
            return x;
        }//end Truncate Domain()
        public static decimal TruncateToDomain(decimal x, decimal lower, decimal upper)
        {
            x = Math.Max(x, lower);
            x = Math.Min(x, upper);
            return x;
        }//end Truncate Domain()
        public static int TruncateToDomain(int x, int lower, int upper)
        {
            x = Math.Max(x, lower);
            x = Math.Min(x, upper);
            return x;
        }//end Truncate Domain()
        //
        //
        //
        //
        // *********************************************************
        // ****					MountainRange					****
        // *********************************************************
        /// <summary>
        ///		Consider the piece-wise linear function y(E) such that it looks like the jagged mountain top.
        ///	This function does allow for a step-like behavior at the first region, then it is continuous although
        ///	not differentiable at the boundaries.
        ///	
        ///					= 0															for E in (-infty,x0),
        ///					= h0 + (E-x0) * (h1-h0)/(x1-x0),							for E in (x0,x1),
        ///					.
        ///					.
        ///					.
        ///					= h_i + (E-x_i) * (h_{i+1} - h_{i}) / (x_{i+1} - x_{i}),	for E in (x_i, x_{i+1}),
        ///					.
        ///					.
        ///					= h_{n-1}													for E in (x_{n-1}, +infty).
        ///					
        ///	The user must supply the n-dimensional vectors x[] and height[].
        ///	The simple version is here explicitly, 
        /// </summary>
        /// <param name="E"></param>
        /// <param name="x0"></param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <param name="lastResult">If last result > 0 then turn on historesis effect.</param>
        /// <returns></returns>
        // Simplest non-generic overloading
        public static double MountainRange(double E, double lastResult, double x0, double x1, double x2, double h1, double h2)
        {	//
            // Validate input variables.
            //
            if (x1 >= x2) { return 0.0d; }

            //
            // Build mountain sections.
            // 
            double m = (h2 - h1) / (x2 - x1);
            double y0 = Heaviside(E - x0) * Heaviside(x1 - E) * Heaviside(lastResult) * h1;
            double y1 = Heaviside(E - x1) * Heaviside(x2 - E) * (m * (E - x1) + h1);
            double y2 = Heaviside(E - x2) * h2;
            // Exit.
            return (y0 + y1 + y2);
        }//end MountainRange().
        //
        //
        //
        // *****************************************************************
        // ****                     Skyline                             ****
        // *****************************************************************
        //
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="nodes">nodes = {x_i}</param>
        /// <param name="functionValues">Value of function at nodes g_i = g(x_i).</param>
        /// <param name="interpolationOrder"></param>
        /// <returns></returns>
        public static double Skyline(double x, double[] nodes, double[] functionValues,
            int interpolationOrder)
        {
            int floor = GetFloor(x, nodes);
            // Handle special cases when x is either less than all nodes 
            // or greater than all nodes.
            if (floor < 0)
            {   // all nodes greater than x.
                return functionValues[0];   // return left-most value.
            }
            else if (floor >= (nodes.Length - 1))
            {
                return functionValues[functionValues.Length - 1];   // return right-most value.
            }
            else
            {
                double gfloor = functionValues[floor];
                double g = 0.0;
                switch (interpolationOrder)
                {
                    case 1:         // linear interpolation (mountain range).
                        double xfloor = nodes[floor];
                        double slope = (functionValues[floor + 1] - gfloor) / (nodes[floor + 1] - xfloor);
                        g = slope * (x - xfloor) + gfloor;
                        break;
                    default:        // constant interpolation (skyline).
                        g = gfloor;
                        break;
                }//end switch.
                //
                // Exit.
                //
                return g;
            }
        }//Skyline().
        //
        //
        // *****************************************************************
        // ****                     Get Floor                           ****
        // *****************************************************************
        /// <summary>
        /// GetFloor() returns the index i of first array element nodes[i] 
        /// that is less than the given value x, and nodes[i+1] is greater than.
        /// GetFloor() returns:
        ///		
        ///  	smallest i >=0,  s.t. nodes[i+1] > x > nodes[i]   
        /// or
        ///		i=-1, if no such i exists.
        ///		
        /// Notes: 
        ///	1)	If the elements of nodes[] are monotonic increasing with i 
        ///		(that is, nodes[i+1] > nodes[i] for all i),
        ///		then this is just the usual definition of a floor function.  It returns 
        ///		the value immediately below the argument x.
        /// 2)  However, if elements of nodes[] are not monotonic increasing then 
        ///		this returns largest index i s.t. x >= {nodes[j] for all j LE i}.
        ///	
        /// </summary>
        /// <param name="x">point of interest</param>
        /// <param name="nodes">nodes[i]</param>
        /// <returns>
        ///     index i  s.t. (x-x_j) >= 0 for all j less than or equal to i .
        /// </returns>
        public static int GetFloor(double x, double[] nodes)
        {
            // New method.
            int i = 0;
            while ((i < nodes.Length) && (x >= nodes[i]))
            {	// Increment ptr while the next node is not bigger than x.
                i++;
            }//wend
            // Exit.
            return (i - 1);
        }//end GetFloor().
        //
        //
        //
        public static int GetFloor(int x, int[] nodes)
        {
            int i = 0;
            while ((i < nodes.Length) && (x > nodes[i]))
            {	// Increment ptr while the next node is not bigger than x.
                i++;
            }//wend
            // Exit.
            return (i - 1);
        }//end GetFloor().
        //
        //
        //
        //
        //
        // *****************************************************************
        // ****						Is Zero Vector						****
        // *****************************************************************
        /// <summary>
        ///		Given a single-dimensional array, this routine checks to see if any
        ///	elements are non-zero.  It returns a true if ALL elements are zero.
        /// </summary>
        /// <param name="x">single-dimensional column vector.</param>
        /// <returns></returns>
        public static bool IsZeroVector(int[] x)
        {
            for (int i = 0; i < x.Length; ++i)
            {
                if (x[i] != 0) { return false; }
            }//i
            return true;
        }//end IsZeroVector()
        public static bool IsNotZeroVector(int[] x)
        {
            for (int i = 0; i < x.Length; ++i)
            {
                if (x[i] != 0) { return true; }
            }//i
            return false;
        }//end IsZeroVector()
        public static bool IsNotZeroVector(double[] x)
        {
            // use default epsilon in this.
            return IsNotZeroVector(x, 0.000001);
        }//end IsZeroVector()
        public static bool IsNotZeroVector(double[] x, double eps)
        {
            if (eps < 0) { return false; }
            for (int i = 0; i < x.Length; ++i)
            {	// search thru each component...
                if (x[i] > eps) { return true; } // if even one component is non-zero, then return true.
            }//i
            return false;
        }//end IsZeroVector()
        //
        #endregion	// end of public static functions.

        #region Price Rounding and Equality
        //
        // *****************************************************************
        // ****						Round Price Safely 					****
        // *****************************************************************
        //
        /// <summary>
        ///		This method takes a price and rounds it to the SAFER tick price. Here, "safer"
        ///		means the price is rounded further away from the market. Therefore, sell side prices
        ///		are rounded up, and buy prices are rounded down to the nearest ticks.
        ///		
        ///		In general, a sloppy price can be writen as an exact price with an small error "eps", 
        ///				price = iPrice * minTickSize	+  eps
        ///		where iPrice is an integer, and  minTickSize > eps.  Error is smaller than eps.  Then, 
        ///		we want to return the safer trade price of 
        ///		
        ///			(iPrice + Theta[eps] )*minTickSize			for Sells,
        ///			
        ///			(iPrice - Theta[-eps])*minTickSize			for Buys.
        ///			
        ///		This formula says 
        ///			for Sells: if eps > 0, then we will add another tick to the price.
        ///			for Buys: if 0 > eps, then we lower the price by another tick.
        ///			
        ///		Using the above relation for the price, we find that (since tickSize > |eps|)
        ///			iPrice = Round(  price / minTickSize )
        ///			eps = price - iPrice * minTickSize
        ///		
        /// </summary>
        /// <param name="price">imperfect price to round to nearest (safe) tick price.</param>
        /// <param name="mktSideSign">Selling price = -1, Buying prices = +1</param>
        /// <param name="minTickSize">0.5 for ED.</param>
        /// <returns></returns>
        public static double RoundPriceSafely(double price, int mktSideSign, double minTickSize)
        {
            double safePrice = 0.0d;
            try
            {
                double nearestTickPrice = minTickSize * Math.Round(price / minTickSize);	// nearest tick price.
                double epsilon = price - nearestTickPrice;		// small error between price and nearest tick price.
                epsilon = -mktSideSign * epsilon;				// make sign denote round off direction.
                double safetyCushion = -mktSideSign * QTMath.Heaviside(epsilon) * minTickSize;
                safePrice = nearestTickPrice + safetyCushion;
            }
            catch (Exception e)
            {
                Console.WriteLine(" errSrc:" + e.Source + " errMsg:" + e.Message + " Stack:" + e.StackTrace.ToString(), "QTMath.RoundPriceSafely");
            }
            return safePrice;
        }//end RoundPrice()
        //
        //
        // *****************************************************************
        // ****						RoundToSafeIPrice 					****
        // *****************************************************************
        /// <summary>
        /// Caller would like to turn a double price into an integerized price using "safe" rounding
        /// (rounding away from the market)
        /// </summary>
        /// <param name="price"></param>
        /// <param name="mktSide"></param>
        /// <param name="tickSize"></param>
        /// <returns></returns>
        public static int RoundToSafeIPrice(double price, int mktSide, double tickSize)
        {
            int mktSign = MktSideToMktSign(mktSide);
            return mktSign * (int)System.Math.Floor(mktSign * price / tickSize);    // integer price to quote- safely rounded away.
        }

        //
        //
        //
        // *****************************************************************
        // ****						Is Price Equal  					****
        // *****************************************************************
        /// <summary>
        /// Simple Check to See if two prices are equal
        /// </summary>
        /// <param name="price1"></param>
        /// <param name="price2"></param>
        /// <param name="tickSize"></param>
        /// <returns> bool </returns>
        public static bool IsPriceEqual(double price1, double price2, double tickSize)
        {
            return Math.Abs(price1 - price2) <= tickSize / 2.0;
        }
        //
        #endregion Price Rounding and Equality

        #region Statistical Functions
        //
        //
        // *************************************************************
        // ****						Combination(n,r)				****
        // *************************************************************
        /// <summary>
        ///	This function is the classic combination function:  The number of ways from 
        ///	"n" given objects one can choose r of them.  In other words this gives the numbers 
        ///	in Pascals triangle.  That is, (n r) gives the rth vaule of the nth row (where both 
        ///	n and r start at zero:
        ///						 r=0,  r=1, r=2, etc
        ///	n=0					1	  /    /
        ///	n=1				1		1     /
        ///	n=2			1		2		1
        ///	n=3		1		3		3		1
        ///	n=4	1		4		6		4		1
        ///	etc.				
        ///	
        ///	if r less than n/2, then we compute  (n r) = ( n!/(n-r)! )  / r! = Fact(n,n-r) / Fact(r,0)
        ///	if r >= n/2 then we compute (n r) = (n!/r!) / (n-r)! = Fact(n,r)/Fact(n-r,0).  This helps 
        ///	cancel out the largest numbers first using Fact(n,r) when n and r are large.
        ///	
        /// </summary>
        /// <param name="n">total number of objects n>=0.</param>
        /// <param name="r">number of objects to choose r>0.</param>
        /// <returns>Number of combinations (n r)</returns>
        public static int Combination(int n, int r)
        {
            if ((n <= 0) || (r <= 0)) { return 1; }	// return 1 for negative numbers of if either are zero.
            if (r > n) { return 0; }

            // Compute combination.
            if (r < n / 2)
            {
                return Factorial(n, n - r) / Factorial(r, 0);
            }
            else
            {
                return Factorial(n, r) / Factorial(n - r, 0);
            }
        }
        //
        // *************************************************************
        // ****						Factorial(n)					****
        // *************************************************************
        /// <summary>
        ///	Return n!, the factorial of n, where n! = n*(n-1)*(n-2)*....(2)*(1).
        /// </summary>
        /// <param name="n">an integer.</param>
        /// <returns>n!</returns>
        public static int Factorial(int n)
        {
            return Factorial(n, 0);
        }
        /// <summary>
        ///	Overload for factorial, this returns n!/r!.  The ways its computed is by iteration.
        ///	Fact(n,r)	= (n)(n-1)(n-2)(n-3) . . . (n-(r-3))(n-(r-2))(n-(r-1))		= n!/r!
        ///				
        ///					|	n Fact(n-1,r)		if (n>r), 
        ///				=	|
        ///					|	1					otherwise.
        /// </summary>
        /// <param name="n">a positive integer.</param>
        /// <param name="r">another positive integer.</param>
        /// <returns>The ratio (n! / r!)</returns>
        public static int Factorial(int n, int r)
        {
            if (n > r)
            {
                return (n * Factorial(n - 1, r));
            }
            else
            {
                return 1;
            }
        }//end Fact(n,r)
        //
        //
        // *************************************************************
        // ****						AlphaToPeriod   	    		****
        // *************************************************************
        /// <summary>
        /// Converts Alpha to a integerized lookback period
        /// </summary>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public static int AlphaToPeriod(double alpha)
        {
            int lookBackPeriod = (int)(-2 * Math.Log(2) / Math.Log(alpha));
            return lookBackPeriod;
        }
        //
        // *************************************************************
        // ****						PeriodToAlpha   	    		****
        // *************************************************************
        /// <summary>
        /// Converts Period (lifetime) to an alpha for a give timeScale (update frequency)
        /// </summary>
        /// <param name="lifeTime"></param>
        /// <param name="timeScale"></param>
        /// <returns></returns>
        public static double PeriodToAlpha(int lifeTime, int timeScale)
        {
            return System.Math.Exp(-0.69314718056 * timeScale / lifeTime);
        }
        #endregion

        #region Logical and Bit Functions
        //
        //
        //
        public static int SetFlags(int flags, int flagsToChange, bool newFlagValue)
        {
            int offMask = ~flagsToChange;           // all bits on except those we want to change.
            int offState = flags & offMask;         // turn off bits that we will change.
            int finalFlags = offState;
            if (newFlagValue) { finalFlags = finalFlags | flagsToChange; }  // turn on those bits, if desired.
            return finalFlags;
        }
        //
        //
        #endregion

        #region Portfolio and vector functions
        // *************************************************************************
        // ****						GetCompleteStrategies()						****
        // *************************************************************************
        /// <summary>
        ///		Given a portfolio and a vector of weights, this returns the signed number of 
        ///	*complete* strategies that can be extracted from the position.  Associated with 
        ///	this is the "excess portfolio" (not computed here).
        ///	Note:
        ///	1.	This routine does not change the values in position[]!
        /// This routine assumes there is only one strategy; i.e., S = S' = 1.
        /// </summary>
        /// <param name="position">portfolio in leg-space to be checked.</param>
        /// <param name="weights">weights in leg-space.</param>
        /// <returns>signed number of complete strategies that can be pulled out.</returns>
        public static int GetCompleteStrategies(int[] position, double[] weights)
        {
            //
            // Estimate the number of strategies
            //
            int nLegs = position.Length;
            double[] nStrats = new double[nLegs];
            for (int leg = 0; leg < nLegs; ++leg)// Ask each leg how many strategies they represent.
            {
                nStrats[leg] = Convert.ToDouble(position[leg]) / weights[leg];	// signed number of strategies.
            }//leg
            // Verify that all signs are the same and compute maximum number of strategies we hold.
            double number = Math.Abs(nStrats[0]);		// largest number of strategies we are holding.
            for (int leg = 1; leg < nLegs; ++leg)			// skip zeroth leg.
            {
                if (nStrats[leg] * nStrats[0] <= 0)
                {
                    return 0;	// positions must be of a consistent side, otherwise return 0.
                }
                number = Math.Min(number, Math.Round(Math.Abs(nStrats[leg])));	// smallest number, but number >= 0 always. 
            }//leg
            // Exit.
            return Convert.ToInt16(Math.Floor(number)) * Math.Sign(nStrats[0]);
        }//GetCompleteStrategies
        /// <summary>
        ///		This overloading also returns sets an excess quantity.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="weights"></param>
        /// <param name="excess"></param>
        /// <returns></returns>
        public static int GetCompleteStrategies(int[] position, double[] weights, out int[] excess)
        {
            int nStrategies = GetCompleteStrategies(position, weights);
            int nLegs = position.Length;
            excess = new int[nLegs];

            // Load excess.
            for (int leg = 0; leg < nLegs; ++leg)
            {
                excess[leg] = position[leg] - Convert.ToInt16(Math.Round(nStrategies * weights[leg]));
            }//leg
            return nStrategies;
        }//GetCompleteStrategies
        //
        //
        // *****************************************************************************
        // ****						GetMaximalNumberOfStrategies					****
        // *****************************************************************************
        /// <summary>
        ///		This is the complement method for GetCompleteStrategies().  This method returns the
        ///		maximal number of strategies implied from the portfolio[] handed to this routine.
        ///		For example:
        ///			Consider a butterfly strategy with weights  (1  -2  1).  If the 
        ///				position[] = (0  -2  0), 
        ///			then the maximal number of butterflies is Max_{leg}(  | position[leg] / w[leg] | }, 
        ///			and the sign of this is Sign(  position[leg]/w[leg] ).
        ///			In this case the number returned would be -1.
        ///			
        /// </summary>
        /// <param name="position">A portfolio from which the maximal number of strategies is
        ///		to be extracted.</param>
        /// <param name="weights">see above</param>
        /// <returns>The maximal number of strategies implied by this position[].</returns>
        public static int GetMaximalStrategies(int[] position, double[] weights)
        {
            //
            // Find maximal unsigned number of strategies.
            //
            double signedMax = 0;
            double absMax = 0;
            double x;
            int nLegs = position.Length;
            for (int leg = 0; leg < nLegs; ++leg)
            {
                x = (position[leg] / weights[leg]);	// number of strategies implied by this leg.
                if (Math.Abs(x) > absMax)
                {	// the absolute magnitude of this leg is the biggest found so far.
                    signedMax = x;					// store the quantity we will return.
                    absMax = Math.Abs(signedMax);	// store the absolute max for easy comparison.
                }
            }//leg

            //
            // Round off result and return.
            //
            signedMax = Math.Sign(signedMax) * Math.Floor(Math.Abs(signedMax));
            return (Convert.ToInt16(signedMax));
        }//end GetMaximalStrategies().	
        //
        /// <summary>
        ///		This overloading also returns sets an excess quantity.
        /// </summary>
        /// <param name="position">see above</param>
        /// <param name="weights"></param>
        /// <param name="excess">see above</param>
        /// <returns></returns>
        public static int GetMaximalStrategies(int[] position, double[] weights, out int[] excess)
        {
            int nStrategies = GetMaximalStrategies(position, weights);
            int nLegs = position.Length;
            excess = new int[nLegs];

            // Load excess.
            for (int leg = 0; leg < nLegs; ++leg)
            {
                excess[leg] = position[leg] - Convert.ToInt16(Math.Round(nStrategies * weights[leg]));
            }//leg
            return nStrategies;
        }//GetCompleteStrategies
        //
        //
        //
        // *****************************************************************************
        // ****						CalculateDripQty            					****
        // *****************************************************************************
        /// <summary>
        /// Calculate what our absolute value drip qty should be given based on our dripQty, totalQty (DesiredQTY), and our current positon.
        /// </summary>
        /// <param name="dripQty"></param>
        /// <param name="totalQty"></param>
        /// <param name="currentPos"></param>
        /// <returns>absolute value drip qty to work. 0 if not qty to be worked.</returns>
        public static int CalculateDripQty(int dripQty, int totalQty, int currentPos)
        {
            if (dripQty < 0)
                throw new Exception(string.Format("Calculate Drip Qty: Drip Qty Must Be a Positive Number! Current Value = {0}", dripQty));

            int absoluteRemainingNeededQty = Math.Abs(totalQty) - Math.Abs(currentPos);  // calculate how many more we need 
            if (absoluteRemainingNeededQty > 0 && absoluteRemainingNeededQty >= dripQty)
                return dripQty * Math.Sign(totalQty);
            else if (dripQty > absoluteRemainingNeededQty)  // if we don't need the full amount of our drip qty only return the remainder.
                return absoluteRemainingNeededQty * Math.Sign(totalQty);
            else                                            // if we are here, we have our full position and don't want to work anything.
                return 0;
        }
        #endregion//end portfolio functions

        #region Market sides
        // *****************************************************************
        // ****			Mkt Sign, Side and String conversions			****
        // *****************************************************************
        //
        //
        // Mkt Side/Sign:
        public const int BidSide = 0;
        public const int AskSide = 1;

        public const int BidSign = +1;
        public const int AskSign = -1;

        //
        // Passive Trade Side/Sign:
        public const int BuySide = 0;
        public const int SellSide = 1;
        public const int LastSide = 2;
        public const int UnknownSide = 3;

        public const int BuySign = +1;
        public const int SellSign = -1;

        //
        //	This collection of methods convert indexes that give the market side into each other.
        /// <summary>
        /// Given the "market side" (0 for buy, 1 for sell), this method returns the corresponding 
        /// +1 for buy, -1 for sell.
        /// Note: Assumes side=0 "bid" and side=1 "ask". In some places, Navaid has been known to use
        /// 0 for asks and 1 for bids so be careful!
        /// </summary>
        /// <param name="side">side of market 0=bid, 1=ask.</param>
        /// <returns> +1 for buys / -1 for shorting </returns>
        public static int MktSideToMktSign(int side)
        {
            return (1 - 2 * side);
        }
        public static int MktSideToOtherSide(int side)
        {
            return (side + 1) % 2;
        }
        /// <summary>
        ///	Given the "market sign" +1 for buys and -1 for sells, this method returns
        ///	a 0 and 1, respectively.
        /// </summary>
        /// <param name="sign">market sign +1=bid, -1=ask.</param>
        /// <returns>
        ///		0 if sign is +1, 1 if sign is -1.
        /// </returns>
        public static int MktSignToMktSide(int sign)
        {
            return (QTMath.Heaviside(-sign));
        }
        public static int MktSignToMktSide(double sign)
        {
            return (QTMath.Heaviside(-sign));
        }
        //
        // 
        // Active Mkt Side:
        //	Input:			Output:			Meaning:
        //	+1				1				actively buy from the offers.
        //  -1				0				actively sell to the bidders.
        public static int MktSignToActiveMktSide(int sign)
        {
            return (QTMath.Heaviside(sign));
        }
        public static int MktSignToActiveMktSide(double sign)
        {
            return (QTMath.Heaviside(sign));
        }
        //
        //
        //
        // 
        //
        /// <summary>
        /// This method when provided a particular trade BuySide or SellSide
        /// returns the "ActiveSide" value that should be handed to ActivePrice[side].
        /// For example:  Say a BuySide (side=0) trade that is active will be placed 
        /// at the OFFER (refered to as side=1).
        /// </summary>
        /// <param name="tradeSide">Buy (BuySide) or Sell (SellSide).</param>
        /// <returns>The side of the market corresponding to doing the
        /// desired trade actively.
        /// </returns>
        public static int MktSideToActiveMktSide(int tradeSide)
        {
            return ((tradeSide + 1) % 2);
        }
        //
        /// <summary>
        ///		Given the market sign (+1 bid/-1 ask), this returns the 
        ///	string "B" or "S".
        /// </summary>
        /// <param name="sign"></param>
        /// <returns></returns>
        public static string MktSignToString(int sign)
        {
            if (sign >= 0)
                return "B";
            else
                return "S";
        }
        public static string MktSignToString(double sign)
        {
            if (sign >= 0)
                return "B";
            else
                return "S";
        }
        public static string MktSideToString(int side)
        {
            if (side == BidSide)
                return "B";
            else
                return "S";
        }
        public static string MktSideToLongString(int side)
        {
            if (side == BidSide)
                return "Buy";
            else
                return "Sell";
        }
        public static int MktStringToMktSign(string stringSide)
        {
            if (stringSide.Equals("B"))
                return BidSign;
            else
                return AskSign;
        }
        public static int MktStringToMktSide(string stringSide)
        {
            if (stringSide.Equals("B"))
                return BidSide;
            else
                return AskSide;
        }
        //
        //
        //
        #endregion

        #region String-Matrix conversions
        //
        //
        //
        // *********************************************************
        // ****							****
        // *********************************************************
        /// <summary>
        /// This conversion matches the convention of matlab.
        /// If the encoded string passed in is a column or row vector, that is, 
        /// it contains either row delimiters or column delimiters (but not both), 
        /// then a single dimension double[] is returned.  There is no distinction
        /// herein between vectors and co-vectors.
        /// Usage:  1 0 0 ; 0 2 0 ; 0 0 3  -->		100
        ///											020
        ///											003
        ///	This function can handle NON-rectangular matrices without complaining.  
        ///	Caller is responsible for error checking.
        /// </summary>
        /// <param name="codedArray"></param>
        /// <param name="array"></param>
        /// <returns>Returns either double[] or double[][].</returns>
        public static bool TryMatrixDecode(string codedArray, out object array)
        {
            // Set up.
            array = null;
            char[] newRowDelim = new char[] { ';' };
            char[] newColDelim = new char[] { ',', ' ' };		// a row 3-vector = "1 0 0"
            bool isSuccessful = true;
            // First split off the rows.	
            string[] rowStr = codedArray.Split(newRowDelim, StringSplitOptions.RemoveEmptyEntries);
            List<double[]> rowCollection = new List<double[]>();
            List<double> aVector = new List<double>();
            double x;
            // Extract one row at a time.
            foreach (string s in rowStr)	// s contains one row.
            {
                if (isSuccessful)
                {
                    string[] elems = s.Split(newColDelim, StringSplitOptions.RemoveEmptyEntries);
                    aVector.Clear();
                    foreach (string s1 in elems)
                        if (isSuccessful && Double.TryParse(s1, out x))
                            aVector.Add(x);
                        else
                            isSuccessful = false;
                }
                if (isSuccessful)
                    rowCollection.Add(aVector.ToArray());// store
            }//next column
            if (!isSuccessful)
                return false;							// Exit, failed to complete conversion.

            // Determine size of result.
            int rowDim = rowCollection.Count;			// number of rows
            int maxColumnDim = 0;						// max columns dimension of columns.
            for (int i = 0; i < rowCollection.Count; ++i)// loop thru each row.
                maxColumnDim = Math.Max(maxColumnDim, rowCollection[i].Length);		// generalized matrix need NOT be rectangular.

            // Construct output matrix or vector.
            if (rowDim < 1 || maxColumnDim < 1)
            {	// This is a null
                array = null;
                return false;
            }
            else if (rowDim == 1 || maxColumnDim == 1)
            {	// this is a vector
                double[] vector = new double[Math.Max(rowDim, maxColumnDim)];
                int k = 0;								// entry index counter.
                for (int i = 0; i < rowDim; ++i)
                    for (int j = 0; j < rowCollection[i].Length; ++j)
                    {
                        vector[k] = rowCollection[i][j];
                        k++;
                    }
                array = vector;
                return true;
            }
            else
            {	// this is a matrix.
                double[][] matrix = new double[rowDim][];
                for (int i = 0; i < rowDim; ++i)
                    matrix[i] = rowCollection[i];
                array = matrix;
                return true;
            }
        }//StringToMatrix()
        //
        //
        // ****				MatrixEncode()			****
        //
        /// <summary>
        /// The inverse of MatrixDecode().
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string MatrixEncode(double[] array)
        {
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < array.Length; ++i)
                s.AppendFormat("{0:0.00#} ", array[i]);
            return s.ToString();
        }// MatrixEncode()
        public static string MatrixEncode(double[][] array)
        {
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < array.Length; ++i)
            {
                for (int j = 0; j < array[i].Length; ++j)
                    s.AppendFormat("{0:0.00#} ", array[i][j]);
                s.Remove(s.Length - 1, 1);	// remove trailing delimiter " "
                s.Append(";");
            }
            s.Remove(s.Length - 1, 1);	// remove trailing delimiter ";"
            return s.ToString();
        }// MatrixEncode()

        //
        //
        //
        //
        //
        //
        /*
        // *********************************************************************
        // ****						Array To String							****
        // *********************************************************************
        /// <summary>
        /// Converts an array of values to a string for easy viewing.  There are overloadings for this
        /// static method for doubles as well.  Here, an optional argument for formatting is allowed.
        /// This format argument is the usual used in strings "F2" for fixed point, "E2" etc; the deault is
        /// "F2".
        /// </summary>
        /// <param name="array">an array of values.</param>
        /// <returns>string of values.</returns>
        public static string ArrayToString(int[] array)
        {
            if (array == null) { return null; }

            //
            // Construct and load string builder object.
            //
            const int maxCharLength = 5;	// the expected number of digits for largest integer in the array.
            const int paddingLength = 2;	// number of characters used to "pad" each value.

            int arrayLength = array.Length;
            System.Text.StringBuilder aString = new System.Text.StringBuilder((maxCharLength + paddingLength + 1) * arrayLength);
            for (int i = 0; i < arrayLength; ++i)
            {
                aString.AppendFormat("[{0}]", array[i].ToString());
            }//i

            //
            // Exit
            //
            return aString.ToString();
        }//end ArrayToString()
        //
        // Default format overloading.
        public static string ArrayToString(double[] array)
        {
            return ArrayToString(array, "F2");
        }
        public static string ArrayToString(double[] array, string valueFormat)
        {
            if (array == null) { return null; }

            // Construct and load string builder object.
            //
            const int paddingLength = 2;	 // number of characters used to "pad" each value.
            int valueLength = array[0].ToString(valueFormat).Length;

            int arrayLength = array.Length;
            System.Text.StringBuilder aString = new System.Text.StringBuilder((valueLength + paddingLength + 1) * arrayLength);
            for (int i = 0; i < arrayLength; ++i)
            {
                aString.AppendFormat("[{0}]", array[i].ToString(valueFormat));
            }//i

            //
            // Exit
            //
            return aString.ToString();
        }//end ArrayToString().
		*/
        //		
        //
        //
        /// <summary>
        /// Exponential Distribution
        /// http://mathworld.wolfram.com/ExponentialDistribution.html
        /// </summary>
        /// <param name="argX"></param>
        /// <param name="argLambda"></param>
        /// <returns></returns>
        public static double ExponentialDistrib(double argX, double argLambda)
        {
            return (1.0d - Math.Exp(-argLambda * argX));
        }
        //
        #endregion

        #region DateTime utilities
        //
        //
        //
        //public static DateTime Epoch2DateTime(int unixTimeStamp)
        //{
        //	return 
        //}
        /*
        public static int DateTimeToEpoch(DateTime date)
        {
            int epoch = (int) Math.Floor((date.Subtract(new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds);
            return epoch;
        }
        public static int DateTimeToEpoch(DateTime date, out double secondFraction)
        {
            double epoch = date.Subtract(new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds;
            int epochint = (int) Math.Floor( epoch );
            secondFraction = epoch - epochint;
            return epochint;
        }
        */
        public static double DateTimeToEpoch(DateTime date)
        {
            return (date.Subtract(new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds;
        }
        public static DateTime EpochToDateTime(uint epoch)
        {
            DateTime baseDate = new DateTime(1970, 1, 1, 0, 0, 0);
            return baseDate.AddSeconds((double)epoch);
        }
        public static DateTime EpochToDateTime(double epoch)
        {
            DateTime baseDate = new DateTime(1970, 1, 1, 0, 0, 0);
            return baseDate.AddSeconds(epoch);
        }
        //
        /// <summary>
        /// Converts an Olson time zone ID to a Windows time zone ID.
        /// See http://unicode.org/repos/cldr-tmp/trunk/diff/supplemental/zone_tzid.html.
        /// </summary>
        /// <param name="olsonTimeZoneId">An Olson time zone ID. </param>
        /// <returns>
        /// The TimeZoneInfo corresponding to the Olson time zone ID, 
        /// or null if you passed in an invalid Olson time zone ID.
        /// </returns>
        public static TimeZoneInfo OlsonTimeZoneToTimeZoneInfo(string olsonTimeZoneId)
        {
            var olsonWindowsTimes = new Dictionary<string, string>()
            {
                { "Africa/Bangui", "W. Central Africa Standard Time" },
                { "Africa/Cairo", "Egypt Standard Time" },
                { "Africa/Casablanca", "Morocco Standard Time" },
                { "Africa/Harare", "South Africa Standard Time" },
                { "Africa/Johannesburg", "South Africa Standard Time" },
                { "Africa/Lagos", "W. Central Africa Standard Time" },
                { "Africa/Monrovia", "Greenwich Standard Time" },
                { "Africa/Nairobi", "E. Africa Standard Time" },
                { "Africa/Windhoek", "Namibia Standard Time" },
                { "America/Anchorage", "Alaskan Standard Time" },
                { "America/Argentina/San_Juan", "Argentina Standard Time" },
                { "America/Asuncion", "Paraguay Standard Time" },
                { "America/Bahia", "Bahia Standard Time" },
                { "America/Bogota", "SA Pacific Standard Time" },
                { "America/Buenos_Aires", "Argentina Standard Time" },
                { "America/Caracas", "Venezuela Standard Time" },
                { "America/Cayenne", "SA Eastern Standard Time" },
                { "America/Chicago", "Central Standard Time" },
                { "America/Chihuahua", "Mountain Standard Time (Mexico)" },
                { "America/Cuiaba", "Central Brazilian Standard Time" },
                { "America/Denver", "Mountain Standard Time" },
                { "America/Fortaleza", "SA Eastern Standard Time" },
                { "America/Godthab", "Greenland Standard Time" },
                { "America/Guatemala", "Central America Standard Time" },
                { "America/Halifax", "Atlantic Standard Time" },
                { "America/Indianapolis", "US Eastern Standard Time" },
                { "America/La_Paz", "SA Western Standard Time" },
                { "America/Los_Angeles", "Pacific Standard Time" },
                { "America/Mexico_City", "Mexico Standard Time" },
                { "America/Montevideo", "Montevideo Standard Time" },
                { "America/New_York", "Eastern Standard Time" },
                { "America/Noronha", "UTC-02" },
                { "America/Phoenix", "US Mountain Standard Time" },
                { "America/Regina", "Canada Central Standard Time" },
                { "America/Santa_Isabel", "Pacific Standard Time (Mexico)" },
                { "America/Santiago", "Pacific SA Standard Time" },
                { "America/Sao_Paulo", "E. South America Standard Time" },
                { "America/St_Johns", "Newfoundland Standard Time" },
                { "America/Tijuana", "Pacific Standard Time" },
                { "Antarctica/McMurdo", "New Zealand Standard Time" },
                { "Atlantic/South_Georgia", "UTC-02" },
                { "Asia/Almaty", "Central Asia Standard Time" },
                { "Asia/Amman", "Jordan Standard Time" },
                { "Asia/Baghdad", "Arabic Standard Time" },
                { "Asia/Baku", "Azerbaijan Standard Time" },
                { "Asia/Bangkok", "SE Asia Standard Time" },
                { "Asia/Beirut", "Middle East Standard Time" },
                { "Asia/Calcutta", "India Standard Time" },
                { "Asia/Colombo", "Sri Lanka Standard Time" },
                { "Asia/Damascus", "Syria Standard Time" },
                { "Asia/Dhaka", "Bangladesh Standard Time" },
                { "Asia/Dubai", "Arabian Standard Time" },
                { "Asia/Irkutsk", "North Asia East Standard Time" },
                { "Asia/Jerusalem", "Israel Standard Time" },
                { "Asia/Kabul", "Afghanistan Standard Time" },
                { "Asia/Kamchatka", "Kamchatka Standard Time" },
                { "Asia/Karachi", "Pakistan Standard Time" },
                { "Asia/Katmandu", "Nepal Standard Time" },
                { "Asia/Kolkata", "India Standard Time" },
                { "Asia/Krasnoyarsk", "North Asia Standard Time" },
                { "Asia/Kuala_Lumpur", "Singapore Standard Time" },
                { "Asia/Kuwait", "Arab Standard Time" },
                { "Asia/Magadan", "Magadan Standard Time" },
                { "Asia/Muscat", "Arabian Standard Time" },
                { "Asia/Novosibirsk", "N. Central Asia Standard Time" },
                { "Asia/Oral", "West Asia Standard Time" },
                { "Asia/Rangoon", "Myanmar Standard Time" },
                { "Asia/Riyadh", "Arab Standard Time" },
                { "Asia/Seoul", "Korea Standard Time" },
                { "Asia/Shanghai", "China Standard Time" },
                { "Asia/Singapore", "Singapore Standard Time" },
                { "Asia/Taipei", "Taipei Standard Time" },
                { "Asia/Tashkent", "West Asia Standard Time" },
                { "Asia/Tbilisi", "Georgian Standard Time" },
                { "Asia/Tehran", "Iran Standard Time" },
                { "Asia/Tokyo", "Tokyo Standard Time" },
                { "Asia/Ulaanbaatar", "Ulaanbaatar Standard Time" },
                { "Asia/Vladivostok", "Vladivostok Standard Time" },
                { "Asia/Yakutsk", "Yakutsk Standard Time" },
                { "Asia/Yekaterinburg", "Ekaterinburg Standard Time" },
                { "Asia/Yerevan", "Armenian Standard Time" },
                { "Atlantic/Azores", "Azores Standard Time" },
                { "Atlantic/Cape_Verde", "Cape Verde Standard Time" },
                { "Atlantic/Reykjavik", "Greenwich Standard Time" },
                { "Australia/Adelaide", "Cen. Australia Standard Time" },
                { "Australia/Brisbane", "E. Australia Standard Time" },
                { "Australia/Darwin", "AUS Central Standard Time" },
                { "Australia/Hobart", "Tasmania Standard Time" },
                { "Australia/Perth", "W. Australia Standard Time" },
                { "Australia/Sydney", "AUS Eastern Standard Time" },
                { "Etc/GMT", "UTC" },
                { "Etc/GMT+11", "UTC-11" },
                { "Etc/GMT+12", "Dateline Standard Time" },
                { "Etc/GMT+2", "UTC-02" },
                { "Etc/GMT-12", "UTC+12" },
                { "Europe/Amsterdam", "W. Europe Standard Time" },
                { "Europe/Athens", "GTB Standard Time" },
                { "Europe/Belgrade", "Central Europe Standard Time" },
                { "Europe/Berlin", "W. Europe Standard Time" },
                { "Europe/Brussels", "Romance Standard Time" },
                { "Europe/Budapest", "Central Europe Standard Time" },
                { "Europe/Dublin", "GMT Standard Time" },
                { "Europe/Helsinki", "FLE Standard Time" },
                { "Europe/Istanbul", "GTB Standard Time" },
                { "Europe/Kiev", "FLE Standard Time" },
                { "Europe/London", "GMT Standard Time" },
                { "Europe/Minsk", "E. Europe Standard Time" },
                { "Europe/Moscow", "Russian Standard Time" },
                { "Europe/Paris", "Romance Standard Time" },
                { "Europe/Sarajevo", "Central European Standard Time" },
                { "Europe/Warsaw", "Central European Standard Time" },
                { "Indian/Mauritius", "Mauritius Standard Time" },
                { "Pacific/Apia", "Samoa Standard Time" },
                { "Pacific/Auckland", "New Zealand Standard Time" },
                { "Pacific/Fiji", "Fiji Standard Time" },
                { "Pacific/Guadalcanal", "Central Pacific Standard Time" },
                { "Pacific/Guam", "West Pacific Standard Time" },
                { "Pacific/Honolulu", "Hawaiian Standard Time" },
                { "Pacific/Pago_Pago", "UTC-11" },
                { "Pacific/Port_Moresby", "West Pacific Standard Time" },
                { "Pacific/Tongatapu", "Tonga Standard Time" }
            };

            var windowsTimeZoneId = default(string);
            var windowsTimeZone = default(TimeZoneInfo);
            if (olsonWindowsTimes.TryGetValue(olsonTimeZoneId, out windowsTimeZoneId))
            {
                try { windowsTimeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZoneId); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return windowsTimeZone;
        }// OlsonTimeCode
        //
        //
        /// <summary>
        /// From seconds from midnight creates a timespan object
        /// </summary>
        /// <param name="secondsFromMidnight"></param>
        /// <returns></returns>
        public static TimeSpan SecondsFromMidnightTimeSpan(int secondsFromMidnight)
        {
            return TimeSpan.FromSeconds(secondsFromMidnight);
        }
        //
        #endregion//dates and times.

        #region MonthCodes
        // *****************************************************
        // ****                 Month Codes                 ****
        // *****************************************************
        public const string MonthCodesList = "FGHJKMNQUVXZ";
        public const string MonthNameList = "JanFebMarAprMayJunJulAugSepOctNovDec";
        /// <summary>
        /// No error checking is performed.
        /// </summary>
        /// <param name="monthNumber">1=Jan,...,12=Dec</param>
        /// <param name="monthCode"></param>
        /// <returns></returns>
        public static bool TryGetMonthCode(int monthNumber, out string monthCode)
        {
            monthCode = string.Empty;
            if (monthNumber > 0 && monthNumber < 13)
            {
                monthCode = GetMonthCode(monthNumber);
                return true;
            }
            else
                return false;
        }
        public static bool TryGetMonthCode(DateTime dt, out string monthCode)
        {
            monthCode = string.Empty;
            try
            {
                monthCode = GetMonthCode(dt);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool TryGetMonthName(int monthNumber, out string monthAbrev)
        {
            monthAbrev = string.Empty;
            if (monthNumber > 0 && monthNumber < 13)
            {
                monthAbrev = GetMonthName(monthNumber);
                return true;
            }
            else
                return false;
        }
        public static string GetMonthName(int monthNumber)
        {
            int ptr = (monthNumber - 1) * 3;
            return MonthNameList.Substring(ptr, 3);
        }
        public static string GetMonthCode(int monthNumber)
        {
            return MonthCodesList.Substring(monthNumber - 1, 1);    // -1 since indexing is zero-based.
        }
        public static string GetMonthCode(DateTime dt)
        {
            return GetMonthCode(dt.Month);
        }
        //
        //
        //
        // ****         Inverse function            ****
        //
        public static int GetMonthNumberFromCode(string monthCode)
        {
            return GetMonthNumber(char.Parse(monthCode.Substring(0)));
        }
        public static int GetMonthNumber(char monthCode)
        {
            int n = MonthCodesList.IndexOf(char.ToUpper(monthCode));
            return n + 1;
        }
        //
        //
        /// <summary>
        /// Try and convert a Month Year ie 'Dec13' or 'Dec3' to a month code and single digit year format
        /// ie 'Z3',  If there are multiple MonthYears a codeYear string seperated by "." will be returned
        /// </summary>
        /// <param name="MonthYear"></param>
        /// <param name="codeYear"></param>
        /// <returns></returns>
        public static bool TryConvertMonthYearToCodeY(string MonthYear, out string codeYear)
        {
            bool isSuccess = false;
            StringBuilder monthCodeYearsBuilder = new StringBuilder();
            DateTime dt = DateTime.Now;
            string monthCode;
            List<string> monthYears = new List<string>();

            if (MonthYear.Length > 5)
            {   // something different was handed to us, lets try and extract the monthYear using regex
                string regExpSearch = @"[a-zA-Z]{3}[0-9]{2}";
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(MonthYear, regExpSearch))
                    monthYears.Add(match.Value);
            }
            else
                monthYears.Add(MonthYear);

            for (int monthYearIndex = 0; monthYearIndex < monthYears.Count; ++monthYearIndex)
            {
                string monthYear = monthYears[monthYearIndex];
                // Determine Year length
                int startYearPtr = monthYear.Length - 1;
                while (startYearPtr >= 0 && Char.IsDigit(monthYear[startYearPtr]))
                    startYearPtr--;
                startYearPtr++;             // now startYearPtr points to the start of the numeric part.

                if (monthYear.Length == 5 && DateTime.TryParseExact(monthYear, "MMMyy", null, System.Globalization.DateTimeStyles.None, out dt))
                { // two digit year code must have been used, and we parsed it okay
                    isSuccess = true;
                    if (isSuccess && UV.Lib.Utilities.QTMath.TryGetMonthCode(dt, out monthCode))
                    { // try and get a month code from our datetime.
                        string year = dt.Year.ToString();
                        char lastYearDigit = year[year.Length - 1];
                        monthCodeYearsBuilder.Append(string.Format("{0}{1}", monthCode, lastYearDigit));
                    }
                }
                else if (monthYear.Length == 4 && DateTime.TryParseExact(monthYear, "MMMyy", null, System.Globalization.DateTimeStyles.None, out dt))
                { // singled digit year code..
                    isSuccess = true;
                    if (isSuccess && UV.Lib.Utilities.QTMath.TryGetMonthCode(dt, out monthCode))
                    { // try and get a month code from our datetime.
                        string year = dt.Year.ToString();
                        char lastYearDigit = year[year.Length - 1];
                        monthCodeYearsBuilder.Append(string.Format("{0}{1}", monthCode, lastYearDigit));
                    }
                }
                else if (monthYear.Length == 3 && startYearPtr == 1)
                {   // This seems to have form "H12"
                    isSuccess = true;
                    monthCodeYearsBuilder.Append(string.Format("{0}{1}", monthYear[0], monthYear[2]));
                }
                else if (monthYear.Length == 2 && startYearPtr == 1)
                {   // This already has form  "H2"
                    isSuccess = true;
                    monthCodeYearsBuilder.Append(monthYear);
                }

                if (isSuccess && monthYearIndex < monthYears.Count - 1)
                    monthCodeYearsBuilder.Append(".");
            }

            codeYear = monthCodeYearsBuilder.ToString();
            return isSuccess;
        }
        //
        /// <summary>
        /// Try and convert a Month Year ie 'Dec13' or 'Dec3' to a month code and two digit year format
        /// ie 'Z13'
        /// </summary>
        /// <param name="MonthYear"></param>
        /// <param name="codeYear"></param>
        /// <returns></returns>
        public static bool TryConvertMonthYearToCodeYY(string MonthYear, out string codeYear)
        {
            codeYear = string.Empty;
            DateTime dt = new DateTime();
            string monthCode;
            bool isSuccess = false;
            if (MonthYear.Length == 5 && DateTime.TryParseExact(MonthYear, "MMMyy", null, System.Globalization.DateTimeStyles.None, out dt))
            { // two digit year code must have been used, and we parsed it okay
                isSuccess = true;
            }
            else if (MonthYear.Length == 4 && DateTime.TryParseExact(MonthYear, "MMMyy", null, System.Globalization.DateTimeStyles.None, out dt))
            { // singled digit year code..
                isSuccess = true;
            }

            if (isSuccess && TryGetMonthCode(dt, out monthCode))
            { // try and get a month code from our datetime.
                string year = dt.Year.ToString();
                string lastTwoYearDigits = year.Substring(year.Length - 2, 2);
                codeYear = string.Format("{0}{1}", monthCode, lastTwoYearDigits); // create our "codeYear"
            }
            return isSuccess;
        }

        // *****************************************************
        // ****               base-60 Codes                 ****
        // *****************************************************
        public const string BaseSixtyCodesList = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        //
        //
        //
        //
        public static bool TryGetBaseSixtyCode(int n, out string code)
        {
            if (n >= 0 && n < 60)
            {
                code = GetBaseSixtyCode(n);
                return true;
            }
            else
            {
                code = string.Empty;
                return false;
            }
        }
        //
        public static string GetBaseSixtyCode(int n)
        {
            return BaseSixtyCodesList.Substring(n, 1);
        }
        //
        // ****             Inverse                 ****
        //
        // "0" --> 0, etc
        // "A" --> 10
        // "F" --> 15
        // "K" --> 20
        // "P" --> 25
        // "U" --> 30,
        // "Z" --> 35
        // "a" --> 36,
        // "w" --> 59
        // "x" --> 60
        // "z" --> 62
        /// <summary>
        /// returns -1 on error.
        /// </summary>
        /// <param name="codedBaseSixtyValue"></param>
        /// <returns></returns>
        public static int GetBaseSixtyDecode(char codedBaseSixtyValue)
        {
            int n = (int)codedBaseSixtyValue;
            if (n <= 47)
                return -1;
            else if (n <= 57)                   // 57 = "9"
                return n - 48;
            else if (n <= 64)                   // "@"
                return -1;
            else if (n <= 90)                   // 90 = "Z"
                return n - 55;                  // Note: 65="A" so for "A" we would return value 10.
            else if (n <= 96)
                return -1;
            else if (n <= 122)
                return n - 61;
            else
                return -1;
        }
        public static bool TryGetBaseSixtyDecode(char codedBaseSixtyValue, out int baseSixtyValue)
        {
            baseSixtyValue = GetBaseSixtyDecode(codedBaseSixtyValue);
            return (baseSixtyValue != -1);
        }
        //
        //
        //
        //
        #endregion//monthcodes
    }
}

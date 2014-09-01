using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace Misty.Lib.Products
{
    /// <summary>
    /// This is now defunct.
    /// </summary>
    public class Instrument : Product
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************          
        // Basic instrument identifier
        //
        public MonthCodes MonthCode = MonthCodes.None;	        // Z for december etc., None for equities, or "TermIdentified"		
        public int Year = -1;							        // full year 2012, or Term identifier if MonthCode = TermIdentified
        
        

        //
        //
        //
        #endregion// members


        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// This property returns true if the instrument is identified with its position along the 
        /// term structure curve rather than by a explicit date.
        /// </summary>
        public bool IsTermIndentified 
        {
            get { return (this.MonthCode == MonthCodes.TermIdentified); }
        }
        //
        //
        // ****                 Full Name                   ****
        //
        public override string FullName
        {
            get
            {
                if (this.MonthCode==MonthCodes.TermIdentified)	        	// is term-location identified
                    return string.Format("{0}#{1}", base.FullName, this.Year.ToString());
                else if (!this.MonthCode.Equals(MonthCodes.None))	        // is specific month identified
                    return string.Format("{0}{1}{2:0}", base.FullName, this.ProductName, this.MonthCode.ToString(), (this.Year % 10));
                else												        // has NO month specified (eg., equity?)
                    return string.Format("{0}", base.FullName);
            }
        }//FullName
        //
        //
        //
        #endregion//Properties



        #region Constructors and Creators
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        protected Instrument(Instrument instrumentToCopy) : base((Product)instrumentToCopy)
        {
            this.MonthCode = instrumentToCopy.MonthCode;
            this.Year = instrumentToCopy.Year;
        }
        protected Instrument(Product baseProduct) : base(baseProduct)
        {

        }
        //
        // ****				Create()			****
        //
        public static Instrument Create(string fullNameStr, string serverName)
        {
            Instrument instr = null;
            if (TryInitialize(fullNameStr, serverName, out instr))
                return instr;
            else
                return null;
        }//Create
        //
        //
        // ****				TryInitialize()			****
        //
        /// <summary>
        /// Allowed format examples:
        /// 1) CME.GEU2		- simplest, full exchange and specific name
        /// 2) CME.GE#12	- term notation, 12th along term structure curve of GE family.
        /// 3) CME.GE		- single product reference only, like an equity where product = instrument.
        /// Using this weird out-function approach allows us to validate the format and then finally create
        /// a read-only object.
        /// </summary>
        protected static bool TryInitialize(string exchInstrName, string serverName, out Instrument instrument)
        {
            bool isSuccess = true;
            string exchange;
            string productName;
            ProductTypes productType = ProductTypes.Future;
            instrument = null;

            int year = -1;
            MonthCodes monthCode = MonthCodes.None;

            //
            // Extract exchange from front of string.
            string fullInstrName;			// place to store the instrument name.
            int delimitIndex = exchInstrName.IndexOf('.');	// locate exchange/instr delimiter
            if (delimitIndex < 1)				// there is no exchange tag
            {
                //this.Exchange = GuessTheExchange(exchInstrName);
                exchange = GuessTheExchange(exchInstrName);
                fullInstrName = exchInstrName;
            }
            else
            {
                //this.Exchange = exchInstrName.Substring(0, delimitIndex);
                exchange = exchInstrName.Substring(0, delimitIndex);
                fullInstrName = exchInstrName.Substring(delimitIndex + 1);
            }
            // Analyze full instrument name.
            delimitIndex = fullInstrName.IndexOf('#');		// locate term-structure delimiter.
            if (delimitIndex < 0)
                delimitIndex = fullInstrName.IndexOf('_');		// alternate term-structure delimiter.
            if (delimitIndex < 0)
            {	// Instrument name is in one of the basic format.
                // Extract year index.  Can accept 0,1, or 2 digit-formats. 0 means no year included (eg., equity).
                int yr;
                int n = fullInstrName.Length;
                if (int.TryParse(fullInstrName.Substring(n - 2), out yr))
                {	// 2-digit year provided.
                    //this.Year = DateTime.Now.Year - (DateTime.Now.Year % 100) + yr;
                    year = DateTime.Now.Year - (DateTime.Now.Year % 100) + yr;
                    string monthStr = fullInstrName.Substring(n - 3, 1);	// Month code must immediately proceed year.
                    //this.MonthCode = (MonthCodes)Enum.Parse(typeof(MonthCodes), monthStr, true);
                    monthCode = (MonthCodes)Enum.Parse(typeof(MonthCodes), monthStr, true);
                    //this.ProductName = fullInstrName.Substring(0, n - 3);
                    productName = fullInstrName.Substring(0, n - 3);
                }
                else if (int.TryParse(fullInstrName.Substring(n - 1), out yr))
                {	// 1-digit year provided.
                    //this.Year = DateTime.Now.Year - (DateTime.Now.Year % 10) + yr;
                    year = DateTime.Now.Year - (DateTime.Now.Year % 10) + yr;
                    string monthStr = fullInstrName.Substring(n - 2, 1);	// Month code must immediately proceed year.
                    //this.MonthCode = (MonthCodes)Enum.Parse(typeof(MonthCodes), monthStr, true);
                    monthCode = (MonthCodes)Enum.Parse(typeof(MonthCodes), monthStr, true);
                    //this.ProductName = fullInstrName.Substring(0, n - 2);
                    productName = fullInstrName.Substring(0, n - 2);
                }
                else
                {	// No year provided.  For example, equities "NYSE.GE"
                    //this.ProductName = fullInstrName;
                    productName = fullInstrName;
                    productType = ProductTypes.Equity;
                }
            }
            else if (delimitIndex < 1)
            {
                return false;
            }
            else
            {	// instrument name formated as "term-structure" format: [prodName]#[location along term]
                //this.IsTermIdentified = true;
                //this.ProductName = fullInstrName.Substring(0, delimitIndex);
                productName = fullInstrName.Substring(0, delimitIndex);
                //this.MonthCode = MonthCodes.TermIdentified;
                monthCode = MonthCodes.TermIdentified;
                string sTermIndex = fullInstrName.Substring(delimitIndex + 1);
                int n;
                if (!int.TryParse(sTermIndex, out n))
                    return false;							// failed to parse integer!
                //this.TermIndex = n;
                //this.Year = n;
                year = n;
            }
            // Exit
            instrument = new Instrument(new Product(exchange, productName, productType, serverName));
            return isSuccess;
        }//initialize()
        //
        //
        //
        #endregion//Constructors


        #region Public Methods
        // *******************************************************************
        // ****                     Public  Methods                       ****
        // *******************************************************************
        //
        //
        // ****             CopyTo()                ****
        //
        /*
        public void CopyTo(Instrument newInstr)
        {
            base.CopyTo(newInstr);                              // copy product info

            newInstr.MonthCode = this.MonthCode;
            newInstr.Year = this.Year;
        }//CopyTo().
        */
        //
        #endregion//public methods


        #region Private Methods
        // *****************************************************************
        // ****                     Methods                             ****
        // *****************************************************************
        //
        private static string GuessTheExchange(string name)
        {
            int n;
            if (int.TryParse(name.Substring(name.Length - 1), out n))
            {	// remove number 
                name = name.Substring(0, name.Length - 2);
            }

            string exch = "CME";
            string s = name.ToUpper();
            switch (s)
            {
                case "ZT":
                case "ZF":
                case "ZN":
                case "ZB":
                case "ZW":
                case "ZL":
                case "ZC":
                case "UB":
                    exch = "CBOT";
                    break;
                case "CL":
                case "HO":
                case "RB":
                case "NG":
                case "SI":
                case "HG":
                    exch = "NYMEX";
                    break;
                case "GC":
                    exch = "COMEX";
                    break;
                default:
                    exch = "CME";
                    break;
            }
            return exch;
        }
        //
        //
        //
        #endregion // private methods


        #region Public Overridden Methods 
        // *****************************************************************************
        // ****                     Public Overridden Methods                       ****
        // *****************************************************************************
        //
        // ****                 ToString()              ****
        //
        public override string ToString()
        {
            return this.FullName; 
        }
        //
        //
        // ****                 Equals()                 ****
        //
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            Instrument other = obj as Instrument;
            if ((Instrument)other == null)
                return false;
            if (! base.Equals( (Product) other) )               // compare base classes.
                return false;
            return (this.MonthCode.Equals(other.MonthCode) && this.Year.Equals(other.Year));
        }
        //
        // ****                 Equals()                 ****\
        //
        public virtual bool Equals(Instrument other)
        {
            if (other == null)
                return false;
            if (!base.Equals((Product)other))
                return false;
            return (this.MonthCode.Equals(other.MonthCode) && this.Year.Equals(other.Year));
        }
        public static bool operator ==(Instrument a, Instrument b)
        {
            if (System.Object.ReferenceEquals(a, b))
                return true;
            if (a == null)
                return false;
            return a.Equals(b);
        }
        public static bool operator !=(Instrument a, Instrument b)
        {
            return !(a == b);
        }
        //
        // ****                 GetHashCode()                 ****
        //
        public override int GetHashCode()
        {
            return string.Format("{0}{1}", this.FullName, this.Type).GetHashCode();
        }
        //
        //
        #endregion//Public Override Methods


    }//end class
}

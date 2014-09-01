using System;
using System.Text;

namespace Misty.Lib.Products
{
    /// <summary>
    /// This is a struct - so its a Value-type of variable.  Two instances of a product are equal if each of their
    /// elements is equal.
    /// The Equals() and GetHashCode() methods are overloaded so that two instances are equal
    /// as long as they both refer to the same logical product (but can be separate instances of this class).
    /// </summary>
    public struct Product : IEquatable<Product>
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************          
        // Basic identifier
        //
        public readonly string ServerName;                  // server name.  Empty if we use only one, default server.
        public readonly string Exchange;					// CME
        public readonly string ProductName;					// GE
        public readonly ProductTypes Type;
        #endregion//members


        #region Constructors
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        public Product(string exchange, string name, ProductTypes type, string serverName)              
        {
            // This is the full constructor, others include default values.  
            // Its done this way to allow us to make variables read-only later, if desired.
            ServerName = serverName;    
            ProductName = name;
            Exchange = exchange;
            this.Type = type;
        }
        public Product(string exchange, string name, ProductTypes type) : this(exchange, name, type, string.Empty) { }
        //
        // Copy constructor      
        //
        public Product(Product productToCopy) : this(productToCopy.Exchange,productToCopy.ProductName,productToCopy.Type,productToCopy.ServerName)
        {            
        }
        //
        //
        //
        //
        //
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public bool IsEmpty
        {
            get { return (string.IsNullOrEmpty(ProductName) || string.IsNullOrEmpty(Exchange)); }
        }
        //
        //
        #endregion//Properties


        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        public string FullName
        {
            get{ return Product.Serialize(this); }
        }// UniqueName
        //
        //
        //
        public static string Serialize(Product product)
        {
            if (string.IsNullOrEmpty(product.ServerName))
                return string.Format("{0}.{1} ({2})", product.Exchange, product.ProductName, (product.Type).ToString());
            else
                return string.Format("[{3}]{0}.{1} ({2})", product.Exchange, product.ProductName, (product.Type).ToString(), product.ServerName); 
        }
        //
        //
        public static bool TryDeserialize(string serialStr,out Product newProduct)
        {                                                   // the empty product
            newProduct = new Product();
            serialStr = serialStr.Trim();
            // The general serial structure is 
            // "[Server1]ICE-IPE.IPE Gas-Oil (FUTURE)"   Notice that the product name may have embedded spaces.
            // I assume that no exchange string is allowed to have embedded periods.
            try
            {
                string serverName = string.Empty;
                string exchangeName = string.Empty;
                string productName = string.Empty;
                ProductTypes type = ProductTypes.Unknown;
                // Extract the server name.                
                int n1 = serialStr.IndexOf(']');
                if (serialStr.StartsWith("[") && n1 > 1)
                {
                    serverName = serialStr.Substring(1, n1 - 1).Trim();
                    serialStr = serialStr.Substring(n1 + 1, serialStr.Length - (n1 + 1)); // remainder after [server] block.
                }
                // Extract the exchange name.
                n1 = serialStr.IndexOf('.');                                             // find first '.'
                if (n1 > 0 && n1 < serialStr.Length)
                {
                    exchangeName = serialStr.Substring(0, n1).Trim();
                    serialStr = serialStr.Substring(n1 + 1, serialStr.Length - (n1 + 1)); // remainder after '.'
                }
                else
                    return false;
                // Extract the product type.
                n1 = serialStr.IndexOf('(');
                int n2 = serialStr.IndexOf(')');
                if (n1 > 0 && n2 > 0 && n1 < n2)
                {
                    if (!Enum.TryParse<ProductTypes>(serialStr.Substring(n1 + 1, n2 - (n1 + 1)).Trim(), out type))
                        return false;
                }
                else
                    return false;
                // Extract the product name which is whatever remains
                productName = serialStr.Substring(0, n1).Trim();
                // Exit successfully!
                newProduct = new Product(exchangeName, productName, type, serverName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        } // TryDeserialize()
        //
        //
        #endregion//Public methods



        #region Public Override Methods
        // *****************************************************************
        // ****             Public Override Methods                     ****
        // *****************************************************************
        //
        //
        //
        //
        // ****             ToString()              ****
        //
        public override string ToString()
        {
           return string.Format("{0}.{1}", this.Exchange, this.ProductName);
        }//ToString()
        //
       
        //
        //
        // ****                 Equals                  ****
        //
        public override bool Equals(object obj)
        {
            if (obj is Product)
            {
                Product other = (Product)obj;
                bool isEqual = this.Exchange.Equals(other.Exchange) && this.ProductName.Equals(other.ProductName)
                && this.ServerName.Equals(other.ServerName) && this.Type.Equals(other.Type);
                return isEqual;
            }
            else
                return false;
        }
        public bool Equals(Product other)
        {
            bool isEqual = this.Exchange.Equals(other.Exchange) && this.ProductName.Equals(other.ProductName) 
                && this.ServerName.Equals(other.ServerName) && this.Type.Equals(other.Type);
            return isEqual;
        }
        /*
        public static bool operator ==(object a, object b)
        {
            if (System.Object.ReferenceEquals(a, b))
                return true;
            if ((object)a == null)
                return false;
            return a.Equals(b);
        }
        public static bool operator !=(object a, object b)
        {
            return !(a == b);
        }
        */ 
        public static bool operator ==(Product a, Product b)
        {
            //if (System.Object.ReferenceEquals(a, b))
            //    return true;
            if ((object) a == null)
                return false;
            return a.Equals(b);
        }
        public static bool operator !=(Product a, Product b)
        {
            return !(a == b);
        }
        public override int GetHashCode()
        {
            return this.FullName.GetHashCode();
        }
        //
        //

        //
        //
        #endregion//Public Methods



    }//end class 
}//Namespace

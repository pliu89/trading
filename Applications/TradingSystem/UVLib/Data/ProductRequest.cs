using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace UV.Lib.Data
{
    using UV.Lib.Products;
    using UV.Lib.IO.Xml;
    using UV.Lib.Application;
    /// <summary>
    /// Simple Object for Product Requests and a Method To Create them from a file
    /// </summary>
    public class ProductRequest : IStringifiable, IEquatable<ProductRequest>
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        private Product m_Product;
        private int m_nInstrumentsToRecord;
        public bool m_IsStandardInstrumentsOnly = true;                           //flag for allowing us to subscribe to instruments outside normal expirations (ie Cal and Q1 in ICE)
        #endregion// members

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        //
        public ProductRequest()
        {
        }
        //
        /// <summary>
        /// Create a new product request object.
        /// </summary>
        /// <param name="product"></param>
        /// <param name="noOfInstrumentsToRecord"></param>
        /// <param name="isStandardInstrumentsOnly">Should we only consider products that have standard expirations and strucutres.  
        /// For ICE, if you want Cal and Q contracts, this should be set to false.</param>
        public ProductRequest(Product product, int noOfInstrumentsToRecord, bool isStandardInstrumentsOnly)
        {
            this.m_Product = product;
            this.m_nInstrumentsToRecord = noOfInstrumentsToRecord;
            this.m_IsStandardInstrumentsOnly = isStandardInstrumentsOnly;
        }
        #endregion//Constructors

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        /// <summary>
        /// Product that the request is for
        /// </summary>
        public Product Product
        {
            get { return m_Product; }
        }
        /// <summary>
        /// No of instruments from the front contract to request.
        /// </summary>
        public int nInstrumentsToRecord
        {
            get { return m_nInstrumentsToRecord; }
        }
        #endregion//Properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //************************************************************
        //****                 TryCreateFromFile                  ****
        //************************************************************
        /// <summary>
        /// Attempt to create a product request from a file using Istringifiable interface.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="prodRequestList"></param>
        /// <returns>false if faled</returns>
        public static bool TryCreateFromFile(string filePath, out List<ProductRequest> prodRequestList)
        {
            prodRequestList = new List<ProductRequest>();
            try
            {
                string fullFilePath = string.Format("{0}{1}", Application.AppServices.GetInstance().Info.UserConfigPath, filePath);
                List<IStringifiable> iStringObjects;
                using (StringifiableReader reader = new StringifiableReader(fullFilePath))
                {
                    iStringObjects = reader.ReadToEnd();
                }
                foreach (IStringifiable iStrObj in iStringObjects)
                    if (iStrObj is ProductRequest)
                        prodRequestList.Add((ProductRequest)iStrObj);
            }
            catch (Exception e)
            {
                StringBuilder msg = new StringBuilder();
                msg.AppendFormat("Exception: {0}\r\nContinue?", e.Message);
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(msg.ToString(), "ProductRequest.TryCreateFromFile", System.Windows.Forms.MessageBoxButtons.OKCancel);
                return result == System.Windows.Forms.DialogResult.OK;
            }
            return true;
        }
        //
        //
        //
        //************************************************************
        //****                      Equals                        ****
        //************************************************************
        /// <summary>
        /// iEquitable interface overide of Equals.  If both the product, and number of contracts to 
        /// request are equal, these objects are equal.
        /// </summary>
        /// <param name="prodRequest"></param>
        /// <returns></returns>
        public bool Equals(ProductRequest prodRequest)
        {
            if (this.Product == prodRequest.Product && this.nInstrumentsToRecord == prodRequest.nInstrumentsToRecord)
                return true;
            else
                return false;
        }
        //
        //
        //
        //
        #endregion//Public Methods

        #region no Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        //
        #endregion//Private Methods

        #region IStringifiable implementation
        // *************************************************
        // ****             IStringifiable              ****
        // *************************************************
        /// <summary>
        /// These are often called before everything is connected, so 
        /// you cant assume that there is a "StrategyHub" or Log here yet.
        /// </summary>
        /// <returns></returns>
        string IStringifiable.GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("Product={0} ", this.m_Product);
            return s.ToString();
        }
        List<IStringifiable> IStringifiable.GetElements()
        {
            return null;
        }
        void IStringifiable.SetAttributes(Dictionary<string, string> attributes)
        {
            Product product;
            int nInstr;
            bool isStandardOnly;
            foreach (KeyValuePair<string, string> keyValue in attributes)
            {
                if (keyValue.Key == "Product" && Product.TryDeserialize(keyValue.Value, out product))
                    this.m_Product = product;
                else if (keyValue.Key == "nInstrumentsToRecord" && int.TryParse(keyValue.Value, out nInstr))
                    this.m_nInstrumentsToRecord = nInstr;
                else if (keyValue.Key == "StandardInstrumentsOnly" && bool.TryParse(keyValue.Value, out isStandardOnly))
                    this.m_IsStandardInstrumentsOnly = isStandardOnly;
            }
        }
        void IStringifiable.AddSubElement(IStringifiable subElement)
        {

        }
        #endregion // IStringifiable
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Misty.Lib.IO.Xml
{
    /// <summary>
    /// This is a holding object for the XML handling libraries.  A node is a 
    /// stand-in for a object that has been extracted from an XML file.
    /// As a stand-in, its name will be the name of the true object that it represnents.
    /// </summary>
    public class Node : IStringifiable
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        public string Name = string.Empty;
        public List<IStringifiable> SubElements = new List<IStringifiable>();
        public Dictionary<string, string> Attributes;




        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Node()
        {
            this.Attributes = new Dictionary<string, string>();
        }
        public Node(string nodeName, Dictionary<string, string> attributes)
        {
            this.Name = nodeName;
            this.Attributes = new Dictionary<string, string>(attributes);
        }
        //
        //       
        #endregion//Constructors



        #region Public Methods
        // *****************************************************************
        // ****                     Public  Methods                     ****
        // *****************************************************************
        //
        //
        //
        /// <summary>
        /// Since A Node object is a "stand-in" for a real object, we usually don't want to 
        /// convert a Node into a string.  (If we did we would call the usual Stringifiable.Stringify( obj ).)
        /// This method will create a string for the object that the Node is standing in for.  That is, 
        /// the start tag will contain the object name stored in Node.Name, not the real class name for the
        /// node itself.
        /// </summary>
        /// <returns></returns>
        public string Stringify()
        {
            StringBuilder s = new StringBuilder();            
            Stringify(0,ref s);            
            return s.ToString();
        }//Stringify().
        //
        //
        private void Stringify(int level,ref StringBuilder s)
        {
            // Nice to read indenting for elements.           
            if (level > 0 )
                s.Append("\r\n");
            for (int i = 0; i < level; ++i)
                s.Append("    ");
                
            // Starting tag:
            s.AppendFormat("<{0}", this.Name);
            foreach (string attributeKey in this.Attributes.Keys)
                s.AppendFormat(" {0}={1}", attributeKey, this.Attributes[attributeKey]);
            

            // sub-Elements
            if (this.SubElements == null || this.SubElements.Count > 0)
            {
                s.Append(">");
                foreach (IStringifiable element in this.SubElements)
                {
                    if (element is Node)
                        ((Node)element).Stringify(level + 1, ref s);
                    else
                        s.AppendFormat("{0}", Stringifiable.Stringify(element));
                }
                // Ending tag
                s.AppendFormat("</{0}>", this.Name);
            }
            else
            {   // If there are no sub elements we can use the "complete" tag format for this object.
                s.Append("/>");
            }
        }// Stringify()
        //
        //
        //public new Type GetType()
        //{
        //    return this.Name.GetType();            
        //}
        /*
        string objectName = m_FillHub.GetType().FullName;
                string startTag = string.Format("<{0}", m_FillHub.GetType().FullName);
                string endTag = string.Format("</{0}", m_FillHub.GetType().FullName);
                Dictionary<string, string> attributes = new Dictionary<string,string>();
                int level = -1;         // default is not found.
                using (BackwardReader br = new BackwardReader(this.FileNamePath))
                {
                    bool isContinuing = true;
                    while (!br.SOF && isContinuing)
                    {
                        string aLine = br.Readline();
                        if (aLine.Contains(endTag))         // We found an endTag, we are enter a desired block (or a sub block).                        
                        {                            
                            level++;
                        }
                        else if (aLine.Contains(startTag))  // we will skip the FillHub tags - assume they are on their own lines!!
                        {
                            level--;
                            if (level < 0)
                            {
                                isContinuing = false;
                            }
                        }
                        else
                            lines.Add(aLine);
                    }//wend
                }//using reader
                lines.Reverse();
        */
        //
        public override string ToString()
        {
            return this.Name;
        }
        #endregion//Public Methods


        #region IStringifiable
        // *****************************************************************
        // ****                     IStringifiable                      ****
        // *****************************************************************
        public string GetAttributes()
        {
            StringBuilder s = new StringBuilder();
            s.AppendFormat("<{0}", this.GetType().FullName);
            foreach (string attributeKey in Attributes.Keys)
                s.AppendFormat(" {0}={1}", attributeKey, Attributes[attributeKey]);
            s.Append(">");
            return s.ToString();
        }
        public List<IStringifiable> GetElements()
        {
            if (this.SubElements.Count > 0)
                return new List<IStringifiable>(this.SubElements);
            else
                return null;
        }
        public void SetAttributes(Dictionary<string, string> attributes)
        {
            foreach (string s in attributes.Keys)
                this.Attributes.Add(s, attributes[s]);
        }
        public void AddSubElement(IStringifiable subElement)
        {
            this.SubElements.Add(subElement);
        }
        //
        #endregion//IStringifiable



    }
}

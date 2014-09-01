using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Tests.Serialization
{
    using UV.Lib.IO.Xml;

    public partial class ReadXMLBlocks : Form
    {

        private UV.Lib.Application.AppInfo m_AppInfo;
        private StringifiableReader m_Reader = null;

        //
        // Constructors
        //
        public ReadXMLBlocks()
        {
            InitializeComponent();
            m_AppInfo = UV.Lib.Application.AppInfo.GetInstance();

            // Example of how to use
            string fileName = string.Format("{0}Test.txt", m_AppInfo.UserPath);
            m_Reader = new StringifiableReader(fileName);
            List<IStringifiable> newObject = m_Reader.ReadToEnd(true);
            m_Reader.Close();

            
        }


        //
        //
        // Private methods
        //
        private void ReadAll()
        {            
            List<Node> topNodeList = new List<Node>();
            Node newNode = null;
            string tagName = string.Empty;
            StringifiableReader.TagType tagType;
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            while (m_Reader.TryReadNextTag(out tagName, out tagType, ref attributes))
            {
                switch (tagType)
                {
                    case StringifiableReader.TagType.StartTag:
                        newNode = new Node();
                        newNode.Name = tagName;
                        newNode.Attributes = new Dictionary<string, string>(attributes);
                        attributes.Clear();
                        topNodeList.Add(newNode);
                        break;
                    case StringifiableReader.TagType.CompleteTag:
                        newNode = new Node();
                        newNode.Name = tagName;
                        newNode.Attributes = new Dictionary<string, string>(attributes);
                        attributes.Clear();
                        break;
                    case StringifiableReader.TagType.EndTag:
                        break;
                    case StringifiableReader.TagType.Comment:
                        // dump comments
                        break;
                    default:
                        break;
                }
            }//wend
        }
        //
        //
        // Form event handlers
        //
        private void ReadXMLBlocks_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_Reader != null)
            {
                m_Reader.Close();
                m_Reader.Dispose();
            }
        }



    }
}

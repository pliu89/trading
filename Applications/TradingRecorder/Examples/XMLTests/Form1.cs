using System;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Data;
//using System.Drawing;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace XMLTests
{
    using System.Xml;

    public partial class Form1 : Form
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        private List<Fill> m_Fills = new List<Fill>();      
        //
        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public Form1()
        {
            InitializeComponent();
            CreateFakeData();

        }
        //
        //       
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        private void WriteXML()
        {            
            System.Xml.Serialization.XmlSerializer writer =
                new System.Xml.Serialization.XmlSerializer(typeof(Fill));
            
            System.IO.StreamWriter stream = new System.IO.StreamWriter("Test.xml", false);
            foreach (Fill aFill in m_Fills)
            {                
                writer.Serialize(stream, aFill);
            }            
            //writer.Serialize(stream, m_Fills);    // no good.
            stream.Close();
        }
        //
        private void ReadXML()
        {
            XmlTextReader reader = new XmlTextReader("Test.xml");
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: 
                        Console.Write("<" + reader.Name);
                        while (reader.MoveToNextAttribute()) // Read the attributes.
                           Console.Write(" " + reader.Name + "='" + reader.Value + "'");
                        Console.WriteLine(">");
                        break;
                    case XmlNodeType.Text: 
                        Console.WriteLine(reader.Value);//Display the text in each element.
                        break;
                    case XmlNodeType.EndElement:
                        Console.WriteLine(string.Format("</{0}>",reader.Name));
                        break;
                    case XmlNodeType.XmlDeclaration:
                        Console.WriteLine(string.Format("</{0}>", reader.Name));
                        break;
                }
            }



            //listBox1.BeginUpdate();
            //listBox1.Items.Clear();
            //listBox1.EndUpdate();

        }
        //
        //
        //
        private void CreateFakeData()
        {
            Random rand = new Random();

            for (int i = 0; i < 5; ++i)
            {
                Fill aFill = new Fill();
                aFill.TransactionTime = DateTime.Now.Subtract(new TimeSpan(0, rand.Next(59), rand.Next(59)));
                aFill.Price = Math.Round(rand.NextDouble() * 10000.0) / 100.0;
                aFill.Qty = rand.Next(11) - 5;
                m_Fills.Add(aFill);
            }
        }

        //
        //
        //
        //
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        //
        private void button1_Click(object sender, EventArgs e)
        {
            WriteXML();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ReadXML();
        }

        //
        #endregion//Event Handlers


    }
}

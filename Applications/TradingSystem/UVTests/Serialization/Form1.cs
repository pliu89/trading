using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UVTests.Serialization
{
    using System.Xml;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.IO;

    using UV.Lib.OrderHubs;
    using UV.Lib.Products;
    //using Ambre.TTServices.Orders;



    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Test1();



        }


        private void Test1()
        {
            InstrumentName name = new InstrumentName(new Product("CME", "GE", ProductTypes.Spread), "H2M2");
            FillBookLifo book = new FillBookLifo(0.5,12.50,name);

            List<Fill> aList = new List<Fill>();
            for (int i = 0; i < 2; ++i)
            {
                Fill aFill = Fill.Create();
                aFill.Qty = i + 2;
                aFill.Price = 1.0 + 0.5*i;
                aFill.LocalTime = DateTime.Now.AddHours(i * .25);
                aFill.ExchangeTime = aFill.LocalTime;
                aList.Add(aFill);
                //string s = SerializeToString(aFill).Replace('\n',' ');
                //textBox1.Text = textBox1.Text + s;
                book.Add(aFill);
   
            }
            textBox1.Text = SerializeToString(book);





        }
        //
        public static string SerializeToString(object obj)
        {
            
            XmlSerializer serializer = new XmlSerializer(obj.GetType());            
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);                
                return writer.ToString();
            }
        }

        public static T SerializeFromString<T>(string xml)
        {
            
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StringReader reader = new StringReader(xml))
            {
                return (T)serializer.Deserialize(reader);
            }
            

        }


        public static string SerializeToString2(object obj)
        {
            DataContractSerializer ser = new DataContractSerializer(obj.GetType());
            using (StringWriter writer = new StringWriter())
            {
                using (XmlWriter xw = XmlWriter.Create(writer))
                {
                    ser.WriteObject(xw, obj);
                }
                return writer.ToString();
            }
        }
        public static T SerializeFromString2<T>(string xml)
        {

            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            using (StringReader sreader = new StringReader(xml))
            {
                using (XmlReader reader = XmlReader.Create(sreader))
                {
                    return (T)serializer.ReadObject(reader);
                }
            }


        }


        private void buttonDeSerialize_Click(object sender, EventArgs e)
        {
            string s = textBox1.Text;
            FillBookLifo book = SerializeFromString<FillBookLifo>(s);
            //int n = 0;


        }




    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MistyTests.Serialization
{
    using Misty.Lib.Products;
    using Misty.Lib.OrderHubs;
    using Misty.Lib.IO.Xml;


    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            Serialize();

        }

        private Dictionary<InstrumentName, FillBookLifo> m_FillBooks = new Dictionary<InstrumentName, FillBookLifo>();

        //
        // ****             Serialize()            ****
        //
        private void Serialize()
        {
            // Create some books.
            StringBuilder msg = new StringBuilder();
            System.Random rand = new Random();
            for (int i = 0; i < 2; ++i)
            {
                InstrumentName name = new InstrumentName(new Product("CME", "ED", ProductTypes.Spread), string.Format("#{0}", i + 1));
                double minTick = 0.5;
                FillBookLifo book = new FillBookLifo(minTick, 12.50, name);
                for (int j = 0; j < 2; ++j)
                {
                    Fill aFill = Fill.Create();
                    aFill.Price = rand.Next(20) * minTick;
                    aFill.Qty = rand.Next(10) + 2;
                    aFill.LocalTime = DateTime.Now.AddSeconds(-rand.NextDouble() * 100);
                    aFill.ExchangeTime = aFill.LocalTime;
                    book.Add(aFill);
                }
                m_FillBooks.Add(name, book);

                msg.AppendFormat("{0}", Stringifiable.Stringify(book));

            }// next i

            textBox1.Text = msg.ToString();
            this.Select();

        }// Serialize().
        //
        // 
        private void button1_Click(object sender, EventArgs e)
        {
            /*
            List<IStringifiable> objects = Stringifiable.Destringify(textBox1.Text);
            List<FillBookLifo> books = new List<FillBookLifo>();
            foreach (IStringifiable obj in objects)
            {
                if (obj is FillBookLifo)
                    books.Add((FillBookLifo)obj);

            }
            */
        }







    }
}

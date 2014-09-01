using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

//using System.Threading;
using System.Windows.Threading;


namespace DispatcherExample
{
    public partial class Form1 : Form
    {

        // 
        // ****                 Constructors                    ****
        //
        public Form1()
        {
            InitializeComponent();

            //LoadList();
        }

        //
        // ****                Private methods                 ****
        //
        private void LoadList()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < 1000; i++)
                list.Add(string.Format("Number {0}", i.ToString()));
            listBox1.DataSource = list;
        }


        //
        // ****                 Event Handlers                  ****
        //
        private void Form1_Load(object sender, EventArgs e)
        {
            //LoadList();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //
            // Dispatcher class usage.
            //      this is found in namespace System.Windows.Threading  inside the WindowsBase library.
            //      
            Dispatcher disp = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            //WorkerDispatcher
            disp.BeginInvoke(new Action(this.LoadList), DispatcherPriority.Background);

        }





    }
}

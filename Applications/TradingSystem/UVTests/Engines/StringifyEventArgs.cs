using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UVTests.Engines
{
    using UV.Lib.Utilities;
    using UV.Lib.Engines;



    public partial class StringifyEventArgs : Form
    {
        public int data = 0;
        public EngineEventArgs.EventStatus status = EngineEventArgs.EventStatus.None;


        public StringifyEventArgs()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            // Change data
            data++;
            object o = data;


            // store data
            string s = o.ToString();
            int result = ConvertType.ChangeType<int>(s);            
        }
        private void button2_Click(object sender, EventArgs e)
        {
            // Change data           
            object o = status;
            status++;

            // store data
            string s = o.ToString();
            EngineEventArgs.EventStatus result = ConvertType.ChangeType<EngineEventArgs.EventStatus>(s);            
        }

    }//end form



}

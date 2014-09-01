using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using UV.Lib.FrontEnds.Utilities;
using System.Timers;

namespace UVTests.GUIs
{
    public partial class CreateForms : Form
    {
        //
        // Members
        //
        private System.Timers.Timer m_Timer;
        private int MaxCount = 3;

        
        //
        //
        //
        public CreateForms()
        {
            InitializeComponent();
            System.Threading.Thread.CurrentThread.Name = "GUI";
            LabelWrite(MaxCount.ToString());

            GuiCreator.Create();
            //m_Creator.FormCreated += Creator_FormCreated;

        }





        //
        // Event Handlers
        //
        void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            m_Timer.Enabled = false;
            
            int n = 0;
            string s = textCount.Text;
            if (int.TryParse(s, out n))
            {
                n -= 1;
                if (n < 0)
                {
                    GuiCreator c = GuiCreator.Create(typeof(NotifyTest),"silly", "name");
                    c.FormCreated += GuiCreator_FormCreated;
                    c.Start();
                    n = MaxCount;
                }
                else
                {

                }
                //
                LabelWrite( n.ToString() );
            }

            m_Timer.Enabled = true ;
        }
        //


        void GuiCreator_FormCreated(object sender, EventArgs eventArgs)
        {
            GuiCreator.CreateFormEventArgs e = (GuiCreator.CreateFormEventArgs)eventArgs;           
        }




        private void CreateForms_Load(object sender, EventArgs e)
        {
            m_Timer = new System.Timers.Timer();
            m_Timer.Elapsed += Timer_Elapsed;
            m_Timer.AutoReset = true;
            m_Timer.Interval = 1000;
            m_Timer.Start();
            m_Timer.Enabled = true;
        }




        public void LabelWrite(string value)
        {
            if (InvokeRequired)
                Invoke(new LabelWriteDelegate(LabelWrite), value);
            else
            {
                textCount.Text = value;
            }
        }
        delegate void LabelWriteDelegate(string value);



    }
}

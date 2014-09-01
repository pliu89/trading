using System;
using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
using System.Drawing;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Tests.Utilities
{
    using UV.Lib.Utilities;
    using UV.Lib.Utilities.Alarms;

    public partial class TestUtilities : Form
    {

        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public TestUtilities()
        {
            InitializeComponent();
        }
        //
        //       
        #endregion//Constructors


        #region Alarms
        // *****************************************************************
        // ****                     Alarms                             ****
        // *****************************************************************
        private Alarm m_Alarm = null;
        //
        private void AlarmStart_Click(object sender, EventArgs e)
        {
            // Create alarm if not already existing
            if (m_Alarm == null)
            {
                m_Alarm = new Alarm();
                m_Alarm.SlopSeconds = 0.20;             // lower the slop 
            }
            // Load events
            DateTime dt = DateTime.Now.AddSeconds(2.0); // ask for call back in two seconds
            MyMsg msg = new MyMsg();
            msg.dateTime = dt;
            m_Alarm.Set(dt, AlarmCallback, msg);

        }// AlarmStart_Click()
        //
        //
        //
        private void AlarmCallback(object sender, EventArgs eventArgs)
        {
            if ( this.InvokeRequired )
            {
                this.Invoke(new EventHandler(AlarmCallback), sender, eventArgs);
            }
            else
            {
                DateTime dt = DateTime.Now;
                MyMsg e = (MyMsg)eventArgs;
                string s = string.Format("\r\nAlarm set for {0:HH:mm:ss.fff}.  Callback received {1:HH:mm:ss.fff}.",e.dateTime, dt);
                textBoxOutput.AppendText(s);
            }

        }// AlarmCallback()
        //
        //
        private class MyMsg : EventArgs
        {
            public DateTime dateTime;
        }

        //
        //
        #endregion// members


        #region HMA
        // *****************************************************************
        // ****                         HMA                             ****
        // *****************************************************************
        //
        //
        //private UV.Math.Filters.EmaHull m_EmaHull = null;
        //
        private double[] sine = new double[]{0.000000000000000000e+00,7.845909572784494357e-02,1.564344650402308690e-01,2.334453638559053912e-01,3.090169943749473958e-01
            ,3.826834323650897818e-01,4.539904997395467490e-01,5.224985647159487989e-01,5.877852522924731371e-01,6.494480483301836582e-01,7.071067811865474617e-01
            ,7.604059656000308198e-01,8.090169943749474513e-01,8.526401643540921782e-01,8.910065241883677878e-01,9.238795325112867385e-01,9.510565162951535312e-01,9.723699203976765570e-01
            ,9.876883405951377704e-01,9.969173337331279638e-01,1.000000000000000000e+00,9.969173337331279638e-01,9.876883405951377704e-01,9.723699203976766681e-01,9.510565162951536422e-01
            ,9.238795325112867385e-01,8.910065241883678988e-01,8.526401643540922892e-01,8.090169943749474513e-01,7.604059656000309309e-01,7.071067811865475727e-01,6.494480483301837692e-01,5.877852522924732481e-01,5.224985647159489099e-01,4.539904997395468600e-01,3.826834323650898928e-01,3.090169943749475068e-01,2.334453638559055300e-01,1.564344650402309800e-01,7.845909572784506847e-02,1.224646799147353207e-16,-7.845909572784437458e-02,-1.564344650402307302e-01,-2.334453638559052802e-01,-3.090169943749468962e-01,-3.826834323650896152e-01
            ,-4.539904997395466379e-01,-5.224985647159491320e-01,-5.877852522924730261e-01,-6.494480483301832141e-01,-7.071067811865474617e-01,-7.604059656000305978e-01,-8.090169943749473402e-01,-8.526401643540924002e-01,-8.910065241883677878e-01,-9.238795325112868495e-01,-9.510565162951535312e-01,-9.723699203976764460e-01,-9.876883405951376593e-01,-9.969173337331279638e-01,-1.000000000000000000e+00,-9.969173337331279638e-01,-9.876883405951377704e-01,-9.723699203976765570e-01,-9.510565162951536422e-01,-9.238795325112869605e-01,-8.910065241883680098e-01,-8.526401643540925113e-01
            ,-8.090169943749475623e-01,-7.604059656000308198e-01,-7.071067811865476838e-01,-6.494480483301834361e-01,-5.877852522924733591e-01,-5.224985647159494651e-01,-4.539904997395469710e-01
            ,-3.826834323650903924e-01,-3.090169943749476733e-01,-2.334453638559051969e-01,-1.564344650402310910e-01,-7.845909572784474928e-02,-2.449293598294706414e-16,7.845909572784424968e-02,1.564344650402297310e-01,2.334453638559055855e-01,3.090169943749471737e-01,3.826834323650899483e-01,4.539904997395465824e-01,5.224985647159482438e-01
            ,5.877852522924722489e-01,6.494480483301837692e-01,7.071067811865473507e-01,7.604059656000304868e-01,8.090169943749472292e-01,8.526401643540918451e-01,8.910065241883681209e-01,9.238795325112868495e-01,9.510565162951535312e-01,9.723699203976764460e-01,9.876883405951375483e-01,9.969173337331279638e-01    
            };

        /// <summary>
        /// Compare against uv.math library functions:
        ///  T = 7.0; 
        ///  alpha = uvmath.period2alpha(T); 
        ///  [hma,short,orig] = uvmath.GetFilterHull(np.sin(x),alpha);
        /// pl.plot(x,np.sin(x),'o',x,hma,'-k',x,orig,'-b',x,short,'-r'); pl.show()
        /// 
        /// alpha = 0.820335356007638;
        /// </summary>        
        private void buttonHMA_Click(object sender, EventArgs e)
        {
            //m_EmaHull = new UV.Math.Filters.EmaHull();
            //m_EmaHull.TimeScale = 1;
            //m_EmaHull.Lifetime = 7.0;
            //// Test alpha calculation
            //double alpha = m_EmaHull.Alpha;
            //string s = string.Format("\r\nHMA alpha = {0}",alpha);
            //textBoxOutput.AppendText(s);
            //// Test sine wave.
            //m_EmaHull.EMAHull.CurrentValue = 0;
            //m_EmaHull.EMAOrig.CurrentValue = 0;
            //m_EmaHull.EMAShort.CurrentValue = 0;
            //foreach (double x in sine)
            //{
            //    m_EmaHull.Update(x);
            //    s = string.Format("\r\nHMA {0:e6} shrt {1:e6} orig {2:e6}", m_EmaHull.CurrentValue,m_EmaHull.EMAShort.CurrentValue,m_EmaHull.EMAOrig.CurrentValue);
            //    textBoxOutput.AppendText(s);
            //}




        }
        //
        //
        //
        //
        //
        //
        #endregion//HMA


        #region Mode-less Message Box
        //
        //
        //
        private void buttonSpawnMessageBox_Click(object sender, EventArgs e)
        {
            //UV.Lib.FrontEnds.Utilities.GuiCreator.ShowMessageBox(string.Format("Show time {0}", DateTime.Now.ToShortTimeString()), "Test", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            UV.Lib.FrontEnds.Utilities.GuiCreator.ShowMessageBox(string.Format("Show time {0}", DateTime.Now.ToShortTimeString()), "Test");
        }
        //
        #endregion



        #region Global Form-Wide Events
        // *****************************************************************
        // ****                     Events                              ****
        // *****************************************************************
        private void TestUtilities_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_Alarm != null)
            {
                m_Alarm.Dispose();
            }

        }
        
        #endregion // form wide events



    }//end class
}

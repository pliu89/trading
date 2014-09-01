using System;
using System.Collections.Generic;
//using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ambre.PositionViewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            /*
            // Original version of Ambre
            FormPositionViewer mainForm = null ;
            mainForm = new FormPositionViewer();
            */

            // Make sure only one copy of Ambre is running.
            System.Diagnostics.Process[] procs = null;
            try
            {
                //procs = System.Diagnostics.Process.GetProcesses();
                procs = System.Diagnostics.Process.GetProcessesByName("AmbrePositionViewer");
            }
            catch (Exception)
            {
                procs = null;
            }
            System.Windows.Forms.DialogResult result = DialogResult.OK;
            if (procs != null && procs.Length > 1)
            {
                 result = System.Windows.Forms.MessageBox.Show("Ambre is detecting another instance. Continue?", "Ambre", MessageBoxButtons.OKCancel);
            }
            if (result == DialogResult.OK)
            {
                FrontEnds.AmbreViewer mainForm = null;
                mainForm = new FrontEnds.AmbreViewer();
                Application.Run(mainForm);
            }
            else
            {
                
            }


        }
    }
}

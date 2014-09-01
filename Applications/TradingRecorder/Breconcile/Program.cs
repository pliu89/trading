using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ambre.Breconcile
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);



            /*
            Application.Run(new Form1(args));
            */

            
            // BookReaders
            // Application.Run(new BookReaders.FormBookViewer(args));

             //BookReaders - Version 2
             //Application.Run(new BookReaders.FormEventPlayer());



            // Test FTP to ABN.
            //ABN.StatementReader sr = new ABN.StatementReader();


            // Reconciliation.
            Application.Run(new Reconciler.ReconcilerForm(args));


        }
    }
}

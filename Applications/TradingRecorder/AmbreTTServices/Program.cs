using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ambre.TTServices
{
    using System.Threading;

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            /*
            // Test: ConsolePriceSubscription
            TTAPIFunctions tf = new TTAPIFunctions();
            Thread workerThread = new Thread(tf.Start);
            workerThread.Name = "TT API Thread";
            workerThread.Start();
            */

            
             //Test: TestMarket 
             //Explore the available servers, their products and instruments.
             //Get live markets for one instrument on screen.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Tests.TestMarket());
            

            /*
            // Talker Stand-alone application
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Talker.FormStartTalkerHub());
            */



            //*
            // Market Monitor - small application for saving market/order books for small collection of instruments.
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Tests.MarketMonitor.Monitor());
            //*/


        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Tests
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

            //Misty.Lib.Application.AppInfo appInfo = Misty.Lib.Application.AppInfo.GetInstance("MistyTests",true);
            //appInfo.CreateDirectories();            // this creates a user and log directories below MistyTests directory/

           
            //
            // Sockets
            //
            //Application.Run(new Sockets.SocketServer());	    // Socket tests: this will open SocketClients.

            //
            // Serialization
            //
            //Application.Run(new Serialization.Form1());
            //Application.Run(new Serialization.Form2());	      
            //Application.Run(new Serialization.ReadXMLBlocks());	      

            //
            // Run code
            //
            //Utilities.StartAnotherProcess.Start(null);

            //
            // Notify and Icons
            //
            //Application.Run(new GUIs.NotifyTest());

            //
            // Creation of GUIs
            //
            //Application.Run(new GUIs.CreateForms());

            //
            // Test for Database Reader Writers
            //
            //Application.Run(new DatabaseReaderWriters.Test1());

            //
            // Test Utilities
            //
            Application.Run(new Utilities.TestUtilities());

            //
            // Engines
            //
            //Application.Run(new  Engines.StringifyEventArgs());

            //
            //
            //
            //Application.Run(new Filters.FilterTest());

            //
            // IconMaker
            //
            //Application.Run(new IconMaker.IconMaker());


        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//using System.ComponentModel;
//using System.Data;
//using System.Linq;
//using System.Threading.Tasks;

namespace MrData.FrontEnds
{
    using Misty.Lib.Application;

    /// <summary>
    /// This is the frontend control for MrData.  MrData is a market-bar generating Hub 
    /// that owns multiple WriterHubs (of whatever type: Database, Textfile, etc) and has
    /// a MrDataPanel to display its state an operational controls.
    /// MrData is modular, so multiple instances can be created, each is an independent service.
    /// </summary>
    public partial class FormMrData : Form
    {
        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        // Application controls:
        private bool m_ShuttingDown = false;


        
        // Constants 
        private Color m_ColorOff = Color.DarkRed;
        private Color[] m_ColorsOff = new Color[] { Color.DarkRed, Color.Red }; // allows flashing capability.
        private Color m_ColorOn = Color.Green;

        //
        //
        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        public FormMrData(string[] args)
        {
            InitializeComponent();
            AppServices appServices = AppServices.GetInstance("MrData");// Register this application - creates user directories.
            
            // Read command-line arguments
            bool isAutoStart = false;
            foreach (string s in args)                              // Search for known arguments.
            {
                if (s.Equals("-Start", StringComparison.CurrentCultureIgnoreCase))
                    isAutoStart = true;
            }
            // Load services in config file.
            typeof(Ambre.TTServices.Markets.MarketTTAPI).ToString();    // Force loading of assemblies with objects we will load.
            appServices.LoadServicesFromFile( "MrDataStartup.txt" );
            appServices.Info.RequestShutdownAddHandler(new EventHandler(this.RequestShutdownHandler)); // let main form know about shutdown requests          
            appServices.Start();


            // Take additional startup actions.
            if (isAutoStart)
            {
                //StartMrData();
                //buttonStart.Text = "auto started";
                //buttonStart.Enabled = false;

            }
        }// Constructor
        //
        //       
        #endregion//Constructors


        #region no Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        //
        #endregion//Properties


        #region no Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        //
        //
        //
        //
        #endregion//Public Methods


        #region Private Methods
        // *****************************************************************
        // ****                     Private Methods                     ****
        // *****************************************************************
        //
        private void Shutdown()
        {
            if (!m_ShuttingDown)
            {
                m_ShuttingDown = true;
                AppServices.GetInstance().Shutdown();
            }
        }//Shutdown().
        //
        #endregion//Private Methods


        #region Event Handlers
        // *****************************************************************
        // ****                     Event Handlers                     ****
        // *****************************************************************
        /// <summary>
        /// Provide to allow services to request a shutdown for the entire application.
        /// This is called by the AppService.RequestShutdown(), it is up to this method
        /// to properly shut the system down.
        /// </summary>
        private void RequestShutdownHandler(object sender, EventArgs eventArgs)
        {
            Shutdown();
            this.Close();
        }
        private void FormMrData_FormClosing(object sender, FormClosingEventArgs e)
        {
            Shutdown();
        }
        //
        #endregion//Event Handlers

    }
}

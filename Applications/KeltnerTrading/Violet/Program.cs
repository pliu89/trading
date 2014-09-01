using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Violet
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
            
            UV.Lib.Application.AppServices.GetInstance("Violet");
            //
            // Force all application assemblies to load.
            //
            typeof(UV.TTServices.Markets.MarketTTAPI).ToString();
            typeof(UV.Strategies.StrategyHubs.StrategyHub).ToString();
            typeof(UV.Strategies.ExecutionHubs.ExecutionHub).ToString();
            typeof(UV.Strategies.ExecutionEngines.OrderEngines.Spreader).ToString();

            //
            // Run Violet
            // 
            FrontEnds.Violet1 mainForm = null;
            mainForm = new FrontEnds.Violet1(args);
            Application.Run(mainForm);
           
        }
    }
}

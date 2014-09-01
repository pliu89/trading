using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// *****************************************************************
// ****            Update AmbreUserName ConfigFiles()           ****
// *****************************************************************
/// <summary>
/// Originally, the config files' FillHubs do not contain a AmbreUserName. 
/// So, this routine is called to upgrade the users' config file.
/// </summary>
/// 

namespace Ambre.PositionViewer.FrontEnds
{
    using SKACERO;

    using Misty.Lib.Application;
    using Misty.Lib.Utilities;
    using Misty.Lib.Hubs;
    using Misty.Lib.FrontEnds;

    using InstrumentName = Misty.Lib.Products.InstrumentName;

    using Ambre.TTServices.Markets;
    using Ambre.TTServices.Fills;
    using Ambre.TTServices.Talker;

    using TradingTechnologies.TTAPI.WinFormsHelpers;

    using Microsoft.Win32;

    public partial class FormUpdateAmbreName : Form
    {
        //public string AmbreName;
        //{
        //    get { return textBoxAmbreName.Text; }
        //}

        public FormUpdateAmbreName(string aLine)
        {
            InitializeComponent(aLine);
            
        }

        private void Button_Click(object sender, EventArgs e)
        {
            if (sender == buttonCreateNewHub)
            {
                if (textBoxAmbreName.Text != string.Empty)
                {
                    this.Tag = textBoxAmbreName.Text;
                    this.Hide();
                }
                else System.Windows.Forms.MessageBox.Show("User Name is needed.");
            }

        }


    }
}

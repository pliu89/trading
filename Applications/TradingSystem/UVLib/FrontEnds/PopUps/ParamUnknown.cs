using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.PopUps
{
    using UV.Lib.Engines;

    /// <summary>
    /// This is a blank place holder for engine parameters of unknown type.
    /// Only the name of the parameter is listed.
    /// </summary>
    public partial class ParamUnknown : ParamControlBase
    {
        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public ParamUnknown(ParameterInfo pInfo)
            : base(pInfo)
        {
            InitializeComponent();
            this.ParameterName.Text = pInfo.DisplayName;
        }
        public ParamUnknown() : base() { InitializeComponent(); } // needed for designer
        //

        //       
        #endregion//Constructors


        public override void InitializeParameter(ParameterInfo pInfo)
        {
            base.InitializeParameter(pInfo);
            //this.ParameterName.Text = m_ParameterInfo.DisplayName;
            //this.tbParameterValue.ReadOnly = m_ParameterInfo.IsReadOnly;
        }

    }
}

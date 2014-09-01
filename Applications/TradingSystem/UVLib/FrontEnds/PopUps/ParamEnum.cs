using System;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Drawing;
//using System.Data;
//using System.Linq;
//using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace UV.Lib.FrontEnds.PopUps
{
    using UV.Lib.Engines;

    public partial class ParamEnum : ParamControlBase
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        //
        //string[] m_Fields


        // Rules for on-off.
        private Color[] m_Colors = new Color[] { Color.DarkRed, Color.DarkGreen };
        private BorderStyle[] m_BorderStyles = new BorderStyle[] { BorderStyle.Fixed3D, BorderStyle.FixedSingle };
        // Rules for read-only
        private Color[] m_ReadOnlyColors = new Color[] { Color.Black, Color.Bisque };


        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        //
        public ParamEnum(ParameterInfo pInfo)
            : base(pInfo)
        {
            InitializeComponent();
            InitializeParameter(pInfo);
        }
        public ParamEnum() { InitializeComponent(); } // needed for designer
        //
        //
        public override void InitializeParameter(ParameterInfo pInfo)
        {
            base.InitializeParameter(pInfo);
            this.label.Text = pInfo.DisplayName;
            this.label.Visible = false;
            //int originalWidth = comboBox.DropDownWidth;
            //int height = this.ClientSize.Height;
            //int width = this.ClientSize.Width;

            // load combobox.
            Type type = m_ParameterInfo.ValueType;
            FieldInfo[] fields = type.GetFields();
            bool hasGenerics = type.ContainsGenericParameters;
            foreach (FieldInfo field in fields)
            {
                if (field.IsPublic && !field.IsSpecialName)
                    comboBox.Items.Add(field.Name);
            }
            if (comboBox.Items.Count > 0)
                m_Memory = comboBox.Items[0].ToString();

            
            //int comboWidth = comboBox.DropDownWidth;
            //this.ClientSize = new Size(width, height);


        }
        #endregion//constructor

        #region Properties
        // *****************************************************************
        // ****                     Properties                          ****
        // *****************************************************************
        //
        public override string Text
        {
            get
            {
                return label.Text;
            }
            set
            {
                this.label.Text = value;
            }
        }
        //
        #endregion//properties

        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****             SetValue()              ****
        //
        /// <summary>
        /// Implements ParamControlBase.
        /// This method updates the internal memory of this control, and so
        /// can be called by any thread.
        /// </summary>
        /// <param name="o"></param>
        public override void SetValue(object o)
        {
            string newValue = o.ToString();
            string oldValue = m_Memory.ToString();
            try
            {
                if (Enum.IsDefined(m_ParameterInfo.ValueType, o))
                    m_Memory = newValue;
            }
            catch (Exception)
            {
                if (Enum.IsDefined(m_ParameterInfo.ValueType, oldValue))
                    m_Memory = oldValue;		// maintain previous value.					
            }
        }
        //
        // ****             Regenerate()            ****
        //
        /// <summary>
        /// This method replaces the displayed quantity with that stored in memory.
        /// This must be called by windows thread only!
        /// </summary>
        public override void Regenerate()
        {
            string valueName = m_Memory.ToString();
            if (Enum.IsDefined(m_ParameterInfo.ValueType, m_Memory))
            {
                object o = comboBox.SelectedItem;
                if (o == null || (!comboBox.SelectedItem.ToString().Equals(valueName)))
                {
                    comboBox.SelectedItem = valueName;
                }
            }
        }
        //
        //
        #endregion//public methods




        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        private void Button_Resized(object sender, EventArgs e)
        {
            int width = Math.Max(comboBox.Width, label.Width);
            int height = this.Height;
            this.ClientSize = new Size(width, height);
        }
        //
        //
        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            string newStr = cb.SelectedItem.ToString();
            string oldStr = m_Memory.ToString();
            if (!oldStr.Equals(newStr))
            {
                object newValue = Enum.Parse(m_ParameterInfo.ValueType, newStr);
                m_RemoteEngineHub.HubEventEnqueue(EngineEventArgs.RequestParameterChange(m_ParameterInfo, newValue));
            }
        }
        //
        //
        //
        #endregion//public methods



    }
}

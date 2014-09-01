using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.PopUps
{
    using UV.Lib.Engines;
    using UV.Lib.Application;


    //
    // ****         Param Control Base              ****
    //
    /// <summary>
    /// This is the base class for a control that displays a single parameter.
    /// </summary>
    public partial class ParamControlBase : UserControl
    {

        #region Members
        // *********************************************************************
        // ****                         Members                             ****
        // *********************************************************************
        //
        protected object m_Memory = null;                           // contains value to be displayed.
        public ParameterInfo m_ParameterInfo = null;             // contains information for my parameter.
        protected IEngineHub m_RemoteEngineHub = null;              // enginehub to which requests are sent!

        #endregion//members


        #region Constructor
        // *********************************************************************
        // ****                       Constructor                           ****
        // *********************************************************************
        //
        public ParamControlBase(ParameterInfo parameterInfo)
            : base()
        {
            
        }
        public ParamControlBase() : base() 
        {
        }  // this is required for Designer
        /*
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ParamControlBase
            // 
            this.Name = "ParamControlBase";
            this.Size = new System.Drawing.Size(155, 98);
            this.ResumeLayout(false);

        }
        */ 
        #endregion//constructor



        #region Virtual Methods
        // *********************************************************************
        // ****                  Virtual Methods                            ****
        // *********************************************************************
        //
        /// <summary>
        /// This overwrites the value of the parameter that is stored in Memory.
        /// But does not update the on screen value.
        /// </summary>
        /// <param name="o"></param>
        public virtual void SetValue(object o)
        {

        }
        //
        //
        /// <summary>
        /// This must be called by windows thread only!  
        /// Call this method when you want to update the displayed value to make it
        /// reflect the stored value in Memory.
        /// </summary>
        public virtual void Regenerate()
        {
        }
        //
        //
        //
        /// <summary>
        /// This must be called by windows thread only!
        /// Call this method in the constructor when the method is first created.
        /// Also, HUD creation sometimes calls this method too.  
        /// </summary>
        /// <param name="parameterInfo"></param>
        public virtual void InitializeParameter(ParameterInfo parameterInfo)
        {
            m_ParameterInfo = parameterInfo;

            // Each parameter info object knows to whom to send engine requests.            
            IService iService;
            if (AppServices.GetInstance().TryGetService(m_ParameterInfo.EngineHubName, out iService) && (iService is IEngineHub))
                m_RemoteEngineHub = (IEngineHub)iService;

            if (m_ParameterInfo.ValueType.IsValueType)
                m_Memory = Activator.CreateInstance(m_ParameterInfo.ValueType);
            else
                m_Memory = new object();


        }
        //
        //
        //
        //public virtual string Text
        //{
        //    get { return String.Empty;}
        //    set { return; }
        //}
        //
        //
        #endregion//Virtural Methods




    }//end class
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.Utilities
{
    using System.Windows.Threading;
    using UV.Lib.Application;


    /// <summary>
    /// Used to create GUIs from threads other than the UI thread.  
    /// Usage: 
    ///     1) Create an instance of the UI Dispatched by calling Create() with NO args on the UI thread!
    ///     2) From then on, anyone can create a new instance by calling Create( formType, args[]), 
    ///         Step 1:     Create a new instance of the GuiCreator for that user, but this instance
    ///                     already has a pointer to the correct UI Dispatcher.
    ///         Step 2:     Then, subscribe to the event in this private GuiCreator object.
    ///         Step 3:     Then, call its "Start()" method.
    ///     3) When the event is triggered, the created form will be inside.
    /// TODO:
    ///     1) We need to manage removing delegates after final call.  MAke reusable ?
    /// </summary>
    public class GuiCreator
    {

        #region Members
        // *****************************************************************
        // ****                     Members                             ****
        // *****************************************************************
        // Static:
        private static Dispatcher m_GuiDispatcher = null;           // singleton gui dispatcher

        // Local variables.
        private CreateFormEventArgs m_EventArgs = null;

        #endregion// members


        #region Constructors
        // *****************************************************************
        // ****                     Constructors                        ****
        // *****************************************************************
        private GuiCreator()
        {
            if (m_GuiDispatcher == null)          // First time 
                m_GuiDispatcher = Dispatcher.CurrentDispatcher;
        }
        //
        public static GuiCreator Create()
        {
            return new GuiCreator();
        }
        //
        //
        //
        public static GuiCreator Create( Type formType, params object[] constructorArgs )
        {
            GuiCreator guiCreator = null;
            bool isValid = formType.IsSubclassOf(typeof(Form)) || formType== typeof(Form);
            if (isValid)
            {
                guiCreator = new GuiCreator();
                CreateFormEventArgs eventArgs = new CreateFormEventArgs();
                eventArgs.FormType = formType;
                if (constructorArgs != null && constructorArgs.Length > 0)
                    eventArgs.ConstructorArgs = constructorArgs;
                guiCreator.m_EventArgs = eventArgs;
            }
            return guiCreator;
        }// Create()
        //
        //       
        #endregion//Constructors



        #region Public Methods
        // *****************************************************************
        // ****                     Public Methods                      ****
        // *****************************************************************
        //
        //
        // ****             Start()             ****
        //
        public void Start()
        {
            m_GuiDispatcher.BeginInvoke(new EventHandler(CreateForm), new object[] { this, this.m_EventArgs });
        }
        //
        //
        //
        // *************************************************
        // ****         TryCreateControl()              ****
        // *************************************************
        /// <summary>
        /// Static function to aid in Control creation.
        /// Must be called by the UI thread.
        /// </summary>
        /// <param name="newControl"></param>
        /// <param name="t"></param>
        /// <param name="constructorArgs"></param>
        /// <returns></returns>
        public static bool TryCreateControl(out Control newControl, Type t, params object[] constructorArgs)
        {
            bool isSuccess = false;
            newControl = null;
            try
            {
                if (t.IsSubclassOf(typeof(Control)))
                {
                    newControl = Activator.CreateInstance(t, constructorArgs) as Control;
                    isSuccess = true;
                }
            }
            catch (Exception)
            {
                isSuccess = false;
            }
            return isSuccess;
        }//TryCreateControl()
        //
        //
        //
        #endregion//Public Methods


        #region Private Stuff 
        // *****************************************************************
        // ****                     Private Stuff                       ****
        // *****************************************************************
        /// <summary>
        /// This is the method called using the Dispatcher of the UI thread.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="eventArgs"></param>
        private void CreateForm(object obj, EventArgs eventArgs)
        {
            CreateFormEventArgs e = (CreateFormEventArgs) eventArgs;
            Type t = e.FormType;
            Form form = null;
            if ( e.ConstructorArgs == null )
                form = Activator.CreateInstance(t) as Form;
            else
                form = Activator.CreateInstance(t,e.ConstructorArgs) as Form;
            if (form != null)
            {
                e.CreatedForm = form;
                form.Show();
                OnFormCreated(e);
            }
        }// CreateForm()
        //
        //
        //
        // *************************************************
        // ****         Create Form EventArgs           ****
        // *************************************************
        //
        //
        public event EventHandler FormCreated;                  // Callback event
        //
        //
        public class CreateFormEventArgs : EventArgs
        {
            public Type FormType = null;
            public object[] ConstructorArgs = null;

            // Output
            public Form CreatedForm = null;
        }
        //
        //
        //
        private void OnFormCreated(CreateFormEventArgs e)
        {
            if (FormCreated != null)
                FormCreated(this, e);
        }// OnFormCreated()
        //
        //
        //
        #endregion//Private Methods



        #region Show Message Box
        // *****************************************************************
        // ****                 Show Message Box                        ****
        // *****************************************************************
        //
        private static bool m_MessageBoxInUse = false;          // global-static denoting whether box is already displayed.
        //
        /// <summary>
        /// A simple asynchronous way to open a Message box.  The caller is
        /// immediately released, and internally a flag is set to allow only one
        /// message box at a time.
        /// </summary>
        /// <param name="msgText">Possibly multiple line of text for body of message.</param>
        /// <param name="captionText">Title of popup form.</param>
        /// <param name="buttons">Buttons to show.</param>
        /// <param name="icon">Icon image to show in box.</param>
        /// <returns></returns>
        public static bool ShowMessageBox(string msgText, string captionText, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Warning)
        {
            bool isSuccessful = false;
            if (GuiCreator.m_MessageBoxInUse == false)
            {
                GuiCreator.m_MessageBoxInUse = true;
                new System.Threading.Thread(new System.Threading.ThreadStart(
                    delegate { 
                        MessageBox.Show(msgText, captionText, buttons, icon);
                        GuiCreator.m_MessageBoxInUse = false;
                    })).Start();
                isSuccessful = true;
            }
            return isSuccessful;
        }
        //
        //
        //
        #endregion//Public Methods


    }
}

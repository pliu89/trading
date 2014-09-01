using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Collections.Generic;

namespace UV.Lib.FrontEnds.Clusters
{
    public class BoxNumeric : Label
    {

        #region Member variables
        // *********************************************************************
        // ****						Member variables						****
        // *********************************************************************
        //
        //
        // Dynamic member variables.
        //
        public readonly int BoxID;									// an ID number set at construction.
        protected int m_Value = 0;									// integer value of entry.


        private int m_ForeColorHiLite = 0;							// current foreColor value.
        private const int NumberOfHiLites = 3;						// number of hilite styles allowed.

        private Color m_BackColorPositive;												// color for positive values.
        private Color m_BackColorNegative;												// color for negative values.
        private Color[] m_ForeColorPositive = new Color[NumberOfHiLites];	// color for positive values.
        private Color[] m_ForeColorNegative = new Color[NumberOfHiLites];	// color for negative values.
        private Color[] m_ForeColorZero = new Color[NumberOfHiLites];		// color for zero values.

        private Color m_ForeColorDefault = Color.White;			// text color.		
        private Color m_BackColorDefault = Color.SlateGray;		// text box background color.


        //
        // Layout controls
        //
        public static int ValueMaxRange = 100000000;		// max (abs) value accepted - otherwise box is left empty.
        public double m_MinTickSize = 1.0;		// minimum step between accepted display values.
        protected string m_TextFormat = "0;0; ";			// mkt qty-style is default.
        //public const int DefaultWidth = 48;					// width of this box.
        public const int DefaultWidth = 64;					// width of this box.

        // Mouse Event variables                            // Used for self-identifications 
        protected int m_RowID = -1;                         // identifies my the BoxRow I am part of.
        protected int m_ClusterEngineContainerID = -1;      // identifies the cluster I am part of. 

        
        #endregion //member variables


        #region Properties
        // *********************************************************************
        // ****							Properties							****
        // *********************************************************************
        //
        //
        // ****					TickValue					****
        /// <summary>
        /// This is the most important property of this control.  It is the 
        /// only way in which new values are accepted to be displayed by this control.
        /// This method updates the displayed value, and MUST be called by the window thread.
        /// </summary>
        public int TickValue
        {
            get { return m_Value; }
            set
            {
                // Check validity of value.
                if (Math.Abs(value) > ValueMaxRange)
                {
                    SetColorByValueSign(0, /*forceColorUpdate*/false);		// use default color for 0.
                    base.Text = " ";
                    return;							// exit.
                }

                // Implement value.
                SetColorByValueSign(value, false);	// set color associated with this value.
                m_Value = value;					// store the new value.
                base.Text = (m_Value * m_MinTickSize).ToString(m_TextFormat);
            }
        }//end TickValue
        //
        //
        public int ForeColorHiLite
        {
            get { return m_ForeColorHiLite; }
            set
            {	// Check validity of the value.
                if ((value >= 0) && (value < m_ForeColorPositive.Length))  // changed April 2011 - from value > 0
                {	// Value is valid. 
                    if (value != m_ForeColorHiLite)
                    {	// Caller has changed the color!
                        m_ForeColorHiLite = value;
                        SetColorByValueSign(m_Value, /*force color update*/ true);	// force color update!
                    }
                }
                else
                {	// Value passed is not valid.  Set to default color.
                    m_ForeColorHiLite = 0;		// default (not hi-lited) value.
                    SetColorByValueSign(m_Value, /*force color update*/ true);	// force color update!
                }
            }
        }
        // 
        #endregion//end Properties


        #region Constructors
        // *********************************************************************
        // ****							Constructors						****
        // *********************************************************************
        //
        //
        /// <summary>
        /// The main overload for constructing this box.
        /// </summary>
        /// <param name="minTickSize"></param>
        /// <param name="userDefinedBoxID">User-defined ID number that can be used to identify 
        ///		this box to its owner.  This is useful when it responds to user-click events, 
        ///		so the owner (a Row object) can uniquely identify this particular box. </param>
        ///	<param name="colorPreference"></param>
        public BoxNumeric(double minTickSize, int userDefinedBoxID, ColorPalette colorPreference)
        {
            m_MinTickSize = minTickSize;
            BoxID = userDefinedBoxID;				// set ID number


            // Set defaults for price chart.
            // TODO: Fix this!  Allow caller to say what its default behavior will be!
            // if (Math.Abs(Math.Round(m_MinTickSize - 1.0, 5)) > 0.0001)
            if (colorPreference == ColorPalette.TextNormal)
            {	// this is a price row box - its colors are not sign dependent.
                // Determine how many decimals to display.
                double err = 1;
                int decimals = 0;
                while (err > 0.0000001 && decimals < 10)
                {
                    decimals++;
                    err = Math.Abs(Math.Pow(10, decimals) * m_MinTickSize - Math.Floor(Math.Pow(10, decimals) * m_MinTickSize));
                }
                StringBuilder decFormat = new StringBuilder("0");
                if (decimals > 0)
                {
                    decFormat.Append(".");
                    for (int i = 1; i <= decimals; ++i) { decFormat.Append("0"); }
                }
                m_TextFormat = string.Format("{0};-{0};{0}",decFormat.ToString());   //TODO: Allow caller to set this!

                //UseColorPalette(ColorPalette.NormalText);
            }
            else
            {	// this is a integer qty row.
                //UseColorPalette(ColorPalette.BackSigned);
            }
			UseColorPalette(colorPreference);
            Initialize();
            this.TickValue = 0;


        }//end constructors
        //
        //
        //
        // ****					Constructor Overloading						****
        //
        public BoxNumeric() : base(){}
        //
        //}//end constructor.
        //
        //
        // ****					Initialize					****
        //
        private void Initialize()
        {
            this.SuspendLayout();
            // Set default properties.
            base.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            base.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            //base.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            base.Font = new System.Drawing.Font("Arial", 9.00F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            base.ForeColor = m_ForeColorDefault;
            base.BackColor = m_BackColorDefault;
            base.ClientSize = new System.Drawing.Size(DefaultWidth, 16);	// 64 x 16 seems large, but good.
            base.Size = new System.Drawing.Size(base.ClientSize.Width, base.ClientSize.Height);
            this.ResumeLayout(false);
        }//end Initialize
        //
        //
        #endregion// constructors


        #region Public Methods
        // *********************************************************************
        // ****						Public Methods							****
        // *********************************************************************
        //
        //

        //
        // ****             Use Color Palette()         ****
        //
        public void UseColorPalette(ColorPalette colorPalette)
        {
            switch (colorPalette)
            {
                case ColorPalette.TextNormal:
                    // This palette does not change colors based on the grid value.
                    // Used for things like the price row.
                    m_BackColorPositive = Color.LightSlateGray;
                    m_BackColorNegative = Color.LightSlateGray;
                    // Fore Colors
                    m_ForeColorPositive[0] = Color.Black;
                    m_ForeColorNegative[0] = Color.Black;
                    m_ForeColorZero[0] = Color.Black;
                    m_ForeColorPositive[1] = Color.MediumBlue;
                    m_ForeColorNegative[1] = Color.MediumBlue;
                    m_ForeColorZero[1] = Color.MediumBlue;
                    m_ForeColorPositive[2] = Color.Maroon;
                    m_ForeColorNegative[2] = Color.Maroon;
                    m_ForeColorZero[2] = Color.Maroon;
                    break;
				case ColorPalette.TextSigned:
					m_BackColorPositive = Color.LightSlateGray;
					m_BackColorNegative = Color.LightSlateGray;
					// Fore Colors
					m_ForeColorPositive[0] = Color.MediumBlue;
					m_ForeColorNegative[0] = Color.Maroon;
					m_ForeColorZero[0] = Color.Black;
					m_ForeColorPositive[1] = Color.MediumBlue;
					m_ForeColorNegative[1] = Color.Maroon;
					m_ForeColorZero[1] = Color.Black;
					m_ForeColorPositive[2] = Color.MediumBlue;
					m_ForeColorNegative[2] = Color.Maroon;
					m_ForeColorZero[2] = Color.Black;
					break;
                case ColorPalette.BackSigned:
                    m_BackColorPositive = Color.Navy;
                    m_BackColorNegative = Color.Maroon;
                    // Fore Colors
                    m_ForeColorPositive[0] = Color.White;
                    m_ForeColorNegative[0] = Color.White;
                    m_ForeColorZero[0] = m_ForeColorDefault;
                    m_ForeColorPositive[1] = Color.Yellow;
                    m_ForeColorNegative[1] = Color.Yellow;
                    m_ForeColorZero[1] = m_ForeColorDefault;
                    m_ForeColorPositive[2] = Color.Yellow;
                    m_ForeColorNegative[2] = Color.Yellow;
                    m_ForeColorZero[2] = m_ForeColorDefault;
                    break;
                case ColorPalette.BackSignedLite:
                    m_BackColorPositive = Color.DodgerBlue;
                    m_BackColorNegative = Color.Crimson;
                    // ForeColors
                    m_ForeColorPositive[0] = Color.Black;
                    m_ForeColorNegative[0] = Color.Black;
                    m_ForeColorZero[0] = m_ForeColorDefault;
                    m_ForeColorPositive[1] = Color.Black;
                    m_ForeColorNegative[1] = Color.Black;
                    m_ForeColorZero[1] = m_ForeColorDefault;
                    m_ForeColorPositive[2] = Color.Black;
                    m_ForeColorNegative[2] = Color.Black;
                    m_ForeColorZero[2] = m_ForeColorDefault;
                    break;
                case ColorPalette.BackSignedLiteForeHiLite:
                    m_BackColorPositive = Color.DodgerBlue;
                    m_BackColorNegative = Color.Crimson;
                    m_ForeColorPositive[0] = Color.White;
                    m_ForeColorNegative[0] = Color.White;
                    m_ForeColorZero[0] = m_ForeColorDefault;
                    m_ForeColorPositive[1] = Color.White;
                    m_ForeColorNegative[1] = Color.White;
                    m_ForeColorZero[1] = m_ForeColorDefault;
                    m_ForeColorPositive[2] = Color.White;
                    m_ForeColorNegative[2] = Color.White;
                    m_ForeColorZero[2] = m_ForeColorDefault;
                    break;
                case ColorPalette.ForeSigned:
                    m_BackColorPositive = Color.LightGray;
                    m_BackColorNegative = Color.LightGray;
                    m_ForeColorPositive[0] = Color.Navy;
                    m_ForeColorNegative[0] = Color.Maroon;
                    m_ForeColorZero[0] = m_ForeColorDefault;
                    m_ForeColorPositive[1] = Color.Navy;
                    m_ForeColorNegative[1] = Color.Maroon;
                    m_ForeColorZero[1] = m_ForeColorDefault;
                    m_ForeColorPositive[2] = Color.Navy;
                    m_ForeColorNegative[2] = Color.Maroon;
                    m_ForeColorZero[2] = m_ForeColorDefault;
                    break;
                case ColorPalette.ForeSignedBackHiLite:
                    m_BackColorPositive = Color.Yellow;
                    m_BackColorNegative = Color.Yellow;
                    m_ForeColorPositive[0] = Color.Navy;
                    m_ForeColorNegative[0] = Color.Maroon;
                    m_ForeColorZero[0] = m_ForeColorDefault;
                    m_ForeColorPositive[1] = Color.Navy;
                    m_ForeColorNegative[1] = Color.Maroon;
                    m_ForeColorZero[1] = m_ForeColorDefault;
                    m_ForeColorPositive[2] = Color.Navy;
                    m_ForeColorNegative[2] = Color.Maroon;
                    m_ForeColorZero[2] = m_ForeColorDefault;
                    break;
                default:
                    m_BackColorPositive = Color.LightSlateGray;
                    m_BackColorNegative = Color.LightSlateGray;
                    m_ForeColorPositive[0] = Color.Black;
                    m_ForeColorNegative[0] = Color.Black;
                    m_ForeColorZero[0] = m_ForeColorDefault;
                    m_ForeColorPositive[1] = Color.Black;
                    m_ForeColorNegative[1] = Color.Black;
                    m_ForeColorZero[1] = m_ForeColorDefault;
                    m_ForeColorPositive[2] = Color.Black;
                    m_ForeColorNegative[2] = Color.Black;
                    m_ForeColorZero[2] = m_ForeColorDefault;
                    break;
            }//color palette switch
        }// UseColorPalette()
        //
        //
        //
        //
        #endregion//end public methods

        #region Click Event Handlers
        // 
        //
        private event EventHandler BoxClick;
        //
        //
        // ****             Register Mouse Events()         ****
        //
        /// <summary>
        /// Register the provided Cluster to receive specific Mouse Events from this box.
        /// </summary>
        /// <param name="aCluster"></param>
        /// <param name="rowID"></param>
        public void RegisterMouseEvents(Cluster aCluster, int rowID)
        {
            // Subscribe my cluster to my events.
            this.MouseEnter += new EventHandler(aCluster.BoxNumeric_MouseEnter);
            this.MouseLeave += new EventHandler(aCluster.BoxNumeric_MouseLeave);
            this.BoxClick += new EventHandler(aCluster.BoxNumeric_Click);
            
            
            // Set up my event handlers
            this.Click += new EventHandler(BoxNumeric_Click);
            m_RowID = rowID;            // store the rowID I am in for event triggers.
            m_ClusterEngineContainerID = aCluster.EngineContainerID;
        }// RegisterMouseEvents()
        //
        //
        //
        //
        // ****             BoxNumeric Click            ****
        //
        private void BoxNumeric_Click(object sender, EventArgs e)
        {
            ClusterEventArgs eventArgs = new ClusterEventArgs();
            eventArgs.OriginalEventArgs = e;
            eventArgs.ModifierKey = Control.ModifierKeys;  // record whatever *modifying keys* are depressed.
            
            

            // My identification.
            eventArgs.BoxID = this.BoxID;                   // my ID. 
            eventArgs.RowID = m_RowID;                      // my owning Rows ID.
            eventArgs.ClusterEngineContainerID = m_ClusterEngineContainerID;    // my owner's cluster ID.
            // Useful information about what I show now.
            eventArgs.BoxValue = m_Value;

            // Trigger the event to the Cluster.
            if (BoxClick != null) { BoxClick(sender, eventArgs); }

        }// BoxNumeric_Click().
        //
        //
        //       
        //
        //
        //
        //
        #endregion// Click event



        #region Private utilities
        // *********************************************************************
        // ****						Private utilities						****
        // *********************************************************************
        //
        //
        //
        //
        // ****					SetColor ByValueSign					****
        //
        //
        private void SetColorByValueSign(int newValue, bool forceColorUpdate)
        {
            // We update the color if
            //	1. caller demands that we update.
            //	2. the value has had a sign change.
            if (forceColorUpdate || (newValue * m_Value <= 0))
            {
                if (newValue < 0)
                {
                    base.BackColor = m_BackColorNegative;
                    base.ForeColor = m_ForeColorNegative[m_ForeColorHiLite];
                }
                else if (newValue > 0)
                {
                    base.BackColor = m_BackColorPositive;
                    base.ForeColor = m_ForeColorPositive[m_ForeColorHiLite];
                }
                else
                {
                    base.BackColor = m_BackColorDefault;
                    base.ForeColor = m_ForeColorZero[m_ForeColorHiLite];
                }
            }
        }
        //
        //
        //
        #endregion//end private utilities.


    }//end TextBoxBasic

    #region ColorPalette Enum
    //
    //
    // ****			Color Palletes 			****
    //
    /// <summary>
    /// </summary>
    public enum ColorPalette
    {
        TextNormal,
		TextSigned,
        BackSigned,
        BackSignedLite,
        BackSignedLiteForeHiLite,
        ForeSigned,
        ForeSignedBackHiLite
    };
    //
    //
    //
    #endregion// color palette



}
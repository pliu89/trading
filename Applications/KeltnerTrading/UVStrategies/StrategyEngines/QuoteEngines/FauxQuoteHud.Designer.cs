namespace UV.Strategies.StrategyEngines.QuoteEngines
{
    partial class TradeEngineHud
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pIsQuoteEnabled = new UV.Lib.FrontEnds.PopUps.ParamBool2();
            this.SuspendLayout();
            // 
            // pIsQuoteEnabled
            // 
            this.pIsQuoteEnabled.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pIsQuoteEnabled.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pIsQuoteEnabled.HideLabel = true;
            this.pIsQuoteEnabled.Location = new System.Drawing.Point(2, 2);
            this.pIsQuoteEnabled.Margin = new System.Windows.Forms.Padding(0);
            this.pIsQuoteEnabled.Name = "IsQuoteEnabled";
            this.pIsQuoteEnabled.Size = new System.Drawing.Size(47, 17);
            this.pIsQuoteEnabled.TabIndex = 0;
            // 
            // FauxQuoteHud
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pIsQuoteEnabled);
            this.Name = "FauxQuoteHud";
            this.Size = new System.Drawing.Size(193, 44);
            this.ResumeLayout(false);

        }

        #endregion

        private Lib.FrontEnds.PopUps.ParamBool2 pIsQuoteEnabled;

    }
}

namespace Ambre.TTServices.Fills.FrontEnds
{
    partial class FillHubGrid
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
            this.components = new System.ComponentModel.Container();
            this.activeGrid1 = new SKACERO.ActiveGrid(this.components);
            this.SuspendLayout();
            // 
            // activeGrid1
            // 
            this.activeGrid1.AllowFlashing = false;
            this.activeGrid1.AlternatingBackColor = System.Drawing.Color.Gainsboro;
            this.activeGrid1.AlternatingGradientEndColor = System.Drawing.Color.White;
            this.activeGrid1.AlternatingGradientStartColor = System.Drawing.Color.White;
            this.activeGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.activeGrid1.FlashBackColor = System.Drawing.Color.Yellow;
            this.activeGrid1.FlashDuration = 2000;
            this.activeGrid1.FlashFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.activeGrid1.FlashForeColor = System.Drawing.Color.Black;
            this.activeGrid1.FlashGradientEndColor = System.Drawing.Color.White;
            this.activeGrid1.FlashGradientStartColor = System.Drawing.Color.White;
            this.activeGrid1.ForeColorNegativeValues = System.Drawing.Color.Red;
            this.activeGrid1.GroupIndex = 0;
            this.activeGrid1.Location = new System.Drawing.Point(3, 0);
            this.activeGrid1.Name = "activeGrid1";
            this.activeGrid1.OwnerDraw = true;
            this.activeGrid1.Size = new System.Drawing.Size(144, 147);
            this.activeGrid1.TabIndex = 0;
            this.activeGrid1.UseCompatibleStateImageBehavior = false;
            // 
            // FillHubGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.activeGrid1);
            this.Name = "FillHubGrid";
            this.ResumeLayout(false);

        }

        #endregion

        private SKACERO.ActiveGrid activeGrid1;
    }
}

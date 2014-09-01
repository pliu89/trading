namespace Ambre.PositionViewer
{
    partial class FormFillBookViewer
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.activeGrid1 = new SKACERO.ActiveGrid(this.components);
            this.button1 = new System.Windows.Forms.Button();
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
            this.activeGrid1.Location = new System.Drawing.Point(1, 44);
            this.activeGrid1.Name = "activeGrid1";
            this.activeGrid1.OwnerDraw = true;
            this.activeGrid1.Size = new System.Drawing.Size(517, 289);
            this.activeGrid1.TabIndex = 0;
            this.activeGrid1.UseCompatibleStateImageBehavior = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(387, 9);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(70, 29);
            this.button1.TabIndex = 1;
            this.button1.Text = "update";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FormFillBookViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(521, 345);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.activeGrid1);
            this.Name = "FormFillBookViewer";
            this.Text = "Fills";
            this.ResumeLayout(false);

        }

        #endregion

        private SKACERO.ActiveGrid activeGrid1;
        private System.Windows.Forms.Button button1;


    }
}
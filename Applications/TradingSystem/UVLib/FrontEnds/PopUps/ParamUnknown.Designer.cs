namespace UV.Lib.FrontEnds.PopUps
{
    partial class ParamUnknown
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
            this.ParameterName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // ParameterName
            // 
            this.ParameterName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ParameterName.Font = new System.Drawing.Font("Blue Highway", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ParameterName.Location = new System.Drawing.Point(3, 2);
            this.ParameterName.Name = "ParameterName";
            this.ParameterName.Size = new System.Drawing.Size(84, 18);
            this.ParameterName.TabIndex = 0;
            this.ParameterName.Text = "txtParameterName";
            this.ParameterName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ParamUnknown
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ParameterName);
            this.Name = "ParamUnknown";
            this.Size = new System.Drawing.Size(90, 20);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label ParameterName;
    }
}

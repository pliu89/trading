namespace UV.Lib.FrontEnds.PopUps
{
    partial class ParamDouble2
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
            this.tbParameterValue = new System.Windows.Forms.TextBox();
            this.ParameterName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tbParameterValue
            // 
            this.tbParameterValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbParameterValue.Location = new System.Drawing.Point(0, 0);
            this.tbParameterValue.Name = "tbParameterValue";
            this.tbParameterValue.Size = new System.Drawing.Size(88, 20);
            this.tbParameterValue.TabIndex = 0;
            this.tbParameterValue.KeyUp += new System.Windows.Forms.KeyEventHandler(this.textBox_KeyUp);
            // 
            // ParameterName
            // 
            this.ParameterName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ParameterName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ParameterName.Location = new System.Drawing.Point(1, 19);
            this.ParameterName.Name = "ParameterName";
            this.ParameterName.Size = new System.Drawing.Size(85, 12);
            this.ParameterName.TabIndex = 1;
            this.ParameterName.Text = "the long label";
            // 
            // ParamDouble2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ParameterName);
            this.Controls.Add(this.tbParameterValue);
            this.Name = "ParamDouble2";
            this.Size = new System.Drawing.Size(90, 35);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbParameterValue;
        private System.Windows.Forms.Label ParameterName;
    }
}

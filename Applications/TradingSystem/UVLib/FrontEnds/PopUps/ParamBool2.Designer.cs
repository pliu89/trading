namespace UV.Lib.FrontEnds.PopUps
{
    partial class ParamBool2
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
            this.bParameter = new System.Windows.Forms.Label();
            this.parameterName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // bParameter
            // 
            this.bParameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.bParameter.AutoSize = true;
            this.bParameter.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.bParameter.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.bParameter.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bParameter.ForeColor = System.Drawing.SystemColors.ControlLight;
            this.bParameter.Location = new System.Drawing.Point(0, 0);
            this.bParameter.Name = "bParameter";
            this.bParameter.Padding = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.bParameter.Size = new System.Drawing.Size(38, 15);
            this.bParameter.TabIndex = 0;
            this.bParameter.Text = "Text";
            this.bParameter.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.bParameter.Click += new System.EventHandler(this.bParameter_Click);
            this.bParameter.Resize += new System.EventHandler(this.Button_Resized);
            // 
            // parameterName
            // 
            this.parameterName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.parameterName.AutoSize = true;
            this.parameterName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.parameterName.Location = new System.Drawing.Point(0, 16);
            this.parameterName.Name = "parameterName";
            this.parameterName.Size = new System.Drawing.Size(35, 13);
            this.parameterName.TabIndex = 1;
            this.parameterName.Text = "label1";
            // 
            // ParamBool2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.parameterName);
            this.Controls.Add(this.bParameter);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "ParamBool2";
            this.Size = new System.Drawing.Size(49, 32);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label bParameter;
        private System.Windows.Forms.Label parameterName;
    }
}

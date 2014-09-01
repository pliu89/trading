namespace Ambre.PositionViewer.FrontEnds
{
    partial class FormUpdateAmbreName
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
        private void InitializeComponent(string aLine)
        {
            this.buttonCreateNewHub = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxAmbreName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // buttonCreateNewHub
            // 
            this.buttonCreateNewHub.Location = new System.Drawing.Point(201, 71);
            this.buttonCreateNewHub.Name = "buttonCreateNewHub";
            this.buttonCreateNewHub.Size = new System.Drawing.Size(96, 23);
            this.buttonCreateNewHub.TabIndex = 11;
            this.buttonCreateNewHub.Text = "Add AmbreName";
            this.buttonCreateNewHub.UseVisualStyleBackColor = true;
            this.buttonCreateNewHub.Click += new System.EventHandler(this.Button_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(105, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = string.Format("{0} {1}", aLine, "do not contain a AmbreUserName, please add it.");
            // 
            // textBoxAmbreName
            // 
            this.textBoxAmbreName.Location = new System.Drawing.Point(12, 71);
            this.textBoxAmbreName.Name = "textBoxAmbreName";
            this.textBoxAmbreName.Size = new System.Drawing.Size(126, 20);
            this.textBoxAmbreName.TabIndex = 9;
            // 
            // FormUpdateAmbreName
            // 
            this.ClientSize = new System.Drawing.Size(714, 103);
            this.Controls.Add(this.buttonCreateNewHub);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxAmbreName);
            this.Name = "FormUpdateAmbreName";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxFilterString;
        private System.Windows.Forms.Button buttonCreateNewHub;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxAmbreName;

    }
}
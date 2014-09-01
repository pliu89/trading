namespace UV.Lib.FrontEnds.Clusters
{
    partial class Header
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
            this.txtName = new System.Windows.Forms.Label();
            this.comboBoxEngines = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // txtName
            // 
            this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtName.BackColor = System.Drawing.Color.Transparent;
            this.txtName.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtName.ForeColor = System.Drawing.Color.LightGray;
            this.txtName.Location = new System.Drawing.Point(0, 1);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(198, 19);
            this.txtName.TabIndex = 3;
            this.txtName.Text = "TT01+6NGU2-26JU2+1UBU2";
            this.txtName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtName.DoubleClick += new System.EventHandler(this.txtName_DoubleClick);
            // 
            // comboBoxEngines
            // 
            this.comboBoxEngines.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxEngines.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(67)))), ((int)(((byte)(92)))));
            this.comboBoxEngines.Cursor = System.Windows.Forms.Cursors.Hand;
            this.comboBoxEngines.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxEngines.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.comboBoxEngines.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxEngines.ForeColor = System.Drawing.SystemColors.Info;
            this.comboBoxEngines.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.comboBoxEngines.ItemHeight = 13;
            this.comboBoxEngines.Location = new System.Drawing.Point(192, 0);
            this.comboBoxEngines.Margin = new System.Windows.Forms.Padding(0);
            this.comboBoxEngines.Name = "comboBoxEngines";
            this.comboBoxEngines.Size = new System.Drawing.Size(157, 21);
            this.comboBoxEngines.TabIndex = 4;
            this.comboBoxEngines.TabStop = false;
            this.comboBoxEngines.SelectionChangeCommitted += new System.EventHandler(this.comboBoxEngines_SelectionChangeCommitted);
            // 
            // Header
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(67)))), ((int)(((byte)(92)))));
            this.Controls.Add(this.comboBoxEngines);
            this.Controls.Add(this.txtName);
            this.MinimumSize = new System.Drawing.Size(340, 0);
            this.Name = "Header";
            this.Size = new System.Drawing.Size(349, 23);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label txtName;
        private System.Windows.Forms.ComboBox comboBoxEngines;
    }
}

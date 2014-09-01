namespace UV.Lib.FrontEnds.PopUps
{
    partial class PopUp1
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.bTitle = new System.Windows.Forms.Label();
            this.bExit = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.Controls.Add(this.bTitle);
            this.panel1.Controls.Add(this.bExit);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(285, 14);
            this.panel1.TabIndex = 3;
            this.panel1.Click += new System.EventHandler(this.bExit_Click);
            // 
            // bTitle
            // 
            this.bTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.bTitle.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bTitle.Location = new System.Drawing.Point(0, 0);
            this.bTitle.Margin = new System.Windows.Forms.Padding(0);
            this.bTitle.Name = "bTitle";
            this.bTitle.Size = new System.Drawing.Size(261, 14);
            this.bTitle.TabIndex = 1;
            this.bTitle.Text = "Title";
            this.bTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.bTitle.Click += new System.EventHandler(this.bExit_Click);
            // 
            // bExit
            // 
            this.bExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bExit.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.bExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bExit.Location = new System.Drawing.Point(273, 0);
            this.bExit.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.bExit.Name = "bExit";
            this.bExit.Size = new System.Drawing.Size(12, 14);
            this.bExit.TabIndex = 0;
            this.bExit.Text = "X";
            this.bExit.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.bExit.Click += new System.EventHandler(this.bExit_Click);
            // 
            // PopUp1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 16);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "PopUp1";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Text";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PopUp1_FormClosing);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PopUp_MouseClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PopUp1_MouseDown);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label bTitle;
        private System.Windows.Forms.Label bExit;

    }
}
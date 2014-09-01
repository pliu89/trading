namespace Ambre.PositionViewer.Dialogs
{
    partial class FormPnLTransferTool
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
            this.groupBoxSideBar = new System.Windows.Forms.GroupBox();
            this.buttonGrowShrink = new System.Windows.Forms.Button();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.groupBoxSideBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxSideBar
            // 
            this.groupBoxSideBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxSideBar.BackColor = System.Drawing.SystemColors.Control;
            this.groupBoxSideBar.Controls.Add(this.buttonRefresh);
            this.groupBoxSideBar.Controls.Add(this.buttonGrowShrink);
            this.groupBoxSideBar.Location = new System.Drawing.Point(282, -5);
            this.groupBoxSideBar.Name = "groupBoxSideBar";
            this.groupBoxSideBar.Size = new System.Drawing.Size(58, 119);
            this.groupBoxSideBar.TabIndex = 0;
            this.groupBoxSideBar.TabStop = false;
            // 
            // buttonGrowShrink
            // 
            this.buttonGrowShrink.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.buttonGrowShrink.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonGrowShrink.Location = new System.Drawing.Point(10, 10);
            this.buttonGrowShrink.Name = "buttonGrowShrink";
            this.buttonGrowShrink.Size = new System.Drawing.Size(38, 23);
            this.buttonGrowShrink.TabIndex = 0;
            this.buttonGrowShrink.Text = ">>";
            this.buttonGrowShrink.UseVisualStyleBackColor = true;
            this.buttonGrowShrink.Click += new System.EventHandler(this.Button_Click);
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.Location = new System.Drawing.Point(4, 39);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(49, 23);
            this.buttonRefresh.TabIndex = 14;
            this.buttonRefresh.Text = "refresh";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            this.buttonRefresh.Click += new System.EventHandler(this.Button_Click);
            // 
            // FormPnLTransferTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ClientSize = new System.Drawing.Size(341, 115);
            this.Controls.Add(this.groupBoxSideBar);
            this.Name = "FormPnLTransferTool";
            this.Text = "PnL Transfer Manager";
            this.groupBoxSideBar.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxSideBar;
        private System.Windows.Forms.Button buttonGrowShrink;
        private System.Windows.Forms.Button buttonRefresh;
    }
}
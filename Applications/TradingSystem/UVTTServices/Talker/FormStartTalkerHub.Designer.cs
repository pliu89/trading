namespace UV.TTServices.Talker
{
    partial class FormStartTalkerHub
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
            this.label1 = new System.Windows.Forms.Label();
            this.txtAPIConnected = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtBreTalkerConnectionStatus = new System.Windows.Forms.Label();
            this.buttonConnectToExcel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(180, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "TT API Status: ";
            // 
            // txtAPIConnected
            // 
            this.txtAPIConnected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAPIConnected.Location = new System.Drawing.Point(256, 9);
            this.txtAPIConnected.Name = "txtAPIConnected";
            this.txtAPIConnected.Size = new System.Drawing.Size(76, 13);
            this.txtAPIConnected.TabIndex = 1;
            this.txtAPIConnected.Text = "not connected";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(165, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(95, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Bre.Talker Status: ";
            // 
            // txtBreTalkerConnectionStatus
            // 
            this.txtBreTalkerConnectionStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBreTalkerConnectionStatus.Location = new System.Drawing.Point(256, 22);
            this.txtBreTalkerConnectionStatus.Name = "txtBreTalkerConnectionStatus";
            this.txtBreTalkerConnectionStatus.Size = new System.Drawing.Size(76, 13);
            this.txtBreTalkerConnectionStatus.TabIndex = 3;
            this.txtBreTalkerConnectionStatus.Text = "not connected";
            // 
            // buttonConnectToExcel
            // 
            this.buttonConnectToExcel.Location = new System.Drawing.Point(12, 9);
            this.buttonConnectToExcel.Name = "buttonConnectToExcel";
            this.buttonConnectToExcel.Size = new System.Drawing.Size(57, 23);
            this.buttonConnectToExcel.TabIndex = 4;
            this.buttonConnectToExcel.Text = "connect";
            this.buttonConnectToExcel.UseVisualStyleBackColor = true;
            this.buttonConnectToExcel.Click += new System.EventHandler(this.buttonConnectToExcel_Click);
            // 
            // FormStartTalkerHub
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(336, 101);
            this.Controls.Add(this.buttonConnectToExcel);
            this.Controls.Add(this.txtBreTalkerConnectionStatus);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtAPIConnected);
            this.Controls.Add(this.label1);
            this.Name = "FormStartTalkerHub";
            this.Text = "BreTalker";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormStartTalkerHub_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label txtAPIConnected;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label txtBreTalkerConnectionStatus;
        private System.Windows.Forms.Button buttonConnectToExcel;
    }
}
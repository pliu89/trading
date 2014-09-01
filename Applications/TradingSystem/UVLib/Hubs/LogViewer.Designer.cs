namespace UV.Lib.Hubs
{
    partial class LogViewer
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
            try
            {
                base.Dispose(disposing);
            }
            catch (System.Exception)
            {
            }
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.comboLogLevel = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textLastUpdate = new System.Windows.Forms.Label();
            this.checkBoxAutoScroll = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(3, 27);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(493, 227);
            this.textBox1.TabIndex = 0;
            this.textBox1.TabStop = false;
            this.textBox1.WordWrap = false;
            this.textBox1.MouseLeave += new System.EventHandler(this.textBox1_MouseLeave);
            this.textBox1.MouseEnter += new System.EventHandler(this.textBox1_MouseHover);
            // 
            // comboLogLevel
            // 
            this.comboLogLevel.Cursor = System.Windows.Forms.Cursors.Default;
            this.comboLogLevel.FormattingEnabled = true;
            this.comboLogLevel.Location = new System.Drawing.Point(3, 0);
            this.comboLogLevel.Name = "comboLogLevel";
            this.comboLogLevel.Size = new System.Drawing.Size(146, 21);
            this.comboLogLevel.TabIndex = 3;
            this.comboLogLevel.TabStop = false;
            this.comboLogLevel.SelectedIndexChanged += new System.EventHandler(this.comboLogLevel_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(334, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "last update:";
            // 
            // textLastUpdate
            // 
            this.textLastUpdate.Location = new System.Drawing.Point(392, 0);
            this.textLastUpdate.Name = "textLastUpdate";
            this.textLastUpdate.Size = new System.Drawing.Size(104, 18);
            this.textLastUpdate.TabIndex = 4;
            this.textLastUpdate.Text = "12:53:25.230 PM";
            this.textLastUpdate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // checkBoxAutoScroll
            // 
            this.checkBoxAutoScroll.AutoSize = true;
            this.checkBoxAutoScroll.Checked = true;
            this.checkBoxAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAutoScroll.Location = new System.Drawing.Point(155, 2);
            this.checkBoxAutoScroll.Name = "checkBoxAutoScroll";
            this.checkBoxAutoScroll.Size = new System.Drawing.Size(71, 17);
            this.checkBoxAutoScroll.TabIndex = 5;
            this.checkBoxAutoScroll.Text = "autoscroll";
            this.checkBoxAutoScroll.UseVisualStyleBackColor = true;
            this.checkBoxAutoScroll.CheckedChanged += new System.EventHandler(this.checkBoxAutoScroll_CheckedChanged);
            // 
            // LogViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(503, 258);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.checkBoxAutoScroll);
            this.Controls.Add(this.textLastUpdate);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboLogLevel);
            this.Name = "LogViewer";
            this.Text = "Log View";
            this.SizeChanged += new System.EventHandler(this.LogViewer_SizeChanged);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LogViewer_FormClosing);
            this.Disposed += new System.EventHandler(this.LogViewer_Dispose);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ComboBox comboLogLevel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label textLastUpdate;
        private System.Windows.Forms.CheckBox checkBoxAutoScroll;
    }
}
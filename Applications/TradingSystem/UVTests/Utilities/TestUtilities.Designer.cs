namespace UV.Tests.Utilities
{
    partial class TestUtilities
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
            this.buttonStartAlarm = new System.Windows.Forms.Button();
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonHMA = new System.Windows.Forms.Button();
            this.buttonSpawnMessageBox = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonStartAlarm
            // 
            this.buttonStartAlarm.Location = new System.Drawing.Point(4, 12);
            this.buttonStartAlarm.Name = "buttonStartAlarm";
            this.buttonStartAlarm.Size = new System.Drawing.Size(75, 23);
            this.buttonStartAlarm.TabIndex = 0;
            this.buttonStartAlarm.Text = "start";
            this.buttonStartAlarm.UseVisualStyleBackColor = true;
            this.buttonStartAlarm.Click += new System.EventHandler(this.AlarmStart_Click);
            // 
            // textBoxOutput
            // 
            this.textBoxOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOutput.Location = new System.Drawing.Point(1, 108);
            this.textBoxOutput.Multiline = true;
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxOutput.Size = new System.Drawing.Size(436, 152);
            this.textBoxOutput.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonStartAlarm);
            this.groupBox1.Location = new System.Drawing.Point(12, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(83, 41);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Alarm";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.buttonHMA);
            this.groupBox2.Location = new System.Drawing.Point(12, 49);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(83, 41);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "HMA";
            // 
            // buttonHMA
            // 
            this.buttonHMA.Location = new System.Drawing.Point(4, 12);
            this.buttonHMA.Name = "buttonHMA";
            this.buttonHMA.Size = new System.Drawing.Size(75, 23);
            this.buttonHMA.TabIndex = 0;
            this.buttonHMA.Text = "compute";
            this.buttonHMA.UseVisualStyleBackColor = true;
            this.buttonHMA.Click += new System.EventHandler(this.buttonHMA_Click);
            // 
            // buttonSpawnMessageBox
            // 
            this.buttonSpawnMessageBox.Location = new System.Drawing.Point(311, 2);
            this.buttonSpawnMessageBox.Name = "buttonSpawnMessageBox";
            this.buttonSpawnMessageBox.Size = new System.Drawing.Size(115, 23);
            this.buttonSpawnMessageBox.TabIndex = 4;
            this.buttonSpawnMessageBox.Text = "spawn msg box";
            this.buttonSpawnMessageBox.UseVisualStyleBackColor = true;
            this.buttonSpawnMessageBox.Click += new System.EventHandler(this.buttonSpawnMessageBox_Click);
            // 
            // TestUtilities
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(438, 262);
            this.Controls.Add(this.buttonSpawnMessageBox);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBoxOutput);
            this.Name = "TestUtilities";
            this.Text = "Use to test small utilities";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TestUtilities_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStartAlarm;
        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonHMA;
        private System.Windows.Forms.Button buttonSpawnMessageBox;
    }
}
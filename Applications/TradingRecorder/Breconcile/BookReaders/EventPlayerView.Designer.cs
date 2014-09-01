namespace Ambre.Breconcile.BookReaders
{
    partial class EventPlayerView
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxBasePath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonLoadEventPlayer = new System.Windows.Forms.Button();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.buttonCollectUserNames = new System.Windows.Forms.Button();
            this.comboBoxUserNames = new System.Windows.Forms.ComboBox();
            this.dateTimePickerDate = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxTime = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxEndTime = new System.Windows.Forms.TextBox();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Base Directory Path:";
            // 
            // textBoxBasePath
            // 
            this.textBoxBasePath.Location = new System.Drawing.Point(113, 9);
            this.textBoxBasePath.Name = "textBoxBasePath";
            this.textBoxBasePath.Size = new System.Drawing.Size(229, 20);
            this.textBoxBasePath.TabIndex = 1;
            this.textBoxBasePath.Text = "\\\\fileserver\\Users\\dv_bre\\Ambre\\Drops\\";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "User/Acct Name:";
            // 
            // buttonLoadEventPlayer
            // 
            this.buttonLoadEventPlayer.Location = new System.Drawing.Point(346, 33);
            this.buttonLoadEventPlayer.Name = "buttonLoadEventPlayer";
            this.buttonLoadEventPlayer.Size = new System.Drawing.Size(58, 20);
            this.buttonLoadEventPlayer.TabIndex = 4;
            this.buttonLoadEventPlayer.Text = "Load";
            this.buttonLoadEventPlayer.UseVisualStyleBackColor = true;
            this.buttonLoadEventPlayer.Click += new System.EventHandler(this.Button_Click);
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabPage1);
            this.tabControl.Location = new System.Drawing.Point(3, 102);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(527, 468);
            this.tabControl.TabIndex = 5;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(519, 442);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // buttonCollectUserNames
            // 
            this.buttonCollectUserNames.Location = new System.Drawing.Point(346, 9);
            this.buttonCollectUserNames.Name = "buttonCollectUserNames";
            this.buttonCollectUserNames.Size = new System.Drawing.Size(58, 20);
            this.buttonCollectUserNames.TabIndex = 6;
            this.buttonCollectUserNames.Text = "Search";
            this.buttonCollectUserNames.UseVisualStyleBackColor = true;
            this.buttonCollectUserNames.Click += new System.EventHandler(this.Button_Click);
            // 
            // comboBoxUserNames
            // 
            this.comboBoxUserNames.FormattingEnabled = true;
            this.comboBoxUserNames.Location = new System.Drawing.Point(113, 32);
            this.comboBoxUserNames.Name = "comboBoxUserNames";
            this.comboBoxUserNames.Size = new System.Drawing.Size(229, 21);
            this.comboBoxUserNames.TabIndex = 7;
            this.comboBoxUserNames.SelectedIndexChanged += new System.EventHandler(this.ComboBox_SelectedIndexChanged);
            // 
            // dateTimePickerDate
            // 
            this.dateTimePickerDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePickerDate.Location = new System.Drawing.Point(113, 56);
            this.dateTimePickerDate.Name = "dateTimePickerDate";
            this.dateTimePickerDate.Size = new System.Drawing.Size(96, 20);
            this.dateTimePickerDate.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(21, 59);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(86, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Start Date/Time:";
            // 
            // textBoxTime
            // 
            this.textBoxTime.Location = new System.Drawing.Point(215, 56);
            this.textBoxTime.Name = "textBoxTime";
            this.textBoxTime.Size = new System.Drawing.Size(77, 20);
            this.textBoxTime.TabIndex = 10;
            this.textBoxTime.Text = "16:00:00";
            this.textBoxTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(24, 76);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(83, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "End Date/Time:";
            // 
            // textBoxEndTime
            // 
            this.textBoxEndTime.Location = new System.Drawing.Point(215, 76);
            this.textBoxEndTime.Name = "textBoxEndTime";
            this.textBoxEndTime.Size = new System.Drawing.Size(77, 20);
            this.textBoxEndTime.TabIndex = 13;
            this.textBoxEndTime.Text = "16:00:00";
            this.textBoxEndTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker1.Location = new System.Drawing.Point(113, 76);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(96, 20);
            this.dateTimePicker1.TabIndex = 12;
            // 
            // EventPlayerView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textBoxEndTime);
            this.Controls.Add(this.dateTimePicker1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxTime);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dateTimePickerDate);
            this.Controls.Add(this.comboBoxUserNames);
            this.Controls.Add(this.buttonCollectUserNames);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.buttonLoadEventPlayer);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxBasePath);
            this.Controls.Add(this.label1);
            this.Name = "EventPlayerView";
            this.Size = new System.Drawing.Size(533, 573);
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxBasePath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonLoadEventPlayer;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button buttonCollectUserNames;
        private System.Windows.Forms.ComboBox comboBoxUserNames;
        private System.Windows.Forms.DateTimePicker dateTimePickerDate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxTime;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxEndTime;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
    }
}

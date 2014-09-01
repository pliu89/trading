namespace Ambre.Breconcile.BookReaders
{
    partial class EventSeriesView
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
            this.listBoxFillEvents = new System.Windows.Forms.ListBox();
            this.comboBoxInstrumentNames = new System.Windows.Forms.ComboBox();
            this.textStartDate = new System.Windows.Forms.Label();
            this.textEndDate = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textStartTime = new System.Windows.Forms.Label();
            this.textEndTime = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonLoadEarlierFile = new System.Windows.Forms.Button();
            this.buttonLoadLaterFile = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.groupCurrentState = new System.Windows.Forms.GroupBox();
            this.textSettletime = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textSelectedPosition = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.textSelectedFill = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textSelectedTime = new System.Windows.Forms.Label();
            this.textSelectedEXTime = new System.Windows.Forms.Label();
            this.textSelectedDate = new System.Windows.Forms.Label();
            this.textMessageBox = new System.Windows.Forms.Label();
            this.textBoxInitialState = new System.Windows.Forms.TextBox();
            this.buttonLoadLaterEnd = new System.Windows.Forms.Button();
            this.buttonLoadEarlierEnd = new System.Windows.Forms.Button();
            this.EXSettle = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.groupCurrentState.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxFillEvents
            // 
            this.listBoxFillEvents.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxFillEvents.FormattingEnabled = true;
            this.listBoxFillEvents.Location = new System.Drawing.Point(8, 145);
            this.listBoxFillEvents.Name = "listBoxFillEvents";
            this.listBoxFillEvents.Size = new System.Drawing.Size(351, 251);
            this.listBoxFillEvents.TabIndex = 1;
            this.listBoxFillEvents.SelectedIndexChanged += new System.EventHandler(this.Control_SelectedIndexChanged);
            // 
            // comboBoxInstrumentNames
            // 
            this.comboBoxInstrumentNames.FormattingEnabled = true;
            this.comboBoxInstrumentNames.Location = new System.Drawing.Point(3, 3);
            this.comboBoxInstrumentNames.Name = "comboBoxInstrumentNames";
            this.comboBoxInstrumentNames.Size = new System.Drawing.Size(271, 21);
            this.comboBoxInstrumentNames.TabIndex = 2;
            this.comboBoxInstrumentNames.SelectedIndexChanged += new System.EventHandler(this.Control_SelectedIndexChanged);
            // 
            // textStartDate
            // 
            this.textStartDate.Location = new System.Drawing.Point(65, 59);
            this.textStartDate.Name = "textStartDate";
            this.textStartDate.Size = new System.Drawing.Size(100, 17);
            this.textStartDate.TabIndex = 3;
            this.textStartDate.Text = "26 June 2013";
            this.textStartDate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textEndDate
            // 
            this.textEndDate.Location = new System.Drawing.Point(65, 76);
            this.textEndDate.Name = "textEndDate";
            this.textEndDate.Size = new System.Drawing.Size(100, 17);
            this.textEndDate.TabIndex = 4;
            this.textEndDate.Text = "26 June 2013";
            this.textEndDate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Start Date:";
            // 
            // textStartTime
            // 
            this.textStartTime.Location = new System.Drawing.Point(171, 59);
            this.textStartTime.Name = "textStartTime";
            this.textStartTime.Size = new System.Drawing.Size(125, 17);
            this.textStartTime.TabIndex = 6;
            this.textStartTime.Text = "14:52:32 +133 ms";
            this.textStartTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textEndTime
            // 
            this.textEndTime.Location = new System.Drawing.Point(170, 76);
            this.textEndTime.Name = "textEndTime";
            this.textEndTime.Size = new System.Drawing.Size(125, 17);
            this.textEndTime.TabIndex = 7;
            this.textEndTime.Text = "14:52:32 +133 ms";
            this.textEndTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "End Date:";
            // 
            // buttonLoadEarlierFile
            // 
            this.buttonLoadEarlierFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLoadEarlierFile.Location = new System.Drawing.Point(4, 30);
            this.buttonLoadEarlierFile.Name = "buttonLoadEarlierFile";
            this.buttonLoadEarlierFile.Size = new System.Drawing.Size(35, 26);
            this.buttonLoadEarlierFile.TabIndex = 9;
            this.buttonLoadEarlierFile.Text = "<";
            this.buttonLoadEarlierFile.UseVisualStyleBackColor = true;
            this.buttonLoadEarlierFile.Click += new System.EventHandler(this.Button_Clicked);
            // 
            // buttonLoadLaterFile
            // 
            this.buttonLoadLaterFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLoadLaterFile.Location = new System.Drawing.Point(45, 30);
            this.buttonLoadLaterFile.Name = "buttonLoadLaterFile";
            this.buttonLoadLaterFile.Size = new System.Drawing.Size(35, 26);
            this.buttonLoadLaterFile.TabIndex = 10;
            this.buttonLoadLaterFile.Text = ">";
            this.buttonLoadLaterFile.UseVisualStyleBackColor = true;
            this.buttonLoadLaterFile.Click += new System.EventHandler(this.Button_Clicked);
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(8, 105);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(350, 11);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 11;
            // 
            // groupCurrentState
            // 
            this.groupCurrentState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupCurrentState.Controls.Add(this.EXSettle);
            this.groupCurrentState.Controls.Add(this.label10);
            this.groupCurrentState.Controls.Add(this.textSettletime);
            this.groupCurrentState.Controls.Add(this.label8);
            this.groupCurrentState.Controls.Add(this.label7);
            this.groupCurrentState.Controls.Add(this.label6);
            this.groupCurrentState.Controls.Add(this.textSelectedPosition);
            this.groupCurrentState.Controls.Add(this.label5);
            this.groupCurrentState.Controls.Add(this.textSelectedFill);
            this.groupCurrentState.Controls.Add(this.label4);
            this.groupCurrentState.Controls.Add(this.label3);
            this.groupCurrentState.Controls.Add(this.textSelectedTime);
            this.groupCurrentState.Controls.Add(this.textSelectedEXTime);
            this.groupCurrentState.Controls.Add(this.textSelectedDate);
            this.groupCurrentState.Location = new System.Drawing.Point(365, 119);
            this.groupCurrentState.Name = "groupCurrentState";
            this.groupCurrentState.Size = new System.Drawing.Size(181, 189);
            this.groupCurrentState.TabIndex = 12;
            this.groupCurrentState.TabStop = false;
            this.groupCurrentState.Text = "selected state";
            // 
            // textSettletime
            // 
            this.textSettletime.AutoSize = true;
            this.textSettletime.Location = new System.Drawing.Point(66, 117);
            this.textSettletime.Name = "textSettletime";
            this.textSettletime.Size = new System.Drawing.Size(83, 13);
            this.textSettletime.TabIndex = 12;
            this.textSettletime.Text = "1:45:00.234 PM";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(8, 117);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(59, 13);
            this.label8.TabIndex = 11;
            this.label8.Text = "Settletime :";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 61);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(53, 13);
            this.label7.TabIndex = 9;
            this.label7.Text = "EX Time :";
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(18, 96);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(49, 21);
            this.label6.TabIndex = 8;
            this.label6.Text = "Position:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textSelectedPosition
            // 
            this.textSelectedPosition.AutoEllipsis = true;
            this.textSelectedPosition.Location = new System.Drawing.Point(66, 96);
            this.textSelectedPosition.Name = "textSelectedPosition";
            this.textSelectedPosition.Size = new System.Drawing.Size(64, 21);
            this.textSelectedPosition.TabIndex = 7;
            this.textSelectedPosition.Text = "0 @ 9999.0";
            this.textSelectedPosition.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(20, 79);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(47, 21);
            this.label5.TabIndex = 6;
            this.label5.Text = "Fill:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textSelectedFill
            // 
            this.textSelectedFill.Location = new System.Drawing.Point(66, 79);
            this.textSelectedFill.Name = "textSelectedFill";
            this.textSelectedFill.Size = new System.Drawing.Size(73, 21);
            this.textSelectedFill.TabIndex = 5;
            this.textSelectedFill.Text = "0 @ 9999.0";
            this.textSelectedFill.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(34, 41);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(33, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Time:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(34, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Date:";
            // 
            // textSelectedTime
            // 
            this.textSelectedTime.Location = new System.Drawing.Point(66, 37);
            this.textSelectedTime.Name = "textSelectedTime";
            this.textSelectedTime.Size = new System.Drawing.Size(90, 20);
            this.textSelectedTime.TabIndex = 1;
            this.textSelectedTime.Text = "1:45:00.234 PM";
            this.textSelectedTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textSelectedEXTime
            // 
            this.textSelectedEXTime.AutoSize = true;
            this.textSelectedEXTime.Location = new System.Drawing.Point(66, 61);
            this.textSelectedEXTime.Name = "textSelectedEXTime";
            this.textSelectedEXTime.Size = new System.Drawing.Size(83, 13);
            this.textSelectedEXTime.TabIndex = 10;
            this.textSelectedEXTime.Text = "1:45:00.234 PM";
            this.textSelectedEXTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textSelectedDate
            // 
            this.textSelectedDate.Location = new System.Drawing.Point(49, 21);
            this.textSelectedDate.Name = "textSelectedDate";
            this.textSelectedDate.Size = new System.Drawing.Size(107, 20);
            this.textSelectedDate.TabIndex = 0;
            this.textSelectedDate.Text = "Mon 12 Jun 2013";
            this.textSelectedDate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textMessageBox
            // 
            this.textMessageBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textMessageBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textMessageBox.Location = new System.Drawing.Point(8, 100);
            this.textMessageBox.Name = "textMessageBox";
            this.textMessageBox.Size = new System.Drawing.Size(351, 18);
            this.textMessageBox.TabIndex = 13;
            this.textMessageBox.Text = "text MessageBox";
            this.textMessageBox.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textBoxInitialState
            // 
            this.textBoxInitialState.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxInitialState.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.textBoxInitialState.Location = new System.Drawing.Point(8, 121);
            this.textBoxInitialState.Name = "textBoxInitialState";
            this.textBoxInitialState.ReadOnly = true;
            this.textBoxInitialState.Size = new System.Drawing.Size(351, 20);
            this.textBoxInitialState.TabIndex = 14;
            this.textBoxInitialState.Click += new System.EventHandler(this.Control_SelectedIndexChanged);
            // 
            // buttonLoadLaterEnd
            // 
            this.buttonLoadLaterEnd.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLoadLaterEnd.Location = new System.Drawing.Point(157, 30);
            this.buttonLoadLaterEnd.Name = "buttonLoadLaterEnd";
            this.buttonLoadLaterEnd.Size = new System.Drawing.Size(35, 26);
            this.buttonLoadLaterEnd.TabIndex = 16;
            this.buttonLoadLaterEnd.Text = ">";
            this.buttonLoadLaterEnd.UseVisualStyleBackColor = true;
            this.buttonLoadLaterEnd.Click += new System.EventHandler(this.Button_Clicked);
            // 
            // buttonLoadEarlierEnd
            // 
            this.buttonLoadEarlierEnd.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLoadEarlierEnd.Location = new System.Drawing.Point(116, 30);
            this.buttonLoadEarlierEnd.Name = "buttonLoadEarlierEnd";
            this.buttonLoadEarlierEnd.Size = new System.Drawing.Size(35, 26);
            this.buttonLoadEarlierEnd.TabIndex = 15;
            this.buttonLoadEarlierEnd.Text = "<";
            this.buttonLoadEarlierEnd.UseVisualStyleBackColor = true;
            this.buttonLoadEarlierEnd.Click += new System.EventHandler(this.Button_Clicked);
            // 
            // EXSettle
            // 
            this.EXSettle.AutoSize = true;
            this.EXSettle.Location = new System.Drawing.Point(66, 130);
            this.EXSettle.Name = "EXSettle";
            this.EXSettle.Size = new System.Drawing.Size(83, 13);
            this.EXSettle.TabIndex = 14;
            this.EXSettle.Text = "1:45:00.234 PM";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(16, 130);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(51, 13);
            this.label10.TabIndex = 13;
            this.label10.Text = "EXSettle:";
            // 
            // EventSeriesView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonLoadLaterEnd);
            this.Controls.Add(this.buttonLoadEarlierEnd);
            this.Controls.Add(this.textBoxInitialState);
            this.Controls.Add(this.groupCurrentState);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.buttonLoadLaterFile);
            this.Controls.Add(this.buttonLoadEarlierFile);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textEndTime);
            this.Controls.Add(this.textStartTime);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textEndDate);
            this.Controls.Add(this.textStartDate);
            this.Controls.Add(this.comboBoxInstrumentNames);
            this.Controls.Add(this.listBoxFillEvents);
            this.Controls.Add(this.textMessageBox);
            this.Name = "EventSeriesView";
            this.Size = new System.Drawing.Size(549, 407);
            this.groupCurrentState.ResumeLayout(false);
            this.groupCurrentState.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxFillEvents;
        private System.Windows.Forms.ComboBox comboBoxInstrumentNames;
        private System.Windows.Forms.Label textStartDate;
        private System.Windows.Forms.Label textEndDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label textStartTime;
        private System.Windows.Forms.Label textEndTime;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonLoadEarlierFile;
        private System.Windows.Forms.Button buttonLoadLaterFile;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.GroupBox groupCurrentState;
        private System.Windows.Forms.Label textSelectedDate;
        private System.Windows.Forms.Label textSelectedTime;
        private System.Windows.Forms.Label textSelectedFill;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label textMessageBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label textSelectedPosition;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxInitialState;
        private System.Windows.Forms.Label textSelectedEXTime;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label textSettletime;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button buttonLoadLaterEnd;
        private System.Windows.Forms.Button buttonLoadEarlierEnd;
        private System.Windows.Forms.Label EXSettle;
        private System.Windows.Forms.Label label10;
    }
}

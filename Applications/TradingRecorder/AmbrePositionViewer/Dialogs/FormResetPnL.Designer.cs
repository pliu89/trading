namespace Ambre.PositionViewer.Dialogs
{
    partial class FormResetPnL
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
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxFillHubs = new System.Windows.Forms.ComboBox();
            this.comboBoxInstruments = new System.Windows.Forms.ComboBox();
            this.textBoxRealPnL = new System.Windows.Forms.TextBox();
            this.textBoxRealStartingPnL = new System.Windows.Forms.TextBox();
            this.buttonSubmit = new System.Windows.Forms.Button();
            this.checkBoxDailyPnL = new System.Windows.Forms.CheckBox();
            this.checkBoxStartingPnL = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Location = new System.Drawing.Point(6, 16);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(135, 17);
            this.radioButton1.TabIndex = 0;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "all mgrs / all instr books";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.Radio_CheckChanged);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(6, 34);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(138, 17);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.Text = "one mgr / all instr books";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.Radio_CheckChanged);
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Location = new System.Drawing.Point(6, 52);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(141, 17);
            this.radioButton3.TabIndex = 2;
            this.radioButton3.Text = "one mgr / one instr book";
            this.radioButton3.UseVisualStyleBackColor = true;
            this.radioButton3.CheckedChanged += new System.EventHandler(this.Radio_CheckChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButton2);
            this.groupBox1.Controls.Add(this.radioButton3);
            this.groupBox1.Controls.Add(this.radioButton1);
            this.groupBox1.Location = new System.Drawing.Point(1, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(157, 74);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Reset selected items";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(159, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Fill manager";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(159, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Instruments";
            // 
            // comboBoxFillHubs
            // 
            this.comboBoxFillHubs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxFillHubs.FormattingEnabled = true;
            this.comboBoxFillHubs.Location = new System.Drawing.Point(161, 17);
            this.comboBoxFillHubs.Name = "comboBoxFillHubs";
            this.comboBoxFillHubs.Size = new System.Drawing.Size(127, 21);
            this.comboBoxFillHubs.TabIndex = 8;
            this.comboBoxFillHubs.SelectedIndexChanged += new System.EventHandler(this.box_SelectedIndexChanged);
            // 
            // comboBoxInstruments
            // 
            this.comboBoxInstruments.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxInstruments.FormattingEnabled = true;
            this.comboBoxInstruments.Location = new System.Drawing.Point(161, 56);
            this.comboBoxInstruments.Name = "comboBoxInstruments";
            this.comboBoxInstruments.Size = new System.Drawing.Size(127, 21);
            this.comboBoxInstruments.TabIndex = 9;
            // 
            // textBoxRealPnL
            // 
            this.textBoxRealPnL.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxRealPnL.Location = new System.Drawing.Point(117, 83);
            this.textBoxRealPnL.Name = "textBoxRealPnL";
            this.textBoxRealPnL.Size = new System.Drawing.Size(100, 20);
            this.textBoxRealPnL.TabIndex = 10;
            this.textBoxRealPnL.Text = "0.00";
            // 
            // textBoxRealStartingPnL
            // 
            this.textBoxRealStartingPnL.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxRealStartingPnL.Location = new System.Drawing.Point(117, 106);
            this.textBoxRealStartingPnL.Name = "textBoxRealStartingPnL";
            this.textBoxRealStartingPnL.Size = new System.Drawing.Size(100, 20);
            this.textBoxRealStartingPnL.TabIndex = 11;
            this.textBoxRealStartingPnL.Text = "0.00";
            // 
            // buttonSubmit
            // 
            this.buttonSubmit.Location = new System.Drawing.Point(241, 102);
            this.buttonSubmit.Name = "buttonSubmit";
            this.buttonSubmit.Size = new System.Drawing.Size(61, 23);
            this.buttonSubmit.TabIndex = 14;
            this.buttonSubmit.Text = "submit";
            this.buttonSubmit.UseVisualStyleBackColor = true;
            this.buttonSubmit.Click += new System.EventHandler(this.Button_Click);
            // 
            // checkBoxDailyPnL
            // 
            this.checkBoxDailyPnL.AutoSize = true;
            this.checkBoxDailyPnL.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.checkBoxDailyPnL.Location = new System.Drawing.Point(1, 85);
            this.checkBoxDailyPnL.Name = "checkBoxDailyPnL";
            this.checkBoxDailyPnL.Size = new System.Drawing.Size(110, 17);
            this.checkBoxDailyPnL.TabIndex = 15;
            this.checkBoxDailyPnL.Text = "Today\'s Real PnL";
            this.checkBoxDailyPnL.UseVisualStyleBackColor = true;
            this.checkBoxDailyPnL.CheckedChanged += new System.EventHandler(this.CheckBox_CheckedChanged);
            // 
            // checkBoxStartingPnL
            // 
            this.checkBoxStartingPnL.AutoSize = true;
            this.checkBoxStartingPnL.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.checkBoxStartingPnL.Location = new System.Drawing.Point(2, 108);
            this.checkBoxStartingPnL.Name = "checkBoxStartingPnL";
            this.checkBoxStartingPnL.Size = new System.Drawing.Size(109, 17);
            this.checkBoxStartingPnL.TabIndex = 16;
            this.checkBoxStartingPnL.Text = "Starting Real PnL";
            this.checkBoxStartingPnL.UseVisualStyleBackColor = true;
            this.checkBoxStartingPnL.CheckedChanged += new System.EventHandler(this.CheckBox_CheckedChanged);
            // 
            // FormResetPnL
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(314, 128);
            this.Controls.Add(this.checkBoxStartingPnL);
            this.Controls.Add(this.checkBoxDailyPnL);
            this.Controls.Add(this.buttonSubmit);
            this.Controls.Add(this.textBoxRealStartingPnL);
            this.Controls.Add(this.textBoxRealPnL);
            this.Controls.Add(this.comboBoxInstruments);
            this.Controls.Add(this.comboBoxFillHubs);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.MinimumSize = new System.Drawing.Size(330, 38);
            this.Name = "FormResetPnL";
            this.Text = "Reset Fill Manager";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxFillHubs;
        private System.Windows.Forms.ComboBox comboBoxInstruments;
        private System.Windows.Forms.TextBox textBoxRealPnL;
        private System.Windows.Forms.TextBox textBoxRealStartingPnL;
        private System.Windows.Forms.Button buttonSubmit;
        private System.Windows.Forms.CheckBox checkBoxDailyPnL;
        private System.Windows.Forms.CheckBox checkBoxStartingPnL;
    }
}
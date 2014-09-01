namespace Ambre.PositionViewer.Dialogs
{
    partial class ControlPnLTransferTool
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
            this.comboBoxFillHubs = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxInstruments = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxRealPnL = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxRealPnLNew = new System.Windows.Forms.TextBox();
            this.buttonSubmitRealPnL = new System.Windows.Forms.Button();
            this.buttonSubmitStartRealPnL = new System.Windows.Forms.Button();
            this.textBoxStartRealPnLNew = new System.Windows.Forms.TextBox();
            this.textBoxStartRealPnL = new System.Windows.Forms.TextBox();
            this.textDescription = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // comboBoxFillHubs
            // 
            this.comboBoxFillHubs.FormattingEnabled = true;
            this.comboBoxFillHubs.Location = new System.Drawing.Point(6, 17);
            this.comboBoxFillHubs.Name = "comboBoxFillHubs";
            this.comboBoxFillHubs.Size = new System.Drawing.Size(171, 21);
            this.comboBoxFillHubs.TabIndex = 0;
            this.comboBoxFillHubs.SelectedIndexChanged += new System.EventHandler(this.ComboBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 1);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Fill manager";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(3, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Instruments";
            // 
            // comboBoxInstruments
            // 
            this.comboBoxInstruments.FormattingEnabled = true;
            this.comboBoxInstruments.Location = new System.Drawing.Point(6, 57);
            this.comboBoxInstruments.Name = "comboBoxInstruments";
            this.comboBoxInstruments.Size = new System.Drawing.Size(197, 21);
            this.comboBoxInstruments.TabIndex = 2;
            this.comboBoxInstruments.SelectedIndexChanged += new System.EventHandler(this.ComboBox_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 85);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(94, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Today\'s Real PnL:";
            // 
            // textBoxRealPnL
            // 
            this.textBoxRealPnL.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRealPnL.Location = new System.Drawing.Point(103, 82);
            this.textBoxRealPnL.Name = "textBoxRealPnL";
            this.textBoxRealPnL.ReadOnly = true;
            this.textBoxRealPnL.Size = new System.Drawing.Size(100, 20);
            this.textBoxRealPnL.TabIndex = 5;
            this.textBoxRealPnL.Text = "0";
            this.textBoxRealPnL.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 135);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(93, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Starting Real PnL:";
            // 
            // textBoxRealPnLNew
            // 
            this.textBoxRealPnLNew.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRealPnLNew.Location = new System.Drawing.Point(103, 101);
            this.textBoxRealPnLNew.Name = "textBoxRealPnLNew";
            this.textBoxRealPnLNew.Size = new System.Drawing.Size(100, 20);
            this.textBoxRealPnLNew.TabIndex = 7;
            this.textBoxRealPnLNew.Text = "0";
            this.textBoxRealPnLNew.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // buttonSubmitRealPnL
            // 
            this.buttonSubmitRealPnL.Location = new System.Drawing.Point(204, 81);
            this.buttonSubmitRealPnL.Name = "buttonSubmitRealPnL";
            this.buttonSubmitRealPnL.Size = new System.Drawing.Size(49, 23);
            this.buttonSubmitRealPnL.TabIndex = 8;
            this.buttonSubmitRealPnL.Text = "submit";
            this.buttonSubmitRealPnL.UseVisualStyleBackColor = true;
            this.buttonSubmitRealPnL.Click += new System.EventHandler(this.Button_Click);
            // 
            // buttonSubmitStartRealPnL
            // 
            this.buttonSubmitStartRealPnL.Location = new System.Drawing.Point(204, 131);
            this.buttonSubmitStartRealPnL.Name = "buttonSubmitStartRealPnL";
            this.buttonSubmitStartRealPnL.Size = new System.Drawing.Size(49, 23);
            this.buttonSubmitStartRealPnL.TabIndex = 11;
            this.buttonSubmitStartRealPnL.Text = "submit";
            this.buttonSubmitStartRealPnL.UseVisualStyleBackColor = true;
            this.buttonSubmitStartRealPnL.Click += new System.EventHandler(this.Button_Click);
            // 
            // textBoxStartRealPnLNew
            // 
            this.textBoxStartRealPnLNew.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxStartRealPnLNew.Location = new System.Drawing.Point(103, 151);
            this.textBoxStartRealPnLNew.Name = "textBoxStartRealPnLNew";
            this.textBoxStartRealPnLNew.Size = new System.Drawing.Size(100, 20);
            this.textBoxStartRealPnLNew.TabIndex = 10;
            this.textBoxStartRealPnLNew.Text = "0";
            this.textBoxStartRealPnLNew.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textBoxStartRealPnL
            // 
            this.textBoxStartRealPnL.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxStartRealPnL.Location = new System.Drawing.Point(103, 132);
            this.textBoxStartRealPnL.Name = "textBoxStartRealPnL";
            this.textBoxStartRealPnL.ReadOnly = true;
            this.textBoxStartRealPnL.Size = new System.Drawing.Size(100, 20);
            this.textBoxStartRealPnL.TabIndex = 9;
            this.textBoxStartRealPnL.Text = "0";
            this.textBoxStartRealPnL.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // textDescription
            // 
            this.textDescription.AutoSize = true;
            this.textDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textDescription.Location = new System.Drawing.Point(100, 1);
            this.textDescription.Name = "textDescription";
            this.textDescription.Size = new System.Drawing.Size(66, 13);
            this.textDescription.TabIndex = 12;
            this.textDescription.Text = "extra label";
            // 
            // ControlPnLTransferTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textDescription);
            this.Controls.Add(this.buttonSubmitStartRealPnL);
            this.Controls.Add(this.textBoxStartRealPnLNew);
            this.Controls.Add(this.textBoxStartRealPnL);
            this.Controls.Add(this.buttonSubmitRealPnL);
            this.Controls.Add(this.textBoxRealPnLNew);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxRealPnL);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBoxInstruments);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxFillHubs);
            this.Name = "ControlPnLTransferTool";
            this.Size = new System.Drawing.Size(259, 179);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxFillHubs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxInstruments;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxRealPnL;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxRealPnLNew;
        private System.Windows.Forms.Button buttonSubmitRealPnL;
        private System.Windows.Forms.Button buttonSubmitStartRealPnL;
        private System.Windows.Forms.TextBox textBoxStartRealPnLNew;
        private System.Windows.Forms.TextBox textBoxStartRealPnL;
        private System.Windows.Forms.Label textDescription;
    }
}

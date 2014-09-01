namespace Ambre.Breconcile.BookReaders
{
    partial class FormBookViewer
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
            this.listBoxFillEvents = new System.Windows.Forms.ListBox();
            this.textStartingBookDateTime = new System.Windows.Forms.Label();
            this.comboBoxInstrumentNames = new System.Windows.Forms.ComboBox();
            this.textStartingBookFill = new System.Windows.Forms.Label();
            this.textFinalBookDateTime = new System.Windows.Forms.Label();
            this.textFinalBookFill = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonLoadEarlier = new System.Windows.Forms.Button();
            this.buttonLoadLater = new System.Windows.Forms.Button();
            this.textStartDate = new System.Windows.Forms.Label();
            this.textEndDate = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // listBoxFillEvents
            // 
            this.listBoxFillEvents.FormattingEnabled = true;
            this.listBoxFillEvents.Location = new System.Drawing.Point(15, 55);
            this.listBoxFillEvents.Name = "listBoxFillEvents";
            this.listBoxFillEvents.Size = new System.Drawing.Size(271, 329);
            this.listBoxFillEvents.TabIndex = 0;
            this.listBoxFillEvents.SelectedIndexChanged += new System.EventHandler(this.Control_SelectedIndexChanged);
            // 
            // textStartingBookDateTime
            // 
            this.textStartingBookDateTime.Location = new System.Drawing.Point(15, 36);
            this.textStartingBookDateTime.Name = "textStartingBookDateTime";
            this.textStartingBookDateTime.Size = new System.Drawing.Size(167, 16);
            this.textStartingBookDateTime.TabIndex = 1;
            this.textStartingBookDateTime.Text = "-";
            // 
            // comboBoxInstrumentNames
            // 
            this.comboBoxInstrumentNames.FormattingEnabled = true;
            this.comboBoxInstrumentNames.Location = new System.Drawing.Point(18, 12);
            this.comboBoxInstrumentNames.Name = "comboBoxInstrumentNames";
            this.comboBoxInstrumentNames.Size = new System.Drawing.Size(192, 21);
            this.comboBoxInstrumentNames.TabIndex = 2;
            this.comboBoxInstrumentNames.SelectedIndexChanged += new System.EventHandler(this.Control_SelectedIndexChanged);
            // 
            // textStartingBookFill
            // 
            this.textStartingBookFill.AutoEllipsis = true;
            this.textStartingBookFill.Location = new System.Drawing.Point(208, 36);
            this.textStartingBookFill.Name = "textStartingBookFill";
            this.textStartingBookFill.Size = new System.Drawing.Size(110, 16);
            this.textStartingBookFill.TabIndex = 3;
            this.textStartingBookFill.Text = "-";
            // 
            // textFinalBookDateTime
            // 
            this.textFinalBookDateTime.Location = new System.Drawing.Point(15, 387);
            this.textFinalBookDateTime.Name = "textFinalBookDateTime";
            this.textFinalBookDateTime.Size = new System.Drawing.Size(167, 16);
            this.textFinalBookDateTime.TabIndex = 4;
            this.textFinalBookDateTime.Text = "-";
            // 
            // textFinalBookFill
            // 
            this.textFinalBookFill.Location = new System.Drawing.Point(208, 387);
            this.textFinalBookFill.Name = "textFinalBookFill";
            this.textFinalBookFill.Size = new System.Drawing.Size(110, 16);
            this.textFinalBookFill.TabIndex = 5;
            this.textFinalBookFill.Text = "-";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Symbol", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
            this.label1.Location = new System.Drawing.Point(80, 424);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 16);
            this.label1.TabIndex = 6;
            // 
            // buttonLoadEarlier
            // 
            this.buttonLoadEarlier.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLoadEarlier.Location = new System.Drawing.Point(216, 10);
            this.buttonLoadEarlier.Name = "buttonLoadEarlier";
            this.buttonLoadEarlier.Size = new System.Drawing.Size(17, 23);
            this.buttonLoadEarlier.TabIndex = 7;
            this.buttonLoadEarlier.Text = "<";
            this.buttonLoadEarlier.UseVisualStyleBackColor = true;
            this.buttonLoadEarlier.Click += new System.EventHandler(this.Button_Clicked);
            // 
            // buttonLoadLater
            // 
            this.buttonLoadLater.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLoadLater.Location = new System.Drawing.Point(239, 10);
            this.buttonLoadLater.Name = "buttonLoadLater";
            this.buttonLoadLater.Size = new System.Drawing.Size(17, 23);
            this.buttonLoadLater.TabIndex = 8;
            this.buttonLoadLater.Text = ">";
            this.buttonLoadLater.UseVisualStyleBackColor = true;
            this.buttonLoadLater.Click += new System.EventHandler(this.Button_Clicked);
            // 
            // textStartDate
            // 
            this.textStartDate.Location = new System.Drawing.Point(119, 426);
            this.textStartDate.Name = "textStartDate";
            this.textStartDate.Size = new System.Drawing.Size(167, 16);
            this.textStartDate.TabIndex = 9;
            this.textStartDate.Text = "-";
            // 
            // textEndDate
            // 
            this.textEndDate.Location = new System.Drawing.Point(119, 442);
            this.textEndDate.Name = "textEndDate";
            this.textEndDate.Size = new System.Drawing.Size(167, 16);
            this.textEndDate.TabIndex = 10;
            this.textEndDate.Text = "-";
            // 
            // FormBookViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(330, 478);
            this.Controls.Add(this.textEndDate);
            this.Controls.Add(this.textStartDate);
            this.Controls.Add(this.buttonLoadLater);
            this.Controls.Add(this.buttonLoadEarlier);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textFinalBookFill);
            this.Controls.Add(this.textFinalBookDateTime);
            this.Controls.Add(this.textStartingBookFill);
            this.Controls.Add(this.comboBoxInstrumentNames);
            this.Controls.Add(this.textStartingBookDateTime);
            this.Controls.Add(this.listBoxFillEvents);
            this.Name = "FormBookViewer";
            this.Text = "Viewer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxFillEvents;
        private System.Windows.Forms.Label textStartingBookDateTime;
        private System.Windows.Forms.ComboBox comboBoxInstrumentNames;
        private System.Windows.Forms.Label textStartingBookFill;
        private System.Windows.Forms.Label textFinalBookDateTime;
        private System.Windows.Forms.Label textFinalBookFill;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonLoadEarlier;
        private System.Windows.Forms.Button buttonLoadLater;
        private System.Windows.Forms.Label textStartDate;
        private System.Windows.Forms.Label textEndDate;
    }
}
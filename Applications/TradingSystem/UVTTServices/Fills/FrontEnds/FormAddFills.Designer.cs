namespace UV.TTServices.Fills.FrontEnds
{
    partial class FormAddFills
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
            this.buttonSubmitFill = new System.Windows.Forms.Button();
            this.labelInstrumentName = new System.Windows.Forms.Label();
            this.labelExpirationDate = new System.Windows.Forms.Label();
            this.textBoxQty = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxPrice = new System.Windows.Forms.TextBox();
            this.labelBidQty = new System.Windows.Forms.Label();
            this.labelAskQty = new System.Windows.Forms.Label();
            this.labelBidPrice = new System.Windows.Forms.Label();
            this.labelAskPrice = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.buttonDeleteBook = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonSubmitFill
            // 
            this.buttonSubmitFill.BackColor = System.Drawing.Color.PaleGoldenrod;
            this.buttonSubmitFill.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSubmitFill.Location = new System.Drawing.Point(296, 27);
            this.buttonSubmitFill.Name = "buttonSubmitFill";
            this.buttonSubmitFill.Size = new System.Drawing.Size(97, 23);
            this.buttonSubmitFill.TabIndex = 0;
            this.buttonSubmitFill.Text = "Submit Fill";
            this.buttonSubmitFill.UseVisualStyleBackColor = false;
            this.buttonSubmitFill.Click += new System.EventHandler(this.buttonSubmitFill_Click);
            // 
            // labelInstrumentName
            // 
            this.labelInstrumentName.AutoSize = true;
            this.labelInstrumentName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelInstrumentName.Location = new System.Drawing.Point(2, 6);
            this.labelInstrumentName.Name = "labelInstrumentName";
            this.labelInstrumentName.Size = new System.Drawing.Size(92, 13);
            this.labelInstrumentName.TabIndex = 1;
            this.labelInstrumentName.Text = "CME ES Mar13";
            // 
            // labelExpirationDate
            // 
            this.labelExpirationDate.Location = new System.Drawing.Point(337, 6);
            this.labelExpirationDate.Name = "labelExpirationDate";
            this.labelExpirationDate.Size = new System.Drawing.Size(95, 13);
            this.labelExpirationDate.TabIndex = 2;
            this.labelExpirationDate.Text = "15 Mar 2013";
            // 
            // textBoxQty
            // 
            this.textBoxQty.Location = new System.Drawing.Point(50, 27);
            this.textBoxQty.Name = "textBoxQty";
            this.textBoxQty.Size = new System.Drawing.Size(83, 20);
            this.textBoxQty.TabIndex = 3;
            this.textBoxQty.Enter += new System.EventHandler(this.textBoxPriceQty_Enter);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "QTY:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(293, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Expiry:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(149, 30);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(42, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "PRICE:";
            // 
            // textBoxPrice
            // 
            this.textBoxPrice.Location = new System.Drawing.Point(196, 27);
            this.textBoxPrice.Name = "textBoxPrice";
            this.textBoxPrice.Size = new System.Drawing.Size(83, 20);
            this.textBoxPrice.TabIndex = 7;
            this.textBoxPrice.Enter += new System.EventHandler(this.textBoxPriceQty_Enter);
            // 
            // labelBidQty
            // 
            this.labelBidQty.BackColor = System.Drawing.Color.RoyalBlue;
            this.labelBidQty.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelBidQty.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelBidQty.ForeColor = System.Drawing.Color.White;
            this.labelBidQty.Location = new System.Drawing.Point(70, 54);
            this.labelBidQty.Name = "labelBidQty";
            this.labelBidQty.Size = new System.Drawing.Size(78, 20);
            this.labelBidQty.TabIndex = 8;
            this.labelBidQty.Text = "100000";
            this.labelBidQty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelAskQty
            // 
            this.labelAskQty.BackColor = System.Drawing.Color.Crimson;
            this.labelAskQty.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelAskQty.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAskQty.ForeColor = System.Drawing.Color.White;
            this.labelAskQty.Location = new System.Drawing.Point(317, 54);
            this.labelAskQty.Name = "labelAskQty";
            this.labelAskQty.Size = new System.Drawing.Size(78, 20);
            this.labelAskQty.TabIndex = 9;
            this.labelAskQty.Text = "100000";
            this.labelAskQty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelBidPrice
            // 
            this.labelBidPrice.BackColor = System.Drawing.SystemColors.ControlLight;
            this.labelBidPrice.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelBidPrice.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelBidPrice.ForeColor = System.Drawing.Color.Black;
            this.labelBidPrice.Location = new System.Drawing.Point(149, 54);
            this.labelBidPrice.Name = "labelBidPrice";
            this.labelBidPrice.Size = new System.Drawing.Size(78, 20);
            this.labelBidPrice.TabIndex = 10;
            this.labelBidPrice.Text = "100000";
            this.labelBidPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelAskPrice
            // 
            this.labelAskPrice.BackColor = System.Drawing.SystemColors.ControlLight;
            this.labelAskPrice.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelAskPrice.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAskPrice.ForeColor = System.Drawing.Color.Black;
            this.labelAskPrice.Location = new System.Drawing.Point(238, 54);
            this.labelAskPrice.Name = "labelAskPrice";
            this.labelAskPrice.Size = new System.Drawing.Size(78, 20);
            this.labelAskPrice.TabIndex = 11;
            this.labelAskPrice.Text = "100000";
            this.labelAskPrice.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(22, 58);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(42, 13);
            this.label8.TabIndex = 12;
            this.label8.Text = "market:";
            // 
            // buttonDeleteBook
            // 
            this.buttonDeleteBook.BackColor = System.Drawing.Color.PaleGoldenrod;
            this.buttonDeleteBook.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDeleteBook.Location = new System.Drawing.Point(401, 51);
            this.buttonDeleteBook.Name = "buttonDeleteBook";
            this.buttonDeleteBook.Size = new System.Drawing.Size(110, 23);
            this.buttonDeleteBook.TabIndex = 13;
            this.buttonDeleteBook.Text = "Delete Book";
            this.buttonDeleteBook.UseVisualStyleBackColor = false;
            this.buttonDeleteBook.Click += new System.EventHandler(this.buttonDeleteBook_Click);
            // 
            // FormAddFills
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(514, 76);
            this.Controls.Add(this.buttonDeleteBook);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.labelAskPrice);
            this.Controls.Add(this.labelBidPrice);
            this.Controls.Add(this.labelAskQty);
            this.Controls.Add(this.labelBidQty);
            this.Controls.Add(this.textBoxPrice);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxQty);
            this.Controls.Add(this.labelExpirationDate);
            this.Controls.Add(this.labelInstrumentName);
            this.Controls.Add(this.buttonSubmitFill);
            this.Name = "FormAddFills";
            this.Text = "Add Fills";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormAddFills_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonSubmitFill;
        private System.Windows.Forms.Label labelInstrumentName;
        private System.Windows.Forms.Label labelExpirationDate;
        private System.Windows.Forms.TextBox textBoxQty;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxPrice;
        private System.Windows.Forms.Label labelBidQty;
        private System.Windows.Forms.Label labelAskQty;
        private System.Windows.Forms.Label labelBidPrice;
        private System.Windows.Forms.Label labelAskPrice;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button buttonDeleteBook;
    }
}
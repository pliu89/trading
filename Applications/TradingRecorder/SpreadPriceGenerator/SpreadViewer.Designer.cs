namespace SpreadPriceGenerator
{
    partial class SpreadViewer
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
            this.listBoxMarkets = new System.Windows.Forms.ListBox();
            this.labelSpreadViewer = new System.Windows.Forms.Label();
            this.labelExchange = new System.Windows.Forms.Label();
            this.listBoxProducts = new System.Windows.Forms.ListBox();
            this.labelProducts = new System.Windows.Forms.Label();
            this.listBoxInstruments = new System.Windows.Forms.ListBox();
            this.labelInstruments = new System.Windows.Forms.Label();
            this.labelInstrumentViewer = new System.Windows.Forms.Label();
            this.labelBidPrice = new System.Windows.Forms.Label();
            this.labelAskPrice = new System.Windows.Forms.Label();
            this.labelBidQty = new System.Windows.Forms.Label();
            this.labelAskQty = new System.Windows.Forms.Label();
            this.labelExpirySeries = new System.Windows.Forms.Label();
            this.textBoxBidPrice = new System.Windows.Forms.TextBox();
            this.textBoxAskPrice = new System.Windows.Forms.TextBox();
            this.textBoxBidQty = new System.Windows.Forms.TextBox();
            this.textBoxAskQty = new System.Windows.Forms.TextBox();
            this.textBoxExpirySeries = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // listBoxMarkets
            // 
            this.listBoxMarkets.FormattingEnabled = true;
            this.listBoxMarkets.Location = new System.Drawing.Point(12, 118);
            this.listBoxMarkets.Name = "listBoxMarkets";
            this.listBoxMarkets.Size = new System.Drawing.Size(230, 485);
            this.listBoxMarkets.TabIndex = 0;
            this.listBoxMarkets.SelectedIndexChanged += new System.EventHandler(this.listBoxMarkets_SelectedIndexChanged);
            // 
            // labelSpreadViewer
            // 
            this.labelSpreadViewer.AutoSize = true;
            this.labelSpreadViewer.Font = new System.Drawing.Font("Times New Roman", 27.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSpreadViewer.Location = new System.Drawing.Point(395, 9);
            this.labelSpreadViewer.Name = "labelSpreadViewer";
            this.labelSpreadViewer.Size = new System.Drawing.Size(399, 42);
            this.labelSpreadViewer.TabIndex = 1;
            this.labelSpreadViewer.Text = "Spread Instrument Viewer";
            // 
            // labelExchange
            // 
            this.labelExchange.AutoSize = true;
            this.labelExchange.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelExchange.Location = new System.Drawing.Point(73, 80);
            this.labelExchange.Name = "labelExchange";
            this.labelExchange.Size = new System.Drawing.Size(106, 23);
            this.labelExchange.TabIndex = 2;
            this.labelExchange.Text = "Market List";
            // 
            // listBoxProducts
            // 
            this.listBoxProducts.FormattingEnabled = true;
            this.listBoxProducts.Location = new System.Drawing.Point(269, 118);
            this.listBoxProducts.Name = "listBoxProducts";
            this.listBoxProducts.Size = new System.Drawing.Size(230, 485);
            this.listBoxProducts.TabIndex = 3;
            this.listBoxProducts.SelectedIndexChanged += new System.EventHandler(this.listBoxProducts_SelectedIndexChanged);
            // 
            // labelProducts
            // 
            this.labelProducts.AutoSize = true;
            this.labelProducts.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelProducts.Location = new System.Drawing.Point(327, 80);
            this.labelProducts.Name = "labelProducts";
            this.labelProducts.Size = new System.Drawing.Size(113, 23);
            this.labelProducts.TabIndex = 4;
            this.labelProducts.Text = "Product List";
            // 
            // listBoxInstruments
            // 
            this.listBoxInstruments.FormattingEnabled = true;
            this.listBoxInstruments.Location = new System.Drawing.Point(524, 118);
            this.listBoxInstruments.Name = "listBoxInstruments";
            this.listBoxInstruments.Size = new System.Drawing.Size(230, 485);
            this.listBoxInstruments.TabIndex = 5;
            this.listBoxInstruments.SelectedIndexChanged += new System.EventHandler(this.listBoxInstruments_SelectedIndexChanged);
            // 
            // labelInstruments
            // 
            this.labelInstruments.AutoSize = true;
            this.labelInstruments.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelInstruments.Location = new System.Drawing.Point(570, 80);
            this.labelInstruments.Name = "labelInstruments";
            this.labelInstruments.Size = new System.Drawing.Size(136, 23);
            this.labelInstruments.TabIndex = 6;
            this.labelInstruments.Text = "Instrument List";
            // 
            // labelInstrumentViewer
            // 
            this.labelInstrumentViewer.AutoSize = true;
            this.labelInstrumentViewer.Font = new System.Drawing.Font("Times New Roman", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelInstrumentViewer.Location = new System.Drawing.Point(839, 80);
            this.labelInstrumentViewer.Name = "labelInstrumentViewer";
            this.labelInstrumentViewer.Size = new System.Drawing.Size(189, 23);
            this.labelInstrumentViewer.TabIndex = 7;
            this.labelInstrumentViewer.Text = "Market Depth Viewer";
            // 
            // labelBidPrice
            // 
            this.labelBidPrice.AutoSize = true;
            this.labelBidPrice.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelBidPrice.Location = new System.Drawing.Point(817, 147);
            this.labelBidPrice.Name = "labelBidPrice";
            this.labelBidPrice.Size = new System.Drawing.Size(68, 19);
            this.labelBidPrice.TabIndex = 8;
            this.labelBidPrice.Text = "Bid Price:";
            // 
            // labelAskPrice
            // 
            this.labelAskPrice.AutoSize = true;
            this.labelAskPrice.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAskPrice.Location = new System.Drawing.Point(817, 198);
            this.labelAskPrice.Name = "labelAskPrice";
            this.labelAskPrice.Size = new System.Drawing.Size(72, 19);
            this.labelAskPrice.TabIndex = 9;
            this.labelAskPrice.Text = "Ask Price:";
            // 
            // labelBidQty
            // 
            this.labelBidQty.AutoSize = true;
            this.labelBidQty.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelBidQty.Location = new System.Drawing.Point(817, 252);
            this.labelBidQty.Name = "labelBidQty";
            this.labelBidQty.Size = new System.Drawing.Size(60, 19);
            this.labelBidQty.TabIndex = 10;
            this.labelBidQty.Text = "Bid Qty:";
            // 
            // labelAskQty
            // 
            this.labelAskQty.AutoSize = true;
            this.labelAskQty.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAskQty.Location = new System.Drawing.Point(817, 306);
            this.labelAskQty.Name = "labelAskQty";
            this.labelAskQty.Size = new System.Drawing.Size(64, 19);
            this.labelAskQty.TabIndex = 11;
            this.labelAskQty.Text = "Ask Qty:";
            // 
            // labelExpirySeries
            // 
            this.labelExpirySeries.AutoSize = true;
            this.labelExpirySeries.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelExpirySeries.Location = new System.Drawing.Point(813, 438);
            this.labelExpirySeries.Name = "labelExpirySeries";
            this.labelExpirySeries.Size = new System.Drawing.Size(88, 19);
            this.labelExpirySeries.TabIndex = 12;
            this.labelExpirySeries.Text = "ExpirySeries:";
            // 
            // textBoxBidPrice
            // 
            this.textBoxBidPrice.Location = new System.Drawing.Point(947, 146);
            this.textBoxBidPrice.Name = "textBoxBidPrice";
            this.textBoxBidPrice.Size = new System.Drawing.Size(100, 20);
            this.textBoxBidPrice.TabIndex = 13;
            // 
            // textBoxAskPrice
            // 
            this.textBoxAskPrice.Location = new System.Drawing.Point(947, 197);
            this.textBoxAskPrice.Name = "textBoxAskPrice";
            this.textBoxAskPrice.Size = new System.Drawing.Size(100, 20);
            this.textBoxAskPrice.TabIndex = 14;
            // 
            // textBoxBidQty
            // 
            this.textBoxBidQty.Location = new System.Drawing.Point(947, 251);
            this.textBoxBidQty.Name = "textBoxBidQty";
            this.textBoxBidQty.Size = new System.Drawing.Size(100, 20);
            this.textBoxBidQty.TabIndex = 15;
            // 
            // textBoxAskQty
            // 
            this.textBoxAskQty.Location = new System.Drawing.Point(947, 305);
            this.textBoxAskQty.Name = "textBoxAskQty";
            this.textBoxAskQty.Size = new System.Drawing.Size(100, 20);
            this.textBoxAskQty.TabIndex = 16;
            // 
            // textBoxExpirySeries
            // 
            this.textBoxExpirySeries.Location = new System.Drawing.Point(947, 437);
            this.textBoxExpirySeries.Name = "textBoxExpirySeries";
            this.textBoxExpirySeries.Size = new System.Drawing.Size(100, 20);
            this.textBoxExpirySeries.TabIndex = 17;
            // 
            // SpreadViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1121, 619);
            this.Controls.Add(this.textBoxExpirySeries);
            this.Controls.Add(this.textBoxAskQty);
            this.Controls.Add(this.textBoxBidQty);
            this.Controls.Add(this.textBoxAskPrice);
            this.Controls.Add(this.textBoxBidPrice);
            this.Controls.Add(this.labelExpirySeries);
            this.Controls.Add(this.labelAskQty);
            this.Controls.Add(this.labelBidQty);
            this.Controls.Add(this.labelAskPrice);
            this.Controls.Add(this.labelBidPrice);
            this.Controls.Add(this.labelInstrumentViewer);
            this.Controls.Add(this.labelInstruments);
            this.Controls.Add(this.listBoxInstruments);
            this.Controls.Add(this.labelProducts);
            this.Controls.Add(this.listBoxProducts);
            this.Controls.Add(this.labelExchange);
            this.Controls.Add(this.labelSpreadViewer);
            this.Controls.Add(this.listBoxMarkets);
            this.Name = "SpreadViewer";
            this.Text = "SpreadGenerator";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxMarkets;
        private System.Windows.Forms.Label labelSpreadViewer;
        private System.Windows.Forms.Label labelExchange;
        private System.Windows.Forms.ListBox listBoxProducts;
        private System.Windows.Forms.Label labelProducts;
        private System.Windows.Forms.ListBox listBoxInstruments;
        private System.Windows.Forms.Label labelInstruments;
        private System.Windows.Forms.Label labelInstrumentViewer;
        private System.Windows.Forms.Label labelBidPrice;
        private System.Windows.Forms.Label labelAskPrice;
        private System.Windows.Forms.Label labelBidQty;
        private System.Windows.Forms.Label labelAskQty;
        private System.Windows.Forms.Label labelExpirySeries;
        private System.Windows.Forms.TextBox textBoxBidPrice;
        private System.Windows.Forms.TextBox textBoxAskPrice;
        private System.Windows.Forms.TextBox textBoxBidQty;
        private System.Windows.Forms.TextBox textBoxAskQty;
        private System.Windows.Forms.TextBox textBoxExpirySeries;
    }
}


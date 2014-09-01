namespace UV.TTServices.Tests
{
    partial class TestMarket
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
            this.components = new System.ComponentModel.Container();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonExit = new System.Windows.Forms.Button();
            this.listBoxMarkets = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.listBoxProducts = new System.Windows.Forms.ListBox();
            this.buttonGetProducts = new System.Windows.Forms.Button();
            this.listBoxInstruments = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonGetInstruments = new System.Windows.Forms.Button();
            this.txtBidSide = new System.Windows.Forms.Label();
            this.txtAskSide = new System.Windows.Forms.Label();
            this.checkBoxUseXTraderFollowLogin = new System.Windows.Forms.CheckBox();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.txtBidQty = new System.Windows.Forms.Label();
            this.txtAskQty = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.txtInstrumentName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(12, 12);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 0;
            this.buttonStart.Text = "Login";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.button_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(12, 110);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 1;
            this.buttonStop.Text = "stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.button_Click);
            // 
            // buttonExit
            // 
            this.buttonExit.Location = new System.Drawing.Point(12, 139);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(75, 23);
            this.buttonExit.TabIndex = 2;
            this.buttonExit.Text = "exit";
            this.buttonExit.UseVisualStyleBackColor = true;
            this.buttonExit.Click += new System.EventHandler(this.button_Click);
            // 
            // listBoxMarkets
            // 
            this.listBoxMarkets.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxMarkets.FormattingEnabled = true;
            this.listBoxMarkets.Location = new System.Drawing.Point(97, 28);
            this.listBoxMarkets.Name = "listBoxMarkets";
            this.listBoxMarkets.Size = new System.Drawing.Size(112, 303);
            this.listBoxMarkets.TabIndex = 3;
            this.listBoxMarkets.DoubleClick += new System.EventHandler(this.ListBox_DoubleClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(122, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Markets";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(242, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Products";
            // 
            // listBoxProducts
            // 
            this.listBoxProducts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBoxProducts.FormattingEnabled = true;
            this.listBoxProducts.Location = new System.Drawing.Point(215, 28);
            this.listBoxProducts.Name = "listBoxProducts";
            this.listBoxProducts.Size = new System.Drawing.Size(236, 303);
            this.listBoxProducts.TabIndex = 6;
            this.listBoxProducts.DoubleClick += new System.EventHandler(this.ListBox_DoubleClick);
            // 
            // buttonGetProducts
            // 
            this.buttonGetProducts.Location = new System.Drawing.Point(12, 193);
            this.buttonGetProducts.Name = "buttonGetProducts";
            this.buttonGetProducts.Size = new System.Drawing.Size(74, 23);
            this.buttonGetProducts.TabIndex = 7;
            this.buttonGetProducts.Text = "get products";
            this.buttonGetProducts.UseVisualStyleBackColor = true;
            this.buttonGetProducts.Click += new System.EventHandler(this.button_Click);
            // 
            // listBoxInstruments
            // 
            this.listBoxInstruments.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxInstruments.FormattingEnabled = true;
            this.listBoxInstruments.Location = new System.Drawing.Point(457, 120);
            this.listBoxInstruments.Name = "listBoxInstruments";
            this.listBoxInstruments.Size = new System.Drawing.Size(186, 212);
            this.listBoxInstruments.TabIndex = 8;
            this.listBoxInstruments.SelectedIndexChanged += new System.EventHandler(this.listBoxInstruments_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(488, 104);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Instruments";
            // 
            // buttonGetInstruments
            // 
            this.buttonGetInstruments.Location = new System.Drawing.Point(4, 222);
            this.buttonGetInstruments.Name = "buttonGetInstruments";
            this.buttonGetInstruments.Size = new System.Drawing.Size(91, 23);
            this.buttonGetInstruments.TabIndex = 10;
            this.buttonGetInstruments.Text = "get instruments";
            this.buttonGetInstruments.UseVisualStyleBackColor = true;
            this.buttonGetInstruments.Click += new System.EventHandler(this.button_Click);
            // 
            // txtBidSide
            // 
            this.txtBidSide.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtBidSide.ForeColor = System.Drawing.Color.SteelBlue;
            this.txtBidSide.Location = new System.Drawing.Point(454, 43);
            this.txtBidSide.Name = "txtBidSide";
            this.txtBidSide.Size = new System.Drawing.Size(83, 21);
            this.txtBidSide.TabIndex = 11;
            this.txtBidSide.Text = "market view";
            this.txtBidSide.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtAskSide
            // 
            this.txtAskSide.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAskSide.ForeColor = System.Drawing.Color.Crimson;
            this.txtAskSide.Location = new System.Drawing.Point(529, 43);
            this.txtAskSide.Name = "txtAskSide";
            this.txtAskSide.Size = new System.Drawing.Size(84, 21);
            this.txtAskSide.TabIndex = 12;
            this.txtAskSide.Text = "market view";
            this.txtAskSide.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // checkBoxUseXTraderFollowLogin
            // 
            this.checkBoxUseXTraderFollowLogin.AutoSize = true;
            this.checkBoxUseXTraderFollowLogin.Location = new System.Drawing.Point(2, 41);
            this.checkBoxUseXTraderFollowLogin.Name = "checkBoxUseXTraderFollowLogin";
            this.checkBoxUseXTraderFollowLogin.Size = new System.Drawing.Size(93, 17);
            this.checkBoxUseXTraderFollowLogin.TabIndex = 13;
            this.checkBoxUseXTraderFollowLogin.Text = "XTrader Login";
            this.checkBoxUseXTraderFollowLogin.UseVisualStyleBackColor = true;
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.BackColor = System.Drawing.SystemColors.ButtonShadow;
            this.buttonUpdate.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonUpdate.ForeColor = System.Drawing.SystemColors.Control;
            this.buttonUpdate.Location = new System.Drawing.Point(491, 2);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(75, 23);
            this.buttonUpdate.TabIndex = 14;
            this.buttonUpdate.Text = "update";
            this.buttonUpdate.UseVisualStyleBackColor = false;
            this.buttonUpdate.Click += new System.EventHandler(this.button_Click);
            // 
            // txtBidQty
            // 
            this.txtBidQty.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtBidQty.ForeColor = System.Drawing.Color.SteelBlue;
            this.txtBidQty.Location = new System.Drawing.Point(454, 64);
            this.txtBidQty.Name = "txtBidQty";
            this.txtBidQty.Size = new System.Drawing.Size(83, 21);
            this.txtBidQty.TabIndex = 15;
            this.txtBidQty.Text = "market view";
            this.txtBidQty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtAskQty
            // 
            this.txtAskQty.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAskQty.ForeColor = System.Drawing.Color.Crimson;
            this.txtAskQty.Location = new System.Drawing.Point(529, 64);
            this.txtAskQty.Name = "txtAskQty";
            this.txtAskQty.Size = new System.Drawing.Size(84, 21);
            this.txtAskQty.TabIndex = 16;
            this.txtAskQty.Text = "market view";
            this.txtAskQty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // timer1
            // 
            this.timer1.Interval = 500;
            // 
            // txtInstrumentName
            // 
            this.txtInstrumentName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInstrumentName.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtInstrumentName.Location = new System.Drawing.Point(457, 28);
            this.txtInstrumentName.Name = "txtInstrumentName";
            this.txtInstrumentName.Size = new System.Drawing.Size(186, 20);
            this.txtInstrumentName.TabIndex = 17;
            this.txtInstrumentName.Text = "name";
            this.txtInstrumentName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TestMarket
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(645, 344);
            this.Controls.Add(this.txtInstrumentName);
            this.Controls.Add(this.txtAskQty);
            this.Controls.Add(this.txtBidQty);
            this.Controls.Add(this.buttonUpdate);
            this.Controls.Add(this.checkBoxUseXTraderFollowLogin);
            this.Controls.Add(this.txtAskSide);
            this.Controls.Add(this.txtBidSide);
            this.Controls.Add(this.buttonGetInstruments);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.listBoxInstruments);
            this.Controls.Add(this.buttonGetProducts);
            this.Controls.Add(this.listBoxProducts);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBoxMarkets);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonStart);
            this.Name = "TestMarket";
            this.Text = "TestMarket";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TestMarket_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.ListBox listBoxMarkets;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox listBoxProducts;
        private System.Windows.Forms.Button buttonGetProducts;
        private System.Windows.Forms.ListBox listBoxInstruments;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonGetInstruments;
        private System.Windows.Forms.Label txtBidSide;
        private System.Windows.Forms.Label txtAskSide;
        private System.Windows.Forms.CheckBox checkBoxUseXTraderFollowLogin;
        private System.Windows.Forms.Button buttonUpdate;
        private System.Windows.Forms.Label txtBidQty;
        private System.Windows.Forms.Label txtAskQty;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label txtInstrumentName;
    }
}
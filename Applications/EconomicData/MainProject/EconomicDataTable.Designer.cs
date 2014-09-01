namespace EconomicBloombergProject
{
    partial class EconomicDataTable
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
            this.Column = new System.Windows.Forms.TextBox();
            this.RecentEconomicData = new System.Windows.Forms.TextBox();
            this.LatestEconomicData = new System.Windows.Forms.TextBox();
            this.HistoricalEconomicData = new System.Windows.Forms.TextBox();
            this.FutureEconomicData = new System.Windows.Forms.TextBox();
            this.AllDataCombined = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Column
            // 
            this.Column.Location = new System.Drawing.Point(82, 0);
            this.Column.Name = "Column";
            this.Column.Size = new System.Drawing.Size(1519, 20);
            this.Column.TabIndex = 0;
            // 
            // RecentEconomicData
            // 
            this.RecentEconomicData.Location = new System.Drawing.Point(82, 26);
            this.RecentEconomicData.Multiline = true;
            this.RecentEconomicData.Name = "RecentEconomicData";
            this.RecentEconomicData.Size = new System.Drawing.Size(1519, 125);
            this.RecentEconomicData.TabIndex = 1;
            // 
            // LatestEconomicData
            // 
            this.LatestEconomicData.Location = new System.Drawing.Point(82, 157);
            this.LatestEconomicData.Multiline = true;
            this.LatestEconomicData.Name = "LatestEconomicData";
            this.LatestEconomicData.Size = new System.Drawing.Size(1519, 104);
            this.LatestEconomicData.TabIndex = 2;
            // 
            // HistoricalEconomicData
            // 
            this.HistoricalEconomicData.Location = new System.Drawing.Point(82, 267);
            this.HistoricalEconomicData.Multiline = true;
            this.HistoricalEconomicData.Name = "HistoricalEconomicData";
            this.HistoricalEconomicData.Size = new System.Drawing.Size(1519, 283);
            this.HistoricalEconomicData.TabIndex = 3;
            // 
            // FutureEconomicData
            // 
            this.FutureEconomicData.Location = new System.Drawing.Point(82, 556);
            this.FutureEconomicData.Multiline = true;
            this.FutureEconomicData.Name = "FutureEconomicData";
            this.FutureEconomicData.Size = new System.Drawing.Size(1519, 144);
            this.FutureEconomicData.TabIndex = 4;
            // 
            // AllDataCombined
            // 
            this.AllDataCombined.Location = new System.Drawing.Point(82, 706);
            this.AllDataCombined.Multiline = true;
            this.AllDataCombined.Name = "AllDataCombined";
            this.AllDataCombined.Size = new System.Drawing.Size(1519, 172);
            this.AllDataCombined.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Columns";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(0, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "RecentData";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(0, 157);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "LatestData";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(0, 267);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(73, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "HistoricalData";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(0, 556);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(60, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "FutureData";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(0, 706);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(73, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "AllDataReport";
            // 
            // EconomicDataTable
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1602, 877);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.AllDataCombined);
            this.Controls.Add(this.FutureEconomicData);
            this.Controls.Add(this.HistoricalEconomicData);
            this.Controls.Add(this.LatestEconomicData);
            this.Controls.Add(this.RecentEconomicData);
            this.Controls.Add(this.Column);
            this.Name = "EconomicDataTable";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.TextBox Column;
        public System.Windows.Forms.TextBox RecentEconomicData;
        public System.Windows.Forms.TextBox LatestEconomicData;
        public System.Windows.Forms.TextBox HistoricalEconomicData;
        public System.Windows.Forms.TextBox FutureEconomicData;
        public System.Windows.Forms.TextBox AllDataCombined;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
    }
}


namespace BRE.Tests.DatabaseTest
{
    partial class HedgeOptionsDatabaseWriterTest
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
            this.buttonStart = new System.Windows.Forms.Button();
            this.labelExchangeName = new System.Windows.Forms.Label();
            this.labelProductName = new System.Windows.Forms.Label();
            this.textBoxExchangeName = new System.Windows.Forms.TextBox();
            this.textBoxProductName = new System.Windows.Forms.TextBox();
            this.labelProductType = new System.Windows.Forms.Label();
            this.textBoxProductType = new System.Windows.Forms.TextBox();
            this.buttonWrite = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonStart
            // 
            this.buttonStart.Enabled = false;
            this.buttonStart.Location = new System.Drawing.Point(0, 0);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(123, 52);
            this.buttonStart.TabIndex = 0;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // labelExchangeName
            // 
            this.labelExchangeName.AutoSize = true;
            this.labelExchangeName.Location = new System.Drawing.Point(12, 84);
            this.labelExchangeName.Name = "labelExchangeName";
            this.labelExchangeName.Size = new System.Drawing.Size(86, 13);
            this.labelExchangeName.TabIndex = 1;
            this.labelExchangeName.Text = "Exchange Name";
            // 
            // labelProductName
            // 
            this.labelProductName.AutoSize = true;
            this.labelProductName.Location = new System.Drawing.Point(12, 117);
            this.labelProductName.Name = "labelProductName";
            this.labelProductName.Size = new System.Drawing.Size(75, 13);
            this.labelProductName.TabIndex = 2;
            this.labelProductName.Text = "Product Name";
            // 
            // textBoxExchangeName
            // 
            this.textBoxExchangeName.Location = new System.Drawing.Point(123, 81);
            this.textBoxExchangeName.Name = "textBoxExchangeName";
            this.textBoxExchangeName.Size = new System.Drawing.Size(100, 20);
            this.textBoxExchangeName.TabIndex = 3;
            // 
            // textBoxProductName
            // 
            this.textBoxProductName.Location = new System.Drawing.Point(123, 114);
            this.textBoxProductName.Name = "textBoxProductName";
            this.textBoxProductName.Size = new System.Drawing.Size(100, 20);
            this.textBoxProductName.TabIndex = 4;
            // 
            // labelProductType
            // 
            this.labelProductType.AutoSize = true;
            this.labelProductType.Location = new System.Drawing.Point(12, 152);
            this.labelProductType.Name = "labelProductType";
            this.labelProductType.Size = new System.Drawing.Size(90, 13);
            this.labelProductType.TabIndex = 5;
            this.labelProductType.Text = "labelProductType";
            // 
            // textBoxProductType
            // 
            this.textBoxProductType.Location = new System.Drawing.Point(123, 149);
            this.textBoxProductType.Name = "textBoxProductType";
            this.textBoxProductType.Size = new System.Drawing.Size(100, 20);
            this.textBoxProductType.TabIndex = 6;
            // 
            // buttonWrite
            // 
            this.buttonWrite.Enabled = false;
            this.buttonWrite.Location = new System.Drawing.Point(129, 0);
            this.buttonWrite.Name = "buttonWrite";
            this.buttonWrite.Size = new System.Drawing.Size(123, 52);
            this.buttonWrite.TabIndex = 7;
            this.buttonWrite.Text = "Write";
            this.buttonWrite.UseVisualStyleBackColor = true;
            this.buttonWrite.Click += new System.EventHandler(this.buttonWrite_Click);
            // 
            // HedgeOptionsDatabaseWriterTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(323, 212);
            this.Controls.Add(this.buttonWrite);
            this.Controls.Add(this.textBoxProductType);
            this.Controls.Add(this.labelProductType);
            this.Controls.Add(this.textBoxProductName);
            this.Controls.Add(this.textBoxExchangeName);
            this.Controls.Add(this.labelProductName);
            this.Controls.Add(this.labelExchangeName);
            this.Controls.Add(this.buttonStart);
            this.Name = "HedgeOptionsDatabaseWriterTest";
            this.Text = "HedgeOptionsWriter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Label labelExchangeName;
        private System.Windows.Forms.Label labelProductName;
        private System.Windows.Forms.TextBox textBoxExchangeName;
        private System.Windows.Forms.TextBox textBoxProductName;
        private System.Windows.Forms.Label labelProductType;
        private System.Windows.Forms.TextBox textBoxProductType;
        private System.Windows.Forms.Button buttonWrite;
    }
}
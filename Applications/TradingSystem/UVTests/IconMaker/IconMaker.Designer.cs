namespace UVTests.IconMaker
{
    partial class IconMaker
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
            this.ofdPicture = new System.Windows.Forms.OpenFileDialog();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.txtImagePath = new System.Windows.Forms.Label();
            this.sfdPicture = new System.Windows.Forms.SaveFileDialog();
            this.pbImage = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbImage)).BeginInit();
            this.SuspendLayout();
            // 
            // ofdPicture
            // 
            this.ofdPicture.FileName = "?";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(22, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(110, 32);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.btnOpenImage_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(22, 50);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(110, 32);
            this.button2.TabIndex = 1;
            this.button2.Text = "button2";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.btnSaveAsIcon_Click);
            // 
            // txtImagePath
            // 
            this.txtImagePath.AutoSize = true;
            this.txtImagePath.Location = new System.Drawing.Point(149, 22);
            this.txtImagePath.Name = "txtImagePath";
            this.txtImagePath.Size = new System.Drawing.Size(35, 13);
            this.txtImagePath.TabIndex = 2;
            this.txtImagePath.Text = "label1";
            // 
            // pbImage
            // 
            this.pbImage.Location = new System.Drawing.Point(65, 88);
            this.pbImage.Name = "pbImage";
            this.pbImage.Size = new System.Drawing.Size(157, 150);
            this.pbImage.TabIndex = 3;
            this.pbImage.TabStop = false;
            // 
            // IconMaker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.pbImage);
            this.Controls.Add(this.txtImagePath);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "IconMaker";
            this.Text = "IconMaker";
            ((System.ComponentModel.ISupportInitialize)(this.pbImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog ofdPicture;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label txtImagePath;
        private System.Windows.Forms.SaveFileDialog sfdPicture;
        private System.Windows.Forms.PictureBox pbImage;
    }
}
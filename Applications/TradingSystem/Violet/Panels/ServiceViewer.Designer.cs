namespace UV.Violet.Panels
{
    partial class ServiceViewer
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
            this.listBoxServices = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textServiceType = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.textServiceLocation = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textServiceName = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonLaunchDisplay = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonLaunchLogViewer = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxServices
            // 
            this.listBoxServices.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxServices.FormattingEnabled = true;
            this.listBoxServices.Location = new System.Drawing.Point(5, 14);
            this.listBoxServices.Name = "listBoxServices";
            this.listBoxServices.Size = new System.Drawing.Size(168, 82);
            this.listBoxServices.TabIndex = 0;
            this.listBoxServices.SelectedIndexChanged += new System.EventHandler(this.listBoxServices_SelectionChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.textServiceType);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.textServiceLocation);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.textServiceName);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(188, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(222, 95);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Service Information";
            // 
            // textServiceType
            // 
            this.textServiceType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textServiceType.Location = new System.Drawing.Point(80, 44);
            this.textServiceType.Name = "textServiceType";
            this.textServiceType.Size = new System.Drawing.Size(133, 15);
            this.textServiceType.TabIndex = 5;
            this.textServiceType.Text = "?";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 43);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(34, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Type:";
            // 
            // textServiceLocation
            // 
            this.textServiceLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textServiceLocation.Location = new System.Drawing.Point(80, 30);
            this.textServiceLocation.Name = "textServiceLocation";
            this.textServiceLocation.Size = new System.Drawing.Size(136, 14);
            this.textServiceLocation.TabIndex = 3;
            this.textServiceLocation.Text = "?";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Service Name:";
            // 
            // textServiceName
            // 
            this.textServiceName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textServiceName.Location = new System.Drawing.Point(80, 16);
            this.textServiceName.Name = "textServiceName";
            this.textServiceName.Size = new System.Drawing.Size(133, 14);
            this.textServiceName.TabIndex = 1;
            this.textServiceName.Text = "local";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Location:";
            // 
            // buttonLaunchDisplay
            // 
            this.buttonLaunchDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonLaunchDisplay.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLaunchDisplay.Location = new System.Drawing.Point(4, 99);
            this.buttonLaunchDisplay.Name = "buttonLaunchDisplay";
            this.buttonLaunchDisplay.Size = new System.Drawing.Size(56, 21);
            this.buttonLaunchDisplay.TabIndex = 6;
            this.buttonLaunchDisplay.Text = "display";
            this.buttonLaunchDisplay.UseVisualStyleBackColor = true;
            this.buttonLaunchDisplay.Click += new System.EventHandler(this.Button_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.buttonLaunchLogViewer);
            this.groupBox2.Controls.Add(this.listBoxServices);
            this.groupBox2.Controls.Add(this.buttonLaunchDisplay);
            this.groupBox2.Location = new System.Drawing.Point(3, 5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(179, 124);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Services";
            // 
            // bLaunchLogViewer
            // 
            this.buttonLaunchLogViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonLaunchLogViewer.Font = new System.Drawing.Font("Calibri", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLaunchLogViewer.Location = new System.Drawing.Point(66, 99);
            this.buttonLaunchLogViewer.Name = "bLaunchLogViewer";
            this.buttonLaunchLogViewer.Size = new System.Drawing.Size(56, 21);
            this.buttonLaunchLogViewer.TabIndex = 7;
            this.buttonLaunchLogViewer.Text = "show log";
            this.buttonLaunchLogViewer.UseVisualStyleBackColor = true;
            this.buttonLaunchLogViewer.Click += new System.EventHandler(this.Button_Click);
            // 
            // ServiceViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "ServiceViewer";
            this.Size = new System.Drawing.Size(416, 133);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxServices;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label textServiceLocation;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label textServiceName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label textServiceType;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button buttonLaunchDisplay;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonLaunchLogViewer;
    }
}

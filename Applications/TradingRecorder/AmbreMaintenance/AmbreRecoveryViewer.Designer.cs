namespace AmbreMaintenance
{
    partial class AmbreRecoveryViewer
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
            this.tabControlFillPageViewer = new System.Windows.Forms.TabControl();
            this.buttonPlayAuditTrail = new System.Windows.Forms.Button();
            this.buttonSaveOutput = new System.Windows.Forms.Button();
            this.DropFileStartDateTime = new System.Windows.Forms.DateTimePicker();
            this.labelUserLoginName = new System.Windows.Forms.Label();
            this.textBoxUserName = new System.Windows.Forms.TextBox();
            this.labelFillHubName = new System.Windows.Forms.Label();
            this.textBoxFillHubName = new System.Windows.Forms.TextBox();
            this.labelTitle = new System.Windows.Forms.Label();
            this.buttonLoadDropFile = new System.Windows.Forms.Button();
            this.EndPlayingDateTime = new System.Windows.Forms.DateTimePicker();
            this.buttonExit = new System.Windows.Forms.Button();
            this.labelTTConnection = new System.Windows.Forms.Label();
            this.buttonLoadAuditTrailFills = new System.Windows.Forms.Button();
            this.buttonRecover = new System.Windows.Forms.Button();
            this.DropFileDateTime = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // tabControlFillPageViewer
            // 
            this.tabControlFillPageViewer.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tabControlFillPageViewer.AllowDrop = true;
            this.tabControlFillPageViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlFillPageViewer.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControlFillPageViewer.Location = new System.Drawing.Point(0, 110);
            this.tabControlFillPageViewer.Name = "tabControlFillPageViewer";
            this.tabControlFillPageViewer.SelectedIndex = 0;
            this.tabControlFillPageViewer.Size = new System.Drawing.Size(739, 323);
            this.tabControlFillPageViewer.TabIndex = 0;
            // 
            // buttonPlayAuditTrail
            // 
            this.buttonPlayAuditTrail.Location = new System.Drawing.Point(874, 73);
            this.buttonPlayAuditTrail.Name = "buttonPlayAuditTrail";
            this.buttonPlayAuditTrail.Size = new System.Drawing.Size(125, 23);
            this.buttonPlayAuditTrail.TabIndex = 3;
            this.buttonPlayAuditTrail.Text = "Play Audit Trail Fills";
            this.buttonPlayAuditTrail.UseVisualStyleBackColor = true;
            this.buttonPlayAuditTrail.Visible = false;
            this.buttonPlayAuditTrail.Click += new System.EventHandler(this.buttonPlayAuditTrailFills_Click);
            // 
            // buttonSaveOutput
            // 
            this.buttonSaveOutput.Location = new System.Drawing.Point(462, 43);
            this.buttonSaveOutput.Name = "buttonSaveOutput";
            this.buttonSaveOutput.Size = new System.Drawing.Size(130, 52);
            this.buttonSaveOutput.TabIndex = 5;
            this.buttonSaveOutput.Text = "Save Output";
            this.buttonSaveOutput.UseVisualStyleBackColor = true;
            this.buttonSaveOutput.Click += new System.EventHandler(this.buttonSaveOutput_Click);
            // 
            // DropFileStartDateTime
            // 
            this.DropFileStartDateTime.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.DropFileStartDateTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.DropFileStartDateTime.Location = new System.Drawing.Point(163, 44);
            this.DropFileStartDateTime.Name = "DropFileStartDateTime";
            this.DropFileStartDateTime.Size = new System.Drawing.Size(163, 20);
            this.DropFileStartDateTime.TabIndex = 6;
            this.DropFileStartDateTime.ValueChanged += new System.EventHandler(this.DropFileStartDateTime_ValueChanged);
            // 
            // labelUserLoginName
            // 
            this.labelUserLoginName.AutoSize = true;
            this.labelUserLoginName.Location = new System.Drawing.Point(12, 9);
            this.labelUserLoginName.Name = "labelUserLoginName";
            this.labelUserLoginName.Size = new System.Drawing.Size(57, 13);
            this.labelUserLoginName.TabIndex = 7;
            this.labelUserLoginName.Text = "UserName";
            // 
            // textBoxUserName
            // 
            this.textBoxUserName.Location = new System.Drawing.Point(75, 6);
            this.textBoxUserName.Name = "textBoxUserName";
            this.textBoxUserName.Size = new System.Drawing.Size(124, 20);
            this.textBoxUserName.TabIndex = 8;
            this.textBoxUserName.TextChanged += new System.EventHandler(this.textBoxUserName_TextChanged);
            this.textBoxUserName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxUserName_KeyDown);
            // 
            // labelFillHubName
            // 
            this.labelFillHubName.AutoSize = true;
            this.labelFillHubName.Location = new System.Drawing.Point(205, 9);
            this.labelFillHubName.Name = "labelFillHubName";
            this.labelFillHubName.Size = new System.Drawing.Size(39, 13);
            this.labelFillHubName.TabIndex = 9;
            this.labelFillHubName.Text = "FillHub";
            // 
            // textBoxFillHubName
            // 
            this.textBoxFillHubName.Location = new System.Drawing.Point(250, 6);
            this.textBoxFillHubName.Name = "textBoxFillHubName";
            this.textBoxFillHubName.Size = new System.Drawing.Size(118, 20);
            this.textBoxFillHubName.TabIndex = 10;
            this.textBoxFillHubName.TextChanged += new System.EventHandler(this.textBoxFillHubName_TextChanged);
            this.textBoxFillHubName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxFillHubName_KeyDown);
            // 
            // labelTitle
            // 
            this.labelTitle.AutoSize = true;
            this.labelTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTitle.Location = new System.Drawing.Point(491, 4);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(236, 20);
            this.labelTitle.TabIndex = 11;
            this.labelTitle.Text = "Ambre Fill Hub Recreation Form";
            // 
            // buttonLoadDropFile
            // 
            this.buttonLoadDropFile.Location = new System.Drawing.Point(874, 9);
            this.buttonLoadDropFile.Name = "buttonLoadDropFile";
            this.buttonLoadDropFile.Size = new System.Drawing.Size(125, 23);
            this.buttonLoadDropFile.TabIndex = 12;
            this.buttonLoadDropFile.Text = "Load Drop File";
            this.buttonLoadDropFile.UseVisualStyleBackColor = true;
            this.buttonLoadDropFile.Visible = false;
            this.buttonLoadDropFile.Click += new System.EventHandler(this.buttonLoadDropFile_Click);
            // 
            // EndPlayingDateTime
            // 
            this.EndPlayingDateTime.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.EndPlayingDateTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.EndPlayingDateTime.Location = new System.Drawing.Point(163, 75);
            this.EndPlayingDateTime.Name = "EndPlayingDateTime";
            this.EndPlayingDateTime.Size = new System.Drawing.Size(163, 20);
            this.EndPlayingDateTime.TabIndex = 13;
            this.EndPlayingDateTime.ValueChanged += new System.EventHandler(this.EndPlayingDateTime_ValueChanged);
            // 
            // buttonExit
            // 
            this.buttonExit.Location = new System.Drawing.Point(598, 43);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(129, 51);
            this.buttonExit.TabIndex = 15;
            this.buttonExit.Text = "Exit Program";
            this.buttonExit.UseVisualStyleBackColor = true;
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // labelTTConnection
            // 
            this.labelTTConnection.AutoSize = true;
            this.labelTTConnection.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTTConnection.Location = new System.Drawing.Point(402, 7);
            this.labelTTConnection.Name = "labelTTConnection";
            this.labelTTConnection.Size = new System.Drawing.Size(66, 16);
            this.labelTTConnection.TabIndex = 16;
            this.labelTTConnection.Text = "TT Status";
            // 
            // buttonLoadAuditTrailFills
            // 
            this.buttonLoadAuditTrailFills.Location = new System.Drawing.Point(874, 41);
            this.buttonLoadAuditTrailFills.Name = "buttonLoadAuditTrailFills";
            this.buttonLoadAuditTrailFills.Size = new System.Drawing.Size(125, 23);
            this.buttonLoadAuditTrailFills.TabIndex = 17;
            this.buttonLoadAuditTrailFills.Text = "Load Audit Trail Fills";
            this.buttonLoadAuditTrailFills.UseVisualStyleBackColor = true;
            this.buttonLoadAuditTrailFills.Visible = false;
            this.buttonLoadAuditTrailFills.Click += new System.EventHandler(this.buttonLoadAuditTrailFills_Click);
            // 
            // buttonRecover
            // 
            this.buttonRecover.Location = new System.Drawing.Point(332, 43);
            this.buttonRecover.Name = "buttonRecover";
            this.buttonRecover.Size = new System.Drawing.Size(124, 52);
            this.buttonRecover.TabIndex = 18;
            this.buttonRecover.Text = "Recover Positions";
            this.buttonRecover.UseVisualStyleBackColor = true;
            this.buttonRecover.Click += new System.EventHandler(this.buttonRecover_Click);
            // 
            // DropFileDateTime
            // 
            this.DropFileDateTime.FormattingEnabled = true;
            this.DropFileDateTime.Location = new System.Drawing.Point(15, 43);
            this.DropFileDateTime.Name = "DropFileDateTime";
            this.DropFileDateTime.Size = new System.Drawing.Size(142, 21);
            this.DropFileDateTime.TabIndex = 19;
            this.DropFileDateTime.SelectedIndexChanged += new System.EventHandler(this.DropFileDateTime_SelectedIndexChanged);
            // 
            // AmbreRecoveryViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(739, 433);
            this.Controls.Add(this.DropFileDateTime);
            this.Controls.Add(this.buttonRecover);
            this.Controls.Add(this.buttonLoadAuditTrailFills);
            this.Controls.Add(this.labelTTConnection);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.EndPlayingDateTime);
            this.Controls.Add(this.buttonLoadDropFile);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.textBoxFillHubName);
            this.Controls.Add(this.labelFillHubName);
            this.Controls.Add(this.textBoxUserName);
            this.Controls.Add(this.labelUserLoginName);
            this.Controls.Add(this.DropFileStartDateTime);
            this.Controls.Add(this.buttonSaveOutput);
            this.Controls.Add(this.buttonPlayAuditTrail);
            this.Controls.Add(this.tabControlFillPageViewer);
            this.Name = "AmbreRecoveryViewer";
            this.Text = "AmbreRecoveryForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlFillPageViewer;
        private System.Windows.Forms.Button buttonPlayAuditTrail;
        private System.Windows.Forms.Button buttonSaveOutput;
        private System.Windows.Forms.DateTimePicker DropFileStartDateTime;
        private System.Windows.Forms.Label labelUserLoginName;
        private System.Windows.Forms.TextBox textBoxUserName;
        private System.Windows.Forms.Label labelFillHubName;
        private System.Windows.Forms.TextBox textBoxFillHubName;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Button buttonLoadDropFile;
        private System.Windows.Forms.DateTimePicker EndPlayingDateTime;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Label labelTTConnection;
        private System.Windows.Forms.Button buttonLoadAuditTrailFills;
        private System.Windows.Forms.Button buttonRecover;
        private System.Windows.Forms.ComboBox DropFileDateTime;
    }
}


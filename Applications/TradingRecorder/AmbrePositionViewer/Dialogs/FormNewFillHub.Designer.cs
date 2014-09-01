namespace Ambre.PositionViewer.Dialogs
{
    partial class FormNewFillHub
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
            this.label1 = new System.Windows.Forms.Label();
            this.groupBoxFillListenFilters = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxFilterString = new System.Windows.Forms.TextBox();
            this.radioFilter3 = new System.Windows.Forms.RadioButton();
            this.radioFilterAcctNumber = new System.Windows.Forms.RadioButton();
            this.radioFilter1 = new System.Windows.Forms.RadioButton();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.textBoxHubName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonCreateNewHub = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.textBoxResetTime = new System.Windows.Forms.TextBox();
            this.groupBoxFillListenFilters.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoEllipsis = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(2, 1);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(228, 50);
            this.label1.TabIndex = 0;
            this.label1.Text = "Choose desired options for new fill manager, and click \"Create.\"";
            // 
            // groupBoxFillListenFilters
            // 
            this.groupBoxFillListenFilters.Controls.Add(this.label2);
            this.groupBoxFillListenFilters.Controls.Add(this.textBoxFilterString);
            this.groupBoxFillListenFilters.Controls.Add(this.radioFilter3);
            this.groupBoxFillListenFilters.Controls.Add(this.radioFilterAcctNumber);
            this.groupBoxFillListenFilters.Controls.Add(this.radioFilter1);
            this.groupBoxFillListenFilters.Location = new System.Drawing.Point(3, 56);
            this.groupBoxFillListenFilters.Name = "groupBoxFillListenFilters";
            this.groupBoxFillListenFilters.Size = new System.Drawing.Size(127, 118);
            this.groupBoxFillListenFilters.TabIndex = 1;
            this.groupBoxFillListenFilters.TabStop = false;
            this.groupBoxFillListenFilters.Text = "Filter fills";
            this.toolTip1.SetToolTip(this.groupBoxFillListenFilters, "New fill manager will only subscribe to fills satisfying one of these filters.");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 75);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "filter value:";
            this.toolTip1.SetToolTip(this.label2, "Enter the filter string in box.");
            // 
            // textBoxFilterString
            // 
            this.textBoxFilterString.Location = new System.Drawing.Point(6, 92);
            this.textBoxFilterString.Name = "textBoxFilterString";
            this.textBoxFilterString.Size = new System.Drawing.Size(113, 20);
            this.textBoxFilterString.TabIndex = 3;
            // 
            // radioFilter3
            // 
            this.radioFilter3.AutoSize = true;
            this.radioFilter3.Location = new System.Drawing.Point(6, 53);
            this.radioFilter3.Name = "radioFilter3";
            this.radioFilter3.Size = new System.Drawing.Size(103, 17);
            this.radioFilter3.TabIndex = 2;
            this.radioFilter3.Text = "Filter by instr key";
            this.radioFilter3.UseVisualStyleBackColor = true;
            this.radioFilter3.CheckedChanged += new System.EventHandler(this.Radio_CheckChanged);
            this.radioFilter3.Click += new System.EventHandler(this.Radio_Click);
            // 
            // radioFilterAcctNumber
            // 
            this.radioFilterAcctNumber.AutoSize = true;
            this.radioFilterAcctNumber.Location = new System.Drawing.Point(6, 36);
            this.radioFilterAcctNumber.Name = "radioFilterAcctNumber";
            this.radioFilterAcctNumber.Size = new System.Drawing.Size(113, 17);
            this.radioFilterAcctNumber.TabIndex = 1;
            this.radioFilterAcctNumber.Text = "Filter by account #";
            this.radioFilterAcctNumber.UseVisualStyleBackColor = true;
            this.radioFilterAcctNumber.CheckedChanged += new System.EventHandler(this.Radio_CheckChanged);
            this.radioFilterAcctNumber.Click += new System.EventHandler(this.Radio_Click);
            // 
            // radioFilter1
            // 
            this.radioFilter1.AutoSize = true;
            this.radioFilter1.Checked = true;
            this.radioFilter1.Location = new System.Drawing.Point(6, 19);
            this.radioFilter1.Name = "radioFilter1";
            this.radioFilter1.Size = new System.Drawing.Size(95, 17);
            this.radioFilter1.TabIndex = 0;
            this.radioFilter1.TabStop = true;
            this.radioFilter1.Text = "Listen to all fills";
            this.radioFilter1.UseVisualStyleBackColor = true;
            this.radioFilter1.CheckedChanged += new System.EventHandler(this.Radio_CheckChanged);
            this.radioFilter1.Click += new System.EventHandler(this.Radio_Click);
            // 
            // textBoxHubName
            // 
            this.textBoxHubName.Location = new System.Drawing.Point(5, 191);
            this.textBoxHubName.Name = "textBoxHubName";
            this.textBoxHubName.Size = new System.Drawing.Size(126, 20);
            this.textBoxHubName.TabIndex = 4;
            this.toolTip1.SetToolTip(this.textBoxHubName, "Enter manager name.  If acct# filter is used, name will be acct#.");
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 176);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(105, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "New manager name:";
            this.toolTip1.SetToolTip(this.label3, "Each manager needs a unique name.  Enter one below.");
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(2, 226);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Reset time:";
            this.toolTip1.SetToolTip(this.label4, "Each day, the manager is reset at this time.");
            // 
            // buttonCreateNewHub
            // 
            this.buttonCreateNewHub.Location = new System.Drawing.Point(144, 151);
            this.buttonCreateNewHub.Name = "buttonCreateNewHub";
            this.buttonCreateNewHub.Size = new System.Drawing.Size(75, 23);
            this.buttonCreateNewHub.TabIndex = 8;
            this.buttonCreateNewHub.Text = "Create";
            this.toolTip1.SetToolTip(this.buttonCreateNewHub, "Submit request to create new fill manager and closes dialog.");
            this.buttonCreateNewHub.UseVisualStyleBackColor = true;
            this.buttonCreateNewHub.Click += new System.EventHandler(this.Button_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(144, 191);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 9;
            this.buttonCancel.Text = "Cancel";
            this.toolTip1.SetToolTip(this.buttonCancel, "Cancels and closes window.");
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.Button_Click);
            // 
            // textBoxResetTime
            // 
            this.textBoxResetTime.Location = new System.Drawing.Point(68, 226);
            this.textBoxResetTime.Name = "textBoxResetTime";
            this.textBoxResetTime.Size = new System.Drawing.Size(64, 20);
            this.textBoxResetTime.TabIndex = 6;
            this.textBoxResetTime.Text = "4:30 PM";
            this.textBoxResetTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBoxResetTime.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TextBox_KeyUp);
            // 
            // FormNewFillHub
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(231, 260);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonCreateNewHub);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxResetTime);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxHubName);
            this.Controls.Add(this.groupBoxFillListenFilters);
            this.Controls.Add(this.label1);
            this.Name = "FormNewFillHub";
            this.Text = "Create Fill Manager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormNewFillHub_FormClosing);
            this.groupBoxFillListenFilters.ResumeLayout(false);
            this.groupBoxFillListenFilters.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBoxFillListenFilters;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxFilterString;
        private System.Windows.Forms.RadioButton radioFilter3;
        private System.Windows.Forms.RadioButton radioFilterAcctNumber;
        private System.Windows.Forms.RadioButton radioFilter1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.TextBox textBoxHubName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxResetTime;
        private System.Windows.Forms.Button buttonCreateNewHub;
        private System.Windows.Forms.Button buttonCancel;
    }
}
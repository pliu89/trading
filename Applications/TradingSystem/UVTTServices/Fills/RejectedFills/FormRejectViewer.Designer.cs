namespace UV.TTServices.Fills.RejectedFills
{
    partial class FormRejectViewer
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxRejectionMessage = new System.Windows.Forms.TextBox();
            this.textReason = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textInstrumentName = new System.Windows.Forms.Label();
            this.textDateTimeLocal = new System.Windows.Forms.Label();
            this.textDateTimeExchange = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxFillDetails = new System.Windows.Forms.TextBox();
            this.buttonAcceptFill = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 65);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Rejection reason:";
            // 
            // textBoxRejectionMessage
            // 
            this.textBoxRejectionMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxRejectionMessage.Location = new System.Drawing.Point(4, 100);
            this.textBoxRejectionMessage.Multiline = true;
            this.textBoxRejectionMessage.Name = "textBoxRejectionMessage";
            this.textBoxRejectionMessage.ReadOnly = true;
            this.textBoxRejectionMessage.Size = new System.Drawing.Size(696, 38);
            this.textBoxRejectionMessage.TabIndex = 2;
            // 
            // textReason
            // 
            this.textReason.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textReason.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.textReason.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textReason.ForeColor = System.Drawing.Color.Red;
            this.textReason.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.textReason.Location = new System.Drawing.Point(4, 79);
            this.textReason.Name = "textReason";
            this.textReason.Size = new System.Drawing.Size(150, 20);
            this.textReason.TabIndex = 3;
            this.textReason.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textInstrumentName);
            this.groupBox1.Controls.Add(this.textDateTimeLocal);
            this.groupBox1.Controls.Add(this.textDateTimeExchange);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.textBoxFillDetails);
            this.groupBox1.Location = new System.Drawing.Point(162, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(225, 91);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "details";
            // 
            // textInstrumentName
            // 
            this.textInstrumentName.Location = new System.Drawing.Point(6, 19);
            this.textInstrumentName.Name = "textInstrumentName";
            this.textInstrumentName.Size = new System.Drawing.Size(216, 13);
            this.textInstrumentName.TabIndex = 8;
            // 
            // textDateTimeLocal
            // 
            this.textDateTimeLocal.AutoSize = true;
            this.textDateTimeLocal.Location = new System.Drawing.Point(58, 60);
            this.textDateTimeLocal.Name = "textDateTimeLocal";
            this.textDateTimeLocal.Size = new System.Drawing.Size(160, 13);
            this.textDateTimeLocal.TabIndex = 7;
            this.textDateTimeLocal.Text = "2013-03-05 08:24:40.794 -06:00";
            // 
            // textDateTimeExchange
            // 
            this.textDateTimeExchange.AutoSize = true;
            this.textDateTimeExchange.Location = new System.Drawing.Point(58, 74);
            this.textDateTimeExchange.Name = "textDateTimeExchange";
            this.textDateTimeExchange.Size = new System.Drawing.Size(160, 13);
            this.textDateTimeExchange.TabIndex = 6;
            this.textDateTimeExchange.Text = "2013-03-05 08:24:40.794 -06:00";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 74);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(57, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "exchange:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(29, 60);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "local:";
            // 
            // textBoxFillDetails
            // 
            this.textBoxFillDetails.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFillDetails.Location = new System.Drawing.Point(6, 37);
            this.textBoxFillDetails.Name = "textBoxFillDetails";
            this.textBoxFillDetails.ReadOnly = true;
            this.textBoxFillDetails.Size = new System.Drawing.Size(213, 20);
            this.textBoxFillDetails.TabIndex = 3;
            this.textBoxFillDetails.Text = "+23 @ 9998.5";
            this.textBoxFillDetails.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // buttonAcceptFill
            // 
            this.buttonAcceptFill.Location = new System.Drawing.Point(393, 71);
            this.buttonAcceptFill.Name = "buttonAcceptFill";
            this.buttonAcceptFill.Size = new System.Drawing.Size(75, 23);
            this.buttonAcceptFill.TabIndex = 6;
            this.buttonAcceptFill.Text = "accept fill";
            this.buttonAcceptFill.UseVisualStyleBackColor = true;
            this.buttonAcceptFill.Click += new System.EventHandler(this.Button_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = true;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataGridView1.Location = new System.Drawing.Point(6, 144);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.Size = new System.Drawing.Size(694, 116);
            this.dataGridView1.TabIndex = 8;
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellClick);
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            this.dataGridView1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dataGridView1_MouseUp);
            // 
            // FormRejectViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(703, 263);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.buttonAcceptFill);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textReason);
            this.Controls.Add(this.textBoxRejectionMessage);
            this.Controls.Add(this.label1);
            this.MinimumSize = new System.Drawing.Size(406, 38);
            this.Name = "FormRejectViewer";
            this.Text = "Ambre: Rejected Fills";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form_Closing);
            this.Load += new System.EventHandler(this.Form_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxRejectionMessage;
        private System.Windows.Forms.Label textReason;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBoxFillDetails;
        private System.Windows.Forms.Label textDateTimeLocal;
        private System.Windows.Forms.Label textDateTimeExchange;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonAcceptFill;
        private System.Windows.Forms.Label textInstrumentName;
        private System.Windows.Forms.DataGridView dataGridView1;
    }
}
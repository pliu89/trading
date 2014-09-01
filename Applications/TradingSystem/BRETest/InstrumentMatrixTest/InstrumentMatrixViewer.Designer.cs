namespace BRE.Tests.InstrumentMatrixTest
{
    partial class InstrumentMatrixViewer
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
            this.dataGridViewInstrumentMatrix = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewInstrumentMatrix)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewInstrumentMatrix
            // 
            this.dataGridViewInstrumentMatrix.AllowUserToAddRows = false;
            this.dataGridViewInstrumentMatrix.AllowUserToDeleteRows = false;
            this.dataGridViewInstrumentMatrix.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewInstrumentMatrix.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewInstrumentMatrix.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewInstrumentMatrix.Name = "dataGridViewInstrumentMatrix";
            this.dataGridViewInstrumentMatrix.ReadOnly = true;
            this.dataGridViewInstrumentMatrix.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            this.dataGridViewInstrumentMatrix.Size = new System.Drawing.Size(875, 589);
            this.dataGridViewInstrumentMatrix.TabIndex = 0;
            // 
            // InstrumentMatrixViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(875, 589);
            this.Controls.Add(this.dataGridViewInstrumentMatrix);
            this.Name = "InstrumentMatrixViewer";
            this.Text = "InstrumentMatrixViewer";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.InstrumentMatrixViewer_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewInstrumentMatrix)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewInstrumentMatrix;
    }
}
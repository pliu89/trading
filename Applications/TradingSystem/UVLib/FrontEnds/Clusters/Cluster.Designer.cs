namespace UV.Lib.FrontEnds.Clusters
{
    partial class Cluster
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
            this.components = new System.ComponentModel.Container();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItemTickSize = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBoxTickSize = new System.Windows.Forms.ToolStripTextBox();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemTickSize});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(153, 48);
            // 
            // toolStripMenuItemTickSize
            // 
            this.toolStripMenuItemTickSize.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBoxTickSize});
            this.toolStripMenuItemTickSize.Name = "toolStripMenuItemTickSize";
            this.toolStripMenuItemTickSize.Size = new System.Drawing.Size(160, 22);
            this.toolStripMenuItemTickSize.Text = "Tick size";
            // 
            // toolStripTextBoxTickSize
            // 
            this.toolStripTextBoxTickSize.Name = "toolStripTextBoxTickSize";
            this.toolStripTextBoxTickSize.Size = new System.Drawing.Size(100, 23);
            this.toolStripTextBoxTickSize.KeyUp += new System.Windows.Forms.KeyEventHandler(this.toolStripTextBoxTickSize_KeyUp);
            // 
            // Cluster
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(25)))), ((int)(((byte)(70)))));
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.White;
            this.Name = "Cluster";
            this.Size = new System.Drawing.Size(94, 27);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemTickSize;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxTickSize;

    }
}


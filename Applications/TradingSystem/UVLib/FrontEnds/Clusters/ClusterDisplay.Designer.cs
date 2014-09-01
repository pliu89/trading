namespace UV.Lib.FrontEnds.Clusters
{
    partial class ClusterDisplay
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.windowsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemNewGraphWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemSaveStrategies = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.windowsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(0);
            this.menuStrip1.Size = new System.Drawing.Size(372, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // windowsToolStripMenuItem
            // 
            this.windowsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuItemNewGraphWindow,
            this.menuItemSaveStrategies});
            this.windowsToolStripMenuItem.Name = "windowsToolStripMenuItem";
            this.windowsToolStripMenuItem.Size = new System.Drawing.Size(37, 24);
            this.windowsToolStripMenuItem.Text = "File";
            // 
            // menuItemNewGraphWindow
            // 
            this.menuItemNewGraphWindow.Name = "menuItemNewGraphWindow";
            this.menuItemNewGraphWindow.Size = new System.Drawing.Size(179, 22);
            this.menuItemNewGraphWindow.Text = "New Graph Window";
            this.menuItemNewGraphWindow.Click += new System.EventHandler(this.NewGraphWindow_Click);
            // 
            // menuItemSaveStrategies
            // 
            this.menuItemSaveStrategies.Name = "menuItemSaveStrategies";
            this.menuItemSaveStrategies.Size = new System.Drawing.Size(179, 22);
            this.menuItemSaveStrategies.Text = "Save Strategies";
            this.menuItemSaveStrategies.Click += new System.EventHandler(this.MenuItem_CLick);
            // 
            // ClusterDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(20)))), ((int)(((byte)(60)))));
            this.ClientSize = new System.Drawing.Size(372, 121);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ClusterDisplay";
            this.Text = "Clusters";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ClusterDisplay_FormClosing);
            this.Load += new System.EventHandler(this.ClusterDisplay_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem windowsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuItemNewGraphWindow;
        private System.Windows.Forms.ToolStripMenuItem menuItemSaveStrategies;
    }
}
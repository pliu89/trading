namespace Ambre.PositionViewer
{
    partial class FormPositionViewer
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRefresh = new System.Windows.Forms.ToolStripMenuItem();
            this.menuResetPnL = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDropFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFinalizeSession = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemConnectToExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.windowsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fillCatalogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFillsRejected = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuItemWindowsShowLog = new System.Windows.Forms.ToolStripMenuItem();
            this.showMarketLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.activeGrid1 = new SKACERO.ActiveGrid(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageAll = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageAll.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.AutoSize = false;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.windowsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.menuStrip1.Size = new System.Drawing.Size(693, 20);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuRefresh,
            this.menuResetPnL,
            this.menuDropFile,
            this.menuFinalizeSession,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // menuRefresh
            // 
            this.menuRefresh.Name = "menuRefresh";
            this.menuRefresh.Size = new System.Drawing.Size(161, 22);
            this.menuRefresh.Text = "Refresh";
            this.menuRefresh.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuResetPnL
            // 
            this.menuResetPnL.Name = "menuResetPnL";
            this.menuResetPnL.Size = new System.Drawing.Size(161, 22);
            this.menuResetPnL.Text = "Reset Real PnL";
            this.menuResetPnL.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuDropFile
            // 
            this.menuDropFile.Name = "menuDropFile";
            this.menuDropFile.Size = new System.Drawing.Size(161, 22);
            this.menuDropFile.Text = "Archive drop file";
            this.menuDropFile.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuFinalizeSession
            // 
            this.menuFinalizeSession.Name = "menuFinalizeSession";
            this.menuFinalizeSession.Size = new System.Drawing.Size(161, 22);
            this.menuFinalizeSession.Text = "Finalize Session";
            this.menuFinalizeSession.Click += new System.EventHandler(this.Menu_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(158, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.Menu_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemConnectToExcel});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // toolStripMenuItemConnectToExcel
            // 
            this.toolStripMenuItemConnectToExcel.Name = "toolStripMenuItemConnectToExcel";
            this.toolStripMenuItemConnectToExcel.Size = new System.Drawing.Size(148, 22);
            this.toolStripMenuItemConnectToExcel.Text = "Excel Connect";
            this.toolStripMenuItemConnectToExcel.Click += new System.EventHandler(this.Menu_Click);
            // 
            // windowsToolStripMenuItem
            // 
            this.windowsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fillCatalogToolStripMenuItem,
            this.menuFillsRejected,
            this.toolStripSeparator1,
            this.menuItemWindowsShowLog,
            this.showMarketLogToolStripMenuItem});
            this.windowsToolStripMenuItem.Name = "windowsToolStripMenuItem";
            this.windowsToolStripMenuItem.Size = new System.Drawing.Size(68, 20);
            this.windowsToolStripMenuItem.Text = "Windows";
            // 
            // fillCatalogToolStripMenuItem
            // 
            this.fillCatalogToolStripMenuItem.Name = "fillCatalogToolStripMenuItem";
            this.fillCatalogToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.fillCatalogToolStripMenuItem.Text = "Fill Catalog";
            this.fillCatalogToolStripMenuItem.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuFillsRejected
            // 
            this.menuFillsRejected.Name = "menuFillsRejected";
            this.menuFillsRejected.Size = new System.Drawing.Size(166, 22);
            this.menuFillsRejected.Text = "Fills Rejected";
            this.menuFillsRejected.Click += new System.EventHandler(this.Menu_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(163, 6);
            // 
            // menuItemWindowsShowLog
            // 
            this.menuItemWindowsShowLog.Name = "menuItemWindowsShowLog";
            this.menuItemWindowsShowLog.Size = new System.Drawing.Size(166, 22);
            this.menuItemWindowsShowLog.Text = "Show Fill Log";
            this.menuItemWindowsShowLog.Click += new System.EventHandler(this.Menu_Click);
            // 
            // showMarketLogToolStripMenuItem
            // 
            this.showMarketLogToolStripMenuItem.Name = "showMarketLogToolStripMenuItem";
            this.showMarketLogToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.showMarketLogToolStripMenuItem.Text = "Show Market Log";
            this.showMarketLogToolStripMenuItem.Click += new System.EventHandler(this.Menu_Click);
            // 
            // activeGrid1
            // 
            this.activeGrid1.AllowFlashing = false;
            this.activeGrid1.AlternatingBackColor = System.Drawing.Color.Gainsboro;
            this.activeGrid1.AlternatingGradientEndColor = System.Drawing.Color.White;
            this.activeGrid1.AlternatingGradientStartColor = System.Drawing.Color.White;
            this.activeGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.activeGrid1.FlashBackColor = System.Drawing.Color.Yellow;
            this.activeGrid1.FlashDuration = 2000;
            this.activeGrid1.FlashFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.activeGrid1.FlashForeColor = System.Drawing.Color.Black;
            this.activeGrid1.FlashGradientEndColor = System.Drawing.Color.White;
            this.activeGrid1.FlashGradientStartColor = System.Drawing.Color.White;
            this.activeGrid1.ForeColorNegativeValues = System.Drawing.Color.Red;
            this.activeGrid1.GroupIndex = 0;
            this.activeGrid1.Location = new System.Drawing.Point(-2, -2);
            this.activeGrid1.Name = "activeGrid1";
            this.activeGrid1.OwnerDraw = true;
            this.activeGrid1.Size = new System.Drawing.Size(688, 411);
            this.activeGrid1.TabIndex = 2;
            this.activeGrid1.UseCompatibleStateImageBehavior = false;
            this.activeGrid1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.activeGrid1_MouseDown);
            this.activeGrid1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.activeGrid1_MouseUp);
            // 
            // tabControl1
            // 
            this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPageAll);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 23);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.Padding = new System.Drawing.Point(6, 2);
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(693, 455);
            this.tabControl1.TabIndex = 3;
            // 
            // tabPageAll
            // 
            this.tabPageAll.Controls.Add(this.activeGrid1);
            this.tabPageAll.Location = new System.Drawing.Point(4, 4);
            this.tabPageAll.Name = "tabPageAll";
            this.tabPageAll.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAll.Size = new System.Drawing.Size(685, 431);
            this.tabPageAll.TabIndex = 0;
            this.tabPageAll.Text = "All";
            this.tabPageAll.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 4);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(685, 431);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2"; 
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // FormPositionViewer
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(693, 478);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormPositionViewer";
            this.Text = "Ambre Position";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Click += new System.EventHandler(this.Form_Click);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.Form1_DragOver);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageAll.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private SKACERO.ActiveGrid activeGrid1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem windowsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fillCatalogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuItemWindowsShowLog;
        private System.Windows.Forms.ToolStripMenuItem showMarketLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem menuResetPnL;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemConnectToExcel;
        private System.Windows.Forms.ToolStripMenuItem menuDropFile;
        private System.Windows.Forms.ToolStripMenuItem menuFinalizeSession;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem menuRefresh;
        private System.Windows.Forms.ToolStripMenuItem menuFillsRejected;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageAll;
        private System.Windows.Forms.TabPage tabPage2;
    }
}


namespace Ambre.PositionViewer.FrontEnds
{
    partial class AmbreViewer
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemArchiveBooks = new System.Windows.Forms.ToolStripMenuItem();
            this.menuResetDailyPnL = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemSaveLogs = new System.Windows.Forms.ToolStripMenuItem();
            this.menuUpdateConfig = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.menuItemExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuWindows = new System.Windows.Forms.ToolStripMenuItem();
            this.menuNewFillHub = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDeleteFillManager = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOpenPnLManager = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemNewTalkerHub = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDeleteBrettTalker = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuItemRejectedFills = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemMarketLog = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemFillHubLog = new System.Windows.Forms.ToolStripMenuItem();
            this.menuViewTalkerLogs = new System.Windows.Forms.ToolStripMenuItem();
            this.menuConnections = new System.Windows.Forms.ToolStripMenuItem();
            this.textExcelLinkWarning = new System.Windows.Forms.Label();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.textApiLoginName = new System.Windows.Forms.Label();
            this.menuItemCreateCashInstrument = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabPage1);
            this.tabControl.Location = new System.Drawing.Point(0, 31);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(814, 430);
            this.tabControl.TabIndex = 0;
            this.tabControl.Selected += new System.Windows.Forms.TabControlEventHandler(this.TabControl_Selected);
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 4);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(806, 404);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile,
            this.menuWindows,
            this.menuConnections});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.MinimumSize = new System.Drawing.Size(0, 28);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(813, 28);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuFile
            // 
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuItemArchiveBooks,
            this.menuResetDailyPnL,
            this.menuItemSaveLogs,
            this.menuUpdateConfig,
            this.toolStripSeparator1,
            this.menuItemExit});
            this.menuFile.Name = "menuFile";
            this.menuFile.Size = new System.Drawing.Size(35, 24);
            this.menuFile.Text = "File";
            // 
            // menuItemArchiveBooks
            // 
            this.menuItemArchiveBooks.Name = "menuItemArchiveBooks";
            this.menuItemArchiveBooks.Size = new System.Drawing.Size(173, 22);
            this.menuItemArchiveBooks.Text = "Archive all books";
            this.menuItemArchiveBooks.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuResetDailyPnL
            // 
            this.menuResetDailyPnL.Name = "menuResetDailyPnL";
            this.menuResetDailyPnL.Size = new System.Drawing.Size(173, 22);
            this.menuResetDailyPnL.Text = "Reset daily book PnL";
            this.menuResetDailyPnL.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuItemSaveLogs
            // 
            this.menuItemSaveLogs.Name = "menuItemSaveLogs";
            this.menuItemSaveLogs.Size = new System.Drawing.Size(173, 22);
            this.menuItemSaveLogs.Text = "Save Log File";
            this.menuItemSaveLogs.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuUpdateConfig
            // 
            this.menuUpdateConfig.Name = "menuUpdateConfig";
            this.menuUpdateConfig.Size = new System.Drawing.Size(173, 22);
            this.menuUpdateConfig.Text = "Update config file";
            this.menuUpdateConfig.Click += new System.EventHandler(this.Menu_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(170, 6);
            // 
            // menuItemExit
            // 
            this.menuItemExit.Name = "menuItemExit";
            this.menuItemExit.Size = new System.Drawing.Size(173, 22);
            this.menuItemExit.Text = "Exit";
            this.menuItemExit.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuWindows
            // 
            this.menuWindows.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuItemCreateCashInstrument,
            this.menuNewFillHub,
            this.menuDeleteFillManager,
            this.menuOpenPnLManager,
            this.menuItemNewTalkerHub,
            this.menuDeleteBrettTalker,
            this.toolStripSeparator2,
            this.menuItemRejectedFills,
            this.menuItemMarketLog,
            this.menuItemFillHubLog,
            this.menuViewTalkerLogs});
            this.menuWindows.Name = "menuWindows";
            this.menuWindows.Size = new System.Drawing.Size(62, 24);
            this.menuWindows.Text = "Windows";
            // 
            // menuNewFillHub
            // 
            this.menuNewFillHub.Name = "menuNewFillHub";
            this.menuNewFillHub.Size = new System.Drawing.Size(185, 22);
            this.menuNewFillHub.Text = "Create Fill Manager...";
            this.menuNewFillHub.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuDeleteFillManager
            // 
            this.menuDeleteFillManager.Name = "menuDeleteFillManager";
            this.menuDeleteFillManager.Size = new System.Drawing.Size(185, 22);
            this.menuDeleteFillManager.Text = "Destroy Fill Manager...";
            this.menuDeleteFillManager.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuOpenPnLManager
            // 
            this.menuOpenPnLManager.Name = "menuOpenPnLManager";
            this.menuOpenPnLManager.Size = new System.Drawing.Size(185, 22);
            this.menuOpenPnLManager.Text = "Open PnL Manager...";
            this.menuOpenPnLManager.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuItemNewTalkerHub
            // 
            this.menuItemNewTalkerHub.Name = "menuItemNewTalkerHub";
            this.menuItemNewTalkerHub.Size = new System.Drawing.Size(185, 22);
            this.menuItemNewTalkerHub.Text = "Create Brett Talker";
            this.menuItemNewTalkerHub.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuDeleteBrettTalker
            // 
            this.menuDeleteBrettTalker.Name = "menuDeleteBrettTalker";
            this.menuDeleteBrettTalker.Size = new System.Drawing.Size(185, 22);
            this.menuDeleteBrettTalker.Text = "Delete a Brett Talker...";
            this.menuDeleteBrettTalker.Click += new System.EventHandler(this.Menu_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(182, 6);
            // 
            // menuItemRejectedFills
            // 
            this.menuItemRejectedFills.Name = "menuItemRejectedFills";
            this.menuItemRejectedFills.Size = new System.Drawing.Size(185, 22);
            this.menuItemRejectedFills.Text = "View Rejected Fills...";
            this.menuItemRejectedFills.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuItemMarketLog
            // 
            this.menuItemMarketLog.Name = "menuItemMarketLog";
            this.menuItemMarketLog.Size = new System.Drawing.Size(185, 22);
            this.menuItemMarketLog.Text = "View Market Logs...";
            this.menuItemMarketLog.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuItemFillHubLog
            // 
            this.menuItemFillHubLog.Name = "menuItemFillHubLog";
            this.menuItemFillHubLog.Size = new System.Drawing.Size(185, 22);
            this.menuItemFillHubLog.Text = "View Fill Logs...";
            this.menuItemFillHubLog.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuViewTalkerLogs
            // 
            this.menuViewTalkerLogs.Name = "menuViewTalkerLogs";
            this.menuViewTalkerLogs.Size = new System.Drawing.Size(185, 22);
            this.menuViewTalkerLogs.Text = "View Brett Logs...";
            this.menuViewTalkerLogs.Click += new System.EventHandler(this.Menu_Click);
            // 
            // menuConnections
            // 
            this.menuConnections.Name = "menuConnections";
            this.menuConnections.Size = new System.Drawing.Size(78, 24);
            this.menuConnections.Text = "Connections";
            // 
            // textExcelLinkWarning
            // 
            this.textExcelLinkWarning.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textExcelLinkWarning.BackColor = System.Drawing.Color.Red;
            this.textExcelLinkWarning.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textExcelLinkWarning.ForeColor = System.Drawing.Color.LightGoldenrodYellow;
            this.textExcelLinkWarning.Location = new System.Drawing.Point(735, 1);
            this.textExcelLinkWarning.Name = "textExcelLinkWarning";
            this.textExcelLinkWarning.Size = new System.Drawing.Size(74, 12);
            this.textExcelLinkWarning.TabIndex = 3;
            this.textExcelLinkWarning.Text = "Excel Link";
            this.textExcelLinkWarning.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.textExcelLinkWarning.Click += new System.EventHandler(this.buttonExcelLink_Click);
            // 
            // notifyIcon
            // 
            this.notifyIcon.Text = "notifyIcon1";
            this.notifyIcon.Visible = true;
            // 
            // textApiLoginName
            // 
            this.textApiLoginName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textApiLoginName.BackColor = System.Drawing.Color.Red;
            this.textApiLoginName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textApiLoginName.ForeColor = System.Drawing.Color.LightGoldenrodYellow;
            this.textApiLoginName.Location = new System.Drawing.Point(735, 16);
            this.textApiLoginName.Name = "textApiLoginName";
            this.textApiLoginName.Size = new System.Drawing.Size(74, 12);
            this.textApiLoginName.TabIndex = 4;
            this.textApiLoginName.Text = "no xtrader";
            this.textApiLoginName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // menuItemCreateCashInstrument
            // 
            this.menuItemCreateCashInstrument.Name = "menuItemCreateCashInstrument";
            this.menuItemCreateCashInstrument.Size = new System.Drawing.Size(185, 22);
            this.menuItemCreateCashInstrument.Text = "Create New Cash Book";
            this.menuItemCreateCashInstrument.Click += new System.EventHandler(this.Menu_Click);
            // 
            // AmbreViewer
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(813, 462);
            this.Controls.Add(this.textApiLoginName);
            this.Controls.Add(this.textExcelLinkWarning);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "AmbreViewer";
            this.Text = "Ambre";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AmbreViewer_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.AmbreViewer_FormClosed);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.Form1_DragOver);
            this.Resize += new System.EventHandler(this.Form_Resize);
            this.tabControl.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.Label textExcelLinkWarning;
        private System.Windows.Forms.ToolStripMenuItem menuFile;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem menuItemExit;
        private System.Windows.Forms.ToolStripMenuItem menuWindows;
        private System.Windows.Forms.ToolStripMenuItem menuConnections;
        private System.Windows.Forms.ToolStripMenuItem menuItemMarketLog;
        private System.Windows.Forms.ToolStripMenuItem menuItemFillHubLog;
        private System.Windows.Forms.ToolStripMenuItem menuItemRejectedFills;
        private System.Windows.Forms.ToolStripMenuItem menuItemArchiveBooks;
        private System.Windows.Forms.ToolStripMenuItem menuNewFillHub;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem menuResetDailyPnL;
        private System.Windows.Forms.ToolStripMenuItem menuOpenPnLManager;
        private System.Windows.Forms.ToolStripMenuItem menuViewTalkerLogs;
        private System.Windows.Forms.ToolStripMenuItem menuItemNewTalkerHub;
        private System.Windows.Forms.ToolStripMenuItem menuDeleteFillManager;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ToolStripMenuItem menuUpdateConfig;
        private System.Windows.Forms.Label textApiLoginName;
        private System.Windows.Forms.ToolStripMenuItem menuDeleteBrettTalker;
        private System.Windows.Forms.ToolStripMenuItem menuItemSaveLogs;
        private System.Windows.Forms.ToolStripMenuItem menuItemCreateCashInstrument;
    }
}
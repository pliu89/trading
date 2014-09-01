namespace ActiveGrid
{
    partial class ActiveGridForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ActiveGridForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnRandom = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.labelActiveThreads = new System.Windows.Forms.Label();
            this.labelAvailableThreads = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.labelFlashedCells = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.labelDataFeedCount = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.marketSpeed = new System.Windows.Forms.TrackBar();
            this.panel2 = new System.Windows.Forms.Panel();
            this.gbBehaviour = new System.Windows.Forms.GroupBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.cbFadeOut = new System.Windows.Forms.CheckBox();
            this.cbUseFlash = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.flashDuration = new System.Windows.Forms.TrackBar();
            this.gbAppearance = new System.Windows.Forms.GroupBox();
            this.label15 = new System.Windows.Forms.Label();
            this.cbColourful = new System.Windows.Forms.CheckBox();
            this.cbAlternating = new System.Windows.Forms.CheckBox();
            this.rbGradient = new System.Windows.Forms.RadioButton();
            this.rbPlain = new System.Windows.Forms.RadioButton();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.panel3 = new System.Windows.Forms.Panel();
            this.lvBalances = new SKACERO.ActiveGrid(this.components);
            this.panel1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.marketSpeed)).BeginInit();
            this.panel2.SuspendLayout();
            this.gbBehaviour.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.flashDuration)).BeginInit();
            this.gbAppearance.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnRandom);
            this.panel1.Controls.Add(this.btnReset);
            this.panel1.Controls.Add(this.groupBox3);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 304);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(10);
            this.panel1.Size = new System.Drawing.Size(878, 225);
            this.panel1.TabIndex = 0;
            // 
            // btnRandom
            // 
            this.btnRandom.Location = new System.Drawing.Point(690, 191);
            this.btnRandom.Name = "btnRandom";
            this.btnRandom.Size = new System.Drawing.Size(119, 23);
            this.btnRandom.TabIndex = 9;
            this.btnRandom.Text = "Random Data Burst";
            this.btnRandom.UseVisualStyleBackColor = true;
            this.btnRandom.Click += new System.EventHandler(this.btnRandom_Click);
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(690, 142);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(119, 23);
            this.btnReset.TabIndex = 8;
            this.btnReset.Text = "Complete Data Burst";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.tableLayoutPanel1);
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.label6);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.label4);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.marketSpeed);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox3.Location = new System.Drawing.Point(310, 10);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(288, 205);
            this.groupBox3.TabIndex = 7;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Market Data Simulation";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 47.32143F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 52.67857F));
            this.tableLayoutPanel1.Controls.Add(this.label9, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label10, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelActiveThreads, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelAvailableThreads, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label12, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelFlashedCells, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label13, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelDataFeedCount, 1, 3);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(282, 88);
            this.tableLayoutPanel1.TabIndex = 19;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label9.Location = new System.Drawing.Point(3, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(127, 22);
            this.label9.TabIndex = 2;
            this.label9.Text = "Active Threads:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label10.Location = new System.Drawing.Point(3, 22);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(127, 22);
            this.label10.TabIndex = 3;
            this.label10.Text = "Available Threads:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelActiveThreads
            // 
            this.labelActiveThreads.AutoSize = true;
            this.labelActiveThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelActiveThreads.Location = new System.Drawing.Point(136, 0);
            this.labelActiveThreads.Name = "labelActiveThreads";
            this.labelActiveThreads.Size = new System.Drawing.Size(143, 22);
            this.labelActiveThreads.TabIndex = 4;
            this.labelActiveThreads.Text = "0";
            this.labelActiveThreads.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelAvailableThreads
            // 
            this.labelAvailableThreads.AutoSize = true;
            this.labelAvailableThreads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelAvailableThreads.Location = new System.Drawing.Point(136, 22);
            this.labelAvailableThreads.Name = "labelAvailableThreads";
            this.labelAvailableThreads.Size = new System.Drawing.Size(143, 22);
            this.labelAvailableThreads.TabIndex = 5;
            this.labelAvailableThreads.Text = "0";
            this.labelAvailableThreads.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label12.Location = new System.Drawing.Point(3, 44);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(127, 22);
            this.label12.TabIndex = 8;
            this.label12.Text = "Flashed Cells:";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelFlashedCells
            // 
            this.labelFlashedCells.AutoSize = true;
            this.labelFlashedCells.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelFlashedCells.Location = new System.Drawing.Point(136, 44);
            this.labelFlashedCells.Name = "labelFlashedCells";
            this.labelFlashedCells.Size = new System.Drawing.Size(143, 22);
            this.labelFlashedCells.TabIndex = 9;
            this.labelFlashedCells.Text = "0";
            this.labelFlashedCells.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label13.Location = new System.Drawing.Point(3, 66);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(127, 22);
            this.label13.TabIndex = 10;
            this.label13.Text = "Active Data Feeds:";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelDataFeedCount
            // 
            this.labelDataFeedCount.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelDataFeedCount.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelDataFeedCount.Location = new System.Drawing.Point(136, 66);
            this.labelDataFeedCount.Name = "labelDataFeedCount";
            this.labelDataFeedCount.Size = new System.Drawing.Size(143, 22);
            this.labelDataFeedCount.TabIndex = 11;
            this.labelDataFeedCount.Text = "0";
            this.labelDataFeedCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(233, 178);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(40, 13);
            this.label7.TabIndex = 18;
            this.label7.Text = "Market";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(20, 178);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(40, 13);
            this.label6.TabIndex = 17;
            this.label6.Text = "Market";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(244, 165);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(27, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Fast";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(23, 165);
            this.label4.Name = "label4";
            this.label4.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.label4.Size = new System.Drawing.Size(30, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "Slow";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(244, 142);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(28, 23);
            this.label3.TabIndex = 13;
            this.label3.Text = "10";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(133, 142);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(28, 23);
            this.label2.TabIndex = 14;
            this.label2.Text = "5";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(20, 142);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 23);
            this.label1.TabIndex = 12;
            this.label1.Text = "0";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // marketSpeed
            // 
            this.marketSpeed.LargeChange = 1;
            this.marketSpeed.Location = new System.Drawing.Point(20, 110);
            this.marketSpeed.Name = "marketSpeed";
            this.marketSpeed.Size = new System.Drawing.Size(252, 45);
            this.marketSpeed.TabIndex = 11;
            this.marketSpeed.ValueChanged += new System.EventHandler(this.marketSpeed_ValueChanged);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.gbBehaviour);
            this.panel2.Controls.Add(this.gbAppearance);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(10, 10);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.panel2.Size = new System.Drawing.Size(300, 205);
            this.panel2.TabIndex = 4;
            // 
            // gbBehaviour
            // 
            this.gbBehaviour.Controls.Add(this.label14);
            this.gbBehaviour.Controls.Add(this.label11);
            this.gbBehaviour.Controls.Add(this.cbFadeOut);
            this.gbBehaviour.Controls.Add(this.cbUseFlash);
            this.gbBehaviour.Controls.Add(this.label8);
            this.gbBehaviour.Controls.Add(this.flashDuration);
            this.gbBehaviour.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbBehaviour.Location = new System.Drawing.Point(10, 91);
            this.gbBehaviour.Name = "gbBehaviour";
            this.gbBehaviour.Size = new System.Drawing.Size(280, 114);
            this.gbBehaviour.TabIndex = 1;
            this.gbBehaviour.TabStop = false;
            this.gbBehaviour.Text = "Behaviour";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(243, 90);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(31, 13);
            this.label14.TabIndex = 21;
            this.label14.Text = "Long";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(9, 90);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(32, 13);
            this.label11.TabIndex = 20;
            this.label11.Text = "Short";
            // 
            // cbFadeOut
            // 
            this.cbFadeOut.AutoSize = true;
            this.cbFadeOut.Location = new System.Drawing.Point(124, 33);
            this.cbFadeOut.Name = "cbFadeOut";
            this.cbFadeOut.Size = new System.Drawing.Size(101, 17);
            this.cbFadeOut.TabIndex = 19;
            this.cbFadeOut.Text = "Fade-Out Effect";
            this.cbFadeOut.UseVisualStyleBackColor = true;
            this.cbFadeOut.CheckedChanged += new System.EventHandler(this.cbFadeOut_CheckedChanged);
            // 
            // cbUseFlash
            // 
            this.cbUseFlash.AutoSize = true;
            this.cbUseFlash.Checked = true;
            this.cbUseFlash.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbUseFlash.Location = new System.Drawing.Point(12, 29);
            this.cbUseFlash.Name = "cbUseFlash";
            this.cbUseFlash.Size = new System.Drawing.Size(73, 17);
            this.cbUseFlash.TabIndex = 18;
            this.cbUseFlash.Text = "Use Flash";
            this.cbUseFlash.UseVisualStyleBackColor = true;
            this.cbUseFlash.CheckedChanged += new System.EventHandler(this.cbUseFlash_CheckedChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(105, 90);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(75, 13);
            this.label8.TabIndex = 16;
            this.label8.Text = "Flash Duration";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // flashDuration
            // 
            this.flashDuration.LargeChange = 450;
            this.flashDuration.Location = new System.Drawing.Point(12, 56);
            this.flashDuration.Maximum = 5000;
            this.flashDuration.Minimum = 500;
            this.flashDuration.Name = "flashDuration";
            this.flashDuration.Size = new System.Drawing.Size(262, 45);
            this.flashDuration.SmallChange = 450;
            this.flashDuration.TabIndex = 17;
            this.flashDuration.TickStyle = System.Windows.Forms.TickStyle.None;
            this.flashDuration.Value = 1500;
            this.flashDuration.ValueChanged += new System.EventHandler(this.flashDuration_ValueChanged);
            // 
            // gbAppearance
            // 
            this.gbAppearance.Controls.Add(this.label15);
            this.gbAppearance.Controls.Add(this.cbColourful);
            this.gbAppearance.Controls.Add(this.cbAlternating);
            this.gbAppearance.Controls.Add(this.rbGradient);
            this.gbAppearance.Controls.Add(this.rbPlain);
            this.gbAppearance.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbAppearance.Location = new System.Drawing.Point(10, 0);
            this.gbAppearance.Name = "gbAppearance";
            this.gbAppearance.Size = new System.Drawing.Size(280, 91);
            this.gbAppearance.TabIndex = 0;
            this.gbAppearance.TabStop = false;
            this.gbAppearance.Text = "Appearance";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(6, 25);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(33, 13);
            this.label15.TabIndex = 17;
            this.label15.Text = "Style:";
            // 
            // cbColourful
            // 
            this.cbColourful.AutoSize = true;
            this.cbColourful.Location = new System.Drawing.Point(124, 60);
            this.cbColourful.Name = "cbColourful";
            this.cbColourful.Size = new System.Drawing.Size(90, 17);
            this.cbColourful.TabIndex = 16;
            this.cbColourful.Text = "Colour Coded";
            this.cbColourful.UseVisualStyleBackColor = true;
            // 
            // cbAlternating
            // 
            this.cbAlternating.AutoSize = true;
            this.cbAlternating.Checked = true;
            this.cbAlternating.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAlternating.Location = new System.Drawing.Point(12, 60);
            this.cbAlternating.Name = "cbAlternating";
            this.cbAlternating.Size = new System.Drawing.Size(106, 17);
            this.cbAlternating.TabIndex = 15;
            this.cbAlternating.Text = "Alternating Rows";
            this.cbAlternating.UseVisualStyleBackColor = true;
            this.cbAlternating.CheckedChanged += new System.EventHandler(this.cbAlternating_CheckedChanged);
            // 
            // rbGradient
            // 
            this.rbGradient.AutoSize = true;
            this.rbGradient.Location = new System.Drawing.Point(95, 23);
            this.rbGradient.Name = "rbGradient";
            this.rbGradient.Size = new System.Drawing.Size(65, 17);
            this.rbGradient.TabIndex = 14;
            this.rbGradient.Text = "Gradient";
            this.rbGradient.UseVisualStyleBackColor = true;
            this.rbGradient.CheckedChanged += new System.EventHandler(this.rbGradient_CheckedChanged);
            // 
            // rbPlain
            // 
            this.rbPlain.AutoSize = true;
            this.rbPlain.Checked = true;
            this.rbPlain.Location = new System.Drawing.Point(41, 23);
            this.rbPlain.Name = "rbPlain";
            this.rbPlain.Size = new System.Drawing.Size(48, 17);
            this.rbPlain.TabIndex = 13;
            this.rbPlain.TabStop = true;
            this.rbPlain.Text = "Plain";
            this.rbPlain.UseVisualStyleBackColor = true;
            this.rbPlain.CheckedChanged += new System.EventHandler(this.rbPlain_CheckedChanged);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.lvBalances);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Padding = new System.Windows.Forms.Padding(10);
            this.panel3.Size = new System.Drawing.Size(878, 304);
            this.panel3.TabIndex = 2;
            // 
            // lvBalances
            // 
            this.lvBalances.AllowColumnReorder = true;
            this.lvBalances.AllowFlashing = true;
            this.lvBalances.AlternatingBackColor = System.Drawing.Color.Gainsboro;
            this.lvBalances.AlternatingGradientEndColor = System.Drawing.Color.Gainsboro;
            this.lvBalances.AlternatingGradientStartColor = System.Drawing.Color.WhiteSmoke;
            this.lvBalances.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvBalances.FlashBackColor = System.Drawing.Color.Yellow;
            this.lvBalances.FlashDuration = 2000;
            this.lvBalances.FlashFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lvBalances.FlashForeColor = System.Drawing.Color.Navy;
            this.lvBalances.FlashGradientEndColor = System.Drawing.Color.LightBlue;
            this.lvBalances.FlashGradientStartColor = System.Drawing.Color.AliceBlue;
            this.lvBalances.ForeColorNegativeValues = System.Drawing.Color.Red;
            this.lvBalances.FullRowSelect = true;
            this.lvBalances.GridLines = true;
            this.lvBalances.GroupIndex = 0;
            this.lvBalances.Location = new System.Drawing.Point(10, 10);
            this.lvBalances.Name = "lvBalances";
            this.lvBalances.OwnerDraw = true;
            this.lvBalances.ShowGroups = false;
            this.lvBalances.Size = new System.Drawing.Size(858, 284);
            this.lvBalances.TabIndex = 1;
            this.lvBalances.UseCompatibleStateImageBehavior = false;
            this.lvBalances.UseRowHeaderButtons = true;
            this.lvBalances.OnRowHeaderRightMouseClick += new SKACERO.ActiveGrid.OnRowHeaderRightMouseClickHandler(this.lvBalances_OnRowHeaderRightMouseClick);
            this.lvBalances.OnRowHeaderLeftMouseClick += new SKACERO.ActiveGrid.OnRowHeaderLeftMouseClickHandler(this.lvBalances_OnRowHeaderLeftMouseClick);
            this.lvBalances.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvBalances_ColumnClick);
            // 
            // ActiveGridForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(878, 529);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ActiveGridForm";
            this.Text = "Active Grid Demonstration";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.marketSpeed)).EndInit();
            this.panel2.ResumeLayout(false);
            this.gbBehaviour.ResumeLayout(false);
            this.gbBehaviour.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.flashDuration)).EndInit();
            this.gbAppearance.ResumeLayout(false);
            this.gbAppearance.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private SKACERO.ActiveGrid lvBalances;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TrackBar marketSpeed;
        private System.Windows.Forms.GroupBox gbBehaviour;
        private System.Windows.Forms.TrackBar flashDuration;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.GroupBox gbAppearance;
        private System.Windows.Forms.RadioButton rbGradient;
        private System.Windows.Forms.RadioButton rbPlain;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox cbFadeOut;
        private System.Windows.Forms.CheckBox cbUseFlash;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label labelActiveThreads;
        private System.Windows.Forms.Label labelAvailableThreads;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label labelFlashedCells;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label labelDataFeedCount;
        private System.Windows.Forms.CheckBox cbAlternating;
        private System.Windows.Forms.CheckBox cbColourful;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnRandom;
        private System.Windows.Forms.Label label15;
    }
}


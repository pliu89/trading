namespace MistyTests.Sockets
{
    partial class SocketServer
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
            this.buttonStartListener = new System.Windows.Forms.Button();
            this.textBoxListenerPort = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonSpawnClient = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonStartListener
            // 
            this.buttonStartListener.BackColor = System.Drawing.Color.Red;
            this.buttonStartListener.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStartListener.ForeColor = System.Drawing.Color.Yellow;
            this.buttonStartListener.Location = new System.Drawing.Point(1, 0);
            this.buttonStartListener.Name = "buttonStartListener";
            this.buttonStartListener.Size = new System.Drawing.Size(75, 23);
            this.buttonStartListener.TabIndex = 1;
            this.buttonStartListener.Text = "stopped";
            this.buttonStartListener.UseVisualStyleBackColor = false;
            this.buttonStartListener.Click += new System.EventHandler(this.button_Click);
            // 
            // textBoxListenerPort
            // 
            this.textBoxListenerPort.Location = new System.Drawing.Point(82, 3);
            this.textBoxListenerPort.Name = "textBoxListenerPort";
            this.textBoxListenerPort.Size = new System.Drawing.Size(54, 20);
            this.textBoxListenerPort.TabIndex = 2;
            this.textBoxListenerPort.Text = "6002";
            this.textBoxListenerPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBoxListenerPort.TextChanged += new System.EventHandler(this.textBoxListenerPort_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(84, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "listen port";
            // 
            // buttonSpawnClient
            // 
            this.buttonSpawnClient.Location = new System.Drawing.Point(142, 1);
            this.buttonSpawnClient.Name = "buttonSpawnClient";
            this.buttonSpawnClient.Size = new System.Drawing.Size(75, 23);
            this.buttonSpawnClient.TabIndex = 4;
            this.buttonSpawnClient.Text = "spawn client";
            this.buttonSpawnClient.UseVisualStyleBackColor = true;
            this.buttonSpawnClient.Click += new System.EventHandler(this.button_Click);
            // 
            // SocketServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(223, 50);
            this.Controls.Add(this.buttonSpawnClient);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxListenerPort);
            this.Controls.Add(this.buttonStartListener);
            this.Name = "SocketServer";
            this.Text = "SocketServer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Sockets_FormClosing);
            this.Load += new System.EventHandler(this.Sockets_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStartListener;
        private System.Windows.Forms.TextBox textBoxListenerPort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonSpawnClient;
    }
}
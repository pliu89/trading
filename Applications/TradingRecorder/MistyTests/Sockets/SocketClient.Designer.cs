namespace MistyTests.Sockets
{
    partial class SocketClient
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
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.buttonClientConnect = new System.Windows.Forms.Button();
            this.textBoxServerAddress = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxServerPort = new System.Windows.Forms.TextBox();
            this.buttonClientDisconnect = new System.Windows.Forms.Button();
            this.textBoxSendText = new System.Windows.Forms.TextBox();
            this.buttonSend = new System.Windows.Forms.Button();
            this.checkBoxAutoReconnect = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // buttonClientConnect
            // 
            this.buttonClientConnect.Location = new System.Drawing.Point(2, 3);
            this.buttonClientConnect.Name = "buttonClientConnect";
            this.buttonClientConnect.Size = new System.Drawing.Size(75, 23);
            this.buttonClientConnect.TabIndex = 1;
            this.buttonClientConnect.Text = "connect";
            this.buttonClientConnect.UseVisualStyleBackColor = true;
            this.buttonClientConnect.Click += new System.EventHandler(this.button_Click);
            // 
            // textBoxServerAddress
            // 
            this.textBoxServerAddress.Location = new System.Drawing.Point(83, 5);
            this.textBoxServerAddress.Name = "textBoxServerAddress";
            this.textBoxServerAddress.Size = new System.Drawing.Size(84, 20);
            this.textBoxServerAddress.TabIndex = 6;
            this.textBoxServerAddress.Text = "6002";
            this.textBoxServerAddress.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(83, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "connect to server";
            // 
            // textBoxServerPort
            // 
            this.textBoxServerPort.Location = new System.Drawing.Point(173, 5);
            this.textBoxServerPort.Name = "textBoxServerPort";
            this.textBoxServerPort.Size = new System.Drawing.Size(48, 20);
            this.textBoxServerPort.TabIndex = 8;
            this.textBoxServerPort.Text = "6002";
            this.textBoxServerPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // buttonClientDisconnect
            // 
            this.buttonClientDisconnect.Location = new System.Drawing.Point(2, 28);
            this.buttonClientDisconnect.Name = "buttonClientDisconnect";
            this.buttonClientDisconnect.Size = new System.Drawing.Size(75, 23);
            this.buttonClientDisconnect.TabIndex = 9;
            this.buttonClientDisconnect.Text = "disconnect";
            this.buttonClientDisconnect.UseVisualStyleBackColor = true;
            this.buttonClientDisconnect.Click += new System.EventHandler(this.button_Click);
            // 
            // textBoxSendText
            // 
            this.textBoxSendText.Location = new System.Drawing.Point(2, 57);
            this.textBoxSendText.Name = "textBoxSendText";
            this.textBoxSendText.Size = new System.Drawing.Size(247, 20);
            this.textBoxSendText.TabIndex = 10;
            this.textBoxSendText.Text = "Hello.";
            // 
            // buttonSend
            // 
            this.buttonSend.Location = new System.Drawing.Point(255, 55);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(47, 23);
            this.buttonSend.TabIndex = 11;
            this.buttonSend.Text = "send";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.button_Click);
            // 
            // checkBoxAutoReconnect
            // 
            this.checkBoxAutoReconnect.AutoSize = true;
            this.checkBoxAutoReconnect.Location = new System.Drawing.Point(227, 3);
            this.checkBoxAutoReconnect.Name = "checkBoxAutoReconnect";
            this.checkBoxAutoReconnect.Size = new System.Drawing.Size(98, 17);
            this.checkBoxAutoReconnect.TabIndex = 12;
            this.checkBoxAutoReconnect.Text = "auto reconnect";
            this.checkBoxAutoReconnect.UseVisualStyleBackColor = true;
            // 
            // SocketClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(322, 81);
            this.Controls.Add(this.checkBoxAutoReconnect);
            this.Controls.Add(this.buttonSend);
            this.Controls.Add(this.textBoxSendText);
            this.Controls.Add(this.buttonClientDisconnect);
            this.Controls.Add(this.textBoxServerPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxServerAddress);
            this.Controls.Add(this.buttonClientConnect);
            this.Name = "SocketClient";
            this.Text = "SocketClient";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Sockets_FormClosing);
            this.Load += new System.EventHandler(this.Sockets_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button buttonClientConnect;
        private System.Windows.Forms.TextBox textBoxServerAddress;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxServerPort;
        private System.Windows.Forms.Button buttonClientDisconnect;
        private System.Windows.Forms.TextBox textBoxSendText;
        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.CheckBox checkBoxAutoReconnect;
    }
}
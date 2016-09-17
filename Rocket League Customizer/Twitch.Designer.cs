namespace Rocket_League_Customizer
{
    partial class Twitch
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Twitch));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.twitchSaveButton = new System.Windows.Forms.Button();
            this.usernameText = new System.Windows.Forms.TextBox();
            this.authText = new System.Windows.Forms.TextBox();
            this.authTokenLink = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(6, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Username:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(6, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "Auth Token:";
            // 
            // twitchSaveButton
            // 
            this.twitchSaveButton.Location = new System.Drawing.Point(108, 107);
            this.twitchSaveButton.Name = "twitchSaveButton";
            this.twitchSaveButton.Size = new System.Drawing.Size(75, 23);
            this.twitchSaveButton.TabIndex = 2;
            this.twitchSaveButton.Text = "Save";
            this.twitchSaveButton.UseVisualStyleBackColor = true;
            this.twitchSaveButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // usernameText
            // 
            this.usernameText.Location = new System.Drawing.Point(89, 13);
            this.usernameText.Name = "usernameText";
            this.usernameText.Size = new System.Drawing.Size(182, 20);
            this.usernameText.TabIndex = 3;
            // 
            // authText
            // 
            this.authText.Location = new System.Drawing.Point(89, 49);
            this.authText.Name = "authText";
            this.authText.Size = new System.Drawing.Size(182, 20);
            this.authText.TabIndex = 4;
            // 
            // authTokenLink
            // 
            this.authTokenLink.AutoSize = true;
            this.authTokenLink.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.authTokenLink.Location = new System.Drawing.Point(86, 72);
            this.authTokenLink.Name = "authTokenLink";
            this.authTokenLink.Size = new System.Drawing.Size(98, 17);
            this.authTokenLink.TabIndex = 5;
            this.authTokenLink.TabStop = true;
            this.authTokenLink.Text = "Get Auth Token";
            this.authTokenLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.authTokenLink_LinkClicked);
            // 
            // Twitch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(285, 142);
            this.Controls.Add(this.authTokenLink);
            this.Controls.Add(this.authText);
            this.Controls.Add(this.usernameText);
            this.Controls.Add(this.twitchSaveButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Twitch";
            this.Text = "Twitch Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button twitchSaveButton;
        private System.Windows.Forms.LinkLabel authTokenLink;
        public System.Windows.Forms.TextBox usernameText;
        public System.Windows.Forms.TextBox authText;
    }
}
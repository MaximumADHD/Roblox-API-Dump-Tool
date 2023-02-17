namespace RobloxApiDumpTool
{
    partial class ApiDumpTool
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
            this.channel = new System.Windows.Forms.ComboBox();
            this.compareVersions = new System.Windows.Forms.Button();
            this.status = new System.Windows.Forms.Label();
            this.viewApiDump = new System.Windows.Forms.Button();
            this.appLogo = new System.Windows.Forms.PictureBox();
            this.channelLbl = new System.Windows.Forms.Label();
            this.apiDumpFormat = new System.Windows.Forms.ComboBox();
            this.formatLbl = new System.Windows.Forms.Label();
            this.fullDump = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.appLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // channel
            // 
            this.channel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.channel.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.channel.FormattingEnabled = true;
            this.channel.Items.AddRange(new object[] {
            "LIVE",
            "zCanary",
            "zIntegration"});
            this.channel.Location = new System.Drawing.Point(10, 87);
            this.channel.Margin = new System.Windows.Forms.Padding(20, 3, 20, 5);
            this.channel.Name = "channel";
            this.channel.Size = new System.Drawing.Size(218, 21);
            this.channel.TabIndex = 0;
            this.channel.SelectedIndexChanged += new System.EventHandler(this.channel_SelectedIndexChanged);
            this.channel.KeyDown += new System.Windows.Forms.KeyEventHandler(this.channel_KeyDown);
            // 
            // compareVersions
            // 
            this.compareVersions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.compareVersions.Enabled = false;
            this.compareVersions.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.compareVersions.Location = new System.Drawing.Point(10, 149);
            this.compareVersions.Margin = new System.Windows.Forms.Padding(30, 3, 30, 5);
            this.compareVersions.Name = "compareVersions";
            this.compareVersions.Size = new System.Drawing.Size(278, 23);
            this.compareVersions.TabIndex = 3;
            this.compareVersions.Text = "Compare to Production";
            this.compareVersions.UseVisualStyleBackColor = true;
            this.compareVersions.Click += new System.EventHandler(this.compareVersions_Click);
            // 
            // status
            // 
            this.status.AutoSize = true;
            this.status.Enabled = false;
            this.status.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.status.Location = new System.Drawing.Point(0, 181);
            this.status.Margin = new System.Windows.Forms.Padding(0);
            this.status.Name = "status";
            this.status.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.status.Size = new System.Drawing.Size(113, 14);
            this.status.TabIndex = 4;
            this.status.Tag = "Testing lol";
            this.status.Text = "Status: Ready!";
            this.status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // viewApiDump
            // 
            this.viewApiDump.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.viewApiDump.Enabled = false;
            this.viewApiDump.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.viewApiDump.Location = new System.Drawing.Point(10, 118);
            this.viewApiDump.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.viewApiDump.Name = "viewApiDump";
            this.viewApiDump.Size = new System.Drawing.Size(278, 23);
            this.viewApiDump.TabIndex = 5;
            this.viewApiDump.Text = "View API Dump";
            this.viewApiDump.UseVisualStyleBackColor = true;
            this.viewApiDump.Click += new System.EventHandler(this.viewApiDumpClassic_Click);
            // 
            // appLogo
            // 
            this.appLogo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.appLogo.BackgroundImage = global::RobloxApiDumpTool.Properties.Resources.AppLogo;
            this.appLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.appLogo.Location = new System.Drawing.Point(10, 10);
            this.appLogo.Margin = new System.Windows.Forms.Padding(10);
            this.appLogo.Name = "appLogo";
            this.appLogo.Padding = new System.Windows.Forms.Padding(5);
            this.appLogo.Size = new System.Drawing.Size(278, 55);
            this.appLogo.TabIndex = 6;
            this.appLogo.TabStop = false;
            // 
            // channelLbl
            // 
            this.channelLbl.AutoSize = true;
            this.channelLbl.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.channelLbl.Location = new System.Drawing.Point(7, 71);
            this.channelLbl.Name = "channelLbl";
            this.channelLbl.Size = new System.Drawing.Size(55, 13);
            this.channelLbl.TabIndex = 7;
            this.channelLbl.Text = "Channel:";
            // 
            // apiDumpFormat
            // 
            this.apiDumpFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.apiDumpFormat.FormattingEnabled = true;
            this.apiDumpFormat.Items.AddRange(new object[] {
            "TXT",
            "HTML",
            "PNG",
            "JSON"});
            this.apiDumpFormat.Location = new System.Drawing.Point(232, 86);
            this.apiDumpFormat.Name = "apiDumpFormat";
            this.apiDumpFormat.Size = new System.Drawing.Size(56, 21);
            this.apiDumpFormat.TabIndex = 8;
            this.apiDumpFormat.SelectedIndexChanged += new System.EventHandler(this.apiDumpFormat_SelectedIndexChanged);
            // 
            // formatLbl
            // 
            this.formatLbl.AutoSize = true;
            this.formatLbl.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.formatLbl.Location = new System.Drawing.Point(229, 70);
            this.formatLbl.Name = "formatLbl";
            this.formatLbl.Size = new System.Drawing.Size(49, 13);
            this.formatLbl.TabIndex = 10;
            this.formatLbl.Text = "Format:";
            // 
            // fullDump
            // 
            this.fullDump.AutoSize = true;
            this.fullDump.Location = new System.Drawing.Point(222, 181);
            this.fullDump.Name = "fullDump";
            this.fullDump.Size = new System.Drawing.Size(73, 17);
            this.fullDump.TabIndex = 11;
            this.fullDump.Text = "Full Dump";
            this.fullDump.UseVisualStyleBackColor = true;
            // 
            // ApiDumpTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(298, 205);
            this.Controls.Add(this.fullDump);
            this.Controls.Add(this.formatLbl);
            this.Controls.Add(this.apiDumpFormat);
            this.Controls.Add(this.channelLbl);
            this.Controls.Add(this.appLogo);
            this.Controls.Add(this.viewApiDump);
            this.Controls.Add(this.status);
            this.Controls.Add(this.compareVersions);
            this.Controls.Add(this.channel);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::RobloxApiDumpTool.Properties.Resources.AppIcon;
            this.MaximizeBox = false;
            this.Name = "ApiDumpTool";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Roblox API Dump Tool";
            this.Load += new System.EventHandler(this.ApiDumpTool_Load);
            ((System.ComponentModel.ISupportInitialize)(this.appLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox channel;
        private System.Windows.Forms.Button compareVersions;
        private System.Windows.Forms.Label status;
        private System.Windows.Forms.Button viewApiDump;
        private System.Windows.Forms.PictureBox appLogo;
        private System.Windows.Forms.Label channelLbl;
        private System.Windows.Forms.ComboBox apiDumpFormat;
        private System.Windows.Forms.Label formatLbl;
        private System.Windows.Forms.CheckBox fullDump;
    }
}


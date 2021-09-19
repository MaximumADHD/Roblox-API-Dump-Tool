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
            this.branch = new System.Windows.Forms.ComboBox();
            this.compareVersions = new System.Windows.Forms.Button();
            this.status = new System.Windows.Forms.Label();
            this.viewApiDump = new System.Windows.Forms.Button();
            this.appLogo = new System.Windows.Forms.PictureBox();
            this.branchLbl = new System.Windows.Forms.Label();
            this.apiDumpFormat = new System.Windows.Forms.ComboBox();
            this.formatLbl = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.appLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // branch
            // 
            this.branch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.branch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.branch.Enabled = true;
            this.branch.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.branch.FormattingEnabled = true;
            this.branch.Items.AddRange(new object[] {
            "roblox",
            "sitetest1.robloxlabs",
            "sitetest2.robloxlabs",
            "sitetest3.robloxlabs"});
            this.branch.Location = new System.Drawing.Point(15, 134);
            this.branch.Margin = new System.Windows.Forms.Padding(30, 5, 30, 8);
            this.branch.Name = "branch";
            this.branch.Size = new System.Drawing.Size(325, 28);
            this.branch.TabIndex = 0;
            this.branch.SelectedIndexChanged += new System.EventHandler(this.branch_SelectedIndexChanged);
            // 
            // compareVersions
            // 
            this.compareVersions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.compareVersions.Enabled = false;
            this.compareVersions.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.compareVersions.Location = new System.Drawing.Point(15, 229);
            this.compareVersions.Margin = new System.Windows.Forms.Padding(45, 5, 45, 8);
            this.compareVersions.Name = "compareVersions";
            this.compareVersions.Size = new System.Drawing.Size(417, 35);
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
            this.status.Location = new System.Drawing.Point(0, 278);
            this.status.Margin = new System.Windows.Forms.Padding(0);
            this.status.Name = "status";
            this.status.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.status.Size = new System.Drawing.Size(162, 22);
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
            this.viewApiDump.Location = new System.Drawing.Point(15, 182);
            this.viewApiDump.Margin = new System.Windows.Forms.Padding(22, 8, 22, 8);
            this.viewApiDump.Name = "viewApiDump";
            this.viewApiDump.Size = new System.Drawing.Size(417, 35);
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
            this.appLogo.Location = new System.Drawing.Point(15, 15);
            this.appLogo.Margin = new System.Windows.Forms.Padding(15);
            this.appLogo.Name = "appLogo";
            this.appLogo.Padding = new System.Windows.Forms.Padding(8);
            this.appLogo.Size = new System.Drawing.Size(417, 85);
            this.appLogo.TabIndex = 6;
            this.appLogo.TabStop = false;
            // 
            // branchLbl
            // 
            this.branchLbl.AutoSize = true;
            this.branchLbl.Enabled = false;
            this.branchLbl.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.branchLbl.Location = new System.Drawing.Point(10, 109);
            this.branchLbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.branchLbl.Name = "branchLbl";
            this.branchLbl.Size = new System.Drawing.Size(72, 20);
            this.branchLbl.TabIndex = 7;
            this.branchLbl.Text = "Branch:";
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
            this.apiDumpFormat.Location = new System.Drawing.Point(348, 132);
            this.apiDumpFormat.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.apiDumpFormat.Name = "apiDumpFormat";
            this.apiDumpFormat.Size = new System.Drawing.Size(82, 28);
            this.apiDumpFormat.TabIndex = 8;
            this.apiDumpFormat.SelectedIndexChanged += new System.EventHandler(this.apiDumpFormat_SelectedIndexChanged);
            // 
            // formatLbl
            // 
            this.formatLbl.AutoSize = true;
            this.formatLbl.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.formatLbl.Location = new System.Drawing.Point(344, 108);
            this.formatLbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.formatLbl.Name = "formatLbl";
            this.formatLbl.Size = new System.Drawing.Size(72, 20);
            this.formatLbl.TabIndex = 10;
            this.formatLbl.Text = "Format:";
            // 
            // ApiDumpTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(447, 315);
            this.Controls.Add(this.formatLbl);
            this.Controls.Add(this.apiDumpFormat);
            this.Controls.Add(this.branchLbl);
            this.Controls.Add(this.appLogo);
            this.Controls.Add(this.viewApiDump);
            this.Controls.Add(this.status);
            this.Controls.Add(this.compareVersions);
            this.Controls.Add(this.branch);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::RobloxApiDumpTool.Properties.Resources.AppIcon;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.Name = "ApiDumpTool";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 0, 15);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Roblox API Dump Tool";
            this.Load += new System.EventHandler(this.ApiDumpTool_Load);
            ((System.ComponentModel.ISupportInitialize)(this.appLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox branch;
        private System.Windows.Forms.Button compareVersions;
        private System.Windows.Forms.Label status;
        private System.Windows.Forms.Button viewApiDump;
        private System.Windows.Forms.PictureBox appLogo;
        private System.Windows.Forms.Label branchLbl;
        private System.Windows.Forms.ComboBox apiDumpFormat;
        private System.Windows.Forms.Label formatLbl;
    }
}


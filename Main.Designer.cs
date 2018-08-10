namespace Roblox
{
    partial class Main
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.branch = new System.Windows.Forms.ComboBox();
            this.viewApiDumpJson = new System.Windows.Forms.Button();
            this.compareToProduction = new System.Windows.Forms.Button();
            this.status = new System.Windows.Forms.Label();
            this.viewApiDumpClassic = new System.Windows.Forms.Button();
            this.appLogo = new System.Windows.Forms.PictureBox();
            this.branchLbl = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.appLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // branch
            // 
            this.branch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.branch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.branch.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.branch.FormattingEnabled = true;
            this.branch.Items.AddRange(new object[] {
            "roblox",
            "gametest1.robloxlabs",
            "gametest2.robloxlabs",
            "gametest3.robloxlabs",
            "gametest4.robloxlabs",
            "gametest5.robloxlabs"});
            this.branch.Location = new System.Drawing.Point(10, 93);
            this.branch.Margin = new System.Windows.Forms.Padding(20, 3, 20, 5);
            this.branch.Name = "branch";
            this.branch.Size = new System.Drawing.Size(278, 21);
            this.branch.TabIndex = 0;
            this.branch.SelectedIndexChanged += new System.EventHandler(this.branch_SelectedIndexChanged);
            // 
            // viewApiDumpJson
            // 
            this.viewApiDumpJson.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.viewApiDumpJson.Enabled = false;
            this.viewApiDumpJson.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.viewApiDumpJson.Location = new System.Drawing.Point(10, 155);
            this.viewApiDumpJson.Margin = new System.Windows.Forms.Padding(15, 3, 15, 5);
            this.viewApiDumpJson.Name = "viewApiDumpJson";
            this.viewApiDumpJson.Size = new System.Drawing.Size(278, 23);
            this.viewApiDumpJson.TabIndex = 2;
            this.viewApiDumpJson.Text = "View JSON API Dump";
            this.viewApiDumpJson.UseVisualStyleBackColor = true;
            this.viewApiDumpJson.Click += new System.EventHandler(this.viewApiDumpJson_Click);
            // 
            // compareToProduction
            // 
            this.compareToProduction.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.compareToProduction.Enabled = false;
            this.compareToProduction.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.compareToProduction.Location = new System.Drawing.Point(10, 186);
            this.compareToProduction.Margin = new System.Windows.Forms.Padding(30, 3, 30, 10);
            this.compareToProduction.Name = "compareToProduction";
            this.compareToProduction.Size = new System.Drawing.Size(278, 23);
            this.compareToProduction.TabIndex = 3;
            this.compareToProduction.Text = "Compare to Production";
            this.compareToProduction.UseVisualStyleBackColor = true;
            this.compareToProduction.Click += new System.EventHandler(this.compareToProduction_Click);
            // 
            // status
            // 
            this.status.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.status.AutoSize = true;
            this.status.Enabled = false;
            this.status.Font = new System.Drawing.Font("Corbel", 9F, System.Drawing.FontStyle.Bold);
            this.status.Location = new System.Drawing.Point(7, 220);
            this.status.Margin = new System.Windows.Forms.Padding(0);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(85, 14);
            this.status.TabIndex = 4;
            this.status.Tag = "Testing lol";
            this.status.Text = "Status: Ready!";
            this.status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // viewApiDumpClassic
            // 
            this.viewApiDumpClassic.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.viewApiDumpClassic.Enabled = false;
            this.viewApiDumpClassic.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.viewApiDumpClassic.Location = new System.Drawing.Point(10, 124);
            this.viewApiDumpClassic.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.viewApiDumpClassic.Name = "viewApiDumpClassic";
            this.viewApiDumpClassic.Size = new System.Drawing.Size(278, 23);
            this.viewApiDumpClassic.TabIndex = 5;
            this.viewApiDumpClassic.Text = "View Classic API Dump";
            this.viewApiDumpClassic.UseVisualStyleBackColor = true;
            this.viewApiDumpClassic.Click += new System.EventHandler(this.viewApiDumpClassic_Click);
            // 
            // appLogo
            // 
            this.appLogo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.appLogo.BackgroundImage = global::Roblox.Properties.Resources.AppLogo;
            this.appLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.appLogo.Location = new System.Drawing.Point(10, 10);
            this.appLogo.Margin = new System.Windows.Forms.Padding(10);
            this.appLogo.Name = "appLogo";
            this.appLogo.Padding = new System.Windows.Forms.Padding(5);
            this.appLogo.Size = new System.Drawing.Size(278, 55);
            this.appLogo.TabIndex = 6;
            this.appLogo.TabStop = false;
            // 
            // branchLbl
            // 
            this.branchLbl.AutoSize = true;
            this.branchLbl.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this.branchLbl.ForeColor = System.Drawing.Color.Black;
            this.branchLbl.Location = new System.Drawing.Point(7, 76);
            this.branchLbl.Margin = new System.Windows.Forms.Padding(1, 0, 0, 0);
            this.branchLbl.Name = "branchLbl";
            this.branchLbl.Size = new System.Drawing.Size(56, 14);
            this.branchLbl.TabIndex = 7;
            this.branchLbl.Text = "Branch:";
            this.branchLbl.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(298, 243);
            this.Controls.Add(this.branchLbl);
            this.Controls.Add(this.appLogo);
            this.Controls.Add(this.viewApiDumpClassic);
            this.Controls.Add(this.status);
            this.Controls.Add(this.compareToProduction);
            this.Controls.Add(this.viewApiDumpJson);
            this.Controls.Add(this.branch);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Main";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Roblox API Dump Tool";
            ((System.ComponentModel.ISupportInitialize)(this.appLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox branch;
        private System.Windows.Forms.Button viewApiDumpJson;
        private System.Windows.Forms.Button compareToProduction;
        private System.Windows.Forms.Label status;
        private System.Windows.Forms.Button viewApiDumpClassic;
        private System.Windows.Forms.PictureBox appLogo;
        private System.Windows.Forms.Label branchLbl;
    }
}


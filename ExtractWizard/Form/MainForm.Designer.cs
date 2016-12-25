namespace ExtractWizard
{
    partial class MainForm
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
            this.groupOptions = new System.Windows.Forms.GroupBox();
            this.btnHelp = new System.Windows.Forms.Button();
            this.btnBrowseArchive = new System.Windows.Forms.Button();
            this.editPassword = new System.Windows.Forms.TextBox();
            this.lblExtractToFolder = new System.Windows.Forms.Label();
            this.lblBackupArchive = new System.Windows.Forms.Label();
            this.editBackupArchive = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.btnExtractToFolder = new System.Windows.Forms.Button();
            this.editExtractToFolder = new System.Windows.Forms.TextBox();
            this.chkIgnoreErrors = new System.Windows.Forms.CheckBox();
            this.chkDryRun = new System.Windows.Forms.CheckBox();
            this.btnExtract = new System.Windows.Forms.Button();
            this.btnDonate = new System.Windows.Forms.Button();
            this.groupProgress = new System.Windows.Forms.GroupBox();
            this.lblExtractedFile = new System.Windows.Forms.Label();
            this.progressBarExtract = new System.Windows.Forms.ProgressBar();
            this.groupOptions.SuspendLayout();
            this.groupProgress.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupOptions
            // 
            this.groupOptions.Controls.Add(this.btnHelp);
            this.groupOptions.Controls.Add(this.btnBrowseArchive);
            this.groupOptions.Controls.Add(this.editPassword);
            this.groupOptions.Controls.Add(this.lblExtractToFolder);
            this.groupOptions.Controls.Add(this.lblBackupArchive);
            this.groupOptions.Controls.Add(this.editBackupArchive);
            this.groupOptions.Controls.Add(this.lblPassword);
            this.groupOptions.Controls.Add(this.btnExtractToFolder);
            this.groupOptions.Controls.Add(this.editExtractToFolder);
            this.groupOptions.Controls.Add(this.chkIgnoreErrors);
            this.groupOptions.Controls.Add(this.chkDryRun);
            this.groupOptions.Controls.Add(this.btnExtract);
            this.groupOptions.Controls.Add(this.btnDonate);
            this.groupOptions.Location = new System.Drawing.Point(6, 6);
            this.groupOptions.Margin = new System.Windows.Forms.Padding(2);
            this.groupOptions.Name = "groupOptions";
            this.groupOptions.Padding = new System.Windows.Forms.Padding(2);
            this.groupOptions.Size = new System.Drawing.Size(542, 203);
            this.groupOptions.TabIndex = 1;
            this.groupOptions.TabStop = false;
            this.groupOptions.Tag = "GROUP_OPTIONS";
            this.groupOptions.Text = "Options";
            // 
            // btnHelp
            // 
            this.btnHelp.Location = new System.Drawing.Point(469, 164);
            this.btnHelp.Margin = new System.Windows.Forms.Padding(2);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(72, 26);
            this.btnHelp.TabIndex = 13;
            this.btnHelp.Tag = "BTN_HELP";
            this.btnHelp.Text = "&Help";
            this.btnHelp.UseVisualStyleBackColor = true;
            // 
            // btnBrowseArchive
            // 
            this.btnBrowseArchive.AutoSize = true;
            this.btnBrowseArchive.Location = new System.Drawing.Point(469, 21);
            this.btnBrowseArchive.Margin = new System.Windows.Forms.Padding(2);
            this.btnBrowseArchive.Name = "btnBrowseArchive";
            this.btnBrowseArchive.Size = new System.Drawing.Size(72, 23);
            this.btnBrowseArchive.TabIndex = 4;
            this.btnBrowseArchive.Tag = "BTN_BROWSE_ARCHIVE";
            this.btnBrowseArchive.Text = "&Browse...";
            this.btnBrowseArchive.UseVisualStyleBackColor = true;
            // 
            // editPassword
            // 
            this.editPassword.Location = new System.Drawing.Point(242, 80);
            this.editPassword.Margin = new System.Windows.Forms.Padding(2);
            this.editPassword.Name = "editPassword";
            this.editPassword.PasswordChar = '*';
            this.editPassword.Size = new System.Drawing.Size(226, 20);
            this.editPassword.TabIndex = 9;
            // 
            // lblExtractToFolder
            // 
            this.lblExtractToFolder.AutoSize = true;
            this.lblExtractToFolder.Location = new System.Drawing.Point(3, 53);
            this.lblExtractToFolder.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblExtractToFolder.Name = "lblExtractToFolder";
            this.lblExtractToFolder.Size = new System.Drawing.Size(81, 13);
            this.lblExtractToFolder.TabIndex = 5;
            this.lblExtractToFolder.Tag = "LBL_EXTRACT_TO";
            this.lblExtractToFolder.Text = "Extract to &folder";
            // 
            // lblBackupArchive
            // 
            this.lblBackupArchive.AutoSize = true;
            this.lblBackupArchive.Location = new System.Drawing.Point(3, 25);
            this.lblBackupArchive.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblBackupArchive.Name = "lblBackupArchive";
            this.lblBackupArchive.Size = new System.Drawing.Size(83, 13);
            this.lblBackupArchive.TabIndex = 2;
            this.lblBackupArchive.Tag = "LBL_BACKUP_ARCHIVE";
            this.lblBackupArchive.Text = "Backup &Archive";
            // 
            // editBackupArchive
            // 
            this.editBackupArchive.Location = new System.Drawing.Point(242, 23);
            this.editBackupArchive.Margin = new System.Windows.Forms.Padding(2);
            this.editBackupArchive.Name = "editBackupArchive";
            this.editBackupArchive.Size = new System.Drawing.Size(226, 20);
            this.editBackupArchive.TabIndex = 3;
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(3, 81);
            this.lblPassword.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(117, 13);
            this.lblPassword.TabIndex = 8;
            this.lblPassword.Tag = "LBL_PASSWORD";
            this.lblPassword.Text = "&Password (for JPS files)";
            // 
            // btnExtractToFolder
            // 
            this.btnExtractToFolder.AutoSize = true;
            this.btnExtractToFolder.Location = new System.Drawing.Point(469, 49);
            this.btnExtractToFolder.Margin = new System.Windows.Forms.Padding(2);
            this.btnExtractToFolder.Name = "btnExtractToFolder";
            this.btnExtractToFolder.Size = new System.Drawing.Size(72, 23);
            this.btnExtractToFolder.TabIndex = 7;
            this.btnExtractToFolder.Tag = "BTN_BROWSE_FOLDER";
            this.btnExtractToFolder.Text = "B&rowse...";
            this.btnExtractToFolder.UseVisualStyleBackColor = true;
            // 
            // editExtractToFolder
            // 
            this.editExtractToFolder.Location = new System.Drawing.Point(242, 51);
            this.editExtractToFolder.Margin = new System.Windows.Forms.Padding(2);
            this.editExtractToFolder.Name = "editExtractToFolder";
            this.editExtractToFolder.Size = new System.Drawing.Size(226, 20);
            this.editExtractToFolder.TabIndex = 6;
            // 
            // chkIgnoreErrors
            // 
            this.chkIgnoreErrors.AutoSize = true;
            this.chkIgnoreErrors.Location = new System.Drawing.Point(242, 108);
            this.chkIgnoreErrors.Margin = new System.Windows.Forms.Padding(2);
            this.chkIgnoreErrors.Name = "chkIgnoreErrors";
            this.chkIgnoreErrors.Size = new System.Drawing.Size(126, 17);
            this.chkIgnoreErrors.TabIndex = 10;
            this.chkIgnoreErrors.Tag = "CHK_IGNORE_ERRORS";
            this.chkIgnoreErrors.Text = "&Ignore file write errors";
            this.chkIgnoreErrors.UseVisualStyleBackColor = true;
            // 
            // chkDryRun
            // 
            this.chkDryRun.AutoSize = true;
            this.chkDryRun.Location = new System.Drawing.Point(242, 136);
            this.chkDryRun.Margin = new System.Windows.Forms.Padding(2);
            this.chkDryRun.Name = "chkDryRun";
            this.chkDryRun.Size = new System.Drawing.Size(133, 17);
            this.chkDryRun.TabIndex = 11;
            this.chkDryRun.Tag = "CHK_DRY_RUN";
            this.chkDryRun.Text = "&Test without extracting";
            this.chkDryRun.UseVisualStyleBackColor = true;
            // 
            // btnExtract
            // 
            this.btnExtract.Location = new System.Drawing.Point(242, 164);
            this.btnExtract.Margin = new System.Windows.Forms.Padding(2);
            this.btnExtract.Name = "btnExtract";
            this.btnExtract.Size = new System.Drawing.Size(224, 26);
            this.btnExtract.TabIndex = 12;
            this.btnExtract.Text = "Do it!";
            this.btnExtract.UseVisualStyleBackColor = true;
            // 
            // btnDonate
            // 
            this.btnDonate.BackColor = System.Drawing.Color.Transparent;
            this.btnDonate.FlatAppearance.BorderSize = 0;
            this.btnDonate.Image = global::ExtractWizard.Properties.Resources.blue_rect_paypal_26px;
            this.btnDonate.Location = new System.Drawing.Point(6, 108);
            this.btnDonate.Margin = new System.Windows.Forms.Padding(2);
            this.btnDonate.Name = "btnDonate";
            this.btnDonate.Size = new System.Drawing.Size(95, 33);
            this.btnDonate.TabIndex = 14;
            this.btnDonate.UseVisualStyleBackColor = false;
            // 
            // groupProgress
            // 
            this.groupProgress.Controls.Add(this.lblExtractedFile);
            this.groupProgress.Controls.Add(this.progressBarExtract);
            this.groupProgress.Location = new System.Drawing.Point(6, 213);
            this.groupProgress.Margin = new System.Windows.Forms.Padding(2);
            this.groupProgress.Name = "groupProgress";
            this.groupProgress.Padding = new System.Windows.Forms.Padding(2);
            this.groupProgress.Size = new System.Drawing.Size(541, 85);
            this.groupProgress.TabIndex = 15;
            this.groupProgress.TabStop = false;
            this.groupProgress.Tag = "GROUP_PROGRESS";
            this.groupProgress.Text = "Progress";
            // 
            // lblExtractedFile
            // 
            this.lblExtractedFile.AutoEllipsis = true;
            this.lblExtractedFile.Location = new System.Drawing.Point(3, 51);
            this.lblExtractedFile.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblExtractedFile.Name = "lblExtractedFile";
            this.lblExtractedFile.Size = new System.Drawing.Size(535, 17);
            this.lblExtractedFile.TabIndex = 17;
            this.lblExtractedFile.Text = "label4";
            // 
            // progressBarExtract
            // 
            this.progressBarExtract.Location = new System.Drawing.Point(3, 20);
            this.progressBarExtract.Margin = new System.Windows.Forms.Padding(2);
            this.progressBarExtract.Name = "progressBarExtract";
            this.progressBarExtract.Size = new System.Drawing.Size(535, 25);
            this.progressBarExtract.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBarExtract.TabIndex = 16;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(554, 306);
            this.Controls.Add(this.groupProgress);
            this.Controls.Add(this.groupOptions);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Akeeba eXtract Wizard";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupOptions.ResumeLayout(false);
            this.groupOptions.PerformLayout();
            this.groupProgress.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnDonate;
        public System.Windows.Forms.TextBox editPassword;
        public System.Windows.Forms.Label lblExtractToFolder;
        public System.Windows.Forms.Label lblBackupArchive;
        public System.Windows.Forms.TextBox editBackupArchive;
        public System.Windows.Forms.Label lblPassword;
        public System.Windows.Forms.TextBox editExtractToFolder;
        public System.Windows.Forms.CheckBox chkIgnoreErrors;
        public System.Windows.Forms.CheckBox chkDryRun;
        public System.Windows.Forms.Label lblExtractedFile;
        public System.Windows.Forms.ProgressBar progressBarExtract;
        public System.Windows.Forms.Button btnBrowseArchive;
        public System.Windows.Forms.Button btnExtractToFolder;
        public System.Windows.Forms.Button btnExtract;
        public System.Windows.Forms.Button btnHelp;
        public System.Windows.Forms.GroupBox groupOptions;
        public System.Windows.Forms.GroupBox groupProgress;
    }
}


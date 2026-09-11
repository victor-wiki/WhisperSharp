namespace WhisperSharp.WinApp
{
    partial class frmSetting
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
            btnChooseFolder = new Button();
            txtDataFolder = new TextBox();
            label1 = new Label();
            btnSave = new Button();
            folderBrowserDialog1 = new FolderBrowserDialog();
            chkEnableLog = new CheckBox();
            SuspendLayout();
            // 
            // btnChooseFolder
            // 
            btnChooseFolder.Location = new Point(496, 12);
            btnChooseFolder.Name = "btnChooseFolder";
            btnChooseFolder.Size = new Size(65, 23);
            btnChooseFolder.TabIndex = 7;
            btnChooseFolder.Text = "...";
            btnChooseFolder.UseVisualStyleBackColor = true;
            btnChooseFolder.Click += btnChooseFolder_Click;
            // 
            // txtDataFolder
            // 
            txtDataFolder.Location = new Point(92, 12);
            txtDataFolder.Name = "txtDataFolder";
            txtDataFolder.Size = new Size(400, 23);
            txtDataFolder.TabIndex = 6;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(9, 15);
            label1.Name = "label1";
            label1.Size = new Size(79, 17);
            label1.TabIndex = 5;
            label1.Text = "Data Folder:";
            // 
            // btnSave
            // 
            btnSave.Location = new Point(243, 108);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 8;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // chkEnableLog
            // 
            chkEnableLog.AutoSize = true;
            chkEnableLog.Location = new Point(12, 54);
            chkEnableLog.Name = "chkEnableLog";
            chkEnableLog.Size = new Size(92, 21);
            chkEnableLog.TabIndex = 9;
            chkEnableLog.Text = "Enable Log";
            chkEnableLog.UseVisualStyleBackColor = true;
            // 
            // frmSetting
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(575, 143);
            Controls.Add(chkEnableLog);
            Controls.Add(btnSave);
            Controls.Add(btnChooseFolder);
            Controls.Add(txtDataFolder);
            Controls.Add(label1);
            MaximizeBox = false;
            Name = "frmSetting";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Setting";
            Load += frmSetting_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnChooseFolder;
        private TextBox txtDataFolder;
        private Label label1;
        private Button btnSave;
        private FolderBrowserDialog folderBrowserDialog1;
        private CheckBox chkEnableLog;
    }
}
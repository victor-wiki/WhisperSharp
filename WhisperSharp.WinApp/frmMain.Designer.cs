namespace WhisperSharp.WinApp
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabControl1 = new TabControl();
            tabText = new TabPage();
            txtText = new TextBox();
            tabSRT = new TabPage();
            txtSRT = new TextBox();
            tabJSON = new TabPage();
            txtJSON = new TextBox();
            label1 = new Label();
            txtFilePath = new TextBox();
            btnOpenFile = new Button();
            label2 = new Label();
            cboLanguage = new ComboBox();
            cboModel = new ComboBox();
            label3 = new Label();
            cboAudioParser = new ComboBox();
            label4 = new Label();
            btnConvert = new Button();
            openFileDialog1 = new OpenFileDialog();
            bgWorker = new System.ComponentModel.BackgroundWorker();
            toolStrip1 = new ToolStrip();
            tsmiSetting = new ToolStripButton();
            chkShowWordTimestamps = new CheckBox();
            tabControl1.SuspendLayout();
            tabText.SuspendLayout();
            tabSRT.SuspendLayout();
            tabJSON.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tabText);
            tabControl1.Controls.Add(tabSRT);
            tabControl1.Controls.Add(tabJSON);
            tabControl1.Location = new Point(0, 112);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(800, 340);
            tabControl1.TabIndex = 1;
            // 
            // tabText
            // 
            tabText.Controls.Add(txtText);
            tabText.Location = new Point(4, 26);
            tabText.Name = "tabText";
            tabText.Padding = new Padding(3);
            tabText.Size = new Size(792, 310);
            tabText.TabIndex = 0;
            tabText.Text = "Text";
            tabText.UseVisualStyleBackColor = true;
            // 
            // txtText
            // 
            txtText.Dock = DockStyle.Fill;
            txtText.Location = new Point(3, 3);
            txtText.Multiline = true;
            txtText.Name = "txtText";
            txtText.ScrollBars = ScrollBars.Both;
            txtText.Size = new Size(786, 304);
            txtText.TabIndex = 0;
            // 
            // tabSRT
            // 
            tabSRT.Controls.Add(txtSRT);
            tabSRT.Location = new Point(4, 26);
            tabSRT.Name = "tabSRT";
            tabSRT.Size = new Size(792, 310);
            tabSRT.TabIndex = 2;
            tabSRT.Text = "SRT";
            tabSRT.UseVisualStyleBackColor = true;
            // 
            // txtSRT
            // 
            txtSRT.Dock = DockStyle.Fill;
            txtSRT.Location = new Point(0, 0);
            txtSRT.Multiline = true;
            txtSRT.Name = "txtSRT";
            txtSRT.ScrollBars = ScrollBars.Both;
            txtSRT.Size = new Size(792, 310);
            txtSRT.TabIndex = 1;
            // 
            // tabJSON
            // 
            tabJSON.Controls.Add(txtJSON);
            tabJSON.Location = new Point(4, 26);
            tabJSON.Name = "tabJSON";
            tabJSON.Padding = new Padding(3);
            tabJSON.Size = new Size(792, 310);
            tabJSON.TabIndex = 1;
            tabJSON.Text = "JSON";
            tabJSON.UseVisualStyleBackColor = true;
            // 
            // txtJSON
            // 
            txtJSON.Dock = DockStyle.Fill;
            txtJSON.Location = new Point(3, 3);
            txtJSON.Multiline = true;
            txtJSON.Name = "txtJSON";
            txtJSON.ScrollBars = ScrollBars.Both;
            txtJSON.Size = new Size(786, 304);
            txtJSON.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(2, 34);
            label1.Name = "label1";
            label1.Size = new Size(30, 17);
            label1.TabIndex = 2;
            label1.Text = "File:";
            // 
            // txtFilePath
            // 
            txtFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFilePath.Location = new Point(51, 31);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.Size = new Size(666, 23);
            txtFilePath.TabIndex = 3;
            // 
            // btnOpenFile
            // 
            btnOpenFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenFile.Location = new Point(723, 31);
            btnOpenFile.Name = "btnOpenFile";
            btnOpenFile.Size = new Size(65, 23);
            btnOpenFile.TabIndex = 4;
            btnOpenFile.Text = "...";
            btnOpenFile.UseVisualStyleBackColor = true;
            btnOpenFile.Click += btnOpenFile_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(2, 71);
            label2.Name = "label2";
            label2.Size = new Size(68, 17);
            label2.TabIndex = 5;
            label2.Text = "Language:";
            // 
            // cboLanguage
            // 
            cboLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLanguage.FormattingEnabled = true;
            cboLanguage.Items.AddRange(new object[] { "en", "zh", "ja", "th", "lo", "my", "yue " });
            cboLanguage.Location = new Point(75, 67);
            cboLanguage.Name = "cboLanguage";
            cboLanguage.Size = new Size(77, 25);
            cboLanguage.TabIndex = 6;
            // 
            // cboModel
            // 
            cboModel.DropDownStyle = ComboBoxStyle.DropDownList;
            cboModel.FormattingEnabled = true;
            cboModel.Items.AddRange(new object[] { "tiny", "base", "small", "medium", "large" });
            cboModel.Location = new Point(229, 67);
            cboModel.Name = "cboModel";
            cboModel.Size = new Size(77, 25);
            cboModel.TabIndex = 8;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(176, 71);
            label3.Name = "label3";
            label3.Size = new Size(49, 17);
            label3.TabIndex = 7;
            label3.Text = "Model:";
            // 
            // cboAudioParser
            // 
            cboAudioParser.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAudioParser.FormattingEnabled = true;
            cboAudioParser.Items.AddRange(new object[] { "FFMpeg", "NAudio" });
            cboAudioParser.Location = new Point(430, 67);
            cboAudioParser.Name = "cboAudioParser";
            cboAudioParser.Size = new Size(77, 25);
            cboAudioParser.TabIndex = 10;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(327, 71);
            label4.Name = "label4";
            label4.Size = new Size(86, 17);
            label4.TabIndex = 9;
            label4.Text = "Audio Parser:";
            // 
            // btnConvert
            // 
            btnConvert.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConvert.Location = new Point(723, 69);
            btnConvert.Name = "btnConvert";
            btnConvert.Size = new Size(65, 23);
            btnConvert.TabIndex = 11;
            btnConvert.Text = "Convert";
            btnConvert.UseVisualStyleBackColor = true;
            btnConvert.Click += btnConvert_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.Filter = "Wav file|*.wav|Mp3 file|*.mp3|Flac file|*.flac|all files|*.*";
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsmiSetting });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(800, 25);
            toolStrip1.TabIndex = 12;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsmiSetting
            // 
            tsmiSetting.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsmiSetting.ImageTransparentColor = Color.Magenta;
            tsmiSetting.Name = "tsmiSetting";
            tsmiSetting.Size = new Size(52, 22);
            tsmiSetting.Text = "Setting";
            tsmiSetting.Click += tsmiSetting_Click;
            // 
            // chkShowWordTimestamps
            // 
            chkShowWordTimestamps.AutoSize = true;
            chkShowWordTimestamps.Location = new Point(537, 69);
            chkShowWordTimestamps.Name = "chkShowWordTimestamps";
            chkShowWordTimestamps.Size = new Size(169, 21);
            chkShowWordTimestamps.TabIndex = 13;
            chkShowWordTimestamps.Text = "Show Word Timestamps";
            chkShowWordTimestamps.UseVisualStyleBackColor = true;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(chkShowWordTimestamps);
            Controls.Add(toolStrip1);
            Controls.Add(btnConvert);
            Controls.Add(cboAudioParser);
            Controls.Add(label4);
            Controls.Add(cboModel);
            Controls.Add(label3);
            Controls.Add(cboLanguage);
            Controls.Add(label2);
            Controls.Add(btnOpenFile);
            Controls.Add(txtFilePath);
            Controls.Add(label1);
            Controls.Add(tabControl1);
            Name = "frmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Audio to Text";
            Load += frmMain_Load;
            tabControl1.ResumeLayout(false);
            tabText.ResumeLayout(false);
            tabText.PerformLayout();
            tabSRT.ResumeLayout(false);
            tabSRT.PerformLayout();
            tabJSON.ResumeLayout(false);
            tabJSON.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabText;
        private TabPage tabJSON;
        private Label label1;
        private TextBox txtFilePath;
        private Button btnOpenFile;
        private Label label2;
        private ComboBox cboLanguage;
        private ComboBox cboModel;
        private Label label3;
        private ComboBox cboAudioParser;
        private Label label4;
        private Button btnConvert;
        private TabPage tabSRT;
        private OpenFileDialog openFileDialog1;
        private System.ComponentModel.BackgroundWorker bgWorker;
        private TextBox txtText;
        private TextBox txtSRT;
        private TextBox txtJSON;
        private ToolStrip toolStrip1;
        private ToolStripButton tsmiSetting;
        private CheckBox chkShowWordTimestamps;
    }
}

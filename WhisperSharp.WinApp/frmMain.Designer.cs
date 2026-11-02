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
            txtFilePath = new TextBox();
            btnOpenFile = new Button();
            label2 = new Label();
            cboLanguage = new ComboBox();
            cboModel = new ComboBox();
            label3 = new Label();
            cboAudioParser = new ComboBox();
            label4 = new Label();
            btnConvertFile = new Button();
            openFileDialog1 = new OpenFileDialog();
            toolStrip1 = new ToolStrip();
            tsmiSetting = new ToolStripButton();
            chkShowWordTimestamps = new CheckBox();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            btnConvertRecording = new Button();
            btnStartRecord = new Button();
            btnStopRecord = new Button();
            progressBar1 = new ProgressBar();
            btnPlayRecording = new Button();
            btnPlaySelectedFile = new Button();
            tabControl1.SuspendLayout();
            tabText.SuspendLayout();
            tabSRT.SuspendLayout();
            tabJSON.SuspendLayout();
            toolStrip1.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tabText);
            tabControl1.Controls.Add(tabSRT);
            tabControl1.Controls.Add(tabJSON);
            tabControl1.Location = new Point(0, 189);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(800, 360);
            tabControl1.TabIndex = 1;
            // 
            // tabText
            // 
            tabText.Controls.Add(txtText);
            tabText.Location = new Point(4, 26);
            tabText.Name = "tabText";
            tabText.Padding = new Padding(3);
            tabText.Size = new Size(792, 330);
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
            txtText.Size = new Size(786, 324);
            txtText.TabIndex = 0;
            // 
            // tabSRT
            // 
            tabSRT.Controls.Add(txtSRT);
            tabSRT.Location = new Point(4, 26);
            tabSRT.Name = "tabSRT";
            tabSRT.Size = new Size(792, 330);
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
            txtSRT.Size = new Size(792, 330);
            txtSRT.TabIndex = 1;
            // 
            // tabJSON
            // 
            tabJSON.Controls.Add(txtJSON);
            tabJSON.Location = new Point(4, 26);
            tabJSON.Name = "tabJSON";
            tabJSON.Padding = new Padding(3);
            tabJSON.Size = new Size(792, 330);
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
            txtJSON.Size = new Size(786, 324);
            txtJSON.TabIndex = 1;
            // 
            // txtFilePath
            // 
            txtFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFilePath.Location = new Point(35, 18);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.Size = new Size(498, 23);
            txtFilePath.TabIndex = 3;
            // 
            // btnOpenFile
            // 
            btnOpenFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenFile.Location = new Point(539, 18);
            btnOpenFile.Name = "btnOpenFile";
            btnOpenFile.Size = new Size(65, 24);
            btnOpenFile.TabIndex = 4;
            btnOpenFile.Text = "...";
            btnOpenFile.UseVisualStyleBackColor = true;
            btnOpenFile.Click += btnOpenFile_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 32);
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
            cboLanguage.Location = new Point(79, 28);
            cboLanguage.Name = "cboLanguage";
            cboLanguage.Size = new Size(77, 25);
            cboLanguage.TabIndex = 6;
            // 
            // cboModel
            // 
            cboModel.DropDownStyle = ComboBoxStyle.DropDownList;
            cboModel.FormattingEnabled = true;
            cboModel.Items.AddRange(new object[] { "tiny", "base", "small", "medium", "large" });
            cboModel.Location = new Point(233, 28);
            cboModel.Name = "cboModel";
            cboModel.Size = new Size(77, 25);
            cboModel.TabIndex = 8;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(180, 32);
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
            cboAudioParser.Location = new Point(434, 28);
            cboAudioParser.Name = "cboAudioParser";
            cboAudioParser.Size = new Size(77, 25);
            cboAudioParser.TabIndex = 10;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(331, 32);
            label4.Name = "label4";
            label4.Size = new Size(86, 17);
            label4.TabIndex = 9;
            label4.Text = "Audio Parser:";
            // 
            // btnConvertFile
            // 
            btnConvertFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConvertFile.Location = new Point(610, 18);
            btnConvertFile.Name = "btnConvertFile";
            btnConvertFile.Size = new Size(65, 24);
            btnConvertFile.TabIndex = 11;
            btnConvertFile.Text = "Convert";
            btnConvertFile.UseVisualStyleBackColor = true;
            btnConvertFile.Click += btnConvertFile_Click;
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
            chkShowWordTimestamps.Location = new Point(541, 30);
            chkShowWordTimestamps.Name = "chkShowWordTimestamps";
            chkShowWordTimestamps.Size = new Size(169, 21);
            chkShowWordTimestamps.TabIndex = 13;
            chkShowWordTimestamps.Text = "Show Word Timestamps";
            chkShowWordTimestamps.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btnPlaySelectedFile);
            groupBox1.Controls.Add(txtFilePath);
            groupBox1.Controls.Add(btnOpenFile);
            groupBox1.Controls.Add(btnConvertFile);
            groupBox1.Location = new Point(12, 65);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(776, 47);
            groupBox1.TabIndex = 14;
            groupBox1.TabStop = false;
            groupBox1.Text = "File";
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(btnConvertRecording);
            groupBox2.Controls.Add(btnStartRecord);
            groupBox2.Controls.Add(btnStopRecord);
            groupBox2.Controls.Add(progressBar1);
            groupBox2.Controls.Add(btnPlayRecording);
            groupBox2.Location = new Point(12, 118);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(776, 64);
            groupBox2.TabIndex = 20;
            groupBox2.TabStop = false;
            groupBox2.Text = "Recording";
            // 
            // btnConvertRecording
            // 
            btnConvertRecording.Location = new Point(183, 23);
            btnConvertRecording.Name = "btnConvertRecording";
            btnConvertRecording.Size = new Size(68, 24);
            btnConvertRecording.TabIndex = 15;
            btnConvertRecording.Text = "Convert";
            btnConvertRecording.UseVisualStyleBackColor = true;
            btnConvertRecording.Click += btnConvertRecording_Click;
            // 
            // btnStartRecord
            // 
            btnStartRecord.Location = new Point(35, 23);
            btnStartRecord.Margin = new Padding(3, 4, 3, 4);
            btnStartRecord.Name = "btnStartRecord";
            btnStartRecord.Size = new Size(68, 24);
            btnStartRecord.TabIndex = 11;
            btnStartRecord.Text = "Start";
            btnStartRecord.UseVisualStyleBackColor = true;
            btnStartRecord.Click += btnStartRecord_Click;
            // 
            // btnStopRecord
            // 
            btnStopRecord.Location = new Point(109, 23);
            btnStopRecord.Margin = new Padding(3, 4, 3, 4);
            btnStopRecord.Name = "btnStopRecord";
            btnStopRecord.Size = new Size(68, 24);
            btnStopRecord.TabIndex = 10;
            btnStopRecord.Text = "Stop";
            btnStopRecord.UseVisualStyleBackColor = true;
            btnStopRecord.Click += btnStopRecord_Click;
            // 
            // progressBar1
            // 
            progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar1.Location = new Point(414, 23);
            progressBar1.Margin = new Padding(3, 4, 3, 4);
            progressBar1.Maximum = 30;
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(261, 24);
            progressBar1.TabIndex = 13;
            // 
            // btnPlayRecording
            // 
            btnPlayRecording.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPlayRecording.Location = new Point(701, 23);
            btnPlayRecording.Margin = new Padding(3, 4, 3, 4);
            btnPlayRecording.Name = "btnPlayRecording";
            btnPlayRecording.Size = new Size(66, 24);
            btnPlayRecording.TabIndex = 12;
            btnPlayRecording.Text = "Play";
            btnPlayRecording.UseVisualStyleBackColor = true;
            btnPlayRecording.Click += btnPlay_Click;
            // 
            // btnPlaySelectedFile
            // 
            btnPlaySelectedFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPlaySelectedFile.Location = new Point(701, 18);
            btnPlaySelectedFile.Margin = new Padding(3, 4, 3, 4);
            btnPlaySelectedFile.Name = "btnPlaySelectedFile";
            btnPlaySelectedFile.Size = new Size(66, 24);
            btnPlaySelectedFile.TabIndex = 13;
            btnPlaySelectedFile.Text = "Play";
            btnPlaySelectedFile.UseVisualStyleBackColor = true;
            btnPlaySelectedFile.Click += btnPlaySelectedFile_Click;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 547);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(chkShowWordTimestamps);
            Controls.Add(toolStrip1);
            Controls.Add(cboAudioParser);
            Controls.Add(label4);
            Controls.Add(cboModel);
            Controls.Add(label3);
            Controls.Add(cboLanguage);
            Controls.Add(label2);
            Controls.Add(tabControl1);
            Name = "frmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Audio to Text";
            FormClosing += frmMain_FormClosing;
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
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
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
        private Button btnConvertFile;
        private TabPage tabSRT;
        private OpenFileDialog openFileDialog1;
        private TextBox txtText;
        private TextBox txtSRT;
        private TextBox txtJSON;
        private ToolStrip toolStrip1;
        private ToolStripButton tsmiSetting;
        private CheckBox chkShowWordTimestamps;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private Button btnStartRecord;
        private Button btnStopRecord;
        private ProgressBar progressBar1;
        private Button btnPlayRecording;
        private Button btnConvertRecording;
        private Button btnPlaySelectedFile;
    }
}

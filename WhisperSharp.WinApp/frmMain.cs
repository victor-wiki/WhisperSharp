using DatabaseManager.Core;
using OpenccNetLib;
using System.ComponentModel;
using TorchSharp;
using WhisperSharp.Helper;
using WhisperSharp.Model;
using WhisperSharp.Writer;

namespace WhisperSharp.WinApp
{
    public partial class frmMain : Form
    {
        private string filePath;

        public frmMain()
        {
            InitializeComponent();

            TextBox.CheckForIllegalCrossThreadCalls = false;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.Init();
        }

        private void Init()
        {
            this.cboLanguage.SelectedIndex = 0;
            this.cboModel.SelectedIndex = 0;
            this.cboAudioParser.SelectedIndex = 0;

            LogHelper.EnableLog = SettingManager.Setting.EnableLog;

            this.bgWorker.DoWork += this.BgWorker_DoWork;
        }

        private void BgWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            this.Convert(this.filePath);
        }

        private void Convert(string filePath)
        {
            this.Reset();

            this.txtText.Text = "processing...";

            var language = this.cboLanguage.Text;
            string device = "cpu";
            string model = this.cboModel.Text;
            string audioParser = this.cboAudioParser.Text;
            string dataFolder = SettingManager.Setting.DataFolder;
            bool showTimestamps = this.chkShowWordTimestamps.Checked;

            try
            {
                Audio.AudioParserType = audioParser == nameof(AudioParserType.NAudio) ? AudioParserType.NAudio : AudioParserType.FFMpeg;

                Whisper whisper = Whisper.load_model(model, device, dataFolder).to(device);

                var result = whisper.transcribe
                (
                    filePath, temperature: 0.0,
                    word_timestamps: showTimestamps,
                    decode_options: new Dictionary<string, dynamic>() { { "language", language } }
                );

                if (language == "zh")
                {
                    this.ConvertText(result);
                }

                if (result.GetValueOrDefault("segments", null) != null && result["segments"].Count > 0)
                {
                    List<string> textList = new List<string>();

                    foreach (Dictionary<string, dynamic> segment in result["segments"])
                    {
                        string text = segment["text"];

                        if (!string.IsNullOrEmpty(text))
                        {
                            textList.Add(text);
                        }
                    }

                    this.txtText.Text = string.Join(language == "en" ? Environment.NewLine : " ", textList);
                }
                else
                {
                    this.txtText.Text = result["text"] ?? "";
                }

                var jsonWriter = Utils.get_writer("json");

                this.txtJSON.Text = (jsonWriter as ResultWriter).get_result(result);

                var srtWriter = Utils.get_writer("srt");

                this.txtSRT.Text = (srtWriter as ResultWriter).get_result(result);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex.Message);

                MessageBox.Show(ex.Message);

                this.Reset();
            }
        }

        private void Reset()
        {
            this.tabControl1.SelectedIndex = 0;
            this.txtText.Text = "";
            this.txtJSON.Text = "";
            this.txtSRT.Text = "";
        }

        private void ConvertText(Dictionary<string, dynamic> result)
        {
            var opencc = new Opencc(OpenccConfig.T2S);

            string text = result["text"];

            if (!string.IsNullOrEmpty(text))
            {
                result["text"] = this.HandleZhText(opencc, text);
            }

            if (result.GetValueOrDefault("segments", null) != null && result["segments"].Count > 0)
            {
                foreach (Dictionary<string, dynamic> segment in result["segments"])
                {
                    string st = segment["text"];

                    if (!string.IsNullOrEmpty(st))
                    {
                        segment["text"] = this.HandleZhText(opencc, st);
                    }

                    var words = segment.GetValueOrDefault("words", null);

                    if (words != null)
                    {
                        foreach (var word in words)
                        {
                            string w = word["word"];

                            if (!string.IsNullOrEmpty(w))
                            {
                                word["word"] = this.HandleZhText(opencc, w);
                            }
                        }
                    }
                }
            }
        }

        private string HandleZhText(Opencc opencc, string text)
        {
            return opencc.Convert(text).Replace(",", "，").Replace(".", "。");
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.FileName = "";

            DialogResult result = this.openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                string filePath = this.openFileDialog1.FileName;

                this.filePath = filePath;

                this.txtFilePath.Text = filePath;
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            if (this.bgWorker.IsBusy)
            {
                MessageBox.Show("It's processing the selected file, please wait for one moment.");
                return;
            }

            string filePath = this.txtFilePath.Text;

            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Please select a file first!");
                return;
            }

            this.bgWorker.RunWorkerAsync();
        }

        private void tsmiSetting_Click(object sender, EventArgs e)
        {
            frmSetting form = new frmSetting();
            form.ShowDialog();
        }
    }
}

using DatabaseManager.Core;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OpenccNetLib;
using SharpUtility.Helper;
using TorchSharp;
using WhisperSharp.Helper;
using WhisperSharp.Model;
using WhisperSharp.WinApp.Adapter;
using WhisperSharp.Writer;

namespace WhisperSharp.WinApp
{
    public partial class frmMain : Form
    {
        private string filePath;
        private const int rate = Audio.SAMPLE_RATE;
        private const int channels = 1;
        private IWaveIn captureDevice;
        private WaveFileWriter writer;
        private MMDevice device;
        private string outputFolder => Path.Combine(FilePathHelper.Location, "TEMP");
        private bool loadingDevices;
        private bool isRecording;
        private string tempAudioFileName;
        private bool devicesChangedWhileRecording;
        private readonly MMDeviceEnumerator deviceEnumerator = new();
        private IWavePlayer wavePlayer;
        private AudioFileReader audioFileReader;

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
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            this.cboLanguage.SelectedIndex = 0;
            this.cboModel.SelectedIndex = 0;
            this.cboAudioParser.SelectedIndex = 0;

            this.LoadDevice();

            LogHelper.EnableLog = SettingManager.Setting.EnableLog;
        }

        private void Convert(string filePath, bool isFile = false)
        {
            this.Reset();

            this.txtText.Text = "processing...";

            Task.Run(() => this.DoConvert(filePath, isFile));
        }

        private void DoConvert(string filePath, bool isFile = false)
        {
            var language = this.cboLanguage.Text;
            string device = "cpu";
            string model = this.cboModel.Text;
            string audioParser = this.cboAudioParser.Text;
            string dataFolder = SettingManager.Setting.DataFolder;
            bool showTimestamps = this.chkShowWordTimestamps.Checked;
            Button button = isFile ? this.btnConvertFile : this.btnConvertRecording;

            button.BeginInvoke(() => { button.Enabled = false; });

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
            finally
            {
                button.BeginInvoke(() => { button.Enabled = true; });
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

        private void btnConvertFile_Click(object sender, EventArgs e)
        {
            string filePath = this.txtFilePath.Text;

            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Please select a file first!");
                return;
            }

            this.Convert(filePath, true);
        }

        private void tsmiSetting_Click(object sender, EventArgs e)
        {
            frmSetting form = new frmSetting();
            form.ShowDialog();
        }

        #region Recording

        private void btnStartRecord_Click(object sender, EventArgs e)
        {
            this.Cleanup();

            try
            {
                this.captureDevice = this.CreateCaptureDevice();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to start recording");
                return;
            }

            if (this.device != null)
            {
                try
                {
                    this.device.AudioEndpointVolume.Mute = false;
                }
                catch (Exception ex)
                {
                }
            }

            try
            {
                this.tempAudioFileName = $"{Path.GetRandomFileName()}.wav";

                this.writer = new WaveFileWriter(this.GetTempAudioFilePath(), captureDevice.WaveFormat);

                this.captureDevice.StartRecording();

                this.SetControlStates(true);
            }
            catch (Exception ex)
            {
                this.Cleanup();
                this.SetControlStates(false);

                MessageBox.Show(ex.Message, "Unable to start recording");
            }
        }

        private void btnStopRecord_Click(object sender, EventArgs e)
        {
            this.StopRecording();
        }

        private void btnConvertRecording_Click(object sender, EventArgs e)
        {
            string tempFilePath = this.GetTempAudioFilePath();

            if (!File.Exists(tempFilePath))
            {
                return;
            }

            this.Convert(tempFilePath, false);
        }

        private IWaveIn CreateCaptureDevice()
        {
            var requestedFormat = new WaveFormat(rate, channels);

            var builder = new WasapiRecorderBuilder().WithDevice(this.device).WithFormat(requestedFormat);

            IWaveIn device = new WasapiRecorderAdapter(builder.Build());

            device.DataAvailable += OnDataAvailable;
            device.RecordingStopped += OnRecordingStopped;

            return device;
        }

        private string GetTempAudioFilePath()
        {
            if (this.tempAudioFileName == null)
            {
                return null;
            }

            return Path.Combine(this.outputFolder, this.tempAudioFileName);
        }

        private void LoadDevice()
        {
            this.loadingDevices = true;

            try
            {
                var flow = DataFlow.Capture;
                var devices = this.deviceEnumerator.EnumerateAudioEndPoints(flow, DeviceState.Active).ToArray();

                if (devices.Length > 0)
                {
                    try
                    {
                        this.device = deviceEnumerator.GetDefaultAudioEndpoint(flow, Role.Console);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Audio Recording Device not found!");
                    }
                }
            }
            finally
            {
                this.loadingDevices = false;
            }

            this.OnDeviceChanged();
        }

        private void OnDeviceChanged()
        {
            if (this.loadingDevices)
            {
                return;
            }

            this.Cleanup();
        }

        private void Cleanup()
        {
            if (this.captureDevice != null)
            {
                this.captureDevice.DataAvailable -= this.OnDataAvailable;
                this.captureDevice.RecordingStopped -= this.OnRecordingStopped;
                this.captureDevice.Dispose();
                this.captureDevice = null;
            }

            this.FinalizeWaveFile();
        }

        private void FinalizeWaveFile()
        {
            this.writer?.Dispose();
            this.writer = null;
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<WaveInEventArgs>(this.OnDataAvailable), sender, e);

                return;
            }

            if (this.writer == null)
            {
                return;
            }

            this.writer.Write(e.Buffer, 0, e.BytesRecorded);

            int secondsRecorded = (int)(writer.Length / writer.WaveFormat.AverageBytesPerSecond);

            if (secondsRecorded >= 30)
            {
                this.StopRecording();
            }
            else
            {
                this.progressBar1.Value = secondsRecorded;
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<StoppedEventArgs>(OnRecordingStopped), sender, e);
                return;
            }

            this.FinalizeWaveFile();
            this.progressBar1.Value = 0;

            if (e.Exception != null)
            {
                this.Cleanup();

                MessageBox.Show($"A problem was encountered during recording: {e.Exception.Message}", "Recording Error");
            }

            this.SetControlStates(false);
        }

        private void SetControlStates(bool isRecording)
        {
            this.isRecording = isRecording;

            if (!this.isRecording && this.devicesChangedWhileRecording)
            {
                this.devicesChangedWhileRecording = false;
            }
        }

        private void StopRecording()
        {
            this.captureDevice?.StopRecording();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            string filePath = this.GetTempAudioFilePath();

            if (filePath == null)
            {
                return;
            }

            if (File.Exists(filePath))
            {
                this.PlayFile(filePath);
            }
            else
            {
                MessageBox.Show("File not found!");
            }
        }

        private void PlayFile(string filePath)
        {
            if (this.wavePlayer == null)
            {
                this.wavePlayer = new WaveOut();

                this.wavePlayer.PlaybackStopped += this.WavePlayer_PlaybackStopped;
            }

            this.audioFileReader = new AudioFileReader(filePath);

            var fadeInOut = new FadeInOutSampleProvider(this.audioFileReader);

            this.wavePlayer.Init(fadeInOut);
            this.wavePlayer.Play();
        }

        private void WavePlayer_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            this.DisposePlayer();
        }

        private void DisposePlayer()
        {
            try
            {
                if (this.wavePlayer != null)
                {
                    this.wavePlayer.Stop();
                    this.wavePlayer.Dispose();
                    this.wavePlayer = null;
                }

                if (this.audioFileReader != null)
                {
                    this.audioFileReader.Dispose();
                    this.audioFileReader = null;
                }
            }
            catch (Exception ex)
            {
            }
        }
        #endregion

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.DisposePlayer();

            if (Directory.Exists(this.outputFolder))
            {
                var files = new DirectoryInfo(this.outputFolder).GetFiles();

                foreach (var file in files)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        private void btnPlaySelectedFile_Click(object sender, EventArgs e)
        {
            string filePath = this.txtFilePath.Text.Trim();

            if (File.Exists(filePath))
            {
                this.PlayFile(filePath);
            }
        }
    }
}

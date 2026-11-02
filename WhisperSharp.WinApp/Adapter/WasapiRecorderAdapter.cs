using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace WhisperSharp.WinApp.Adapter
{
    internal sealed class WasapiRecorderAdapter : IWaveIn
    {
        private readonly WasapiRecorder recorder;

        public event EventHandler<WaveInEventArgs> DataAvailable;
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        public WaveFormat WaveFormat
        {
            get => recorder.WaveFormat;
            set => throw new NotSupportedException(
                "Set the capture format via WasapiRecorderBuilder.WithFormat() before Build().");
        }

        public WasapiRecorderAdapter(WasapiRecorder recorder)
        {
            this.recorder = recorder;

            recorder.DataAvailable += OnDataAvailable;
            recorder.RecordingStopped += OnRecordingStopped;
        }       

        public void StartRecording() => recorder.StartRecording();
        public void StopRecording() => recorder.StopRecording();

        private void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
        {
            var handler = this.DataAvailable;

            if (handler == null)
            {
                return;
            }

            var copy = buffer.ToArray();

            handler(this, new WaveInEventArgs(copy, copy.Length));
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            this.RecordingStopped?.Invoke(this, e);
        }

        public void Dispose()
        {
            this.recorder.DataAvailable -= OnDataAvailable;
            this.recorder.RecordingStopped -= OnRecordingStopped;
            this.recorder.Dispose();
        }
    }
}

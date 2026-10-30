using FFMpegCore;
using FFMpegCore.Pipes;
using NAudio.SoundFile;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NumSharp;
using SharpUtility.Helper;
using TorchSharp;
using WhisperSharp.Helper;
using WhisperSharp.Model;
using static TorchSharp.torch;

namespace WhisperSharp
{
    public class Audio
    {
        #region hard-coded audio hyperparameters
        public const int SAMPLE_RATE = 16000;
        public const int N_FFT = 400;
        public const int HOP_LENGTH = 160;
        public const int CHUNK_LENGTH = 30;
        public const int N_SAMPLES = CHUNK_LENGTH * SAMPLE_RATE;  // 480000 samples in a 30-second chunk
        public static readonly int N_FRAMES = Utils.exact_div(N_SAMPLES, HOP_LENGTH);  // 3000 frames in a mel spectrogram input
        public const int N_SAMPLES_PER_TOKEN = HOP_LENGTH * 2; //the initial convolutions has stride 2
        public static readonly int FRAMES_PER_SECOND = Utils.exact_div(SAMPLE_RATE, HOP_LENGTH); //10ms per audio frame
        public static readonly int TOKENS_PER_SECOND = Utils.exact_div(SAMPLE_RATE, N_SAMPLES_PER_TOKEN); //20ms per audio token; 
        #endregion

        public static AudioParserType AudioParserType { get; set; }

        /// <summary>
        /// Open an audio file and read as mono waveform, resampling as necessary
        /// </summary>
        /// <param name="file">The audio file to open</param>
        /// <param name="sr">The sample rate to resample the audio if necessary</param>
        /// <returns>A NumPy array containing the audio waveform, in float32 dtype.</returns>
        public static NDArray load_audio(string file, int sr = SAMPLE_RATE)
        {
            byte[] bytes = null;

            if(AudioParserType == AudioParserType.None || AudioParserType == AudioParserType.FFMpeg)
            {
                using (var ms = new MemoryStream())
                {
                    var args = FFMpegArguments
                           .FromFileInput(file, true, options => options.WithCustomArgument("-nostdin -threads 0"))
                           .OutputToPipe(new StreamPipeSink(ms), options => options.WithCustomArgument($"-f s16le -ac 1 -acodec pcm_s16le -ar {sr}")
                       );

                    args.ProcessSynchronously();

                    bytes = ms.ToArray();                   
                }
            }
            else if(AudioParserType == AudioParserType.NAudio)
            {
                using (var reader = new SoundFileReader(file))
                {
                    var resampler = new WdlResamplingSampleProvider(reader, sr);

                    ISampleProvider mono = resampler.WaveFormat.Channels == 1 ?
                        resampler : new StereoToMonoSampleProvider(resampler) { LeftVolume = 0.5f, RightVolume = 0.5f };

                    IWaveProvider wave16 = new SampleToWaveProvider16(mono);

                    using (var ms = new MemoryStream())
                    {
                        byte[] buffer = new byte[8192];

                        int read;

                        while ((read = wave16.Read(buffer.AsSpan())) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }                       

                        bytes = ms.ToArray();
                    }
                }                
            }

            return np.frombuffer(bytes, NPTypeCode.Int16).flatten().astype(np.float32) / 32768.0;
        }

        /// <summary>
        /// Pad or trim the audio array to N_SAMPLES, as expected by the encoder.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="length"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        public static dynamic pad_or_trim(dynamic array, int length = N_SAMPLES, int axis = -1)
        {
            if (torch.is_tensor(array))
            {
                var arr = array as Tensor;

                if (GetShapeValue(arr.shape, axis) > length)
                {
                    array = arr.index_select(dim: axis, index: torch.arange(length, device: array.device));
                }

                if (GetShapeValue(arr.shape, axis) < length)
                {
                    var pad_widths = new List<(long, long)>();

                    for (int i = 0; i < arr.ndim; i++)
                    {
                        pad_widths.Add((0, 0));
                    }

                    pad_widths[axis == -1 ? pad_widths.Count - 1 : axis] = (0, length - GetShapeValue(arr.shape, axis));

                    pad_widths.Reverse();

                    array = nn.functional.pad(array, pad_widths.Select(item => new[] { item.Item1, item.Item2 }).SelectMany(item => item).ToArray());
                }
            }
            else
            {
                if (GetShapeValue(array, axis) > length)
                {
                    array = array.take(indices: RangeHelper.GetRange(length), axis: axis);
                }

                if (GetShapeValue(array, axis) < length)
                {
                    var pad_widths = new List<(int, int)>();

                    for (int i = 0; i < array.ndim; i++)
                    {
                        pad_widths.Add((0, 0));
                    }

                    pad_widths[axis == -1 ? pad_widths.Count - 1 : axis] = (0, (int)(length - GetShapeValue(array, axis)));

                    array = np.pad(array as NDArray, ArrayHelper.ToRectangular(pad_widths.Select(item => new int[] { item.Item1, item.Item2 }).ToArray()));
                }
            }

            return array;
        }

        public static long GetShapeValue(long[] shape, int axis = -1)
        {
            if (axis == -1)
            {
                return shape.LastOrDefault();
            }

            return shape[axis];
        }

        /// <summary>
        /// load the mel filterbank matrix for projecting STFT into a Mel spectrogram.
        /// Allows decoupling librosa dependency; saved using:
        /// 
        /// np.savez_compressed(
        ///   "mel_filters.npz",
        ///    mel_80=librosa.filters.mel(sr=16000, n_fft=400, n_mels=80),
        ///    mel_128=librosa.filters.mel(sr=16000, n_fft=400, n_mels=128),
        /// )
        /// </summary>
        /// <param name="device"></param>
        /// <param name="n_mels"></param>
        /// <returns></returns>
        public static Tensor mel_filters(string device, int n_mels)
        {
            var filters_path = FilePathHelper.GetAssetsFilePath($"mel_{n_mels}.npy");

            var arr = np.load(filters_path) as NDArray;

            return torch.from_array(arr.ToMuliDimArray<float>() as float[,], device);
        }

        /// <summary>
        /// Compute the log-Mel spectrogram of
        /// </summary>
        /// <param name="audio">Union[str, np.ndarray, torch.Tensor], shape = (*)
        /// The path to audio or either a NumPy array or Tensor containing the audio waveform in 16 kHz
        /// </param>
        /// <param name="n_mels">The number of Mel-frequency filters, only 80 and 128 are supported</param>
        /// <param name="padding">Number of zero samples to pad to the right</param>
        /// <param name="device">Optional[Union[str, torch.device]]
        /// If given, the audio tensor is moved to this device before STFT
        /// </param>
        /// <returns>torch.Tensor, shape = (n_mels, n_frames)
        /// A Tensor that contains the Mel spectrogram
        /// </returns>
        public static Tensor log_mel_spectrogram(dynamic audio, int n_mels = 80, int padding = 0, string device = null)
        {
            if (!torch.is_tensor(audio))
            {
                if (audio is string str)
                {
                    audio = load_audio(str);
                }

                audio = torch.tensor((audio as NDArray).ToArray<float>());
            }

            if (device != null)
            {
                audio = audio.to(device);
            }

            if (padding > 0)
            {
                audio = nn.functional.pad(audio, ((long)0, (long)padding));
            }

            var window = torch.hann_window(N_FFT).to(audio.device);
            Tensor stft = torch.stft(audio, N_FFT, HOP_LENGTH, window: window, return_complex: true);
            var magnitudes = stft[TensorIndex.Ellipsis, ..(int)(stft.shape[1] - 1)].abs().pow(2);

            Tensor filters = mel_filters(audio.device.ToString(), n_mels);
            var mel_spec = torch.matmul(filters, magnitudes);

            var log_spec = torch.clamp(mel_spec, min: 1e-10).log10();
            log_spec = torch.maximum(log_spec, log_spec.max() - 8.0);
            log_spec = (log_spec + 4.0) / 4.0;

            return log_spec;
        }
    }
}

using static TorchSharp.torch;

namespace WhisperSharp.Model
{
    public class DecodingResult
    {
        public Tensor audio_features { get; set; }
        public string language { get; set; }
        public dynamic language_probs { get; set; }
        public List<int> tokens { get; set; }
        public string text { get; set; } = "";
        public float avg_logprob { get; set; } = float.NaN;
        public float no_speech_prob { get; set; } = float.NaN;
        public float temperature { get; set; } = float.NaN;
        public float compression_ratio { get; set; } = float.NaN;
    }
}

namespace WhisperSharp.Model
{
    public class DecodingOptions
    {
        /// <summary>
        /// whether to perform X->X "transcribe" or X->English "translate"
        /// </summary>
        public string task { get; set; } = "transcribe";

        /// <summary>
        /// language that the audio is in; uses detected language if None
        /// </summary>
        public string language { get; set; }

        /// <summary>
        /// sampling-related options
        /// </summary>
        public float temperature { get; set; }

        /// <summary>
        /// maximum number of tokens to sample
        /// </summary>
        public int? sample_len { get; set; }

        /// <summary>
        /// number of independent sample trajectories, if t > 0
        /// </summary>
        public int? best_of { get; set; }
        public int? beam_size { get; set; }  // number of beams in beam search, if t == 0
        public float? patience { get; set; }  // patience in beam search (arxiv:2204.05424)

        /// <summary>
        /// "alpha" in Google NMT, or None for length norm, when ranking generations
        /// to select which to return among the beams or best-of-N samples
        /// </summary>
        public float? length_penalty { get; set; }

        /// <summary>
        /// text or tokens to feed as the prompt or the prefix; for more info:
        /// https://github.com/openai/whisper/discussions/117//discussioncomment-3727051
        /// </summary>
        public dynamic prompt { get; set; }  // for the previous context

        /// <summary>
        /// to prefix the current context
        /// </summary>
        public dynamic prefix { get; set; }

        /// <summary>
        /// list of tokens ids (or comma-separated token ids) to suppress
        /// "-1" will suppress a set of symbols as defined in `tokenizer.non_speech_tokens()`
        /// </summary>
        public dynamic suppress_tokens { get; set; } = "-1";
        public bool suppress_blank { get; set; } = true; // this will suppress blank outputs

        /// <summary>
        /// timestamp sampling options
        /// </summary>
        public bool without_timestamps { get; set; } // use <|notimestamps|> to sample text tokens only
        public float? max_initial_timestamp { get; set; } = 1.0f;

        /// <summary>
        /// implementation details
        /// </summary>
        public bool fp16 { get; set; } = true; // use fp16 for most of the calculation
    }
}

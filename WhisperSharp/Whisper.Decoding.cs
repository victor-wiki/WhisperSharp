using NumSharp;
using TorchSharp;
using WhisperSharp.Helper;
using WhisperSharp.Model;
using static TorchSharp.torch;

namespace WhisperSharp
{
    public partial class Whisper
    {
        /// <summary>
        /// Detect the spoken language in the audio, and return them as list of strings, along with the ids
        /// of the most probable language tokens and the probability distribution over all language tokens.
        /// This is performed outside the main decode loop in order to not interfere with kv-caching.
        /// </summary>
        /// <param name="mel"></param>
        /// <param name="tokenizer"></param>
        /// <returns>
        /// language_tokens : Tensor, shape = (n_audio,)
        ///                   ids of the most probable language tokens, which appears after the startoftranscript token.
        ///                   
        /// language_probs : List[Dict[str, float]], length = n_audio
        ///                  list of dictionaries containing the probability distribution over all languages.
        /// </returns>
        public (Tensor Tokens, List<Dictionary<string, dynamic>> Probs) detect_language(Tensor mel, Tokenizer tokenizer = null)
        {
            if (tokenizer == null)
            {
                tokenizer = new Tokenizer(this.is_multilingual, num_languages: this.num_languages);
            }

            if (tokenizer.language == null || !tokenizer.sot_sequence.Contains(tokenizer.language_token))
            {
                throw new Exception("This model doesn't have language tokens so it can't perform lang id");
            }

            var single = mel.ndim == 2;

            if (single)
                mel = mel.unsqueeze(0);

            //skip encoder forward pass if already-encoded audio features were given
            var lastTwoItems = mel.shape.Skip(mel.shape.Length - 2).ToArray();

            if (!(lastTwoItems[0] == this.dims.n_audio_ctx && lastTwoItems[1] == this.dims.n_audio_state))
            {
                mel = this.encoder.forward(mel);
            }

            //forward pass using a single token, startoftranscript
            var n_audio = mel.shape[0];

            int[,] arr = new int[3, 1];

            for (var i = 1; i < n_audio; i++)
            {
                arr[i - 1, 0] = tokenizer.sot.Value;
            }

            var x = torch.tensor(arr).to(mel.device);  // [n_audio, 1]
            var logits = this.logits(x, mel)[TensorIndex.Colon, 0];

            //collect detected languages; suppress all non-language tokens
            var mask = torch.ones(logits.shape.LastOrDefault(), dtype: torch.@bool);

            mask[tokenizer.all_language_tokens.ToArray()].fill_(false);
            logits[TensorIndex.Colon, mask] = -np.inf;

            var language_tokens = logits.argmax(dim: -1);
            var language_token_probs = logits.softmax(dim: -1).cpu();

            List<Dictionary<string, dynamic>> language_probs = new List<Dictionary<string,dynamic>>();

            foreach (var i in ArrayHelper.GetRange((int)n_audio))
            {
                Dictionary<string, dynamic> dict = new Dictionary<string, dynamic>();

                var length = Math.Min(tokenizer.all_language_tokens.Count, tokenizer.all_language_codes.Count);

                for (var j = 0; j < length; j++)
                {
                    var c = tokenizer.all_language_codes[j];

                    dict.Add(c, language_token_probs[i, j].item<float>());
                }

                language_probs.Add(dict);
            }

            if (single)
            {
                language_tokens = language_tokens[0];
                language_probs = new List<Dictionary<string, dynamic>>() { language_probs[0] };
            }

            return (language_tokens, language_probs);
        }

        /// <summary>
        /// Performs decoding of 30-second audio segment(s), provided as Mel spectrogram(s).
        /// </summary>
        /// <param name="mel">torch.Tensor, shape = (80, 3000) or (*, 80, 3000)
        ///  A tensor containing the Mel spectrogram(s)
        /// </param>
        /// <param name="options">A dataclass that contains all necessary options for decoding 30-second segments</param>
        /// <param name="kwargs"></param>
        /// <returns>The result(s) of decoding contained in `DecodingResult` dataclass instance(s)</returns>
        public List<DecodingResult> decode(Tensor mel, DecodingOptions options = null, Dictionary<string, object> kwargs = null)
        {
            options ??= new DecodingOptions();

            bool single = mel.ndim == 2;

            if (single)
            {
                mel = mel.unsqueeze(0);
            }

            if (kwargs != null && kwargs.Count > 0)
            {
                ObjectHelper.CopyDictionaryToObject(kwargs, options, true);
            }

            var result = new DecodingTask(this, options).run(mel);

            return result;
        }
    }
}

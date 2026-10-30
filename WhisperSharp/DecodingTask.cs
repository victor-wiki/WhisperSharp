using NumSharp;
using TorchSharp;
using WhisperSharp.Decoder;
using WhisperSharp.Extension;
using WhisperSharp.Filter;
using WhisperSharp.Helper;
using WhisperSharp.Model;
using static TorchSharp.torch;

namespace WhisperSharp
{
    public class DecodingTask
    {
        private Inference inference;
        private SequenceRanker sequence_ranker;
        private TokenDecoder decoder;
        private List<LogitFilter> logit_filters;
        private Whisper model = null;
        private int n_group;
        private int n_ctx;
        private int sample_len;
        private List<int> sot_sequence;
        private Tokenizer tokenizer;
        private DecodingOptions options;
        private List<int> initial_tokens = null;
        private int sample_begin;
        private int sot_index;

        public DecodingTask(Whisper model, DecodingOptions options)
        {
            this.model = model;

            var language = options.language ?? "en";

            this.tokenizer = new Tokenizer(
                model.is_multilingual,
                num_languages: model.num_languages,
                language: language,
                task: options.task
            );

            this.options = this._verify_options(options);

            this.n_group = options.beam_size ?? options.best_of ?? 1;
            this.n_ctx = model.dims.n_text_ctx;
            this.sample_len = options.sample_len ?? (int)Math.Floor(model.dims.n_text_ctx / 2.0);

            this.sot_sequence = tokenizer.sot_sequence;

            if (this.options.without_timestamps)
            {
                this.sot_sequence = tokenizer.sot_sequence_including_notimestamps;
            }

            this.initial_tokens = this._get_initial_tokens();
            this.sample_begin = this.initial_tokens.Count;
            this.sot_index = this.initial_tokens.IndexOf(tokenizer.sot.Value);

            // inference: implements the forward pass through the decoder, including kv caching
            this.inference = new PyTorchInference(model, this.initial_tokens.Count);

            // sequence ranker: implements how to rank a group of sampled sequences
            this.sequence_ranker = new MaximumLikelihoodRanker(options.length_penalty);

            //decoder: implements how to select the next tokens, given the autoregressive distribution
            if (options.beam_size != null)
            {
                this.decoder = new BeamSearchDecoder(
                  options.beam_size.Value, tokenizer.eot.Value, this.inference, options.patience
               );
            }
            else
            {
                this.decoder = new GreedyDecoder(options.temperature, tokenizer.eot.Value);
            }

            //logit filters: applies various rules to suppress or penalize certain tokens
            this.logit_filters = new List<LogitFilter>();

            if (this.options.suppress_blank)
                this.logit_filters.Add(new SuppressBlank(this.tokenizer, this.sample_begin));
            if (this.options.suppress_tokens != "0")
                this.logit_filters.Add(new SuppressTokens(this._get_suppress_tokens()));

            if (!options.without_timestamps)
            {
                var precision = Audio.CHUNK_LENGTH * 1.0 / model.dims.n_audio_ctx; // usually 0.02 seconds
                int? max_initial_timestamp_index = null;

                if (options.max_initial_timestamp.HasValue)
                {
                    max_initial_timestamp_index = (int)Math.Round((double)(this.options.max_initial_timestamp / precision));
                }

                this.logit_filters.Add(new ApplyTimestampRules(tokenizer, this.sample_begin, max_initial_timestamp_index));
            }
        }

        private DecodingOptions _verify_options(DecodingOptions options)
        {
            if (options.beam_size != null && options.best_of != null)
            {
                throw new Exception("beam_size and best_of can't be given together");
            }

            if (options.temperature == 0)
            {
                if (options.best_of != null)
                {
                    throw new Exception("best_of with greedy sampling (T=0) is not compatible");
                }
            }

            if (options.patience != null && options.beam_size == null)
            {
                throw new Exception("patience requires beam_size to be given");
            }

            if (options.length_penalty != null && !(
                0 <= options.length_penalty && options.length_penalty <= 1
            ))
            {
                throw new Exception("length_penalty (alpha) should be a value between 0 and 1");
            }

            return options;
        }

        private List<int> _get_initial_tokens()
        {
            var tokens = this.sot_sequence;

            var prefix = this.options.prefix;

            if (prefix != null)
            {
                List<int> prefix_tokens = prefix is string ? this.tokenizer.encode(" " + prefix.Trim()) : prefix as List<int>;

                if (this.sample_len != 0)
                {
                    var max_prefix_len = (int)Math.Floor(this.n_ctx / 2.0) - this.sample_len;
                    prefix_tokens = prefix_tokens.Skip(prefix_tokens.Count - max_prefix_len).ToList();
                }

                tokens = Enumerable.Concat(tokens, prefix_tokens).ToList();
            }

            var prompt = this.options.prompt;

            if (prompt != null)
            {
                List<int> prompt_tokens = prompt is string ? this.tokenizer.encode(" " + prompt.Trim()) : prompt as List<int>;

                if (prompt_tokens != null)
                {
                    tokens = Enumerable.Concat(Enumerable.Concat(new int[] { this.tokenizer.sot_prev.Value }
                      , prompt_tokens.Skip(prompt_tokens.Count - (int)(Math.Floor(this.n_ctx / 2.0) - 1)).ToArray()).ToList()
                      , tokens).ToList();
                }
            }

            return tokens;
        }

        private List<int> _get_suppress_tokens()
        {
            List<int> suppress_tokens = new List<int>();

            if (this.options.suppress_tokens is string str)
            {
                suppress_tokens = str.Split('.').Select(item => int.Parse(item)).ToList();
            }
            else
            {
                suppress_tokens.AddRange(this.options.suppress_tokens as List<int>);
            }

            if (suppress_tokens.Contains(-1))
            {
                suppress_tokens = suppress_tokens.Where(item => item > 0).ToList();
                suppress_tokens.AddRange(this.tokenizer.non_speech_tokens);
            }
            else if (suppress_tokens == null || suppress_tokens.Count == 0)
            {
                suppress_tokens = new List<int>();
            }

            suppress_tokens.AddRange(
               new int[]  {this.tokenizer.transcribe.Value,
                this.tokenizer.translate.Value,
                this.tokenizer.sot.Value,
                this.tokenizer.sot_prev.Value,
                this.tokenizer.sot_lm.Value }
                );

            if (this.tokenizer.no_speech != null)
            {
                //no-speech probability is collected separately
                suppress_tokens.Add(this.tokenizer.no_speech.Value);
            }

            suppress_tokens.Sort();

            return suppress_tokens;
        }

        private Tensor _get_audio_features(Tensor mel)
        {
            if (this.options.fp16)
            {
                mel = mel.half();
            }

            Tensor audio_features = null;

            if (mel.shape[1] == this.model.dims.n_audio_ctx && mel.shape[2] == this.model.dims.n_audio_state)
            {
                //encoded audio features are given; skip audio encoding
                audio_features = mel;
            }
            else
            {
                audio_features = this.model.encoder.forward(mel);
            }

            if (audio_features.dtype != (this.options.fp16 ? torch.float16 : torch.float32))
            {
                throw new Exception($"audio_features has an incorrect dtype: {audio_features.dtype}");
            }

            return audio_features;
        }

        private (List<string>, List<Dictionary<string, dynamic>>) _detect_language(Tensor audio_features, Tensor tokens)
        {
            var languages = new List<string>();

            for (var i = 1; i <= audio_features.shape[0]; i++)
            {
                languages.Add(this.options.language);
            }

            List<Dictionary<string, dynamic>> lang_probs = null;

            if (this.options.language == null || this.options.task == "lang_id")
            {
                var result = this.model.detect_language(audio_features, this.tokenizer);

                lang_probs = result.Probs;

                languages = new List<string>();

                foreach (var lang_prob in result.Probs)
                {
                    var maxValue = lang_prob.Values.Max();
                    var maxKey = lang_prob.FirstOrDefault(item => item.Value == maxValue).Key;

                    languages.Add(maxKey);
                }

                if (this.options.language == null)
                {
                    tokens[TensorIndex.Colon, this.sot_index + 1] = result.Tokens;  // write language tokens
                }
            }

            return (languages, lang_probs);
        }

        private (Tensor Tokens, Tensor SumLogprobs, List<float> NoSpeechProbs) _main_loop(Tensor audio_features, Tensor tokens)
        {
            var n_batch = tokens.shape[0];
            Tensor sum_logprobs = torch.zeros(n_batch, device: audio_features.device);
            var no_speech_probs = new List<float>();

            for (var i = 1; i < n_batch; i++)
            {
                no_speech_probs.Add((float)np.nan);
            }

            Tensor logits = null;

            try
            {
                for (var i = 0; i < sample_len; i++)
                {
                    logits = this.inference.logits(tokens, audio_features);

                    if (i == 0 && this.tokenizer.no_speech != null) //save no_speech_probs
                    {
                        var probs_at_sot = logits[TensorIndex.Colon, this.sot_index].@float().softmax(dim: -1);
                        no_speech_probs = TorchHelper.GetFloatArray(probs_at_sot[TensorIndex.Colon, this.tokenizer.no_speech.Value].tolist()).ToList();
                    }

                    //now we need to consider the logits at the last token only
                    logits = logits[TensorIndex.Colon, -1];

                    //apply the logit filters, e.g. for suppressing or applying penalty to
                    foreach (var logit_filter in this.logit_filters)
                    {
                        logit_filter.apply(logits, tokens);
                    }             

                    //expand the tokens tensor with the selected next tokens
                    (Tensor Tokens, bool Completed) update = this.decoder.update(tokens,ref logits, ref sum_logprobs);

                    tokens = update.Tokens;
                    var completed = update.Completed;

                    if (completed || tokens.shape.LastOrDefault() > this.n_ctx)
                    {
                        break;
                    }
                }
            }
            finally
            {
                this.inference.cleanup_caching();
            }

            return (tokens, sum_logprobs, no_speech_probs);
        }

        public List<DecodingResult> run(Tensor mel)
        {
            List<DecodingResult> result = new List<DecodingResult>();

            this.decoder.reset();

            Tokenizer tokenizer = this.tokenizer;
            long n_audio = mel.shape[0];

            Tensor audio_features = this._get_audio_features(mel); // encoder forward pass

            int[,] array = new int[1, this.initial_tokens.Count];

            for (var i = 0; i < this.initial_tokens.Count; i++)
            {
                array[0, i] = this.initial_tokens[i];
            }

            Tensor tokens = torch.tensor(array).repeat(n_audio, 1);

            //detect language if requested, overwriting the language token
            var (languages, language_probs) = this._detect_language(audio_features, tokens);

            if (this.options.task == "lang_id")
            {
                foreach (var language in languages)
                {
                    result.Add(new DecodingResult() { audio_features = audio_features, language = language, language_probs = language_probs });
                }
            }

            // repeat text tensors by the group size, for beam search or best-of-n sampling
            tokens = tokens.repeat_interleave(this.n_group, dim: 0).to(audio_features.device);

            // call the main sampling loop
            (Tensor Tokens, Tensor SumLogprobs, List<float> NoSpeechProbs) loop = this._main_loop(audio_features, tokens);

            tokens = loop.Tokens;
            var sum_logprobs = loop.SumLogprobs;
            var no_speech_probs = loop.NoSpeechProbs;

            // reshape the tensors to have (n_audio, n_group) as the first two dimensions
            audio_features = audio_features.GetStepOnes(this.n_group);
            no_speech_probs = TorchHelper.GetStepOnes(no_speech_probs, this.n_group).ToList();

            tokens = tokens.reshape(n_audio, this.n_group, -1);
            sum_logprobs = sum_logprobs.reshape(n_audio, this.n_group);

            // get the final candidates for each group, and slice between the first sampled token and EOT
            (dynamic Tokens, List<List<float>> SumLogprobs) finalize = this.decoder.finalize(tokens, sum_logprobs);

            var tokenList = finalize.Tokens;
            var sumLogprobList = finalize.SumLogprobs;

            List<List<Tensor>> tokenList2 = new List<List<Tensor>>();

            var start = this.sample_begin;

            if (tokenList is Tensor t1)
            {
                for (int i = 0; i < t1.shape[0]; i++)
                {
                    List<Tensor> list = new List<Tensor>();

                    for (var j = 0; j < t1.shape[1]; j++)
                    {
                        var t = t1[i][j];

                        var end = (int)(t == tokenizer.eot).nonzero()[0, 0].item<long>();
                        var res = t[start .. end];

                        list.Add(res);
                    }

                    tokenList2.Add(list);
                }
            }
            else if(tokenList is List<List<Tensor>> t2)
            {
                foreach (var s in t2)
                {
                    List<Tensor> list = new List<Tensor>();

                    foreach (Tensor t in s)
                    {
                        var end = (int)(t == tokenizer.eot).nonzero()[0, 0].item<long>();

                        var item = t[start..end];

                        list.Add(item);
                    }

                    tokenList2.Add(list);
                }
            }            

            //select the top-ranked sample in each group
            var selected = this.sequence_ranker.rank(tokenList2, sumLogprobList);

            List<List<int>> tokenIds = new List<List<int>>();

            int k = 0;
            foreach (var token in tokenList2)
            {
                var i = selected[k];

                tokenIds.Add(TorchHelper.GetInt32Array(token[i].tolist()).ToList());

                k++;
            }

            List<string> texts = tokenIds.Select(item => tokenizer.decode(item).Trim()).ToList();

            List<float> sumlogprobValues = new List<float>();

            k = 0;
            foreach (var lp in sumLogprobList)
            {
                var i = selected[k];

                sumlogprobValues.Add(lp[i]);
                k++;
            }

            List<float> avg_logprobs = new List<float>();

            k = 0;
            foreach (var lp in sumlogprobValues)
            {
                var t = tokenIds[k];

                avg_logprobs.Add(lp * 1.0f / (t.Count + 1));

                k++;
            }

            var fields = (
                    texts,
                    languages,
                    tokenIds,
                    audio_features,
                    avg_logprobs,
                    no_speech_probs
            );

            List<int> lengthList = new List<int>() {
                texts.Count, languages.Count, tokenIds.Count, (int)audio_features.shape[0],
                avg_logprobs.Count, no_speech_probs.Count  };

            if (lengthList.Distinct().Count() != 1)
            {
                throw new Exception($"inconsistent result lengths: {string.Join("", lengthList.Select(item => item))}");
            }

            k = 0;
            foreach (var text in fields.texts)
            {
                var language = fields.languages[k];
                var no_speech_prob = fields.no_speech_probs[k];
                var avg_logprob = fields.avg_logprobs[k];
                var features = audio_features[k];

                result.Add(new DecodingResult()
                {
                    audio_features = features,
                    language = language,
                    tokens = fields.tokenIds[k],
                    text = text,
                    avg_logprob = avg_logprob,
                    no_speech_prob = no_speech_prob,
                    temperature = this.options.temperature,
                    compression_ratio = Utils.compression_ratio(text)
                });

                k++;
            }

            return result;
        }
    }
}

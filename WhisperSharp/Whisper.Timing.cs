using NumSharp;
using TorchSharp;
using WhisperSharp.Extension;
using WhisperSharp.Helper;
using WhisperSharp.Model;
using static TorchSharp.torch;

namespace WhisperSharp
{
    public partial class Whisper
    {
        public void add_word_timestamps(List<Dictionary<string, dynamic>> segments, Tokenizer tokenizer, Tensor mel, int num_frames, float last_speech_timestamp,
            string prepend_punctuations = "\"'“¿([{-", string append_punctuations = "\"'.。,，!！?？:：”)]}、", dynamic kwargs = null)
        {
            if (segments.Count == 0)
            {
                return;
            }

            List<List<int>> text_tokens_per_segment = new List<List<int>>();

            foreach (var segment in segments)
            {
                var items = (segment["tokens"] as List<int>).Where(item => item < tokenizer.eot).ToList();

                text_tokens_per_segment.Add(items);
            }

            var text_tokens = text_tokens_per_segment.SelectMany(item => item).ToList();

            List<WordTiming> alignment = find_alignment(tokenizer: tokenizer, text_tokens: text_tokens, mel: mel, num_frames: num_frames, kwargs: kwargs);

            var word_durations = np.array(alignment.Select(item => item.end - item.start).ToArray());
            word_durations = word_durations[np.nonzero(word_durations)];
            var median_duration = word_durations.__len__() > 0 ? np.median(word_durations).item<float>() : 0.0f;
            median_duration = Math.Min(0.7f, median_duration);
            var max_duration = (median_duration * 2);

            // hack: truncate long words at sentence boundaries.
            // a better segmentation algorithm based on VAD should be able to replace this.
            if (word_durations.__len__() > 0)
            {
                var sentence_end_marks = ".。!！?？";

                // ensure words at sentence boundaries are not longer than twice the median word duration.
                for (var i = 1; i < alignment.Count; i++)
                {
                    if (alignment[i].end - alignment[i].start > max_duration)
                    {
                        if (sentence_end_marks.Contains(alignment[i].word))
                        {
                            alignment[i].end = alignment[i].start + max_duration;
                        }
                        else if (sentence_end_marks.Contains(alignment[i - 1].word))
                        {
                            alignment[i].start = alignment[i].end - max_duration;
                        }
                    }
                }
            }

            merge_punctuations(alignment, prepend_punctuations, append_punctuations);

            var time_offset = segments[0]["seek"] * Audio.HOP_LENGTH / Audio.SAMPLE_RATE;
            var word_index = 0;

            foreach (var item in segments.Zip(text_tokens_per_segment, (segment, text_tokens) => (segment, text_tokens)))
            {
                var segment = item.segment;
                var ttks = item.text_tokens;

                var saved_tokens = 0;
                var words = new List<Dictionary<string, dynamic>>();

                while (word_index < alignment.Count && saved_tokens < ttks.Count)
                {
                    var timing = alignment[word_index];

                    if (timing.word != null)
                    {
                        words.Add(
                            new Dictionary<string, dynamic>()
                            {
                                { "word", timing.word },
                                { "start", Math.Round(time_offset + timing.start, 2) },
                                { "end",  Math.Round(time_offset + timing.end, 2) },
                                { "probability"  , timing.probability }
                            }
                        );
                    }

                    saved_tokens += timing.tokens.Count;
                    word_index += 1;
                }

                // hack: truncate long words at segment boundaries.
                // a better segmentation algorithm based on VAD should be able to replace this.
                if (words.Count > 0)
                {
                    // ensure the first and second word after a pause is not longer than
                    // twice the median word duration.
                    if (
                        words[0]["end"] - last_speech_timestamp > median_duration * 4 &&
                        (
                        words[0]["end"] - words[0]["start"] > max_duration
                        || (
                            words.Count > 1
                            && words[1]["end"] - words[0]["start"] > max_duration * 2
                        )
                    ))
                    {
                        if (words.Count > 1 && words[1]["end"] - words[1]["start"] > max_duration)
                        {
                            var boundary = Math.Max(words[1]["end"] / 2, words[1]["end"] - max_duration);
                            words[0]["end"] = words[1]["start"] = boundary;
                        }

                        words[0]["start"] = Math.Max(0, words[0]["end"] - max_duration);
                    }

                    // prefer the segment-level start timestamp if the first word is too long.
                    if (segment["start"] < words[0]["end"] && (segment["start"] - 0.5) > words[0]["start"])
                    {
                        words[0]["start"] = Math.Max(0, Math.Min(words[0]["end"] - median_duration, segment["start"]));
                    }
                    else
                    {
                        segment["start"] = words[0]["start"];
                    }

                    var lastWord = words.LastOrDefault();

                    // prefer the segment-level end timestamp if the last word is too long.
                    if (segment["end"] > lastWord["start"] && segment["end"] + 0.5 < lastWord["end"])
                    {
                        words[words.Count - 1]["end"] = Math.Max(lastWord["start"] + median_duration, segment["end"]);
                    }
                    else
                    {
                        segment["end"] = words.LastOrDefault()["end"];
                    }

                    last_speech_timestamp = (float)segment["end"];
                }

                segment["words"] = words;
            }
        }

        public List<WordTiming> find_alignment(Tokenizer tokenizer, List<int> text_tokens, Tensor mel, int num_frames, int medfilt_width = 7, float qk_scale = 1.0f, dynamic kwargs = null)
        {
            if (text_tokens.Count == 0)
            {
                return new List<WordTiming>();
            }

            var tokenList = new List<int>();

            tokenList.AddRange(tokenizer.sot_sequence);
            tokenList.Add(tokenizer.no_timestamps);
            tokenList.AddRange(text_tokens);
            tokenList.Add(tokenizer.eot.Value);

            var tokens = torch.tensor(tokenList).to(this.device);

            // install hooks on the cross attention layers to retrieve the attention weights
            List<Tensor> qkList = new List<Tensor>();

            foreach (var _ in ArrayHelper.GetRange(this.dims.n_text_layer))
            {
                qkList.Add(null);
            }

            var hooks = new List<dynamic>();
            int i = 0;

            Func<nn.Module<Tensor, Tensor>, Tensor, Tensor, Tensor> hook = (module, wv, qk) =>
            {
                if(i< qkList.Count)
                {
                    qkList[i] = !qk.IsNull() ? qk[-1] : null;
                }                

                i++;

                return null;
            };

            foreach (ResidualAttentionBlock block in this.decoder.blocks)
            {
                block.cross_attn.register_forward_hook(hook);
            }

            Tensor logits = null;
            Tensor sampled_logits = null;
            Tensor token_probs = null;
            float[] text_token_probs = null;

            using (torch.no_grad())
            {
                var prev_state = MultiHeadAttention.use_sdpa;

                MultiHeadAttention.use_sdpa = false;

                logits = this.forward(mel.unsqueeze(0), tokens.unsqueeze(0))[0];
                sampled_logits = logits[tokenizer.sot_sequence.Count.., ..tokenizer.eot.Value];
                token_probs = sampled_logits.softmax(dim: -1);

                var array = (np.arange(text_tokens.Count).tolist() as List<object>).Select(item => Convert.ToInt32(item)).ToArray();

                var indices = new TensorIndex[]
                {
                     TensorIndex.Tensor(array),                 // for first tensor dimension (row)
                     TensorIndex.Tensor(text_tokens.ToArray())  // for second tensor dimension (column)
                };

                text_token_probs = ArrayHelper.GetFloatArray(token_probs[indices].tolist());

                MultiHeadAttention.use_sdpa = prev_state;
            }

            foreach (var h in hooks)
            {
                h.remove();
            }

            // heads * tokens * frames
            var t = this.alignment_heads.SparseIndices.T;
            List<Tensor> list = new List<Tensor>();

            for (int index = 0; index < t.shape[0]; index++)
            {
                var _l = (int)t[index][0].item<long>();
                var _h = (int)t[index][1].item<long>();

                list.Add(qkList[_l][_h]);
            }

            var weights = torch.stack(list.ToArray());
            weights = weights[TensorIndex.Colon, TensorIndex.Colon, ..(int)Math.Floor(num_frames / 2.0)];
            weights = (weights * qk_scale).softmax(dim: -1);
            var (std, mean) = torch.std_mean(weights, dimensions: [-2], keepdim: true, unbiased: false);
            weights = (weights - mean) / std;
            weights = median_filter(weights, medfilt_width);

            var matrix = torch.mean(weights, [0]);
            matrix = matrix[tokenizer.sot_sequence.Count..^1];

            var dwt = dtw(-matrix);

            var text_indices = dwt[0];
            var time_indices = dwt[1];

            (List<string> Words, List<List<int>> WordTokens) wordTokens = tokenizer.split_to_word_tokens(Enumerable.Concat(text_tokens.ToArray<int>(), new int[] { tokenizer.eot.Value }).ToList());

            if (wordTokens.WordTokens.Count <= 1)
            {
                // return on eot only
                // >>> np.pad([], (1, 0))
                // array([0.])
                // This results in crashes when we lookup jump_times with float, like
                // IndexError: arrays used as indices must be of integer (or boolean) type
                return new List<WordTiming>();
            }

            var word_boundaries = np.pad(np.cumsum(wordTokens.WordTokens.SkipLast(1).Select(item => item.Count).ToArray()), (1, 0));

            var jumps = np.pad(np.diff(text_indices), (1, 0), constant_values: 1).astype(typeof(bool));
            var jump_times = time_indices[jumps] / Audio.TOKENS_PER_SECOND;

            var wb1 = word_boundaries[":-1"];
            var wb2 = word_boundaries["1:"];

            var start_times = jump_times[wb1];
            var end_times = jump_times[wb2];

            int m = 0;
            var word_probabilities = new List<NDArray>();

            foreach (var w in wb1)
            {
                var k = (int)wb2[m];
                var w2 = Convert.ToInt32(w);

                var array = text_token_probs.Skip(w2).Take(k - w2).ToArray();

                word_probabilities.Add(np.mean(array));

                m++;
            }

            List<WordTiming> result = new List<WordTiming>();

            int length = (new int[] { wordTokens.Words.Count, wordTokens.WordTokens.Count, (int)start_times.__len__() }).Min();

            for (int n = 0; n < length; n++)
            {
                var word = wordTokens.Words[n];
                var tks = wordTokens.WordTokens[n];
                var start = start_times[n];
                var end = end_times[n];
                var probability = word_probabilities[n];

                result.Add(new WordTiming() { word = word, tokens = tks, start = start.item<float>(), end = end.item<float>(), probability = probability.item<float>() });
            }

            return result;
        }

        public NDArray dtw(Tensor x)
        {
            if (x.is_cuda)
            {
                return dtw_cuda(x);
            }

            var array = ArrayHelper.GetDouble2DimensonArray(x.@double().cpu().tolist());

            return dtw_cpu(np.array(array));
        }

        public NDArray dtw_cpu(NDArray x)
        {
            var N = x.shape[0];
            var M = x.shape[1];

            using (var cost = np.ones((N + 1, M + 1), dtype: np.float32) * np.inf)
            {
                var trace = -np.ones((N + 1, M + 1), dtype: np.float32);

                cost[0, 0] = 0;

                for (var j = 1; j <= M; j++)
                {
                    for (var i = 1; i <= N; i++)
                    {
                        var c0 = cost[i - 1, j - 1];
                        var c1 = cost[i - 1, j];
                        var c2 = cost[i, j - 1];

                        NDArray c = null;
                        int t = 0;

                        if (np.any(np.less(c0, c1)) && np.any(np.less(c0, c2)))
                        {
                            c = c0;
                            t = 0;
                        }
                        else if (np.any(np.less(c1, c0)) && np.any(np.less(c1, c2)))
                        {
                            c = c1;
                            t = 1;
                        }
                        else
                        {
                            c = c2;
                            t = 2;
                        }

                        cost[i, j] = x[i - 1, j - 1] + c;
                        trace[i, j] = t;
                    }
                }

                return backtrace(trace);
            }         
        }

        public NDArray backtrace(NDArray trace)
        {
            var i = trace.shape[0] - 1;
            var j = trace.shape[1] - 1;

            trace["0,:"] = 2;
            trace[":,0"] = 1;

            List<long[]> result = new List<long[]>();

            while (i > 0 || j > 0)
            {
                result.Add([i - 1, j - 1]);

                if (trace[i, j].item<int>() == 0)
                {
                    i -= 1;
                    j -= 1;
                }
                else if (trace[i, j].item<int>() == 1)
                {
                    i -= 1;
                }
                else if (trace[i, j].item<int>() == 2)
                {
                    j -= 1;
                }
                else
                {
                    throw new Exception("Unexpected trace[i, j]");
                }
            }

            using (var res = np.array(result.ToArray()))
            {
                return res["::-1, :"].T;
            }            
        }

        public NDArray dtw_cuda(Tensor x, int BLOCK_SIZE = 1024)
        {
            return null; ////to do
        }

        public Tensor median_filter(Tensor x, int filter_width)
        {
            // Apply a median filter of width `filter_width` along the last dimension of `x`
            var pad_width = Math.Floor(filter_width / 2.0);

            if (x.shape.LastOrDefault() <= pad_width)
            {
                //F.pad requires the padding width to be smaller than the input dimension
                return x;
            }

            var ndim = x.ndim;

            if (ndim <= 2)
            {
                // `F.pad` does not support 1D or 2D inputs for reflect padding but supports 3D and 4D

                x = x[TensorIndex.None, TensorIndex.None, TensorIndex.Colon];
            }

            Tensor result = null;

            x = nn.functional.pad(x, ((long)Math.Floor(filter_width / 2.0), (long)Math.Floor(filter_width / 2.0), 0l, 0l), mode: PaddingModes.Reflect);

            if (x.is_cuda)
            {
                ////to do
            }

            if (result.IsNull())
            {
                // sort() is faster than torch.median (https://github.com/pytorch/pytorch/issues/51450)
                result = x.unfold(-1, filter_width, 1).sort().Values[TensorIndex.Ellipsis, (int)Math.Floor(filter_width / 2.0)];
            }

            if (ndim <= 2)
            {
                result = result[0, 0];
            }

            return result;
        }

        public void merge_punctuations(List<WordTiming> alignment, string prepended, string appended)
        {
            // merge prepended punctuations
            var i = alignment.Count - 2;
            var j = alignment.Count - 1;

            while (i >= 0)
            {
                var previous = alignment[i];
                var following = alignment[j];

                if (previous.word.StartsWith(" ") && prepended.Contains(previous.word.Trim()))
                {
                    // prepend it to the following word
                    following.word = previous.word + following.word;
                    following.tokens = Enumerable.Concat(previous.tokens, following.tokens).ToList();
                    previous.word = "";
                    previous.tokens = new List<int>();
                }
                else
                {
                    j = i;
                }

                i -= 1;
            }

            // merge appended punctuations
            i = 0;
            j = 1;

            while (j < alignment.Count)
            {
                var previous = alignment[i];
                var following = alignment[j];

                if (!previous.word.EndsWith(" ") && appended.Contains(following.word))
                {
                    // append it to the previous word
                    previous.word = previous.word + following.word;
                    previous.tokens = Enumerable.Concat(previous.tokens, following.tokens).ToList();
                    following.word = "";
                    following.tokens = new List<int>();
                }
                else
                {
                    i = j;
                }

                j += 1;
            }
        }
    }
}

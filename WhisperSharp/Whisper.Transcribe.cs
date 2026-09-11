using Newtonsoft.Json;
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
        /// Transcribe an audio file using Whisper
        /// </summary>
        /// <param name="audio">The path to the audio file to open, or the audio waveform</param>
        /// <param name="verbose">Whether to display the text being decoded to the console. If True, displays all the details,
        /// If False, displays minimal details. If null, does not display anything
        /// </param>
        /// <param name="temperature">Temperature for sampling. It can be a tuple of temperatures, which will be successively used
        /// upon failures according to either `compression_ratio_threshold` or `logprob_threshold`.
        /// </param>
        /// <param name="compression_ratio_threshold">If the gzip compression ratio is above this value, treat as failed</param>
        /// <param name="logprob_threshold">If the average log probability over sampled tokens is below this value, treat as failed</param>
        /// <param name="no_speech_threshold">If the no_speech probability is higher than this value AND the average log probability
        /// over sampled tokens is below `logprob_threshold`, consider the segment as silent
        /// </param>
        /// <param name="condition_on_previous_text">if True, the previous output of the model is provided as a prompt for the next window;
        /// disabling may make the text inconsistent across windows, but the model becomes less prone to
        /// getting stuck in a failure loop, such as repetition looping or timestamps going out of sync.
        /// </param>
        /// <param name="initial_prompt">Optional text to provide as a prompt for the first window. This can be used to provide, or
        /// "prompt-engineer" a context for transcription, e.g. custom vocabularies or proper nouns
        /// to make it more likely to predict those word correctly.
        /// </param>
        /// <param name="carry_initial_prompt">If carry_initial_prompt is True, `initial_prompt` is prepended to the prompt of each internal
        /// `decode()` call. If there is not enough context space at the start of the prompt, it is
        /// left-sliced to make space.
        /// </param>
        /// <param name="word_timestamps">Extract word-level timestamps using the cross-attention pattern and dynamic time warping,
        /// and include the timestamps for each word in each segment.
        /// </param>
        /// <param name="prepend_punctuations">If word_timestamps is True, merge these punctuation symbols with the next word</param>
        /// <param name="append_punctuations">If word_timestamps is True, merge these punctuation symbols with the previous word</param>
        /// <param name="clip_timestamps">Comma-separated list start,end,start,end,... timestamps (in seconds) of clips to process.
        /// The last end timestamp defaults to the end of the file.
        /// </param>
        /// <param name="hallucination_silence_threshold">When word_timestamps is True, skip silent periods longer than this threshold (in seconds)
        /// when a possible hallucination is detected
        /// </param>
        /// <param name="decode_options"></param>
        /// <returns>
        /// A dictionary containing the resulting text ("text") and segment-level details ("segments"), and
        /// the spoken language ("language"), which is detected when `decode_options["language"]` is null.
        /// </returns>
        public Dictionary<string, dynamic> transcribe(dynamic audio, bool? verbose = null,
                dynamic temperature = null,
                float? compression_ratio_threshold = 2.4f,
                float? logprob_threshold = -1.0f,
                float? no_speech_threshold = 0.6f,
                bool condition_on_previous_text = true,
                string initial_prompt = null,
                bool carry_initial_prompt = false,
                bool word_timestamps = false,
                string prepend_punctuations = "\"'“¿([{-",
                string append_punctuations = "\"'.。,，!！?？:：”)]}、",
                dynamic clip_timestamps = null,
                float? hallucination_silence_threshold = null,
                Dictionary<string, dynamic> decode_options = null)
        {
            if (temperature == null)
            {
                temperature = (0.0, 0.2, 0.4, 0.6, 0.8, 1.0);
            }

            if (clip_timestamps == null)
            {
                clip_timestamps = "0";
            }

            decode_options ??= new Dictionary<string, dynamic>();

            var dtype = decode_options.ContainsKey("fp16") && decode_options["fp16"] ? torch.float16 : torch.float32;

            if (this.device.ToString() == torch.device("cpu").ToString())
            {
                if (torch.cuda.is_available())
                {
                    LogHelper.LogWarnning("Performing inference on CPU when CUDA is available");
                }

                if (dtype == torch.float16)
                {
                    LogHelper.LogWarnning("FP16 is not supported on CPU; using FP32 instead");
                    dtype = torch.float32;
                }

                if (dtype == torch.float32)
                {
                    decode_options["fp16"] = false;
                }
            }

            //Pad 30-seconds of silence to the input audio, for slicing
            Tensor mel = Audio.log_mel_spectrogram(audio, this.dims.n_mels, padding: Audio.N_SAMPLES);
            var content_frames = (int)mel.shape.LastOrDefault() - Audio.N_FRAMES;
            var content_duration = (float)(content_frames * Audio.HOP_LENGTH * 1.0 / Audio.SAMPLE_RATE);

            if (!decode_options.ContainsKey("language"))
            {
                if (!this.is_multilingual)
                {
                    decode_options["language"] = "en";
                }
                else
                {
                    if (verbose == true)
                    {
                        Console.Write("Detecting language using up to the first 30 seconds. Use `--language` to specify the language");
                    }

                    var mel_segment = Audio.pad_or_trim(mel, Audio.N_FRAMES).to(this.device).to(dtype);
                    (Tensor Tokens, List<Dictionary<string, dynamic>> Probs) result = this.detect_language(mel_segment);

                    var probs = result.Probs.FirstOrDefault();

                    var maxValue = probs.Values.Max();
                    var maxKey = probs.FirstOrDefault(item => item.Value == maxValue).Key;

                    decode_options["language"] = maxKey;
                }
            }

            string language = decode_options["language"];
            string task = (!decode_options.ContainsKey("task") || decode_options["task"] == null) ? "transcribe" : decode_options["task"];

            var tokenizer = new Tokenizer(
                            this.is_multilingual,
                            num_languages: this.num_languages,
                            language: language,
                            task: task
                        );

            List<float> clip_timestampsList = null;

            if (clip_timestamps is string str)
            {
                clip_timestampsList = (str != null ? str.Split(',') : []).Select(item => float.Parse(item)).ToList();
            }
            else
            {
                clip_timestampsList = clip_timestamps as List<float>;
            }

            List<int> seek_points = clip_timestampsList.Select(item => (int)Math.Round(item * Audio.FRAMES_PER_SECOND)).ToList();

            if (seek_points.Count == 0)
            {
                seek_points.Add(0);
            }

            if (seek_points.Count % 2 == 1)
            {
                seek_points.Add(content_frames);
            }

            var odds = seek_points.Where((item, index) => index % 2 == 0);
            var evens = seek_points.Where((item, index) => index % 2 == 1);

            List<(int, int)> seek_clips = odds.Zip(evens, (odd, even) => (odd, even)).ToList();

            string punctuation = "\"'“¿([{-\"'.。,，!！?？:：”)]}、";

            if (word_timestamps && task == "translate")
            {
                LogHelper.LogWarnning("Word-level timestamps on translations may not be reliable.");
            }

            Func<Tensor, DecodingResult> decode_with_fallback = (segment) =>
            {
                dynamic temperatures = (temperature is int || temperature is float || temperature is double) ? new float[] { (float)temperature } : temperature;

                DecodingResult decode_result = null;

                foreach (var t in temperatures)
                {
                    var kwargs = decode_options as Dictionary<string, dynamic>;

                    List<string> ignoreAttributes = new List<string>();

                    if (t > 0)
                    {
                        //disable beam_size and patience when t > 0
                        ignoreAttributes.Add("beam_size");
                        ignoreAttributes.Add("patience");
                    }
                    else
                    {
                        //disable best_of when t == 0
                        ignoreAttributes.Add("best_of");
                    }

                    var options = new DecodingOptions() { temperature = t };

                    DecodingOptions opt = (DecodingOptions)JsonConvert.DeserializeObject<DecodingOptions>(JsonConvert.SerializeObject(decode_options));

                    ObjectHelper.CopyProperties(opt, options, true, ignoreAttributes);

                    decode_result = this.decode(segment, options)?.FirstOrDefault();

                    var needs_fallback = false;

                    if (compression_ratio_threshold != null && decode_result.compression_ratio > compression_ratio_threshold)
                    {
                        needs_fallback = true;
                    }

                    if (logprob_threshold != null && decode_result.avg_logprob < logprob_threshold)
                    {
                        needs_fallback = true; // average log probability is too low
                    }

                    if (no_speech_threshold != null
                        && decode_result.no_speech_prob > no_speech_threshold
                        && logprob_threshold != null
                        && decode_result.avg_logprob < logprob_threshold)
                    {
                        needs_fallback = false; // silence
                    }

                    if (!needs_fallback)
                    {
                        break;
                    }
                }

                return decode_result;
            };

            var clip_idx = 0;
            var seek = seek_clips[clip_idx].Item1;
            var input_stride = Utils.exact_div(Audio.N_FRAMES, this.dims.n_audio_ctx); // mel frames per output token: 2

            var time_precision = (input_stride * Audio.HOP_LENGTH * 1.0 / Audio.SAMPLE_RATE); // time per output token: 0.02 (seconds)
            var all_tokens = new List<int>();
            var all_segments = new List<Dictionary<string, dynamic>>();
            var prompt_reset_since = 0;

            var remaining_prompt_length = (int)Math.Floor(this.dims.n_text_ctx / 2.0) - 1;

            List<int> initial_prompt_tokens = null;

            if (initial_prompt != null)
            {
                initial_prompt_tokens = tokenizer.encode(" " + initial_prompt.Trim());
                all_tokens.AddRange(initial_prompt_tokens);
                remaining_prompt_length -= initial_prompt_tokens.Count;
            }
            else
            {
                initial_prompt_tokens = new List<int>();
            }

            var last_speech_timestamp = 0.0f;

            // NOTE: This loop is obscurely flattened to make the diff readable.
            // A later commit should turn this into a simpler nested loop.
            // for seek_clip_start, seek_clip_end in seek_clips:
            // while seek < seek_clip_end
            while (clip_idx < seek_clips.Count)
            {
                var (seek_clip_start, seek_clip_end) = seek_clips[clip_idx];

                if (seek < seek_clip_start)
                {
                    seek = seek_clip_start;
                }

                if (seek >= seek_clip_end)
                {
                    clip_idx += 1;

                    if (clip_idx < seek_clips.Count)
                    {
                        seek = seek_clips[clip_idx].Item1;
                    }

                    continue;
                }

                var time_offset = (float)(seek * Audio.HOP_LENGTH * 1.0 / Audio.SAMPLE_RATE);
                var window_end_time = (float)((seek + Audio.N_FRAMES) * Audio.HOP_LENGTH * 1.0 / Audio.SAMPLE_RATE);
                var segment_size = Math.Min(Math.Min(Audio.N_FRAMES, content_frames - seek), seek_clip_end - seek);

                var mel_segment = mel[TensorIndex.Colon, seek..(seek + segment_size)];

                var segment_duration = segment_size * Audio.HOP_LENGTH * 1.0 / Audio.SAMPLE_RATE;
                mel_segment = Audio.pad_or_trim(mel_segment, Audio.N_FRAMES).to(this.device).to(dtype);

                if (carry_initial_prompt)
                {
                    var nignored = Math.Max(initial_prompt_tokens.Count, prompt_reset_since);
                    var remaining_prompt = all_tokens.Skip(nignored).TakeLast(remaining_prompt_length);

                    decode_options["prompt"] = Enumerable.Concat(initial_prompt_tokens, remaining_prompt).ToList();
                }
                else
                {
                    decode_options["prompt"] = all_tokens.Skip(prompt_reset_since).ToList();
                }

                DecodingResult result = decode_with_fallback(mel_segment);

                var tokens = torch.tensor(result.tokens);

                if (no_speech_threshold != null)
                {
                    // no voice activity check
                    var should_skip = result.no_speech_prob > no_speech_threshold;

                    if (logprob_threshold != null && result.avg_logprob > logprob_threshold)
                    {
                        //don't skip if the logprob is high enough, despite the no_speech_prob
                        should_skip = false;
                    }

                    if (should_skip)
                    {
                        seek += segment_size; // fast-forward to the next segment boundary

                        continue;
                    }

                    var previous_seek = seek;
                    var current_segments = new List<Dictionary<string, dynamic>>();

                    // anomalous words are very long/short/improbable
                    Func<Dictionary<string, dynamic>, float> word_anomaly_score = (word) =>
                    {
                        var probability = word.GetValueOrDefault<string, dynamic>("probability", 0.0f);
                        var duration = word["end"] - word["start"];
                        var score = 0.0f;

                        if (probability < 0.15f)
                        {
                            score += 1.0f;
                        }

                        if (duration < 0.133f)
                        {
                            score += (0.133f - duration) * 15;
                        }

                        if (duration > 2.0f)
                        {
                            score += duration - 2.0f;
                        }

                        return score;
                    };

                    Func<Dictionary<string, dynamic>, bool> is_segment_anomaly = (segment) =>
                    {
                        if (segment == null || segment.GetValueOrDefault("words", null) == null)
                        {
                            return false;
                        }

                        List<Dictionary<string, dynamic>> words = new List<Dictionary<string, dynamic>>();

                        var wordList = segment["words"];

                        int count = 0;

                        foreach (var item in wordList)
                        {
                            var word = item["word"] as string;

                            if (!punctuation.Contains(word))
                            {
                                words.Add(item);

                                count++;
                            }
                        }

                        words = words.Take(8).ToList();

                        var score = words.Select(item => word_anomaly_score(item)).Sum();

                        return score >= 3 || score + 0.01f >= words.Count;
                    };

                    Func<List<Dictionary<string, dynamic>>, Dictionary<string, dynamic>> next_words_segment = (segments) =>
                    {
                        return segments.FirstOrDefault(item => item["words"] != null);
                    };

                    Tensor timestamp_tokens = tokens.ge(tokenizer.timestamp_begin);
                    var single_timestamp_ending = ArrayHelper.GetBoolArray(timestamp_tokens[^2..].tolist()).SequenceEqual([false, true]);

                    var consecutive = torch.where(timestamp_tokens[..^1] & timestamp_tokens[1..])[0];
                    consecutive.add_(1);

                    if (consecutive.shape[0] > 0)
                    {
                        // if the output contains two consecutive timestamp tokens
                        var slices = ArrayHelper.GetInt32Array(consecutive.tolist()).ToList();

                        if (single_timestamp_ending)
                        {
                            slices.Add((int)tokens.shape[0]);
                        }

                        int last_slice = 0;
            
                        foreach (var current_slice in slices)
                        {
                            var sliced_tokens = tokens[last_slice..current_slice];

                            var start_timestamp_pos = sliced_tokens[0].item<int>() - tokenizer.timestamp_begin;
                            var end_timestamp_pos = sliced_tokens[-1].item<int>() - tokenizer.timestamp_begin;

                            current_segments.Add(
                                new_segment(
                                start: (float)(time_offset + start_timestamp_pos * time_precision),
                                end: (float)(time_offset + end_timestamp_pos * time_precision),
                                seek: seek,
                                tokens: sliced_tokens,
                                tokenizer: tokenizer,
                                result: result
                            ));

                            last_slice = current_slice;
                        }

                        if (single_timestamp_ending)
                        {
                            // single timestamp at the end means no speech after the last timestamp.
                            seek += segment_size;
                        }
                        else
                        {
                            // otherwise, ignore the unfinished segment and seek to the last timestamp
                            var last_timestamp_pos = tokens[last_slice - 1].item<int>() - tokenizer.timestamp_begin;
                            seek += last_timestamp_pos.Value * input_stride;
                        }
                    }
                    else
                    {
                        var duration = segment_duration;
                        var timestamps = tokens[timestamp_tokens.nonzero().flatten()];

                        if (timestamps.shape[0] > 0 && timestamps[-1].item<int>() != tokenizer.timestamp_begin)
                        {
                            // no consecutive timestamps but it has a timestamp; use the last one.
                            var last_timestamp_pos = timestamps[-1].item<int>() - tokenizer.timestamp_begin;

                            duration = last_timestamp_pos.Value * time_precision;
                        }

                        current_segments.Add(
                            new_segment(
                                start: time_offset,
                                end: (float)(time_offset + duration),
                                seek: seek,
                                tokens = tokens,
                                tokenizer: tokenizer,
                                result = result
                            ));
                        seek += segment_size;
                    }

                    if (word_timestamps)
                    {
                        add_word_timestamps(
                            segments: current_segments,
                            tokenizer : tokenizer,
                            mel : mel_segment,
                            num_frames: segment_size,
                            prepend_punctuations: prepend_punctuations,
                            append_punctuations: append_punctuations,
                            last_speech_timestamp: last_speech_timestamp
                        );

                        float? last_word_end = null;

                        if (!single_timestamp_ending)
                        {
                            last_word_end = get_end(current_segments);

                            if (last_word_end != null && last_word_end > time_offset)
                            {
                                seek = (int)Math.Round((double)last_word_end * Audio.FRAMES_PER_SECOND);
                            }
                        }

                        // skip silence before possible hallucinations
                        if (hallucination_silence_threshold != null)
                        {
                            var threshold = hallucination_silence_threshold;

                            if (!single_timestamp_ending)
                            {
                                last_word_end = get_end(current_segments);

                                if (last_word_end != null && last_word_end > time_offset)
                                {
                                    var remaining_duration = window_end_time - last_word_end;

                                    if (remaining_duration > threshold)
                                    {
                                        seek = (int)Math.Round((double)last_word_end * Audio.FRAMES_PER_SECOND);
                                    }
                                    else
                                    {
                                        seek = previous_seek + segment_size;
                                    }
                                }
                            }

                            // if first segment might be a hallucination, skip leading silence
                            var first_segment = next_words_segment(current_segments);

                            if (first_segment != null && is_segment_anomaly(first_segment))
                            {
                                var gap = first_segment["start"] - time_offset;

                                if (gap > threshold)
                                {
                                    seek = previous_seek + round(gap * Audio.FRAMES_PER_SECOND);
                                    continue;
                                }
                            }

                            // skip silence before any possible hallucination that is surrounded
                            // by silence or more hallucinations
                            var hal_last_end = last_speech_timestamp;

                            foreach (var si in ArrayHelper.GetRange(current_segments.Count))
                            {
                                var segment = current_segments[si];

                                if (segment["words"] == null)
                                {
                                    continue;
                                }

                                if (is_segment_anomaly(segment))
                                {
                                    var next_segment = next_words_segment(current_segments[(si + 1)..]);

                                    dynamic hal_next_start = null;

                                    if (next_segment != null)
                                    {
                                        hal_next_start = next_segment["words"][0]["start"];
                                    }
                                    else
                                    {
                                        hal_next_start = time_offset + segment_duration;
                                    }

                                    var silence_before = segment["start"] - hal_last_end > threshold
                                        || segment["start"] < threshold
                                        || segment["start"] - time_offset < 2.0;

                                    var silence_after = hal_next_start - segment["end"] > threshold
                                        || is_segment_anomaly(next_segment)
                                        || window_end_time - segment["end"] < 2.0;

                                    if (silence_before && silence_after)
                                    {
                                        seek = Math.Round(Math.Max(time_offset + 1, segment["start"]) * Audio.FRAMES_PER_SECOND);
                                    }

                                    if (content_duration - segment["end"] < threshold)
                                    {
                                        seek = content_frames;
                                    }

                                    current_segments.RemoveRange(si, current_segments.Count - si);

                                    break;
                                }

                                hal_last_end = segment["end"];
                            }
                        }

                        last_word_end = get_end(current_segments);

                        if (last_word_end != null)
                        {
                            last_speech_timestamp = last_word_end.Value;
                        }
                    }

                    if (verbose == true)
                    {
                        foreach (var segment in current_segments)
                        {
                            var start = segment["start"];
                            var end = segment["end"];
                            var text = segment["text"];

                            var line = $"[{Utils.format_timestamp(start)} --> {Utils.format_timestamp(end)}] {text}";

                            Console.WriteLine(Utils.make_safe(line));
                        }
                    }

                    // if a segment is instantaneous or does not contain text, clear it
                    int i = 0;

                    foreach (var segment in current_segments)
                    {
                        if (segment["start"] == segment["end"] || segment["text"].Trim() == "")
                        {
                            segment["text"] = "";
                            segment["tokens"] = new List<int>();
                            segment["words"] = new List<Dictionary<string, dynamic>>();
                        }

                        i++;
                    }

                    i = all_segments.Count;

                    foreach (var segment in current_segments)
                    {
                        segment["id"] = i;

                        all_segments.Add(segment);
                    }

                    foreach (var segment in current_segments)
                    {
                        foreach (var token in segment["tokens"])
                        {
                            all_tokens.Add(token);
                        }
                    }

                    if (!condition_on_previous_text || result.temperature > 0.5f)
                    {
                        // do not feed the prompt tokens if a high temperature was used
                        prompt_reset_since = all_tokens.Count;
                    }
                }
            }

            return new Dictionary<string, dynamic>() 
            {
                {"text", tokenizer.decode(all_tokens.Skip(initial_prompt_tokens.Count).ToList())},
                {"segments", all_segments },
                {"language", language }
            };
        }

        public Dictionary<string, dynamic> new_segment(float start, float end, long seek, Tensor tokens, Tokenizer tokenizer, DecodingResult result)
        {
            var tokenList = ArrayHelper.GetInt32Array(tokens.tolist()).ToList();
            var text_tokens = tokenList.Where(item => item < tokenizer.eot).ToList();
            var text = tokenizer.decode(text_tokens);

            return new Dictionary<string, dynamic>()
            {
                {"seek", seek },
                {"start", start },
                {"end", end },
                {"text", text },
                {"tokens", tokenList },
                {"temperature", result.temperature },
                {"avg_logprob", result.avg_logprob },
                {"compression_ratio",result.compression_ratio },
                {"no_speech_prob", result.no_speech_prob }
            };
        }

        private float? get_end(List<Dictionary<string, dynamic>> segments)
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                var segment = segments[i];

                var words = segment["words"] as List<Dictionary<string, dynamic>>;

                for (var j = words.Count-1; j >= 0; j--)
                {
                    if (j == words.Count - 1)
                    {
                        return Convert.ToSingle(words[j]["end"]);
                    }
                }
            }

            var defaultValue = segments != null ? segments.LastOrDefault()?["end"] ?? null : null;

            if (defaultValue != null)
            {
                return Convert.ToSingle(defaultValue);
            }

            return null;
        }
    }
}

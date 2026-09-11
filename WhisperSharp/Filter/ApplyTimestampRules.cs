using NumSharp;
using TorchSharp;
using WhisperSharp.Extension;
using WhisperSharp.Helper;
using static TorchSharp.torch;
using F = TorchSharp.torch.nn.functional;

namespace WhisperSharp.Filter
{
    public class ApplyTimestampRules : LogitFilter
    {
        private Tokenizer tokenizer;
        private int sample_begin;
        private int? max_initial_timestamp_index;

        public ApplyTimestampRules(Tokenizer tokenizer, int sample_begin, int? max_initial_timestamp_index)
        {
            this.tokenizer = tokenizer;
            this.sample_begin = sample_begin;
            this.max_initial_timestamp_index = max_initial_timestamp_index;
        }

        public void apply(torch.Tensor logits, torch.Tensor tokens)
        {
            //suppress <|notimestamps|> which is handled by without_timestamps
            if (this.tokenizer.no_timestamps != null)
            {
                logits[TensorIndex.Colon, this.tokenizer.no_timestamps] = -np.inf;
            }

            //timestamps have to appear in pairs, except directly before EOT; mask logits accordingly
            for (var k = 0; k < tokens.shape[0]; k++)
            {
                var sampled_tokens = tokens[k, this.sample_begin..];
                var seq = ArrayHelper.GetInt32Array(sampled_tokens.tolist()).ToList();
                var last_was_timestamp = seq.Count >= 1 && seq.LastOrDefault() >= this.tokenizer.timestamp_begin;
                var penultimate_was_timestamp = seq.Count < 2 || seq[seq.Count - 2] >= this.tokenizer.timestamp_begin;

                if (last_was_timestamp)
                {
                    if (penultimate_was_timestamp) //has to be non-timestamp
                    {
                        logits[k, this.tokenizer.timestamp_begin.Value..] = -np.inf;
                    }
                    else // cannot be normal text tokens
                    {
                        logits[k, ..this.tokenizer.eot.Value] = -np.inf;
                    }
                }

                var timestamps = sampled_tokens[sampled_tokens.ge(this.tokenizer.timestamp_begin)];

                if (timestamps.numel() > 0)
                {
                    //timestamps shouldn't decrease; forbid timestamp tokens smaller than the last
                    //also force each segment to have a nonzero length, to prevent infinite looping

                    Tensor timestamp_last;

                    if (last_was_timestamp && !penultimate_was_timestamp)
                    {
                        timestamp_last = timestamps[-1];
                    }
                    else
                    {
                        timestamp_last = timestamps[-1] + 1;
                    }

                    logits[k, this.tokenizer.timestamp_begin.Value..(int)timestamp_last.item<long>()] = -np.inf;
                }
            }

            if (tokens.shape[1] == this.sample_begin)
            {
                //suppress generating non-timestamp tokens at the beginning
                logits[TensorIndex.Colon, ..this.tokenizer.timestamp_begin.Value] = -np.inf;

                int last_allowed;

                //apply the `max_initial_timestamp` option
                if (this.max_initial_timestamp_index != null)
                {
                    last_allowed = this.tokenizer.timestamp_begin.Value + this.max_initial_timestamp_index.Value;                  

                    logits.UpdateEachRowValue(ArrayHelper.GetRangeLong(logits.shape[1], last_allowed + 1), -np.inf);
                }
            }

            //if sum of probability over timestamps is above any other token, sample timestamp
            var logprobs = F.log_softmax(logits.@float(), dim: -1);

            for (var k = 0; k < tokens.shape[0]; k++)
            {
                var timestamp_logprob = logprobs[k, this.tokenizer.timestamp_begin.Value..].logsumexp(dim: -1);
                var max_text_token_logprob = logprobs[k, ..this.tokenizer.timestamp_begin.Value].max();

                if ((timestamp_logprob > max_text_token_logprob).item<bool>())
                {
                    logits[k, ..this.tokenizer.timestamp_begin.Value] = -np.inf;
                }
            }
        }
    }
}

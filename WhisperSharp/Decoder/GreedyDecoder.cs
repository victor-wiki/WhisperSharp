using TorchSharp;
using TorchSharp.Modules;
using WhisperSharp.Helper;
using static TorchSharp.torch;
using F = TorchSharp.torch.nn.functional;

namespace WhisperSharp.Decoder
{
    public class GreedyDecoder : TokenDecoder
    {
        private float temperature = 0.0f;
        private int eot;

        public GreedyDecoder(float temperature, int eot)
        {
            this.temperature = temperature;
            this.eot = eot;
        }

        public override (Tensor, bool) update(Tensor tokens, ref Tensor logits, ref Tensor sum_logprobs)
        {
            Tensor next_tokens;

            if (this.temperature == 0.0f)
                next_tokens = logits.argmax(dim: -1);
            else
                next_tokens = new Categorical(logits : logits / this.temperature).sample();

            var logprobs = F.log_softmax(logits.@float(), dim: -1);
            var current_logprobs = logprobs[torch.arange(logprobs.shape[0]), next_tokens];

            var lastItems = tokens[TensorIndex.Colon, -1];

            sum_logprobs += current_logprobs * (lastItems != this.eot);

            next_tokens[lastItems == this.eot] = this.eot;
            tokens = torch.cat([tokens, next_tokens.unsqueeze(1)], dim: -1);

            var completed = (tokens[TensorIndex.Colon, -1] == this.eot).item<bool>();

            return (tokens, completed);
        }

        public override (dynamic Tokens, List<List<float>> SumLogprobs) finalize(Tensor tokens, Tensor sum_logprobs)
        {
            //make sure each sequence has at least one EOT token at the end
            tokens = F.pad(tokens, (0, 1), value: this.eot);
            return (tokens, TorchHelper.GetFloatArrayList(sum_logprobs.tolist()));
        }
    }
}

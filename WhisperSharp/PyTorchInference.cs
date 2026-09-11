using TorchSharp;
using static TorchSharp.torch;

namespace WhisperSharp
{
    public class PyTorchInference : Inference
    {
        private Whisper model;
        private int initial_token_length;
        private List<LinearExt> kv_modules;
        private Dictionary<nn.Module<Tensor, Tensor>, Tensor> kv_cache;
        private List<dynamic> hooks;

        public PyTorchInference(Whisper whisper, int initial_token_length)
        {
            this.model = whisper;
            this.initial_token_length = initial_token_length;
            this.kv_cache = new Dictionary<nn.Module<Tensor, Tensor>, Tensor>();
            this.hooks = new List<dynamic>();

            var key_modules = new List<LinearExt>();
            var value_modules = new List<LinearExt>();

            foreach (var attn in this.model.decoder.blocks.Select(item => item.attn))
            {
                key_modules.Add(attn.key);
                value_modules.Add(attn.value);
            }

            this.kv_modules = Enumerable.Concat(key_modules, value_modules).ToList();
        }       

        public torch.Tensor logits(torch.Tensor tokens, torch.Tensor audio_features)
        {
            if (this.kv_cache == null || this.kv_cache.Count ==0)
            {
                (Dictionary<nn.Module<Tensor, Tensor>, Tensor>, List<dynamic>) kv = this.model.install_kv_cache_hooks();

                this.kv_cache = kv.Item1;
                this.hooks = kv.Item2;
            }

            if (tokens.shape.LastOrDefault() > this.initial_token_length)
            {
                //only need to use the last token except in the first forward pass
                tokens = tokens[TensorIndex.Colon, ^1..];
            }

            return this.model.decoder.forward(tokens, audio_features, kv_cache: this.kv_cache);
        }

        public void rearrange_kv_cache(List<int> source_indices)
        {
            List<int> list = new List<int>();

            for(var i=0;i< source_indices.Count;i++)
            {
                list.Add(i);
            }

            if(!source_indices.SequenceEqual(list))
            {
                foreach(var module in this.kv_modules)
                {
                    //update the key/value cache to contain the selected sequences
                    this.kv_cache[module] = this.kv_cache[module][source_indices.ToArray()].detach();
                }
            }
        }

        public void cleanup_caching()
        {
            foreach (var hook in this.hooks)
            {
                hook.remove();
            }

            this.kv_cache = new Dictionary<nn.Module<Tensor, Tensor>, Tensor>();
            this.hooks = new List<dynamic>();
        }
    }
}

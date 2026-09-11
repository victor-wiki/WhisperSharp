using NumSharp;
using System.Collections;
using TorchSharp;
using WhisperSharp.Helper;
using static TorchSharp.torch;
using F = TorchSharp.torch.nn.functional;

namespace WhisperSharp.Decoder
{
    public class BeamSearchDecoder : TokenDecoder
    {
        private int beam_size;
        private int eot;
        private Inference inference;
        private float? patience;
        private int max_candidates;
        private List<Dictionary<string, dynamic>> finished_sequences;

        public BeamSearchDecoder(int beam_size, int eot, Inference inference, float? patience = null)
        {
            this.beam_size = beam_size;
            this.eot = eot;
            this.inference = inference;
            this.patience = patience ?? 1.0f;
            this.max_candidates = (int)Math.Round((double)(beam_size * this.patience));
            this.finished_sequences = null;
        }

        public void reset()
        {
            this.finished_sequences = null;
        }

        public override (torch.Tensor, bool) update(torch.Tensor tokens, ref torch.Tensor logits, ref torch.Tensor sum_logprobs)
        {
            if (tokens.shape[0] % this.beam_size != 0)
            {
                throw new Exception($"{tokens.shape}[0] % {this.beam_size} != 0");
            }

            var n_audio = Math.Floor(tokens.shape[0] * 1.0 / this.beam_size);

            if (this.finished_sequences == null)
            {
                this.finished_sequences = new List<Dictionary<string, dynamic>>();

                for (int t = 1; t <= n_audio; t++)
                {
                    this.finished_sequences.Add(new Dictionary<string, dynamic>());
                }
            }

            var logprobs = F.log_softmax(logits.@float(), dim: -1);
            var next_tokens = new List<string>();
            var source_indices = new List<int>();
            var finished_sequences = new List<Dictionary<string,dynamic>>();

            for (int v = 0; v < n_audio; v++)
            {
                var scores = new Dictionary<string, dynamic>();
                var sources = new Dictionary<string, dynamic>();
                var finished = new Dictionary<string, dynamic>();

                //STEP 1: calculate the cumulative log probabilities for possible candidates
                for (int j = 0; j < beam_size; j++)
                {
                    var idx = v * this.beam_size + j;
                    var prefix = (tokens[idx].tolist() as ArrayList).ToArray().Select(item => (item as Scalar).ToInt32());
   
                    var topk = logprobs[idx].topk(this.beam_size + 1);
                    Tensor values = topk.values;
                    Tensor indeies = topk.indexes;

                    var valueTensors = torch.unbind(values, 0);
                    var indexTensors = torch.unbind(indeies, 0);

                    foreach (var (logprob, token) in valueTensors.Zip(indexTensors, (v,i)=>(v,i)))
                    {
                        var new_logprob = (sum_logprobs[idx] + logprob).item<int>();
                        var sequence = string.Join("," , Enumerable.Concat(prefix , new int[] { token.item<int>() }));

                        scores[sequence] = new_logprob;
                        sources[sequence] = idx;                        
                    }
                }

                //STEP 2: rank the candidates and keep the top beam_size sequences for each audio
                var saved = 0;

                foreach (var kp in scores.OrderByDescending(item => item.Value))
                {
                    string key = kp.Key;

                    var sequence = key.Split(',').Select(item => int.Parse(item)).ToArray();

                    if (sequence.LastOrDefault() == this.eot)
                    {
                        finished[key] = scores[key];
                    }
                    else
                    {
                        sum_logprobs[next_tokens.Count] = scores[key];
                        next_tokens.Add(key);
                        source_indices.Add(sources[key]);

                        saved += 1;

                        if (saved == this.beam_size)
                        {
                            break;
                        }
                    }
                }

                finished_sequences.Add(finished);
            }

            var next_tokenValues = next_tokens.Select(item => item.Split(',').Select(t=>int.Parse(t)).ToArray()).ToArray();

            tokens = torch.tensor(ArrayHelper.ToRectangular<int>(next_tokenValues), device: tokens.device);
            this.inference.rearrange_kv_cache(source_indices);

            //add newly finished sequences to this.finished_sequences
            var i = 0;
            foreach (var previously_finished in this.finished_sequences)
            {
                Dictionary<string,dynamic> newly_finished = finished_sequences[i];

                foreach (var seq in newly_finished.OrderByDescending(item => item.Value))
                {
                    string key = seq.Key;

                    if (previously_finished.Count >= this.max_candidates)
                    {
                        break;  // the candidate list is full
                    }

                    previously_finished[key] = newly_finished[key];
                }

                i++;
            }

            //mark as completed if all audio has enough number of samples
            var completed = this.finished_sequences.All(item => item.Count >= this.max_candidates);

            return (tokens, completed);
        }

        public override (dynamic Tokens, List<List<float>> SumLogprobs) finalize(torch.Tensor preceding_tokens, torch.Tensor sum_logprobs)
        {
            //collect all finished sequences, including patience, and add unfinished ones if not enough
            sum_logprobs = sum_logprobs.cpu();

            int i = 0;

            foreach (Dictionary<string, dynamic> sequences in finished_sequences)
            {
                if (sequences.Count < this.beam_size) //when not enough sequences are finished
                {
                    var items = torch.unbind(sum_logprobs[i], 0).Reverse().Select(item => item.item<int>()).ToArray();
                    var array = np.argsort(items);
                    var len = array.__len__();

                    for (var k = 0; k < len; k++)
                    {
                        var j = array[k].GetValue<int>();

                        var seq = Enumerable.Concat(ArrayHelper.GetInt32Array(preceding_tokens[i, j].tolist()), new int[] { this.eot });

                        string key = string.Join(",", seq);

                        sequences[key] = sum_logprobs[i][j].item<int>();

                        if (sequences.Count >= this.beam_size)
                        {
                            break;
                        }
                    }
                }

                i++;
            }

            var tokens = new List<List<Tensor>>();

            foreach (var sequence in this.finished_sequences)
            {
                var keys = sequence.Keys;

                List<Tensor> list = new List<Tensor>();

                foreach(var key in keys)
                {
                    var array = ArrayHelper.GetIntArray(key);

                    Tensor t = torch.tensor(array);

                    list.Add(t);
                }

                tokens.Add(list);
            }

            List<List<float>> logprobs = new List<List<float>>();
            
            foreach(var sequence in this.finished_sequences)
            {
                logprobs.Add(sequence.Values.Select(item=> (float)item).ToList());
            }                      

            return (tokens, logprobs);
        }
    }
}

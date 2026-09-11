using Newtonsoft.Json;
using NumSharp;
using TorchSharp;
using WhisperSharp.Helper;
using WhisperSharp.Model;
using static TorchSharp.torch;
using nn = TorchSharp.torch.nn;

namespace WhisperSharp
{
    public partial class Whisper : nn.Module
    {
        private Tensor alignment_heads;
        public ModelDimensions dims { get; set; }
        public AudioEncoder encoder { get; set; }
        public TextDecoder decoder { get; set; }        

        public torch.Device device => this.parameters().FirstOrDefault()?.device?? new torch.Device(DeviceType.CPU);
        public bool is_multilingual => this.dims.n_vocab >= 51865;
        public int num_languages => this.dims.n_vocab - 51765 - (this.is_multilingual ? 1 : 0);

        public Whisper(ModelDimensions dims):base(nameof(Whisper))
        {            
            this.dims = dims;

            this.encoder = new AudioEncoder(
                dims.n_mels,
                dims.n_audio_ctx,
                dims.n_audio_state,
                dims.n_audio_head,
                dims.n_audio_layer
            );

            this.decoder = new TextDecoder(
                dims.n_vocab,
                dims.n_text_ctx,
                dims.n_text_state,
                dims.n_text_head,
                dims.n_text_layer
            );

            // use the last half among the decoder layers for time alignment by default;
            // to use a specific set of heads, see `set_alignment_heads()` below.
            var all_heads = torch.zeros(dims.n_text_layer, dims.n_text_head, dtype: ScalarType.Bool);

            var start = (int)Math.Floor(this.dims.n_text_layer / 2.0);

            all_heads[start..] = true;

            var sparse = all_heads.to_sparse();

            this.alignment_heads = sparse;

            RegisterComponents();

            //this.register_buffer("alignment_heads", sparse, persistent: false);
        }

        public void set_alignment_heads(string dump)
        {
            var bytes = Base85Helper.Decode(dump);

            var decompressBytes = ZipHelper.DecompressBytes(bytes);
    
            var array = np.frombuffer(decompressBytes, dtype: typeof(bool)).copy();

            var boolArray = ArrayHelper.GetBoolListArray(array.tolist());

            var mask = torch.from_array(boolArray).reshape(
                this.dims.n_text_layer, this.dims.n_text_head
            );

            var sparse = mask.to_sparse();

            this.alignment_heads = sparse;            

            //this.register_buffer("alignment_heads", sparse, persistent: false);
        }
        

        public static bool[] BytesToBoolArray(byte[] bytes)
        {
            bool[] result = new bool[bytes.Length];

            for (int i = 0; i < bytes.Length; i++)
            {
                result[i] = bytes[i] != 0;
            }

            return result;
        }

        public Tensor embed_audio(Tensor mel)
        {
            return this.encoder.forward(mel);
        }

        public Tensor logits(Tensor tokens, Tensor audio_features)
        {
            return this.decoder.forward(tokens, audio_features);
        }

        public Tensor forward(Tensor mel, Tensor tokens)
        {
            return this.decoder.forward(tokens, this.encoder.forward(mel));
        }            

        /// <summary>
        /// The `MultiHeadAttention` module optionally accepts `kv_cache` which stores the key and value
        /// tensors calculated for the previous positions.This method returns a dictionary that stores
        /// all caches, and the necessary hooks for the key and value projection modules that save the
        /// intermediate tensors to be reused during later calculations.
        /// </summary>
        /// <param name="cache"></param>
        /// <returns>cache : Dict[nn.Module, torch.Tensor]
        ///                  A dictionary object mapping the key/value projection modules to its cache
        ///          hooks : List[RemovableHandle]
        ///                  List of PyTorch RemovableHandle objects to stop the hooks to be called</returns>
        public (Dictionary<nn.Module<Tensor, Tensor>, Tensor>, List<dynamic>) install_kv_cache_hooks(Dictionary<nn.Module<Tensor, Tensor>, Tensor> cache = null)
        {
            cache ??= new Dictionary<nn.Module<Tensor, Tensor>, Tensor>();

            var hooks = new List<dynamic>();

            Func<nn.Module<Tensor, Tensor>, Tensor, Tensor, Tensor> save_to_cache = (module, _, output) =>
            {
                if (!cache.ContainsKey(module) || output.shape[1] > this.dims.n_text_ctx)
                {
                    //save as-is, for the first token or cross attention
                    cache[module] = output;
                }
                else
                {
                    cache[module] = torch.cat([cache[module], output], dim: 1).detach();
                }

                return cache[module];
            };

            Action<nn.Module> install_hooks = (layer) =>
            {
                if (layer is MultiHeadAttention m)
                {
                    hooks.Add(m.key.register_forward_hook(save_to_cache));
                    hooks.Add(m.value.register_forward_hook(save_to_cache));
                }
            };

            this.decoder.apply(install_hooks);         

            return (cache, hooks);
        }

        /// <summary>
        /// Load a Whisper ASR model
        /// </summary>
        /// <param name="name">one of the official model names listed by `whisper.available_models()`, or
        /// path to a model checkpoint containing the model dimensions and the model state_dict.
        /// </param>
        /// <param name="device">the PyTorch device to put the model into</param>
        /// <param name="data_root">path to save dat file</param>
        /// <param name="in_memory">whether to preload the model weights into host memory</param>
        /// <returns></returns>
        public static Whisper load_model(string name, dynamic device = null, string data_root = null, bool in_memory = false)
        {
            if (device == null)
            {
                device = torch.cuda.is_available() ? "cuda" : "cpu";
            }      

            string alignment_heads = null;

            if (Initor.MODELS.ContainsKey(name))
            {
                alignment_heads = Initor.ALIGNMENT_HEADS[name];
            }   
            
            Whisper whisper = null;

            string jsonFilePath = PathHelper.GetConfigFilePath($"{name}.json");

            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"File not found:{jsonFilePath}");
            }

            ModelDimensions dims = JsonConvert.DeserializeObject<ModelDimensions>(File.ReadAllText(jsonFilePath));

            whisper = new Whisper(dims);

            Dictionary<string, bool> loadedParameters = new Dictionary<string, bool>();
            Dictionary<string, Tensor> readTensors = new Dictionary<string, Tensor>();

            string dataFileName = $"{name}.dat";
            string datFilePath =!string.IsNullOrEmpty(data_root) ? Path.Combine(data_root, dataFileName): PathHelper.GetDataFilePath(dataFileName);

            if (!File.Exists(datFilePath))
            {
                throw new FileNotFoundException($"File not found:{datFilePath}");
            }

            var model = whisper.load(datFilePath, false, null, loadedParameters, readTensors);

            model.load_state_dict(readTensors, false);

            whisper = model as Whisper;

            if (alignment_heads != null)
            {
                whisper.set_alignment_heads(alignment_heads);
            }

            return ModuleExtensionMethods.to<Whisper>(whisper, device);
        }     
    }
}

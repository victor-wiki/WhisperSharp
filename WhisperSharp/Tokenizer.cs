using NumSharp;
using TiktokenSharp;
using TiktokenSharp.Model;
using WhisperSharp.Extension;
using WhisperSharp.Helper;

namespace WhisperSharp
{
    /// <summary>
    /// A thin wrapper around `tiktoken` providing quick access to special tokens
    /// </summary>
    public partial class Tokenizer
    {
        private TikToken encoding;
        private int num_languages;        
        private string task;       
        private Dictionary<string, List<int>> special_tokens = new Dictionary<string, List<int>>();
        private List<string> specials;

        public string language { get; set; }
        public List<int> sot_sequence { get; set; }
        public List<string> special_tokens_set => this.special_tokens?.Keys?.ToList();
        public List<int> special_tokens_values => this.special_tokens?.Values?.SelectMany(item => item)?.ToList();
        public int? timestamp_begin => this.special_tokens?["<|0.00|>"]?.FirstOrDefault();
        public int? eot => this.special_tokens?["<|endoftext|>"]?.FirstOrDefault();
        public int? transcribe => this.special_tokens?["<|transcribe|>"]?.FirstOrDefault();
        public int? translate => this.special_tokens?["<|translate|>"]?.FirstOrDefault();
        public int? sot => this.special_tokens?["<|startoftranscript|>"]?.FirstOrDefault();
        public int? sot_lm => this.special_tokens?["<|startoflm|>"]?.FirstOrDefault();
        public int? sot_prev => this.special_tokens?["<|startofprev|>"]?.FirstOrDefault();
        public int? no_speech => this.special_tokens?["<|nospeech|>"]?.FirstOrDefault();
        public int no_timestamps => this.special_tokens?["<|notimestamps|>"]?.FirstOrDefault()??0;

        public Tokenizer(bool multilingual, int num_languages = 99, string language = null, string task = null)
        {
            if (language != null)
            {
                language = language.ToLower();

                if (!LANGUAGES.ContainsKey(language))
                {
                    if (TO_LANGUAGE_CODE.ContainsKey(language))
                    {
                        language = TO_LANGUAGE_CODE[language];
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported language: {language}");
                    }
                }
            }

            string encoding_name = null;

            if (multilingual)
            {
                encoding_name = "multilingual";
                language = language ?? "en";
                task = task ?? "transcribe";
            }
            else
            {
                encoding_name = "gpt2";
                language = null;
                task = null;
            }

            this.encoding = this.get_encoding(name: encoding_name, num_languages: num_languages);
            this.num_languages = num_languages;
            this.language = language;
            this.task = task;          

            this.post();          
        }

        public TikToken get_encoding(string name = "gpt2", int num_languages = 99)
        {
            var vocab_path = PathHelper.GetAssetsFilePath($"{name}.tiktoken");

            var lines = File.ReadAllLines(vocab_path);

            Dictionary<ReadOnlyMemory<byte>, int> ranks = new Dictionary<ReadOnlyMemory<byte>, int>(new ReadOnlyMemoryByteComparer());

            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    var items = line.Split(' ').ToArray();
                    string token = items[0];
                    string rank = items[1];

                    ranks.Add(token!="="? Convert.FromBase64String(token): new byte[0], int.Parse(rank));
                }
            }

            var n_vocab = ranks.Count;
            var special_tokens = new Dictionary<string, int>();

            this.specials = GetSpecials(num_languages);

            foreach (var token in this.specials)
            {
                special_tokens[token] = n_vocab;            

                n_vocab += 1;
            }

            EncodingSettingModel setting = new EncodingSettingModel()
            {
                Name = Path.GetFileName(vocab_path),
                ExplicitNVocab = n_vocab,
                PatStr = """'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+""",
                MergeableRanks = ranks,
                SpecialTokens = special_tokens
            };

            foreach (var special_token in special_tokens)
            {
                this.special_tokens.Add(special_token.Key, new List<int>() { special_token.Value });
            }

            this.encoding = new TikToken(setting);

            return this.encoding;
        }

        public void post()
        {
            int sot = this.special_tokens["<|startoftranscript|>"].FirstOrDefault();
            int translate = this.special_tokens["<|translate|>"].FirstOrDefault();
            int transcribe = this.special_tokens["<|transcribe|>"].FirstOrDefault();

            var langs = LANGUAGES.Keys.Take(this.num_languages).ToArray();
            List<int> sot_sequence = new List<int>() { sot };

            if (this.language != null)
            {
                sot_sequence.Add(sot + 1 + langs.IndexOf(this.language));
            }

            if (this.task != null)
            {
                int task_token = this.task == "transcribe" ? transcribe : translate;
                sot_sequence.Add(task_token);
            }

            this.sot_sequence = sot_sequence;
        }

        public List<int> encode(string text, HashSet<string> allowedSpecial = null, HashSet<string> disallowedSpecial = null, SpecialTokensMode allowedSpecialMode = SpecialTokensMode.Custom, SpecialTokensMode disallowedSpecialMode = SpecialTokensMode.Custom)
        {
            return this.encoding.Encode(text, allowedSpecial, disallowedSpecial, allowedSpecialMode, disallowedSpecialMode);
        }

        public string decode(List<int> token_ids)
        {
            var token_ids2 = token_ids.Where(item => item < timestamp_begin).ToList();

            return this.encoding.Decode(token_ids2);
        }

        private bool is_special_token(int token)
        {
            return this.special_tokens_values?.Contains(token) == true;
        }

        /// <summary>
        /// Returns the token id corresponding to the value of the `language` field
        /// </summary>
        /// <returns></returns>
        public int language_token
        {
            get
            {
                if (this.language == null)
                {
                    throw new Exception("This tokenizer does not have language token configured");
                }

                return this.to_language_token(this.language).FirstOrDefault();
            }
        }

        public List<int> to_language_token(string language)
        {
            string key = $"<|{language}|>";
            var token = this.special_tokens != null && this.special_tokens.ContainsKey(key) ? this.special_tokens[key] : null;

            if (token != null)
            {
                return token;
            }

            return null;
        }

        public List<int> all_language_tokens
        {
            get
            {
                List<int> list = new List<int>();

                foreach (var item in this.special_tokens)
                {
                    string token = item.Key.Trim("<|>".ToArray());

                    if (LANGUAGES.ContainsKey(token))
                    {
                        list.Add(item.Value.FirstOrDefault());
                    }
                }

                return list.Take(this.num_languages).ToList();
            }
        }

        public List<string> all_language_codes
        {
            get
            {
                List<string> list = new List<string>();

                foreach (var item in this.all_language_tokens)
                {
                    list.Add(this.decode([item]).Trim('<','|','>'));
                }

                return list;
            }
        }

        public List<int> sot_sequence_including_notimestamps
        {
            get
            {
                return Enumerable.Concat(this.sot_sequence,  new int[] { this.no_timestamps }).ToList();
            }
        }

        /// <summary>
        ///  Returns the list of tokens to suppress in order to avoid any speaker tags or non-speech
        ///  annotations, to prevent sampling texts that are not actually spoken in the audio, e.g.
        ///  
        ///  - ♪♪♪
        ///  - ( SPEAKING FOREIGN LANGUAGE )
        ///  - [DAVID] Hey there,
        ///  
        ///  keeping basic punctuations like commas, periods, question marks, exclamation points, etc.
        /// </summary>
        /// <returns></returns>
        public List<int> non_speech_tokens
        {
            get
            {
                var symbols = new List<string>();

                symbols.AddRange(@"""#()*+/:;<=>@[\]^_`{|}~「」『』".Select(item=>item.ToString()));

                symbols.AddRange("<< >> <<< >>> -- --- -( -[ (' (\" (( )) ((( ))) [[ ]] {{ }} ♪♪ ♪♪♪".Split(' ').ToArray());                   

                // symbols that may be a single token or multiple tokens depending on the tokenizer.
                // In case they're multiple tokens, suppress the first token, which is safe because:
                // These are between U+2640 and U+267F miscellaneous symbols that are okay to suppress
                // in generations, and in the 3-byte UTF-8 representation they share the first two bytes.

                var miscellaneous = new List<string>();

                miscellaneous.AddRange("♩♪♫♬♭♮♯".Select(item => item.ToString()));

                //allow hyphens "-" and single quotes "'" between words, but not at the beginning of a word
                var result = new List<int>() { this.encoding.Encode(" -")[0], this.encoding.Encode(" '")[0] };

                foreach (var symbol in Enumerable.Concat(symbols, miscellaneous))
                {
                    var list = new List<List<int>>();

                    list.Add(this.encoding.Encode(symbol));
                    list.Add(this.encoding.Encode(" " + symbol));

                    foreach (var tokens in list)
                    {
                        if (tokens.Count == 1 || miscellaneous.Contains(symbol))
                        {
                            var token = tokens[0];

                            result.Add(token);
                        }
                    }
                }

                result.Sort();

                return result.Distinct().ToList();
            }
        }

        public dynamic split_to_word_tokens(List<int> tokens)
        {
            if ((new string[] { "zh", "ja", "th", "lo", "my", "yue" }).Contains(this.language))
            {
                // These languages don't typically use spaces, so it is difficult to split words
                // without morpheme analysis. Here, we instead split words at any
                // position where the tokens are decoded as valid unicode points

                return this.split_tokens_on_unicode(tokens);
            }

            return this.split_tokens_on_spaces(tokens);
        }

        public (List<string>, List<List<int>>) split_tokens_on_unicode(List<int> tokens)
        {
            var decoded_full = this.decode_with_timestamps(tokens);
            char replacement_char = '\ufffd';

            var words = new List<string>();
            var word_tokens = new List<List<int>>();
            var current_tokens = new List<int>();
            var unicode_offset = 0;

            foreach (var token in tokens)
            {
                current_tokens.Add(token);
                string decoded = this.decode_with_timestamps(current_tokens);

                if (!decoded.Contains(replacement_char)
                || decoded_full[unicode_offset + decoded.IndexOf(replacement_char)] == replacement_char)
                {
                    words.Add(decoded);
                    word_tokens.Add(current_tokens);
                    current_tokens = new List<int>();
                    unicode_offset += decoded.Length;
                }
            }

            return (words, word_tokens);
        }

        public (List<string>, List<List<int>>) split_tokens_on_spaces(List<int> tokens)
        {
            var (subwords, subword_tokens_list) = this.split_tokens_on_unicode(tokens);

            var words = new List<string>();
            var word_tokens = new List<List<int>>();

            int i = 0;

            foreach (var subword in subwords)
            {
                var subword_tokens = subword_tokens_list[i];

                var special = subword_tokens[0] >= this.eot;
                var with_space = subword.StartsWith(" ");
                var punctuation = """!"#$%&'()*+,-./:;<=>?@[\]^_`{|}~""".Contains(subword.Trim());

                if (special || with_space || punctuation || words.Count == 0)
                {
                    words.Add(subword);
                    word_tokens.Add(subword_tokens);
                }
                else
                {
                    words[words.Count - 1] = words.LastOrDefault() + subword;
                    word_tokens[word_tokens.Count - 1].AddRange(subword_tokens);
                }

                i++;
            }

            return (words, word_tokens);
        }

        /// <summary>
        /// Timestamp tokens are above other special tokens' id range and are ignored by `decode()`.
        /// This method decodes given tokens with timestamps tokens annotated, e.g. "<|1.08|>".
        /// </summary>
        /// <param name="token_ids"></param>
        /// <returns></returns>
        public string decode_with_timestamps(List<int> token_ids)
        {
            return this.encoding.Decode(token_ids);
        }

        public static List<string> GetSpecials(int num_languages)
        {
            List<string> list = new List<string>() { "<|endoftext|>", "<|startoftranscript|>" };

            foreach (var item in LANGUAGES.Keys.Take(num_languages))
            {
                list.Add($"<|{item}|>");
            }

            list.AddRange(
                "<|translate|>",
                "<|transcribe|>",
                "<|startoflm|>",
                "<|startofprev|>",
                "<|nospeech|>",
                "<|notimestamps|>");

            for (var i = 0; i < 1501; i++)
            {
                list.Add($"<|{(i * 0.02f).ToFixed(2)}|>");
            }

            return list;
        }       
    }
}

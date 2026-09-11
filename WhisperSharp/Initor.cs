using System.Net;

namespace WhisperSharp
{
    public class Initor
    {
        public static readonly Dictionary<string, string> MODELS = new Dictionary<string, string>() 
        {
            {"tiny.en", "https://openaipublic.azureedge.net/main/whisper/models/d3dd57d32accea0b295c96e26691aa14d8822fac7d9d27d5dc00b4ca2826dd03/tiny.en.pt"},
            {"tiny", "https://openaipublic.azureedge.net/main/whisper/models/65147644a518d12f04e32d6f3b26facc3f8dd46e5390956a9424a650c0ce22b9/tiny.pt"},
            {"base.en", "https://openaipublic.azureedge.net/main/whisper/models/25a8566e1d0c1e2231d1c762132cd20e0f96a85d16145c3a00adf5d1ac670ead/base.en.pt"},
            {"base", "https://openaipublic.azureedge.net/main/whisper/models/ed3a0b6b1c0edf879ad9b11b1af5a0e6ab5db9205f891f668f8b0e6c6326e34e/base.pt"},
            {"small.en", "https://openaipublic.azureedge.net/main/whisper/models/f953ad0fd29cacd07d5a9eda5624af0f6bcf2258be67c92b79389873d91e0872/small.en.pt"},
            {"small", "https://openaipublic.azureedge.net/main/whisper/models/9ecf779972d90ba49c06d968637d720dd632c55bbf19d441fb42bf17a411e794/small.pt"},
            {"medium.en", "https://openaipublic.azureedge.net/main/whisper/models/d7440d1dc186f76616474e0ff0b3b6b879abc9d1a4926b7adfa41db2d497ab4f/medium.en.pt"},
            {"medium", "https://openaipublic.azureedge.net/main/whisper/models/345ae4da62f9b3d59415adc60127b97c714f32e89e936602e85993674d08dcb1/medium.pt"},
            {"large-v1", "https://openaipublic.azureedge.net/main/whisper/models/e4b87e7e0bf463eb8e6956e646f1e277e901512310def2c24bf0e11bd3c28e9a/large-v1.pt"},
            {"large-v2", "https://openaipublic.azureedge.net/main/whisper/models/81f7c96c852ee8fc832187b0132e569d6c3065a3252ed18e56effd0b6a73e524/large-v2.pt"},
            {"large-v3", "https://openaipublic.azureedge.net/main/whisper/models/e5b1a55b89c1367dacf97e3e19bfd829a01529dbfdeefa8caeb59b3f1b81dadb/large-v3.pt"},
            {"large", "https://openaipublic.azureedge.net/main/whisper/models/e5b1a55b89c1367dacf97e3e19bfd829a01529dbfdeefa8caeb59b3f1b81dadb/large-v3.pt"},
            {"large-v3-turbo", "https://openaipublic.azureedge.net/main/whisper/models/aff26ae408abcba5fbf8813c21e62b0941638c5f6eebfb145be0c9839262a19a/large-v3-turbo.pt"},
            {"turbo", "https://openaipublic.azureedge.net/main/whisper/models/aff26ae408abcba5fbf8813c21e62b0941638c5f6eebfb145be0c9839262a19a/large-v3-turbo.pt"}
        };

        /// <summary>
        /// base85-encoded (n_layers, n_heads) boolean arrays indicating the cross-attention heads that are
        /// highly correlated to the word-level timing, i.e. the alignment between audio and text tokens.
        /// </summary>
        public static readonly Dictionary<string, string> ALIGNMENT_HEADS = new Dictionary<string, string>()
        {
            {"tiny.en", "ABzY8J1N>@0{>%R00Bk>$p{7v037`oCl~+#00"},
            {"tiny", "ABzY8bu8Lr0{>%RKn9Fp%m@SkK7Kt=7ytkO"},
            {"base.en", "ABzY8;40c<0{>%RzzG;p*o+Vo09|#PsxSZm00"},
            {"base", "ABzY8KQ!870{>%RzyTQH3`Q^yNP!>##QT-<FaQ7m"},
            {"small.en", "ABzY8>?_)10{>%RpeA61k&I|OI3I$65C{;;pbCHh0B{qLQ;+}v00"},
            {"small", "ABzY8DmU6=0{>%Rpa?J`kvJ6qF(V^F86#Xh7JUGMK}P<N0000"},
            {"medium.en", "ABzY8usPae0{>%R7<zz_OvQ{)4kMa0BMw6u5rT}kRKX;$NfYBv00*Hl@qhsU00"},
            {"medium", "ABzY8B0Jh+0{>%R7}kK1fFL7w6%<-Pf*t^=N)Qr&0RR9"},
            {"large-v1", "ABzY8r9j$a0{>%R7#4sLmoOs{s)o3~84-RPdcFk!JR<kSfC2yj"},
            {"large-v2", "ABzY8zd+h!0{>%R7=D0pU<_bnWW*tkYAhobTNnu$jnkEkXqp)j;w1Tzk)UH3X%SZd&fFZ2fC2yj"},
            {"large-v3", "ABzY8gWO1E0{>%R7(9S+Kn!D~%ngiGaR?*L!iJG9p-nab0JQ=-{D1-g00"},
            {"large", "ABzY8gWO1E0{>%R7(9S+Kn!D~%ngiGaR?*L!iJG9p-nab0JQ=-{D1-g00"},
            {"large-v3-turbo", "ABzY8j^C+e0{>%RARaKHP%t(lGR*)0g!tONPyhe`"},
            {"turbo", "ABzY8j^C+e0{>%RARaKHP%t(lGR*)0g!tONPyhe`"},
        };

        public static dynamic Download(string url, string root, bool in_memory)
        {
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            var items = url.Split('/');
            var expected_sha256 = items[items.Length - 2];

            var download_target = Path.Combine(root, Path.GetFileName(url));

            bool found = false;

            if (!File.Exists(download_target))
            {
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.DownloadFile(url, download_target);
                    }

                    FileInfo fileInfo = new FileInfo(download_target);

                    found = fileInfo.Length > 0;

                    if (!found)
                    {
                        File.Delete(download_target);
                    }
                }
                catch (Exception ex)
                {
                    found = found;
                }
            }
            else
            {
                found = true;
            }

            if (found)
            {
                if (in_memory)
                {
                    return GetFileContent(download_target);
                }
                else
                {
                    return download_target;
                }
            }

            return null;
        }

        public static byte[] GetFileContent(string filePath)
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                MemoryStream ms = new MemoryStream();

                fs.CopyTo(ms);

                return ms.ToArray();
            }
        }

        public static List<string> AvailableModels()
        {
            return MODELS.Keys.ToList();
        }        
    }
}

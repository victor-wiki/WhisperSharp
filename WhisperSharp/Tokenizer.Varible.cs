namespace WhisperSharp
{
    public partial class Tokenizer
    {
        public static readonly Dictionary<string, string> LANGUAGES = new Dictionary<string, string>()
        {
            {"en", "english"},
            {"zh", "chinese"},
            {"de", "german"},
            {"es", "spanish"},
            {"ru", "russian"},
            {"ko", "korean"},
            {"fr", "french"},
            {"ja", "japanese"},
            {"pt", "portuguese"},
            {"tr", "turkish"},
            {"pl", "polish"},
            {"ca", "catalan"},
            {"nl", "dutch"},
            {"ar", "arabic"},
            {"sv", "swedish"},
            {"it", "italian"},
            {"id", "indonesian"},
            {"hi", "hindi"},
            {"fi", "finnish"},
            {"vi", "vietnamese"},
            {"he", "hebrew"},
            {"uk", "ukrainian"},
            {"el", "greek"},
            {"ms", "malay"},
            {"cs", "czech"},
            {"ro", "romanian"},
            {"da", "danish"},
            {"hu", "hungarian"},
            {"ta", "tamil"},
            {"no", "norwegian"},
            {"th", "thai"},
            {"ur", "urdu"},
            {"hr", "croatian"},
            {"bg", "bulgarian"},
            {"lt", "lithuanian"},
            {"la", "latin"},
            {"mi", "maori"},
            {"ml", "malayalam"},
            {"cy", "welsh"},
            {"sk", "slovak"},
            {"te", "telugu"},
            {"fa", "persian"},
            {"lv", "latvian"},
            {"bn", "bengali"},
            {"sr", "serbian"},
            {"az", "azerbaijani"},
            {"sl", "slovenian"},
            {"kn", "kannada"},
            {"et", "estonian"},
            {"mk", "macedonian"},
            {"br", "breton"},
            {"eu", "basque"},
            {"is", "icelandic"},
            {"hy", "armenian"},
            {"ne", "nepali"},
            {"mn", "mongolian"},
            {"bs", "bosnian"},
            {"kk", "kazakh"},
            {"sq", "albanian"},
            {"sw", "swahili"},
            {"gl", "galician"},
            {"mr", "marathi"},
            {"pa", "punjabi"},
            {"si", "sinhala"},
            {"km", "khmer"},
            {"sn", "shona"},
            {"yo", "yoruba"},
            {"so", "somali"},
            {"af", "afrikaans"},
            {"oc", "occitan"},
            {"ka", "georgian"},
            {"be", "belarusian"},
            {"tg", "tajik"},
            {"sd", "sindhi"},
            {"gu", "gujarati"},
            {"am", "amharic"},
            {"yi", "yiddish"},
            {"lo", "lao"},
            {"uz", "uzbek"},
            {"fo", "faroese"},
            {"ht", "haitian creole"},
            {"ps", "pashto"},
            {"tk", "turkmen"},
            {"nn", "nynorsk"},
            {"mt", "maltese"},
            {"sa", "sanskrit"},
            {"lb", "luxembourgish"},
            {"my", "myanmar"},
            {"bo", "tibetan"},
            {"tl", "tagalog"},
            {"mg", "malagasy"},
            {"as", "assamese"},
            {"tt", "tatar"},
            {"haw", "hawaiian"},
            {"ln", "lingala"},
            {"ha", "hausa"},
            {"ba", "bashkir"},
            {"jw", "javanese"},
            {"su", "sundanese"},
            { "yue", "cantonese"},
        };

        //language code lookup by name, with a few language aliases
        public static Dictionary<string, string> TO_LANGUAGE_CODE
        {
            get
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();

                foreach (var kp in LANGUAGES)
                {
                    dict.Add(kp.Value, kp.Key);
                }

                Dictionary<string, string> dict2 = new Dictionary<string, string>()
                {
                    {"burmese", "my"},
                    {"valencian", "ca"},
                    {"flemish", "nl"},
                    {"haitian", "ht"},
                    {"letzeburgesch", "lb"},
                    {"pushto", "ps"},
                    {"panjabi", "pa"},
                    {"moldavian", "ro"},
                    {"moldovan", "ro"},
                    {"sinhalese", "si"},
                    {"castilian", "es"},
                    { "mandarin", "zh"},
                };

                foreach(var kp in dict2)
                {
                    dict.Add(kp.Key, kp.Value);
                }

                return dict;
            }
        }       
    }
}

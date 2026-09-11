using System.Text.RegularExpressions;

namespace WhisperSharp.Writer
{
    public class SubtitlesWriter : ResultWriter
    {
        protected virtual bool always_include_hours { get; }
        protected virtual string decimal_marker { get; }

        public SubtitlesWriter():base(){}

        public SubtitlesWriter(string output_dir) : base(output_dir) { }

        public override string get_result(Dictionary<string, dynamic> result, Dictionary<string, dynamic> options = null, dynamic kwargs = null)
        {
            return null;
        }

        protected IEnumerable<(string start, string end, string text)> iterate_result(Dictionary<string, dynamic> result,
                                    Dictionary<string, dynamic> options = null,
                                    int? max_line_width = null,
                                    int? max_line_count = null,
                                    bool? highlight_words = false,
                                    int? max_words_per_line = null)
        {
            options ??= new Dictionary<string, dynamic>();
            max_line_width = max_line_width ?? (options.ContainsKey("max_line_width") ? options["max_line_width"] : null);
            max_line_count = max_line_count ?? (options.ContainsKey("max_line_count") ? options["max_line_count"] : null);
            highlight_words = highlight_words ?? (options.ContainsKey("highlight_words") ? options["highlight_words"] : false);
            max_words_per_line = max_words_per_line ?? (options.ContainsKey("max_words_per_line") ? options["max_words_per_line"] : null);
            var preserve_segments = max_line_count == null ? max_line_width == null : false;
            max_line_width = max_line_width ?? 1000;
            max_words_per_line = max_words_per_line ?? 1000;

            if (result["segments"].Count > 0 && result["segments"][0].ContainsKey("words"))
            {
                var subtitles = iterate_subtitles(result, max_words_per_line.Value, max_line_width.Value, max_line_count, preserve_segments);

                foreach (var subtitle in subtitles)
                {
                    var subtitle_start = this.format_timestamp((float)subtitle[0]["start"]);
                    var subtitle_end = this.format_timestamp((float)subtitle.LastOrDefault()["end"]);
                    var subtitle_text = string.Join("", subtitle.Select(item => item["word"]));

                    if (highlight_words == true)
                    {
                        var last = subtitle_start;
                        var all_words = subtitle.Select(item => item["word"] as string).ToArray();

                        int i = 0;

                        foreach (var this_word in subtitle)
                        {
                            var start = this.format_timestamp(this_word["start"]);
                            var end = this.format_timestamp(this_word["end"]);

                            if (last != start)
                            {
                                yield return (last, start, subtitle_text);
                            }

                            int j = 0;

                            List<string> wordList = new List<string>();

                            foreach (var word in all_words)
                            {
                                wordList.Add(i == j ? Regex.Replace(word, @"^(\s*)(.*)$", @"\1<u>\2</u>") : word);

                                j++;
                            }

                            yield return (start, end, string.Join("", wordList));

                            last = end;

                            i++;
                        }
                    }    
                    else
                    {
                        yield return (subtitle_start, subtitle_end, subtitle_text);
                    }
                }
            }
            else
            {
                foreach (var segment in result["segments"])
                {
                    var segment_start = this.format_timestamp((float)segment["start"]);
                    var segment_end = this.format_timestamp((float)segment["end"]);
                    var segment_text = segment["text"].Trim().Replace("-->", "->");

                    yield return (segment_start, segment_end, segment_text);
                }
            }
        }

        private IEnumerable<List<Dictionary<string, dynamic>>> iterate_subtitles(Dictionary<string, dynamic> result, int max_words_per_line,
            int max_line_width, int? max_line_count, bool preserve_segments)
        {
            var line_len = 0;
            var line_count = 1;

            // the next subtitle to yield (a list of word timings with whitespace)
            var subtitle = new List<Dictionary<string, dynamic>>();

            float last = get_start(result["segments"]) ?? 0.0f;

            foreach (Dictionary<string, dynamic> segment in result["segments"])
            {
                var chunk_index = 0;
                var words_count = max_words_per_line;
                var words = segment["words"] as List<Dictionary<string,dynamic>>;

                while (chunk_index < words.Count)
                {
                    var remaining_words = words.Count - chunk_index;

                    if (max_words_per_line > words.Count - chunk_index)
                    {
                        words_count = remaining_words;
                    }

                    int i = 0;

                    foreach (var original_timing in words.Skip(chunk_index).Take(chunk_index + words_count - chunk_index))
                    {
                        var timing = original_timing;

                        var long_pause = (!preserve_segments && timing["start"] - last > 3.0);

                        var has_room = line_len + timing["word"].Length <= max_line_width;
                        var seg_break = i == 0 && subtitle.Count > 0 && preserve_segments;

                        if (line_len > 0 && has_room && !long_pause && !seg_break)
                        {
                            //line continuation
                            line_len += timing["word"].Length;
                        }
                        else
                        {
                            // new line
                            timing["word"] = timing["word"].Trim();

                            if (subtitle.Count > 0
                                && max_line_count != null
                                && (long_pause || line_count >= max_line_count)
                                || seg_break)
                            {
                                // subtitle break
                                yield return subtitle;

                                subtitle = [];
                                line_count = 1;
                            }
                            else if (line_len > 0)
                            {
                                //line break
                                line_count += 1;
                                timing["word"] = "\n" + timing["word"];
                            }

                            line_len = timing["word"].Trim().Length;
                        }

                        subtitle.Add(timing);

                        last = (float)timing["start"];

                        i++;
                    }

                    chunk_index += max_words_per_line;
                }
            }

            if (subtitle.Count > 0)
            {
                yield return subtitle;
            }
        }

        public string format_timestamp(float seconds)
        {
            return Utils.format_timestamp(
                seconds: seconds,
                always_include_hours: this.always_include_hours,
                decimal_marker: this.decimal_marker
            );
        }

        public float? get_start(List<Dictionary<string, dynamic>> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                var words = segment["words"] as List<Dictionary<string, dynamic>>;

                for (var j = 0; j < words.Count; j++)
                {
                    if (j == 0)
                    {
                        return Convert.ToSingle(words[j]["start"]);
                    }
                }
            }

            var defaultValue = segments != null ? segments.LastOrDefault()?["start"] ?? null : null;

            if (defaultValue != null)
            {
                return Convert.ToSingle(defaultValue);
            }

            return null;
        }
    }
}

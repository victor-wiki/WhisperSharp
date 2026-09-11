namespace WhisperSharp.Extension
{
    public static class FloatExtension
    {
        public static string ToFixed(this float value, int number)
        {
            return value.ToString("0." + string.Join("", Enumerable.Repeat("0", number)));
        }       
    }
}

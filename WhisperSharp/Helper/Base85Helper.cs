namespace WhisperSharp.Helper
{
    public class Base85Helper
    {
        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&()*+-;<=>?@^_`{|}~";

        public static byte[] Decode(string input)
        {
            var result = new List<byte>();
            int block = 0;
            int count = 0;

            foreach (char c in input)
            {
                int value = Alphabet.IndexOf(c);

                if (value < 0)
                {
                    continue;
                }

                block = block * 85 + value;
                count++;

                if (count == 5)
                {
                    result.Add((byte)(block >> 24));
                    result.Add((byte)(block >> 16));
                    result.Add((byte)(block >> 8));
                    result.Add((byte)block);

                    block = 0;
                    count = 0;
                }
            }

            if (count > 0)
            {
                for (int i = count; i < 5; i++)
                {
                    block = block * 85 + 84;
                }                  

                int bytesToWrite = count - 1;

                for (int i = 0; i < bytesToWrite; i++)
                {
                    result.Add((byte)(block >> (24 - 8 * i)));
                }                   
            }

            return result.ToArray();
        }
    }
}

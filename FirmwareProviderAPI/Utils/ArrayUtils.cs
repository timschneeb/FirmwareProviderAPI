namespace FirmwareProviderAPI.Utils
{
    public static class ArrayUtils
    {
        public static int FindPattern(this byte[] src, byte[] pattern)
        {
            var maxFirstCharSlot = src.Length - pattern.Length + 1;
            for (var i = 0; i < maxFirstCharSlot; i++)
            {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;
        
                // found a match on first byte, now try to match rest of the pattern
                for (var j = pattern.Length - 1; j >= 1; j--) 
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        }
    }
}
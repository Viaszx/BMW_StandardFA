using System;
using System.Security.Cryptography;
using System.Linq;

namespace StandardFA
{
    public class FASign
    {
        public byte[] GenerateHMAC(byte[] message, string keySelector)
        {
            byte[] key = GetKeyForSelector(keySelector);

            using (HMACSHA1 hmac = new HMACSHA1(key))
            {
                byte[] result = hmac.ComputeHash(message);
                return result;
            }
        }

        private byte[] GetKeyForSelector(string keySelector)
        {
            switch (keySelector)
            {
                case "type1":
                    return new byte[] { 0x30 }; // KEY_HERE
                case "type2":
                    return new byte[] { 0x03 }; // KEY_HERE
                default:
                    throw new ArgumentException("unknown");
            }
        }
    }
}

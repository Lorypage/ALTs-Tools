using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EasyCompressor;
using SevenZip.Compression.LZMA;

namespace RefreshToAccess2.Crypto
{
    internal class DataPacker
    {
        public static readonly string aesKey = "_hujinxin_433125200902120119_sb_";
        public static readonly string aesIV = "weedhackexploded";
        public static readonly string[] animals1 = { "🐯", "🐶", "🐵", "🐷", "🦁", "🐺" };
        public static readonly string[] animals0 = { "🐨", "🐮", "🐰", "🐼", "👽", "🐱" };
        //public static readonly string[] animals1 = { "🐯" };
        //public static readonly string[] animals0 = { "👽" };
        public static string PackData(bool useOldMethod, string data, int pinKey)
        {
            LZMACompressor compressor = new LZMACompressor();
            compressor.CompressionLevel=LZMACompressionLevel.Ultra;
            byte[] tmp1 = CryptoUtils.EncryptStringToBytes_Aes(data, Encoding.UTF8.GetBytes(aesKey), Encoding.UTF8.GetBytes(aesIV));
            byte[] tmp2 = byteOffset(tmp1, pinKey);
            byte[] tmp3= { };
            if (useOldMethod)
            {
                tmp3 = compressor.Compress(Encoding.UTF8.GetBytes(Animalize(tmp2)));
            }
            else
            {
                tmp3 = compressor.Compress(tmp2);
            }
            return Convert.ToBase64String(tmp3);
        }
        public static string UnpackData(bool useOldMethod, string data, int pinKey)
        {
            LZMACompressor compressor = new LZMACompressor();
            byte[] tmp1 = compressor.Decompress(Convert.FromBase64String(data));
            byte[] tmp2 = { };
            if (useOldMethod)
            {
                tmp2 = DeAnimalize(Encoding.UTF8.GetString(tmp1));
            }
            else
            {
                tmp2=tmp1;
            }
            byte[] tmp3 = byteDeOffset(tmp2, pinKey);
            return CryptoUtils.DecryptStringFromBytes_Aes(tmp3, Encoding.UTF8.GetBytes(aesKey), Encoding.UTF8.GetBytes(aesIV));
        }
        private static byte[] byteOffset(byte[] bytes, int pinKeyPattern)
        {
            byte[] offsetResult = new byte[bytes.Length];
            int[] offsetPattern = pinKeyPattern.ToString().Select(o => Convert.ToInt32(o) - 48).ToArray();
            for (int i = 0; i<bytes.Length; i++)
            {
                offsetResult[i]=(byte)((int)bytes[i]+offsetPattern[i%offsetPattern.Length]);
            }
            return offsetResult;
        }
        private static byte[] byteDeOffset(byte[] bytes, int pinKeyPattern)
        {
            byte[] offsetResult = new byte[bytes.Length];
            int[] offsetPattern = pinKeyPattern.ToString().Select(o => Convert.ToInt32(o) - 48).ToArray();
            for (int i = 0; i<bytes.Length; i++)
            {
                offsetResult[i]=(byte)((int)bytes[i]-offsetPattern[i%offsetPattern.Length]);
            }
            return offsetResult;
        }
        public static string Animalize(byte[] bytes)
        {
            Random rand = new Random();
            string animalizedString = "";
            BitArray bitArray = new BitArray(bytes);
            int animals0Len = animals0.Length;
            int animals1Len = animals1.Length;
            for (int i = 0; i<bitArray.Length; i++)
            {
                if (bitArray[i])
                {
                    animalizedString+=(animals1[rand.Next(animals1Len)]);
                }
                else
                {
                    animalizedString+=(animals0[rand.Next(animals0Len)]);
                }
            }
            return animalizedString;
        }
        public static byte[] DeAnimalize(string deAnimalizeTargetString)
        {
            byte[] result=new byte[deAnimalizeTargetString.Length/16];
            BitArray bitArray=new BitArray(deAnimalizeTargetString.Length/2);
            string tmp = "";
            for (int i = 0; i<bitArray.Length*2; i+=2)
            {
                tmp=deAnimalizeTargetString.Substring(i, 2);
                if (animals0.Any(tmp.Contains))
                {
                    bitArray.Set(i/2, false);
                }
                else
                {
                    bitArray.Set(i/2, true);
                }
                tmp="";
            }
            bitArray.CopyTo(result, 0);
            return result;
        }
    }
}
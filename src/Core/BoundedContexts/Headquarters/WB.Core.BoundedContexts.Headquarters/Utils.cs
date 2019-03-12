﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace WB.Core.BoundedContexts.Headquarters
{
    public static class Utils
    {
        private static readonly Regex RemoveNewLineRegEx = new Regex(@"\t|\n|\r", RegexOptions.Compiled);
        public static string RemoveNewLine(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : RemoveNewLineRegEx.Replace(value, " ");
        }
        
        public static long InKb(this long bytes)
        {
            return bytes / 1024;
        }

        public static string GetRandomAlphanumericString(int length)
        {
            const string nonAmbiguousAlphanumericCharacters = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
                
            return GetRandomString(length, nonAmbiguousAlphanumericCharacters);
        }

        public static string GetRandomString(int length, IEnumerable<char> characterSet)
        {
            if (length < 0)
                throw new ArgumentException("length must not be negative", "length");
            if (length > int.MaxValue / 8) 
                throw new ArgumentException("length is too big", "length");
            if (characterSet == null)
                throw new ArgumentNullException("characterSet");
            var characterArray = characterSet.Distinct().ToArray();
            if (characterArray.Length == 0)
                throw new ArgumentException("characterSet must not be empty", "characterSet");

            var bytes = new byte[length * 8];
            new RNGCryptoServiceProvider().GetBytes(bytes);
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                ulong value = BitConverter.ToUInt64(bytes, i * 8);
                result[i] = characterArray[value % (uint)characterArray.Length];
            }
            return new string(result);
        }
    }
}

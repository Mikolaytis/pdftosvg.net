﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    internal static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value) where TKey : notnull
        {
            if (dic.ContainsKey(key))
            {
                return false;
            }
            else
            {
                dic.Add(key, value);
                return true;
            }
        }

        public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key) where TKey : notnull
        {
            return dic.TryGetValue(key, out var value) ? value : default;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue defaultValue) where TKey : notnull
        {
            return dic.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}

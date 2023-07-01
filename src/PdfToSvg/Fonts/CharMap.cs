﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts
{
    internal class CharMap : IEnumerable<CharInfo>
    {
        private readonly Dictionary<uint, CharInfo> chars = new();
        private bool charsPopulated;

        public bool TryGetChar(uint charCode, out CharInfo foundChar) => chars.TryGetValue(charCode, out foundChar);

        private string ResolveUnicode(CharInfo ch, UnicodeMap toUnicode, SingleByteEncoding? explicitEncoding, bool preferSingleChar)
        {
            // PDF 1.7 section 9.10.2

            var pdfUnicode = toUnicode.GetUnicode(ch.CharCode);

            // Prio 1: Single char ToUnicode
            if (pdfUnicode != null && (!preferSingleChar || IsSingleChar(pdfUnicode)))
            {
                return pdfUnicode;
            }

            // Prio 2: Explicit PDF encoding
            if (explicitEncoding != null && ch.CharCode <= byte.MaxValue)
            {
                var encodingUnicode = explicitEncoding.GetUnicode((byte)ch.CharCode);
                if (encodingUnicode != null)
                {
                    return encodingUnicode;
                }
            }

            // Prio 3: Unicode from font CMap
            if (ch.Unicode.Length != 0 && ch.Unicode != CharInfo.NotDef)
            {
                return ch.Unicode;
            }

            // Prio 4: Multi char ToUnicode
            if (pdfUnicode != null)
            {
                return pdfUnicode;
            }

            // Prio 5: Unicode from glyph name
            if (AdobeGlyphList.TryGetUnicode(ch.GlyphName, out var aglUnicode))
            {
                return aglUnicode;
            }

            return CharInfo.NotDef;
        }

        private static bool IsSingleChar(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            if (s.Length == 1)
            {
                return true;
            }

            Utf16Encoding.DecodeCodePoint(s, 0, out var codePointLength);

            if (codePointLength == s.Length)
            {
                return true;
            }

            return false;
        }

        private void PopulateForEmbeddedFont(IEnumerable<CharInfo> chars, UnicodeMap toUnicode, SingleByteEncoding? explicitEncoding)
        {
            const char StartPrivateUseArea = '\uE000';
            const char EndPrivateUseArea = '\uF8FF';

            var usedUnicodeToGidMappings = new Dictionary<string, uint>();
            var usedUnicode = new HashSet<string>();
            var nextReplacementChar = StartPrivateUseArea;

            foreach (var ch in chars)
            {
                if (this.chars.ContainsKey(ch.CharCode))
                {
                    continue;
                }

                ch.Unicode = ResolveUnicode(ch, toUnicode, explicitEncoding, preferSingleChar: true);
                ch.Unicode = Ligatures.Lookup(ch.Unicode);

                if (ch.GlyphIndex == null)
                {
                    this.chars[ch.CharCode] = ch;
                }
                else if (
                    ch.Unicode != CharInfo.NotDef &&
                    IsSingleChar(ch.Unicode) &&
                    (
                        ch.Unicode.Length > 1 ||       // Surrogate pair cannot be a control character
                        !char.IsControl(ch.Unicode[0]) // Don't allow mapping to control characters
                    ) &&
                    (
                        !usedUnicodeToGidMappings.TryGetValue(ch.Unicode, out var mappedGid) ||
                        mappedGid == ch.GlyphIndex.Value
                    ))
                {
                    this.chars[ch.CharCode] = ch;
                    usedUnicodeToGidMappings[ch.Unicode] = ch.GlyphIndex.Value;
                }
                else
                {
                    // Remap
                    var replacement = new string(nextReplacementChar, 1);

                    while (!usedUnicode.Add(replacement))
                    {
                        if (nextReplacementChar < EndPrivateUseArea)
                        {
                            nextReplacementChar++;
                            replacement = new string(nextReplacementChar, 1);
                        }
                        else
                        {
                            replacement = null;
                            break;
                        }
                    }

                    if (replacement != null)
                    {
                        ch.Unicode = replacement;
                        nextReplacementChar++;

                        this.chars[ch.CharCode] = ch;
                        usedUnicodeToGidMappings[ch.Unicode] = ch.GlyphIndex.Value;
                    }
                }
            }
        }

        private void PopulateForTextExtract(IEnumerable<CharInfo> chars, UnicodeMap toUnicode, SingleByteEncoding? explicitEncoding)
        {
            foreach (var ch in chars)
            {
                ch.Unicode = ResolveUnicode(ch, toUnicode, explicitEncoding, preferSingleChar: false);
                this.chars.TryAdd(ch.CharCode, ch);
            }
        }

        public bool TryPopulate(Func<IEnumerable<CharInfo>> charEnumerator, UnicodeMap toUnicode, SingleByteEncoding? explicitEncoding, bool optimizeForEmbeddedFont)
        {
            if (charsPopulated)
            {
                return false;
            }

            if (!Monitor.TryEnter(chars))
            {
                return false;
            }

            try
            {
                if (charsPopulated)
                {
                    return false;
                }

                var chars = charEnumerator();

                if (optimizeForEmbeddedFont)
                {
                    PopulateForEmbeddedFont(chars, toUnicode, explicitEncoding);
                }
                else
                {
                    PopulateForTextExtract(chars, toUnicode, explicitEncoding);
                }

                charsPopulated = true;
                return true;
            }
            finally
            {
                if (!charsPopulated)
                {
                    // Population failed
                    chars.Clear();
                }

                Monitor.Exit(chars);
            }
        }

        public IEnumerator<CharInfo> GetEnumerator()
        {
            if (charsPopulated)
            {
                return chars.Values.GetEnumerator();
            }
            else
            {
                return Enumerable.Empty<CharInfo>().GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

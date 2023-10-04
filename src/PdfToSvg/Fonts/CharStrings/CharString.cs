﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharString
    {
        private readonly CharStringInfo info;

        public CharString()
        {
            var endchar = CharStringLexeme.Operator(14);

            info = new CharStringInfo();
            info.Content.Add(endchar);
        }

        public CharString(CharStringInfo info)
        {
            this.info = info;
            this.Width = info.Width;
        }

        public IList<CharStringLexeme> Hints => info.Hints;
        public IList<CharStringLexeme> Content => info.Content;

        public CharStringSeacInfo? Seac => info.Seac;

        public double? Width { get; set; }

        public double MinX => info.Path.MinX;
        public double MaxX => info.Path.MaxX;
        public double MinY => info.Path.MinY;
        public double MaxY => info.Path.MaxY;

        public double LastX => info.Path.LastX;
        public double LastY => info.Path.LastY;

        public double HintCount => info.HintCount;
    }
}

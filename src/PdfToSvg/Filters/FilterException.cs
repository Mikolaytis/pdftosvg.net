﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal class FilterException : PdfException
    {
        public FilterException(string filter, byte unexpectedByte) :
            base(string.Format("Unexpected byte 0x{0:x2} in {1} stream.", unexpectedByte, filter))
        {
        }

        public FilterException(string message) : base(message)
        {
        }
    }
}

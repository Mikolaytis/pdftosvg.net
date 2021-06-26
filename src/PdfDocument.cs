﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg
{
    /// <summary>
    /// Contains information about a loaded PDF file.
    /// </summary>
    /// <example>
    /// <para>
    ///     The following example opens a PDF file and saves each page as an SVG file.
    /// </para>
    /// <code lang="cs" title="Convert PDF to SVG">
    /// using (var doc = PdfDocument.Open("input.pdf"))
    /// {
    ///     var pageIndex = 0;
    /// 
    ///     foreach (var page in doc.Pages)
    ///     {
    ///         page.SaveAsSvg($"output-{pageIndex++}.svg");
    ///     }
    /// }
    /// </code>
    /// </example>
    public sealed class PdfDocument : IDisposable
    {
        private readonly PdfDictionary root;
        private readonly PdfDictionary info;
        private InputFile? file;

        internal PdfDocument(InputFile file, PdfDictionary? trailer)
        {
            this.info = trailer.GetDictionaryOrEmpty(Names.Info);
            this.root = trailer.GetDictionaryOrEmpty(Names.Root);
            this.file = file;

            this.Pages = new PdfPageCollection(PdfReader
                .GetFlattenedPages(root)
                .Select(dict => new PdfPage(this, dict))
                .ToList());
        }

        /// <summary>
        /// Loads a PDF file from a stream.
        /// </summary>
        /// <param name="stream">Stream to read the PDF content from. The stream must be seekable.</param>
        /// <param name="leaveOpen">If <c>true</c>, the stream is left open when the returned <see cref="PdfDocument"/> is disposed.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <returns><see cref="PdfDocument"/> for the specified stream.</returns>
        /// <exception cref="ArgumentException"><paramref name="stream"/> is not readable and/or seekable.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="EncryptedPdfException">The PDF file is encrypted. Encrypted PDFs are currently not supported.</exception>
        public static PdfDocument Open(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead || !stream.CanSeek) throw new ArgumentException("The stream must be readable and seekable.", nameof(stream));

            return PdfReader.Read(new InputFile(stream, leaveOpen), cancellationToken);
        }

        /// <summary>
        /// Loads a PDF file from a file path.
        /// </summary>
        /// <param name="path">Path to PDF file to open.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <returns><see cref="PdfDocument"/> for the specified file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
        /// <exception cref="IOException">An IO error occured while reading the file.</exception>
        /// <exception cref="FileNotFoundException">No file was found at <paramref name="path"/>.</exception>
        /// <exception cref="EncryptedPdfException">The PDF file is encrypted. Encrypted PDFs are currently not supported.</exception>
        public static PdfDocument Open(string path, CancellationToken cancellationToken = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path.Length == 0) throw new ArgumentException("The path must not be empty.", nameof(path));

            var file = new InputFile(path, useAsync: false);

            try
            {
                return PdfReader.Read(file, cancellationToken);
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Loads a PDF file from a stream asynchronously.
        /// </summary>
        /// <inheritdoc cref="Open(Stream, bool, CancellationToken)"/>
        public static async Task<PdfDocument> OpenAsync(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead || !stream.CanSeek) throw new ArgumentException("The stream must be readable and seekable.", nameof(stream));

            return await PdfReader.ReadAsync(new InputFile(stream, leaveOpen), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads a PDF file from a file path asynchronously.
        /// </summary>
        /// <inheritdoc cref="Open(string, CancellationToken)"/>
        public static async Task<PdfDocument> OpenAsync(string path, CancellationToken cancellationToken = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path.Length == 0) throw new ArgumentException("The path must not be empty.", nameof(path));

            var file = new InputFile(path, useAsync: true);

            try
            {
                return await PdfReader.ReadAsync(file, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Gets the title of the document.
        /// </summary>
        public string? Title => info.GetValueOrDefault<PdfString?>(Names.Title)?.ToString();

        /// <summary>
        /// Gets the author of the document.
        /// </summary>
        public string? Author => info.GetValueOrDefault<PdfString?>(Names.Author)?.ToString();

        /// <summary>
        /// Gets the subject of the document.
        /// </summary>
        public string? Subject => info.GetValueOrDefault<PdfString?>(Names.Subject)?.ToString();

        /// <summary>
        /// Gets keywords specified for this document.
        /// </summary>
        public string? Keywords => info.GetValueOrDefault<PdfString?>(Names.Keywords)?.ToString();

        /// <summary>
        /// Gets the software used for creating the document.
        /// </summary>
        public string? Creator => info.GetValueOrDefault<PdfString?>(Names.Creator)?.ToString();

        /// <summary>
        /// Gets the software used for creating the PDF file.
        /// </summary>
        public string? Producer => info.GetValueOrDefault<PdfString?>(Names.Producer)?.ToString();

        /// <summary>
        /// Gets the date when the document was created.
        /// </summary>
        public DateTimeOffset? CreationDate => info.GetValueOrDefault<DateTimeOffset?>(Names.CreationDate);

        /// <summary>
        /// Gets the date when the document was modified.
        /// </summary>
        public DateTimeOffset? ModDate => info.GetValueOrDefault<DateTimeOffset?>(Names.ModDate);

        /// <summary>
        /// Gets a collection of the pages in this PDF document.
        /// </summary>
        public PdfPageCollection Pages { get; }

        /// <summary>
        /// Closes the PDF file.
        /// </summary>
        public void Dispose()
        {
            file?.Dispose();
            file = null;
        }
    }
}

<div align="center">
    <img src="src/logo.svg" alt="PdfToSvg.NET Logo" width="200" height="200">
</div>

# PdfToSvg.NET
Fully managed library for converting PDF files to SVG. Potential usage is embedding PDFs on your site without the need of loading a PDF reader.

## State
The library is still under active development and should not yet be used in production code.

## Install
Install the pdftosvg.net NuGet package.

```
PM> Install-Package pdftosvg.net
```

## Usage

Open a PDF document by calling `PdfDocument.Open`. Use `ToSvg()` on each page to convert it to SVG.

```csharp
using (var doc = PdfDocument.Open("input.pdf"))
{
    var pageIndex = 0;

    foreach (var page in doc.Pages)
    {
        File.WriteAllText($"output-{pageIndex++}.svg", page.ToSvg());
    }
}
```

Note that is you parse the XML returned from pdftosvg.net, you need to preserve space and not add indentation.
Otherwise text will not be rendered correctly in the modified markup.

## Limitations
Not all PDF features are supported. Here is a summary of non-supported features:

* Embedded fonts from the PDF are not exported to the SVG. There is however an API for specifying subsitute fonts during the conversion.
* Opening encrypted PDF files.
* Blending modes.
* PDF forms.
* JavaScript embedded in the PDF.

Non-supported image formats:
* CCITT Fax
* JPEG 2000
* CMYK JPEG
* JBIG2

Non-supported color spaces:
* CalGray
* CalRGB
* Lab
* DeviceN
* Pattern
* ICCBased

/*
$Id: 7b48389d594bd76e7ed5ca63d10441a209b71f4c $

This file is part of the iText (R) project.
Copyright (c) 1998-2016 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using com.itextpdf.kernel.pdf;
using com.itextpdf.kernel.pdf.canvas.parser.listener;

namespace com.itextpdf.kernel.pdf.canvas.parser
{
	public sealed class PdfTextExtractor
	{
		private PdfTextExtractor()
		{
		}

		/// <summary>Extract text from a specified page using an extraction strategy.</summary>
		/// <param name="page">the page for the text to be extracted from</param>
		/// <param name="strategy">the strategy to use for extracting text</param>
		/// <returns>the extracted text</returns>
		public static String GetTextFromPage(PdfPage page, ITextExtractionStrategy strategy
			)
		{
			PdfCanvasProcessor parser = new PdfCanvasProcessor(strategy);
			parser.ProcessPageContent(page);
			return strategy.GetResultantText();
		}

		/// <summary>Extract text from a specified page using the default strategy.</summary>
		/// <remarks>
		/// Extract text from a specified page using the default strategy.
		/// Node: the default strategy is subject to change. If using a specific strategy
		/// is important, please use
		/// <see cref="GetTextFromPage(com.itextpdf.kernel.pdf.PdfPage, com.itextpdf.kernel.pdf.canvas.parser.listener.ITextExtractionStrategy)
		/// 	"/>
		/// .
		/// </remarks>
		/// <param name="page">the page for the text to be extracted from</param>
		/// <returns>the extracted text</returns>
		/// <exception cref="System.IO.IOException"/>
		public static String GetTextFromPage(PdfPage page)
		{
			return GetTextFromPage(page, new LocationTextExtractionStrategy());
		}
	}
}

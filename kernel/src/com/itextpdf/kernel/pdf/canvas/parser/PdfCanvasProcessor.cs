/*
$Id: 19ae9383cfded4951de446315fcea360620bc406 $

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
using System.Collections.Generic;
using com.itextpdf.io.source;
using com.itextpdf.kernel;
using com.itextpdf.kernel.color;
using com.itextpdf.kernel.font;
using com.itextpdf.kernel.geom;
using com.itextpdf.kernel.pdf;
using com.itextpdf.kernel.pdf.canvas;
using com.itextpdf.kernel.pdf.canvas.parser.data;
using com.itextpdf.kernel.pdf.canvas.parser.listener;
using com.itextpdf.kernel.pdf.canvas.parser.util;
using com.itextpdf.kernel.pdf.colorspace;

namespace com.itextpdf.kernel.pdf.canvas.parser
{
	/// <summary>Processor for a PDF content stream.</summary>
	public class PdfCanvasProcessor
	{
		public const String DEFAULT_OPERATOR = "DefaultOperator";

		/// <summary>Listener that will be notified of render events</summary>
		protected internal readonly IEventListener eventListener;

		/// <summary>
		/// Cache supported events in case the user's
		/// <see cref="com.itextpdf.kernel.pdf.canvas.parser.listener.IEventListener.GetSupportedEvents()
		/// 	"/>
		/// method is not very efficient
		/// </summary>
		protected internal readonly ICollection<EventType> supportedEvents;

		protected internal com.itextpdf.kernel.geom.Path currentPath = new com.itextpdf.kernel.geom.Path
			();

		/// <summary>
		/// Indicates whether the current clipping path should be modified by
		/// intersecting it with the current path.
		/// </summary>
		protected internal bool isClip;

		/// <summary>
		/// Specifies the filling rule which should be applied while calculating
		/// new clipping path.
		/// </summary>
		protected internal int clippingRule;

		/// <summary>A map with all supported operators (PDF syntax).</summary>
		private IDictionary<String, IContentOperator> operators;

		/// <summary>Resources for the content stream.</summary>
		/// <remarks>
		/// Resources for the content stream.
		/// Current resources are always at the top of the stack.
		/// Stack is needed in case if some "inner" content stream with it's own resources
		/// is encountered (like Form XObject).
		/// </remarks>
		private Stack<PdfResources> resourcesStack;

		/// <summary>Stack keeping track of the graphics state.</summary>
		private readonly Stack<ParserGraphicsState> gsStack = new Stack<ParserGraphicsState
			>();

		private Matrix textMatrix;

		private Matrix textLineMatrix;

		/// <summary>A map with all supported XObject handlers</summary>
		private IDictionary<PdfName, IXObjectDoHandler> xobjectDoHandlers;

		/// <summary>The font cache</summary>
		private IDictionary<int, PdfFont> cachedFonts = new Dictionary<int, PdfFont>();

		/// <summary>A stack containing marked content info.</summary>
		private Stack<CanvasTag> markedContentStack = new Stack<CanvasTag>();

		/// <summary>
		/// Creates a new PDF Content Stream Processor that will send it's output to the
		/// designated render listener.
		/// </summary>
		/// <param name="eventListener">
		/// the
		/// <see cref="com.itextpdf.kernel.pdf.canvas.parser.listener.IEventListener"/>
		/// that will receive rendering notifications
		/// </param>
		public PdfCanvasProcessor(IEventListener eventListener)
		{
			this.eventListener = eventListener;
			this.supportedEvents = eventListener.GetSupportedEvents();
			operators = new Dictionary<String, IContentOperator>();
			PopulateOperators();
			xobjectDoHandlers = new Dictionary<PdfName, IXObjectDoHandler>();
			PopulateXObjectDoHandlers();
			Reset();
		}

		/// <summary>Registers a Do handler that will be called when Do for the provided XObject subtype is encountered during content processing.
		/// 	</summary>
		/// <remarks>
		/// Registers a Do handler that will be called when Do for the provided XObject subtype is encountered during content processing.
		/// <br />
		/// If you register a handler, it is a very good idea to pass the call on to the existing registered handler (returned by this call), otherwise you
		/// may inadvertently change the internal behavior of the processor.
		/// </remarks>
		/// <param name="xobjectSubType">the XObject subtype this handler will process, or PdfName.DEFAULT for a catch-all handler
		/// 	</param>
		/// <param name="handler">the handler that will receive notification when the Do operator for the specified subtype is encountered
		/// 	</param>
		/// <returns>the existing registered handler, if any</returns>
		public virtual IXObjectDoHandler RegisterXObjectDoHandler(PdfName xobjectSubType, 
			IXObjectDoHandler handler)
		{
			return xobjectDoHandlers[xobjectSubType] = handler;
		}

		/// <summary>Registers a content operator that will be called when the specified operator string is encountered during content processing.
		/// 	</summary>
		/// <remarks>
		/// Registers a content operator that will be called when the specified operator string is encountered during content processing.
		/// <br />
		/// If you register an operator, it is a very good idea to pass the call on to the existing registered operator (returned by this call), otherwise you
		/// may inadvertently change the internal behavior of the processor.
		/// </remarks>
		/// <param name="operatorString">the operator id, or DEFAULT_OPERATOR for a catch-all operator
		/// 	</param>
		/// <param name="operator">the operator that will receive notification when the operator is encountered
		/// 	</param>
		/// <returns>the existing registered operator, if any</returns>
		public virtual IContentOperator RegisterContentOperator(String operatorString, IContentOperator
			 @operator)
		{
			return operators[operatorString] = @operator;
		}

		/// <returns>
		/// 
		/// <see cref="System.Collections.ICollection{E}"/>
		/// containing all the registered operators strings
		/// </returns>
		public virtual ICollection<String> GetRegisteredOperatorStrings()
		{
			return new List<String>(operators.Keys);
		}

		/// <summary>Resets the graphics state stack, matrices and resources.</summary>
		public virtual void Reset()
		{
			gsStack.Clear();
			gsStack.Push(new ParserGraphicsState());
			textMatrix = null;
			textLineMatrix = null;
			resourcesStack = new Stack<PdfResources>();
			isClip = false;
			currentPath = new com.itextpdf.kernel.geom.Path();
		}

		/// <returns>the current graphics state</returns>
		public virtual ParserGraphicsState GetGraphicsState()
		{
			return gsStack.Peek();
		}

		/// <summary>Processes PDF syntax.</summary>
		/// <remarks>
		/// Processes PDF syntax.
		/// <b>Note:</b> If you re-use a given
		/// <see cref="PdfCanvasProcessor"/>
		/// , you must call
		/// <see cref="Reset()"/>
		/// </remarks>
		/// <param name="contentBytes">the bytes of a content stream</param>
		/// <param name="resources">the resources of the content stream. Must not be null.</param>
		public virtual void ProcessContent(byte[] contentBytes, PdfResources resources)
		{
			if (resources == null)
			{
				throw new PdfException(PdfException.ResourcesCannotBeNull);
			}
			this.resourcesStack.Push(resources);
			PdfTokenizer tokeniser = new PdfTokenizer(new RandomAccessFileOrArray(new RandomAccessSourceFactory
				().CreateSource(contentBytes)));
			PdfCanvasParser ps = new PdfCanvasParser(tokeniser, resources);
			IList<PdfObject> operands = new List<PdfObject>();
			try
			{
				while (ps.Parse(operands).Count > 0)
				{
					PdfLiteral @operator = (PdfLiteral)operands[operands.Count - 1];
					InvokeOperator(@operator, operands);
				}
			}
			catch (System.IO.IOException e)
			{
				throw new PdfException(PdfException.CannotParseContentStream, e);
			}
			this.resourcesStack.Pop();
		}

		/// <summary>Processes PDF syntax.</summary>
		/// <remarks>
		/// Processes PDF syntax.
		/// <br/>
		/// <strong>Note:</strong> If you re-use a given
		/// <see cref="PdfCanvasProcessor"/>
		/// , you must call
		/// <see cref="Reset()"/>
		/// </remarks>
		/// <param name="page">the page to process</param>
		public virtual void ProcessPageContent(PdfPage page)
		{
			InitClippingPath(page);
			ParserGraphicsState gs = GetGraphicsState();
			EventOccurred(new ClippingPathInfo(gs.GetClippingPath(), gs.GetCtm()), EventType.
				CLIP_PATH_CHANGED);
			ProcessContent(page.GetContentBytes(), page.GetResources());
		}

		/// <summary>
		/// Accessor method for the
		/// <see cref="com.itextpdf.kernel.pdf.canvas.parser.listener.IEventListener"/>
		/// object maintained in this class.
		/// Necessary for implementing custom ContentOperator implementations.
		/// </summary>
		/// <returns>the renderListener</returns>
		public virtual IEventListener GetEventListener()
		{
			return eventListener;
		}

		/// <summary>Loads all the supported graphics and text state operators in a map.</summary>
		protected internal virtual void PopulateOperators()
		{
			RegisterContentOperator(DEFAULT_OPERATOR, new PdfCanvasProcessor.IgnoreOperator()
				);
			RegisterContentOperator("q", new PdfCanvasProcessor.PushGraphicsStateOperator());
			RegisterContentOperator("Q", new PdfCanvasProcessor.PopGraphicsStateOperator());
			RegisterContentOperator("cm", new PdfCanvasProcessor.ModifyCurrentTransformationMatrixOperator
				());
			RegisterContentOperator("Do", new PdfCanvasProcessor.DoOperator());
			RegisterContentOperator("BMC", new PdfCanvasProcessor.BeginMarkedContentOperator(
				));
			RegisterContentOperator("BDC", new PdfCanvasProcessor.BeginMarkedContentDictionaryOperator
				());
			RegisterContentOperator("EMC", new PdfCanvasProcessor.EndMarkedContentOperator());
			if (supportedEvents == null || supportedEvents.Contains(EventType.RENDER_TEXT) ||
				 supportedEvents.Contains(EventType.RENDER_PATH) || supportedEvents.Contains(EventType
				.CLIP_PATH_CHANGED))
			{
				RegisterContentOperator("g", new PdfCanvasProcessor.SetGrayFillOperator());
				RegisterContentOperator("G", new PdfCanvasProcessor.SetGrayStrokeOperator());
				RegisterContentOperator("rg", new PdfCanvasProcessor.SetRGBFillOperator());
				RegisterContentOperator("RG", new PdfCanvasProcessor.SetRGBStrokeOperator());
				RegisterContentOperator("k", new PdfCanvasProcessor.SetCMYKFillOperator());
				RegisterContentOperator("K", new PdfCanvasProcessor.SetCMYKStrokeOperator());
				RegisterContentOperator("cs", new PdfCanvasProcessor.SetColorSpaceFillOperator());
				RegisterContentOperator("CS", new PdfCanvasProcessor.SetColorSpaceStrokeOperator(
					));
				RegisterContentOperator("sc", new PdfCanvasProcessor.SetColorFillOperator());
				RegisterContentOperator("SC", new PdfCanvasProcessor.SetColorStrokeOperator());
				RegisterContentOperator("scn", new PdfCanvasProcessor.SetColorFillOperator());
				RegisterContentOperator("SCN", new PdfCanvasProcessor.SetColorStrokeOperator());
				RegisterContentOperator("gs", new PdfCanvasProcessor.ProcessGraphicsStateResourceOperator
					());
			}
			if (supportedEvents == null || supportedEvents.Contains(EventType.RENDER_IMAGE))
			{
				RegisterContentOperator("EI", new PdfCanvasProcessor.EndImageOperator());
			}
			if (supportedEvents == null || supportedEvents.Contains(EventType.RENDER_TEXT) ||
				 supportedEvents.Contains(EventType.BEGIN_TEXT) || supportedEvents.Contains(EventType
				.END_TEXT))
			{
				RegisterContentOperator("BT", new PdfCanvasProcessor.BeginTextOperator());
				RegisterContentOperator("ET", new PdfCanvasProcessor.EndTextOperator());
			}
			if (supportedEvents == null || supportedEvents.Contains(EventType.RENDER_TEXT))
			{
				PdfCanvasProcessor.SetTextCharacterSpacingOperator tcOperator = new PdfCanvasProcessor.SetTextCharacterSpacingOperator
					();
				RegisterContentOperator("Tc", tcOperator);
				PdfCanvasProcessor.SetTextWordSpacingOperator twOperator = new PdfCanvasProcessor.SetTextWordSpacingOperator
					();
				RegisterContentOperator("Tw", twOperator);
				RegisterContentOperator("Tz", new PdfCanvasProcessor.SetTextHorizontalScalingOperator
					());
				PdfCanvasProcessor.SetTextLeadingOperator tlOperator = new PdfCanvasProcessor.SetTextLeadingOperator
					();
				RegisterContentOperator("TL", tlOperator);
				RegisterContentOperator("Tf", new PdfCanvasProcessor.SetTextFontOperator());
				RegisterContentOperator("Tr", new PdfCanvasProcessor.SetTextRenderModeOperator());
				RegisterContentOperator("Ts", new PdfCanvasProcessor.SetTextRiseOperator());
				PdfCanvasProcessor.TextMoveStartNextLineOperator tdOperator = new PdfCanvasProcessor.TextMoveStartNextLineOperator
					();
				RegisterContentOperator("Td", tdOperator);
				RegisterContentOperator("TD", new PdfCanvasProcessor.TextMoveStartNextLineWithLeadingOperator
					(tdOperator, tlOperator));
				RegisterContentOperator("Tm", new PdfCanvasProcessor.TextSetTextMatrixOperator());
				PdfCanvasProcessor.TextMoveNextLineOperator tstarOperator = new PdfCanvasProcessor.TextMoveNextLineOperator
					(tdOperator);
				RegisterContentOperator("T*", tstarOperator);
				PdfCanvasProcessor.ShowTextOperator tjOperator = new PdfCanvasProcessor.ShowTextOperator
					();
				RegisterContentOperator("Tj", tjOperator);
				PdfCanvasProcessor.MoveNextLineAndShowTextOperator tickOperator = new PdfCanvasProcessor.MoveNextLineAndShowTextOperator
					(tstarOperator, tjOperator);
				RegisterContentOperator("'", tickOperator);
				RegisterContentOperator("\"", new PdfCanvasProcessor.MoveNextLineAndShowTextWithSpacingOperator
					(twOperator, tcOperator, tickOperator));
				RegisterContentOperator("TJ", new PdfCanvasProcessor.ShowTextArrayOperator());
			}
			if (supportedEvents == null || supportedEvents.Contains(EventType.CLIP_PATH_CHANGED
				) || supportedEvents.Contains(EventType.RENDER_PATH))
			{
				RegisterContentOperator("w", new PdfCanvasProcessor.SetLineWidthOperator());
				RegisterContentOperator("J", new PdfCanvasProcessor.SetLineCapOperator(this));
				RegisterContentOperator("j", new PdfCanvasProcessor.SetLineJoinOperator(this));
				RegisterContentOperator("M", new PdfCanvasProcessor.SetMiterLimitOperator(this));
				RegisterContentOperator("d", new PdfCanvasProcessor.SetLineDashPatternOperator(this
					));
				int fillStroke = PathRenderInfo.FILL | PathRenderInfo.STROKE;
				RegisterContentOperator("m", new PdfCanvasProcessor.MoveToOperator());
				RegisterContentOperator("l", new PdfCanvasProcessor.LineToOperator());
				RegisterContentOperator("c", new PdfCanvasProcessor.CurveOperator());
				RegisterContentOperator("v", new PdfCanvasProcessor.CurveFirstPointDuplicatedOperator
					());
				RegisterContentOperator("y", new PdfCanvasProcessor.CurveFourhPointDuplicatedOperator
					());
				RegisterContentOperator("h", new PdfCanvasProcessor.CloseSubpathOperator());
				RegisterContentOperator("re", new PdfCanvasProcessor.RectangleOperator());
				RegisterContentOperator("S", new PdfCanvasProcessor.PaintPathOperator(PathRenderInfo
					.STROKE, -1, false));
				RegisterContentOperator("s", new PdfCanvasProcessor.PaintPathOperator(PathRenderInfo
					.STROKE, -1, true));
				RegisterContentOperator("f", new PdfCanvasProcessor.PaintPathOperator(PathRenderInfo
					.FILL, PdfCanvasConstants.FillingRule.NONZERO_WINDING, false));
				RegisterContentOperator("F", new PdfCanvasProcessor.PaintPathOperator(PathRenderInfo
					.FILL, PdfCanvasConstants.FillingRule.NONZERO_WINDING, false));
				RegisterContentOperator("f*", new PdfCanvasProcessor.PaintPathOperator(PathRenderInfo
					.FILL, PdfCanvasConstants.FillingRule.EVEN_ODD, false));
				RegisterContentOperator("B", new PdfCanvasProcessor.PaintPathOperator(fillStroke, 
					PdfCanvasConstants.FillingRule.NONZERO_WINDING, false));
				RegisterContentOperator("B*", new PdfCanvasProcessor.PaintPathOperator(fillStroke
					, PdfCanvasConstants.FillingRule.EVEN_ODD, false));
				RegisterContentOperator("b", new PdfCanvasProcessor.PaintPathOperator(fillStroke, 
					PdfCanvasConstants.FillingRule.NONZERO_WINDING, true));
				RegisterContentOperator("b*", new PdfCanvasProcessor.PaintPathOperator(fillStroke
					, PdfCanvasConstants.FillingRule.EVEN_ODD, true));
				RegisterContentOperator("n", new PdfCanvasProcessor.PaintPathOperator(PathRenderInfo
					.NO_OP, -1, false));
				RegisterContentOperator("W", new PdfCanvasProcessor.ClipPathOperator(PdfCanvasConstants.FillingRule
					.NONZERO_WINDING));
				RegisterContentOperator("W*", new PdfCanvasProcessor.ClipPathOperator(PdfCanvasConstants.FillingRule
					.EVEN_ODD));
			}
		}

		/// <summary>Displays the current path.</summary>
		/// <param name="operation">
		/// One of the possible combinations of
		/// <see cref="com.itextpdf.kernel.pdf.canvas.parser.data.PathRenderInfo.STROKE"/>
		/// and
		/// <see cref="com.itextpdf.kernel.pdf.canvas.parser.data.PathRenderInfo.FILL"/>
		/// values or
		/// <see cref="com.itextpdf.kernel.pdf.canvas.parser.data.PathRenderInfo.NO_OP"/>
		/// </param>
		/// <param name="rule">
		/// Either
		/// <see cref="com.itextpdf.kernel.pdf.canvas.PdfCanvasConstants.FillingRule.NONZERO_WINDING
		/// 	"/>
		/// or
		/// <see cref="com.itextpdf.kernel.pdf.canvas.PdfCanvasConstants.FillingRule.EVEN_ODD
		/// 	"/>
		/// In case it isn't applicable pass any <CODE>byte</CODE> value.
		/// </param>
		protected internal virtual void PaintPath(int operation, int rule)
		{
			PathRenderInfo renderInfo = new PathRenderInfo(currentPath, operation, rule, isClip
				, clippingRule, GetGraphicsState());
			EventOccurred(renderInfo, EventType.RENDER_PATH);
			if (isClip)
			{
				isClip = false;
				ParserGraphicsState gs = GetGraphicsState();
				gs.Clip(currentPath, clippingRule);
				EventOccurred(new ClippingPathInfo(gs.GetClippingPath(), gs.GetCtm()), EventType.
					CLIP_PATH_CHANGED);
			}
			currentPath = new com.itextpdf.kernel.geom.Path();
		}

		/// <summary>Invokes an operator.</summary>
		/// <param name="operator">the PDF Syntax of the operator</param>
		/// <param name="operands">a list with operands</param>
		protected internal virtual void InvokeOperator(PdfLiteral @operator, IList<PdfObject
			> operands)
		{
			IContentOperator op = operators[@operator.ToString()];
			if (op == null)
			{
				op = operators[DEFAULT_OPERATOR];
			}
			op.Invoke(this, @operator, operands);
		}

		protected internal virtual PdfStream GetXObjectStream(PdfName xobjectName)
		{
			PdfDictionary xobjects = GetResources().GetResource(PdfName.XObject);
			return xobjects.GetAsStream(xobjectName);
		}

		protected internal virtual PdfResources GetResources()
		{
			return resourcesStack.Peek();
		}

		protected internal virtual void PopulateXObjectDoHandlers()
		{
			RegisterXObjectDoHandler(PdfName.Default, new PdfCanvasProcessor.IgnoreXObjectDoHandler
				());
			RegisterXObjectDoHandler(PdfName.Form, new PdfCanvasProcessor.FormXObjectDoHandler
				());
			if (supportedEvents == null || supportedEvents.Contains(EventType.RENDER_IMAGE))
			{
				RegisterXObjectDoHandler(PdfName.Image, new PdfCanvasProcessor.ImageXObjectDoHandler
					());
			}
		}

		/// <summary>Gets the font pointed to by the indirect reference.</summary>
		/// <remarks>Gets the font pointed to by the indirect reference. The font may have been cached.
		/// 	</remarks>
		/// <param name="fontDict"/>
		/// <returns>the font</returns>
		protected internal virtual PdfFont GetFont(PdfDictionary fontDict)
		{
			int n = fontDict.GetIndirectReference().GetObjNumber();
			PdfFont font = cachedFonts[n];
			if (font == null)
			{
				font = PdfFontFactory.CreateFont(fontDict);
				cachedFonts[n] = font;
			}
			return font;
		}

		/// <summary>Add to the marked content stack</summary>
		/// <param name="tag">the tag of the marked content</param>
		/// <param name="dict">the PdfDictionary associated with the marked content</param>
		protected internal virtual void BeginMarkedContent(PdfName tag, PdfDictionary dict
			)
		{
			markedContentStack.Push(new CanvasTag(tag).SetProperties(dict));
		}

		/// <summary>Remove the latest marked content from the stack.</summary>
		/// <remarks>Remove the latest marked content from the stack.  Keeps track of the BMC, BDC and EMC operators.
		/// 	</remarks>
		protected internal virtual void EndMarkedContent()
		{
			markedContentStack.Pop();
		}

		/// <summary>Used to trigger beginTextBlock on the renderListener</summary>
		private void BeginText()
		{
			EventOccurred(null, EventType.BEGIN_TEXT);
		}

		/// <summary>Used to trigger endTextBlock on the renderListener</summary>
		private void EndText()
		{
			EventOccurred(null, EventType.END_TEXT);
		}

		/// <summary>This is a proxy to pass only those events to the event listener which are supported by it.
		/// 	</summary>
		/// <param name="data">event data</param>
		/// <param name="type">event type</param>
		private void EventOccurred(IEventData data, EventType type)
		{
			if (supportedEvents == null || supportedEvents.Contains(type))
			{
				eventListener.EventOccurred(data, type);
			}
		}

		/// <summary>Displays text.</summary>
		/// <param name="string">the text to display</param>
		private void DisplayPdfString(PdfString @string)
		{
			TextRenderInfo renderInfo = new TextRenderInfo(@string, GetGraphicsState(), textMatrix
				, markedContentStack);
			EventOccurred(renderInfo, EventType.RENDER_TEXT);
			textMatrix = new Matrix(renderInfo.GetUnscaledWidth(), 0).Multiply(textMatrix);
		}

		/// <summary>Displays an XObject using the registered handler for this XObject's subtype
		/// 	</summary>
		/// <param name="xobjectName">the name of the XObject to retrieve from the resource dictionary
		/// 	</param>
		private void DisplayXObject(PdfName xobjectName)
		{
			PdfStream xobjectStream = GetXObjectStream(xobjectName);
			PdfName subType = xobjectStream.GetAsName(PdfName.Subtype);
			IXObjectDoHandler handler = xobjectDoHandlers[subType];
			if (handler == null)
			{
				handler = xobjectDoHandlers[PdfName.Default];
			}
			handler.HandleXObject(this, xobjectStream);
		}

		private void DisplayImage(PdfStream imageStream, bool isInline)
		{
			PdfDictionary colorSpaceDic = GetResources().GetResource(PdfName.ColorSpace);
			ImageRenderInfo renderInfo = new ImageRenderInfo(GetGraphicsState().GetCtm(), imageStream
				, colorSpaceDic, isInline);
			EventOccurred(renderInfo, EventType.RENDER_IMAGE);
		}

		/// <summary>Adjusts the text matrix for the specified adjustment value (see TJ operator in the PDF spec for information)
		/// 	</summary>
		/// <param name="tj">the text adjustment</param>
		private void ApplyTextAdjust(float tj)
		{
			float adjustBy = -tj / 1000f * GetGraphicsState().GetFontSize() * (GetGraphicsState
				().GetHorizontalScaling() / 100f);
			textMatrix = new Matrix(adjustBy, 0).Multiply(textMatrix);
		}

		private void InitClippingPath(PdfPage page)
		{
			com.itextpdf.kernel.geom.Path clippingPath = new com.itextpdf.kernel.geom.Path();
			clippingPath.Rectangle(page.GetCropBox());
			GetGraphicsState().SetClippingPath(clippingPath);
		}

		/// <summary>A content operator implementation (unregistered).</summary>
		private class IgnoreOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
			}
			// ignore the operator
		}

		/// <summary>A content operator implementation (TJ).</summary>
		private class ShowTextArrayOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfArray array = (PdfArray)operands[0];
				float tj = 0;
				foreach (PdfObject entryObj in array)
				{
					if (entryObj is PdfString)
					{
						processor.DisplayPdfString((PdfString)entryObj);
						tj = 0;
					}
					else
					{
						tj = ((PdfNumber)entryObj).FloatValue();
						processor.ApplyTextAdjust(tj);
					}
				}
			}
		}

		/// <summary>A content operator implementation (").</summary>
		private class MoveNextLineAndShowTextWithSpacingOperator : IContentOperator
		{
			private readonly PdfCanvasProcessor.SetTextWordSpacingOperator setTextWordSpacing;

			private readonly PdfCanvasProcessor.SetTextCharacterSpacingOperator setTextCharacterSpacing;

			private readonly PdfCanvasProcessor.MoveNextLineAndShowTextOperator moveNextLineAndShowText;

			public MoveNextLineAndShowTextWithSpacingOperator(PdfCanvasProcessor.SetTextWordSpacingOperator
				 setTextWordSpacing, PdfCanvasProcessor.SetTextCharacterSpacingOperator setTextCharacterSpacing
				, PdfCanvasProcessor.MoveNextLineAndShowTextOperator moveNextLineAndShowText)
			{
				this.setTextWordSpacing = setTextWordSpacing;
				this.setTextCharacterSpacing = setTextCharacterSpacing;
				this.moveNextLineAndShowText = moveNextLineAndShowText;
			}

			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfNumber aw = (PdfNumber)operands[0];
				PdfNumber ac = (PdfNumber)operands[1];
				PdfString @string = (PdfString)operands[2];
				IList<PdfObject> twOperands = new List<PdfObject>(1);
				twOperands.Insert(0, aw);
				setTextWordSpacing.Invoke(processor, null, twOperands);
				IList<PdfObject> tcOperands = new List<PdfObject>(1);
				tcOperands.Insert(0, ac);
				setTextCharacterSpacing.Invoke(processor, null, tcOperands);
				IList<PdfObject> tickOperands = new List<PdfObject>(1);
				tickOperands.Insert(0, @string);
				moveNextLineAndShowText.Invoke(processor, null, tickOperands);
			}
		}

		/// <summary>A content operator implementation (').</summary>
		private class MoveNextLineAndShowTextOperator : IContentOperator
		{
			private readonly PdfCanvasProcessor.TextMoveNextLineOperator textMoveNextLine;

			private readonly PdfCanvasProcessor.ShowTextOperator showText;

			public MoveNextLineAndShowTextOperator(PdfCanvasProcessor.TextMoveNextLineOperator
				 textMoveNextLine, PdfCanvasProcessor.ShowTextOperator showText)
			{
				this.textMoveNextLine = textMoveNextLine;
				this.showText = showText;
			}

			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				textMoveNextLine.Invoke(processor, null, new List<PdfObject>(0));
				showText.Invoke(processor, null, operands);
			}
		}

		/// <summary>A content operator implementation (Tj).</summary>
		private class ShowTextOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfString @string = (PdfString)operands[0];
				processor.DisplayPdfString(@string);
			}
		}

		/// <summary>A content operator implementation (T*).</summary>
		private class TextMoveNextLineOperator : IContentOperator
		{
			private readonly PdfCanvasProcessor.TextMoveStartNextLineOperator moveStartNextLine;

			public TextMoveNextLineOperator(PdfCanvasProcessor.TextMoveStartNextLineOperator 
				moveStartNextLine)
			{
				this.moveStartNextLine = moveStartNextLine;
			}

			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				IList<PdfObject> tdoperands = new List<PdfObject>(2);
				tdoperands.Insert(0, new PdfNumber(0));
				tdoperands.Insert(1, new PdfNumber(-processor.GetGraphicsState().GetLeading()));
				moveStartNextLine.Invoke(processor, null, tdoperands);
			}
		}

		/// <summary>A content operator implementation (Tm).</summary>
		private class TextSetTextMatrixOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				float a = ((PdfNumber)operands[0]).FloatValue();
				float b = ((PdfNumber)operands[1]).FloatValue();
				float c = ((PdfNumber)operands[2]).FloatValue();
				float d = ((PdfNumber)operands[3]).FloatValue();
				float e = ((PdfNumber)operands[4]).FloatValue();
				float f = ((PdfNumber)operands[5]).FloatValue();
				processor.textLineMatrix = new Matrix(a, b, c, d, e, f);
				processor.textMatrix = processor.textLineMatrix;
			}
		}

		/// <summary>A content operator implementation (TD).</summary>
		private class TextMoveStartNextLineWithLeadingOperator : IContentOperator
		{
			private readonly PdfCanvasProcessor.TextMoveStartNextLineOperator moveStartNextLine;

			private readonly PdfCanvasProcessor.SetTextLeadingOperator setTextLeading;

			public TextMoveStartNextLineWithLeadingOperator(PdfCanvasProcessor.TextMoveStartNextLineOperator
				 moveStartNextLine, PdfCanvasProcessor.SetTextLeadingOperator setTextLeading)
			{
				this.moveStartNextLine = moveStartNextLine;
				this.setTextLeading = setTextLeading;
			}

			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				float ty = ((PdfNumber)operands[1]).FloatValue();
				IList<PdfObject> tlOperands = new List<PdfObject>(1);
				tlOperands.Insert(0, new PdfNumber(-ty));
				setTextLeading.Invoke(processor, null, tlOperands);
				moveStartNextLine.Invoke(processor, null, operands);
			}
		}

		/// <summary>A content operator implementation (Td).</summary>
		private class TextMoveStartNextLineOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				float tx = ((PdfNumber)operands[0]).FloatValue();
				float ty = ((PdfNumber)operands[1]).FloatValue();
				Matrix translationMatrix = new Matrix(tx, ty);
				processor.textMatrix = translationMatrix.Multiply(processor.textLineMatrix);
				processor.textLineMatrix = processor.textMatrix;
			}
		}

		/// <summary>A content operator implementation (Tf).</summary>
		private class SetTextFontOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfName fontResourceName = (PdfName)operands[0];
				float size = ((PdfNumber)operands[1]).FloatValue();
				PdfDictionary fontsDictionary = processor.GetResources().GetResource(PdfName.Font
					);
				PdfDictionary fontDict = fontsDictionary.GetAsDictionary(fontResourceName);
				PdfFont font = null;
				font = processor.GetFont(fontDict);
				processor.GetGraphicsState().SetFont(font);
				processor.GetGraphicsState().SetFontSize(size);
			}
		}

		/// <summary>A content operator implementation (Tr).</summary>
		private class SetTextRenderModeOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfNumber render = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetTextRenderingMode(render.IntValue());
			}
		}

		/// <summary>A content operator implementation (Ts).</summary>
		private class SetTextRiseOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfNumber rise = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetTextRise(rise.FloatValue());
			}
		}

		/// <summary>A content operator implementation (TL).</summary>
		private class SetTextLeadingOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfNumber leading = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetLeading(leading.FloatValue());
			}
		}

		/// <summary>A content operator implementation (Tz).</summary>
		private class SetTextHorizontalScalingOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfNumber scale = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetHorizontalScaling(scale.FloatValue());
			}
		}

		/// <summary>A content operator implementation (Tc).</summary>
		private class SetTextCharacterSpacingOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfNumber charSpace = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetCharSpacing(charSpace.FloatValue());
			}
		}

		/// <summary>A content operator implementation (Tw).</summary>
		private class SetTextWordSpacingOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfNumber wordSpace = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetWordSpacing(wordSpace.FloatValue());
			}
		}

		/// <summary>A content operator implementation (gs).</summary>
		private class ProcessGraphicsStateResourceOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfName dictionaryName = (PdfName)operands[0];
				PdfDictionary extGState = processor.GetResources().GetResource(PdfName.ExtGState);
				if (extGState == null)
				{
					throw new PdfException(PdfException.ResourcesDoNotContainExtgstateEntryUnableToProcessOperator1
						).SetMessageParams(@operator);
				}
				PdfDictionary gsDic = extGState.GetAsDictionary(dictionaryName);
				if (gsDic == null)
				{
					throw new PdfException(PdfException._1IsAnUnknownGraphicsStateDictionary).SetMessageParams
						(dictionaryName);
				}
				// at this point, all we care about is the FONT entry in the GS dictionary TODO merge the whole gs dictionary
				PdfArray fontParameter = gsDic.GetAsArray(PdfName.Font);
				if (fontParameter != null)
				{
					PdfFont font = processor.GetFont(fontParameter.GetAsDictionary(0));
					float size = fontParameter.GetAsNumber(1).FloatValue();
					processor.GetGraphicsState().SetFont(font);
					processor.GetGraphicsState().SetFontSize(size);
				}
			}
		}

		/// <summary>A content operator implementation (q).</summary>
		private class PushGraphicsStateOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				ParserGraphicsState gs = processor.gsStack.Peek();
				ParserGraphicsState copy = new ParserGraphicsState(gs);
				processor.gsStack.Push(copy);
			}
		}

		/// <summary>A content operator implementation (cm).</summary>
		private class ModifyCurrentTransformationMatrixOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				float a = ((PdfNumber)operands[0]).FloatValue();
				float b = ((PdfNumber)operands[1]).FloatValue();
				float c = ((PdfNumber)operands[2]).FloatValue();
				float d = ((PdfNumber)operands[3]).FloatValue();
				float e = ((PdfNumber)operands[4]).FloatValue();
				float f = ((PdfNumber)operands[5]).FloatValue();
				Matrix matrix = new Matrix(a, b, c, d, e, f);
				processor.GetGraphicsState().UpdateCtm(matrix);
			}
		}

		/// <summary>Gets a color based on a list of operands and Color space.</summary>
		private static Color GetColor(PdfColorSpace pdfColorSpace, IList<PdfObject> operands
			, PdfResources resources)
		{
			PdfObject pdfObject;
			if (pdfColorSpace.GetPdfObject().IsIndirectReference())
			{
				pdfObject = ((PdfIndirectReference)pdfColorSpace.GetPdfObject()).GetRefersTo();
			}
			else
			{
				pdfObject = pdfColorSpace.GetPdfObject();
			}
			if (pdfObject.IsName())
			{
				if (PdfName.DeviceGray.Equals(pdfObject))
				{
					return new DeviceGray(GetColorants(operands)[0]);
				}
				else
				{
					if (PdfName.Pattern.Equals(pdfObject))
					{
						PdfDictionary patterns = resources.GetResource(PdfName.Pattern);
						if (patterns != null && operands[0] is PdfName)
						{
							PdfObject pattern = patterns.Get((PdfName)operands[0]);
							if (pattern is PdfDictionary)
							{
								return new PatternColor(PdfPattern.GetPatternInstance((PdfDictionary)pattern));
							}
						}
					}
				}
				if (PdfName.DeviceRGB.Equals(pdfObject))
				{
					float[] c = GetColorants(operands);
					return new DeviceRgb(c[0], c[1], c[2]);
				}
				else
				{
					if (PdfName.DeviceCMYK.Equals(pdfObject))
					{
						float[] c = GetColorants(operands);
						return new DeviceCmyk(c[0], c[1], c[2], c[3]);
					}
				}
			}
			else
			{
				if (pdfObject.IsArray())
				{
					PdfArray array = (PdfArray)pdfObject;
					PdfName csType = array.GetAsName(0);
					if (PdfName.CalGray.Equals(csType))
					{
						return new CalGray((PdfCieBasedCs.CalGray)pdfColorSpace, GetColorants(operands)[0
							]);
					}
					else
					{
						if (PdfName.CalRGB.Equals(csType))
						{
							return new CalRgb((PdfCieBasedCs.CalRgb)pdfColorSpace, GetColorants(operands));
						}
						else
						{
							if (PdfName.Lab.Equals(csType))
							{
								return new Lab((PdfCieBasedCs.Lab)pdfColorSpace, GetColorants(operands));
							}
							else
							{
								if (PdfName.ICCBased.Equals(csType))
								{
									return new IccBased((PdfCieBasedCs.IccBased)pdfColorSpace, GetColorants(operands)
										);
								}
								else
								{
									if (PdfName.Indexed.Equals(csType))
									{
										return new Indexed(pdfColorSpace, (int)GetColorants(operands)[0]);
									}
									else
									{
										if (PdfName.Separation.Equals(csType))
										{
											return new Separation((PdfSpecialCs.Separation)pdfColorSpace, GetColorants(operands
												)[0]);
										}
										else
										{
											if (PdfName.DeviceN.Equals(csType))
											{
												return new DeviceN((PdfSpecialCs.DeviceN)pdfColorSpace, GetColorants(operands));
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return null;
		}

		/// <summary>Gets a color based on a list of operands.</summary>
		private static Color GetColor(int nOperands, IList<PdfObject> operands)
		{
			float[] c = new float[nOperands];
			for (int i = 0; i < nOperands; i++)
			{
				c[i] = ((PdfNumber)operands[i]).FloatValue();
			}
			switch (nOperands)
			{
				case 1:
				{
					return new DeviceGray(c[0]);
				}

				case 3:
				{
					return new DeviceRgb(c[0], c[1], c[2]);
				}

				case 4:
				{
					return new DeviceCmyk(c[0], c[1], c[2], c[3]);
				}
			}
			return null;
		}

		private static float[] GetColorants(IList<PdfObject> operands)
		{
			float[] c = new float[operands.Count - 1];
			for (int i = 0; i < operands.Count - 1; i++)
			{
				c[i] = ((PdfNumber)operands[i]).FloatValue();
			}
			return c;
		}

		/// <summary>A content operator implementation (Q).</summary>
		protected internal class PopGraphicsStateOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.gsStack.Pop();
				ParserGraphicsState gs = processor.GetGraphicsState();
				processor.EventOccurred(new ClippingPathInfo(gs.GetClippingPath(), gs.GetCtm()), 
					EventType.CLIP_PATH_CHANGED);
			}
		}

		/// <summary>A content operator implementation (g).</summary>
		private class SetGrayFillOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.GetGraphicsState().SetFillColor(GetColor(1, operands));
			}
		}

		/// <summary>A content operator implementation (G).</summary>
		private class SetGrayStrokeOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.GetGraphicsState().SetStrokeColor(GetColor(1, operands));
			}
		}

		/// <summary>A content operator implementation (rg).</summary>
		private class SetRGBFillOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.GetGraphicsState().SetFillColor(GetColor(3, operands));
			}
		}

		/// <summary>A content operator implementation (RG).</summary>
		private class SetRGBStrokeOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.GetGraphicsState().SetStrokeColor(GetColor(3, operands));
			}
		}

		/// <summary>A content operator implementation (k).</summary>
		private class SetCMYKFillOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.GetGraphicsState().SetFillColor(GetColor(4, operands));
			}
		}

		/// <summary>A content operator implementation (K).</summary>
		private class SetCMYKStrokeOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.GetGraphicsState().SetStrokeColor(GetColor(4, operands));
			}
		}

		/// <summary>A content operator implementation (CS).</summary>
		private class SetColorSpaceFillOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfColorSpace pdfColorSpace = DetermineColorSpace((PdfName)operands[0], processor
					);
				processor.GetGraphicsState().SetFillColorSpace(pdfColorSpace);
				processor.GetGraphicsState().SetFillColor(new Color(pdfColorSpace, pdfColorSpace.
					GetDefaultColorants()));
			}

			internal static PdfColorSpace DetermineColorSpace(PdfName colorSpace, PdfCanvasProcessor
				 processor)
			{
				PdfColorSpace pdfColorSpace = null;
				if (PdfColorSpace.directColorSpaces.Contains(colorSpace))
				{
					pdfColorSpace = PdfColorSpace.MakeColorSpace(colorSpace);
				}
				else
				{
					PdfResources pdfResources = processor.GetResources();
					PdfDictionary resourceColorSpace = pdfResources.GetPdfObject().GetAsDictionary(PdfName
						.ColorSpace);
					pdfColorSpace = PdfColorSpace.MakeColorSpace(resourceColorSpace.Get(colorSpace));
				}
				return pdfColorSpace;
			}
		}

		/// <summary>A content operator implementation (cs).</summary>
		private class SetColorSpaceStrokeOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfColorSpace pdfColorSpace = PdfCanvasProcessor.SetColorSpaceFillOperator.DetermineColorSpace
					((PdfName)operands[0], processor);
				processor.GetGraphicsState().SetStrokeColorSpace(pdfColorSpace);
				processor.GetGraphicsState().SetStrokeColor(new Color(pdfColorSpace, pdfColorSpace
					.GetDefaultColorants()));
			}
		}

		/// <summary>A content operator implementation (sc / scn).</summary>
		private class SetColorFillOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.GetGraphicsState().SetFillColor(GetColor(processor.GetGraphicsState().GetFillColorSpace
					(), operands, processor.GetResources()));
			}
		}

		/// <summary>A content operator implementation (SC / SCN).</summary>
		private class SetColorStrokeOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.GetGraphicsState().SetStrokeColor(GetColor(processor.GetGraphicsState()
					.GetStrokeColorSpace(), operands, processor.GetResources()));
			}
		}

		/// <summary>A content operator implementation (BT).</summary>
		private class BeginTextOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.textMatrix = new Matrix();
				processor.textLineMatrix = processor.textMatrix;
				processor.BeginText();
			}
		}

		/// <summary>A content operator implementation (ET).</summary>
		private class EndTextOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.textMatrix = null;
				processor.textLineMatrix = null;
				processor.EndText();
			}
		}

		/// <summary>A content operator implementation (BMC).</summary>
		private class BeginMarkedContentOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.BeginMarkedContent((PdfName)operands[0], new PdfDictionary());
			}
		}

		/// <summary>A content operator implementation (BDC).</summary>
		private class BeginMarkedContentDictionaryOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfObject properties = operands[1];
				processor.BeginMarkedContent((PdfName)operands[0], GetPropertiesDictionary(properties
					, processor.GetResources()));
			}

			internal virtual PdfDictionary GetPropertiesDictionary(PdfObject operand1, PdfResources
				 resources)
			{
				if (operand1.IsDictionary())
				{
					return (PdfDictionary)operand1;
				}
				PdfName dictionaryName = ((PdfName)operand1);
				return resources.GetResource(PdfName.Properties).GetAsDictionary(dictionaryName);
			}
		}

		/// <summary>A content operator implementation (EMC).</summary>
		private class EndMarkedContentOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.EndMarkedContent();
			}
		}

		/// <summary>A content operator implementation (Do).</summary>
		private class DoOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfName xobjectName = (PdfName)operands[0];
				processor.DisplayXObject(xobjectName);
			}
		}

		/// <summary>A content operator implementation (EI).</summary>
		/// <remarks>
		/// A content operator implementation (EI). BI and ID operators are parsed along with this operator.
		/// This not a usual operator, it will have a single operand, which will be a PdfStream object which
		/// encapsulates inline image dictionary and bytes
		/// </remarks>
		private class EndImageOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				PdfStream imageStream = (PdfStream)operands[0];
				processor.DisplayImage(imageStream, true);
			}
		}

		/// <summary>A content operator implementation (w).</summary>
		private class SetLineWidthOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral oper, IList<PdfObject
				> operands)
			{
				float lineWidth = ((PdfNumber)operands[0]).FloatValue();
				processor.GetGraphicsState().SetLineWidth(lineWidth);
			}
		}

		/// <summary>A content operator implementation (J).</summary>
		private class SetLineCapOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral oper, IList<PdfObject
				> operands)
			{
				int lineCap = ((PdfNumber)operands[0]).IntValue();
				processor.GetGraphicsState().SetLineCapStyle(lineCap);
			}

			internal SetLineCapOperator(PdfCanvasProcessor _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly PdfCanvasProcessor _enclosing;
		}

		/// <summary>A content operator implementation (j).</summary>
		private class SetLineJoinOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral oper, IList<PdfObject
				> operands)
			{
				int lineJoin = ((PdfNumber)operands[0]).IntValue();
				processor.GetGraphicsState().SetLineJoinStyle(lineJoin);
			}

			internal SetLineJoinOperator(PdfCanvasProcessor _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly PdfCanvasProcessor _enclosing;
		}

		/// <summary>A content operator implementation (M).</summary>
		private class SetMiterLimitOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral oper, IList<PdfObject
				> operands)
			{
				float miterLimit = ((PdfNumber)operands[0]).FloatValue();
				processor.GetGraphicsState().SetMiterLimit(miterLimit);
			}

			internal SetMiterLimitOperator(PdfCanvasProcessor _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly PdfCanvasProcessor _enclosing;
		}

		/// <summary>A content operator implementation (d).</summary>
		private class SetLineDashPatternOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral oper, IList<PdfObject
				> operands)
			{
				processor.GetGraphicsState().SetDashPattern(new PdfArray(com.itextpdf.io.util.JavaUtil.ArraysAsList
					(operands[0], operands[1])));
			}

			internal SetLineDashPatternOperator(PdfCanvasProcessor _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly PdfCanvasProcessor _enclosing;
		}

		/// <summary>An XObject subtype handler for FORM</summary>
		private class FormXObjectDoHandler : IXObjectDoHandler
		{
			public virtual void HandleXObject(PdfCanvasProcessor processor, PdfStream stream)
			{
				PdfDictionary resourcesDic = stream.GetAsDictionary(PdfName.Resources);
				PdfResources resources;
				if (resourcesDic == null)
				{
					resources = processor.GetResources();
				}
				else
				{
					resources = new PdfResources(resourcesDic);
				}
				// we read the content bytes up here so if it fails we don't leave the graphics state stack corrupted
				// this is probably not necessary (if we fail on this, probably the entire content stream processing
				// operation should be rejected
				byte[] contentBytes;
				contentBytes = stream.GetBytes();
				PdfArray matrix = stream.GetAsArray(PdfName.Matrix);
				new PdfCanvasProcessor.PushGraphicsStateOperator().Invoke(processor, null, null);
				if (matrix != null)
				{
					float a = matrix.GetAsNumber(0).FloatValue();
					float b = matrix.GetAsNumber(1).FloatValue();
					float c = matrix.GetAsNumber(2).FloatValue();
					float d = matrix.GetAsNumber(3).FloatValue();
					float e = matrix.GetAsNumber(4).FloatValue();
					float f = matrix.GetAsNumber(5).FloatValue();
					Matrix formMatrix = new Matrix(a, b, c, d, e, f);
					processor.GetGraphicsState().UpdateCtm(formMatrix);
				}
				processor.ProcessContent(contentBytes, resources);
				new PdfCanvasProcessor.PopGraphicsStateOperator().Invoke(processor, null, null);
			}
		}

		/// <summary>An XObject subtype handler for IMAGE</summary>
		private class ImageXObjectDoHandler : IXObjectDoHandler
		{
			public virtual void HandleXObject(PdfCanvasProcessor processor, PdfStream xobjectStream
				)
			{
				processor.DisplayImage(xobjectStream, false);
			}
		}

		/// <summary>An XObject subtype handler that does nothing</summary>
		private class IgnoreXObjectDoHandler : IXObjectDoHandler
		{
			public virtual void HandleXObject(PdfCanvasProcessor processor, PdfStream xobjectStream
				)
			{
			}
			// ignore XObject subtype
		}

		/// <summary>A content operator implementation (m).</summary>
		private class MoveToOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				float x = ((PdfNumber)operands[0]).FloatValue();
				float y = ((PdfNumber)operands[1]).FloatValue();
				processor.currentPath.MoveTo(x, y);
			}
		}

		/// <summary>A content operator implementation (l).</summary>
		private class LineToOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				float x = ((PdfNumber)operands[0]).FloatValue();
				float y = ((PdfNumber)operands[1]).FloatValue();
				processor.currentPath.LineTo(x, y);
			}
		}

		/// <summary>A content operator implementation (c).</summary>
		private class CurveOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				float x1 = ((PdfNumber)operands[0]).FloatValue();
				float y1 = ((PdfNumber)operands[1]).FloatValue();
				float x2 = ((PdfNumber)operands[2]).FloatValue();
				float y2 = ((PdfNumber)operands[3]).FloatValue();
				float x3 = ((PdfNumber)operands[4]).FloatValue();
				float y3 = ((PdfNumber)operands[5]).FloatValue();
				processor.currentPath.CurveTo(x1, y1, x2, y2, x3, y3);
			}
		}

		/// <summary>A content operator implementation (v).</summary>
		private class CurveFirstPointDuplicatedOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				float x2 = ((PdfNumber)operands[0]).FloatValue();
				float y2 = ((PdfNumber)operands[1]).FloatValue();
				float x3 = ((PdfNumber)operands[2]).FloatValue();
				float y3 = ((PdfNumber)operands[3]).FloatValue();
				processor.currentPath.CurveTo(x2, y2, x3, y3);
			}
		}

		/// <summary>A content operator implementation (y).</summary>
		private class CurveFourhPointDuplicatedOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				float x1 = ((PdfNumber)operands[0]).FloatValue();
				float y1 = ((PdfNumber)operands[1]).FloatValue();
				float x3 = ((PdfNumber)operands[2]).FloatValue();
				float y3 = ((PdfNumber)operands[3]).FloatValue();
				processor.currentPath.CurveFromTo(x1, y1, x3, y3);
			}
		}

		/// <summary>A content operator implementation (h).</summary>
		private class CloseSubpathOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.currentPath.CloseSubpath();
			}
		}

		/// <summary>A content operator implementation (re).</summary>
		private class RectangleOperator : IContentOperator
		{
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				float x = ((PdfNumber)operands[0]).FloatValue();
				float y = ((PdfNumber)operands[1]).FloatValue();
				float w = ((PdfNumber)operands[2]).FloatValue();
				float h = ((PdfNumber)operands[3]).FloatValue();
				processor.currentPath.Rectangle(x, y, w, h);
			}
		}

		/// <summary>A content operator implementation (S, s, f, F, f*, B, B*, b, b*).</summary>
		private class PaintPathOperator : IContentOperator
		{
			private int operation;

			private int rule;

			private bool close;

			/// <summary>Constructs PainPath object.</summary>
			/// <param name="operation">
			/// One of the possible combinations of
			/// <see cref="com.itextpdf.kernel.pdf.canvas.parser.data.PathRenderInfo.STROKE"/>
			/// and
			/// <see cref="com.itextpdf.kernel.pdf.canvas.parser.data.PathRenderInfo.FILL"/>
			/// values or
			/// <see cref="com.itextpdf.kernel.pdf.canvas.parser.data.PathRenderInfo.NO_OP"/>
			/// </param>
			/// <param name="rule">
			/// Either
			/// <see cref="com.itextpdf.kernel.pdf.canvas.PdfCanvasConstants.FillingRule.NONZERO_WINDING
			/// 	"/>
			/// or
			/// <see cref="com.itextpdf.kernel.pdf.canvas.PdfCanvasConstants.FillingRule.EVEN_ODD
			/// 	"/>
			/// In case it isn't applicable pass any value.
			/// </param>
			/// <param name="close">Indicates whether the path should be closed or not.</param>
			public PaintPathOperator(int operation, int rule, bool close)
			{
				this.operation = operation;
				this.rule = rule;
				this.close = close;
			}

			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				if (close)
				{
					processor.currentPath.CloseSubpath();
				}
				processor.PaintPath(operation, rule);
			}
		}

		/// <summary>A content operator implementation (W, W*)</summary>
		private class ClipPathOperator : IContentOperator
		{
			private int rule;

			public ClipPathOperator(int rule)
			{
				this.rule = rule;
			}

			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList
				<PdfObject> operands)
			{
				processor.isClip = true;
				processor.clippingRule = rule;
			}
		}
	}
}

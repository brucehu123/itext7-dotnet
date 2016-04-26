/*
$Id: b87356a4873be885037daefbbae329b50897fe20 $

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
using com.itextpdf.layout;
using com.itextpdf.layout.element;
using com.itextpdf.layout.layout;
using com.itextpdf.layout.property;

namespace com.itextpdf.layout.renderer
{
	/// <summary>
	/// Renderer object for the
	/// <see cref="com.itextpdf.layout.element.AreaBreak"/>
	/// layout element. Will terminate the
	/// current content area and initialize a new one.
	/// </summary>
	public class AreaBreakRenderer : IRenderer
	{
		protected internal AreaBreak areaBreak;

		/// <summary>Creates an AreaBreakRenderer.</summary>
		/// <param name="areaBreak">
		/// the
		/// <see cref="com.itextpdf.layout.element.AreaBreak"/>
		/// that will be rendered by this object
		/// </param>
		public AreaBreakRenderer(AreaBreak areaBreak)
		{
			this.areaBreak = areaBreak;
		}

		public virtual void AddChild(IRenderer renderer)
		{
			throw new Exception();
		}

		public virtual LayoutResult Layout(LayoutContext layoutContext)
		{
			LayoutArea occupiedArea = layoutContext.GetArea().Clone();
			occupiedArea.GetBBox().SetHeight(0);
			occupiedArea.GetBBox().SetWidth(0);
			return new LayoutResult(LayoutResult.NOTHING, occupiedArea, null, null).SetAreaBreak
				(areaBreak);
		}

		public virtual void Draw(DrawContext drawContext)
		{
			throw new NotSupportedException();
		}

		public virtual LayoutArea GetOccupiedArea()
		{
			throw new NotSupportedException();
		}

		public virtual bool HasProperty(Property property)
		{
			return false;
		}

		public virtual bool HasOwnProperty(Property property)
		{
			return false;
		}

		public virtual T1 GetProperty<T1>(Property key)
		{
			return null;
		}

		public virtual T1 GetOwnProperty<T1>(Property property)
		{
			return null;
		}

		public virtual T1 GetDefaultProperty<T1>(Property property)
		{
			return null;
		}

		public virtual T1 GetProperty<T1>(Property property, T1 defaultValue)
		{
			throw new NotSupportedException();
		}

		public virtual void SetProperty(Property property, Object value)
		{
			throw new NotSupportedException();
		}

		public virtual void DeleteOwnProperty(Property property)
		{
		}

		public virtual IRenderer SetParent(IRenderer parent)
		{
			return this;
		}

		public virtual IPropertyContainer GetModelElement()
		{
			return null;
		}

		public virtual IList<IRenderer> GetChildRenderers()
		{
			return null;
		}

		public virtual bool IsFlushed()
		{
			return false;
		}

		public virtual void Move(float dx, float dy)
		{
			throw new NotSupportedException();
		}

		public virtual IRenderer GetNextRenderer()
		{
			return null;
		}
	}
}

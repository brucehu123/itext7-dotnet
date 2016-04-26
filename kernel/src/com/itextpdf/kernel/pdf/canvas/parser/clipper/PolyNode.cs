/*
* $Id: 788247a2670b2b0c0ac2cf8a545ef6d5cfb0a4f1 $
*
* This file is part of the iText (R) project.
* Copyright (c) 2014-2015 iText Group NV
* Authors: Bruno Lowagie, Paulo Soares, et al.
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU Affero General Public License version 3
* as published by the Free Software Foundation with the addition of the
* following permission added to Section 15 as permitted in Section 7(a):
* FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
* ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
* OF THIRD PARTY RIGHTS
*
* This program is distributed in the hope that it will be useful, but
* WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
* or FITNESS FOR A PARTICULAR PURPOSE.
* See the GNU Affero General Public License for more details.
* You should have received a copy of the GNU Affero General Public License
* along with this program; if not, see http://www.gnu.org/licenses or write to
* the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
* Boston, MA, 02110-1301 USA, or download the license from the following URL:
* http://itextpdf.com/terms-of-use/
*
* The interactive user interfaces in modified source and object code versions
* of this program must display Appropriate Legal Notices, as required under
* Section 5 of the GNU Affero General Public License.
*
* In accordance with Section 7(b) of the GNU Affero General Public License,
* a covered work must retain the producer line in every PDF that is created
* or manipulated using iText.
*
* You can be released from the requirements of the license by purchasing
* a commercial license. Buying such a license is mandatory as soon as you
* develop commercial activities involving the iText software without
* disclosing the source code of your own applications.
* These activities include: offering paid services to customers as an ASP,
* serving PDFs on the fly in a web application, shipping iText with a closed
* source product.
*
* For more information, please contact iText Software Corp. at this
* address: sales@itextpdf.com
*
*
* This class is based on the C# open source freeware library Clipper:
* http://www.angusj.com/delphi/clipper.php
* The original classes were distributed under the Boost Software License:
*
* Freeware for both open source and commercial applications
* Copyright 2010-2014 Angus Johnson
* Boost Software License - Version 1.0 - August 17th, 2003
*
* Permission is hereby granted, free of charge, to any person or organization
* obtaining a copy of the software and accompanying documentation covered by
* this license (the "Software") to use, reproduce, display, distribute,
* execute, and transmit the Software, and to prepare derivative works of the
* Software, and to permit third-parties to whom the Software is furnished to
* do so, all subject to the following:
*
* The copyright notices in the Software and this entire statement, including
* the above license grant, this restriction and the following disclaimer,
* must be included in all copies of the Software, in whole or in part, and
* all derivative works of the Software, unless such copies or derivative
* works are solely in the form of machine-executable object code generated by
* a source language processor.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
* SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
* FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
* ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
* DEALINGS IN THE SOFTWARE.
*/
using System.Collections.Generic;

namespace com.itextpdf.kernel.pdf.canvas.parser.clipper
{
	public class PolyNode
	{
		internal enum NodeType
		{
			ANY,
			OPEN,
			CLOSED
		}

		private PolyNode parent;

		private readonly Path polygon = new Path();

		private int index;

		private IClipper.JoinType joinType;

		private IClipper.EndType endType;

		protected internal readonly IList<PolyNode> childs = new List<PolyNode>();

		private bool isOpen;

		public virtual void AddChild(PolyNode child)
		{
			int cnt = childs.Count;
			childs.Add(child);
			child.parent = this;
			child.index = cnt;
		}

		public virtual int GetChildCount()
		{
			return childs.Count;
		}

		public virtual IList<PolyNode> GetChilds()
		{
			return java.util.Collections.UnmodifiableList(childs);
		}

		public virtual IList<Point.LongPoint> GetContour()
		{
			return polygon;
		}

		public virtual IClipper.EndType GetEndType()
		{
			return endType;
		}

		public virtual IClipper.JoinType GetJoinType()
		{
			return joinType;
		}

		public virtual PolyNode GetNext()
		{
			if (!childs.IsEmpty())
			{
				return childs[0];
			}
			else
			{
				return GetNextSiblingUp();
			}
		}

		private PolyNode GetNextSiblingUp()
		{
			if (parent == null)
			{
				return null;
			}
			else
			{
				if (index == parent.childs.Count - 1)
				{
					return parent.GetNextSiblingUp();
				}
				else
				{
					return parent.childs[index + 1];
				}
			}
		}

		public virtual PolyNode GetParent()
		{
			return parent;
		}

		public virtual Path GetPolygon()
		{
			return polygon;
		}

		public virtual bool IsHole()
		{
			return IsHoleNode();
		}

		private bool IsHoleNode()
		{
			bool result = true;
			PolyNode node = parent;
			while (node != null)
			{
				result = !result;
				node = node.parent;
			}
			return result;
		}

		public virtual bool IsOpen()
		{
			return isOpen;
		}

		public virtual void SetEndType(IClipper.EndType value)
		{
			endType = value;
		}

		public virtual void SetJoinType(IClipper.JoinType value)
		{
			joinType = value;
		}

		public virtual void SetOpen(bool isOpen)
		{
			this.isOpen = isOpen;
		}

		public virtual void SetParent(PolyNode n)
		{
			parent = n;
		}
	}
}

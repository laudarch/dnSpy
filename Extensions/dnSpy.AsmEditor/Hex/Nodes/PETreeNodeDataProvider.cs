﻿/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.TreeView;

namespace dnSpy.AsmEditor.Hex.Nodes {
	abstract class PETreeNodeDataProviderBase : ITreeNodeDataProvider {
		readonly Lazy<IHexDocumentService> hexDocumentService;

		protected PETreeNodeDataProviderBase(Lazy<IHexDocumentService> hexDocumentService) {
			this.hexDocumentService = hexDocumentService;
		}

		public IEnumerable<ITreeNodeData> Create(TreeNodeDataProviderContext context) {
			var fileNode = context.Owner.Data as IDsDocumentNode;
			Debug.Assert(fileNode != null);
			if (fileNode == null)
				yield break;

			bool hasPENode = HasPENode(fileNode);
			var peImage = fileNode.Document.PEImage;
			Debug.Assert(!hasPENode || peImage != null);
			if (hasPENode && peImage != null)
				yield return new PENode(hexDocumentService.Value, peImage, fileNode.Document.ModuleDef as ModuleDefMD);
		}

		public static bool HasPENode(IDsDocumentNode node) {
			if (node == null)
				return false;

			var peImage = node.Document.PEImage;

			// Only show the PE node if it was loaded from a file. The hex document is always loaded
			// from a file, so if the PEImage wasn't loaded from the same file, conversion to/from
			// RVA/FileOffset won't work and the wrong data will be displayed, eg. in the .NET
			// storage stream nodes.
			bool loadedFromFile = node.Document.Key is FilenameKey;
			return loadedFromFile && peImage != null;
		}
	}

	[ExportTreeNodeDataProvider(Guid = DocumentTreeViewConstants.MODULE_NODE_GUID)]
	sealed class ModulePETreeNodeDataProvider : PETreeNodeDataProviderBase {
		[ImportingConstructor]
		ModulePETreeNodeDataProvider(Lazy<IHexDocumentService> hexDocumentService)
			: base(hexDocumentService) {
		}
	}

	[ExportTreeNodeDataProvider(Guid = DocumentTreeViewConstants.PEDOCUMENT_NODE_GUID)]
	sealed class PEFilePETreeNodeDataProvider : PETreeNodeDataProviderBase {
		[ImportingConstructor]
		PEFilePETreeNodeDataProvider(Lazy<IHexDocumentService> hexDocumentService)
			: base(hexDocumentService) {
		}
	}
}

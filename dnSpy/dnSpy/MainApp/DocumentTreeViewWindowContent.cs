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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Contracts.TreeView;
using dnSpy.Properties;

namespace dnSpy.MainApp {
	[Export(typeof(IMainToolWindowContentProvider))]
	sealed class DocumentTreeViewWindowContentProvider : IMainToolWindowContentProvider {
		readonly IDocumentTreeView documentTreeView;

		DocumentTreeViewWindowContent DocumentTreeViewWindowContent => documentTreeViewWindowContent ?? (documentTreeViewWindowContent = new DocumentTreeViewWindowContent(documentTreeView.TreeView));
		DocumentTreeViewWindowContent documentTreeViewWindowContent;

		[ImportingConstructor]
		DocumentTreeViewWindowContentProvider(IDocumentTreeView documentTreeView) {
			this.documentTreeView = documentTreeView;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(DocumentTreeViewWindowContent.THE_GUID, DocumentTreeViewWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_LEFT_FILES, true); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			if (guid == DocumentTreeViewWindowContent.THE_GUID)
				return DocumentTreeViewWindowContent;
			return null;
		}
	}

	sealed class DocumentTreeViewWindowContent : IToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("5495EE9F-1EF2-45F3-A320-22A89BFDF731");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultVertical;

		public IInputElement FocusedElement => null;
		public FrameworkElement ScaleElement => treeView.UIObject as FrameworkElement;
		public Guid Guid => THE_GUID;
		public string Title => dnSpy_Resources.AssemblyExplorerTitle;
		public object ToolTip => null;
		public object UIObject => treeView.UIObject;
		public bool CanFocus => true;

		readonly ITreeView treeView;

		public DocumentTreeViewWindowContent(ITreeView treeView) {
			this.treeView = treeView;
			var control = treeView.UIObject as Control;
			Debug.Assert(control != null);
			if (control != null)
				control.Padding = new Thickness(0, 2, 0, 0);
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) { }
		public void Focus() => treeView.Focus();
	}

	[ExportAutoLoaded]
	sealed class ShowDocumentTreeViewCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand ShowDocumentTreeViewRoutedCommand = new RoutedCommand("ShowDocumentTreeViewRoutedCommand", typeof(ShowDocumentTreeViewCommandLoader));

		[ImportingConstructor]
		ShowDocumentTreeViewCommandLoader(IWpfCommandService wpfCommandService, IDsToolWindowService toolWindowService) {
			wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW).Add(
				ShowDocumentTreeViewRoutedCommand,
				(s, e) => toolWindowService.Show(DocumentTreeViewWindowContent.THE_GUID),
				(s, e) => e.CanExecute = true,
				ModifierKeys.Control | ModifierKeys.Alt, Key.L);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "res:AssemblyExplorerCommand", InputGestureText = "res:AssemblyExplorerKey", Icon = "Assembly", Group = MenuConstants.GROUP_APP_MENU_VIEW_WINDOWS, Order = 10)]
	sealed class ShowDocumentTreeViewCommand : MenuItemCommand {
		ShowDocumentTreeViewCommand()
			: base(ShowDocumentTreeViewCommandLoader.ShowDocumentTreeViewRoutedCommand) {
		}
	}
}

﻿/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.Debugger.Modules {
	[Export(typeof(IMainToolWindowContentCreator))]
	sealed class ModulesToolWindowContentCreator : IMainToolWindowContentCreator {
		readonly Lazy<IModulesContent> modulesContent;

		public ModulesToolWindowContent ModulesToolWindowContent {
			get { return modulesToolWindowContent ?? (modulesToolWindowContent = new ModulesToolWindowContent(modulesContent)); }
		}
		ModulesToolWindowContent modulesToolWindowContent;

		[ImportingConstructor]
		ModulesToolWindowContentCreator(Lazy<IModulesContent> modulesContent) {
			this.modulesContent = modulesContent;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(ModulesToolWindowContent.THE_GUID, ModulesToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_DEBUGGER_MODULES, false); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			if (guid == ModulesToolWindowContent.THE_GUID)
				return ModulesToolWindowContent;
			return null;
		}
	}

	sealed class ModulesToolWindowContent : IToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("8C95EB2E-25F4-4D2F-A00D-A303754990DF");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.Default;

		public IInputElement FocusedElement {
			get { return modulesContent.Value.FocusedElement; }
		}

		public Guid Guid {
			get { return THE_GUID; }
		}

		public string Title {
			get { return "Modules"; }
		}

		public object ToolTip {
			get { return null; }
		}

		public object UIObject {
			get { return modulesContent.Value.UIObject; }
		}

		public bool CanFocus {
			get { return true; }
		}

		readonly Lazy<IModulesContent> modulesContent;

		public ModulesToolWindowContent(Lazy<IModulesContent> modulesContent) {
			this.modulesContent = modulesContent;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				modulesContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				modulesContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				modulesContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				modulesContent.Value.OnHidden();
				break;
			}
		}

		public void Focus() {
			modulesContent.Value.Focus();
		}
	}
}
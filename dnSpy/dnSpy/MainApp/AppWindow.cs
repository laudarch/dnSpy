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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Controls;
using dnSpy.Events;

namespace dnSpy.MainApp {
	[Export, Export(typeof(IAppWindow))]
	sealed class AppWindow : IAppWindow, IDsLoaderContentProvider {
		public IDocumentTabService DocumentTabService => documentTabService;
		readonly IDocumentTabService documentTabService;

		public IDocumentTreeView DocumentTreeView => documentTabService.DocumentTreeView;

		public IDsToolWindowService ToolWindowService => mainWindowControl;

		public IDecompilerService DecompilerManager => decompilerService;

		public IAppStatusBar StatusBar => statusBar;
		readonly AppStatusBar statusBar;

		Window IAppWindow.MainWindow => mainWindow;
		internal MainWindow MainWindow => mainWindow;
		MainWindow mainWindow;

		public IWpfCommands MainWindowCommands => mainWindowCommands;
		readonly IWpfCommands mainWindowCommands;

		public IAppSettings AppSettings => appSettings;
		readonly IAppSettings appSettings;

		public bool AppLoaded { get; internal set; }

		public string AssemblyInformationalVersion => assemblyInformationalVersion;
		readonly string assemblyInformationalVersion;

		public IAppCommandLineArgs CommandLineArgs { get; set; }

		sealed class UISettings {
			static readonly Guid SETTINGS_GUID = new Guid("33E1988B-8EFF-4F4C-A064-FA99A7D0C64D");
			const string SAVEDWINDOWSTATE_SECTION = "SavedWindowState";
			const string MAINWINDOWCONTROLSTATE_SECTION = "MainWindowControlState";

			readonly ISettingsService settingsService;

			public SavedWindowState SavedWindowState;
			public MainWindowControlState MainWindowControlState;

			public UISettings(ISettingsService settingsService) {
				this.settingsService = settingsService;
			}

			public void Read() {
				var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
				this.SavedWindowState = new SavedWindowState().Read(sect.GetOrCreateSection(SAVEDWINDOWSTATE_SECTION));
				this.MainWindowControlState = new MainWindowControlState().Read(sect.GetOrCreateSection(MAINWINDOWCONTROLSTATE_SECTION));
			}

			public void Write() {
				var sect = settingsService.RecreateSection(SETTINGS_GUID);
				SavedWindowState.Write(sect.GetOrCreateSection(SAVEDWINDOWSTATE_SECTION));
				MainWindowControlState.Write(sect.GetOrCreateSection(MAINWINDOWCONTROLSTATE_SECTION));
			}
		}

		readonly UISettings uiSettings;
		readonly IWpfCommandService wpfCommandService;
		readonly StackedContent<IStackedContentChild> stackedContent;
		readonly IThemeService themeService;
		readonly IImageService imageService;
		readonly AppToolBar appToolBar;
		readonly MainWindowControl mainWindowControl;
		readonly IDecompilerService decompilerService;

		[ImportingConstructor]
		AppWindow(IThemeService themeService, IImageService imageService, IAppSettings appSettings, ISettingsService settingsService, IDocumentTabService documentTabService, AppToolBar appToolBar, MainWindowControl mainWindowControl, IWpfCommandService wpfCommandService, IDecompilerService decompilerService) {
			this.assemblyInformationalVersion = CalculateAssemblyInformationalVersion(GetType().Assembly);
			this.uiSettings = new UISettings(settingsService);
			this.uiSettings.Read();
			this.appSettings = appSettings;
			this.stackedContent = new StackedContent<IStackedContentChild>(margin: new Thickness(6));
			this.themeService = themeService;
			themeService.ThemeChanged += ThemeService_ThemeChanged;
			this.imageService = imageService;
			this.documentTabService = documentTabService;
			this.statusBar = new AppStatusBar();
			this.appToolBar = appToolBar;
			this.mainWindowControl = mainWindowControl;
			this.wpfCommandService = wpfCommandService;
			this.decompilerService = decompilerService;
			this.mainWindowCommands = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			this.mainWindowClosing = new WeakEventList<CancelEventArgs>();
			this.mainWindowClosed = new WeakEventList<EventArgs>();
		}

		static string CalculateAssemblyInformationalVersion(Assembly asm) {
			var attrs = asm.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
			var attr = attrs.Length == 0 ? null : attrs[0] as AssemblyInformationalVersionAttribute;
			Debug.Assert(attr != null);
			if (attr != null)
				return attr.InformationalVersion;
			return asm.GetName().Version.ToString();
		}

		void ThemeService_ThemeChanged(object sender, ThemeChangedEventArgs e) => RefreshToolBar();

		static readonly Rect DefaultWindowLocation = new Rect(10, 10, 1300, 730);
		public Window InitializeMainWindow() {
			var sc = new StackedContent<IStackedContentChild>(false);
			sc.AddChild(appToolBar, StackedContentChildInfo.CreateVertical(new GridLength(0, GridUnitType.Auto)));
			sc.AddChild(stackedContent, StackedContentChildInfo.CreateVertical(new GridLength(1, GridUnitType.Star)));
			sc.AddChild(statusBar, StackedContentChildInfo.CreateVertical(new GridLength(0, GridUnitType.Auto)));
			mainWindow = new MainWindow(themeService, imageService, sc.UIObject);
			AddTitleInfo(IntPtr.Size == 4 ? "x86" : "x64");
			wpfCommandService.Add(ControlConstants.GUID_MAINWINDOW, mainWindow);
			new SavedWindowStateRestorer(mainWindow, uiSettings.SavedWindowState, DefaultWindowLocation);
			mainWindow.Closing += MainWindow_Closing;
			mainWindow.Closed += MainWindow_Closed;
			mainWindow.GotKeyboardFocus += MainWindow_GotKeyboardFocus;
			RefreshToolBar();
			return mainWindow;
		}

		void IDsLoaderContentProvider.SetLoadingContent(object content) {
			Debug.Assert(stackedContent.Count == 0);
			stackedContent.AddChild(StackedContentChildImpl.GetOrCreate(content, content));
		}

		void IDsLoaderContentProvider.RemoveLoadingContent() {
			stackedContent.Clear();
			stackedContent.AddChild(mainWindowControl);
			mainWindowControl.Initialize(StackedContentChildImpl.GetOrCreate(documentTabService.TabGroupService, documentTabService.TabGroupService.UIObject), uiSettings.MainWindowControlState);
		}

		void MainWindow_Closing(object sender, CancelEventArgs e) {
			mainWindowClosing.Raise(this, e);
			if (e.Cancel)
				return;

			uiSettings.SavedWindowState = new SavedWindowState(mainWindow);
			uiSettings.MainWindowControlState = mainWindowControl.CreateState();
			uiSettings.Write();
		}

		void MainWindow_Closed(object sender, EventArgs e) => mainWindowClosed.Raise(this, e);

		void MainWindow_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			if (e.NewFocus == MainWindow) {
				var g = documentTabService.TabGroupService.ActiveTabGroup;
				if (g != null && g.ActiveTabContent != null) {
					g.SetFocus(g.ActiveTabContent);
					e.Handled = true;
					return;
				}
			}
		}

		public event EventHandler<CancelEventArgs> MainWindowClosing {
			add { mainWindowClosing.Add(value); }
			remove { mainWindowClosing.Remove(value); }
		}
		readonly WeakEventList<CancelEventArgs> mainWindowClosing;

		public event EventHandler<EventArgs> MainWindowClosed {
			add { mainWindowClosed.Add(value); }
			remove { mainWindowClosed.Remove(value); }
		}
		readonly WeakEventList<EventArgs> mainWindowClosed;

		public void RefreshToolBar() {
			if (mainWindow != null)
				appToolBar.Initialize(mainWindow);
		}

		void UpdateTitle() => this.mainWindow.Title = GetDefaultTitle();

		string GetDefaultTitle() {
			var t = string.Format("dnSpy {0} ({1})", AssemblyInformationalVersion, string.Join(", ", titleInfos.ToArray()));
			return t;
		}
		readonly List<string> titleInfos = new List<string>();

		public void AddTitleInfo(string info) {
			if (titleInfos.Contains(info))
				return;
			titleInfos.Add(info);
			UpdateTitle();
		}

		public void RemoveTitleInfo(string info) {
			if (titleInfos.Remove(info))
				UpdateTitle();
		}
	}
}

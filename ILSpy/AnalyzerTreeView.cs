﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.ILSpy.TreeNodes.Analyzer;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	[Export(typeof(IPaneCreator))]
	public class AnalyzerTreeViewCreator : IPaneCreator
	{
		public IPane Create(string name)
		{
			if (name == AnalyzerTreeView.Instance.PaneName)
				return AnalyzerTreeView.Instance;
			return null;
		}
	}

	/// <summary>
	/// Analyzer tree view.
	/// </summary>
	public class AnalyzerTreeView : SharpTreeView, IPane
	{
		static AnalyzerTreeView instance;

		public static AnalyzerTreeView Instance
		{
			get
			{
				if (instance == null) {
					App.Current.VerifyAccess();
					instance = new AnalyzerTreeView();
					MainWindow.InitializeTreeView(instance);
				}
				return instance;
			}
		}

		public string PaneName {
			get { return "analyzer treeview window"; }
		}

		public string PaneTitle {
			get { return "Analyzer"; }
		}

		private AnalyzerTreeView()
		{
			this.ShowRoot = false;
			this.Root = new AnalyzerRootNode { Language = MainWindow.Instance.CurrentLanguage };
			this.BorderThickness = new Thickness(0);
			ContextMenuProvider.Add(this);
			MainWindow.Instance.CurrentAssemblyListChanged += MainWindow_CurrentAssemblyListChanged;
			MainWindow.Instance.OnModuleModified += MainWindow_OnModuleModified;
			dntheme.Themes.ThemeChanged += Themes_ThemeChanged;
			Options.DisplaySettingsPanel.CurrentDisplaySettings.PropertyChanged += CurrentDisplaySettings_PropertyChanged;
		}

		void MainWindow_OnModuleModified(object sender, MainWindow.ModuleModifiedEventArgs e)
		{
			((AnalyzerRootNode)Root).HandleModelUpdated(e.LoadedAssembly);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Delete) {
				var nodes = this.GetTopLevelSelection().ToArray();
				if (nodes.Length > 0 && nodes.All(n => n.Parent.IsRoot)) {
					foreach (var node in nodes)
						node.Parent.Children.Remove(node);
					e.Handled = true;
					return;
				}
			}

			base.OnKeyDown(e);
		}

		void Themes_ThemeChanged(object sender, EventArgs e)
		{
			UpdateUIColors();
		}

		void CurrentDisplaySettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "SyntaxHighlightAnalyzerTreeViewUI" || e.PropertyName == "ShowMetadataTokens")
				UpdateUIColors();
		}

		void UpdateUIColors()
		{
			foreach (var c in this.Root.DescendantsAndSelf()) {
				var atv = c as AnalyzerTreeNode;
				if (atv != null)
					atv.RaiseUIPropsChanged();
				var ilt = c as TreeNodes.ILSpyTreeNode;
				if (ilt != null)
					ilt.RaiseUIPropsChanged();
			}
		}

		void MainWindow_CurrentAssemblyListChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (MainWindow.Instance.CurrentAssemblyList.IsReArranging)
				return;
			if (e.Action == NotifyCollectionChangedAction.Reset) {
				this.Root.Children.Clear();
			} else {
				List<LoadedAssembly> removedAssemblies = new List<LoadedAssembly>();
				if (e.OldItems != null)
					removedAssemblies.AddRange(e.OldItems.Cast<LoadedAssembly>());
				List<LoadedAssembly> addedAssemblies = new List<LoadedAssembly>();
				if (e.NewItems != null)
					addedAssemblies.AddRange(e.NewItems.Cast<LoadedAssembly>());
				((AnalyzerRootNode)this.Root).HandleAssemblyListChanged(removedAssemblies, addedAssemblies);
			}
		}

		public void Show()
		{
			if (!IsVisible)
				MainWindow.Instance.ShowInBottomPane(PaneTitle, this);
		}

		public void Opened()
		{
		}

		public void Show(AnalyzerTreeNode node)
		{
			Show();

			node.IsExpanded = true;
			this.Root.Children.Add(node);
			this.SelectedItem = node;
			this.FocusNode(node);
		}
		
		public void ShowOrFocus(AnalyzerTreeNode node)
		{
			if (node is AnalyzerEntityTreeNode) {
				var an = node as AnalyzerEntityTreeNode;
				var found = this.Root.Children.OfType<AnalyzerEntityTreeNode>().FirstOrDefault(n => n.Member == an.Member);
				if (found != null) {
					Show();
					
					found.IsExpanded = true;
					this.SelectedItem = found;
					this.FocusNode(found);
					return;
				}
			}
			Show(node);
		}

		void IPane.Closed()
		{
			this.Root.Children.Clear();
		}
		
		sealed class AnalyzerRootNode : AnalyzerTreeNode
		{
			protected override void Write(ITextOutput output, Language language)
			{
			}

			public override bool HandleAssemblyListChanged(ICollection<LoadedAssembly> removedAssemblies, ICollection<LoadedAssembly> addedAssemblies)
			{
				this.Children.RemoveAll(
					delegate(SharpTreeNode n) {
						AnalyzerTreeNode an = n as AnalyzerTreeNode;
						return an == null || !an.HandleAssemblyListChanged(removedAssemblies, addedAssemblies);
					});
				return true;
			}

			public override bool HandleModelUpdated(LoadedAssembly asm)
			{
				this.Children.RemoveAll(
					delegate(SharpTreeNode n) {
						AnalyzerTreeNode an = n as AnalyzerTreeNode;
						return an == null || !an.HandleModelUpdated(asm);
					});
				return true;
			}
		}
	}
}
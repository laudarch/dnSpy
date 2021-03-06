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
using System.Linq;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.App;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;

namespace dnSpy.AsmEditor.Compiler {
	[Export(typeof(EditCodeVMCreator))]
	sealed class EditCodeVMCreator {
		readonly IImageService imageService;
		readonly IOpenFromGAC openFromGAC;
		readonly IOpenAssembly openAssembly;
		readonly IDecompilerService decompilerService;
		readonly ILanguageCompilerProvider[] languageCompilerProviders;

		public bool CanCreate => TryGetUsedLanguage() != null;

		[ImportingConstructor]
		EditCodeVMCreator(IImageService imageService, IOpenFromGAC openFromGAC, IDocumentTreeView documentTreeView, IDecompilerService decompilerService, [ImportMany] IEnumerable<ILanguageCompilerProvider> languageCompilerProviders) {
			this.imageService = imageService;
			this.openFromGAC = openFromGAC;
			this.openAssembly = new OpenAssembly(documentTreeView.DocumentService);
			this.decompilerService = decompilerService;
			this.languageCompilerProviders = languageCompilerProviders.OrderBy(a => a.Order).ToArray();
		}

		public ImageReference? GetIcon() {
			var lang = TryGetUsedLanguage();
			Debug.Assert(lang != null);
			if (lang == null)
				return null;

			return languageCompilerProviders.FirstOrDefault(a => a.Language == lang.GenericGuid)?.Icon;
		}

		public string GetHeader() {
			var lang = TryGetUsedLanguage();
			Debug.Assert(lang != null);
			if (lang == null)
				return null;

			return string.Format(dnSpy_AsmEditor_Resources.EditMethodBodyCode, lang.GenericNameUI);
		}

		bool IsSupportedLanguage(IDecompiler decompiler) {
			if (decompiler == null)
				return false;
			if (!decompiler.CanDecompile(DecompilationType.TypeMethods))
				return false;
			return languageCompilerProviders.Any(a => a.Language == decompiler.GenericGuid);
		}

		IDecompiler TryGetUsedLanguage() {
			var defaultDecompiler = decompilerService.Decompiler;
			if (IsSupportedLanguage(defaultDecompiler))
				return defaultDecompiler;
			return decompilerService.AllDecompilers.FirstOrDefault(a => a.GenericGuid == defaultDecompiler.GenericGuid && IsSupportedLanguage(a)) ??
					decompilerService.AllDecompilers.FirstOrDefault(a => IsSupportedLanguage(a));
		}

		public EditCodeVM Create(MethodDef method, IList<MethodSourceStatement> statements) {
			Debug.Assert(CanCreate);
			if (!CanCreate)
				throw new InvalidOperationException();

			var language = TryGetUsedLanguage();
			Debug.Assert(language != null);
			if (language == null)
				throw new InvalidOperationException();

			var serviceCreator = languageCompilerProviders.FirstOrDefault(a => a.Language == language.GenericGuid);
			Debug.Assert(serviceCreator != null);
			if (serviceCreator == null)
				throw new InvalidOperationException();

			return new EditCodeVM(imageService, openFromGAC, openAssembly, serviceCreator.Create(), language, method, statements);
		}
	}
}

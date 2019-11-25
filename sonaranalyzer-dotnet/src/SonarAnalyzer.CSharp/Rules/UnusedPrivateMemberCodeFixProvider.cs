/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2019 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class UnusedPrivateMemberCodeFixProvider : SonarCodeFixProvider
    {
        internal const string Title = "Remove unused member";
        // We only want to fix S1144 and not S4487 because for S4487 the field is written so we don't know the fix
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UnusedPrivateMember.S1144DiagnosticId);
        public sealed override FixAllProvider GetFixAllProvider() => DocumentBasedFixAllProvider.Instance;

        protected sealed override Task RegisterCodeFixesAsync(SyntaxNode root, CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First(diag => diag.Id == UnusedPrivateMember.S1144DiagnosticId);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var syntax = root.FindNode(diagnosticSpan);

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    c =>
                    {
                        var newRoot = root.RemoveNode(syntax, SyntaxRemoveOptions.KeepNoTrivia);
                        return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                    }),
                context.Diagnostics);

            return TaskHelper.CompletedTask;
        }
    }
}


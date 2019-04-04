﻿using System;
using System.Linq;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.DotNet.Try.Markdown
{
    public class CodeLinkBlockRenderer : CodeBlockRenderer
    {
        public bool InlineControls { get; set; }

        public CodeLinkBlockRenderer()
        {
            OutputAttributesOnPre = false;
        }

        protected override void Write(
            HtmlRenderer renderer,
            CodeBlock codeBlock)
        {
            if (codeBlock is CodeLinkBlock codeLinkBlock)
            {
                if (codeLinkBlock.Diagnostics.Any())
                {
                    renderer.WriteLine(@"<div class=""notification is-danger"">");
                    renderer.WriteLine(SvgResources.ErrorSvg);

                    foreach (var diagnostic in codeLinkBlock.Diagnostics)
                    {
                        renderer.WriteEscape("\t" + diagnostic);
                        renderer.WriteLine();
                    }

                    renderer.WriteLine(@"</div>");
                }
                else
                {
                    codeLinkBlock.RenderTo(
                        renderer,
                        InlineControls,
                        EnablePreviewFeatures);
                }
            }
            else
            {
                base.Write(renderer, codeBlock);
            }
        }

        public bool EnablePreviewFeatures { get; set; }
    }
}
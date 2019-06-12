﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Elsa.Common.Noml.Core;
using Elsa.Common.Noml.Forms.Tables;

using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;

using IElement = Elsa.Common.Noml.Core.IElement;

namespace Elsa.Common.Noml.Forms
{
    public abstract class Form : Markup, IRenderable
    {
        protected ITableBuilder NewTable(params string[] classes)
        {
            var clsList = classes;
            if (!clsList.Any())
            {
                clsList = new[] { "fullWidth" };
            }

            var tableBuilder = new TableBuilder(clsList);
            
            return tableBuilder;
        }

        protected IElement Row(params Column[] columns)
        {
            var remainingPercent = Math.Max(0, 100 - columns.Sum(c => c.WidthPercent ?? 0));
            var unspecifiedColsCount = columns.Count(c => c.WidthPercent == null);
            var defaultPercent = unspecifiedColsCount == 0 ? 0 : remainingPercent / unspecifiedColsCount;

            var table = Table(Class("fullWidth"));

            var row = table.NewNode(Tbody()).NewNode(Tr());

            foreach (var col in columns)
            {
                var prc = $"width:{col.WidthPercent ?? defaultPercent}%";
                row.NewNode(Td(Style(prc), col.Content));
            }

            return table;
        }

        protected Column Col(int? width, params object[] content)
        {
            return new Column(width, content);
        }

        protected Column Col10(params object[] content)
        {
            return Col(10, content);
        }

        protected Column Col25(params object[] content)
        {
            return Col(25, content);
        }

        protected Column Col50(params object[] content)
        {
            return Col(50, content);
        }

        protected Column Col75(params object[] content)
        {
            return Col(75, content);
        }

        protected IElement Frame(FrameStyle frame, IElement target)
        {
            var mappedStyles =
                new[] { FrameStyle.Left, FrameStyle.Top, FrameStyle.Right, FrameStyle.Bottom };

            foreach (var mappedStyle in mappedStyles)
            {
                if ((frame & mappedStyle) == mappedStyle)
                {
                    target.SetClass($"frame{Enum.GetName(typeof(FrameStyle), mappedStyle)}");
                }
            }

            return target;
        }

        protected class Column
        {
            public Column(int? widthPercent, params object[] content)
            {
                WidthPercent = widthPercent;
                Content = content.ToList();
            }

            public int? WidthPercent { get; }

            public List<object> Content { get; }
        }

        protected abstract IEnumerable<IRenderable> Build();

        public void Render(TextWriter writer)
        {
            foreach (var elm in Build())
            {
                elm.Render(writer);
            }
        }

        public byte[] GetPdf()
        {
            var htmlText = new StringBuilder();
            using (var writer = new StringWriter(htmlText))
            {
                Render(writer);
            }

            var cssText = File.ReadAllText(HttpContext.Current.Server.MapPath("/Style/PaperForms.css"));

            using (var pdfStream = new MemoryStream())
            {
                PdfGenerator.Generate(htmlText.ToString(), cssText, pdfStream);

                return pdfStream.GetBuffer();
            }
        }
        
        [DebuggerNonUserCode]
        private static void TryDispose(params IDisposable[] o)
        {
            foreach (var disposable in o)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    ;
                }
            }
        }

        [Flags]
        protected enum FrameStyle : int
        {
            Left = 1,
            Top = 2,
            Right = 4,
            Bottom = 8,
            All = Left | Top | Right | Bottom,
            U = Left | Top | Right,
            Sides = Left | Top
        }
    }
}
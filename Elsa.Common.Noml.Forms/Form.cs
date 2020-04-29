using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Elsa.Common.Noml.Core;
using Elsa.Common.Noml.Forms.Tables;
using Elsa.Common.Utils;
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

        protected IElement TitleValue(string title, string value)
        {
            return Div(Class("titvalcont"),
                Div(Class("titvaltit"), title),
                Div(Class("titvalval"), value));
        }

        protected IElement ValueRow(string value)
        {
            return Div(Class("valueRow"), value);
        }

        protected IElement Crlf()
        {
            return Div(Class("crlf"), HtmlLiteral("&nbsp;"));
        }

        protected IElement Tabbed(params IRenderable[] content)
        {
            return Div(Class("tabbed"), content);
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

            target.SetClass("frame");

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

            string cssText;
            using (var reader =
                new StreamReader(
                    typeof(Form).Assembly.GetManifestResourceStream("Elsa.Common.Noml.Forms.Resources.PaperForms.css").Ensure("Resource not found")))
            {
                cssText = reader.ReadToEnd();
            }

            return PdfGenerator.Generate(htmlText.ToString(), cssText);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Elsa.Common;
using HtmlAgilityPack;

namespace Elsa.Portal.HtmlTransformations
{
    public class TransformScriptTags
    {
        public static string Transform(string html)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var replacements = new List<string>();

                Visit(doc.DocumentNode, replacements, NodeVisitor);

                foreach (var url in replacements)
                {
                    var splitter = url.Contains("?") ? "&" : "?";
                    {
                        html = html.Replace(url, $"{url}{splitter}_buildtag={ReleaseVersionInfo.Tag}");
                    }
                }

                html = html.Replace("%releasetag%", ReleaseVersionInfo.Tag);

                return html;
            }
            catch (Exception ex)
            {
                return $"{html}\r\n<!-- {ex.Message} -->";
            }
        }

        private static void NodeVisitor(HtmlNode node, List<string> replacements)
        {
            if (!FindScriptSources(node, replacements))
            {
                FindStylesheets(node, replacements);
            }
        }

        private static bool FindScriptSources(HtmlNode node, List<string> replacements)
        {
            if (!node.Name.Equals("script", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            var url = node.GetAttributeValue("src", null);
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                //it means external script
                return false;
            }

            replacements.Add(url);

            return true;
        }

        private static bool FindStylesheets(HtmlNode node, List<string> replacements)
        {
            if (!node.Name.Equals("link", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            var url = node.GetAttributeValue("href", null);
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                //it means external css
                return false;
            }

            replacements.Add(url);

            return true;
        }

        private static void Visit(HtmlNode node, List<string> replacements, Action<HtmlNode, List<string>> visitor)
        {
            visitor(node, replacements);

            foreach (var child in node.ChildNodes)
            {
                Visit(child, replacements, visitor);
            }
        }
    }
}
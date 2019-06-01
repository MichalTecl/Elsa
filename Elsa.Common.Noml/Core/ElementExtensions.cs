using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Noml.Core
{
    public static class ElementExtensions
    {
        public static T NewNode<T>(this IElement element, T node)
        {
            if (node == null)
            {
                return node;
            }

            if (node is string)
            {
                element.Content = node.ToString();
                return node;
            }

            switch (node)
            {
                case IAttribute a:
                    element.Attributes.Add(a);
                    break;
                case IRenderable e:
                    element.Children.Add(e);
                    break;
                case IEnumerable r:
                    foreach (var x in r)
                    {
                        element.NewNode(x);
                    }
                    break;
                default:
                    element.Content = node.ToString();
                    break;
            }

            return node;
        }

        public static void SetClass(this IElement element, string className)
        {
            var cls = element.Attributes.FirstOrDefault(a =>
                a.Name.Equals("class", StringComparison.InvariantCultureIgnoreCase));
            if (cls == null)
            {
                element.NewNode(Markup.Class(className));
                return;
            }

            var classList = cls.Value.Split(' ').ToList();
            if (classList.Contains(className))
            {
                return;
            }

            classList.Add(className);
            cls.Value = string.Join(" ", classList);
        }
    }
}

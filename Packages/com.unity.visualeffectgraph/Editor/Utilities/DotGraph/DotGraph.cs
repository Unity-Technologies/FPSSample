using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnityEditor.Dot
{
    public class DotGraph
    {
        public void AddElement(DotElement element)
        {
            if (element == null)
                throw new ArgumentNullException();

            if (element is DotEdge)
            {
                var edge = element as DotEdge;
                AddElement(edge.From);
                AddElement(edge.To);
            }

            if (!elements.ContainsKey(element))
                elements[element] = elements.Count;
        }

        public string GenerateDotString()
        {
            var nodes = elements.Where(kvp => kvp.Key is DotNode).OrderBy(kvp => kvp.Value);
            var edges = elements.Where(kvp => kvp.Key is DotEdge).Select(kvp => new KeyValuePair<DotEdge, int>((DotEdge)(kvp.Key), kvp.Value)).OrderBy(kvp => kvp.Value);

            StringBuilder builder = new StringBuilder();

            builder.AppendLine("digraph G {");

            foreach (var node in nodes)
            {
                WriteName(builder, node);
                builder.Append(' ');
                WriteAttributes(builder, node.Key);
                builder.AppendLine();
            }

            foreach (var edge in edges)
            {
                WriteName(builder, Kvp(edge.Key.From));
                builder.Append(" -> ");
                WriteName(builder, Kvp(edge.Key.To));
                builder.Append(' ');
                WriteAttributes(builder, edge.Key);
                builder.AppendLine();
            }

            builder.AppendLine("}");

            return builder.ToString();
        }

        public void OutputToDotFile(string path)
        {
            (new FileInfo(path)).Directory.Create();
            File.WriteAllText(path, GenerateDotString());
        }

        /*public OutputToImageFile(string path)
        {

        }*/

        private KeyValuePair<DotElement, int> Kvp(DotElement element)
        {
            return new KeyValuePair<DotElement, int>(element, elements[element]);
        }

        private static void WriteName<T>(StringBuilder builder, KeyValuePair<T, int> element) where T : DotElement
        {
            builder.Append(element.Key.Name);
            builder.Append(element.Value);
        }

        private static void WriteAttributes(StringBuilder builder, DotElement element)
        {
            if (!element.HasAttributes())
                return;

            char separator = '[';
            foreach (var attrib in element.attributes)
            {
                builder.Append(separator);
                builder.Append(attrib.Key);
                builder.Append("=\"");
                builder.Append(attrib.Value);
                builder.Append('\"');
                separator = ' ';
            }
            builder.Append(']');
        }

        private Dictionary<DotElement, int> elements = new Dictionary<DotElement, int>(); // elements to id
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    struct ShaderStringMapping
    {
        public INode node { get; set; }
        public int startIndex { get; set; }
        public int count { get; set; }
    }

    public class ShaderStringBuilder : IDisposable
    {
        enum ScopeType
        {
            Indent,
            Block,
            BlockSemicolon
        }

        StringBuilder m_StringBuilder;
        Stack<ScopeType> m_ScopeStack;
        int m_IndentationLevel;
        ShaderStringMapping m_CurrentMapping;
        List<ShaderStringMapping> m_Mappings;

        const string k_IndentationString = "    ";

        internal INode currentNode
        {
            get { return m_CurrentMapping.node; }
            set
            {
                m_CurrentMapping.count = m_StringBuilder.Length - m_CurrentMapping.startIndex;
                if (m_CurrentMapping.count > 0)
                    m_Mappings.Add(m_CurrentMapping);
                m_CurrentMapping.node = value;
                m_CurrentMapping.startIndex = m_StringBuilder.Length;
                m_CurrentMapping.count = 0;
            }
        }

        public ShaderStringBuilder()
        {
            m_StringBuilder = new StringBuilder();
            m_ScopeStack = new Stack<ScopeType>();
            m_Mappings = new List<ShaderStringMapping>();
            m_CurrentMapping = new ShaderStringMapping();
        }

        public ShaderStringBuilder(int indentationLevel)
            : this()
        {
            IncreaseIndent(indentationLevel);
        }

        public void AppendNewLine()
        {
            m_StringBuilder.AppendLine();
        }

        public void AppendLine(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                AppendIndentation();
                m_StringBuilder.Append(value);
            }
            AppendNewLine();
        }

        [StringFormatMethod("formatString")]
        public void AppendLine(string formatString, params object[] args)
        {
            AppendIndentation();
            m_StringBuilder.AppendFormat(formatString, args);
            AppendNewLine();
        }

        public void AppendLines(string lines)
        {
            if (string.IsNullOrEmpty(lines))
                return;
            var splitLines = lines.Split('\n');
            var lineCount = splitLines.Length;
            var lastLine = splitLines[lineCount - 1];
            if (string.IsNullOrEmpty(lastLine) || lastLine == "\r")
                lineCount--;
            for (var i = 0; i < lineCount; i++)
                AppendLine(splitLines[i].Trim('\r'));
        }

        public void Append(string value)
        {
            m_StringBuilder.Append(value);
        }

        public void Append(string value, int start, int count)
        {
            m_StringBuilder.Append(value, start, count);
        }

        [StringFormatMethod("formatString")]
        public void Append(string formatString, params object[] args)
        {
            m_StringBuilder.AppendFormat(formatString, args);
        }

        public void AppendSpaces(int count)
        {
            m_StringBuilder.Append(' ', count);
        }

        public void AppendIndentation()
        {
            for (var i = 0; i < m_IndentationLevel; i++)
                m_StringBuilder.Append(k_IndentationString);
        }

        public IDisposable IndentScope()
        {
            m_ScopeStack.Push(ScopeType.Indent);
            IncreaseIndent();
            return this;
        }

        public IDisposable BlockScope()
        {
            AppendLine("{");
            IncreaseIndent();
            m_ScopeStack.Push(ScopeType.Block);
            return this;
        }

        public IDisposable BlockSemicolonScope()
        {
            AppendLine("{");
            IncreaseIndent();
            m_ScopeStack.Push(ScopeType.BlockSemicolon);
            return this;
        }

        public void IncreaseIndent()
        {
            m_IndentationLevel++;
        }

        public void IncreaseIndent(int level)
        {
            for (var i = 0; i < level; i++)
                IncreaseIndent();
        }

        public void DecreaseIndent()
        {
            m_IndentationLevel--;
        }

        public void DecreaseIndent(int level)
        {
            for (var i = 0; i < level; i++)
                DecreaseIndent();
        }

        public void Dispose()
        {
            switch (m_ScopeStack.Pop())
            {
                case ScopeType.Indent:
                    DecreaseIndent();
                    break;
                case ScopeType.Block:
                    DecreaseIndent();
                    AppendLine("}");
                    break;
                case ScopeType.BlockSemicolon:
                    DecreaseIndent();
                    AppendLine("};");
                    break;
            }
        }

        public void Concat(ShaderStringBuilder other)
        {
            // First re-add all the mappings from `other`, such that their mappings are transformed.
            foreach (var mapping in other.m_Mappings)
            {
                currentNode = mapping.node;

                // Use `AppendLines` to indent according to the current indentation.
                AppendLines(other.ToString(mapping.startIndex, mapping.count));
            }
            currentNode = other.currentNode;
            AppendLines(other.ToString(other.m_CurrentMapping.startIndex, other.length - other.m_CurrentMapping.startIndex));
        }

        public override string ToString()
        {
            return m_StringBuilder.ToString();
        }

        public string ToString(out ShaderSourceMap sourceMap)
        {
            m_CurrentMapping.count = m_StringBuilder.Length - m_CurrentMapping.startIndex;
            if (m_CurrentMapping.count > 0)
                m_Mappings.Add(m_CurrentMapping);
            var source = m_StringBuilder.ToString();
            sourceMap = new ShaderSourceMap(source, m_Mappings);
            m_Mappings.RemoveAt(m_Mappings.Count - 1);
            m_CurrentMapping.count = 0;
            return source;
        }

        public string ToString(int startIndex, int length)
        {
            return m_StringBuilder.ToString(startIndex, length);
        }

        internal void Clear()
        {
            m_StringBuilder.Length = 0;
        }

        internal int length
        {
            get { return m_StringBuilder.Length; }
            set { m_StringBuilder.Length = value; }
        }
    }
}

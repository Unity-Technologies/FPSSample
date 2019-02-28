using System.Collections.Generic;
using System.Linq;
using UnityEditor.Dot;
using UnityEngine;
using System.Diagnostics;

namespace UnityEditor.VFX
{
    static class DotGraphOutput
    {
        private static void FillMainExpressions(Dictionary<VFXExpression, List<string>> mainExpressions, Dictionary<VFXExpression, VFXExpression> inputExpressions, VFXExpressionGraph graph)
        {
            foreach (var kvp in inputExpressions)
            {
                var exp = kvp.Key;
                var reduced = kvp.Value;

                if (mainExpressions.ContainsKey(reduced))
                    mainExpressions[reduced].AddRange(graph.GetAllNames(exp));
                else
                {
                    var list = new List<string>();
                    list.AddRange(graph.GetAllNames(exp));
                    mainExpressions[reduced] = list;
                }
            }
        }

        public static void DebugExpressionGraph(VFXGraph graph, VFXExpressionContextOption option, string fileName = "expGraph.dot")
        {
            var expressionGraph = new VFXExpressionGraph();
            expressionGraph.CompileExpressions(graph, option, true);

            var mainExpressions = new Dictionary<VFXExpression, List<string>>();
            FillMainExpressions(mainExpressions, expressionGraph.GPUExpressionsToReduced, expressionGraph);
            FillMainExpressions(mainExpressions, expressionGraph.CPUExpressionsToReduced, expressionGraph);

            var expressions = expressionGraph.Expressions;

            DotGraph dotGraph = new DotGraph();

            var expressionsToDot = new Dictionary<VFXExpression, DotNode>();
            foreach (var exp in expressions)
            {
                var dotNode = new DotNode();

                string name = exp.GetType().Name;
                name += " " + exp.valueType.ToString();
                string valueStr = GetExpressionValue(exp);
                if (!string.IsNullOrEmpty(valueStr))
                    name += string.Format(" ({0})", valueStr);

                dotNode.attributes[DotAttribute.Shape] = DotShape.Box;

                if (mainExpressions.ContainsKey(exp))
                {
                    string allOwnersStr = string.Empty;
                    foreach (var str in mainExpressions[exp])
                        allOwnersStr += "\n" + str;

                    name += string.Format("{0}", allOwnersStr);

                    if (exp.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                        dotNode.attributes[DotAttribute.Color] = DotColor.Orange;
                    else if (exp.Is(VFXExpression.Flags.Constant))
                        dotNode.attributes[DotAttribute.Color] = DotColor.SteelBlue;
                    else
                        dotNode.attributes[DotAttribute.Color] = DotColor.Cyan;
                }
                else if (exp.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                    dotNode.attributes[DotAttribute.Color] = DotColor.Yellow;
                else if (exp.Is(VFXExpression.Flags.Constant))
                    dotNode.attributes[DotAttribute.Color] = DotColor.SlateGray;
                else if (exp.Is(VFXExpression.Flags.Foldable))
                    dotNode.attributes[DotAttribute.Color] = DotColor.LightGray;

                if (dotNode.attributes.ContainsKey(DotAttribute.Color))
                    dotNode.attributes[DotAttribute.Style] = DotStyle.Filled;

                dotNode.Label = name;

                expressionsToDot[exp] = dotNode;
                dotGraph.AddElement(dotNode);
            }

            foreach (var exp in expressionsToDot)
            {
                var parents = exp.Key.parents;
                for (int i = 0; i < parents.Length; ++i)
                {
                    var dotEdge = new DotEdge(expressionsToDot[parents[i]], exp.Value);
                    if (parents.Length > 1)
                        dotEdge.attributes[DotAttribute.HeadLabel] = i.ToString();
                    dotGraph.AddElement(dotEdge);
                }
            }

            var basePath = Application.dataPath;
            basePath = basePath.Replace("/Assets", "");
            basePath = basePath.Replace("/", "\\");

            var outputfile = basePath + "\\GraphViz\\output\\" + fileName;
            dotGraph.OutputToDotFile(outputfile);

            var proc = new Process();
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.FileName = "C:\\Windows\\system32\\cmd.exe";
            var path = basePath + "\\GraphViz\\Postbuild.bat";
            proc.StartInfo.Arguments = "/c" + path + " \"" + outputfile + "\"";
            proc.EnableRaisingEvents = true;
            proc.Start();
        }

        private static string GetExpressionValue(VFXExpression exp)
        {
            if (exp is VFXValue)
            {
                var content = exp.GetContent();
                return content == null ? "null" : content.ToString();
            }
            if (exp is VFXBuiltInExpression) return ((VFXBuiltInExpression)exp).operation.ToString();
            if (exp is VFXAttributeExpression) return ((VFXAttributeExpression)exp).attributeName;

            return string.Empty;
        }
    }
}

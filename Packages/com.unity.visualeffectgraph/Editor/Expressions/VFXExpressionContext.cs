using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [Flags]
    public enum VFXExpressionContextOption
    {
        None = 0,
        Reduction = 1 << 0,
        CPUEvaluation = 1 << 1,
        ConstantFolding = 1 << 2,
        GPUDataTransformation = 1 << 3,
    }

    abstract partial class VFXExpression
    {
        public class Context
        {
            public VFXExpressionContextOption Options { get { return m_ReductionOptions; } }

            private bool Has(VFXExpressionContextOption options)
            {
                return (Options & options) == options;
            }

            private bool HasAny(VFXExpressionContextOption options)
            {
                return (Options & options) != 0;
            }

            public Context(VFXExpressionContextOption reductionOption = VFXExpressionContextOption.Reduction)
            {
                m_ReductionOptions = reductionOption;

                if (Has(VFXExpressionContextOption.CPUEvaluation) && Has(VFXExpressionContextOption.GPUDataTransformation))
                    throw new ArgumentException("Invalid reduction options");
            }

            public void RegisterExpression(VFXExpression expression)
            {
                m_EndExpressions.Add(expression);
            }

            public void UnregisterExpression(VFXExpression expression)
            {
                Invalidate(expression);
                m_EndExpressions.Remove(expression);
            }

            public void Compile()
            {
                Profiler.BeginSample("VFXEditor.CompileExpressionContext");

                try
                {
                    foreach (var exp in m_EndExpressions)
                        Compile(exp);

                    if (Has(VFXExpressionContextOption.GPUDataTransformation))
                        foreach (var exp in m_EndExpressions)
                            m_ReducedCache[exp] = InsertGPUTransformation(GetReduced(exp));
                }
                finally
                {
                    Profiler.EndSample();
                }
            }

            public void Recompile()
            {
                Invalidate();
                Compile();
            }

            private bool ShouldEvaluate(VFXExpression exp, VFXExpression[] reducedParents)
            {
                if (!HasAny(VFXExpressionContextOption.Reduction | VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding))
                    return false;

                if (exp.IsAny(Flags.NotCompilableOnCPU))
                    return false;

                if (!exp.Is(Flags.Value) && reducedParents.Length == 0) // not a value
                    return false;

                Flags flag = Flags.Value;
                if (!Has(VFXExpressionContextOption.CPUEvaluation))
                    flag |= Has(VFXExpressionContextOption.ConstantFolding) ? Flags.Foldable : Flags.Constant;

                if (exp.Is(Flags.Value) && ((exp.m_Flags & (flag | Flags.InvalidOnCPU)) != flag))
                    return false;

                return reducedParents.All(e => (e.m_Flags & (flag | Flags.InvalidOnCPU)) == flag);
            }

            private VFXExpression InsertGPUTransformation(VFXExpression exp)
            {
                switch (exp.valueType)
                {
                    case VFXValueType.ColorGradient:
                        return new VFXExpressionBakeGradient(exp);
                    case VFXValueType.Curve:
                        return new VFXExpressionBakeCurve(exp);
                    default:
                        return exp;
                }
            }

            public VFXExpression Compile(VFXExpression expression)
            {
                VFXExpression reduced;
                if (!m_ReducedCache.TryGetValue(expression, out reduced))
                {
                    var parents = expression.parents.Select(e =>
                    {
                        var parent = Compile(e);

                        if (Has(VFXExpressionContextOption.GPUDataTransformation)
                            && expression.IsAny(VFXExpression.Flags.NotCompilableOnCPU)
                            && !parent.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                            parent = InsertGPUTransformation(parent);

                        return parent;
                    }).ToArray();

                    if (ShouldEvaluate(expression, parents))
                    {
                        reduced = expression.Evaluate(parents);
                    }
                    else if (HasAny(VFXExpressionContextOption.Reduction | VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding) || !parents.SequenceEqual(expression.parents))
                    {
                        reduced = expression.Reduce(parents);
                    }
                    else
                    {
                        reduced = expression;
                    }

                    m_ReducedCache[expression] = reduced;
                }
                return reduced;
            }

            public void Invalidate()
            {
                m_ReducedCache.Clear();
            }

            public void Invalidate(VFXExpression expression)
            {
                m_ReducedCache.Remove(expression);
            }

            public VFXExpression GetReduced(VFXExpression expression)
            {
                VFXExpression reduced;
                m_ReducedCache.TryGetValue(expression, out reduced);
                return reduced != null ? reduced : expression;
            }

            private void AddReducedGraph(HashSet<VFXExpression> dst, VFXExpression exp)
            {
                if (!dst.Contains(exp))
                {
                    dst.Add(exp);
                    foreach (var parent in exp.parents)
                        AddReducedGraph(dst, parent);
                }
            }

            public HashSet<VFXExpression> BuildAllReduced()
            {
                var reduced = new HashSet<VFXExpression>();
                foreach (var exp in m_EndExpressions)
                    if (m_ReducedCache.ContainsKey(exp))
                        AddReducedGraph(reduced, m_ReducedCache[exp]);
                return reduced;
            }

            public ReadOnlyCollection<VFXExpression> RegisteredExpressions { get { return m_EndExpressions.ToList().AsReadOnly(); } }

            private Dictionary<VFXExpression, VFXExpression> m_ReducedCache = new Dictionary<VFXExpression, VFXExpression>();
            private HashSet<VFXExpression> m_EndExpressions = new HashSet<VFXExpression>();

            private VFXExpressionContextOption m_ReductionOptions;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    struct VFXNamedExpression
    {
        public VFXNamedExpression(VFXExpression exp, string name)
        {
            this.exp = exp;
            this.name = name;
        }

        public VFXExpression exp;
        public string name;
    }

    class VFXExpressionMapper
    {
        public struct Data
        {
            public string fullName { get { return id == -1 ? name : string.Format("{0}_{1}", name, VFXCodeGeneratorHelper.GeneratePrefix((uint)id)); } }
            public string name;
            public int id;
        }

        public VFXExpressionMapper()
        {
        }

        public IEnumerable<VFXExpression> expressions { get { return m_ExpressionsData.Keys; } }

        public void AddExpressionFromSlotContainer(IVFXSlotContainer slotContainer, int blockId)
        {
            foreach (var master in slotContainer.inputSlots)
            {
                foreach (var slot in master.GetExpressionSlots())
                {
                    var exp = slot.GetExpression();
                    if (!Contains(exp))
                        AddExpression(exp, slot.fullName, blockId);
                }
            }
        }

        public static VFXExpressionMapper FromBlocks(IEnumerable<VFXBlock> blocks)
        {
            var mapper = new VFXExpressionMapper();
            foreach (var block in blocks.Select((value, index) => new { index, value }))
                mapper.AddExpressions(block.value.parameters, block.index);
            return mapper;
        }

        public static VFXExpressionMapper FromContext(VFXContext context)
        {
            var mapper = FromBlocks(context.activeChildrenWithImplicit);
            mapper.AddExpressionFromSlotContainer(context, -1);
            return mapper;
        }

        public ReadOnlyCollection<Data> GetData(VFXExpression exp)
        {
            List<Data> data;
            if (m_ExpressionsData.TryGetValue(exp, out data))
            {
                return data.AsReadOnly();
            }
            return (new List<Data>()).AsReadOnly();
        }

        public bool Contains(VFXExpression exp)
        {
            return m_ExpressionsData.ContainsKey(exp);
        }

        public void AddExpression(VFXExpression exp, Data data)
        {
            AddExpression(exp, data.name, data.id);
        }

        public VFXExpression FromNameAndId(string name, int id)
        {
            foreach (var expression in m_ExpressionsData)
            {
                if (expression.Value.Any(o => o.name == name && o.id == id))
                {
                    return expression.Key;
                }
            }
            return null;
        }

        public IEnumerable<VFXNamedExpression> CollectExpression(int id, bool fullname = true)
        {
            foreach (var expressionData in m_ExpressionsData)
            {
                foreach (var data in expressionData.Value)
                {
                    if (data.id == id)
                    {
                        yield return new VFXNamedExpression(expressionData.Key, fullname ? data.fullName : data.name);
                    }
                }
            }
        }

        public void AddExpression(VFXExpression exp, string name, int id)
        {
            if (exp == null || name == null)
                throw new ArgumentNullException();

            if (m_ExpressionsData.SelectMany(o => o.Value).Any(o => o.name == name && o.id == id))
                throw new ArgumentException(string.Format("{0}_{1} has been added twice: {2}", name, id, exp));

            var data = new Data();
            data.name = name;
            data.id = id;

            if (!m_ExpressionsData.ContainsKey(exp))
            {
                m_ExpressionsData.Add(exp, new List<Data>());
            }
            m_ExpressionsData[exp].Add(data);
        }

        public void AddExpressions(IEnumerable<VFXNamedExpression> expressions, int id)
        {
            foreach (var exp in expressions)
                AddExpression(exp.exp, exp.name, id);
        }

        private Dictionary<VFXExpression, List<Data>> m_ExpressionsData = new Dictionary<VFXExpression, List<Data>>();
    }
}

using System;
using System.Linq.Expressions;

namespace UnityEditor.Recorder
{
    public static class SerializableObjHelper
    {
        public delegate TResult FuncX<TResult, TType>(TType x);
        public static SerializedProperty FindProperty(this SerializedObject obj, Expression<Func<object>> exp)
        {
            var body = exp.Body as MemberExpression;
            if (body == null)
            {
                var ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
            }

            var name = body.Member.Name;

            return obj.FindProperty(name);
        }

        public static SerializedProperty FindProperty<T>(this SerializedObject obj, Expression<FuncX<object, T>> exp)
        {
            var body = exp.Body as MemberExpression;
            if (body == null)
            {
                var ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
            }

            var name = body.Member.Name;

            return obj.FindProperty(name);
        }

        public static SerializedProperty FindPropertyRelative(this SerializedProperty obj, Expression<Func<object>> exp)
        {
            var body = exp.Body as MemberExpression;
            if (body == null)
            {
                var ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
            }

            var name = body.Member.Name;

            return obj.FindPropertyRelative(name);
        }

        public static bool TargetsSameField(this SerializedProperty obj, SerializedProperty other)
        {
            if (obj.serializedObject.targetObject != other.serializedObject.targetObject)
                return false;

            return obj.name == other.name;
        }
    }

    public class StructPropertyFinder<TType> where TType : struct
    {
        SerializedObject m_Obj;
        public StructPropertyFinder(SerializedObject obj)
        {
            m_Obj = obj;
        }

        public delegate TResult FuncX<TResult>(TType x);
        public SerializedProperty Find( Expression<FuncX<object>> exp)
        {
            var body = exp.Body as MemberExpression;
            if (body == null)
            {
                var ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
            }

            var name = body.Member.Name;

            return m_Obj.FindProperty(name);
        }

    }

    public class PropertyFinder<TType> where TType : class
    {
        SerializedObject m_Obj;
        public PropertyFinder(SerializedObject obj)
        {
            m_Obj = obj;
        }

        public delegate TResult FuncX<TResult>(TType x);
        public SerializedProperty Find( Expression<FuncX<object>> exp)
        {
            var body = exp.Body as MemberExpression;
            if (body == null)
            {
                var ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
            }

            var name = body.Member.Name;

            return m_Obj.FindProperty(name);
        }

    }
}

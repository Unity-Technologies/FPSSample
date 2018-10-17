using System;
using System.Linq;
using UnityEngine;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    static class MaterialGraphAsset
    {
        public static bool ShaderHasError(Shader shader)
        {
            var errors = GetShaderErrors(shader);
            return errors.Any(x => x.warning == 0);
        }

        public struct ShaderError
        {
            public string message;
            public string messageDetails;
            public string platform;
            public string file;
            public int line;
            public int warning;
        }

        static MethodInfo s_GetErrorsCall = typeof(ShaderUtil).GetMethod("GetShaderErrors", BindingFlags.Static | BindingFlags.NonPublic);
        static Type s_ShaderErrorType = typeof(ShaderUtil).Assembly.GetType("UnityEditor.ShaderError");
        static FieldInfo s_ShaderErrorMessageField = s_ShaderErrorType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
        static FieldInfo s_ShaderErrorMessageDetailsField = s_ShaderErrorType.GetField("messageDetails", BindingFlags.Instance | BindingFlags.Public);
        static FieldInfo s_ShaderErrorPlatformField = s_ShaderErrorType.GetField("platform", BindingFlags.Instance | BindingFlags.Public);
        static FieldInfo s_ShaderErrorFileField = s_ShaderErrorType.GetField("file", BindingFlags.Instance | BindingFlags.Public);
        static FieldInfo s_ShaderErrorLineField = s_ShaderErrorType.GetField("line", BindingFlags.Instance | BindingFlags.Public);
        static FieldInfo s_ShaderErrorWarningField = s_ShaderErrorType.GetField("warning", BindingFlags.Instance | BindingFlags.Public);

        public static ShaderError[] GetShaderErrors(Shader shader)
        {
            var invoke = s_GetErrorsCall.Invoke(null, new object[] { shader });
            var objects = (Array)invoke;
            var errors = new ShaderError[objects.Length];
            for (var i = 0; i < objects.Length; i++)
            {
                var obj = objects.GetValue(i);
                errors[i] = new ShaderError
                {
                    message = (string)s_ShaderErrorMessageField.GetValue(obj),
                    messageDetails = (string)s_ShaderErrorMessageDetailsField.GetValue(obj),
                    platform = (string)s_ShaderErrorPlatformField.GetValue(obj),
                    file = (string)s_ShaderErrorFileField.GetValue(obj),
                    line = (int)s_ShaderErrorLineField.GetValue(obj),
                    warning = (int)s_ShaderErrorWarningField.GetValue(obj),
                };
            }
            return errors;
        }
    }
}

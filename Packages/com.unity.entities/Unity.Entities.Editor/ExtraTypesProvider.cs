using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#if !UNITY_2018_2_OR_NEWER
using UnityEditor.Experimental.Build.Player;
#else
using UnityEditor.Build.Player;
#endif

namespace Unity.Entities.Editor
{
    [InitializeOnLoad]
    public sealed class ExtraTypesProvider
    {
        const string k_AssemblyName = "Unity.Entities";

        static ExtraTypesProvider()
        {
            //@TODO: Only produce JobProcessComponentDataExtensions.JobStruct_Process1
            //       if there is any use of that specific type in deployed code.
            
            PlayerBuildInterface.ExtraTypesProvider += () =>
            {
                var extraTypes = new HashSet<string>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!assembly.GetReferencedAssemblies().Any(a => a.Name.Contains(k_AssemblyName)) &&
                        assembly.GetName().Name != k_AssemblyName)
                        continue;

                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IBaseJobProcessComponentData).IsAssignableFrom(type) && !type.IsAbstract)
                        {
                            var genericArgumentList = new List<Type>
                            {
                                type
                            };

                            bool withEntity = false;
                            foreach (var @interface in type.GetInterfaces())
                            {
                                if (@interface.Name.StartsWith("IJobProcessComponentData"))
                                {
                                    genericArgumentList.AddRange(@interface.GetGenericArguments());
                                    withEntity |= @interface.Name.StartsWith("IJobProcessComponentDataWithEntity");
                                }
                            }
                            var genericArgs = genericArgumentList.ToArray();
                            int argCount = genericArgs.Length - 1;

                            Type generatedType;
                            if (argCount == 1)
                            {
                                if (withEntity)
                                    generatedType = typeof(JobProcessComponentDataExtensions.JobStruct_Process1_WE<,>).MakeGenericType(genericArgs);
                                else
                                    generatedType = typeof(JobProcessComponentDataExtensions.JobStruct_Process1<,>).MakeGenericType(genericArgs);
                                extraTypes.Add(generatedType.ToString());
                            }
                            else if (argCount == 2)
                            {
                                if (withEntity)
                                    generatedType = typeof(JobProcessComponentDataExtensions.JobStruct_Process2_WE<,,>).MakeGenericType(genericArgs);
                                else
                                    generatedType = typeof(JobProcessComponentDataExtensions.JobStruct_Process2<,,>).MakeGenericType(genericArgs);
                                    
                                extraTypes.Add(generatedType.ToString());
                            }
                            else if (argCount == 3)
                            {
                                if (withEntity)
                                    generatedType = typeof(JobProcessComponentDataExtensions.JobStruct_Process3_WE<,,,>).MakeGenericType(genericArgs);
                                else
                                    generatedType = typeof(JobProcessComponentDataExtensions.JobStruct_Process3<,,,>).MakeGenericType(genericArgs);
                                
                                extraTypes.Add(generatedType.ToString());
                            }
                            else if (argCount == 4)
                            {
                                if (withEntity)
                                    generatedType = typeof(JobProcessComponentDataExtensions.JobStruct_Process4_WE<,,,,>).MakeGenericType(genericArgs);
                                else
                                    generatedType = typeof(JobProcessComponentDataExtensions.JobStruct_Process4<,,,,>).MakeGenericType(genericArgs);
                                extraTypes.Add(generatedType.ToString());
                            }
                        }
                    }
                }

                return extraTypes;
            };
        }
    }
}
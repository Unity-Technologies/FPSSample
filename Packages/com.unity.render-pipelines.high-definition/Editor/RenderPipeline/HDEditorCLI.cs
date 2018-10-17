using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDEditorCLI
    {
        enum CommandLineOperation
        {
            None,
            ResetMaterialKeywords
        }

        struct CommandLineAction
        {
            internal CommandLineOperation operation;
        }

        const string k_SwitchOperation = "-operation";

        public static void Run()
        {
            var args = System.Environment.GetCommandLineArgs();

            var action = ParseCommandLine(args);
            Execute(action);
        }

        static void Execute(CommandLineAction action)
        {
            switch (action.operation)
            {
                case CommandLineOperation.ResetMaterialKeywords:
                {
                    Console.WriteLine("[HDEditorCLI][ResetMaterialKeywords] Starting material reset");

                    var matIds = AssetDatabase.FindAssets("t:Material");

                    for (int i = 0, length = matIds.Length; i < length; i++)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(matIds[i]);
                        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                        if (HDEditorUtils.ResetMaterialKeywords(mat))
                            Console.WriteLine("[HDEditorCLI][ResetMaterialKeywords] " + path);
                    }
                    break;
                }
            }
        }

        static CommandLineAction ParseCommandLine(string[] args)
        {
            CommandLineAction action = new CommandLineAction();
            for (int i = 0, length = args.Length; i < length; ++i)
            {
                switch (args[i])
                {
                    case k_SwitchOperation:
                    {
                        if (i + 1 < length)
                        {
                            ++i;
                            try
                            {
                                action.operation = (CommandLineOperation)Enum.Parse(typeof(CommandLineOperation), args[i]);
                            }
                            catch (Exception e) { Debug.Log(e.ToString());  }
                        }
                        break;
                    }
                }
            }
            return action;
        }
    }
}

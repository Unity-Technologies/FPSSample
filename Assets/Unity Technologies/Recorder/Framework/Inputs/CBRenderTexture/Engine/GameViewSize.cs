#if UNITY_EDITOR

using System;
using System.Collections;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Recorder.Input
{

    public class GameViewSize
    {
        static object m_InitialSizeObj;
        public static int m_ModifiedResolutionCount;

        public static EditorWindow GetMainGameView()
        {
            Type T = Type.GetType("UnityEditor.GameView,UnityEditor");
            MethodInfo GetMainGameView = T.GetMethod("GetMainGameView", BindingFlags.NonPublic | BindingFlags.Static);
            System.Object Res = GetMainGameView.Invoke(null, null);
            return (EditorWindow)Res;
        }


        public static void DisableMaxOnPlay()
        {
            var gameView = GetMainGameView();

            if ((gameView.GetType().GetField("m_MaximizeOnPlay", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(gameView)  as bool? )== true)
            {
                Debug.LogWarning("'Maximize on Play' not compatible wit recorder: disabling it!");
                gameView.GetType().GetField("m_MaximizeOnPlay", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(gameView, false);
            }
        }

        public static void GetGameRenderSize(out int width, out int height)
        {
            var gameView = GetMainGameView();
            var prop = gameView.GetType().GetProperty("targetSize", BindingFlags.NonPublic | BindingFlags.Instance);
            var size = (Vector2)prop.GetValue(gameView, new object[0] { });
            width = (int)size.x;
            height = (int)size.y;
        }

        static object Group()
        {
            var T = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
            var sizes = T.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = sizes.GetValue(null, new object[0] { });

            var currentGroup = instance.GetType().GetProperty("currentGroup", BindingFlags.Public | BindingFlags.Instance);
            var group = currentGroup.GetValue(instance, new object[0] { });
            return group;
        }

        public static object SetCustomSize(int width, int height)
        {
            var sizeObj = FindRecorderSizeObj();
            if (sizeObj != null)
            {
                sizeObj.GetType().GetField("m_Width",BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sizeObj,width);
                sizeObj.GetType().GetField("m_Height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sizeObj,height);
            }
            else
            {
                sizeObj = AddSize(width, height);
            }

            return sizeObj;
        }


        static object FindRecorderSizeObj()
        {
            var group = Group();

            var customs = @group.GetType().GetField("m_Custom", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(@group);

            var itr = (IEnumerator)customs.GetType().GetMethod("GetEnumerator").Invoke(customs, new object[] {});
            while (itr.MoveNext())
            {
                var txt = (string)itr.Current.GetType().GetField("m_BaseText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(itr.Current);
                if (txt == "(Recording resolution)")
                    return itr.Current;
            }

            return null;
        }


        public static int IndexOf(object sizeObj)
        {
            var group = Group();
            var method = @group.GetType().GetMethod("IndexOf", BindingFlags.Public | BindingFlags.Instance);
            int index = (int)method.Invoke(@group, new object[] {sizeObj}) ;

            var builtinList = @group.GetType().GetField("m_Builtin", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(@group);

            method = builtinList.GetType().GetMethod("Contains");
            if ((bool)method.Invoke(builtinList, new object[] { sizeObj }))
                return index;

            method = @group.GetType().GetMethod("GetBuiltinCount");
            index += (int)method.Invoke(@group, new object[] { });

            return index;
        }

        static object NewSizeObj(int width, int height)
        {
            var T = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
            var TT = Type.GetType("UnityEditor.GameViewSizeType,UnityEditor");

            var c = T.GetConstructor( new Type[] {TT, typeof(int), typeof(int), typeof(string)} );
            var sizeObj = c.Invoke(new object[] {1,width, height,  "(Recording resolution)"});
            return sizeObj;
        }

        public static object AddSize(int width, int height)
        {
            var sizeObj = NewSizeObj(width, height);

            var group = Group();
            var obj = @group.GetType().GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
            obj.Invoke(@group, new object[] {sizeObj}) ;

            return sizeObj;
        }

        public static void SelectSize(object size)
        {
            var index = IndexOf(size);

            var gameView = GetMainGameView();
            var obj = gameView.GetType().GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance);
            obj.Invoke(gameView, new object[] { index, size });
        }

        public static object StoredInitialSize
        {
            get
            {
                return m_InitialSizeObj; 
            }
        }

        public static object currentSize
        {
            get
            {
                var gv = GetMainGameView();
                var prop = gv.GetType().GetProperty("currentGameViewSize", BindingFlags.NonPublic | BindingFlags.Instance);
                return prop.GetValue(gv, new object[0] { });
            }
        }

        public static void BackupCurrentSize()
        {
            m_InitialSizeObj = currentSize;
        }

        public static void RestoreSize()
        {
            SelectSize(m_InitialSizeObj);
            m_InitialSizeObj = null;
        }
    }
}

#endif
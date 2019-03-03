using System;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering
{
    public partial class DebugUI
    {
        // Generic field - will be serialized in the editor if it's not read-only
        public abstract class Field<T> : Widget, IValueField
        {
            public Func<T> getter { get; set; }
            public Action<T> setter { get; set; }

            // This should be an `event` but they don't play nice with object initializers in the
            // version of C# we use.
            public Action<Field<T>, T> onValueChanged;

            object IValueField.ValidateValue(object value)
            {
                return ValidateValue((T)value);
            }

            public virtual T ValidateValue(T value)
            {
                return value;
            }

            object IValueField.GetValue()
            {
                return GetValue();
            }

            public T GetValue()
            {
                Assert.IsNotNull(getter);
                return getter();
            }

            public void SetValue(object value)
            {
                SetValue((T)value);
            }

            public void SetValue(T value)
            {
                Assert.IsNotNull(setter);
                var v = ValidateValue(value);

                if (!v.Equals(getter()))
                {
                    setter(v);

                    if (onValueChanged != null)
                        onValueChanged(this, v);
                }
            }
        }

        public class BoolField : Field<bool> {}

        public class IntField : Field<int>
        {
            public Func<int> min;
            public Func<int> max;

            // Runtime-only
            public int incStep = 1;
            public int intStepMult = 10;

            public override int ValidateValue(int value)
            {
                if (min != null) value = Mathf.Max(value, min());
                if (max != null) value = Mathf.Min(value, max());
                return value;
            }
        }

        public class UIntField : Field<uint>
        {
            public Func<uint> min;
            public Func<uint> max;

            // Runtime-only
            public uint incStep = 1u;
            public uint intStepMult = 10u;

            public override uint ValidateValue(uint value)
            {
                if (min != null) value = (uint)Mathf.Max((int)value, (int)min());
                if (max != null) value = (uint)Mathf.Min((int)value, (int)max());
                return value;
            }
        }

        public class FloatField : Field<float>
        {
            public Func<float> min;
            public Func<float> max;

            // Runtime-only
            public float incStep = 0.1f;
            public float incStepMult = 10f;
            public int decimals = 3;

            public override float ValidateValue(float value)
            {
                if (min != null) value = Mathf.Max(value, min());
                if (max != null) value = Mathf.Min(value, max());
                return value;
            }
        }

        public class EnumField : Field<int>
        {
            public GUIContent[] enumNames;
            public int[] enumValues;
            public int[] quickSeparators;
            public int[] indexes;
            
            public Func<int> getIndex { get; set; }
            public Action<int> setIndex { get; set; }

            public int currentIndex { get { return getIndex(); } set { setIndex(value); } }

            public Type autoEnum
            {
                set
                {
                    enumNames = Enum.GetNames(value).Select(x => new GUIContent(x)).ToArray();

                    // Linq.Cast<T> on a typeless Array breaks the JIT on PS4/Mono so we have to do it manually
                    //enumValues = Enum.GetValues(value).Cast<int>().ToArray();

                    var values = Enum.GetValues(value);
                    enumValues = new int[values.Length];
                    for (int i = 0; i < values.Length; i++)
                        enumValues[i] = (int)values.GetValue(i);

                    InitIndexes();
                    InitQuickSeparators();
                }
            }

            public void InitQuickSeparators()
            {
                var enumNamesPrefix = enumNames.Select(x =>
                {
                    string[] splitted = x.text.Split('/');
                    if (splitted.Length == 1)
                        return "";
                    else
                        return splitted[0];
                });
                quickSeparators = new int[enumNamesPrefix.Distinct().Count()];
                string lastPrefix = null;
                for (int i = 0, wholeNameIndex = 0; i < quickSeparators.Length; ++i)
                {
                    var currentTestedPrefix = enumNamesPrefix.ElementAt(wholeNameIndex);
                    while (lastPrefix == currentTestedPrefix)
                    {
                        currentTestedPrefix = enumNamesPrefix.ElementAt(++wholeNameIndex);
                    }
                    lastPrefix = currentTestedPrefix;
                    quickSeparators[i] = wholeNameIndex++;
                }
            }
            
            public void InitIndexes()
            {
                indexes = new int[enumNames.Length];
                for (int i = 0; i < enumNames.Length; i++)
                {
                    indexes[i] = i;
                }
            }
        }

        public class ColorField : Field<Color>
        {
            public bool hdr = false;
            public bool showAlpha = true;

            // Editor-only
            public bool showPicker = true;

            // Runtime-only
            public float incStep = 0.025f;
            public float incStepMult = 5f;
            public int decimals = 3;

            public override Color ValidateValue(Color value)
            {
                if (!hdr)
                {
                    value.r = Mathf.Clamp01(value.r);
                    value.g = Mathf.Clamp01(value.g);
                    value.b = Mathf.Clamp01(value.b);
                    value.a = Mathf.Clamp01(value.a);
                }

                return value;
            }
        }

        public class Vector2Field : Field<Vector2>
        {
            // Runtime-only
            public float incStep = 0.025f;
            public float incStepMult = 10f;
            public int decimals = 3;
        }

        public class Vector3Field : Field<Vector3>
        {
            // Runtime-only
            public float incStep = 0.025f;
            public float incStepMult = 10f;
            public int decimals = 3;
        }

        public class Vector4Field : Field<Vector4>
        {
            // Runtime-only
            public float incStep = 0.025f;
            public float incStepMult = 10f;
            public int decimals = 3;
        }
    }
}

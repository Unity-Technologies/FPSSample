using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Runtime.CompilerServices;

namespace UnityEditor.Experimental.Rendering
{
    internal class ShaderTypeGenerator
    {
        public ShaderTypeGenerator(Type type, GenerateHLSL attr)
        {
            this.type = type;
            this.attr = attr;
            debugCounter = 0;
        }

        enum PrimitiveType
        {
            Float, Int, UInt, Bool
        };

        static string PrimitiveToString(PrimitiveType type, int rows, int cols)
        {
            string text = "";
            switch (type)
            {
                case PrimitiveType.Float:
                    text = "float";
                    break;
                case PrimitiveType.Int:
                    text = "int";
                    break;
                case PrimitiveType.UInt:
                    text = "uint";
                    break;
                case PrimitiveType.Bool:
                    text = "bool";
                    break;
            }

            if (rows > 1)
            {
                text += rows.ToString();
                if (cols > 1)
                {
                    text += "x" + cols.ToString();
                }
            }

            return text;
        }

        class Accessor
        {
            public Accessor(PrimitiveType type, string name, int rows, int cols)
            {
                this.name = name;
                this.fullType = PrimitiveToString(type, rows, cols);
                field = name;
            }

            Accessor(string name, string swizzle, string field, string fullType)
            {
                this.name = name;
                this.field = field;
                this.fullType = fullType;
            }

            public string name;
            public string field;
            public string fullType;
        };

        class ShaderFieldInfo : ICloneable
        {
            public ShaderFieldInfo(PrimitiveType type, string name, int rows, int cols, int arraySize)
            {
                this.type = type;
                this.name = originalName = name;
                this.rows = rows;
                this.cols = cols;
                this.arraySize = arraySize;
                this.comment = "";
                swizzleOffset = 0;
                packed = false;
                accessor = new Accessor(type, name, rows, cols);
            }

            public ShaderFieldInfo(PrimitiveType type, string name, int rows, int cols, int arraySize, string comment)
            {
                this.type = type;
                this.name = originalName = name;
                this.rows = rows;
                this.cols = cols;
                this.arraySize = arraySize;
                this.comment = comment;
                swizzleOffset = 0;
                packed = false;
                accessor = new Accessor(type, name, rows, cols);
            }

            public string typeString
            {
                get { return PrimitiveToString(type, rows, cols); }
            }

            public string DeclString()
            {
                string arrayText = (arraySize > 0) ? "[" + arraySize + "]" : "";
                return PrimitiveToString(type, rows, cols) + " " + name + arrayText;
            }

            public override string ToString()
            {
                string text = DeclString() + ";";
                if (comment.Length > 0)
                {
                    text += " // " + comment;
                }
                return text;
            }

            public int elementCount
            {
                get { return rows * cols * Mathf.Max(arraySize, 1); }
            }

            public object Clone()
            {
                ShaderFieldInfo info = new ShaderFieldInfo(type, name, rows, cols, arraySize, comment);
                info.swizzleOffset = swizzleOffset;
                info.packed = packed;
                info.accessor = accessor;
                return info;
            }

            public readonly PrimitiveType type;
            public string name;
            public readonly string originalName;
            public readonly string comment;
            public int rows;
            public int cols;
            public int arraySize;
            public int swizzleOffset;
            public bool packed;
            public Accessor accessor;
        };

        class DebugFieldInfo
        {
            public DebugFieldInfo(string defineName, string fieldName, Type fieldType, bool isDirection, bool isSRGB)
            {
                this.defineName = defineName;
                this.fieldName = fieldName;
                this.fieldType = fieldType;
                this.isDirection = isDirection;
                this.isSRGB = isSRGB;
            }

            public string defineName;
            public string fieldName;
            public Type fieldType;
            public bool isDirection;
            public bool isSRGB;
        }

        void Error(string error)
        {
            if (errors == null)
            {
                errors = new List<string>();
            }
            errors.Add("Failed to generate shader type for " + type.ToString() + ": " + error);
        }

        public void PrintErrors()
        {
            if (errors != null)
            {
                foreach (var e in errors)
                {
                    Debug.LogError(e);
                }
            }
        }

        void EmitPrimitiveType(PrimitiveType type, int elements, int arraySize, string name, string comment, List<ShaderFieldInfo> fields)
        {
            fields.Add(new ShaderFieldInfo(type, name, elements, 1, arraySize, comment));
        }

        void EmitMatrixType(PrimitiveType type, int rows, int cols, int arraySize, string name, string comment, List<ShaderFieldInfo> fields)
        {
            fields.Add(new ShaderFieldInfo(type, name, rows, cols, arraySize, comment));
        }

        bool ExtractComplex(FieldInfo field, List<ShaderFieldInfo> shaderFields)
        {
            var floatFields = new List<FieldInfo>();
            var intFields   = new List<FieldInfo>();
            var uintFields  = new List<FieldInfo>();
            var boolFields  = new List<FieldInfo>();
            var descs = new string[4] { "x: ", "y: ", "z: ", "w: " };
            int numFields = 0;

            string fieldName = "'" + field.FieldType.Name + " " + field.Name + "'";

            foreach (var subField in field.FieldType.GetFields())
            {
                if (subField.IsStatic)
                    continue;

                if (!subField.FieldType.IsPrimitive)
                {
                    Error("'" + fieldName + "' can not be packed into a register, since it contains a non-primitive field type '" + subField.FieldType + "'");
                    return false;
                }
                if (subField.FieldType == typeof(float))
                    floatFields.Add(subField);
                else if (subField.FieldType == typeof(int))
                    intFields.Add(subField);
                else if (subField.FieldType == typeof(uint))
                    uintFields.Add(subField);
                else if (subField.FieldType == typeof(bool))
                    boolFields.Add(subField);
                else
                {
                    Error("'" + fieldName + "' can not be packed into a register, since it contains an unsupported field type '" + subField.FieldType + "'");
                    return false;
                }

                if (numFields == 4)
                {
                    Error("'" + fieldName + "' can not be packed into a register because it contains more than 4 fields.");
                    return false;
                }

                descs[numFields] += subField.Name + " ";
                numFields++;
            }
            Array.Resize(ref descs, numFields);

            string comment = string.Concat(descs);
            string mismatchErrorMsg = "'" + fieldName + "' can not be packed into a single register because it contains mixed basic types.";

            if (floatFields.Count > 0)
            {
                if (intFields.Count + uintFields.Count + boolFields.Count > 0)
                {
                    Error(mismatchErrorMsg);
                    return false;
                }
                EmitPrimitiveType(PrimitiveType.Float, floatFields.Count, 0, field.Name, comment, shaderFields);
            }
            else if (intFields.Count > 0)
            {
                if (floatFields.Count + uintFields.Count + boolFields.Count > 0)
                {
                    Error(mismatchErrorMsg);
                    return false;
                }
                EmitPrimitiveType(PrimitiveType.Int, intFields.Count, 0, field.Name, "", shaderFields);
            }
            else if (uintFields.Count > 0)
            {
                if (floatFields.Count + intFields.Count + boolFields.Count > 0)
                {
                    Error(mismatchErrorMsg);
                    return false;
                }
                EmitPrimitiveType(PrimitiveType.UInt, uintFields.Count, 0, field.Name, "", shaderFields);
            }
            else if (boolFields.Count > 0)
            {
                if (floatFields.Count + intFields.Count + uintFields.Count > 0)
                {
                    Error(mismatchErrorMsg);
                    return false;
                }
                EmitPrimitiveType(PrimitiveType.Bool, boolFields.Count, 0, field.Name, "", shaderFields);
            }
            else
            {
                // Empty struct.
            }

            return true;
        }

        enum MergeResult
        {
            Merged,
            Full,
            Failed
        };

        MergeResult PackFields(ShaderFieldInfo info, ref ShaderFieldInfo merged)
        {
            if (merged.elementCount % 4 == 0)
            {
                return MergeResult.Full;
            }

            if (info.type != merged.type)
            {
                Error("can't merge '" + merged.DeclString() + "' and '" + info.DeclString() + "' into the same register because they have incompatible types.  Consider reordering the fields so that adjacent fields have the same primitive type.");
                return MergeResult.Failed;  // incompatible types
            }

            if (info.cols > 1 || merged.cols > 1)
            {
                Error("merging matrix types not yet supported ('" + merged.DeclString() + "' and '" + info.DeclString() + "').  Consider reordering the fields to place matrix-typed variables on four-component vector boundaries.");
                return MergeResult.Failed;  // don't merge matrix types
            }

            if (info.rows + merged.rows > 4)
            {
                // @TODO:  lift the restriction
                Error("can't merge '" + merged.DeclString() + "' and '" + info.DeclString() + "' because then " + info.name + " would cross register boundary.  Consider reordering the fields so that none of them cross four-component vector boundaries when packed.");
                return MergeResult.Failed;  // out of space
            }

            merged.rows += info.rows;
            merged.name += "_" + info.name;
            return MergeResult.Merged;
        }

        List<ShaderFieldInfo> Pack(List<ShaderFieldInfo> shaderFields)
        {
            List<ShaderFieldInfo> mergedFields = new List<ShaderFieldInfo>();

            using (var e = shaderFields.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    // Empty shader struct definition.
                    return shaderFields;
                }

                ShaderFieldInfo current = e.Current.Clone() as ShaderFieldInfo;

                while (e.MoveNext())
                {
                    while (true)
                    {
                        int offset = current.elementCount;
                        var result = PackFields(e.Current, ref current);

                        if (result == MergeResult.Failed)
                        {
                            return null;
                        }
                        else if (result == MergeResult.Full)
                        {
                            break;
                        }

                        // merge accessors
                        var acc = current.accessor;

                        acc.name = current.name;
                        e.Current.accessor = acc;
                        e.Current.swizzleOffset += offset;

                        current.packed = e.Current.packed = true;

                        if (!e.MoveNext())
                        {
                            mergedFields.Add(current);
                            return mergedFields;
                        }
                    }
                    mergedFields.Add(current);
                    current = e.Current.Clone() as ShaderFieldInfo;
                }
            }
            return mergedFields;
        }

        public string EmitTypeDecl()
        {
            string shaderText = string.Empty;

            shaderText += "// Generated from " + type.FullName + "\n";
            shaderText += "// PackingRules = " + attr.packingRules.ToString() + "\n";
            if (!attr.omitStructDeclaration)
            {
                shaderText += "struct " + type.Name + "\n";
                shaderText += "{\n";
            }

            foreach (var shaderFieldInfo in m_PackedFields)
            {
                shaderText += "    " + shaderFieldInfo.ToString() + "\n";
            }

            if (!attr.omitStructDeclaration)
            {
                shaderText += "};\n";
            }

            return shaderText;
        }

        public string EmitAccessors()
        {
            string shaderText = string.Empty;

            shaderText += "//\n";
            shaderText += "// Accessors for " + type.FullName + "\n";
            shaderText += "//\n";
            foreach (var shaderField in m_ShaderFields)
            {
                Accessor acc = shaderField.accessor;
                string accessorName = shaderField.originalName;
                accessorName = "Get" + char.ToUpper(accessorName[0]) + accessorName.Substring(1);

                if (shaderField.arraySize > 0)
                    shaderText += shaderField.typeString + " " + accessorName + "(" + type.Name + " value, int index)\n";
                else
                    shaderText += shaderField.typeString + " " + accessorName + "(" + type.Name + " value)\n";
                shaderText += "{\n";

                string swizzle = "";
                string arrayAccess = "";

                // @TODO:  support matrix type packing?
                if (shaderField.cols == 1) // @TEMP
                {
                    // don't emit redundant swizzles
                    if (shaderField.originalName != acc.name)
                    {
                        swizzle = "." + "xyzw".Substring(shaderField.swizzleOffset, shaderField.elementCount);
                    }
                }

                if (shaderField.arraySize > 0)
                {
                    arrayAccess = "[index]";
                }

                shaderText +=
                            //"\t"
                            "    " // unity convention use space instead of tab...
                            + "return value." + acc.name + swizzle + arrayAccess + ";\n";
                shaderText += "}\n";
            }

            return shaderText;
        }

        public string EmitDefines()
        {
            string shaderText = string.Empty;

            shaderText += "//\n";
            shaderText += "// " + type.FullName + ":  static fields\n";
            shaderText += "//\n";
            foreach (var def in m_Statics)
            {
                shaderText += "#define " + def.Key + " (" + def.Value + ")\n";
            }

            return shaderText;
        }

        public string EmitFunctions()
        {
            string shaderText = string.Empty;

            // In case users ask for debug functions
            if (!attr.needParamDebug)
                return shaderText;

            // Specific to HDRenderPipeline
            string lowerStructName = type.Name.ToLower();

            shaderText += "//\n";
            shaderText += "// Debug functions\n";
            shaderText += "//\n";

            shaderText += "void GetGenerated" + type.Name + "Debug(uint paramId, " + type.Name + " " + lowerStructName + ", inout float3 result, inout bool needLinearToSRGB)\n";
            shaderText += "{\n";
            shaderText += "    switch (paramId)\n";
            shaderText += "    {\n";

            foreach (var debugField in m_DebugFields)
            {
                shaderText += "        case " + debugField.defineName + ":\n";
                if (debugField.fieldType == typeof(float))
                {
                    if (debugField.isDirection)
                    {
                        shaderText += "            result = " + lowerStructName + "." + debugField.fieldName + ".xxx * 0.5 + 0.5;\n";
                    }
                    else
                    {
                        shaderText += "            result = " + lowerStructName + "." + debugField.fieldName + ".xxx;\n";
                    }
                }
                else if (debugField.fieldType == typeof(Vector2))
                {
                    shaderText += "            result = float3(" + lowerStructName + "." + debugField.fieldName + ", 0.0);\n";
                }
                else if (debugField.fieldType == typeof(Vector3))
                {
                    if (debugField.isDirection)
                    {
                        shaderText += "            result = " + lowerStructName + "." + debugField.fieldName + " * 0.5 + 0.5;\n";
                    }
                    else
                    {
                        shaderText += "            result = " + lowerStructName + "." + debugField.fieldName + ";\n";
                    }
                }
                else if (debugField.fieldType == typeof(Vector4))
                {
                    shaderText += "            result = " + lowerStructName + "." + debugField.fieldName + ".xyz;\n";
                }
                else if (debugField.fieldType == typeof(bool))
                {
                    shaderText += "            result = (" + lowerStructName + "." + debugField.fieldName + ") ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);\n";
                }
                else if (debugField.fieldType == typeof(uint) || debugField.fieldType == typeof(int))
                {
                    shaderText += "            result = GetIndexColor(" + lowerStructName + "." + debugField.fieldName + ");\n";
                }
                else // This case left is suppose to be a complex structure. Either we don't support it or it is an enum. Consider it is an enum with GetIndexColor, user can override it if he want.
                {
                    shaderText += "            result = GetIndexColor(" + lowerStructName + "." + debugField.fieldName + ");\n";
                }

                if (debugField.isSRGB)
                {
                    shaderText += "            needLinearToSRGB = true;\n";
                }

                shaderText += "            break;\n";
            }

            shaderText += "    }\n";
            shaderText += "}\n";

            return shaderText;
        }

        public string Emit()
        {
            return EmitDefines() + EmitTypeDecl() + EmitAccessors();
        }

        // This function is a helper to follow unity convention
        // when converting fooBar to FOO_BAR
        string InsertUnderscore(string name)
        {
            for (int i = 1; i < name.Length; i++)
            {
                if (char.IsLower(name[i - 1]) && char.IsUpper(name[i]))
                {
                    // case switch, insert underscore
                    name = name.Insert(i, "_");
                }
            }

            return name;
        }

        public bool Generate()
        {
            m_Statics = new Dictionary<string, string>();

            FieldInfo[] fields = type.GetFields();
            m_ShaderFields = new List<ShaderFieldInfo>();
            m_DebugFields = new List<DebugFieldInfo>();

            if (type.IsEnum)
            {
                foreach (var field in fields)
                {
                    if (!field.IsSpecialName)
                    {
                        string name = field.Name;
                        name = InsertUnderscore(name);
                        m_Statics[(type.Name + "_" + name).ToUpper()] = field.GetRawConstantValue().ToString();
                    }
                }
                errors = null;
                return true;
            }

            foreach (var field in fields)
            {
                // Support array get getting array type
                Type fieldType = field.FieldType;
                int arraySize = -1;

                if (Attribute.IsDefined(field, typeof(FixedBufferAttribute)))
                {
                    var arrayInfos = (field.GetCustomAttributes(typeof(HLSLArray), false) as HLSLArray[]);
                    if (arrayInfos.Length != 0)
                    {
                        arraySize = arrayInfos[0].arraySize;
                        fieldType = arrayInfos[0].elementType;
                    }
                    else
                    {
                        // We just ignore every array type not marked with the HLSLArray attribute
                        continue;
                    }
                }
                else if (Attribute.IsDefined(field, typeof(HLSLArray)))
                {
                    Error("Invalid HLSLArray target: '" + field.FieldType + "'" + ", this attribute can only be used on fixed array");
                    return false;
                }

                if (field.IsStatic)
                {
                    if (fieldType.IsPrimitive)
                    {
                        // Unity convention is to start static of constant with k_ or s_, remove this part
                        string name = InsertUnderscore(field.Name);
                        if (name.StartsWith("k_") || name.StartsWith("s_"))
                        {
                            name = name.Substring(2);
                        }
                        string defineName = name.ToUpper();
                        m_Statics[defineName] = field.GetValue(null).ToString();
                    }
                    continue;
                }

                if (attr.needParamDebug)
                {
                    List<string> displayNames = new List<string>();
                    displayNames.Add(field.Name);

                    bool isDirection = false;
                    bool sRGBDisplay = false;

                    // Check if the display name have been override by the users
                    if (Attribute.IsDefined(field, typeof(SurfaceDataAttributes)))
                    {
                        var propertyAttr = (SurfaceDataAttributes[])field.GetCustomAttributes(typeof(SurfaceDataAttributes), false);

                        // User can want to only override isDirection and sRGBDisplay
                        // in this case it can specify empty string
                        if (propertyAttr[0].displayNames[0] != "")
                        {
                            displayNames.Clear();
                            displayNames.AddRange(propertyAttr[0].displayNames);
                        }
                        isDirection = propertyAttr[0].isDirection;
                        sRGBDisplay = propertyAttr[0].sRGBDisplay;
                    }

                    string className = type.FullName.Substring(type.FullName.LastIndexOf((".")) + 1); // ClassName include nested class
                    className = className.Replace('+', '_'); // FullName is Class+NestedClass replace by Class_NestedClass

                    foreach (string it in displayNames)
                    {
                        string fieldName = it.Replace(' ', '_');
                        string name = InsertUnderscore(fieldName);
                        string defineName = ("DEBUGVIEW_" + className + "_" + name).ToUpper();
                        m_Statics[defineName] = Convert.ToString(attr.paramDefinesStart + debugCounter++);

                        m_DebugFields.Add(new DebugFieldInfo(defineName, field.Name, fieldType, isDirection, sRGBDisplay));
                    }
                }

                if (fieldType.IsPrimitive)
                {
                    if (fieldType == typeof(float))
                        EmitPrimitiveType(PrimitiveType.Float, 1, arraySize, field.Name, "", m_ShaderFields);
                    else if (fieldType == typeof(int))
                        EmitPrimitiveType(PrimitiveType.Int, 1, arraySize, field.Name, "", m_ShaderFields);
                    else if (fieldType == typeof(uint))
                        EmitPrimitiveType(PrimitiveType.UInt, 1, arraySize, field.Name, "", m_ShaderFields);
                    else if (fieldType == typeof(bool))
                        EmitPrimitiveType(PrimitiveType.Bool, 1, arraySize, field.Name, "", m_ShaderFields);
                    else
                    {
                        Error("unsupported field type '" + fieldType + "'");
                        return false;
                    }
                }
                else
                {
                    // handle special types, otherwise try parsing the struct
                    if (fieldType == typeof(Vector2))
                        EmitPrimitiveType(PrimitiveType.Float, 2, arraySize, field.Name, "", m_ShaderFields);
                    else if (fieldType == typeof(Vector3))
                        EmitPrimitiveType(PrimitiveType.Float, 3, arraySize, field.Name, "", m_ShaderFields);
                    else if (fieldType == typeof(Vector4))
                        EmitPrimitiveType(PrimitiveType.Float, 4, arraySize, field.Name, "", m_ShaderFields);
                    else if (fieldType == typeof(Matrix4x4))
                        EmitMatrixType(PrimitiveType.Float, 4, 4, arraySize, field.Name, "", m_ShaderFields);
                    else if (!ExtractComplex(field, m_ShaderFields))
                    {
                        // Error reporting done in ExtractComplex()
                        return false;
                    }
                }
            }

            m_PackedFields = m_ShaderFields;
            if (attr.packingRules == PackingRules.Aggressive)
            {
                m_PackedFields = Pack(m_ShaderFields);

                if (m_PackedFields == null)
                {
                    return false;
                }
            }

            errors = null;
            return true;
        }

        public bool hasFields
        {
            get { return m_ShaderFields.Count > 0; }
        }

        public bool hasStatics
        {
            get { return m_Statics.Count > 0; }
        }

        public bool needAccessors
        {
            get { return attr.needAccessors; }
        }
        public bool needParamDebug
        {
            get { return attr.needParamDebug; }
        }

        public Type type;
        public GenerateHLSL attr;
        public int debugCounter;
        public List<string> errors = null;

        Dictionary<string, string> m_Statics;
        List<ShaderFieldInfo> m_ShaderFields;
        List<ShaderFieldInfo> m_PackedFields;
        List<DebugFieldInfo> m_DebugFields;
    }
}

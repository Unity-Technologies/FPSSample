using System;
using System.Collections.Generic;

namespace UnityEditor.Recorder
{
    public static class EnumHelper
    {

        public static int GetEnumValueFromMaskedIndex<TEnum>(int index, int mask)
        {
            if (!typeof(TEnum).IsEnum) throw new ArgumentException("Arg not an enum");
            var values = Enum.GetValues(typeof(TEnum));
            for( int i = 0, j = -1; i < values.Length; i++ )
            {
                if (((int)values.GetValue(i) & mask) != 0)
                    j++;

                if (j == index)
                    return (int)values.GetValue(i);
            }
            throw new ArgumentException("invalid masked index");
        }

        public static int GetEnumValueFromClippedIndex<TEnum>(int index, int start, int end)
        {
            if (!typeof(TEnum).IsEnum) throw new ArgumentException("Arg not an enum");
            var values = Enum.GetValues(typeof(TEnum));
            for( int i = 0, j = -1; i < values.Length; i++ )
            {
                if ((int)values.GetValue(i) >= start && (int)values.GetValue(i) <= end )
                    j++;

                if (j == index)
                    return (int)values.GetValue(i);
            }
            throw new ArgumentException("invalid clipped index");
        }


        public static int GetMaskedIndexFromEnumValue<TEnum>(int value, int mask)
        {
            if (!typeof(TEnum).IsEnum) throw new ArgumentException("Arg not an enum");
            var values = Enum.GetValues(typeof(TEnum));
            for( int i = 0, j = -1; i < values.Length; i++ )
            {
                var v = (int)values.GetValue(i);
                if ((v & mask) != 0)
                {
                    j++;
                    if (v == value)
                        return j;
                }
            }
            return 0;
        }

        public static int GetClippedIndexFromEnumValue<TEnum>(int value, int start, int end)
        {
            if (!typeof(TEnum).IsEnum) throw new ArgumentException("Arg not an enum");
            var values = Enum.GetValues(typeof(TEnum));
            for( int i = 0, j = -1; i < values.Length; i++ )
            {
                var v = (int)values.GetValue(i);
                if (v >= start && v <= end)
                {
                    j++;
                    if (v == value)
                        return j;
                }
            }
            return 0;
        }

        public delegate string EnumToStringDelegate(int value); 
        public static string[] MaskOutEnumNames<TEnum>(int mask, EnumToStringDelegate toString )
        {
            if (!typeof(TEnum).IsEnum) throw new ArgumentException("Arg not an enum");
            var values = Enum.GetValues(typeof(TEnum));
            var result = new List<String>();    
            for( int i = 0; i < values.Length; i++ )
            {
                if( ((int)values.GetValue(i) & mask ) != 0 )
                    result.Add( toString( (int)values.GetValue(i) ));
            }
            return result.ToArray();
        }

        public static string[] MaskOutEnumNames<TEnum>(int mask )
        {
            if (!typeof(TEnum).IsEnum) throw new ArgumentException("Arg not an enum");
            var names = Enum.GetNames(typeof(TEnum));
            var values = Enum.GetValues(typeof(TEnum));
            var result = new List<String>();
            for( int i = 0; i < values.Length; i++ )
            {
                if( ((int)values.GetValue(i) & mask ) != 0 )
                    result.Add( (string)names.GetValue(i) );
            }
            return result.ToArray();
        }

        public static string[] ClipOutEnumNames<TEnum>(int start, int end )
        {
            if (!typeof(TEnum).IsEnum) throw new ArgumentException("Arg not an enum");
            var names = Enum.GetNames(typeof(TEnum));
            var values = Enum.GetValues(typeof(TEnum));
            var result = new List<String>();
            for( int i = 0; i < values.Length; i++ )
            {
                if( (int)values.GetValue(i) >= start && (int)values.GetValue(i) <= end  )
                    result.Add( (string)names.GetValue(i) );
            }
            return result.ToArray();
        }

        

    }

}
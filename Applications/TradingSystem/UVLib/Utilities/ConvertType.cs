using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

using System.ComponentModel;

namespace UV.Lib.Utilities
{
    /// <summary>
    /// This is a a type conversion utility.
    /// </summary>
    public static class ConvertType
    {

        // Members
        //
        // Table lookup for type converters once they have been 
        // determined.
        //
        private static ConcurrentDictionary<Type, TypeConverter> _TypeConverters = new ConcurrentDictionary<Type, TypeConverter>();


        //
        // Public Methods
        //
        //
        //        
        // *************************************************
        // ****             ChangeType()                ****
        // *************************************************
        public static T ChangeType<T>(object value)
        {
            return (T)ChangeType(typeof(T), value);
        }
        //
        //
        //
        // *************************************************
        // ****             ChangeType()                ****
        // *************************************************
        public static object ChangeType(Type t, object value)
        {
            TypeConverter tc = null;
            if (_TypeConverters.TryGetValue(t, out tc) == false)
            {
                tc = TypeDescriptor.GetConverter(t);
                _TypeConverters.TryAdd(t, tc);
            }
            return tc.ConvertFrom(value);
        }
        //
        //
        //
        // *************************************************
        // ****        RegisterTypeConverter()          ****
        // *************************************************
        public static void RegisterTypeConverter<T, TC>() where TC : TypeConverter
        {
            TypeDescriptor.AddAttributes(typeof(T), new TypeConverterAttribute(typeof(TC)));
        }
        


    }//end class
}

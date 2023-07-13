using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace PatchKit.Unity.Utilities
{
    public class EnumExtension
    {
        public static T GetEnumValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum)
                throw new ArgumentException();
            FieldInfo[] fields = type.GetFields();
            var field = fields
                .SelectMany(f => f.GetCustomAttributes(
                    typeof(DescriptionAttribute), false), (
                    f, a) => new {Field = f, Att = a}).SingleOrDefault(a => ((DescriptionAttribute) a.Att)
                    .Description == description);
            return field == null ? default(T) : (T) field.Field.GetRawConstantValue();
        }
    }
}
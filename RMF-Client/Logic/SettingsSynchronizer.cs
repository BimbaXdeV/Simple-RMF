
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class SettingsSynchronizer
    {
        public static void Upload(Type source, Type target)
        {
            FieldInfo[] sourceFields = source.GetFields(BindingFlags.Static | BindingFlags.Public);
            if (sourceFields.Length == 0)
            {
                return;
            }

            FieldInfo[] targetFields = target.GetFields(BindingFlags.Static | BindingFlags.Public);
            if (targetFields.Length == 0)
            {
                return;
            }

            int updatedFieldsCounter = 0;
            foreach (var sourceField in sourceFields)
            {
                FieldInfo? targetField = targetFields.FirstOrDefault(f => f.Name == sourceField.Name);
                if (targetField != null && sourceField.FieldType == targetField.FieldType)
                {
                    var value = sourceField.GetValue(null);
                    targetField.SetValue(null, value);
                    updatedFieldsCounter++;
                }
            }
        }
    }
}

using System;

namespace DesktopEye.Common.Extensions;

public static class EnumExtensions
{
    public static string GetStringValue(this Enum enumValue)
    {
        var type = enumValue.GetType();
        var fieldInfo = type.GetField(enumValue.ToString());

        var attributes = fieldInfo?.GetCustomAttributes(false);

        if (attributes != null)
            foreach (var attribute in attributes)
            {
                var attributeType = attribute.GetType();

                // Look for StringAttribute regardless of namespace
                if (attributeType.Name == "StringAttribute")
                {
                    // Try common property names
                    var properties = new[] { "Value", "Text", "String" };
                    foreach (var propName in properties)
                    {
                        var prop = attributeType.GetProperty(propName);
                        if (prop != null) return prop.GetValue(attribute)?.ToString() ?? enumValue.ToString();
                    }
                }
            }

        return enumValue.ToString();
    }
}
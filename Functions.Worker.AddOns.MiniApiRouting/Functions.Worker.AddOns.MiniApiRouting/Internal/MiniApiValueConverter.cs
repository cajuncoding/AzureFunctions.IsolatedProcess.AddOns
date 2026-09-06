using System.ComponentModel;
using System.Globalization;

namespace Functions.Worker.AddOns.MiniApiRouting;

public static class MiniApiValueConverter
{
    public static T Convert<T>(string? value, string name, string source) => (T)Convert(value, typeof(T), name, source)!;

    public static object? Convert(string? value, Type targetType, string name, string source)
    {
        if (value is null && (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null))
            return null;

        var valueType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (valueType == typeof(string))
                return value;
            if (valueType == typeof(char))
                return char.Parse(value!);
            if (valueType == typeof(Guid))
                return Guid.Parse(value!);
            if (valueType == typeof(DateTime))
                return DateTime.Parse(value!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            if (valueType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(value!, CultureInfo.InvariantCulture);
            if (valueType == typeof(TimeSpan))
                return TimeSpan.Parse(value!, CultureInfo.InvariantCulture);
            if (valueType.IsEnum)
                return ParseDefinedEnum(valueType, value!);

            var converter = TypeDescriptor.GetConverter(valueType);
            if (converter.CanConvertFrom(typeof(string)))
                return converter.ConvertFrom(null, CultureInfo.InvariantCulture, value!);

            throw new InvalidOperationException($"Type '{targetType.FullName}' cannot be converted from a string value.");
        }
        catch (Exception exception)
        {
            throw new MiniApiParameterBindingException(name, targetType, value, source, exception);
        }
    }

    private static object ParseDefinedEnum(Type enumType, string value)
    {
        if (!Enum.TryParse(enumType, value, true, out var parsedValue) || !Enum.IsDefined(enumType, parsedValue))
            throw new InvalidOperationException($"Value '{value}' is not defined by enum type '{enumType.FullName}'.");

        return parsedValue;
    }
}

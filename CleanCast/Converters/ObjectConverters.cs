using System;
using Avalonia.Data.Converters;
using System.Globalization;

namespace CleanCast.Converters
{
    public class IsNullConverter : IValueConverter
    {
        public static readonly IsNullConverter Instance = new IsNullConverter();
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsNotNullConverter : IValueConverter
    {
        public static readonly IsNotNullConverter Instance = new IsNotNullConverter();
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is not null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsNotNullOrEmptyConverter : IValueConverter
    {
        public static readonly IsNotNullOrEmptyConverter Instance = new IsNotNullOrEmptyConverter();
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return false;
            if (value is string s) return !string.IsNullOrWhiteSpace(s);
            return true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public static class ObjectConverters
    {
        public static IValueConverter IsNull => IsNullConverter.Instance;
        public static IValueConverter IsNotNull => IsNotNullConverter.Instance;
        public static IValueConverter IsNotNullOrEmpty => IsNotNullOrEmptyConverter.Instance;
    }
}

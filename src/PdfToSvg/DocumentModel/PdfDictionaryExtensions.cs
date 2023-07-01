﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Drawing;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PdfToSvg.DocumentModel
{
    internal static class PdfDictionaryExtensions
    {
        public static bool TryGetValue(this PdfDictionary? dict, PdfNamePath path, out object? value)
        {
            value = dict;

            foreach (var name in path)
            {
                if (value is object[] arr)
                {
                    if (arr.Length == 0)
                    {
                        return false;
                    }
                    else if (name == Indexes.First)
                    {
                        value = arr[0];
                    }
                    else if (name == Indexes.Last)
                    {
                        value = arr[arr.Length - 1];
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (value is PdfDictionary subdict)
                {
                    if (!subdict.TryGetValue(name, out value))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public static PdfName? GetNameOrNull(this PdfDictionary? dict, PdfNamePath path)
        {
            return GetValueOrDefault<PdfName?>(dict, path, null);
        }

        public static PdfDictionary? GetDictionaryOrNull(this PdfDictionary? dict, PdfNamePath path)
        {
            return GetValueOrDefault<PdfDictionary?>(dict, path, null);
        }

        public static PdfDictionary GetDictionaryOrEmpty(this PdfDictionary? dict, PdfNamePath path)
        {
            return GetValueOrDefault<PdfDictionary?>(dict, path, null) ?? new PdfDictionary();
        }

        public static T[]? GetArrayOrNull<T>(this PdfDictionary? dict, PdfNamePath path)
        {
            return TryGetArray<T>(dict, path, out var arr) ? arr : null;
        }

        public static T GetValueOrDefault<T>(this PdfDictionary? dict, PdfNamePath path, T defaultValue = default!)
        {
            if (TryGetValue(dict, path, out var objValue) && TryConvert(objValue, typeof(T), out var convertedValue))
            {
                return (T)convertedValue!;
            }

            return defaultValue;
        }

        public static object? GetValueOrDefault(this PdfDictionary? dict, PdfNamePath path, object? defaultValue = default)
        {
            if (TryGetValue(dict, path, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public static bool TryGetValue<T>(this PdfDictionary? dict, PdfNamePath path, out T value)
        {
            if (TryGetValue(dict, path, out object? objValue) &&
                TryConvert(objValue, typeof(T), out var convertedValue))
            {
                value = (T)convertedValue;
                return true;
            }

            value = default!;
            return false;
        }

        public static bool TryGetInteger(this PdfDictionary? dict, PdfNamePath path, out int value)
        {
            return TryGetValue(dict, path, out value);
        }

        public static bool TryGetName(this PdfDictionary? dict, PdfNamePath path, [NotNullWhen(true)] out PdfName? value)
        {
            return TryGetValue(dict, path, out value);
        }

        public static bool TryGetArray(this PdfDictionary? dict, PdfNamePath path, [NotNullWhen(true)] out object?[]? value)
        {
            return TryGetValue(dict, path, out value);
        }

        public static bool TryGetArray<T>(this PdfDictionary? dict, PdfNamePath path, [NotNullWhen(true)] out T[]? value)
        {
            if (dict.TryGetArray(path, out var objArray))
            {
                value = new T[objArray.Length];

                for (var i = 0; i < objArray.Length; i++)
                {
                    if (TryConvert(objArray[i], typeof(T), out var convertedValue))
                    {
                        value[i] = (T)convertedValue;
                    }
                    else
                    {
                        value = null;
                        return false;
                    }
                }

                return true;
            }

            value = null;
            return false;
        }

        public static bool TryGetDictionary(this PdfDictionary? dict, PdfNamePath path, [NotNullWhen(true)] out PdfDictionary? value)
        {
            return TryGetValue(dict, path, out value);
        }

        public static bool TryGetNumber(this PdfDictionary? dict, PdfNamePath path, out double value)
        {
            if (dict.TryGetValue(path, out object? objValue))
            {
                if (objValue is int intValue)
                {
                    value = intValue;
                    return true;
                }

                if (objValue is double doubleValue)
                {
                    value = doubleValue;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        public static bool TryGetStream(this PdfDictionary? dict, PdfNamePath path, [NotNullWhen(true)] out PdfStream? stream)
        {
            return TryGetValue(dict, path, out stream);
        }

        private static bool TryParseDate(object? value, out DateTimeOffset result)
        {
            // PDF spec 1.7, 7.9.4, page 95
            // Note that there is a difference between what is documented in ISO 32000-1:2008 and what pdf producers do.
            // https://stackoverflow.com/questions/41661477/what-is-the-correct-format-of-a-date-string

            var str = value as string;

            if (str == null && value is PdfString pdfString)
            {
                str = pdfString.ToString();
            }

            if (str != null && str.Length < 24 && str.StartsWith("D:"))
            {
                var cursor = 2;
                var invalid = false;

                int ReadNumber(int digits, int defaultValue, int min, int max)
                {
                    if (cursor < str.Length)
                    {
                        if (cursor + digits <= str.Length &&
                            int.TryParse(str.Substring(cursor, digits), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) &&
                            number >= min && number <= max)
                        {
                            cursor += digits;
                            return number;
                        }
                        else
                        {
                            invalid = true;
                            cursor = str.Length;
                        }
                    }

                    return defaultValue;
                }

                var year = ReadNumber(4, -1, 0, 9999);
                var month = ReadNumber(2, 1, 1, 12);
                var day = ReadNumber(2, 1, 1, 31);
                var hour = ReadNumber(2, 0, 0, 23);
                var minute = ReadNumber(2, 0, 0, 59);
                var second = ReadNumber(2, 0, 0, 59);

                var negativeOffset = false;

                if (cursor < str.Length)
                {
                    switch (str[cursor++])
                    {
                        case 'z':
                        case 'Z':
                            if (cursor < str.Length)
                            {
                                invalid = true;
                            }
                            break;
                        case '+':
                            break;
                        case '-':
                            negativeOffset = true;
                            break;
                        default:
                            invalid = true;
                            break;
                    }
                }

                var tzHour = ReadNumber(2, 0, 0, 23);

                if (cursor < str.Length && str[cursor++] != '\'')
                {
                    invalid = true;
                }

                var tzMinute = ReadNumber(2, 0, 0, 59);

                // Trailing apostrophe is not valid, but many pdfs have one
                // https://stackoverflow.com/questions/41661477/what-is-the-correct-format-of-a-date-string
                if (cursor < str.Length && str[cursor++] != '\'')
                {
                    invalid = true;
                }

                if (!invalid && cursor == str.Length)
                {
                    var tzOffset = new TimeSpan(tzHour, tzMinute, 0);
                    if (negativeOffset)
                    {
                        tzOffset = -tzOffset;
                    }

                    try
                    {
                        result = new DateTimeOffset(
                            year, month, day,
                            hour, minute, second,
                            tzOffset);
                        return true;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }
                }
            }

            result = default;
            return false;
        }

        private static bool TryConvertDoubleArray(object? value, int minLength, [NotNullWhen(true)] out double[]? result)
        {
            if (value is object[] objArray && objArray.Length >= minLength)
            {
                result = new double[objArray.Length];

                for (var i = 0; i < objArray.Length; i++)
                {
                    if (TryConvert(objArray[i], typeof(double), out var convertedValue))
                    {
                        result[i] = (float)(double)convertedValue;
                    }
                    else
                    {
                        result = null;
                        return false;
                    }
                }

                return true;
            }

            result = null;
            return false;
        }

        private static bool TryConvertFloatArray(object? value, int minLength, [NotNullWhen(true)] out float[]? result)
        {
            if (value is object[] objArray && objArray.Length >= minLength)
            {
                result = new float[objArray.Length];

                for (var i = 0; i < objArray.Length; i++)
                {
                    if (TryConvert(objArray[i], typeof(double), out var convertedValue))
                    {
                        result[i] = (float)(double)convertedValue;
                    }
                    else
                    {
                        result = null;
                        return false;
                    }
                }

                return true;
            }

            result = null;
            return false;
        }

        private static bool TryConvert(object? value, Type destinationType, [NotNullWhen(true)] out object? result)
        {
            var nullableType = Nullable.GetUnderlyingType(destinationType);
            if (nullableType != null)
            {
                destinationType = nullableType;
            }

            if (value == null)
            {
                result = null;
                return nullableType != null || !destinationType.GetTypeInfo().IsValueType;
            }

            if (destinationType.GetTypeInfo().IsAssignableFrom(value.GetType()))
            {
                result = value;
                return true;
            }

            if (destinationType == typeof(int))
            {
                if (value is double dbl)
                {
                    result = (int)dbl;
                    return true;
                }
            }
            else if (destinationType == typeof(double))
            {
                if (value is int intval)
                {
                    result = (double)intval;
                    return true;
                }
            }
            else if (destinationType == typeof(DateTimeOffset))
            {
                if (TryParseDate(value, out var dto))
                {
                    result = dto;
                    return true;
                }
            }
            else if (destinationType == typeof(DateTime))
            {
                if (TryParseDate(value, out var dto))
                {
                    result = dto.DateTime;
                    return true;
                }
            }
            else if (destinationType == typeof(PdfStream))
            {
                if (value is PdfDictionary dict && dict.Stream != null)
                {
                    result = dict.Stream;
                    return true;
                }
            }
            else if (destinationType == typeof(Matrix1x3))
            {
                if (TryConvertFloatArray(value, 3, out var arr))
                {
                    result = new Matrix1x3(arr[0], arr[1], arr[2]);
                    return true;
                }
            }
            else if (destinationType == typeof(Matrix3x3))
            {
                if (TryConvertFloatArray(value, 9, out var arr))
                {
                    result = new Matrix3x3(
                        arr[0], arr[1], arr[2],
                        arr[3], arr[4], arr[5],
                        arr[6], arr[7], arr[8]);
                    return true;
                }
            }
            else if (destinationType == typeof(Matrix))
            {
                if (TryConvertDoubleArray(value, 6, out var arr))
                {
                    result = new Matrix(
                        arr[0], arr[1],
                        arr[2], arr[3],
                        arr[4], arr[5]);
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
}

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace PRN232.LMS.API.Formatters;

/// <summary>
/// Writes any object graph as XML elements. Used by <see cref="Prn232XmlOutputFormatter"/>.
/// Handles nested objects, dictionaries (field-selection results) and collections,
/// which the built-in XML serializers cannot handle for object/dictionary payloads.
/// </summary>
internal static class XmlGraphWriter
{
    public static void Write(XmlWriter writer, string name, object? value)
    {
        var elementName = Sanitize(name);

        if (value is null)
        {
            writer.WriteStartElement(elementName);
            writer.WriteEndElement();
            return;
        }

        if (value is string or char or bool or byte or sbyte or short or ushort or int or uint
            or long or ulong or float or double or decimal or DateTime or DateTimeOffset or Guid
            || value.GetType().IsEnum)
        {
            writer.WriteElementString(elementName, Format(value));
            return;
        }

        if (value is IDictionary dictionary)
        {
            writer.WriteStartElement(elementName);
            foreach (DictionaryEntry entry in dictionary)
            {
                Write(writer, entry.Key?.ToString() ?? "item", entry.Value);
            }

            writer.WriteEndElement();
            return;
        }

        if (value is IEnumerable enumerable)
        {
            writer.WriteStartElement(elementName);
            foreach (var item in enumerable)
            {
                Write(writer, "item", item);
            }

            writer.WriteEndElement();
            return;
        }

        writer.WriteStartElement(elementName);
        var properties = value.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            Write(writer, ToCamelCase(property.Name), propertyValue);
        }

        writer.WriteEndElement();
    }

    /// <summary>Keeps XML element names consistent with the JSON (camelCase) contract.</summary>
    private static string ToCamelCase(string name)
        => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private static string Format(object value)
        => value switch
        {
            bool boolean => XmlConvert.ToString(boolean),
            DateTime dt => dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

    /// <summary>Converts an arbitrary property/key name into a valid XML element name.</summary>
    private static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "item";
        }

        var chars = name.Select(c => XmlConvert.IsXmlChar(c) && (char.IsLetterOrDigit(c) || c is '_' or '-' or '.')
            ? c
            : '_').ToArray();

        var result = new string(chars);
        if (!char.IsLetter(result[0]) && result[0] != '_')
        {
            result = "_" + result;
        }

        return result;
    }
}

using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace PRN232.LMS.API.Formatters;

/// <summary>
/// XML output formatter (Lab 2 — content negotiation).
/// Handles MVC object responses, including field-selection (dictionaries) and expansion.
/// </summary>
public class Prn232XmlOutputFormatter : TextOutputFormatter
{
    // [LAB2-01] Formatter là bộ chuyển object -> biểu diễn HTTP response.
    // SupportedMediaTypes nói formatter này ghi được kiểu XML nào; JSON do formatter khác ghi.
    // MVC chỉ gọi WriteResponseBodyAsync SAU KHI action đã return object và XML được chọn.
    // Không áp dụng cho WriteAsJsonAsync trong middleware/JWT events vì chúng tự ghi response.
    public Prn232XmlOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));
        SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    // Graph writer hỗ trợ object, collection, dictionary: dùng được với fields/expand của bài.
    // CanWriteType tham gia bước chọn formatter, chưa ghi dữ liệu ra client.
    protected override bool CanWriteType(Type? type) => type is not null;

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        // context.Object chính là envelope ApiResponse<T> từ Success/CreatedAtRouteResponse.
        // Indent chỉ làm XML dễ đọc; không đổi cấu trúc dữ liệu.
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = selectedEncoding,
            OmitXmlDeclaration = false
        };

        // Serialize into memory, then write asynchronously to Kestrel. The recursive
        // graph writer uses synchronous XmlWriter methods, which must not write directly
        // to the HTTP response stream (synchronous HTTP I/O is disabled).
        using var buffer = new MemoryStream();
        using (var xmlWriter = XmlWriter.Create(buffer, settings))
        {
            xmlWriter.WriteStartDocument();
            // Ví dụ object { Success = true, Data = ... } -> <response><success>true</success>...
            // XmlGraphWriter viết đệ quy và escape ký tự XML; không nối chuỗi dữ liệu client.
            XmlGraphWriter.Write(xmlWriter, "response", context.Object);
            xmlWriter.WriteEndDocument();
        }
        buffer.Position = 0;
        // Đây là bước gửi byte XML ra response; Content-Type được MVC chọn theo formatter.
        await buffer.CopyToAsync(context.HttpContext.Response.Body, context.HttpContext.RequestAborted);
    }
}

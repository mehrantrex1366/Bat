namespace Bat.AspNetCore;

public static class AspNetCoreExtensions
{
    public async static Task<string> ReadRequestBody(this HttpRequest request)
    {
        // Requires request buffering (BatEnableRequestBufferingMiddleware / EnableBuffering). Without it the body
        // stream is not seekable; return early instead of throwing and catching two exceptions per call.
        if (request?.Body is null || !request.Body.CanSeek || request.ContentLength is null or <= 0)
            return string.Empty;

        try
        {
            request.Body.Position = 0;
            var buffer = new byte[(long)request.ContentLength];
            await request.Body.ReadExactlyAsync(buffer, 0, buffer.Length);
            var body = Encoding.UTF8.GetString(buffer);
            return body;
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    public static void FillWithHttpRequest<TDestination>(this TDestination destinationObject, HttpRequest sourceRequest) where TDestination : class
    {
        var destinationProperties = destinationObject.GetType().GetProperties();
        if (sourceRequest.Method.ToLower() == "post")
        {
            var sourceObject = sourceRequest.Form;
            System.Reflection.PropertyInfo tempProperty;
            foreach (var key in sourceObject)
            {
                tempProperty = destinationProperties.FirstOrDefault(x => x.CanWrite && x.Name == key.Key);
                if (tempProperty != null) SetFromStringValues(tempProperty, destinationObject, key.Value);
            }
        }
        else
        {
            var sourceObject = sourceRequest.Query;
            System.Reflection.PropertyInfo tempProperty;
            foreach (var key in sourceObject)
            {
                tempProperty = destinationProperties.FirstOrDefault(x => x.CanWrite && x.Name == key.Key);
                if (tempProperty != null) SetFromStringValues(tempProperty, destinationObject, key.Value);
            }
        }
    }

    // Fixed: the raw StringValues was assigned to the property (ArgumentException for any non-StringValues
    // property), and the query branch looked values up by key.ToString() ("[key, value]"), which never matched.
    private static void SetFromStringValues(System.Reflection.PropertyInfo property, object destination, Microsoft.Extensions.Primitives.StringValues values)
    {
        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (targetType == typeof(Microsoft.Extensions.Primitives.StringValues))
        {
            property.SetValue(destination, values);
            return;
        }

        var text = values.ToString();
        try
        {
            object value = targetType == typeof(string) ? text
                : targetType.IsEnum ? Enum.Parse(targetType, text, ignoreCase: true)
                : targetType == typeof(Guid) ? Guid.Parse(text)
                : Convert.ChangeType(text, targetType, System.Globalization.CultureInfo.InvariantCulture);
            property.SetValue(destination, value);
        }
        catch
        {
            // Values that cannot be converted are skipped (same as model binding leaving the default).
        }
    }

}
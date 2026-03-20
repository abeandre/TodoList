using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ToDoApi.Filters
{
    /// <summary>
    /// Strips HTML/script from all string properties on the action arguments before the action runs.
    /// Uses HtmlEncoder.Default which encodes characters that would be interpreted as HTML, making
    /// stored XSS impossible even if a client renders the values as raw HTML.
    /// </summary>
    public class SanitizeStringsFilter : IActionFilter
    {
        private const int MaxDepth = 5;
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

        public void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null) continue;
                SanitizeObject(argument, depth: 0);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }

        private static void SanitizeObject(object obj, int depth)
        {
            if (depth >= MaxDepth) return;

            var type = obj.GetType();
            var properties = _propertyCache.GetOrAdd(type, t => t.GetProperties());

            foreach (var prop in properties)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;

                if (prop.PropertyType == typeof(string))
                {
                    var value = (string?)prop.GetValue(obj);
                    if (value is not null)
                        prop.SetValue(obj, HtmlEncoder.Default.Encode(value));
                }
                else if (!prop.PropertyType.IsPrimitive && prop.PropertyType.IsClass)
                {
                    var nested = prop.GetValue(obj);
                    if (nested is not null)
                        SanitizeObject(nested, depth + 1);
                }
            }
        }
    }
}

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
        public void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is null) continue;
                SanitizeObject(argument);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }

        private static void SanitizeObject(object obj)
        {
            var type = obj.GetType();
            foreach (var prop in type.GetProperties())
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
                        SanitizeObject(nested);
                }
            }
        }
    }
}

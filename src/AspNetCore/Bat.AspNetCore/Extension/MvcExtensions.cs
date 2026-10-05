using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Bat.AspNetCore;

public static class MvcExtensions
{
    // Text is the member's [Description], or its name when it has none.
    // A null enum yields an empty list.
    public static List<SelectListItem> ToSelectListFromDescription(this Enum @enum)
    {
        if (@enum is null) return [];

        return Enum.GetValues(@enum.GetType())
            .Cast<Enum>()
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.GetDescription()
            }).ToList();
    }

    public static List<SelectListItem> ToSelectListFromDescriptionAttribute(this Enum @enum)
        => ToSelectListFromDescription(@enum);

    public static List<SelectListItem> ToSelectListItems(this IDictionary<object, object> keyValues)
        => keyValues.Select(x => new SelectListItem
        {
            Value = x.Key.ToString(),
            Text = x.Value.ToString()
        }).ToList();

    public static string GetModelError(this ModelStateDictionary modelState)
        => string.Join("|", modelState.Values.SelectMany(x => x.Errors.Select(e => e.ErrorMessage)).ToList());

}
using System.ComponentModel.DataAnnotations;
using DesktopEye.Common.Resources;

namespace DesktopEye.Common.Enums;

public enum Language
{
    [Display(Name = "English", ResourceType = typeof(Resources_Languages))]
    English,

    [Display(Name = "French", ResourceType = typeof(Resources_Languages))]
    French,

    [Display(Name = "German", ResourceType = typeof(Resources_Languages))]
    German,

    [Display(Name = "Spanish", ResourceType = typeof(Resources_Languages))]
    Spanish,

    [Display(Name = "Unknown", ResourceType = typeof(Resources_Languages))]
    Unknown
}
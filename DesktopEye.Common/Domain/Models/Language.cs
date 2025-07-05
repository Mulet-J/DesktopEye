using System.ComponentModel.DataAnnotations;
using DesktopEye.Common.Resources;

namespace DesktopEye.Common.Domain.Models;

public enum Language
{
    [Display(Name = "English", ResourceType = typeof(ResourcesLanguages))]
    English,

    [Display(Name = "French", ResourceType = typeof(ResourcesLanguages))]
    French,

    [Display(Name = "German", ResourceType = typeof(ResourcesLanguages))]
    German,

    [Display(Name = "Spanish", ResourceType = typeof(ResourcesLanguages))]
    Spanish,

    [Display(Name = "Chinese", ResourceType = typeof(ResourcesLanguages))]
    Chinese,

    [Display(Name = "Japanese", ResourceType = typeof(ResourcesLanguages))]
    Japanese,

    [Display(Name = "Korean", ResourceType = typeof(ResourcesLanguages))]
    Korean,

    [Display(Name = "Portuguese", ResourceType = typeof(ResourcesLanguages))]
    Portuguese,

    [Display(Name = "Italian", ResourceType = typeof(ResourcesLanguages))]
    Italian,

    [Display(Name = "Dutch", ResourceType = typeof(ResourcesLanguages))]
    Dutch,

    [Display(Name = "Russian", ResourceType = typeof(ResourcesLanguages))]
    Russian,

    [Display(Name = "Swedish", ResourceType = typeof(ResourcesLanguages))]
    Swedish,

    [Display(Name = "Norwegian", ResourceType = typeof(ResourcesLanguages))]
    Norwegian,

    [Display(Name = "Danish", ResourceType = typeof(ResourcesLanguages))]
    Danish
}
using HairCarePlus.Client.Patient.Features.Progress.Domain.Entities; // For RestrictionTimer
using HairCarePlus.Client.Patient.Features.Progress.ViewModels; // For ShowMoreRestrictionPlaceholderViewModel
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Features.Progress.Selectors;

public class RestrictionItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate StandardTemplate { get; set; }
    public DataTemplate ShowMoreTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            RestrictionTimer _ => StandardTemplate,
            ShowMoreRestrictionPlaceholderViewModel _ => ShowMoreTemplate,
            _ => null // Default case or throw new ArgumentOutOfRangeException(nameof(item), "Unsupported item type")
        };
    }
} 
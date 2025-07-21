using HairCarePlus.Client.Clinic.Features.Patient.ViewModels;
using HairCarePlus.Client.Clinic.Features.Patient.Models;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Clinic.Features.Patient.Selectors;

public class RestrictionItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? StandardTemplate { get; set; }
    public DataTemplate? ShowMoreTemplate { get; set; }

    protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
    {
        return item switch
        {
            PatientPageViewModel.RestrictionTimer => StandardTemplate,
            ShowMoreRestrictionPlaceholderViewModel => ShowMoreTemplate,
            _ => null
        };
    }
} 
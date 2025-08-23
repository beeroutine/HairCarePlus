using Microsoft.Maui.Controls;
using System.Collections;
using System.Windows.Input;

namespace HairCarePlus.Client.Clinic.Common.Views;

public partial class RestrictionStrip : ContentView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
        nameof(Items), typeof(IEnumerable), typeof(RestrictionStrip));

    // ShowMoreCommand removed to align with Patient app behavior

    public IEnumerable? Items
    {
        get => (IEnumerable?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    // Removed ShowMoreCommand property

    public RestrictionStrip()
    {
        InitializeComponent();
    }
}



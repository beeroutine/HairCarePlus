using Microsoft.Maui.Controls;
using System.Collections;

namespace HairCarePlus.Client.Clinic.Common.Views;

public partial class RestrictionStrip : ContentView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
        nameof(Items), typeof(IEnumerable), typeof(RestrictionStrip));

    public IEnumerable? Items
    {
        get => (IEnumerable?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public RestrictionStrip()
    {
        InitializeComponent();
    }
}



using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HairCarePlus.Client.Patient.Features.Calendar.Models;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Features.Calendar.Converters
{
    public class EventIndicatorsConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime date && parameter is TodayViewModel viewModel)
            {
                // Check if there are any events for this date
                if (!viewModel.HasEvents(date))
                {
                    return null;
                }

                // Create horizontal stacklayout for event indicators
                var indicatorsLayout = new HorizontalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 4
                };

                // Add indicator for each event type if count > 0
                AddIndicatorIfNeeded(indicatorsLayout, viewModel, date, EventType.MedicationTreatment, "#4CAF50");
                AddIndicatorIfNeeded(indicatorsLayout, viewModel, date, EventType.Photo, "#2196F3");
                AddIndicatorIfNeeded(indicatorsLayout, viewModel, date, EventType.Video, "#FF9800");
                AddIndicatorIfNeeded(indicatorsLayout, viewModel, date, EventType.CriticalWarning, "#F44336");

                return indicatorsLayout;
            }

            return null;
        }

        private void AddIndicatorIfNeeded(HorizontalStackLayout layout, TodayViewModel viewModel, DateTime date, EventType eventType, string colorHex)
        {
            int count = viewModel.GetEventCount(date, eventType);
            
            if (count > 0)
            {
                var indicator = new BoxView
                {
                    WidthRequest = 6,
                    HeightRequest = 6,
                    CornerRadius = 3,
                    BackgroundColor = Color.FromArgb(colorHex)
                };
                
                layout.Add(indicator);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
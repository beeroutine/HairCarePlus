using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HairCarePlus.Client.Patient.Features.Calendar.ViewModels;

namespace HairCarePlus.Client.Patient.Features.Calendar.Helpers
{
    public class BoolToCalendarViewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CalendarViewModel.CalendarViewMode mode && parameter is string paramStr)
            {
                switch (paramStr)
                {
                    case "MonthVisible":
                        return mode == CalendarViewModel.CalendarViewMode.Month;
                    
                    case "DayVisible":
                        return mode == CalendarViewModel.CalendarViewMode.Day;
                    
                    case "Month":
                        return mode == CalendarViewModel.CalendarViewMode.Month 
                            ? Application.Current.Resources["PrimaryColor"] 
                            : Colors.Transparent;
                    
                    case "Day":
                        return mode == CalendarViewModel.CalendarViewMode.Day 
                            ? Application.Current.Resources["PrimaryColor"] 
                            : Colors.Transparent;
                    
                    case "MonthText":
                        return mode == CalendarViewModel.CalendarViewMode.Month 
                            ? Colors.White 
                            : Application.Current.Resources["PrimaryColor"];
                    
                    case "DayText":
                        return mode == CalendarViewModel.CalendarViewMode.Day 
                            ? Colors.White 
                            : Application.Current.Resources["PrimaryColor"];
                }
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return CalendarViewModel.CalendarViewMode.Month;
        }
    }
} 
using CommunityToolkit.Mvvm.ComponentModel;

namespace HairCarePlus.Client.Patient.Features.Calendar.ViewModels
{
    // Shared view models to avoid duplicate partial class definitions
    
    public partial class MedicationViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string instructions;

        [ObservableProperty]
        private string dosage;

        [ObservableProperty]
        private int timesPerDay;

        [ObservableProperty]
        private bool isOptional;
    }

    public partial class RestrictionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private string reason;

        [ObservableProperty]
        private bool isCritical;

        [ObservableProperty]
        private string recommendedAlternative;
    }

    public partial class InstructionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;
        
        [ObservableProperty]
        private string description;
        
        [ObservableProperty]
        private string[] steps;
    }
    
    public partial class WarningViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;
        
        [ObservableProperty]
        private string description;
    }

    public partial class CalendarEventViewModel : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string description;

        [ObservableProperty]
        private int dayNumber;
    }
} 
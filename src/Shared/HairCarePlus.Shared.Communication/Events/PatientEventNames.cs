namespace HairCarePlus.Shared.Communication.Events;

public static class PatientEventNames
{
    public const string GroupPrefix = "patient-";
    public static string GroupName(string patientId) => $"{GroupPrefix}{patientId}";

    public const string PhotoReportAdded = nameof(PhotoReportAdded);
    public const string PhotoCommentAdded = nameof(PhotoCommentAdded);
    public const string PhotoReportSetAdded = nameof(PhotoReportSetAdded);
} 
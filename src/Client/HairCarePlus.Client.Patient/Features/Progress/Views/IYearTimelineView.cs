namespace HairCarePlus.Client.Patient.Features.Progress.Views
{
    /// <summary>
    /// Интерфейс для YearTimelineView, позволяющий абстрагировать компонент и упростить тестирование.
    /// </summary>
    public interface IYearTimelineView
    {
        /// <summary>
        /// Дата операции пациента.
        /// </summary>
        DateTime SurgeryDate { get; set; }
    }
} 
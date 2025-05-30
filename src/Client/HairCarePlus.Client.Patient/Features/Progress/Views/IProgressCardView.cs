namespace HairCarePlus.Client.Patient.Features.Progress.Views
{
    /// <summary>
    /// Интерфейс для ProgressCardView, позволяющий абстрагировать компонент и упростить тестирование.
    /// </summary>
    public interface IProgressCardView
    {
        /// <summary>
        /// Устанавливает источник данных для карточки прогресса.
        /// </summary>
        object BindingContext { get; set; }
    }
} 
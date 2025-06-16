using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    public class FadeToAction : TriggerAction<VisualElement>
    {
        public double Opacity { get; set; }

        public uint Length { get; set; } = 250;

        protected override void Invoke(VisualElement sender)
        {
            sender?.FadeTo(Opacity, Length);
        }
    }
} 
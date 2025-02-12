namespace HairCarePlus.Client.Patient.Features.Doctor.Models
{
    public class DoctorProfile
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Specialty { get; set; }
        public required string PhotoUrl { get; set; }
        public bool IsOnline { get; set; }
    }
} 
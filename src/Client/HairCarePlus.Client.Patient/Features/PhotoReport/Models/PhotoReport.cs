using System;

namespace HairCarePlus.Client.Patient.Features.PhotoReport.Models
{
    public class PhotoReport
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string PhotoUrl { get; set; }
        public string Status { get; set; }
        public string Analysis { get; set; }
        public double GrowthProgress { get; set; }
        public string DoctorComment { get; set; }
        public bool IsAnalyzed { get; set; }
    }

    public enum PhotoStatus
    {
        Uploading,
        Analyzing,
        Analyzed,
        Failed
    }
} 
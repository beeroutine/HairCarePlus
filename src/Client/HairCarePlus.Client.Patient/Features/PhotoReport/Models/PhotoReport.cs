using System;

namespace HairCarePlus.Client.Patient.Features.PhotoReport.Models
{
    public class PhotoReport
    {
        public required string Id { get; set; }
        public DateTime Date { get; set; }
        public required string PhotoUrl { get; set; }
        public required string Status { get; set; }
        public required string Analysis { get; set; }
        public double GrowthProgress { get; set; }
        public required string DoctorComment { get; set; }
        public bool IsAnalyzed { get; set; }

        public PhotoReport()
        {
            Id = string.Empty;
            PhotoUrl = string.Empty;
            Status = PhotoStatus.Uploading.ToString();
            Analysis = string.Empty;
            DoctorComment = string.Empty;
            Date = DateTime.Now;
        }
    }

    public enum PhotoStatus
    {
        Uploading,
        Analyzing,
        Analyzed,
        Failed
    }
} 
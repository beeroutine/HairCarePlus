using System;
using System.Collections.Generic;
using HairCarePlus.Server.Domain.Entities;

namespace HairCarePlus.Server.Domain.ValueObjects
{
    public class PhotoReport : BaseEntity
    {
        public string? ImageUrl { get; private set; }
        public string? ImageUploadUrl { get; private set; }
        public string ThumbnailUrl { get; private set; }
        public Guid PatientId { get; private set; }
        public Patient Patient { get; private set; }
        public DateTime CaptureDate { get; private set; }
        public string Notes { get; private set; }
        public PhotoType Type { get; private set; }
        public AnalysisResult AnalysisResult { get; private set; }
        public ICollection<PhotoComment> Comments { get; private set; } = new List<PhotoComment>();

        private PhotoReport() : base() { }

        public PhotoReport(
            Guid id,
            Guid patientId,
            string? imageUrl,
            string? imageUploadUrl,
            string thumbnailUrl,
            DateTime captureDate,
            string notes,
            PhotoType type) : this(imageUrl, imageUploadUrl, thumbnailUrl, patientId, captureDate, notes, type)
        {
            Id = id;
        }

        public PhotoReport(
            string? imageUrl,
            string? imageUploadUrl,
            string thumbnailUrl,
            Guid patientId,
            DateTime captureDate,
            string notes,
            PhotoType type)
        {
            ImageUrl = imageUrl;
            ImageUploadUrl = imageUploadUrl;
            ThumbnailUrl = thumbnailUrl;
            PatientId = patientId;
            CaptureDate = captureDate;
            Notes = notes;
            Type = type;
        }

        public void UpdateAnalysisResult(AnalysisResult result)
        {
            AnalysisResult = result;
            Update();
        }

        public void UpdateNotes(string notes)
        {
            Notes = notes;
            Update();
        }
    }

    public enum PhotoType
    {
        FrontView,
        TopView,
        BackView,
        LeftSideView,
        RightSideView,
        Custom
    }

    public class AnalysisResult
    {
        public double? GrowthPercentage { get; private set; }
        public string? AIAnalysis { get; private set; }
        public DateTime? AnalysisDate { get; private set; }

        private AnalysisResult() { }

        public AnalysisResult(double growthPercentage, string aiAnalysis)
        {
            GrowthPercentage = growthPercentage;
            AIAnalysis = aiAnalysis;
            AnalysisDate = DateTime.UtcNow;
        }
    }
} 
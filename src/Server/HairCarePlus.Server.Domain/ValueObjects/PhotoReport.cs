using System;
using HairCarePlus.Server.Domain.Entities;

namespace HairCarePlus.Server.Domain.ValueObjects
{
    public class PhotoReport : BaseEntity
    {
        public string ImageUrl { get; private set; }
        public string ThumbnailUrl { get; private set; }
        public DateTime CaptureDate { get; private set; }
        public string Notes { get; private set; }
        public PhotoType Type { get; private set; }
        public AnalysisResult AnalysisResult { get; private set; }

        private PhotoReport() : base() { }

        public PhotoReport(
            string imageUrl,
            string thumbnailUrl,
            DateTime captureDate,
            string notes,
            PhotoType type)
        {
            ImageUrl = imageUrl;
            ThumbnailUrl = thumbnailUrl;
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
        public double GrowthPercentage { get; private set; }
        public string AIAnalysis { get; private set; }
        public DateTime AnalysisDate { get; private set; }

        private AnalysisResult() { }

        public AnalysisResult(double growthPercentage, string aiAnalysis)
        {
            GrowthPercentage = growthPercentage;
            AIAnalysis = aiAnalysis;
            AnalysisDate = DateTime.UtcNow;
        }
    }
} 
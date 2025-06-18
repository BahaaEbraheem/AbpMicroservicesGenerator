using System;
using System.Collections.Generic;

namespace AbpMicroservicesGenerator.SolutionGeneration
{
    public class SolutionGenerationResponse
    {
        public Guid Id { get; set; }
        public string SolutionName { get; set; } = string.Empty;
        public GenerationStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> GeneratedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int Progress { get; set; } // 0-100
        public string CurrentStep { get; set; } = string.Empty;
        
        public bool IsCompleted => Status == GenerationStatus.Completed;
        public bool IsFailed => Status == GenerationStatus.Failed;
        public bool IsInProgress => Status == GenerationStatus.InProgress;
        
        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - CreatedAt : null;
    }
}

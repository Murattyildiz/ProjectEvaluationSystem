using System.ComponentModel.DataAnnotations;

namespace ProjectEvaluationSystem.Models
{
    public class ProjectImage
    {
        public int Id { get; set; }
        
        public int ProjectId { get; set; }
        
        [Required]
        public string ImagePath { get; set; } = string.Empty;
        
        public bool IsMainImage { get; set; } = false;
        
        // Navigation property
        public virtual Project? Project { get; set; }
    }
} 
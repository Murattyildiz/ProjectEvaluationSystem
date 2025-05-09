using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectEvaluationSystem.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Proje adı zorunludur.")]
        [Display(Name = "Proje Adı")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Proje açıklaması zorunludur.")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "GitHub Link")]
        public string? GithubLink { get; set; }

        [Display(Name = "Proje Fotoğrafı")]
        public string? ImagePath { get; set; }

        [Display(Name = "Yüklenme Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ortalama Puan")]
        public double AverageRating { get; set; }

        // Proje görselleri (Çoklu)
        public List<ProjectImage> ProjectImages { get; set; } = new List<ProjectImage>();

        // Proje hazırlayanlar
        public List<Contributor> Contributors { get; set; } = new List<Contributor>();

        // Proje yorumları ve değerlendirmeleri
        public List<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    }
} 
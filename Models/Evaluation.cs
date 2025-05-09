using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectEvaluationSystem.Models
{
    public class Evaluation
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad zorunludur.")]
        [Display(Name = "Adınız")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [Display(Name = "Soyadınız")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Puan zorunludur.")]
        [Range(1, 10, ErrorMessage = "Puan 1-10 arasında olmalıdır.")]
        [Display(Name = "Puan")]
        public int Rating { get; set; }

        [Display(Name = "Yorum")]
        public string? Comment { get; set; }

        [Display(Name = "Değerlendirme Tarihi")]
        public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;

        // IP Adresi
        public string IpAddress { get; set; } = string.Empty;
        
        // MAC Adresi
        public string MacAddress { get; set; } = string.Empty;

        // Tarayıcı Bilgisi
        public string UserAgent { get; set; } = string.Empty;

        public int ProjectId { get; set; }
        public Project? Project { get; set; }
    }
} 
using System.ComponentModel.DataAnnotations;

namespace ProjectEvaluationSystem.Models
{
    public class Contributor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad zorunludur.")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad zorunludur.")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        [Display(Name = "Ad Soyad")]
        public string FullName => $"{FirstName} {LastName}";
    }
} 
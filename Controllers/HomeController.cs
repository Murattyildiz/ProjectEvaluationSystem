using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectEvaluationSystem.Data;
using ProjectEvaluationSystem.Models;
using ProjectEvaluationSystem.Services;

namespace ProjectEvaluationSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public HomeController(
            ILogger<HomeController> logger, 
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var projects = _context.Projects
                .Include(p => p.ProjectImages)
                .Include(p => p.Evaluations)
                .ToList();

            // Kullanıcı bilgilerini ViewBag'e ekle - null kontrolü ekleyelim
            ViewBag.UserFirstName = HttpContext.Session.GetString("UserFirstName") ?? string.Empty;
            ViewBag.UserLastName = HttpContext.Session.GetString("UserLastName") ?? string.Empty;
            
            return View(projects);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var project = _context.Projects
                .Include(p => p.ProjectImages)
                .Include(p => p.Contributors)
                .Include(p => p.Evaluations)
                .FirstOrDefault(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // Kullanıcı bilgilerini ViewBag'e ekle - null kontrolü ekleyelim
            ViewBag.UserFirstName = HttpContext.Session.GetString("UserFirstName") ?? string.Empty;
            ViewBag.UserLastName = HttpContext.Session.GetString("UserLastName") ?? string.Empty;
            
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEvaluation(int projectId, Evaluation model)
        {
            _logger.LogInformation($"SubmitEvaluation çağrıldı. ProjectId: {projectId}, Rating: {model.Rating}, FirstName: {model.FirstName}, LastName: {model.LastName}");
            
            if (ModelState.IsValid)
            {
                // Kullanıcı IP adresini al
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogInformation($"Kullanıcı IP adresi: {ipAddress}");
                
                // MAC adresi ve kullanıcı tarayıcı bilgilerini al
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                var macAddress = GetClientMacAddress(ipAddress ?? "127.0.0.1");
                
                _logger.LogInformation($"Kullanıcı MAC adresi: {macAddress}");
                _logger.LogInformation($"Kullanıcı tarayıcı bilgisi: {userAgent}");
                
                // Aynı isim-soyisimle değerlendirme kontrolü
                var existingNameEvaluation = await _context.Evaluations
                    .FirstOrDefaultAsync(e => e.ProjectId == projectId && 
                    e.FirstName.ToLower() == model.FirstName.ToLower() && 
                    e.LastName.ToLower() == model.LastName.ToLower());
                
                if (existingNameEvaluation != null)
                {
                    ModelState.AddModelError(string.Empty, "Bu isim ve soyisimle daha önce bir değerlendirme yapılmış.");
                    TempData["ErrorMessage"] = "Bu isim ve soyisimle daha önce bir değerlendirme yapılmış.";
                    
                    var currentProject = await _context.Projects
                        .Include(p => p.ProjectImages)
                        .Include(p => p.Contributors)
                        .Include(p => p.Evaluations)
                        .FirstOrDefaultAsync(p => p.Id == projectId);
                    return View("Details", currentProject);
                }
                
                // Daha önce bu IP veya MAC adresinden bu projeye değerlendirme yapılmış mı kontrol et
                var existingEvaluation = await _context.Evaluations
                    .FirstOrDefaultAsync(e => e.ProjectId == projectId && 
                    (e.IpAddress == ipAddress || e.MacAddress == macAddress));
                
                if (existingEvaluation != null)
                {
                    _logger.LogWarning($"Bu kişi ({ipAddress}, {macAddress}) daha önce bu projeyi ({projectId}) değerlendirmiş.");
                    ModelState.AddModelError(string.Empty, "Bu projeyi daha önce değerlendirdiniz.");
                    TempData["ErrorMessage"] = "Bu projeyi daha önce değerlendirdiniz.";
                    
                    var currentProject = await _context.Projects
                        .Include(p => p.ProjectImages)
                        .Include(p => p.Contributors)
                        .Include(p => p.Evaluations)
                        .FirstOrDefaultAsync(p => p.Id == projectId);
                    return View("Details", currentProject);
                }
                
                // Yeni değerlendirmeyi ekle
                model.ProjectId = projectId;
                model.IpAddress = ipAddress ?? "Unknown";
                model.MacAddress = macAddress;
                model.UserAgent = userAgent;
                model.EvaluationDate = DateTime.UtcNow;
                
                _logger.LogInformation($"Yeni değerlendirme ekleniyor: ProjectId={model.ProjectId}, Rating={model.Rating}, MAC={model.MacAddress}");
                
                try 
                {
                    _context.Evaluations.Add(model);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Değerlendirme başarıyla kaydedildi. EvaluationId: {model.Id}");
                    
                    // Projenin ortalama puanını güncelle
                    var updatedProject = await _context.Projects
                        .Include(p => p.Evaluations)
                        .FirstOrDefaultAsync(p => p.Id == projectId);
                    
                    if (updatedProject != null && updatedProject.Evaluations.Any())
                    {
                        updatedProject.AverageRating = updatedProject.Evaluations.Average(e => e.Rating);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Proje ortalama puanı güncellendi: {updatedProject.AverageRating}");
                    }
                    
                    // Proje katkıda bulunanlarına anonim değerlendirme bildirimi gönder
                    try
                    {
                        await _emailService.SendAnonymousEvaluationNotificationAsync(model.Id);
                    }
                    catch (Exception ex)
                    {
                        // E-posta gönderimi başarısız olsa bile işleme devam et
                        _logger.LogError($"Değerlendirme bildirimi gönderilemedi: {ex.Message}");
                    }
                    
                    TempData["SuccessMessage"] = "Değerlendirmeniz başarıyla kaydedildi.";
                    return RedirectToAction(nameof(Details), new { id = projectId });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Değerlendirme kaydedilirken hata oluştu: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "Değerlendirme kaydedilirken bir hata oluştu.");
                    TempData["ErrorMessage"] = "Değerlendirme kaydedilirken bir hata oluştu.";
                }
            }
            else
            {
                _logger.LogWarning("Model doğrulaması başarısız: " + 
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                TempData["ErrorMessage"] = "Lütfen gerekli alanları doldurun.";
            }
            
            // Form geçerli değilse
            var projectWithEvaluations = await _context.Projects
                .Include(p => p.ProjectImages)
                .Include(p => p.Contributors)
                .Include(p => p.Evaluations)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            
            return View("Details", projectWithEvaluations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUserInfo(string FirstName, string LastName)
        {
            _logger.LogInformation($"SaveUserInfo çağrıldı: {FirstName} {LastName}");
            
            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
            {
                // Bu isim-soyisim daha önce değerlendirme yapmış mı kontrol et
                var existingEvaluation = await _context.Evaluations
                    .FirstOrDefaultAsync(e => 
                        e.FirstName.ToLower() == FirstName.ToLower() && 
                        e.LastName.ToLower() == LastName.ToLower());
                
                if (existingEvaluation != null)
                {
                    _logger.LogWarning($"Bu isim-soyisim zaten kullanılmış: {FirstName} {LastName}");
                    TempData["ErrorMessage"] = "Bu isim ve soyisim kombinasyonu daha önce kullanılmış. Lütfen farklı bir isim ve soyisim girin.";
                    return RedirectToAction(nameof(Index));
                }
                
                // İsim-soyisim kullanılmamışsa kaydet
                HttpContext.Session.SetString("UserFirstName", FirstName);
                HttpContext.Session.SetString("UserLastName", LastName);
                TempData["SuccessMessage"] = "Bilgileriniz kaydedildi. Şimdi projeleri değerlendirebilirsiniz.";
            }
            else
            {
                TempData["ErrorMessage"] = "Lütfen adınızı ve soyadınızı girin.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearUserInfo()
        {
            HttpContext.Session.Remove("UserFirstName");
            HttpContext.Session.Remove("UserLastName");
            
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // MAC adresi alma yöntemi
        private string GetClientMacAddress(string ipAddress)
        {
            try
            {
                // Gerçek uygulama ortamında bu metot geliştirilmelidir
                // Not: Tarayıcı API'lerinin güvenlik sebepleriyle MAC adresine doğrudan erişimi kısıtlıdır
                // Bu yüzden gerçek bir MAC adresi elde etmek modern tarayıcılarda mümkün değildir
                
                // Örnek olarak benzersiz bir tanımlayıcı olarak kullanılabilecek bir değer üretiyoruz
                string fingerprint = $"{ipAddress}_{HttpContext.Request.Headers["User-Agent"]}";
                
                // Parmak izinden bir hash oluşturma
                using var sha = System.Security.Cryptography.SHA256.Create();
                var bytes = System.Text.Encoding.UTF8.GetBytes(fingerprint);
                var hash = sha.ComputeHash(bytes);
                
                // Hash'i bir MAC benzeri formata dönüştürme
                string macLike = string.Join(":", hash.Take(6).Select(b => b.ToString("X2")));
                
                return macLike;
            }
            catch (Exception ex)
            {
                _logger.LogError($"MAC adresi alırken hata: {ex.Message}");
                return "00:00:00:00:00:00"; // Hata durumunda varsayılan değer
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ProjectEvaluationSystem.Data;
using ProjectEvaluationSystem.Models;
using ProjectEvaluationSystem.Services;
using System.Collections.Generic;

namespace ProjectEvaluationSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(
            ApplicationDbContext context, 
            IEmailService emailService,
            ICloudinaryService cloudinaryService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _emailService = emailService;
            _cloudinaryService = cloudinaryService;
            _webHostEnvironment = webHostEnvironment;
        }

        // Admin giriş sayfası
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("AdminId") != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        // Admin giriş işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Kullanıcı adı ve şifre gereklidir.";
                return View();
            }

            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password && u.IsAdmin);

            if (admin != null)
            {
                HttpContext.Session.SetString("AdminId", admin.Id.ToString());
                TempData["SuccessMessage"] = "Giriş başarılı. Hoş geldiniz!";
                return RedirectToAction("Index");
            }

            ViewBag.ErrorMessage = "Kullanıcı adı veya şifre hatalı.";
            return View();
        }

        // Admin çıkış işlemi
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Admin sayfası - Dashboard
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            // İstatistikleri hesapla
            var projects = await _context.Projects
                .Include(p => p.ProjectImages)
                .Include(p => p.Evaluations)
                .Include(p => p.Contributors)
                .ToListAsync();

            ViewBag.TotalEvaluations = await _context.Evaluations.CountAsync();
            ViewBag.TotalContributors = await _context.Contributors.CountAsync();

            // Her projenin değerlendirme ve katkıda bulunan sayılarını hesapla
            var projectEvaluations = await _context.Projects
                .Include(p => p.Evaluations)
                .ToDictionaryAsync(p => p.Id, p => p.Evaluations.Count);

            var projectContributors = await _context.Projects
                .Include(p => p.Contributors)
                .ToDictionaryAsync(p => p.Id, p => p.Contributors.Count);

            ViewBag.ProjectEvaluations = projectEvaluations;
            ViewBag.ProjectContributors = projectContributors;

            return View(projects);
        }

        // Tüm projeler sayfası
        public async Task<IActionResult> Projects(string searchTerm, string sortOrder, int page = 1)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            const int pageSize = 10;
            
            var query = _context.Projects
                .Include(p => p.ProjectImages)
                .Include(p => p.Evaluations)
                .Include(p => p.Contributors)
                .AsQueryable();

            // Arama filtresi
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            // Sıralama
            switch (sortOrder)
            {
                case "name_desc":
                    query = query.OrderByDescending(p => p.Name);
                    break;
                case "date":
                    query = query.OrderBy(p => p.CreatedDate);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(p => p.CreatedDate);
                    break;
                case "rating":
                    query = query.OrderBy(p => p.AverageRating);
                    break;
                case "rating_desc":
                    query = query.OrderByDescending(p => p.AverageRating);
                    break;
                default:
                    query = query.OrderBy(p => p.Name);
                    break;
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var projects = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Her projenin değerlendirme ve katkıda bulunan sayılarını hesapla
            var projectEvaluations = await _context.Projects
                .Include(p => p.Evaluations)
                .ToDictionaryAsync(p => p.Id, p => p.Evaluations.Count);

            var projectContributors = await _context.Projects
                .Include(p => p.Contributors)
                .ToDictionaryAsync(p => p.Id, p => p.Contributors.Count);

            ViewBag.ProjectEvaluations = projectEvaluations;
            ViewBag.ProjectContributors = projectContributors;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortOrder = sortOrder;

            return View(projects);
        }

        // Katkıda bulunanlar sayfası
        public async Task<IActionResult> Contributors(int? projectId, int page = 1)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            const int pageSize = 10;
            
            var query = _context.Contributors
                .Include(c => c.Project)
                .AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(c => c.ProjectId == projectId);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var contributors = await query
                .OrderBy(c => c.Project.Name)
                .ThenBy(c => c.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.ProjectId = projectId;
            ViewBag.Projects = await _context.Projects.OrderBy(p => p.Name).ToListAsync();

            return View(contributors);
        }

        // Yeni proje ekleme sayfası
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // Yeni proje ekleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project, List<IFormFile> projectImages, string[] contributorFirstName, string[] contributorLastName, string[] contributorEmail)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                // Önce projeyi ekle
                project.CreatedDate = DateTime.UtcNow;
                _context.Add(project);
                await _context.SaveChangesAsync();

                // Proje fotoğraflarını yükle
                if (projectImages != null && projectImages.Count > 0)
                {
                    bool isFirstImage = true;
                    foreach (var imageFile in projectImages)
                    {
                        if (imageFile != null && imageFile.Length > 0)
                        {
                            try
                            {
                                string imagePath = await _cloudinaryService.UploadImageAsync(imageFile);

                                // Yeni proje görseli oluştur
                                var projectImage = new ProjectImage
                                {
                                    ProjectId = project.Id,
                                    ImagePath = imagePath,
                                    IsMainImage = isFirstImage // İlk görsel ana görsel olsun
                                };

                                _context.ProjectImages.Add(projectImage);

                                // İlk görsel ise ana görsel olarak projeye de ekle
                                if (isFirstImage)
                                {
                                    project.ImagePath = imagePath;
                                    isFirstImage = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("", $"Resim yükleme işlemi başarısız: {ex.Message}");
                                // Hataya rağmen devam et
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // Katkıda bulunanları ekle
                if (contributorFirstName != null && contributorFirstName.Length > 0)
                {
                    for (int i = 0; i < contributorFirstName.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(contributorFirstName[i]) && !string.IsNullOrEmpty(contributorLastName[i]))
                        {
                            var contributor = new Contributor
                            {
                                FirstName = contributorFirstName[i],
                                LastName = contributorLastName[i],
                                Email = i < contributorEmail.Length ? contributorEmail[i] : string.Empty,
                                ProjectId = project.Id
                            };

                            _context.Contributors.Add(contributor);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Proje başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // Proje düzenleme sayfası
        public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            var project = await _context.Projects
                .Include(p => p.Contributors)
                .Include(p => p.ProjectImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // Proje düzenleme işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project, List<IFormFile> projectImages, int[] imageIdsToDelete, string[] contributorIds, string[] contributorFirstName, string[] contributorLastName, string[] contributorEmail)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProject = await _context.Projects
                        .Include(p => p.Contributors)
                        .Include(p => p.ProjectImages)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (existingProject == null)
                    {
                        return NotFound();
                    }

                    // Update basic project properties
                    existingProject.Name = project.Name;
                    existingProject.Description = project.Description;
                    existingProject.GithubLink = project.GithubLink;

                    // Handle image deletions if any
                    if (imageIdsToDelete != null && imageIdsToDelete.Length > 0)
                    {
                        foreach (var imageId in imageIdsToDelete)
                        {
                            var imageToDelete = existingProject.ProjectImages.FirstOrDefault(i => i.Id == imageId);
                            if (imageToDelete != null)
                            {
                                _context.ProjectImages.Remove(imageToDelete);
                            }
                        }
                    }

                    // Upload new images if any
                    if (projectImages != null && projectImages.Count > 0)
                    {
                        bool hasMainImage = existingProject.ProjectImages.Any(p => p.IsMainImage);
                        
                        foreach (var imageFile in projectImages)
                        {
                            if (imageFile != null && imageFile.Length > 0)
                            {
                                try
                                {
                                    string imagePath = await _cloudinaryService.UploadImageAsync(imageFile);

                                    // Create new project image
                                    var newImage = new ProjectImage
                                    {
                                        ProjectId = existingProject.Id,
                                        ImagePath = imagePath,
                                        IsMainImage = !hasMainImage // If no main image exists, make this the main image
                                    };

                                    _context.ProjectImages.Add(newImage);

                                    // If this is set as the main image, update the project's ImagePath
                                    if (!hasMainImage)
                                    {
                                        existingProject.ImagePath = imagePath;
                                        hasMainImage = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ModelState.AddModelError("", $"Resim yükleme işlemi başarısız: {ex.Message}");
                                    // Continue despite error
                                }
                            }
                        }
                    }

                    // Ensure project still has a main image
                    if (!existingProject.ProjectImages.Any(p => p.IsMainImage) && existingProject.ProjectImages.Any())
                    {
                        var firstImage = existingProject.ProjectImages.First();
                        firstImage.IsMainImage = true;
                        existingProject.ImagePath = firstImage.ImagePath;
                    }

                    // Handle contributors
                    // 1. Remove existing contributors not in the updated list
                    var contributorIdsToKeep = new List<int>();
                    if (contributorIds != null)
                    {
                        foreach (var idStr in contributorIds)
                        {
                            if (int.TryParse(idStr, out int contributorId))
                            {
                                contributorIdsToKeep.Add(contributorId);
                            }
                        }
                    }

                    var contributorsToRemove = existingProject.Contributors
                        .Where(c => !contributorIdsToKeep.Contains(c.Id))
                        .ToList();

                    foreach (var contributor in contributorsToRemove)
                    {
                        _context.Contributors.Remove(contributor);
                    }

                    // 2. Update existing contributors and add new ones
                    if (contributorFirstName != null && contributorFirstName.Length > 0)
                    {
                        for (int i = 0; i < contributorFirstName.Length; i++)
                        {
                            if (!string.IsNullOrEmpty(contributorFirstName[i]) && !string.IsNullOrEmpty(contributorLastName[i]))
                            {
                                int? contributorId = null;
                                if (contributorIds != null && i < contributorIds.Length && !string.IsNullOrEmpty(contributorIds[i]))
                                {
                                    int.TryParse(contributorIds[i], out int parsedId);
                                    contributorId = parsedId;
                                }

                                if (contributorId.HasValue && contributorId.Value > 0)
                                {
                                    // Update existing contributor
                                    var existingContributor = await _context.Contributors.FindAsync(contributorId.Value);
                                    if (existingContributor != null)
                                    {
                                        existingContributor.FirstName = contributorFirstName[i];
                                        existingContributor.LastName = contributorLastName[i];
                                        existingContributor.Email = i < contributorEmail?.Length ? contributorEmail[i] : existingContributor.Email;
                                    }
                                }
                                else
                                {
                                    // Add new contributor
                                    var newContributor = new Contributor
                                    {
                                        FirstName = contributorFirstName[i],
                                        LastName = contributorLastName[i],
                                        Email = i < contributorEmail?.Length ? contributorEmail[i] : string.Empty,
                                        ProjectId = existingProject.Id
                                    };
                                    _context.Contributors.Add(newContributor);
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Proje başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(project);
        }

        // Proje silme onay sayfası
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // Proje silme işlemi
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            var project = await _context.Projects
                .Include(p => p.Contributors)
                .Include(p => p.Evaluations)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // Proje fotoğrafını sil
            if (!string.IsNullOrEmpty(project.ImagePath))
            {
                await _cloudinaryService.DeleteImageAsync(project.ImagePath);
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Proje başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        // Değerlendirme özetlerini e-posta ile gönder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEvaluationSummary(int id)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            await _emailService.SendProjectEvaluationSummaryAsync(id);

            TempData["SuccessMessage"] = "Değerlendirme özeti e-posta ile gönderildi.";
            return RedirectToAction(nameof(Index));
        }

        // Değerlendirmeleri görüntüleme sayfası
        public async Task<IActionResult> ViewEvaluations(int? projectId, int page = 1)
        {
            if (HttpContext.Session.GetString("AdminId") == null)
            {
                return RedirectToAction("Login");
            }

            var query = _context.Evaluations
                .Include(e => e.Project)
                .AsQueryable();

            // Proje ID'sine göre filtreleme
            if (projectId.HasValue)
            {
                query = query.Where(e => e.ProjectId == projectId.Value);
            }

            // Varsayılan sıralama - en yeni değerlendirmeler önce
            query = query.OrderByDescending(e => e.EvaluationDate);

            // Tüm değerlendirmeleri al (sayfalama View'da yapılacak)
            var evaluations = await query.ToListAsync();
            
            // Dropdown için proje listesi
            ViewBag.Projects = await _context.Projects
                .OrderBy(p => p.Name)
                .ToListAsync();
            
            ViewBag.SelectedProjectId = projectId;
            ViewBag.CurrentPage = page;

            // İstatistikler ekle
            if (evaluations.Any())
            {
                ViewBag.AverageRating = evaluations.Average(e => e.Rating);
                ViewBag.TotalCount = evaluations.Count;
                
                if (projectId.HasValue)
                {
                    var project = await _context.Projects.FindAsync(projectId.Value);
                    ViewBag.ProjectName = project?.Name;
                }
            }

            return View(evaluations);
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(p => p.Id == id);
        }
    }
} 
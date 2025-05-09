using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using ProjectEvaluationSystem.Data;
using ProjectEvaluationSystem.Models;

namespace ProjectEvaluationSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public EmailService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage)
        {
            var email = new MimeMessage();
            
            email.From.Add(MailboxAddress.Parse(_configuration["Mail:Username"] ?? ""));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            
            var builder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };
            
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _configuration["Mail:Host"] ?? "smtp.gmail.com", 
                int.Parse(_configuration["Mail:Port"] ?? "587"), 
                SecureSocketOptions.StartTls);
            
            await smtp.AuthenticateAsync(
                _configuration["Mail:Username"] ?? "", 
                _configuration["Mail:Password"] ?? "");
            
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendAnonymousEvaluationNotificationAsync(int evaluationId)
        {
            // Değerlendirmeyi, projeyi ve katkıda bulunanları al
            var evaluation = await _context.Evaluations
                .Include(e => e.Project)
                    .ThenInclude(p => p.Contributors)
                .FirstOrDefaultAsync(e => e.Id == evaluationId);

            if (evaluation == null || evaluation.Project == null || !evaluation.Project.Contributors.Any())
                return;

            var project = evaluation.Project;

            // E-posta içeriği hazırla
            var subject = $"Yeni Anonim Değerlendirme: {project.Name}";
            var htmlContent = $@"
                <h2>Projenize Yeni Değerlendirme</h2>
                <h3>{project.Name}</h3>
                <hr>
                <p><strong>Puan:</strong> {evaluation.Rating}/10</p>
                
                <h4>Değerlendirme Yorumu:</h4>
                <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #007bff; margin: 10px 0;'>
                    {(string.IsNullOrEmpty(evaluation.Comment) ? "<em>Yorum yapılmamış</em>" : evaluation.Comment)}
                </div>
                
                <p><strong>Değerlendirme Tarihi:</strong> {evaluation.EvaluationDate:dd/MM/yyyy HH:mm}</p>
                <p><strong>Güncel Ortalama Puan:</strong> {project.AverageRating:F1}/10</p>
                <p><strong>Toplam Değerlendirme Sayısı:</strong> {(project.Evaluations != null ? project.Evaluations.Count : 0)}</p>
                
                <p style='font-style: italic; color: #666;'>
                    Not: Bu bir anonim değerlendirmedir. Değerlendiren kişinin kimlik bilgileri gizli tutulmaktadır.
                    Bu e-posta, Proje Değerlendirme Sistemi tarafından otomatik olarak gönderilmiştir.
                </p>";

            // Her bir katkıda bulunan kişiye e-posta gönder
            foreach (var contributor in project.Contributors)
            {
                if (!string.IsNullOrEmpty(contributor.Email))
                {
                    try {
                        await SendEmailAsync(contributor.Email, subject, htmlContent);
                    }
                    catch (Exception ex) {
                        // Log the error but continue with other emails
                        Console.WriteLine($"Failed to send email to {contributor.Email}: {ex.Message}");
                    }
                }
            }
        }

        public async Task SendProjectEvaluationSummaryAsync(int projectId)
        {
            // Projeyi, katkıda bulunanları ve değerlendirmeleri al
            var project = await _context.Projects
                .Include(p => p.Contributors)
                .Include(p => p.Evaluations)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null || !project.Contributors.Any())
                return;

            // Değerlendirme özeti oluştur
            var averageRating = project.Evaluations?.Any() == true 
                ? project.Evaluations.Average(e => e.Rating) 
                : 0;
            
            var comments = project.Evaluations?
                .Where(e => !string.IsNullOrEmpty(e.Comment))
                .Select(e => e.Comment ?? string.Empty)
                .ToList() ?? new List<string>();

            // E-posta içeriği hazırla
            var subject = $"Proje Değerlendirme Özeti: {project.Name}";
            var htmlContent = $@"
                <h2>Proje Değerlendirme Özeti</h2>
                <h3>{project.Name}</h3>
                <p><strong>Ortalama Puan:</strong> {averageRating:F1}/5</p>
                <p><strong>Toplam Değerlendirme:</strong> {project.Evaluations?.Count ?? 0}</p>
                
                <h4>Değerlendirme Yorumları</h4>
                <ul>";

            if (comments.Any())
            {
                foreach (var comment in comments)
                {
                    htmlContent += $"<li>{comment}</li>";
                }
            }
            else
            {
                htmlContent += "<li>Henüz yorum yapılmamış.</li>";
            }

            htmlContent += @"</ul>
                <p>Bu e-posta, Proje Değerlendirme Sistemi tarafından otomatik olarak gönderilmiştir.</p>";

            // Her bir katkıda bulunan kişiye e-posta gönder
            foreach (var contributor in project.Contributors)
            {
                if (!string.IsNullOrEmpty(contributor.Email))
                {
                    try {
                        await SendEmailAsync(contributor.Email, subject, htmlContent);
                    }
                    catch (Exception ex) {
                        // Log the error but continue with other emails
                        Console.WriteLine($"Failed to send email to {contributor.Email}: {ex.Message}");
                    }
                }
            }
        }
    }
} 
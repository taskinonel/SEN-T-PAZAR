using Microsoft.AspNetCore.Mvc;
using SEN_T_PAZAR.Models;
using System.Linq;

namespace SEN_T_PAZAR.Controllers
{
    public class ProjectController : Controller
    {
        public IActionResult ProjectDetails(int id)
        {
            // Örnek veri, gerçek uygulamada veritabanından alınmalı
            var project = FakeProjects().FirstOrDefault(p => p.Id == id);
            if (project == null)
                return NotFound();
            return View(project);
        }

        // Sadece demo amaçlı örnek veri
        private static List<ProjectCard> FakeProjects()
        {
            return new List<ProjectCard>
            {
                new ProjectCard { Id = 1, Name = "Karpaz", ImageUrl = "/img/karpaz.jpg", Location = "Karpaz", Company = "Firma A", DeliveryDate = "2026", PriceFrom = "1.000.000 TL", Description = "Karpaz projesi detay açıklaması." },
                new ProjectCard { Id = 2, Name = "Girne", ImageUrl = "/img/girne.jpg", Location = "Girne", Company = "Firma B", DeliveryDate = "2027", PriceFrom = "2.000.000 TL", Description = "Girne projesi detay açıklaması." }
            };
        }
    }
}

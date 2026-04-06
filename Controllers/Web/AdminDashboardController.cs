using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UngDungOnThiBangLai.Models;

namespace UngDungOnThiBangLai.Controllers.Web
{
    // [Authorize(Roles = "Admin")] // Mở comment này sau khi bạn cài đặt Cookie Authentication
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _context;

        public AdminDashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy các số liệu thống kê cơ bản để hiển thị lên Dashboard
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalQuestions = await _context.Questions.CountAsync();
            ViewBag.TotalCategories = await _context.LicenseCategories.CountAsync();

            return View();
        }
    }
}
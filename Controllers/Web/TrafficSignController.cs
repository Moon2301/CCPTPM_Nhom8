using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UngDungOnThiBangLai.Models;

namespace UngDungOnThiBangLai.Controllers.Web
{
    public class TrafficSignController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TrafficSignController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. DANH SÁCH (Gộp 2 hàm Index cũ để tránh lỗi Ambiguous)
        public async Task<IActionResult> Index(string category, string searchString)
        {
            var query = _context.TrafficSigns.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(s => s.Category == category);

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(s => s.Name.Contains(searchString) || s.Code.Contains(searchString));

            ViewBag.Categories = new List<string> { "Biển cấm", "Biển báo nguy hiểm", "Biển hiệu lệnh", "Biển chỉ dẫn", "Biển phụ", "Vạch kẻ đường" };

            return View(await query.ToListAsync());
        }

        [HttpGet]
        public async Task<JsonResult> GetAllSigns(string search)
        {
            var query = _context.TrafficSigns.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.Name.Contains(search));
            }

            var result = await query.Select(s => new {
                id = s.Id,
                name = s.Name,
                imageUrl = s.ImageUrl
            }).ToListAsync();

            return Json(result);
        }

        // 2. THÊM MỚI
        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TrafficSignViewModel model)
        {
            if (ModelState.IsValid)
            {
                string imageUrl = await SaveImage(model.ImageFile) ?? "";

                var sign = new TrafficSign
                {
                    Code = model.Code,
                    Name = model.Name,
                    Category = model.Category,
                    Description = model.Description,
                    ImageUrl = imageUrl
                };

                _context.TrafficSigns.Add(sign);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // 3. SỬA (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var sign = await _context.TrafficSigns.FindAsync(id);
            if (sign == null) return NotFound();

            var model = new TrafficSignViewModel
            {
                Id = sign.Id,
                Code = sign.Code,
                Name = sign.Name,
                Category = sign.Category,
                Description = sign.Description,
                ExistingImageUrl = sign.ImageUrl
            };
            return View(model);
        }

        // 4. SỬA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TrafficSignViewModel model)
        {
            var sign = await _context.TrafficSigns.FindAsync(id);
            if (sign == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Nếu có upload ảnh mới
                if (model.ImageFile != null)
                {
                    // Xóa ảnh cũ
                    DeleteImage(sign.ImageUrl);
                    // Lưu ảnh mới
                    sign.ImageUrl = await SaveImage(model.ImageFile) ?? sign.ImageUrl;
                }

                sign.Code = model.Code;
                sign.Name = model.Name;
                sign.Category = model.Category;
                sign.Description = model.Description;

                _context.Update(sign);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            model.ExistingImageUrl = sign.ImageUrl;
            return View(model);
        }

        // 5. XÓA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var sign = await _context.TrafficSigns.FindAsync(id);
            if (sign == null) return NotFound();

            // Xóa file vật lý
            DeleteImage(sign.ImageUrl);

            _context.TrafficSigns.Remove(sign);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // --- HÀM HELPER XỬ LÝ ẢNH ---
        private async Task<string?> SaveImage(IFormFile? file)
        {
            if (file == null) return null;
            string folder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "trafficsigns");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/images/trafficsigns/" + fileName;
        }

        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            var path = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}
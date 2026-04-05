using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UngDungOnThiBangLai.Models;

namespace UngDungOnThiBangLai.Controllers.Web
{
    public class LicenseCategoryController : Controller
    {
        private readonly AppDbContext _context;

        public LicenseCategoryController(AppDbContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH
        public async Task<IActionResult> Index()
        {
            var categories = await _context.LicenseCategories
                .Include(c => c.QuestionTopics) 
                .ToListAsync();

            return View(categories);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LicenseCategory model)
        {
            // Bỏ qua validate các navigation properties để tránh lỗi ModelState.IsValid = false
            ModelState.Remove("QuestionTopics");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.LicenseCategories.Add(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không thể lưu dữ liệu: " + ex.Message);
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.LicenseCategories
                .Include(c => c.QuestionTopics)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LicenseCategory model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Load Hạng bằng kèm danh sách Topic hiện tại (EF sẽ track những cái này)
                    var dbCategory = await _context.LicenseCategories
                        .Include(c => c.QuestionTopics)
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (dbCategory == null) return NotFound();

                    // 2. Cập nhật thông tin cơ bản của LicenseCategory
                    _context.Entry(dbCategory).CurrentValues.SetValues(model);

                    // 3. Xử lý danh sách QuestionTopics từ Form gửi về
                    if (model.QuestionTopics != null)
                    {
                        foreach (var topicFromForm in model.QuestionTopics)
                        {
                            topicFromForm.LicenseCategoryId = id;
                            // Tìm Topic tương ứng đã được nạp từ DB
                            var existingTopic = dbCategory.QuestionTopics
                                .FirstOrDefault(t => t.Id == topicFromForm.Id);

                            if (existingTopic != null)
                            {
                                // NẾU ĐÃ CÓ: Cập nhật giá trị
                                _context.Entry(existingTopic).CurrentValues.SetValues(topicFromForm);
                            }
                            else
                            {
                                // NẾU LÀ MỚI (Id = 0): Thêm mới
                                topicFromForm.LicenseCategoryId = id;
                                dbCategory.QuestionTopics.Add(topicFromForm);
                            }
                        }

                        // 4. Xóa những Topic không còn nằm trong Form gửi về
                        var formIds = model.QuestionTopics.Select(t => t.Id).ToList();
                        var topicsToRemove = dbCategory.QuestionTopics
                            .Where(t => t.Id != 0 && !formIds.Contains(t.Id)).ToList();

                        foreach (var toRemove in topicsToRemove)
                        {
                            // Kiểm tra xem Topic này có đang được map với Câu hỏi nào trong bảng trung gian không
                            bool hasQuestions = await _context.Set<QuestionTopicQuestion>()
                                                              .AnyAsync(qtq => qtq.QuestionTopicId == toRemove.Id);

                            if (!hasQuestions)
                            {
                                _context.QuestionTopics.Remove(toRemove);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }
            return View(model);
        }

        // 6. XÓA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.LicenseCategories.FindAsync(id);
            if (category == null) return NotFound();

            // Nhờ cấu hình OnDelete(Cascade) ở DbContext, EF sẽ tự động xóa các Topic thuộc Hạng bằng này
            _context.LicenseCategories.Remove(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
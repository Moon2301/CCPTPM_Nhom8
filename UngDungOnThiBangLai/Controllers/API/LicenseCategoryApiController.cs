using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UngDungOnThiBangLai.Models;

namespace UngDungOnThiBangLai.Controllers.API
{
    [ApiController]
    [Route("api/LicenseCategory")]
    public class LicenseCategoryApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LicenseCategoryApiController(AppDbContext context)
        {
            _context = context;
        }

        // URL sẽ là: GET https://localhost:<port>/api/LicenseCategory/GetAvailable
        [HttpGet("GetAvailable")]
        public async Task<IActionResult> GetAvailableCategories()
        {
            try
            {
                // Dùng .Select() để tạo Data Transfer Object (DTO) ẩn danh (Anonymous Object)
                // Điều này giúp loại bỏ các Navigation Properties rườm rà (như QuestionTopics)
                // giúp file JSON trả về cực kỳ nhẹ và tăng tốc độ tải của React App.
                var categories = await _context.LicenseCategories
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        description = c.Description,
                        totalQuestions = c.TotalQuestions,
                        timeLimit = c.TimeLimit,
                        minimumPassScore = c.MinimumPassScore
                    })
                    .ToListAsync();

                if (!categories.Any())
                {
                    return NotFound(new { message = "Chưa có dữ liệu hạng bằng nào trong hệ thống." });
                }

                return Ok(categories);
            }
            catch (Exception ex)
            {
                // Trả về mã 500 kèm thông báo JSON chuẩn mực khi có lỗi DB
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ", details = ex.Message });
            }
        }
    }
}

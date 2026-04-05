using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UngDungOnThiBangLai.Models;

namespace UngDungOnThiBangLai.Controllers.API
{
    [ApiController]
    [Route("api/TrafficSign")]
    public class TrafficSignApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrafficSignApiController(AppDbContext context)
        {
            _context = context;
        }

        // 1. API Lấy danh mục biển báo (Tạo danh sách tĩnh)
        [HttpGet("GetCategories")]
        public IActionResult GetCategories()
        {
            try
            {
                var categories = new[]
                {
                    // NOTE: `id` phải khớp với giá trị `TrafficSign.Category` đang lưu trong DB
                    // (UI quản trị đang dùng các option: "Biển cấm", "Biển báo nguy hiểm", ...)
                    new { id = "Biển cấm", name = "Biển báo cấm", description = "Biểu thị các điều cấm người tham gia giao thông không được vi phạm.", color = "from-red-500 to-rose-600" },
                    new { id = "Biển báo nguy hiểm", name = "Biển báo nguy hiểm", description = "Báo trước các tình huống nguy hiểm có thể xảy ra trên đường.", color = "from-yellow-400 to-orange-500" },
                    new { id = "Biển hiệu lệnh", name = "Biển hiệu lệnh", description = "Báo các hiệu lệnh mà người tham gia giao thông bắt buộc phải thi hành.", color = "from-blue-500 to-cyan-600" },
                    new { id = "Biển chỉ dẫn", name = "Biển chỉ dẫn", description = "Chỉ dẫn hướng đi hoặc các điều cần biết cho người tham gia giao thông.", color = "from-blue-600 to-indigo-600" },
                    new { id = "Biển phụ", name = "Biển phụ", description = "Thuyết minh, bổ sung chi tiết ý nghĩa cho các biển báo chính.", color = "from-gray-500 to-slate-600" },
                    new { id = "Vạch kẻ đường", name = "Vạch kẻ đường", description = "Hướng dẫn, điều khiển giao thông nhằm nâng cao an toàn.", color = "from-yellow-600 to-amber-700" }
                };

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tải danh mục.", details = ex.Message });
            }
        }

        // 2. API Lấy danh sách biển báo từ DB
        [HttpGet("GetSigns")]
        public async Task<IActionResult> GetSigns()
        {
            try
            {
                // Dùng .Select() để map dữ liệu chuẩn form mà Frontend đang chờ
                var signs = await _context.TrafficSigns
                    .Select(s => new
                    {
                        id = s.Code,           // Dùng Code (VD: P.102) làm ID hiển thị ở Frontend
                        name = s.Name,         // Tên biển báo
                        meaning = s.Description, // Ý nghĩa
                        categoryId = s.Category, // Gắn với id của Categories tĩnh ở trên (VD: "Biển cấm")
                        imageUrl = s.ImageUrl  // Link ảnh gốc 
                    })
                    .ToListAsync();

                return Ok(signs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tải biển báo.", details = ex.Message });
            }
        }
    }
}

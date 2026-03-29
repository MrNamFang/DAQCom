using System;
using System.Linq;
using System.Web.Mvc;
using DAQCom.Models; // Đảm bảo đúng namespace model của bạn

namespace DAQCom.Controllers
{
    public class DatBanController : Controller
    {
        private QLQuanComEntities db = new QLQuanComEntities();

        // GET: Hiển thị form đặt bàn
        [HttpGet]
        public ActionResult Index()
        {
            // Tạo đối tượng DatBan để truyền sang View (để binding dữ liệu)
            DatBan model = new DatBan();

            // Nếu khách đã đăng nhập, tự động điền thông tin
            if (Session["KhachHang"] != null)
            {
                var kh = Session["KhachHang"] as DAQCom.Models.KhachHang;
                model.HoTen = kh.TenKH;
                model.SDT = kh.SDT;
                model.MaKH = kh.MaKH;
            }
            return View(model);
        }

        // POST: Xử lý khi khách nhấn nút "Đặt bàn ngay"
        [HttpPost]
        public ActionResult Index(DatBan model, FormCollection f)
        {
            // Kiểm tra dữ liệu nhập vào
            if (ModelState.IsValid)
            {
                // 1. Xử lý ngày giờ đến (Ghép ngày + giờ thành DateTime hoàn chỉnh)
                string strNgay = f["NgayDenInput"]; // Lấy ngày từ input type="date"
                string strGio = f["GioDenInput"];   // Lấy giờ từ input type="time"

                if (!string.IsNullOrEmpty(strNgay) && !string.IsNullOrEmpty(strGio))
                {
                    DateTime ngay = DateTime.Parse(strNgay);
                    TimeSpan gio = TimeSpan.Parse(strGio);
                    model.NgayDen = ngay + gio; // Cộng lại ra ngày giờ chính xác
                }
                else
                {
                    // Nếu thiếu ngày giờ thì báo lỗi và trả về view
                    ModelState.AddModelError("", "Vui lòng chọn đầy đủ ngày và giờ đến!");
                    return View(model);
                }

                // 2. Gán các thông tin hệ thống tự tạo
                model.NgayDat = DateTime.Now;   // Thời điểm bấm nút đặt
                model.TrangThai = 0;            // 0: Mới đặt (Chờ xác nhận)

                // 3. Nếu khách đã đăng nhập mà form chưa có MaKH, gán lại cho chắc
                if (Session["KhachHang"] != null)
                {
                    var kh = Session["KhachHang"] as DAQCom.Models.KhachHang;
                    model.MaKH = kh.MaKH;
                }

                // 4. Lưu vào Database
                db.DatBans.Add(model);
                db.SaveChanges();

                // 5. Chuyển hướng sang trang thông báo thành công
                return RedirectToAction("ThongBaoThanhCong");
            }

            return View(model);
        }

        public ActionResult ThongBaoThanhCong()
        {
            return View();
        }
    }
}
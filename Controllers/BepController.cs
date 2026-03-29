using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DAQCom.Models;
using System.Data.Entity;

namespace DAQCom.Controllers
{
    public class BepController : Controller
    {
        private QLQuanComEntities db = new QLQuanComEntities();

        // 1. HIỂN THỊ DANH SÁCH CẦN LÀM
        public ActionResult Index()
        {
            // Làm mới bộ nhớ đệm để tránh việc đơn mới đặt nhưng Bếp không thấy
            db = new QLQuanComEntities();

            // Lấy danh sách món đang chờ (0) hoặc đang nấu (1)
            // Lọc thêm hd.TrangThai == 0 để chắc chắn đơn chưa bị hủy/thanh toán
            var rawItems = db.ChiTietHoaDons
                             .Include(c => c.HoaDon)
                             .Include(c => c.MonAn)
                             .Where(ct => ct.TrangThaiMon == 0 || ct.TrangThaiMon == 1)
                             .ToList();

            // Nhóm theo Hóa Đơn để hiển thị dạng Ticket
            var listTickets = rawItems
                .GroupBy(x => x.MaHD)
                .Select(g =>
                {
                    var maHD = g.Key;
                    var firstItem = g.First();
                    var hd = firstItem.HoaDon;
                    string tenHienThi = "";

                    // Kiểm tra bàn ăn (Dùng MaHD_HienTai)
                    var ban = db.BanAns.AsNoTracking().FirstOrDefault(b => b.MaHD_HienTai == maHD);

                    if (ban != null)
                    {
                        tenHienThi = "Bàn " + ban.TenBan;
                    }
                    else
                    {
                        // Xử lý tên cho đơn Online/Mang về
                        string tenKhach = "Khách lẻ";
                        if (!string.IsNullOrEmpty(hd.TenNguoiNhan))
                        {
                            tenKhach = hd.TenNguoiNhan;
                        }
                        else if (hd.KhachHang != null)
                        {
                            tenKhach = hd.KhachHang.TenKH;
                        }
                        tenHienThi = $"Online: {tenKhach}";
                    }

                    return new BepTicketDTO
                    {
                        MaHD = maHD,
                        TenBan = tenHienThi,
                        GioGoi = hd.NgayDat.HasValue ? hd.NgayDat.Value.ToString("HH:mm") : "--:--",
                        ListMon = g.Select(i => new BepItemDTO
                        {
                            MaCT = i.MaCT,
                            TenMon = i.MonAn.TenMon,
                            SoLuong = i.SoLuong ?? 1,
                            GhiChu = "",
                            TrangThai = i.TrangThaiMon ?? 0
                        }).ToList()
                    };
                })
                .OrderBy(t => t.MaHD) // Đơn cũ làm trước, đơn mới làm sau
                .ToList();

            return View(listTickets);
        }

        // 2. AJAX: KIỂM TRA CÓ ĐƠN MỚI KHÔNG (Dùng để thông báo chuông)
        [HttpGet]
        public JsonResult KiemTraDonMoi()
        {
            // Tránh dùng cache
            using (var context = new QLQuanComEntities())
            {
                int count = context.ChiTietHoaDons.Count(x => x.TrangThaiMon == 0);
                return Json(new { SoLuong = count }, JsonRequestBehavior.AllowGet);
            }
        }

        // 3. AJAX: HOÀN THÀNH 1 MÓN
        [HttpPost]
        public JsonResult HoanThanhMon(int maCT)
        {
            var item = db.ChiTietHoaDons.Find(maCT);
            if (item != null)
            {
                item.TrangThaiMon = 2; // Giả sử 2 là trạng thái 'Đã nấu xong'
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy chi tiết món ăn." });
        }

        [HttpPost]
        public JsonResult HoanThanhTatCaMon(int maHD)
        {
            var listMon = db.ChiTietHoaDons.Where(ct => ct.MaHD == maHD && ct.TrangThaiMon < 2).ToList();
            if (listMon.Any())
            {
                foreach (var item in listMon)
                {
                    item.TrangThaiMon = 2;
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Đơn hàng không có món nào cần xử lý." });
        }
        // 4. AJAX: HOÀN THÀNH CẢ ĐƠN (Xong hết các món trong 1 bàn/hóa đơn)
        [HttpPost]
        public ActionResult HoanThanhCaBan(int maHD)
        {
            var listMon = db.ChiTietHoaDons
                            .Where(c => c.MaHD == maHD && (c.TrangThaiMon == null || c.TrangThaiMon < 2))
                            .ToList();

            foreach (var item in listMon)
            {
                item.TrangThaiMon = 2;
            }

            var hd = db.HoaDons.Find(maHD);
            if (hd != null)
            {
                hd.TrangThai = 1; // Chờ thanh toán

                var ban = db.BanAns.FirstOrDefault(b => b.MaHD_HienTai == maHD);
                if (ban != null)
                {
                    ban.TrangThai = 1; // Bàn đang chờ khách thanh toán
                }
            }

            try
            {
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message });
            }
        }
    }
}
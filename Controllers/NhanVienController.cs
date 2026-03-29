using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DAQCom.Models;
using System.Data.Entity;

namespace DAQCom.Controllers
{
    public class NhanVienController : Controller
    {
        private QLQuanComEntities db = new QLQuanComEntities();

        // 0. Hàm bổ trợ: Tìm hóa đơn hiện tại của bàn
        private HoaDon TimHoaDonHienTai(int idBan)
        {
            var ban = db.BanAns.Find(idBan);
            if (ban != null && ban.MaHD_HienTai != null)
            {
                return db.HoaDons
                         .Include(h => h.ChiTietHoaDons.Select(ct => ct.MonAn))
                         .FirstOrDefault(h => h.MaHD == ban.MaHD_HienTai && h.TrangThai < 2);
            }
            return null;
        }

        // 1. DANH SÁCH BÀN (Trang chính của nhân viên)
        public ActionResult Index()
        {
            var danhSachBan = db.BanAns.ToList();
            var danhSachDTO = new List<BanAnDTO>();
            foreach (var b in danhSachBan)
            {
                var hd = db.HoaDons.FirstOrDefault(h => h.MaHD == b.MaHD_HienTai && h.TrangThai < 2);
                danhSachDTO.Add(new BanAnDTO
                {
                    MaBan = b.MaBan,
                    TenBan = b.TenBan,
                    TrangThai = b.TrangThai ?? 0,
                    GioVao = (hd != null && hd.NgayDat.HasValue) ? hd.NgayDat.Value.ToString("HH:mm") : "",
                    TamTinh = hd != null ? (hd.TongTien ?? 0) : 0
                });
            }
            return View(danhSachDTO);
        }

        // 2. TRANG GỌI MÓN
        public ActionResult GoiMon(int? idBan)
        {
            if (idBan == null) return RedirectToAction("Index");
            var ban = db.BanAns.Find(idBan);
            if (ban == null) return HttpNotFound();

            ViewBag.BanAn = ban;
            ViewBag.HoaDon = TimHoaDonHienTai(idBan.Value);
            ViewBag.AllMonAn = db.MonAns.ToList();
            ViewBag.LoaiMon = db.MonAns.Select(m => m.LoaiMon).Distinct().Where(l => l != null).ToList();
            return View();
        }

        // 3. THÊM MÓN (AJAX)
        [HttpPost]
        public ActionResult ThemMonVaoBan(int idBan, int maMon, int soLuong, string ghiChu)
        {
            try
            {
                var monAn = db.MonAns.Find(maMon);
                if (monAn == null) return Json(new { success = false, msg = "Không tìm thấy món ăn" });

                var hoaDon = TimHoaDonHienTai(idBan);

                if (hoaDon == null)
                {
                    var banAn = db.BanAns.Find(idBan);
                    hoaDon = new HoaDon
                    {
                        MaKH = null,
                        NgayDat = DateTime.Now,
                        TrangThai = 0,
                        TongTien = 0,
                        TenNguoiNhan = banAn != null ? banAn.TenBan : "Bàn " + idBan
                    };
                    db.HoaDons.Add(hoaDon);
                    db.SaveChanges();

                    if (banAn != null)
                    {
                        banAn.TrangThai = 1;
                        banAn.MaHD_HienTai = hoaDon.MaHD;
                    }
                    db.SaveChanges();
                }

                var chiTiet = db.ChiTietHoaDons.FirstOrDefault(ct => ct.MaHD == hoaDon.MaHD && ct.MaMon == maMon && ct.TrangThaiMon == -1);
                if (chiTiet != null)
                {
                    chiTiet.SoLuong += soLuong;
                    if (!string.IsNullOrEmpty(ghiChu))
                        chiTiet.GhiChu = string.IsNullOrEmpty(chiTiet.GhiChu) ? ghiChu : chiTiet.GhiChu + ", " + ghiChu;
                }
                else
                {
                    db.ChiTietHoaDons.Add(new ChiTietHoaDon
                    {
                        MaHD = hoaDon.MaHD,
                        MaMon = maMon,
                        SoLuong = soLuong,
                        DonGia = monAn.Gia,
                        GhiChu = ghiChu ?? "",
                        TrangThaiMon = -1
                    });
                }
                db.SaveChanges();

                var listChiTiet = db.ChiTietHoaDons.Where(ct => ct.MaHD == hoaDon.MaHD).ToList();
                hoaDon.TongTien = listChiTiet.Sum(ct => (ct.DonGia ?? 0) * (ct.SoLuong ?? 0));
                db.SaveChanges();

                return PartialView("_BillPartial", hoaDon);
            }
            catch (Exception ex) { return Json(new { success = false, msg = ex.Message }); }
        }

        // 4. GỬI BẾP
        [HttpPost]
        public JsonResult GuiBepVaInPhieu(int idBan)
        {
            var hoaDon = TimHoaDonHienTai(idBan);
            if (hoaDon != null)
            {
                var dsMon = db.ChiTietHoaDons.Where(ct => ct.MaHD == hoaDon.MaHD && ct.TrangThaiMon == -1).ToList();
                foreach (var item in dsMon) item.TrangThaiMon = 0;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // 5. YÊU CẦU THANH TOÁN (Từ máy tính bảng của khách hoặc nhân viên phục vụ)
        [HttpPost]
        public JsonResult YeuCauThanhToan(int idBan)
        {
            var hd = TimHoaDonHienTai(idBan);
            if (hd != null)
            {
                hd.TrangThai = 1; // Chuyển sang trạng thái "Chờ thanh toán" để hiện bên Thu Ngân
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // 6. TRANG THU NGÂN
        public ActionResult ThuNgan()
        {
            // --- PHẦN MỚI: TÍNH TOÁN DOANH THU (ĐÃ SỬA ĐỂ NHẬN TARGET) ---
            var today = DateTime.Now.Date;
            double doanhThuNgay = (double)(db.HoaDons
                .Where(h => DbFunctions.TruncateTime(h.NgayDat) == today && h.TrangThai == 2)
                .Sum(h => h.TongTien) ?? 0);

            int currentMonth = DateTime.Now.Month;
            int currentYear = DateTime.Now.Year;
            double doanhThuThang = (double)(db.HoaDons
                .Where(h => h.NgayDat.Value.Month == currentMonth && h.NgayDat.Value.Year == currentYear && h.TrangThai == 2)
                .Sum(h => h.TongTien) ?? 0);

            // CHỖ CẦN THAY ĐỔI: Kiểm tra Session trước khi dùng số mặc định
            // Thay vì targetThang = 50000000;
            var config = db.Database.SqlQuery<double>("SELECT GiaTri FROM CauHinhHeThong WHERE MaThamSo = 'TARGET_THANG'").FirstOrDefault();
            double targetThang = config > 0 ? config : 50000000;
            if (Session["TargetThang"] != null)
            {
                targetThang = Convert.ToDouble(Session["TargetThang"]);
            }

            ViewBag.DoanhThuNgay = doanhThuNgay;
            ViewBag.DoanhThuThang = doanhThuThang;
            ViewBag.TargetThang = targetThang;
            // ------------------------------------

            // Giữ nguyên logic lấy danh sách bàn chờ của bạn
            var dsBanRaw = db.BanAns
                .Where(b => b.MaHD_HienTai != null &&
                       db.HoaDons.Any(h => h.MaHD == b.MaHD_HienTai && h.TrangThai == 1))
                .ToList();

            var dsBanCho = dsBanRaw.Select(b => {
                var hd = db.HoaDons.FirstOrDefault(h => h.MaHD == b.MaHD_HienTai);
                decimal tongTien = 0;
                if (hd != null)
                {
                    tongTien = db.ChiTietHoaDons
                                .Where(ct => ct.MaHD == hd.MaHD)
                                .AsEnumerable()
                                .Sum(ct => (ct.SoLuong ?? 0) * (ct.DonGia ?? 0));
                }

                return new DAQCom.Models.BanAnDTO
                {
                    MaBan = b.MaBan,
                    TenBan = b.TenBan,
                    TrangThai = b.TrangThai ?? 0,
                    GioVao = hd?.NgayDat != null ? hd.NgayDat.Value.ToString("HH:mm") : "--:--",
                    TamTinh = tongTien
                };
            }).ToList();

            var maHDDangNgoiBan = db.BanAns.Where(b => b.MaHD_HienTai != null).Select(b => b.MaHD_HienTai).ToList();

            ViewBag.ListOnline = db.HoaDons
                .Include(h => h.KhachHang)
                .Where(h => h.TrangThai < 2 && !maHDDangNgoiBan.Contains(h.MaHD))
                .OrderByDescending(h => h.NgayDat)
                .ToList();

            return View(dsBanCho);
        }

        // 7. XEM CHI TIẾT THANH TOÁN (Cho Modal của Bàn)
        public ActionResult XemChiTietThanhToan(int idBan)
        {
            var hd = TimHoaDonHienTai(idBan);
            if (hd == null) return Content("<div class='alert alert-danger'>Không tìm thấy hóa đơn!</div>");
            return PartialView("_HoaDonPartial", hd);
        }

        // 8. XEM HÓA ĐƠN (Cho Modal của đơn Online)
        public ActionResult XemHoaDon(int? maHD)
        {
            if (maHD == null) return Content("Không tìm thấy hóa đơn");
            var hd = db.HoaDons.Include(h => h.ChiTietHoaDons.Select(ct => ct.MonAn)).FirstOrDefault(h => h.MaHD == maHD);
            return PartialView("_BillPartial", hd);
        }

        // 9. XÁC NHẬN THANH TOÁN (Kết thúc hóa đơn tại bàn)
        [HttpPost]
        public JsonResult XacNhanThanhToan(int maHD, string hinhThuc)
        {
            try
            {
                var hd = db.HoaDons.Find(maHD);
                if (hd == null) return Json(new { success = false, msg = "Hóa đơn không tồn tại" });

                // 1. Cập nhật trạng thái Hóa đơn thành 2 (Hoàn thành)
                hd.TrangThai = 2;

                // 2. Tạo bản ghi Thanh toán mới (Khớp đúng tên cột trong Model của bạn)
                var tt = new ThanhToan
                {
                    MaHD = maHD,
                    PhuongThuc = hinhThuc, // Khớp với cột 'PhuongThuc' trong Model
                    SoTien = hd.TongTien ?? 0,
                    NgayThanhToan = DateTime.Now
                    // Bỏ cột TrangThai vì bảng ThanhToan của bạn không có cột này
                };
                db.ThanhToans.Add(tt);

                // 3. Giải phóng bàn ăn liên quan
                var ban = db.BanAns.FirstOrDefault(b => b.MaHD_HienTai == maHD);
                if (ban != null)
                {
                    ban.TrangThai = 0; // Trạng thái Trống
                    ban.MaHD_HienTai = null;
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // 10. XÁC NHẬN GIAO HÀNG (Cho đơn Online)
        [HttpPost]
        public JsonResult XacNhanGiaoHang(int maHD)
        {
            try
            {
                var hd = db.HoaDons.Find(maHD);
                if (hd != null)
                {
                    hd.TrangThai = 2; // Hoàn thành
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, msg = "Không tìm thấy mã hóa đơn." });
            }
            catch (Exception ex) { return Json(new { success = false, msg = ex.Message }); }
        }
        // 11. Xử lý xong TỪNG MÓN
        [HttpPost]
        public JsonResult HoanThanhMon(int maCT) // Đổi từ XongMon thành HoanThanhMon
        {
            try
            {
                var ct = db.ChiTietHoaDons.Find(maCT);
                if (ct != null)
                {
                    ct.TrangThaiMon = 2; // Đã xong
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy chi tiết món" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // 12. Xử lý xong TOÀN BỘ HÓA ĐƠN
        [HttpPost]
        public JsonResult HoanThanhTatCaMon(int maHD) // Đổi từ XongHoaDon thành HoanThanhTatCaMon
        {
            try
            {
                var dsMon = db.ChiTietHoaDons.Where(ct => ct.MaHD == maHD).ToList();
                foreach (var item in dsMon)
                {
                    item.TrangThaiMon = 2;
                }

                var hd = db.HoaDons.Find(maHD);
                if (hd != null)
                {
                    hd.TrangThai = 1; // Chuyển sang trạng thái "Chờ thanh toán" để hiện bên Thu Ngân
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // 13. Kiểm tra đơn mới (Cho hàm checkDonHang chạy ngầm 5s/lần)
        [HttpGet]
        public JsonResult KiemTraDonMoi()
        {
            // Đếm số lượng món ăn đang chờ (TrangThaiMon == 0)
            var count = db.ChiTietHoaDons.Count(ct => ct.TrangThaiMon == 0);
            return Json(new { SoLuong = count }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult XacNhanThanhToanByBan(int idBan)
        {
            try
            {
                var ban = db.BanAns.Find(idBan);
                if (ban == null || ban.MaHD_HienTai == null)
                    return Json(new { success = false, msg = "Bàn không có hóa đơn" });

                int maHD = ban.MaHD_HienTai.Value;
                var hd = db.HoaDons.Find(maHD);

                if (hd != null)
                {
                    hd.TrangThai = 2; // Hoàn thành
                    ban.TrangThai = 0; // Trống
                    ban.MaHD_HienTai = null;
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, msg = "Lỗi dữ liệu" });
            }
            catch (Exception ex) { return Json(new { success = false, msg = ex.Message }); }
        }

        [HttpPost]
        public JsonResult CapNhatTarget(double target)
        {
            try
            {
                // Chạy lệnh update trực tiếp vào SQL
                db.Database.ExecuteSqlCommand("UPDATE CauHinhHeThong SET GiaTri = @p0 WHERE MaThamSo = 'TARGET_THANG'", target);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message });
            }
        }
    }
}
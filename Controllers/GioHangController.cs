using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DAQCom.Models;

namespace DAQCom.Controllers
{
    public class GioHangController : Controller
    {
        // Sử dụng QLQuanComEntities như cũ
        private QLQuanComEntities db = new QLQuanComEntities();

        // 1. Lấy giỏ hàng từ Session
        public List<GioHang> LayGioHang()
        {
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang == null)
            {
                lstGioHang = new List<GioHang>();
                Session["GioHang"] = lstGioHang;
            }
            return lstGioHang;
        }

        // --- AJAX: Thêm món vào giỏ ---
        [HttpPost]
        public ActionResult ThemGioHang(int maMon)
        {
            // Lấy giỏ hàng hiện tại
            List<GioHang> lstGioHang = LayGioHang();

            // Kiểm tra món đã có trong giỏ chưa
            GioHang sanPham = lstGioHang.FirstOrDefault(n => n.iMaMon == maMon);

            if (sanPham == null)
            {
                // Tạo mới món trong giỏ (Đảm bảo Class GioHang của bạn có Constructor nhận maMon)
                sanPham = new GioHang(maMon);
                lstGioHang.Add(sanPham);
            }
            else
            {
                sanPham.iSoLuong++;
            }

            // Tính tổng số lượng để cập nhật giao diện
            int tongSL = lstGioHang.Sum(x => x.iSoLuong);

            // Trả về kết quả cho Ajax
            return Json(new { success = true, tongSL = tongSL });
        }

        // 2. CẬP NHẬT SỐ LƯỢNG MÓN ĂN
        [HttpPost]
        public ActionResult CapNhatGioHang(int maMon, FormCollection f)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sanPham = lstGioHang.SingleOrDefault(n => n.iMaMon == maMon);

            if (sanPham != null)
            {
                var soLuongValue = f.GetValue("txtSoLuong");

                if (soLuongValue != null && int.TryParse(soLuongValue.AttemptedValue, out int sl) && sl > 0)
                {
                    sanPham.iSoLuong = sl;
                    TempData["SuccessMessage"] = $"Đã cập nhật số lượng món **{sanPham.sTenMon}** thành {sl}.";
                }
                else
                {
                    lstGioHang.RemoveAll(n => n.iMaMon == maMon);
                    TempData["SuccessMessage"] = $"Đã xóa món **{sanPham.sTenMon}** khỏi giỏ hàng.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy món ăn trong giỏ hàng để cập nhật.";
            }

            if (lstGioHang.Count == 0)
            {
                Session["GioHang"] = null;
            }

            return RedirectToAction("GioHang");
        }

        // 3. XÓA TỪNG MÓN ĂN
        public ActionResult XoaGioHang(int maMon)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sanPham = lstGioHang.SingleOrDefault(n => n.iMaMon == maMon);

            if (sanPham != null)
            {
                string tenMon = sanPham.sTenMon;
                lstGioHang.RemoveAll(n => n.iMaMon == maMon);
                TempData["SuccessMessage"] = $"Đã xóa món **{tenMon}** khỏi giỏ hàng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy món ăn trong giỏ hàng để xóa.";
            }

            if (lstGioHang.Count == 0)
            {
                Session["GioHang"] = null;
            }

            return RedirectToAction("GioHang");
        }

        // 4. XÓA TẤT CẢ
        public ActionResult XoaTatCaGioHang()
        {
            List<GioHang> lstGioHang = LayGioHang();
            if (lstGioHang.Count > 0)
            {
                lstGioHang.Clear();
                Session["GioHang"] = null;
                TempData["SuccessMessage"] = "Đã xóa toàn bộ giỏ hàng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Giỏ hàng đã trống.";
            }
            return RedirectToAction("GioHang");
        }

        // 5. HIỂN THỊ GIỎ HÀNG
        public ActionResult GioHang()
        {
            List<GioHang> lstGioHang = LayGioHang();

            if (lstGioHang.Count == 0)
            {
                ViewBag.TongSoLuong = 0;
                ViewBag.TongTien = 0.0;
                return View(lstGioHang);
            }

            ViewBag.TongSoLuong = lstGioHang.Sum(n => n.iSoLuong);
            ViewBag.TongTien = lstGioHang.Sum(n => n.dThanhTien);
            return View(lstGioHang);
        }

        // 6. ĐẶT HÀNG (GET)
        [HttpGet]
        public ActionResult DatHang()
        {
            List<GioHang> lstGioHang = LayGioHang();

            if (lstGioHang == null || lstGioHang.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống, vui lòng thêm món ăn.";
                return RedirectToAction("DatMon", "QuanAn");
            }

            ViewBag.TongSoLuong = lstGioHang.Sum(n => n.iSoLuong);
            ViewBag.TongTien = lstGioHang.Sum(n => n.dThanhTien);

            if (Session["UserKhachHang"] != null)
            {
                var kh = Session["UserKhachHang"] as DAQCom.Models.KhachHang;
                if (kh != null)
                {
                    ViewBag.TenNguoiNhan = kh.TenKH;
                    ViewBag.SDTNguoiNhan = kh.SDT;
                    ViewBag.DiaChiNguoiNhan = kh.DiaChi;
                }
            }
            return View(lstGioHang);
        }

        // 7. ĐẶT HÀNG (POST) - Đã sửa lỗi MaBan
        [HttpPost]
        public ActionResult DatHang(FormCollection f)
        {
            List<GioHang> lst = LayGioHang();

            if (lst == null || lst.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống, vui lòng thêm món ăn.";
                return RedirectToAction("DatMon", "QuanAn");
            }

            // 1. Tạo Hóa Đơn Mới
            HoaDon hd = new HoaDon();
            hd.NgayDat = DateTime.Now;
            hd.TongTien = (decimal)lst.Sum(n => n.dThanhTien);

            // Lưu thông tin người nhận
            hd.TenNguoiNhan = f["TenNguoiNhan"];
            hd.SDTNguoiNhan = f["SDTNguoiNhan"];
            hd.DiaChiNguoiNhan = f["DiaChiNguoiNhan"];

            // TrangThai = 0: Mới đặt (Bếp sẽ thấy đơn này)
            hd.TrangThai = 0;

            if (Session["UserKhachHang"] != null)
            {
                var kh = Session["UserKhachHang"] as DAQCom.Models.KhachHang;
                if (kh != null) hd.MaKH = kh.MaKH;
            }

            db.HoaDons.Add(hd);
            db.SaveChanges(); // Lưu để sinh MaHD

            // 2. Lưu Chi Tiết Hóa Đơn
            foreach (var item in lst)
            {
                ChiTietHoaDon ct = new ChiTietHoaDon();
                ct.MaHD = hd.MaHD;
                ct.MaMon = item.iMaMon;
                ct.SoLuong = item.iSoLuong;
                ct.DonGia = (decimal)item.dDonGia;

                // QUAN TRỌNG: Gán trạng thái món để Bếp lọc được
                ct.TrangThaiMon = 0;

                db.ChiTietHoaDons.Add(ct);
            }
            db.SaveChanges();

            // 3. Xóa giỏ hàng
            Session["GioHang"] = null;
            TempData["SuccessMessage"] = $"Đơn hàng #{hd.MaHD} đã đặt thành công! Bếp đang chuẩn bị món.";

            return RedirectToAction("XacNhanDonHang");
        }
        public ActionResult XacNhanDonHang()
        {
            return View();
        }
    }
}
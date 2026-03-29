using System;
using System.Linq;
using System.Web.Mvc;
using DAQCom.Models;
using System.Data.Entity;

namespace DAQCom.Areas.Admin.Controllers
{
    public class HomeAdminController : BaseController
    {
        private QLQuanComEntities db = new QLQuanComEntities();

        // GET: Admin/HomeAdmin
        public ActionResult Index()
        {
            // --- 1. THỐNG KÊ SỐ LƯỢNG MÓN ---
            ViewBag.SoLuongMonMan = db.MonAns.Count(n => n.LoaiMon == "Món mặn");
            ViewBag.SoLuongMonChay = db.MonAns.Count(n => n.LoaiMon == "Món chay");
            ViewBag.SoLuongMonCanh = db.MonAns.Count(n => n.LoaiMon == "Món canh");
            ViewBag.SoLuongMonAnKem = db.MonAns.Count(n => n.LoaiMon == "Món ăn kèm");
            ViewBag.SoLuongTrangMieng = db.MonAns.Count(n => n.LoaiMon == "Tráng miệng");
            ViewBag.SoLuongNuocUong = db.MonAns.Count(n => n.LoaiMon == "Nước uống");

            // --- 2. THỐNG KÊ DOANH THU & ĐƠN HÀNG ---
            try
            {
                DateTime today = DateTime.Now.Date;

                // A. Tổng đơn hàng
                ViewBag.TongDonHang = db.HoaDons.Count();

                // B. Khách đặt bàn (TrangThai == 0: Chờ xác nhận)
                ViewBag.SoLuongDatBanCho = db.DatBans.Count(n => n.TrangThai == 0);

                // C. Doanh thu hôm nay
                var doanhThu = db.HoaDons
                    .Where(h => h.NgayDat >= today && h.TrangThai == 3) // 3 = Đã thanh toán
                    .Sum(h => (decimal?)h.TongTien);

                ViewBag.DoanhThuNgay = doanhThu ?? 0;

                // D. Đơn đang nấu / Chưa xử lý (TrangThai == 0 hoặc 1)
                ViewBag.DonDangNau = db.HoaDons.Count(h => h.TrangThai == 0 || h.TrangThai == 1);
            }
            catch (Exception ex)
            {
                ViewBag.TongDonHang = 0;
                ViewBag.SoLuongDatBanCho = 0;
                ViewBag.DoanhThuNgay = 0;
                ViewBag.DonDangNau = 0;
                System.Diagnostics.Debug.WriteLine("Lỗi Dashboard: " + ex.Message);
            }

            return View();
        }

    }
}
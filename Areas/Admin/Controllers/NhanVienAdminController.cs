using System;
using System.Linq;
using System.Web.Mvc;
using DAQCom.Models; // Đảm bảo đúng namespace của Models
using System.Collections.Generic;

namespace DAQCom.Areas.Admin.Controllers
{
    public class NhanVienAdminController : BaseController // Kế thừa BaseController để kiểm tra đăng nhập Admin
    {
        private QLQuanComEntities db = new QLQuanComEntities();

        // HIỂN THỊ DANH SÁCH NHÂN VIÊN CHỜ DUYỆT (TrangThai = 0)
        public ActionResult NhanVienChoDuyet()
        {
            // Lấy danh sách nhân viên có TrangThai = 0 (chờ duyệt)
            var lstNhanVien = db.NhanViens
                                .Where(nv => nv.TrangThai == 0)
                                .OrderByDescending(nv => nv.NgayTao)
                                .ToList();

            // Lưu số lượng để hiển thị trên Dashboard hoặc Menu
            ViewBag.CountChoDuyet = lstNhanVien.Count;
            ViewBag.CurrentTab = "ChoDuyet";
            return View(lstNhanVien);
        }

        // HIỂN THỊ DANH SÁCH NHÂN VIÊN ĐÃ DUYỆT (TrangThai = 1)
        public ActionResult NhanVienDaDuyet()
        {
            // Lấy danh sách nhân viên có TrangThai = 1 (đã duyệt)
            var lstNhanVien = db.NhanViens
                                .Where(nv => nv.TrangThai == 1)
                                .OrderBy(nv => nv.HoTen)
                                .ToList();

            ViewBag.CurrentTab = "DaDuyet";
            return View("NhanVienChoDuyet", lstNhanVien); // Tái sử dụng View NhanVienChoDuyet
        }

        // PHÊ DUYỆT TÀI KHOẢN (POST)
        [HttpPost]
        public ActionResult PheDuyet(int maNV)
        {
            var nv = db.NhanViens.SingleOrDefault(n => n.MaNV == maNV);

            if (nv != null)
            {
                // Cập nhật trạng thái từ 0 (Chờ duyệt) sang 1 (Đã duyệt)
                nv.TrangThai = 1;
                db.SaveChanges();
                TempData["SuccessMessage"] = $"Đã phê duyệt thành công tài khoản **{nv.HoTen}**.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên cần phê duyệt.";
            }

            // Quay lại trang danh sách chờ duyệt
            return RedirectToAction("NhanVienChoDuyet");
        }

        // TỪ CHỐI/XÓA TÀI KHOẢN (POST)
        [HttpPost]
        public ActionResult TuChoi(int maNV)
        {
            var nv = db.NhanViens.SingleOrDefault(n => n.MaNV == maNV);

            if (nv != null)
            {
                string tenNV = nv.HoTen;
                db.NhanViens.Remove(nv);
                db.SaveChanges();
                TempData["SuccessMessage"] = $"Đã xóa và từ chối tài khoản **{tenNV}**.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy nhân viên cần từ chối.";
            }

            return RedirectToAction("NhanVienChoDuyet");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using DAQCom.Models;

namespace DAQCom.Areas.Admin.Controllers
{
    // QUAN TRỌNG: Sửa thành BaseController để chặn đăng nhập
    public class MonAnAdminController : BaseController
    {
        private QLQuanComEntities db = new QLQuanComEntities();

        // Hàm hỗ trợ tạo dropdown loại món
        private SelectList GetLoaiMonList(string selectedValue = null)
        {
            var items = new List<string>
            {
                "Món mặn", "Món chay", "Món canh",
                "Món ăn kèm", "Tráng miệng", "Nước uống"
            };
            return new SelectList(items, selectedValue);
        }

        // 1. HIỂN THỊ DANH SÁCH (LỌC THEO LOẠI)
        public ActionResult Index(string loaiMon)
        {
            var ds = db.MonAns.AsQueryable();

            // Logic lọc: Nếu có loaiMon truyền vào thì chỉ lấy món đó
            if (!string.IsNullOrEmpty(loaiMon))
            {
                ds = ds.Where(n => n.LoaiMon == loaiMon);
            }

            // Lưu lại loại món hiện tại để dùng cho View
            ViewBag.CurrentLoai = loaiMon;

            return View(ds.OrderByDescending(m => m.MaMon).ToList());
        }

        // 2. HIỆN POPUP THÊM MỚI
        public ActionResult Create()
        {
            ViewBag.LoaiMonList = GetLoaiMonList();
            return PartialView("_CreatePartial");
        }

        // 3. XỬ LÝ THÊM MỚI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MonAn mon, HttpPostedFileBase HinhAnhUpload)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (HinhAnhUpload != null && HinhAnhUpload.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(HinhAnhUpload.FileName);
                        string ext = Path.GetExtension(HinhAnhUpload.FileName);
                        fileName = fileName + "_" + DateTime.Now.Ticks + ext;
                        string path = Server.MapPath("~/Content/Images/" + fileName);
                        HinhAnhUpload.SaveAs(path);
                        mon.HinhAnh = fileName;
                    }
                    db.MonAns.Add(mon);
                    db.SaveChanges();
                    TempData["Success"] = "Thêm thành công!";
                }
                catch { TempData["Error"] = "Lỗi hệ thống!"; }

                // QUAN TRỌNG: Thêm xong quay lại đúng loại món đó
                return RedirectToAction("Index", new { loaiMon = mon.LoaiMon });
            }
            return RedirectToAction("Index");
        }

        // 4. HIỆN POPUP SỬA
        public ActionResult Edit(int id)
        {
            var mon = db.MonAns.Find(id);
            if (mon == null) return HttpNotFound();
            ViewBag.LoaiMonList = GetLoaiMonList(mon.LoaiMon);
            return PartialView("_EditPartial", mon);
        }

        // 5. XỬ LÝ SỬA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(MonAn mon, HttpPostedFileBase HinhAnhMoi)
        {
            var oldMon = db.MonAns.Find(mon.MaMon);
            if (oldMon != null)
            {
                oldMon.TenMon = mon.TenMon;
                oldMon.Gia = mon.Gia;
                oldMon.MoTa = mon.MoTa;
                oldMon.LoaiMon = mon.LoaiMon;

                if (HinhAnhMoi != null && HinhAnhMoi.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(HinhAnhMoi.FileName);
                    string ext = Path.GetExtension(HinhAnhMoi.FileName);
                    fileName = fileName + "_" + DateTime.Now.Ticks + ext;
                    string path = Server.MapPath("~/Content/Images/" + fileName);
                    HinhAnhMoi.SaveAs(path);
                    oldMon.HinhAnh = fileName;
                }
                db.SaveChanges();
                TempData["Success"] = "Cập nhật thành công!";
            }
            // Sửa xong quay lại đúng loại món đó
            return RedirectToAction("Index", new { loaiMon = mon.LoaiMon });
        }

        // 6. XÓA MÓN
        public ActionResult Delete(int id)
        {
            var mon = db.MonAns.Find(id);
            string loaiMonCanQuayVe = ""; // Biến tạm để nhớ loại món

            if (mon != null)
            {
                loaiMonCanQuayVe = mon.LoaiMon; // Lưu lại loại món trước khi xóa
                db.MonAns.Remove(mon);
                db.SaveChanges();
                TempData["Success"] = "Đã xóa thành công!";
            }
            // Xóa xong quay lại đúng loại món đó
            return RedirectToAction("Index", new { loaiMon = loaiMonCanQuayVe });
        }
    }
}
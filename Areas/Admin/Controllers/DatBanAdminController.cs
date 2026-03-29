using System;
using System.Linq;
using System.Web.Mvc;
using DAQCom.Models;

namespace DAQCom.Areas.Admin.Controllers
{
    public class DatBanAdminController : Controller
    {
        private QLQuanComEntities db = new QLQuanComEntities();

        // 1. Hiển thị danh sách đặt bàn
        public ActionResult Index()
        {
            // Sắp xếp theo ngày đến mới nhất
            var listDatBan = db.DatBans.OrderByDescending(n => n.NgayDen).ToList();
            return View(listDatBan);
        }

        // 2. Duyệt bàn
        public ActionResult DuyetBan(int id)
        {
            var item = db.DatBans.Find(id);
            if (item != null)
            {
                item.TrangThai = 1; // 1: Đã xác nhận
                db.SaveChanges();
                TempData["Success"] = "Đã duyệt lịch đặt bàn!";
            }
            return RedirectToAction("Index");
        }

        // 3. Hủy bàn
        public ActionResult HuyBan(int id)
        {
            var item = db.DatBans.Find(id);
            if (item != null)
            {
                item.TrangThai = 2; // 2: Đã hủy
                db.SaveChanges();
                TempData["Success"] = "Đã hủy lịch đặt bàn!";
            }
            return RedirectToAction("Index");
        }

        // 4. Xóa vĩnh viễn
        public ActionResult Xoa(int id)
        {
            var item = db.DatBans.Find(id);
            if (item != null)
            {
                db.DatBans.Remove(item);
                db.SaveChanges();
                TempData["Success"] = "Đã xóa dữ liệu!";
            }
            return RedirectToAction("Index");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DAQCom.Models;

namespace DAQCom.Controllers
{
    public class HomeController : Controller
    {
        // Khởi tạo kết nối CSDL
        private QLQuanComEntities db = new QLQuanComEntities();

        public ActionResult Index()
        {
            // Lấy danh sách món ăn từ bảng MonAns và chia theo loại
            // ToList() giúp dữ liệu không bị null khi truyền sang View
            ViewBag.MonMan = db.MonAns.Where(n => n.LoaiMon == "Món mặn").Take(8).ToList();
            ViewBag.MonChay = db.MonAns.Where(n => n.LoaiMon == "Món chay").Take(8).ToList();
            ViewBag.MonCanh = db.MonAns.Where(n => n.LoaiMon == "Món canh").Take(8).ToList();
            ViewBag.MonAnKem = db.MonAns.Where(n => n.LoaiMon == "Món ăn kèm").Take(8).ToList();
            ViewBag.MonTrangMieng = db.MonAns.Where(n => n.LoaiMon == "Tráng miệng").Take(8).ToList();
            ViewBag.NuocUong = db.MonAns.Where(n => n.LoaiMon == "Nước uống").Take(8).ToList();

            return View();
        }
    }
}
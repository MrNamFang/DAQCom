using BCrypt.Net;
using DAQCom.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace DAQCom.Controllers
{
    public class QuanAnController : Controller
    {
        private QLQuanComEntities db = new QLQuanComEntities();

        // ================= TRANG CHỦ =================
        public ActionResult Index()
        {
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.Success = TempData["SuccessMessage"];
            }
            if (TempData["ThongBao"] != null)
            {
                ViewBag.ThongBao = TempData["ThongBao"];
            }
            ViewBag.MonMan = db.MonAns.Where(x => x.LoaiMon == "Món mặn").Take(8).ToList();
            ViewBag.MonCanh = db.MonAns.Where(x => x.LoaiMon == "Món canh").Take(8).ToList();
            ViewBag.MonChay = db.MonAns.Where(x => x.LoaiMon == "Món chay").Take(8).ToList();
            ViewBag.MonTrangMieng = db.MonAns.Where(x => x.LoaiMon == "Tráng miệng").Take(8).ToList();
            ViewBag.MonAnKem = db.MonAns.Where(x => x.LoaiMon == "Món ăn kèm").Take(8).ToList();
            ViewBag.NuocUong = db.MonAns.Where(x => x.LoaiMon == "Nước uống").Take(8).ToList();
            return View();
        }

        // ================= ĐẶT MÓN =================
        public ActionResult DatMon(string loai)
        {
            // 1. Lấy toàn bộ món ăn
            var dsMon = db.MonAns.AsQueryable();

            // 2. Lọc theo loại nếu có (dùng cho tìm kiếm nhanh hoặc lọc)
            if (!string.IsNullOrEmpty(loai))
            {
                dsMon = dsMon.Where(x => x.LoaiMon.Contains(loai));
            }

            var ketQua = dsMon.ToList();

            // 3. QUAN TRỌNG: Gán dữ liệu cho các nhóm món ăn để View hiển thị được
            ViewBag.MonMan = ketQua.Where(x => x.LoaiMon == "Món mặn").ToList();
            ViewBag.MonCanh = ketQua.Where(x => x.LoaiMon == "Món canh").ToList();
            ViewBag.MonChay = ketQua.Where(x => x.LoaiMon == "Món chay").ToList();
            ViewBag.MonTrangMieng = ketQua.Where(x => x.LoaiMon == "Tráng miệng").ToList();
            ViewBag.MonAnKem = ketQua.Where(x => x.LoaiMon == "Món ăn kèm").ToList();
            ViewBag.NuocUong = ketQua.Where(x => x.LoaiMon == "Nước uống").ToList();

            ViewBag.LoaiHienTai = loai;

            if (Request.IsAjaxRequest())
            {
                return PartialView("_DanhSachMonAn", ketQua);
            }

            return View(ketQua);
        }

        // ================= TÌM KIẾM (ĐÃ SỬA KHỚP VỚI GIAO DIỆN) =================
        public ActionResult TimKiem(string timkiem) // Đổi từ tuKhoa thành timkiem để khớp với URL
        {
            // 1. Lấy toàn bộ danh sách dưới dạng truy vấn (chưa thực thi)
            var dsMon = db.MonAns.AsQueryable();

            // 2. Kiểm tra nếu có nhập từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(timkiem))
            {
                // Chuyển từ khóa về chữ thường và lọc (Entity Framework sẽ tự xử lý tiếng Việt)
                dsMon = dsMon.Where(x => x.TenMon.Contains(timkiem));
            }

            // 3. Thực thi truy vấn và ép kiểu về List
            var ketQua = dsMon.ToList();

            // 4. Gán dữ liệu vào ViewBag để View hiển thị
            ViewBag.KetQua = ketQua;
            ViewBag.TuKhoa = timkiem;

            return View();
        }

        // ================= LIÊN HỆ (BỔ SUNG MỚI) =================
        public ActionResult LienHe()
        {
            return View();
        }

        // ================= CHI TIẾT MÓN (DÙNG CHUNG) =================
        // Đổi tên thành ChiTiet để khớp với link /QuanAn/ChiTiet/48
        public ActionResult ChiTiet(int id)
        {
            // 1. Tìm món ăn theo mã ID truyền vào
            var mon = db.MonAns.SingleOrDefault(n => n.MaMon == id);

            // 2. Nếu không thấy món, báo lỗi 404
            if (mon == null)
            {
                return HttpNotFound();
            }

            // 3. Trả về View có tên là ChiTiet (nằm trong Views/QuanAn/ChiTiet.cshtml)
            return View("ChiTiet", mon);
        }

        // Giữ thêm hàm này để nếu chỗ nào lỡ gọi ChiTietMon thì không bị lỗi
        public ActionResult ChiTietMon(int id)
        {
            return RedirectToAction("ChiTiet", new { id = id });
        }


        // ================= ĐĂNG NHẬP (GET) =================
        [HttpGet]
        public ActionResult DangNhap(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(string TenDangNhap, string MatKhau, string returnUrl)
        {
            if (!VerifyCaptcha())
            {
                ViewBag.Error = "Vui lòng xác nhận Captcha.";
                return View();
            }

            try
            {
                // --- 1. Admin & 2. Nhân viên ---
                var ad = db.admins.FirstOrDefault(x => x.username == TenDangNhap);
                if (ad != null && ((ad.password == MatKhau) || (ad.password.StartsWith("$2") && BCrypt.Net.BCrypt.Verify(MatKhau, ad.password))))
                {
                    Session["user"] = ad;
                    Session["Role"] = "Admin";
                    Session.Timeout = 15;
                    return RedirectToAction("Index", "HomeAdmin", new { area = "Admin" });
                }

                var nv = db.NhanViens.FirstOrDefault(x => x.TaiKhoan == TenDangNhap);
                if (nv != null && ((nv.MatKhau == MatKhau) || (nv.MatKhau.StartsWith("$2") && BCrypt.Net.BCrypt.Verify(MatKhau, nv.MatKhau))))
                {
                    Session["UserNhanVien"] = nv;
                    Session["Role"] = "NhanVien";
                    Session.Timeout = 1;
                    return RedirectToAction("Index", "NhanVien");
                }

                // --- 3. Khách hàng ---
                var kh = db.KhachHangs.FirstOrDefault(x => x.TenDangNhap == TenDangNhap);
                if (kh != null)
                {
                    // Kiểm tra xem tài khoản có đang bị khóa không
                    if (kh.IsLocked == true)
                    {
                        ViewBag.Error = "Tài khoản bị khóa. Vui lòng nhập OTP để mở khóa.";
                        ViewBag.ShowUnlock = true;
                        return View();
                    }

                    // Kiểm tra mật khẩu
                    bool isPasswordOk = false;
                    try { isPasswordOk = (kh.MatKhau == MatKhau) || (kh.MatKhau.StartsWith("$2") && BCrypt.Net.BCrypt.Verify(MatKhau, kh.MatKhau)); } catch { }

                    if (isPasswordOk)
                    {
                        // Nếu mật khẩu đúng, mới kiểm tra xem đã kích hoạt chưa
                        if (kh.TrangThai == 0 || kh.TrangThai == null)
                        {
                            ViewBag.Error = "Tài khoản chưa xác thực OTP sau khi đăng ký.";
                            return View();
                        }

                        // Đăng nhập thành công -> Reset số lần sai
                        kh.SoLanSai = 0;
                        db.Entry(kh).State = EntityState.Modified;
                        db.SaveChanges();

                        Session["UserKhachHang"] = kh;
                        Session["Role"] = "KhachHang";
                        Session.Timeout = 30; // Tăng thời gian session cho khách

                        if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
                        return RedirectToAction("Index", "QuanAn");
                    }
                    else
                    {
                        // SAI MẬT KHẨU: Tăng số lần sai (Xử lý cả trường hợp NULL)
                        kh.SoLanSai = (kh.SoLanSai ?? 0) + 1;

                        // Hiển thị thông báo cho khách biết đã sai bao nhiêu lần
                        ViewBag.Error = "Sai mật khẩu lần " + kh.SoLanSai + "/5.";

                        if (kh.SoLanSai >= 5)
                        {
                            kh.IsLocked = true;
                            string otp = new Random().Next(100000, 999999).ToString();
                            kh.OTP = otp;

                            string ketQuaGuiMail = GuiEmailOTP(kh.Email, otp);

                            if (ketQuaGuiMail == "OK")
                            {
                                ViewBag.Error = "Tài khoản bị khóa do sai 5 lần. Mã OTP mở khóa đã được gửi đến Email.";
                            }
                            else
                            {
                                ViewBag.Error = "Đã khóa tài khoản nhưng lỗi gửi mail: " + ketQuaGuiMail;
                            }
                            ViewBag.ShowUnlock = true;
                        }

                        // QUAN TRỌNG: Lưu lại số lần sai vào Database sau mỗi lần nhập sai
                        db.Entry(kh).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                else
                {
                    ViewBag.Error = "Tài khoản không tồn tại.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
            }
            return View();
        }

        // ================= MỞ KHÓA TÀI KHOẢN =================
        [HttpPost]
        public ActionResult MoKhoa(string TenDangNhap, string maOTP)
        {
            var kh = db.KhachHangs.FirstOrDefault(x => x.TenDangNhap == TenDangNhap && x.OTP == maOTP);
            if (kh != null)
            {
                kh.IsLocked = false;
                kh.SoLanSai = 0;
                kh.OTP = null;
                db.Entry(kh).State = EntityState.Modified;
                db.SaveChanges();

                TempData["ThongBao"] = "Mở khóa thành công! Mời bạn đăng nhập lại.";
                return RedirectToAction("DangNhap");
            }

            ViewBag.Error = "Mã OTP không chính xác.";
            ViewBag.ShowUnlock = true;
            return View("DangNhap");
        }

        public ActionResult DangXuat()
        {
            Session.Clear();      // Xóa hết các biến Session
            Session.RemoveAll();  // Xóa sạch toàn bộ
            Session.Abandon();    // Hủy phiên làm việc hiện tại

            // Xóa Cookie xác thực nếu có dùng FormsAuthentication
            if (Request.Cookies["ASP.NET_SessionId"] != null)
            {
                Response.Cookies["ASP.NET_SessionId"].Expires = DateTime.Now.AddDays(-1);
            }

            return RedirectToAction("Index", "QuanAn");
        }
        // ================= GỬI EMAIL OTP =================
        public string GuiEmailOTP(string emailNhan, string otp)
        {
            try
            {
                string fromEmail = ConfigurationManager.AppSettings["Email"];
                string fromPassword = ConfigurationManager.AppSettings["EmailPassword"];

                var fromAddress = new MailAddress(fromEmail, "Hệ thống Quán Ăn PNK");
                var toAddress = new MailAddress(emailNhan);

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = "Ma OTP Mo Khoa Tai Khoan",
                    Body = $"<h3>Xác thực mở khóa tài khoản</h3><p>Mã OTP của bạn là: <b style='color:red; font-size: 20px;'>{otp}</b></p><p>Mã này có hiệu lực trong 5 phút.</p>",
                    IsBodyHtml = true
                })
                {
                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromEmail, fromPassword)
                    };
                    smtp.Send(message);
                }
                return "OK";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        // ================= ĐĂNG KÝ =================
        [HttpGet]
        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(string LoaiTaiKhoan, string HoTen, string Email, string SDT, string TenDangNhap, string MatKhau)
        {
            if (!VerifyCaptcha())
            {
                ViewBag.Error = "Vui lòng xác nhận Captcha.";
                return View();
            }

            string hashedPass = BCrypt.Net.BCrypt.HashPassword(MatKhau);

            if (LoaiTaiKhoan == "KhachHang")
            {
                if (db.KhachHangs.Any(x => x.TenDangNhap == TenDangNhap))
                {
                    ViewBag.Error = "Tên đăng nhập khách đã tồn tại.";
                    return View();
                }
                KhachHang kh = new KhachHang { TenKH = HoTen, Email = Email, SDT = SDT, TenDangNhap = TenDangNhap, MatKhau = hashedPass, TrangThai = 0, NgayDangKy = DateTime.Now };
                Session["TempUser"] = kh;
            }
            else // NhanVien
            {
                if (db.NhanViens.Any(x => x.TaiKhoan == TenDangNhap))
                {
                    ViewBag.Error = "Tên đăng nhập nhân viên đã tồn tại.";
                    return View();
                }
                NhanVien nv = new NhanVien { TenNV = HoTen, Email = Email, SDT = SDT, TaiKhoan = TenDangNhap, MatKhau = hashedPass, TrangThai = 0 };
                Session["TempUser"] = nv;
            }

            Session["UserRole_Register"] = LoaiTaiKhoan;
            Session["IsRegistering"] = true;
            return RedirectToAction("GuiMaOTP");
        }

        // ================= QUÊN MẬT KHẨU =================
        [HttpGet]
        public ActionResult QuenMatKhau()
        {
            return View();
        }

        [HttpPost]
        public ActionResult QuenMatKhau(string TenDangNhap)
        {
            var userKH = db.KhachHangs.FirstOrDefault(x => x.TenDangNhap == TenDangNhap);
            var userNV = db.NhanViens.FirstOrDefault(x => x.TaiKhoan == TenDangNhap);

            if (userKH == null && userNV == null)
            {
                ViewBag.Error = "Tài khoản không tồn tại.";
                return View();
            }

            if (userKH != null)
            {
                Session["UserReset"] = userKH;
                Session["ResetRole"] = "KH";
            }
            else if (userNV != null)
            {
                Session["UserReset"] = userNV;
                Session["ResetRole"] = "NV";
            }

            Session["IsRegistering"] = false;
            return RedirectToAction("GuiMaOTP");
        }

        public ActionResult QuenTenDangNhap()
        {
            return View();
        }

        // ================= XỬ LÝ OTP =================

        public ActionResult GuiMaOTP()
        {
            string emailDest = "";
            string otp = new Random().Next(100000, 999999).ToString();
            Session["OTP"] = otp;

            if (Session["IsRegistering"] != null && (bool)Session["IsRegistering"])
            {
                var role = Session["UserRole_Register"]?.ToString();
                var tempUser = Session["TempUser"];
                if (tempUser == null) return RedirectToAction("DangKy");

                if (role == "KhachHang") emailDest = ((KhachHang)tempUser).Email;
                else emailDest = ((NhanVien)tempUser).Email;
            }
            else
            {
                string role = Session["ResetRole"]?.ToString();
                var userReset = Session["UserReset"];
                if (userReset == null) return RedirectToAction("QuenMatKhau");

                if (role == "KH") emailDest = ((KhachHang)userReset).Email;
                else if (role == "NV") emailDest = ((NhanVien)userReset).Email;
            }

            if (string.IsNullOrEmpty(emailDest))
            {
                TempData["Error"] = "Tài khoản không có Email liên kết.";
                return RedirectToAction("QuenMatKhau");
            }

            Session["ResetEmail"] = emailDest;
            return thucHienGuiMail(emailDest, otp);
        }

        private ActionResult thucHienGuiMail(string Email, string otp)
        {
            try
            {
                string fromEmail = System.Web.Configuration.WebConfigurationManager.AppSettings["Email"];
                string password = System.Web.Configuration.WebConfigurationManager.AppSettings["EmailPassword"];

                System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
                mail.From = new System.Net.Mail.MailAddress(fromEmail, "PNK Restaurant");
                mail.To.Add(Email);
                mail.Subject = otp + " là mã xác thực của bạn";
                mail.Body = "<h3>Mã xác thực OTP</h3><p>Mã của bạn là: <b>" + otp + "</b></p>";
                mail.IsBodyHtml = true;

                System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
                smtp.EnableSsl = true;
                smtp.Credentials = new System.Net.NetworkCredential(fromEmail, password);
                smtp.Send(mail);

                return RedirectToAction("XacNhanOTP");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi gửi mail: " + ex.Message;
                return RedirectToAction("QuenMatKhau");
            }
        }

        [HttpGet]
        public ActionResult XacNhanOTP()
        {
            if (Session["OTP"] == null) return RedirectToAction("DangNhap");
            return View();
        }

        [HttpPost]
        public ActionResult XacNhanOTP(string otpInput)
        {
            if (Session["OTP"] != null && otpInput == Session["OTP"].ToString())
            {
                if (Session["IsRegistering"] != null && (bool)Session["IsRegistering"])
                {
                    string role = Session["UserRole_Register"]?.ToString();
                    var tempUser = Session["TempUser"];

                    if (tempUser == null)
                    {
                        ViewBag.Error = "Phiên đăng ký hết hạn.";
                        return View();
                    }

                    try
                    {
                        if (role == "NhanVien")
                        {
                            var nv = (NhanVien)tempUser;
                            nv.TrangThai = 1;
                            nv.HoTen = nv.TenNV;
                            nv.ChucVu = "Nhân viên";
                            nv.NgayTao = DateTime.Now;
                            db.NhanViens.Add(nv);
                            db.SaveChanges();
                            Session["UserNhanVien"] = nv;
                            Session["Role"] = "NhanVien";
                            ClearOTPSessions();
                            return RedirectToAction("Index", "NhanVien");
                        }
                        else // KhachHang
                        {
                            var kh = (KhachHang)tempUser;
                            kh.TrangThai = 1;
                            kh.NgayDangKy = DateTime.Now;
                            if (string.IsNullOrEmpty(kh.DiaChi)) kh.DiaChi = "Chưa cập nhật";
                            db.KhachHangs.Add(kh);
                            db.SaveChanges();
                            Session["UserKhachHang"] = kh;
                            Session["Role"] = "KhachHang";
                            ClearOTPSessions();
                            return RedirectToAction("Index", "QuanAn");
                        }
                    }
                    catch (Exception ex)
                    {
                        ViewBag.Error = "Lỗi lưu DB: " + ex.Message;
                        return View();
                    }
                }
                return RedirectToAction("DatLaiMatKhau");
            }

            ViewBag.Error = "Mã OTP không chính xác.";
            return View();
        }

        private void ClearOTPSessions()
        {
            Session.Remove("TempUser");
            Session.Remove("IsRegistering");
            Session.Remove("OTP");
            Session.Remove("UserRole_Register");
        }

        public ActionResult ResendOTP()
        {
            return RedirectToAction("GuiMaOTP");
        }

        // ================= ĐẶT LẠI MẬT KHẨU =================
        [HttpGet] public ActionResult DatLaiMatKhau() { return View(); }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatLaiMatKhau(string MatKhauMoi, string NhapLaiMatKhau)
        {
            if (MatKhauMoi != NhapLaiMatKhau) { ViewBag.Error = "Mật khẩu không khớp."; return View(); }
            string hashed = BCrypt.Net.BCrypt.HashPassword(MatKhauMoi);
            string role = Session["ResetRole"].ToString();

            if (role == "KH") { var id = ((KhachHang)Session["UserReset"]).MaKH; var u = db.KhachHangs.Find(id); u.MatKhau = hashed; }
            else if (role == "NV") { var id = ((NhanVien)Session["UserReset"]).MaNV; var u = db.NhanViens.Find(id); u.MatKhau = hashed; }

            db.SaveChanges();
            Session.Clear();
            TempData["ThongBao"] = "Đã đặt lại mật khẩu thành công!";
            return RedirectToAction("DangNhap");
        }

        // ================= THÔNG TIN CÁ NHÂN =================
        public ActionResult ThongTinCaNhan()
        {
            if (Session["UserKhachHang"] != null)
            {
                var khSession = (KhachHang)Session["UserKhachHang"];
                var kh = db.KhachHangs.Find(khSession.MaKH);
                ViewBag.UserRecord = kh;
                ViewBag.IsKhachHang = true;
                ViewBag.LichSu = db.HoaDons.Where(h => h.MaKH == kh.MaKH).OrderByDescending(h => h.NgayDat).ToList();
                return View();
            }
            else if (Session["UserNhanVien"] != null)
            {
                var nvSession = (NhanVien)Session["UserNhanVien"];
                var nv = db.NhanViens.Find(nvSession.MaNV);
                ViewBag.UserRecord = nv;
                ViewBag.IsKhachHang = false;
                return View();
            }
            return RedirectToAction("DangNhap");
        }

        private bool VerifyCaptcha()
        {
            string resp = Request["g-recaptcha-response"];
            string secret = System.Web.Configuration.WebConfigurationManager.AppSettings["reCaptchaSecretKey"];
            if (string.IsNullOrEmpty(resp)) return false;
            try
            {
                using (var wc = new WebClient())
                {
                    string json = wc.DownloadString($"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={resp}");
                    return (bool)JObject.Parse(json)["success"];
                }
            }
            catch { return false; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatThongTin(int ID, string HoTen, string Email, string SDT, string DiaChi)
        {
            if (Session["UserKhachHang"] != null)
            {
                var kh = db.KhachHangs.Find(ID);
                if (kh != null)
                {
                    kh.TenKH = HoTen;
                    kh.Email = Email;
                    kh.SDT = SDT;
                    kh.DiaChi = DiaChi;
                    db.Entry(kh).State = EntityState.Modified;
                    db.SaveChanges();
                    Session["UserKhachHang"] = kh;
                    TempData["ThongBao"] = "Cập nhật thông tin khách hàng thành công!";
                }
            }
            else if (Session["UserNhanVien"] != null)
            {
                var nv = db.NhanViens.Find(ID);
                if (nv != null)
                {
                    nv.TenNV = HoTen;
                    nv.Email = Email;
                    nv.SDT = SDT;
                    db.Entry(nv).State = EntityState.Modified;
                    db.SaveChanges();
                    Session["UserNhanVien"] = nv;
                    TempData["ThongBao"] = "Cập nhật thông tin nhân viên thành công!";
                }
            }
            return RedirectToAction("ThongTinCaNhan");
        }

        public void LoginGoogle()
        {
            // 1. Lấy ClientID từ file Web.config
            string clientId = ConfigurationManager.AppSettings["GoogleClientId"];

            // 2. Tự động lấy đường dẫn callback (vừa khớp với Google Cloud bạn đã cài)
            string redirectUri = Url.Action("GoogleLoginCallback", "QuanAn", null, Request.Url.Scheme);

            // 3. Tạo URL yêu cầu Google: 
            // - scope=email%20profile: Lấy email và họ tên
            // - prompt=select_account: BẮT BUỘC Google phải hiện bảng chọn tài khoản
            string url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope=email%20profile&prompt=select_account";

            // 4. Chuyển hướng người dùng sang trang Google
            Response.Redirect(url);
        }

        public async Task<ActionResult> GoogleLoginCallback(string code)
        {
            if (string.IsNullOrEmpty(code)) return RedirectToAction("DangNhap");

            string clientId = ConfigurationManager.AppSettings["GoogleClientId"];
            string clientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];
            string redirectUri = Url.Action("GoogleLoginCallback", "QuanAn", null, Request.Url.Scheme);

            // 1. Đổi code lấy Access Token
            var client = new HttpClient();
            var values = new Dictionary<string, string>
    {
        { "code", code },
        { "client_id", clientId },
        { "client_secret", clientSecret },
        { "redirect_uri", redirectUri },
        { "grant_type", "authorization_code" }
    };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://oauth2.googleapis.com/token", content);
            var responseString = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<dynamic>(responseString);
            string accessToken = tokenData.access_token;

            // 2. Dùng Token lấy thông tin User (Email, Name)
            var infoResponse = await client.GetAsync($"https://www.googleapis.com/oauth2/v3/userinfo?access_token={accessToken}");
            var infoString = await infoResponse.Content.ReadAsStringAsync();
            var userInfo = JsonConvert.DeserializeObject<dynamic>(infoString);

            string email = userInfo.email;
            string name = userInfo.name;

            // 3. Kiểm tra database (Bảng KhachHang)
            var kh = db.KhachHangs.FirstOrDefault(x => x.Email == email);
            if (kh == null)
            {
                // Nếu chưa có thì tự động tạo mới tài khoản khách hàng
                kh = new KhachHang
                {
                    TenKH = name,
                    Email = email,
                    TenDangNhap = email, // Dùng email làm tên đăng nhập luôn
                    MatKhau = Guid.NewGuid().ToString().Substring(0, 8), // Mật khẩu ngẫu nhiên
                    SDT = "0000000000"
                };
                db.KhachHangs.Add(kh);
                db.SaveChanges();
            }

            // 4. Lưu Session và đăng nhập
            Session["TaiKhoan"] = kh; // Dùng cho Layout hiện tại (nếu có)
            Session["UserKhachHang"] = kh; // Dùng cho trang ThongTinCaNhan và các logic kiểm tra khác
            return RedirectToAction("Index", "QuanAn");
        }


    }
}
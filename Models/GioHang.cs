using System;
using System.Linq;

// Đảm bảo bạn đã using thư viện chứa lớp MonAn
// Ví dụ: using DAQCom.Models; 

namespace DAQCom.Models
{
    public class GioHang
    {
        // Loại bỏ private QLQuanComEntities db = new QLQuanComEntities();
        // Không nên giữ DbContext trong class Model Session

        public int iMaMon { get; set; }
        public string sTenMon { get; set; }
        public string sAnh { get; set; } // Đổi tên thuộc tính ảnh thành sAnh
        public double dDonGia { get; set; }
        public int iSoLuong { get; set; }

        // Thuộc tính Ghi Chú
        public string GhiChu { get; set; }

        public double dThanhTien
        {
            get { return iSoLuong * dDonGia; }
        }

        public GioHang()
        {
            GhiChu = "";
        }

        // Constructor có tham số
        public GioHang(int maMon)
        {
            iMaMon = maMon;

            // ✅ SỬA LỖI: Khởi tạo DbContext bên trong khối using
            using (var db = new QLQuanComEntities())
            {
                MonAn mon = db.MonAns.SingleOrDefault(n => n.MaMon == iMaMon);
                if (mon != null)
                {
                    sTenMon = mon.TenMon;
                    // Lỗi: bạn dùng mon.HinhAnh, tôi dùng mon.Anh trong code trước đó.
                    // Tùy theo tên cột trong DB của bạn. Tôi giữ lại theo code của bạn:
                    sAnh = mon.HinhAnh;

                    // Xử lý an toàn khi Gia có thể là decimal?
                    dDonGia = double.Parse((mon.Gia ?? 0).ToString());
                    iSoLuong = 1;
                    GhiChu = "";
                }
                else
                {
                    // Trường hợp món ăn không tìm thấy
                    sTenMon = "Món ăn không tồn tại";
                    sAnh = "noimage.jpg";
                    dDonGia = 0;
                    iSoLuong = 1;
                    GhiChu = "";
                }
            }
        }
    }
}
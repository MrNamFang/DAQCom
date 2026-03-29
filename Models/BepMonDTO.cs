using System;

namespace DAQCom.Models
{
    public class BepMonDTO
    {
        public int MaCT { get; set; }
        public string TenMon { get; set; }
        public int SoLuong { get; set; }
        public string GhiChu { get; set; }
        public string TenBan { get; set; } // Tên bàn lấy từ bảng BanAn
        public string GioGoi { get; set; }
    }
}
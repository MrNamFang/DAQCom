using System;

namespace DAQCom.Models
{
    public class BanAnDTO
    {
        public int MaBan { get; set; }
        public string TenBan { get; set; }
        public int TrangThai { get; set; } // 0: Trống, 1: Có khách
        public string GioVao { get; set; }
        public decimal TamTinh { get; set; }
    }
}
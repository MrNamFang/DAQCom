using System;
using System.Collections.Generic;

namespace DAQCom.Models
{
    // Class đại diện cho 1 món ăn trong phiếu
    public class BepItemDTO
    {
        public int MaCT { get; set; }
        public string TenMon { get; set; }
        public int SoLuong { get; set; }
        public string GhiChu { get; set; }
        public int TrangThai { get; set; } // 0: Chờ nấu, 1: Đang nấu, 2: Xong
    }

    // Class đại diện cho 1 Phiếu Order (1 Bàn)
    public class BepTicketDTO
    {
        public int MaHD { get; set; }
        public string TenBan { get; set; }
        public string GioGoi { get; set; } // Giờ gọi món gần nhất
        public List<BepItemDTO> ListMon { get; set; } // Danh sách các món của bàn này
    }
}
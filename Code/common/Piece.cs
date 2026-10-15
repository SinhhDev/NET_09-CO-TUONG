using System; 

namespace CoTuongOnline.Common 
{
    // Enum định nghĩa các loại quân cờ trong cờ tướng
    public enum PieceType
    {
        General,  // Tướng
        Advisor,  // Sĩ
        Elephant, // Tượng
        Horse,    // Mã
        Rook,     // Xe
        Cannon,   // Pháo
        Soldier   // Tốt
    }

    // Lớp đại diện cho một quân cờ trên bàn cờ
    public class Piece
    {
        public PieceType Type; // Loại quân cờ (Xe, Mã, Tướng...)
        public bool IsRed;     // Màu quân cờ: true = Đỏ, false = Đen
        public int Row;        // Vị trí hàng của quân cờ trên bàn cờ
        public int Col;        // Vị trí cột của quân cờ trên bàn cờ

        // Constructor: Hàm khởi tạo khi tạo một quân cờ mới
        public Piece(PieceType type, bool isRed, int row, int col)
        {
            Type = type;   // Gán loại quân cờ
            IsRed = isRed; // Gán màu quân cờ
            Row = row;     // Gán vị trí hàng
            Col = col;     // Gán vị trí cột
        }

        // Hàm override ToString để hiển thị thông tin quân cờ khi debug hoặc in ra console
        public override string ToString()
        {
            // Trả về chuỗi gồm:
            // Loại quân + màu quân + vị trí trên bàn cờ
            return $"{Type} ({(IsRed ? "Red" : "Black")}) [{Row},{Col}]";
        }
    }
}
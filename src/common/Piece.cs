namespace CoTuongOnline.Common
{
    // Màu cờ
    public enum PieceColor
    {
        Red,
        Black
    }

    // Quân cờ
    public enum PieceType
    {
        King,     // Tướng 
        Advisor,  // Sĩ
        Elephant, // Tượng
        Horse,    // Mã
        Chariot,  // Xe
        Cannon,   // Pháo
        Soldier   // Tốt 
    }

    // Tọa độ 
    public struct Position
    {
        public int Y { get; set; }  // Hàng ngang - Row 0-9
        public int X { get; set; }  // Hàng dọc - Column 0-8

        public Position(int x, int y)
        {
            Y = y;
            X = x;
        }

        public override string ToString() => $"({X}, {Y})";
    }

    // Class quân cờ 
    public class Piece
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }
        public Position Pos { get; set; }
        //cái này 3 tham sô 
       //public Piece(PieceType type, PieceColor color, Position pos)
       // {
       //     Type = type;
       //     Color = color;
       //     Pos = pos;
        //}

        // hàm khởi tạo 4 tham số 
        public Piece(PieceType type, PieceColor color, int x, int y)
        {
            Type = type;
            Color = color;
            Pos = new Position(x, y);
        }

        public override string ToString()
        {
            string colorPrefix = (Color == PieceColor.Red) ? "R" : "B";  //cái này để R/B trước quân cờ (phân biệt màu)
            return $"{colorPrefix}_{Type}";
        }
    }
}
namespace Cotuong.Common
{
	//Màu cờ
	public enum PieceColor
	{
		Red,
		Black
	}

	// Quaan co
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
		public int Y { get; set; }  //hang ngang - Row 0-9
		public int X { get; set; }  //hang doc - Column 0-8

		public Position(int x, int y)
		{
			Y = y;
			X = x;
		}

		public override string ToString() => $"({X}, {Y})";
	}

	//Class quân cờ 
	public class Piece
	{
		public PieceType Type { get; set; }
		public PieceColor Color { get; set; }
		public Position Pos { get; set; }

		public Piece(PieceType type, PieceColor color, Position pos)
		{
			Type = type;
			Color = color;
			Pos = pos;
		}

		public override string ToString()
		{
			string colorPrefix = (Color == PieceColor.Red) ? "R" : "B";
			return $"{colorPrefix}_{Type}";
		}
	}
}
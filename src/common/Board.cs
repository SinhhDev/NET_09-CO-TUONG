using System;

namespace CoTuongOnline.Common
{
    public class Board
    {
        public const int COLS = 9;  // X: 0 -> 8
        public const int ROWS = 10; // Y: 0 -> 9

        // Ma trận [X, Y]: Cột X  (9), Hàng Y (10)
        public Piece[,] grid = new Piece[COLS, ROWS];

        public Board()
        {
            Init();
        }

        public void Init()
        {
            // Xóa sạch bàn cờ trước khi xếp quân mới
            Array.Clear(grid, 0, grid.Length);

            // Phe Đen (Y = 0 -> 4)
            grid[0, 0] = new Piece(PieceType.Chariot, PieceColor.Black, 0, 0);
            grid[8, 0] = new Piece(PieceType.Chariot, PieceColor.Black, 8, 0);
            grid[1, 0] = new Piece(PieceType.Horse, PieceColor.Black, 1, 0);
            grid[7, 0] = new Piece(PieceType.Horse, PieceColor.Black, 7, 0);
            grid[2, 0] = new Piece(PieceType.Elephant, PieceColor.Black, 2, 0);
            grid[6, 0] = new Piece(PieceType.Elephant, PieceColor.Black, 6, 0);
            grid[3, 0] = new Piece(PieceType.Advisor, PieceColor.Black, 3, 0);
            grid[5, 0] = new Piece(PieceType.Advisor, PieceColor.Black, 5, 0);
            grid[4, 0] = new Piece(PieceType.King, PieceColor.Black, 4, 0);

            grid[1, 2] = new Piece(PieceType.Cannon, PieceColor.Black, 1, 2);
            grid[7, 2] = new Piece(PieceType.Cannon, PieceColor.Black, 7, 2);

            for (int x = 0; x < COLS; x += 2)
                grid[x, 3] = new Piece(PieceType.Soldier, PieceColor.Black, x, 3);

            // Phe Đỏ (Y = 5 -> 9)
            grid[0, 9] = new Piece(PieceType.Chariot, PieceColor.Red, 0, 9);
            grid[8, 9] = new Piece(PieceType.Chariot, PieceColor.Red, 8, 9);
            grid[1, 9] = new Piece(PieceType.Horse, PieceColor.Red, 1, 9);
            grid[7, 9] = new Piece(PieceType.Horse, PieceColor.Red, 7, 9);
            grid[2, 9] = new Piece(PieceType.Elephant, PieceColor.Red, 2, 9);
            grid[6, 9] = new Piece(PieceType.Elephant, PieceColor.Red, 6, 9);
            grid[3, 9] = new Piece(PieceType.Advisor, PieceColor.Red, 3, 9);
            grid[5, 9] = new Piece(PieceType.Advisor, PieceColor.Red, 5, 9);
            grid[4, 9] = new Piece(PieceType.King, PieceColor.Red, 4, 9);

            grid[1, 7] = new Piece(PieceType.Cannon, PieceColor.Red, 1, 7);
            grid[7, 7] = new Piece(PieceType.Cannon, PieceColor.Red, 7, 7);

            for (int x = 0; x < COLS; x += 2)
                grid[x, 6] = new Piece(PieceType.Soldier, PieceColor.Red, x, 6);
        }

        public Piece GetPiece(int x, int y)
        {
            if (x < 0 || x >= COLS || y < 0 || y >= ROWS) return null;
            return grid[x, y];
        }

        public void Move(int fromX, int fromY, int toX, int toY)
        {
            // Kiểm tra tọa độ hợp lệ 
            if (fromX < 0 || fromX >= COLS || fromY < 0 || fromY >= ROWS ||
                toX < 0 || toX >= COLS || toY < 0 || toY >= ROWS)
            {
                Console.WriteLine("Tọa độ ngoài bàn cờ!");
                return;
            }

            Piece p = grid[fromX, fromY];
            if (p == null)
            {
                Console.WriteLine("Không có quân cờ tại vị trí xuất phát!");
                return;
            }

            // Cập nhật vị trí trên mảng và cập nhật luôn tọa độ bên trong quân cờ
            p.Pos = new Position(toX, toY);
            grid[toX, toY] = p;
            grid[fromX, fromY] = null;
        }

        public void PrintBoard()
        {
            for (int y = 0; y < ROWS; y++)
            {
                for (int x = 0; x < COLS; x++)
                {
                    Console.Write(grid[x, y] == null ? "  .   " : $"{grid[x, y].ToString().Substring(0, 5)} ");    
                }
                Console.WriteLine();
            }
        }
    }
}
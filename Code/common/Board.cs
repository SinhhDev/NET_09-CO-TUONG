using System;

namespace CoTuongOnline.Common
{
    public class Board
    {
        public Piece?[,] grid = new Piece?[10, 9];

        public void Init()
        {
            grid[0, 0] = new Piece(PieceType.Rook, false, 0, 0);
            grid[0, 8] = new Piece(PieceType.Rook, false, 0, 8);
            grid[0, 1] = new Piece(PieceType.Horse, false, 0, 1);
            grid[0, 7] = new Piece(PieceType.Horse, false, 0, 7);
            grid[0, 2] = new Piece(PieceType.Elephant, false, 0, 2);
            grid[0, 6] = new Piece(PieceType.Elephant, false, 0, 6);
            grid[0, 3] = new Piece(PieceType.Advisor, false, 0, 3);
            grid[0, 5] = new Piece(PieceType.Advisor, false, 0, 5);
            grid[0, 4] = new Piece(PieceType.General, false, 0, 4);
            grid[2, 1] = new Piece(PieceType.Cannon, false, 2, 1);
            grid[2, 7] = new Piece(PieceType.Cannon, false, 2, 7);
            for (int i = 0; i < 9; i += 2)
                grid[3, i] = new Piece(PieceType.Soldier, false, 3, i);

            grid[9, 0] = new Piece(PieceType.Rook, true, 9, 0);
            grid[9, 8] = new Piece(PieceType.Rook, true, 9, 8);
            grid[9, 1] = new Piece(PieceType.Horse, true, 9, 1);
            grid[9, 7] = new Piece(PieceType.Horse, true, 9, 7);
            grid[9, 2] = new Piece(PieceType.Elephant, true, 9, 2);
            grid[9, 6] = new Piece(PieceType.Elephant, true, 9, 6);
            grid[9, 3] = new Piece(PieceType.Advisor, true, 9, 3);
            grid[9, 5] = new Piece(PieceType.Advisor, true, 9, 5);
            grid[9, 4] = new Piece(PieceType.General, true, 9, 4);
            grid[7, 1] = new Piece(PieceType.Cannon, true, 7, 1);
            grid[7, 7] = new Piece(PieceType.Cannon, true, 7, 7);
            for (int i = 0; i < 9; i += 2)
                grid[6, i] = new Piece(PieceType.Soldier, true, 6, i);
        }

        public void PrintBoard()
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 9; j++)
                    Console.Write(grid[i, j] == null ? " . " : " X ");
                Console.WriteLine();
            }
        }

        public void Move(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (grid[fromRow, fromCol] == null) { Console.WriteLine("Không có quân!"); return; }
            grid[toRow, toCol] = grid[fromRow, fromCol];
            grid[toRow, toCol]!.Row = toRow;
            grid[toRow, toCol]!.Col = toCol;
            grid[fromRow, fromCol] = null;
        }

        public Board Clone()
        {
            var clone = new Board();
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 9; c++)
                {
                    var p = grid[r, c];
                    if (p != null)
                        clone.grid[r, c] = new Piece(p.Type, p.IsRed, p.Row, p.Col);
                }
            return clone;
        }
    }
}

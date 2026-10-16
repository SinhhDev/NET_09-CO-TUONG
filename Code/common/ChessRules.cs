using System;

namespace CoTuongOnline.Common
{
    public static class ChessRules
    {
        public static bool IsCheckmate(Board board, bool isRedTurn)
        {
            // Kiểm tra xem quân Tướng của bên đi lượt này còn trên bàn cờ không
            bool generalFound = false;
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = board.grid[r, c];
                    if (p != null && p.Type == PieceType.General && p.IsRed == isRedTurn)
                    {
                        generalFound = true;
                        break;
                    }
                }
                if (generalFound) break;
            }

            // Nếu mất Tướng -> Chiếu bí / Thua
            return !generalFound;
        }
    }
}

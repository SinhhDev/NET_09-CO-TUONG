using System;
using System.Drawing;

namespace CoTuongOnline.Common
{
    public static class ChessRules
    {
        /// <summary>
        /// Tìm tọa độ (X = Col, Y = Row) của quân Tướng thuộc phe isRed.
        /// </summary>
        public static Point? FindGeneral(Board board, bool isRed)
        {
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = board.grid[r, c];
                    if (p != null && p.Type == PieceType.General && p.IsRed == isRed)
                    {
                        return new Point(c, r);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Kiểm tra Tướng của phe isRed có đang bị chiếu hay không.
        /// Bao gồm:
        /// 1. Bị bất kỳ quân địch nào (Xe, Pháo, Mã, Tốt, Sĩ) chiếu tới vị trí Tướng.
        /// 2. Bị lộ mặt Tướng (2 Tướng đối mặt nhau trên cùng một cột mà không có quân cản).
        /// </summary>
        public static bool IsUnderCheck(Board board, bool isRed)
        {
            Point? genPos = FindGeneral(board, isRed);
            if (!genPos.HasValue) return false;

            int genCol = genPos.Value.X;
            int genRow = genPos.Value.Y;

            // 1. Kiểm tra luật lộ mặt tướng (2 Tướng nhìn mặt nhau không có quân chắn giữa)
            Point? oppGenPos = FindGeneral(board, !isRed);
            if (oppGenPos.HasValue && oppGenPos.Value.X == genCol)
            {
                int oppGenRow = oppGenPos.Value.Y;
                if (MoveValidator.CountPiecesBetween(board, genRow, genCol, oppGenRow, genCol) == 0)
                {
                    return true;
                }
            }

            // 2. Kiểm tra các quân của đối phương có thể tấn công Tướng không
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? opp = board.grid[r, c];
                    if (opp != null && opp.IsRed != isRed)
                    {
                        if (opp.Type == PieceType.General) continue; // Đã kiểm tra lộ mặt tướng ở trên

                        if (MoveValidator.IsValidMove(board, opp, r, c, genRow, genCol))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra chiếu bí: Nếu Tướng bị mất hoặc bên isRedTurn không còn bất kỳ nước đi hợp lệ nào để thoát chiếu.
        /// </summary>
        public static bool IsCheckmate(Board board, bool isRedTurn)
        {
            Point? genPos = FindGeneral(board, isRedTurn);
            if (!genPos.HasValue) return true;

            // Kiểm tra xem bên này có còn bất kỳ nước đi nào hợp lệ không
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = board.grid[r, c];
                    if (p != null && p.IsRed == isRedTurn)
                    {
                        for (int tr = 0; tr < 10; tr++)
                        {
                            for (int tc = 0; tc < 9; tc++)
                            {
                                Piece? target = board.grid[tr, tc];
                                if (target != null && target.IsRed == isRedTurn) continue;

                                if (MoveValidator.IsValidMove(board, p, r, c, tr, tc))
                                {
                                    Board sim = board.Clone();
                                    sim.Move(r, c, tr, tc);
                                    if (!IsUnderCheck(sim, isRedTurn))
                                    {
                                        return false; // Còn ít nhất 1 nước đi thoát chiếu -> Chưa chiếu bí
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}

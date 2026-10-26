using System;
using CoTuongOnline.Common;
using CoTuongOnline.Logic;

namespace CoTuongOnline.Final
{
    public static class FinalLogic
    {
        // ===== MOVE CHUẨN (FULL LOGIC) =====
        public static bool TryMove(Board board, int fr, int fc, int tr, int tc, bool isRedTurn)
        {
            // ===== CHECK BASIC =====
            if (!InBoard(fr, fc) || !InBoard(tr, tc))
                return false;

            Piece piece = board.grid[fr, fc];

            if (piece == null)
                return false;

            // Không phải lượt mình
            if (piece.IsRed != isRedTurn)
                return false;

            // Không đi vào chính nó
            if (fr == tr && fc == tc)
                return false;

            // ===== CHECK LUẬT DI CHUYỂN =====
            if (!MoveValidator.IsValidMove(board, piece, tr, tc))
                return false;

            // ===== GIẢ LẬP NƯỚC ĐI =====
            Piece target = board.grid[tr, tc];

            int oldR = piece.Row;
            int oldC = piece.Col;

            board.grid[tr, tc] = piece;
            board.grid[fr, fc] = null;

            piece.Row = tr;
            piece.Col = tc;

            // ===== CHECK SELF-CHECK =====
            bool isIllegal = ChessRules.IsCheckmate(board, isRedTurn);

            // ===== CHECK 2 TƯỚNG NHÌN NHAU (SAU KHI ĐI) =====
            bool faceToFace = IsGeneralFaceToFace(board);

            // ===== ROLLBACK =====
            board.grid[fr, fc] = piece;
            board.grid[tr, tc] = target;

            piece.Row = oldR;
            piece.Col = oldC;

            // Nếu bị chiếu hoặc vi phạm → không hợp lệ
            if (isIllegal || faceToFace)
                return false;

            // ===== THỰC HIỆN MOVE THẬT =====
            board.grid[tr, tc] = piece;
            board.grid[fr, fc] = null;

            piece.Row = tr;
            piece.Col = tc;

            return true;
        }

        // ===== CHECK 2 TƯỚNG NHÌN NHAU =====
        private static bool IsGeneralFaceToFace(Board board)
        {
            Piece redGen = null;
            Piece blackGen = null;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    var p = board.grid[i, j];
                    if (p != null && p.Type == PieceType.General)
                    {
                        if (p.IsRed) redGen = p;
                        else blackGen = p;
                    }
                }
            }

            if (redGen == null || blackGen == null)
                return false;

            // Cùng cột
            if (redGen.Col != blackGen.Col)
                return false;

            int col = redGen.Col;
            int minRow = Math.Min(redGen.Row, blackGen.Row) + 1;
            int maxRow = Math.Max(redGen.Row, blackGen.Row);

            for (int r = minRow; r < maxRow; r++)
            {
                if (board.grid[r, col] != null)
                    return false;
            }

            return true; // không có quân chắn → lỗi
        }

        // ===== CHECK TRONG BOARD =====
        private static bool InBoard(int r, int c)
        {
            return r >= 0 && r < 10 && c >= 0 && c < 9;
        }
    }
}
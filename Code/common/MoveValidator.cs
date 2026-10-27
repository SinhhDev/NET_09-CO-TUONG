using System;

namespace CoTuongOnline.Common
{
    public static class MoveValidator
    {
        public static bool TryMove(Board board, int fromRow, int fromCol, int toRow, int toCol, bool isRedTurn)
        {
            if (fromRow < 0 || fromRow > 9 || fromCol < 0 || fromCol > 8 ||
                toRow < 0 || toRow > 9 || toCol < 0 || toCol > 8)
                return false;

            Piece? piece = board.grid[fromRow, fromCol];
            if (piece == null || piece.IsRed != isRedTurn)
                return false;

            Piece? target = board.grid[toRow, toCol];
            if (target != null && target.IsRed == isRedTurn)
                return false;

            if (!IsValidMove(board, piece, fromRow, fromCol, toRow, toCol))
                return false;

            // Kiểm tra nước đi có khiến Tướng phe mình bị chiếu không (nước đi tự sát / bị chặn)
            // Thử nghiệm di chuyển trực tiếp trên bàn cờ thay vì Clone() để tối ưu bộ nhớ
            Piece? originalTarget = board.grid[toRow, toCol];
            board.grid[toRow, toCol] = piece;
            board.grid[fromRow, fromCol] = null;
            piece.Row = toRow;
            piece.Col = toCol;

            bool isSelfCheck = ChessRules.IsUnderCheck(board, isRedTurn);

            if (isSelfCheck)
            {
                // Hoàn tác nếu nước đi khiến Tướng phe mình bị chiếu
                board.grid[fromRow, fromCol] = piece;
                board.grid[toRow, toCol] = originalTarget;
                piece.Row = fromRow;
                piece.Col = fromCol;
                return false;
            }

            return true;
        }

        public static bool IsValidMove(Board board, Piece piece, int fromRow, int fromCol, int toRow, int toCol)
        {
            if (fromRow == toRow && fromCol == toCol) return false;

            int dRow = toRow - fromRow;
            int dCol = toCol - fromCol;
            int absRow = Math.Abs(dRow);
            int absCol = Math.Abs(dCol);

            switch (piece.Type)
            {
                case PieceType.General:
                    // Tướng: chỉ đi trong cung (hàng 0-2 hoặc 7-9, cột 3-5), đi 1 ô ngang hoặc dọc
                    if (piece.IsRed)
                    {
                        if (toRow < 7 || toRow > 9 || toCol < 3 || toCol > 5) return false;
                    }
                    else
                    {
                        if (toRow < 0 || toRow > 2 || toCol < 3 || toCol > 5) return false;
                    }
                    if (absRow + absCol != 1) return false;
                    return true;

                case PieceType.Advisor:
                    // Sĩ: đi chéo 1 ô trong cung
                    if (piece.IsRed)
                    {
                        if (toRow < 7 || toRow > 9 || toCol < 3 || toCol > 5) return false;
                    }
                    else
                    {
                        if (toRow < 0 || toRow > 2 || toCol < 3 || toCol > 5) return false;
                    }
                    return absRow == 1 && absCol == 1;

                case PieceType.Elephant:
                    // Tượng: đi chéo 2 ô, không qua sông, không bị cản mắt tượng
                    if (piece.IsRed && toRow < 5) return false;
                    if (!piece.IsRed && toRow > 4) return false;
                    if (absRow != 2 || absCol != 2) return false;
                    int eyeRow = fromRow + dRow / 2;
                    int eyeCol = fromCol + dCol / 2;
                    if (board.grid[eyeRow, eyeCol] != null) return false; // Cản mắt tượng
                    return true;

                case PieceType.Horse:
                    // Mã: đi chữ L (2 dọc 1 ngang hoặc 2 ngang 1 dọc), không bị cản chân mã
                    if (!((absRow == 2 && absCol == 1) || (absRow == 1 && absCol == 2)))
                        return false;
                    int blockRow = absRow == 2 ? fromRow + dRow / 2 : fromRow;
                    int blockCol = absCol == 2 ? fromCol + dCol / 2 : fromCol;
                    if (board.grid[blockRow, blockCol] != null) return false; // Cản chân mã
                    return true;

                case PieceType.Rook:
                    // Xe: đi ngang hoặc dọc không giới hạn, đường đi phải trống
                    if (dRow != 0 && dCol != 0) return false;
                    return CountPiecesBetween(board, fromRow, fromCol, toRow, toCol) == 0;

                case PieceType.Cannon:
                    // Pháo: đi như Xe khi không ăn quân; nhảy qua đúng 1 quân khi ăn quân
                    if (dRow != 0 && dCol != 0) return false;
                    int count = CountPiecesBetween(board, fromRow, fromCol, toRow, toCol);
                    if (board.grid[toRow, toCol] == null)
                        return count == 0;
                    else
                        return count == 1;

                case PieceType.Soldier:
                    // Tốt:
                    // Đỏ (đi từ dưới lên: toRow < fromRow)
                    // Đen (đi từ trên xuống: toRow > fromRow)
                    if (piece.IsRed)
                    {
                        if (dRow > 0) return false; // Không được lùi
                        if (fromRow >= 5) // Chưa qua sông: chỉ đi thẳng 1 bước
                            return dRow == -1 && dCol == 0;
                        else // Đã qua sông: đi thẳng hoặc ngang 1 bước
                            return (dRow == -1 && dCol == 0) || (dRow == 0 && absCol == 1);
                    }
                    else
                    {
                        if (dRow < 0) return false; // Không được lùi
                        if (fromRow <= 4) // Chưa qua sông: chỉ đi thẳng 1 bước
                            return dRow == 1 && dCol == 0;
                        else // Đã qua sông: đi thẳng hoặc ngang 1 bước
                            return (dRow == 1 && dCol == 0) || (dRow == 0 && absCol == 1);
                    }

                default:
                    return false;
            }
        }

        public static int CountPiecesBetween(Board board, int r1, int c1, int r2, int c2)
        {
            int count = 0;
            if (r1 == r2)
            {
                int min = Math.Min(c1, c2) + 1;
                int max = Math.Max(c1, c2);
                for (int c = min; c < max; c++)
                    if (board.grid[r1, c] != null) count++;
            }
            else if (c1 == c2)
            {
                int min = Math.Min(r1, r2) + 1;
                int max = Math.Max(r1, r2);
                for (int r = min; r < max; r++)
                    if (board.grid[r, c1] != null) count++;
            }
            return count;
        }
    }
}

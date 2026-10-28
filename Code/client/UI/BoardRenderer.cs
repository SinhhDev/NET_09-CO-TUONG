using System;
using System.Collections.Generic;
using System.Drawing;
using CoTuongOnline.Common;

namespace CoTuongOnline.Client
{
    /// <summary>
    /// Lớp tiện ích dựng hình (render) bàn cờ và quân cờ tướng dùng chung cho FormGame và FormReplay
    /// </summary>
    public static class BoardRenderer
    {
        // GDI+ cached resources dùng chung
        public static readonly Brush BoardBrush = new SolidBrush(Color.FromArgb(255, 222, 173));
        public static readonly Pen BlackPen2 = new Pen(Color.Black, 2);
        public static readonly Font RiverFont = new Font("Microsoft YaHei", 22f, FontStyle.Bold);
        public static readonly Brush RiverBrush = new SolidBrush(Color.FromArgb(220, 95, 30, 20));
        public static readonly Brush LastMoveBrush = new SolidBrush(Color.FromArgb(50, 30, 130, 245));
        public static readonly Pen LastMovePen = new Pen(Color.FromArgb(220, 40, 120, 230), 2.5f);

        // Ánh xạ loại quân cờ sang tên file hình ảnh
        private static readonly Dictionary<PieceType, string> PieceNameMap = new()
        {
            { PieceType.General,  "vua"   },
            { PieceType.Advisor,  "si"    },
            { PieceType.Elephant, "tuong" },
            { PieceType.Horse,    "ma"    },
            { PieceType.Rook,     "xe"    },
            { PieceType.Cannon,   "phao"  },
            { PieceType.Soldier,  "tot"   },
        };

        // 14 điểm đánh dấu Pháo và Tốt trên bàn cờ
        private static readonly int[][] Marks = {
            new[]{1,2}, new[]{7,2},
            new[]{0,3}, new[]{2,3}, new[]{4,3}, new[]{6,3}, new[]{8,3},
            new[]{1,7}, new[]{7,7},
            new[]{0,6}, new[]{2,6}, new[]{4,6}, new[]{6,6}, new[]{8,6}
        };

        public static int ToScreenRow(int row, bool flipBoard) => flipBoard ? (9 - row) : row;
        public static int ToScreenCol(int col, bool flipBoard) => flipBoard ? (8 - col) : col;
        public static int ToBoardRow(int screenRow, bool flipBoard) => flipBoard ? (9 - screenRow) : screenRow;
        public static int ToBoardCol(int screenCol, bool flipBoard) => flipBoard ? (8 - screenCol) : screenCol;

        public static string GetPieceKey(PieceType type, bool isRed)
        {
            return $"{PieceNameMap[type]}_{(isRed ? "do" : "den")}";
        }

        /// <summary>
        /// Vẽ toàn bộ khung lưới bàn cờ (nền, đường kẻ, cung Tướng, khúc sông, dấu định vị)
        /// </summary>
        public static void DrawBoard(Graphics g, int startX, int startY, int cell, bool flipBoard)
        {
            // 1. Nền bàn cờ
            g.FillRectangle(BoardBrush, startX, startY, 8 * cell, 9 * cell);
            g.DrawRectangle(BlackPen2, startX, startY, 8 * cell, 9 * cell);

            // 2. Các đường kẻ ngang (1 đến 8)
            for (int i = 1; i < 9; i++)
                g.DrawLine(BlackPen2, startX, startY + i * cell, startX + 8 * cell, startY + i * cell);

            // 3. Các đường kẻ dọc (0 đến 8, đứt đoạn ở khúc sông)
            for (int j = 0; j <= 8; j++)
            {
                if (j == 0 || j == 8)
                    g.DrawLine(BlackPen2, startX + j * cell, startY, startX + j * cell, startY + 9 * cell);
                else
                {
                    g.DrawLine(BlackPen2, startX + j * cell, startY, startX + j * cell, startY + 4 * cell);
                    g.DrawLine(BlackPen2, startX + j * cell, startY + 5 * cell, startX + j * cell, startY + 9 * cell);
                }
            }

            // 4. Cung Tướng (đường chéo X)
            g.DrawLine(BlackPen2, startX + 3 * cell, startY, startX + 5 * cell, startY + 2 * cell);
            g.DrawLine(BlackPen2, startX + 5 * cell, startY, startX + 3 * cell, startY + 2 * cell);
            g.DrawLine(BlackPen2, startX + 3 * cell, startY + 7 * cell, startX + 5 * cell, startY + 9 * cell);
            g.DrawLine(BlackPen2, startX + 5 * cell, startY + 7 * cell, startX + 3 * cell, startY + 9 * cell);

            // 5. Dấu vị trí Pháo, Tốt
            foreach (var m in Marks)
                DrawMark(g, BlackPen2, startX, startY, cell, m[0], m[1]);

            // 6. Dòng chữ Sở Hà - Hán Giới (Căn giữa chuẩn vào các cột 1, 3, 5, 7, hoàn toàn không chạm quân cờ ở cột 0, 2, 4, 6, 8)
            var prevHint = g.TextRenderingHint;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            string w1 = flipBoard ? "漢" : "楚";
            string w2 = flipBoard ? "界" : "河";
            string w3 = flipBoard ? "楚" : "漢";
            string w4 = flipBoard ? "河" : "界";

            float riverY = startY + 4.5f * cell;
            g.DrawString(w1, RiverFont, RiverBrush, startX + 1 * cell, riverY, sf);
            g.DrawString(w2, RiverFont, RiverBrush, startX + 3 * cell, riverY, sf);
            g.DrawString(w3, RiverFont, RiverBrush, startX + 5 * cell, riverY, sf);
            g.DrawString(w4, RiverFont, RiverBrush, startX + 7 * cell, riverY, sf);

            g.TextRenderingHint = prevHint;
        }

        /// <summary>
        /// Vẽ dấu góc vuông định vị tại các giao điểm Pháo và Tốt
        /// </summary>
        public static void DrawMark(Graphics g, Pen pen, int startX, int startY, int cell, int col, int row)
        {
            int x = startX + col * cell;
            int y = startY + row * cell;
            int arm = cell / 6;
            int offset = cell / 8;
            if (col > 0 && row > 0) { g.DrawLine(pen, x - offset, y - offset, x - offset - arm, y - offset); g.DrawLine(pen, x - offset, y - offset, x - offset, y - offset - arm); }
            if (col < 8 && row > 0) { g.DrawLine(pen, x + offset, y - offset, x + offset + arm, y - offset); g.DrawLine(pen, x + offset, y - offset, x + offset, y - offset - arm); }
            if (col > 0 && row < 9) { g.DrawLine(pen, x - offset, y + offset, x - offset - arm, y + offset); g.DrawLine(pen, x - offset, y + offset, x - offset, y + offset + arm); }
            if (col < 8 && row < 9) { g.DrawLine(pen, x + offset, y + offset, x + offset + arm, y + offset); g.DrawLine(pen, x + offset, y + offset, x + offset, y + offset + arm); }
        }

        /// <summary>
        /// Vẽ 4 góc vuông định vị (Corner Brackets ┌ ┐ └ ┘) quanh tâm (cx, cy)
        /// </summary>
        public static void DrawCornerBrackets(Graphics g, Pen pen, int cx, int cy, int size, int arm)
        {
            int half = size / 2;
            int left = cx - half;
            int right = cx + half;
            int top = cy - half;
            int bottom = cy + half;

            // Góc trên - trái ┌
            g.DrawLine(pen, left, top, left + arm, top);
            g.DrawLine(pen, left, top, left, top + arm);

            // Góc trên - phải ┐
            g.DrawLine(pen, right, top, right - arm, top);
            g.DrawLine(pen, right, top, right, top + arm);

            // Góc dưới - trái └
            g.DrawLine(pen, left, bottom, left + arm, bottom);
            g.DrawLine(pen, left, bottom, left, bottom - arm);

            // Góc dưới - phải ┘
            g.DrawLine(pen, right, bottom, right - arm, bottom);
            g.DrawLine(pen, right, bottom, right, bottom - arm);
        }

        /// <summary>
        /// Vẽ một quân cờ lên màn hình tại tọa độ screenCol, screenRow chuẩn tâm giao điểm
        /// </summary>
        public static void DrawPiece(Graphics g, Image img, int startX, int startY, int cell, int screenCol, int screenRow)
        {
            int size = cell - 6;
            int cx = startX + screenCol * cell;
            int cy = startY + screenRow * cell;
            g.DrawImage(img, cx - size / 2, cy - size / 2, size, size);
        }

        /// <summary>
        /// Tô viền/dấu nhận diện nước đi vừa đi (from -> to)
        /// - isDestination = false (ô xuất phát): vẽ chấm tròn xanh tinh tế tại tâm giao điểm và vạch định vị nhẹ, không vẽ vòng tròn to gây rối bàn cờ
        /// - isDestination = true (ô đích đến): vẽ vầng sáng nhẹ phía sau quân cờ và 4 góc vuông ôm khít quân cờ
        /// </summary>
        public static void DrawLastMoveHighlight(Graphics g, int startX, int startY, int cell, int screenCol, int screenRow, bool isDestination = true)
        {
            int cx = startX + screenCol * cell;
            int cy = startY + screenRow * cell;

            if (isDestination)
            {
                // 1. Vầng sáng nhẹ phía sau quân cờ đích đến
                int r = (cell - 4) / 2;
                g.FillEllipse(LastMoveBrush, cx - r, cy - r, r * 2, r * 2);

                // 2. Khung 4 góc vuông bo ôm quân cờ
                DrawCornerBrackets(g, LastMovePen, cx, cy, cell - 2, 10);
            }
            else
            {
                // Ô xuất phát: vẽ điểm chấm xuất phát tinh gọn ở tâm giao điểm
                int dotR = 6;
                using var dotBrush = new SolidBrush(Color.FromArgb(170, 30, 130, 245));
                using var dotBorderPen = new Pen(Color.FromArgb(230, 30, 130, 245), 2f);
                g.FillEllipse(dotBrush, cx - dotR, cy - dotR, dotR * 2, dotR * 2);
                g.DrawEllipse(dotBorderPen, cx - dotR, cy - dotR, dotR * 2, dotR * 2);

                // 4 vạch góc nhỏ thanh mảnh chỉ vị trí ô cũ
                using var faintPen = new Pen(Color.FromArgb(120, 30, 130, 245), 1.5f);
                DrawCornerBrackets(g, faintPen, cx, cy, cell - 12, 6);
            }
        }
    }
}

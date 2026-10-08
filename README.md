# NET_09_CO-TUONG

# ♟️️ Trò Chơi Cờ Tướng Online

Đồ Án Lập Trình Mạng — Lớp: [012012301303]

Trường Đại học Giao Thông Vận Tải TP. Hồ Chí Minh

---

## 👥 Thông Tin Nhóm

| STT | Họ và Tên      | MSSV | 
| --- | -------------- | ---- | 
| 1   | Ngô Quốc An    |      |         
| 2   | Trần Thế Hiển  |      |         
| 3   | Võ Duy Hưng    |      |         
| 4   | Đỗ Chí Kiên    |      |         
| 5   | Lê Trọng Nghĩa |      |         
| 6   | Trần Nhật Sinh |      |        

---

## 📌 Giới Thiệu Đề Tài

Cờ Tướng (Tượng Kỳ) là trò chơi trí tuệ có nguồn gốc từ Trung Quốc, phổ biến rộng rãi tại Việt Nam và các nước châu Á. Trò chơi được chơi bởi hai người trên bàn cờ 9 cột × 10 hàng, mỗi bên có 16 quân với các vai trò khác nhau.

---

## 🎮 Tính Năng Chính

| Tính năng                        | Mô tả                                |
| -------------------------------- | ------------------------------------ |
| Kết nối mạng P2P / Client-Server | Hai người chơi kết nối qua TCP/UDP   |
| Giao diện đồ họa                 | Hiển thị bàn cờ và quân cờ trực quan |
| Kiểm tra nước đi hợp lệ          | Luật di chuyển của từng loại quân    |
| Phát hiện chiếu tướng / chiếu bí | Xác định điều kiện thắng/thua        |
| Chat trong game                  | Nhắn tin giữa hai người chơi         |
| Đồng hồ thi đấu                  | Giới hạn thời gian mỗi lượt          |

---

## 🗂️ Cấu Trúc Repository

```
co-tuong-online/
├── src/                        # Toàn bộ mã nguồn
│   ├── server/                 # Code phía Server
│   │   └── ...
│   ├── client/                 # Code phía Client
│   │   └── ...
│   └── common/                 # Logic dùng chung (luật cờ, v.v.)
│       └── ...
├── docs/                       # Tài liệu
│   ├── Bao_cao.docx            # Báo cáo Word
│   └── Thuyet_trinh.pptx       # PowerPoint thuyết trình
├── reports/                    
│   └── Phan_cong.xlsx          # Bảng phân công nhiệm vụ
├── assets/                     
│   └── images/                 # Hình ảnh quân cờ, bàn cờ
├── .gitignore
└── README.md
```

---

_Network Programming Project — Ho Chi Minh City University of Transport_

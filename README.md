# NET_09_CO-TUONG

# ♟️️ Trò Chơi Cờ Tướng Trực Tuyến

**Đồ Án Lập Trình Mạng** — Lớp: [012012301303]

*Trường Đại học Giao Thông Vận Tải TP. Hồ Chí Minh*

---

## 👥 Thông Tin Nhóm

| STT | Họ và Tên      | MSSV | 
| --- | -------------- | ---- | 
| 1   | Ngô Quốc An    |   082206003598   |         
| 2   | Trần Thế Hiển  |   091207016773   |         
| 3   | Võ Duy Hưng    |   075206008335   |         
| 4   | Đỗ Chí Kiên    |   052206007333   |         
| 5   | Lê Trọng Nghĩa |   079207027585   |         
| 6   | Trần Nhật Sinh |   045206007877   |        

---

## 📌 Giới Thiệu Đề Tài

**Cờ Tướng** (Tượng Kỳ) là trò chơi trí tuệ có nguồn gốc từ Trung Quốc, phổ biến rộng rãi tại Việt Nam và các nước châu Á. Trò chơi được chơi bởi hai người trên bàn cờ 9 cột × 10 hàng, mỗi bên có 16 quân với các vai trò khác nhau.

Dự án xây dựng ứng dụng chơi Cờ Tướng qua mạng theo mô hình **Client-Server** sử dụng giao thức **TCP thuần** với giao thức tự định nghĩa.

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
NET_09_CO-TUONG/
├── Code/                       # Toàn bộ mã nguồn dự án
│   ├── client/                 # Mã nguồn phía Client
│   ├── common/                 # Logic dùng chung (luật cờ, model...)
│   └── server/                 # Mã nguồn phía Server
├── DOCX/                       # Tài liệu báo cáo (Word)
├── Extra/                      # Thông tin bổ sung
│   ├── images/                 # Hình ảnh quân cờ, bàn cờ, ảnh minh họa
│   └── tasks/                  # Các file phân công nhiệm vụ
├── PPTX/                       # Tài liệu thuyết trình (PowerPoint)
├── .gitignore                  # File cấu hình bỏ qua các file không cần thiết trên Git
└── README.md                   # File giới thiệu dự án
```

---

_Network Programming Project — Ho Chi Minh City University of Transport_

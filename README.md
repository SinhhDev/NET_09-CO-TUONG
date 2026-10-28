# NET_09_CO-TUONG — GAME CỜ TƯỚNG TRỰC TUYẾN

> **Trường Đại học Giao Thông Vận Tải TP. Hồ Chí Minh (UTH)**   
> **ĐỒ ÁN MÔN HỌC: LẬP TRÌNH MẠNG**  
> **Lớp học phần:** `012012301303`  

---

## 📌 THÔNG TIN ĐỀ TÀI

- **Tên đề tài:** Game Cờ Tướng trực tuyến
- **Mã đề tài:** `UDM_18`
- **Mô tả ngắn gọn:** Ứng dụng Desktop chơi Cờ Tướng hai người qua Server trung tâm theo thời gian thực (Real-time), hỗ trợ quản lý phòng đấu độc lập, xác thực nước đi phía máy chủ (Authoritative Server), tính giờ suy nghĩ cho từng lượt, hệ thống chat phòng có lọc spam và từ ngữ nhạy cảm, chức năng xin hòa / đầu hàng / xem lại ván đấu (Replay) và tự động thu hồi tài nguyên an toàn khi mất kết nối.
- **Mã nguồn (GitHub Repository):** [https://github.com/SinhhDev/NET_09-CO-TUONG](https://github.com/SinhhDev/NET_09-CO-TUONG)

---

## 👥 DANH SÁCH THÀNH VIÊN VÀ PHÂN CÔNG NHIỆM VỤ

| STT | Họ và Tên | MSSV | Vai Trò | Nhiệm Vụ Phụ Trách | Sản Phẩm Bàn Giao | Đóng Góp |
| :---: | :--- | :---: | :--- | :--- | :--- | :---: |
| 1 | **Trần Nhật Sinh** | `045206007877` | **Nhóm trưởng** / Server Core Architect | • Thiết kế kiến trúc Server đa luồng (Multi-threading)<br>• Xây dựng hệ thống ghép phòng (`RoomManager`) & phòng đấu độc lập (`GameRoom`)<br>• Quản lý `TcpListener`, mở cổng kết nối & hệ thống ghi log (`Logger`) | `ServerManager.cs`<br>`RoomManager.cs`<br>`GameRoom.cs`<br>`Logger.cs` | 100% |
| 2 | **Lê Trọng Nghĩa** | `079207027585` | Client Network Engineer | • Xây dựng tầng Socket Client bất đồng bộ (`SocketClient`)<br>• Hiện thực bộ giải mã gói tin chống dính/xé gói (`PacketParser`)<br>• Lớp trung gian cầu nối sự kiện mạng sang UI (`ClientEventListener`)<br>• Xử lý bắt lỗi rớt mạng và cơ chế Auto-Reconnect | `SocketClient.cs`<br>`PacketParser.cs`<br>`ClientEventListener.cs` | 100% |
| 3 | **Võ Duy Hưng** | `075206008335` | UI/UX Designer & Graphics | • Thiết kế toàn bộ giao diện WinForms (`FormMenu`, `FormGame`, `FormResult`, `FormReplay`)<br>• Vẽ bàn cờ chuẩn GDI+ (`BoardRenderer`), highlight ô cờ, hiệu ứng chọn quân và gợi ý nước đi<br>• Tích hợp bộ âm thanh di chuyển quân / ăn quân (`SoundManager`) | `FormGame.cs`<br>`FormMenu.cs`<br>`BoardRenderer.cs`<br>`SoundManager.cs`<br>`Bộ tài nguyên ảnh/âm` | 100% |
| 4 | **Đỗ Chí Kiên** | `052206007333` | Game Logic & Rule Engine | • Xây dựng cấu trúc dữ liệu bàn cờ (`Board`, `Piece`)<br>• Hiện thực luật di chuyển của 7 loại quân cờ: Tướng, Sĩ, Tượng, Xe, Pháo, Mã, Tốt<br>• Xử lý cản chân Mã, cản mắt Tượng, ngòi Pháo, qua sông, cung Tướng<br>• Thuật toán kiểm tra Chiếu tướng, Chiếu bí (`ChessRules`, `MoveValidator`) | `Board.cs`<br>`Piece.cs`<br>`ChessRules.cs`<br>`MoveValidator.cs` | 100% |
| 5 | **Ngô Quốc An** | `082206003598` | Protocol & Sync Timer | • Thiết kế chuẩn giao thức mạng nhị phân Big-Endian (`Protocol`, `PacketType`)<br>• Xây dựng đồng hồ đếm ngược thời gian suy nghĩ 45s mỗi lượt (`GameTimer`)<br>• Đồng bộ hóa thời gian thi đấu giữa 2 máy khách, xử lý hết giờ (Timeout) | `Protocol.cs`<br>`GameTimer.cs` | 100% |
| 6 | **Trần Thế Hiển** | `091207016773` | User & Chat  | • Quản lý người dùng, đăng ký Username và danh sách Online (`UserManager`)<br>• Xây dựng khung chat thời gian thực, chống spam tin nhắn, lọc từ ngữ thô tục (`ChatHandler`) | `UserManager.cs`<br>`ChatHandler.cs`<br>`ChatBox.cs` | 100% |

---

## 🏛️ KIẾN TRÚC HỆ THỐNG VÀ GIAO TIẾP MẠNG

### 1. Mô hình kiến trúc: Client - Server (Authoritative Server)

Ứng dụng hoạt động theo mô hình **Client - Server tập trung** qua kết nối **TCP Socket thuần** (`System.Net.Sockets`), đa luồng bất đồng bộ (`async`/`await`):
- **Server (Authoritative Trọng tài):** Lắng nghe tại cổng `54000`, quản lý danh sách người dùng (`UserManager`), hàng chờ ghép phòng (`RoomManager`), phòng đấu độc lập (`GameRoom`) và trực tiếp thẩm định tính hợp lệ của mọi nước đi (`MoveValidator`, `ChessRules`) trước khi chuyển tiếp.
- **Client (Giao diện & Tương tác):** Hiển thị đồ họa WinForms GDI+, tính toán gợi ý nước đi, gửi nhận dữ liệu bất đồng bộ (`SocketClient`, `PacketParser`, `ClientEventListener`) và tự động lật ngược góc nhìn bàn cờ khi cầm quân Đen.

### 2. Giao thức truyền thông mạng (Custom TCP Protocol)

Để chống hiện tượng **dính gói (packet concatenation)** và **xé gói (packet fragmentation)** trên stream TCP, hệ thống sử dụng cơ chế đóng khung Header cố định:

> **Cấu trúc gói tin:** `[ 1 byte PacketType ] [ 4 bytes Payload Length (Big-Endian) ] [ N bytes Payload (UTF-8) ]`

| Nhóm Gói Tin | PacketType (Mã) | Payload | Ý Nghĩa / Mục Đích |
| :--- | :---: | :--- | :--- |
| **Xác thực** | `Login` (10) / `LoginOK` (11) / `LoginFail` (12) | Username / Thông báo | Đăng ký tên người chơi & vào hàng chờ |
| **Trận đấu** | `GameStart` (50) | `COLOR\|Tên_đối_thủ` | Server ghép cặp & phân vai Đỏ (đi trước) / Đen (đi sau) |
| | `Move` (31) | `fromRow,fromCol,toRow,toCol` | Gửi nước đi (Server kiểm tra luật và chuyển tiếp) |
| | `Timeout` (34) | Trống | Báo hết 45s thời gian suy nghĩ của lượt đi |
| | `DrawOffer` (35) / `DrawResponse` (36) | Trống / `ACCEPT` hoặc `REJECT` | Đề nghị xin hòa cờ & phản hồi từ đối thủ |
| | `Surrender` (32) / `GameEnd` (60) | Lý do kết thúc | Đầu hàng / Kết thúc ván (chiếu bí, mất kết nối) |
| **Giao tiếp** | `Chat` (33) | Nội dung tin nhắn | Chat thời gian thực (Server lọc spam & từ ngữ cấm) |

---

## 💻 YÊU CẦU MÔI TRƯỜNG VÀ CÔNG CỤ PHÁT TRIỂN

| Thành phần | Yêu cầu tối thiểu / Khuyến nghị |
| :--- | :--- |
| **Hệ điều hành** | Windows 10 hoặc Windows 11 (64-bit) |
| **Nền tảng (.NET)** | **.NET 10.0 SDK** (phiên bản `10.0.100` trở lên) hoặc tương thích hoàn toàn với **.NET 8.0 / .NET 9.0 SDK** |
| **Giao diện (UI)** | Windows Forms (WinForms) với tính năng `UseWindowsForms=true` |
| **Môi trường IDE** | • **Visual Studio 2022** (v17.10 trở lên) — Cài workload *.NET Desktop Development*<br>• Hoặc **Visual Studio Code** (Cài extension *C# Dev Kit* / *OmniSharp*) |
| **Phần cứng** | CPU 2 Cores 2.0 GHz trở lên, RAM tối thiểu 4 GB, Dung lượng trống 300 MB |
| **Mạng** | Hỗ trợ cổng TCP cục bộ (Local Loopback `127.0.0.1`) hoặc kết nối mạng nội bộ (LAN / Wi-Fi) giữa các máy |

---

## ⚙️ CẤU HÌNH HỆ THỐNG

Ứng dụng được thiết kế linh hoạt, **tuyệt đối không hard-code** địa chỉ IP hoặc cổng mạng cho riêng một máy tính cụ thể:

1. **Cấu hình Địa chỉ IP và Cổng mạng (Port):**
   - **Phía Server (`ServerManager.cs`):**
     - Mặc định lắng nghe trên mọi giao diện mạng của máy tính: `IPAddress.Any` tại cổng `54000`.
     - Cho phép nhận kết nối đồng thời từ localhost (`127.0.0.1`) và từ các máy tính khác trong cùng dải mạng LAN (`192.168.x.x`).
   - **Phía Client (`FormGame.cs`):**
     - Khi bấm nút **"Chơi Online"** hoặc **"Thách Đấu"**, một hộp thoại cấu hình sẽ xuất hiện cho phép người dùng tự do điền:
       - **Địa chỉ IP Server:** Mặc định là `127.0.0.1` khi chạy trên cùng máy tính, hoặc nhập IP LAN của máy chủ (ví dụ: `192.168.1.15`).
       - **Tên người chơi:** Nhập tùy ý từ 3 đến 20 ký tự (không chứa ký tự `|` hoặc ký tự xuống dòng).

2. **Cấu hình Luật chơi và Thời gian suy nghĩ:**
   - **Thời gian cho mỗi lượt (`GameTimer.TurnDurationSeconds`):** Mặc định là **`45` giây/lượt**. Hết 45 giây mà người chơi chưa thực hiện nước đi sẽ bị tính thua cuộc do hết giờ.
   - **Grace Period (Thời gian bù trễ mạng):** Phía đối thủ có thêm **`5` giây** dự phòng trước khi chốt thắng thua nhằm tránh tình trạng mất đồng bộ do mạng lag.

3. **Cấu hình Bộ lọc Chat và Chống Spam (`ChatHandler.cs`):**
   - **Tần suất gửi tin:** Tối thiểu cách nhau `700ms` giữa 2 tin nhắn liên tiếp.
   - **Giới hạn số lượng:** Tối đa `4` tin nhắn trong vòng `5` giây. Nếu vượt quá sẽ bị **khóa chat tạm thời (Mute) trong 10 giây**.
   - **Độ dài tối đa:** `200` ký tự cho mỗi tin nhắn.
   - **Lọc từ ngữ cấm (Censorship):** Tự động che dấu / ngăn chặn các từ khóa xúc phạm, thô tục và ngăn chặn tin nhắn giả mạo hệ thống (`[Hệ thống]`, `[Server]`).

4. **Cấu hình Nhật ký Hệ thống (Logging):**
   - File log được tự động tạo và lưu trữ tại `server_log.txt`.
   - Ghi lại đầy đủ: Dấu thời gian `[dd/MM/yyyy HH:mm:ss]`, IP và Port của Client kết nối/ngắt kết nối, định danh gói tin, cảnh báo nước đi sai lượt / sai luật cờ và kết quả ván đấu.

---

## 🚀 HƯỚNG DẪN CÀI ĐẶT VÀ CHẠY ỨNG DỤNG

### Cách 1: Khởi chạy bằng .NET CLI (Dòng lệnh - Khuyên dùng)

Mở **PowerShell** hoặc **Command Prompt** tại thư mục gốc của đồ án (`NET_09-CO-TUONG`):

#### Bước 1: Khôi phục dependencies và kiểm tra mã nguồn
```powershell
dotnet restore
```

#### Bước 2: Khởi động Server Cờ Tướng
Mở một cửa sổ Terminal riêng và chạy:
```powershell
dotnet run --project Code/server/server.csproj
```
> Khi Server khởi động thành công, màn hình sẽ hiển thị:  
> `=== SERVER CỜ TƯỚNG ĐÃ MỞ TẠI CỔNG 54000 ===`

#### Bước 3: Khởi động Client 1 (Người chơi 1)
Mở một cửa sổ Terminal thứ hai và chạy:
```powershell
dotnet run --project Code/client/client.csproj
```
- Trên menu chính, nhấn chọn **"Chơi Online"**.
- Nhập IP Server (`127.0.0.1` nếu cùng máy) và nhập Tên người chơi (ví dụ: `KyThu_Do`).
- Nhấn **"Vào Trận"** ➔ Ứng dụng kết nối thành công và chuyển sang trạng thái: `⏳ Đang chờ đối thủ...`

#### Bước 4: Khởi động Client 2 (Người chơi 2)
Mở một cửa sổ Terminal thứ ba và chạy:
```powershell
dotnet run --project Code/client/client.csproj --no-build
```
> **Lưu ý:** Thêm cờ `--no-build` để không bị xung đột khóa file (`file locked`) khi Client 1 đang chạy. Bạn cũng có thể mở thêm Client bằng cách nhấp đúp file `client.exe` tại thư mục `Code/client/bin/Debug/net10.0-windows/`.

- Nhấn chọn **"Chơi Online"**.
- Nhập IP Server (`127.0.0.1`) và nhập Tên người chơi (ví dụ: `KyThu_Den`).
- Nhấn **"Vào Trận"** ➔ Ngay lập tức Server ghép cặp 2 người chơi vào một phòng:
  - **Client 1:** Nhận quân **Đỏ (đi trước)**, bàn cờ giữ nguyên góc nhìn truyền thống.
  - **Client 2:** Nhận quân **Đen (đi sau)**, bàn cờ **tự động lật ngược góc nhìn** để phe Đen nhìn từ dưới lên.
  - Đồng hồ 45s bắt đầu đếm ngược cho lượt đi của Đỏ!

---

### Cách 2: Khởi chạy bằng Microsoft Visual Studio 2022

1. Mở file giải pháp [`NET_09-CO-TUONG.sln`](file:///c:/UTH%20m%C3%A3i%20%C4%91%E1%BB%89nh/L%E1%BA%ADp%20tr%C3%ACnh%20M%E1%BA%A1ng/Project/NET_09-CO-TUONG/NET_09-CO-TUONG.sln).
2. Nhấp chuột phải vào `Solution 'NET_09-CO-TUONG'` trong cửa sổ *Solution Explorer* ➔ Chọn **Configure Startup Projects...**
3. Chọn mục **Multiple startup projects**:
   - `server` ➔ Action: **Start**
   - `client` ➔ Action: **Start**
4. Nhấn **F5** (hoặc nút **Start**) để khởi chạy đồng thời cả Server và Client.
5. Để mở thêm Client thứ hai: Nhấp chuột phải vào dự án `client` ➔ **Debug** ➔ **Start New Instance**.

---

### Cách 3: Chơi 2 máy tính khác nhau qua mạng LAN (Wi-Fi nội bộ)

1. **Trên máy chủ (Máy chạy Server):**
   - Mở CMD/PowerShell gõ `ipconfig` để lấy địa chỉ IPv4 (ví dụ: `192.168.1.20`).
   - Mở tường lửa (Windows Firewall) cho phép cổng TCP `54000` (Inbound Rule) hoặc cho phép ứng dụng `server.exe`.
   - Khởi động Server.
2. **Trên máy khách (Máy người chơi):**
   - Mở ứng dụng Client.
   - Nhấn **"Chơi Online"** ➔ Nhập địa chỉ IP của máy chủ (ví dụ: `192.168.1.20`) và tên người chơi.
   - Nhấn **"Vào Trận"** để kết nối và thi đấu liên máy!
---

## 🗂️ CẤU TRÚC THƯ MỤC DỰ ÁN

Tuân thủ đúng quy định cấu trúc Repository môn học:

```
NET_09-CO-TUONG/
├── Code/                               # Toàn bộ mã nguồn dự án
│   ├── client/                         # Dự án WinForms Client
│   │   ├── Network/                    # Tầng mạng & giao thức phía Client
│   │   │   ├── ClientEventListener.cs  # Chuyển đổi gói tin sang sự kiện C# cho giao diện (UI)
│   │   │   ├── PacketParser.cs         # Bộ giải mã framing, chống dính gói / xé gói TCP
│   │   │   └── SocketClient.cs         # Quản lý kết nối TCP Socket, nhận/gửi async thread-safe
│   │   ├── Sounds/                     # File âm thanh nước đi
│   │   ├── UI/                         # Tầng giao diện người dùng (WinForms GDI+)
│   │   │   ├── BoardRenderer.cs        # Render đồ họa bàn cờ, quân cờ, hiệu ứng và lật bàn cờ
│   │   │   ├── ChatBox.cs              # Khung chat realtime & xử lý lệnh nhanh (/surrender, /draw...)
│   │   │   ├── FormGame.cs             # Màn hình bàn cờ chính, xử lý tương tác và luật chơi
│   │   │   ├── FormGame.Designer.cs    # Khởi tạo layout và thành phần đồ họa của FormGame
│   │   │   ├── FormMenu.cs             # Giao diện Menu chính (Chơi Offline, Chơi Online, Thoát)
│   │   │   ├── FormReplay.cs           # Giao diện xem lại toàn bộ nước đi của ván đấu đã kết thúc
│   │   │   ├── FormResult.cs           # Hộp thoại thông báo kết quả ván đấu (Thắng / Thua / Hòa)
│   │   │   └── SoundManager.cs         # Quản lý phát hiệu ứng âm thanh di chuyển và ăn quân
│   │   ├── Program.cs                  # Entry point của Client
│   │   └── client.csproj               # Cấu hình dự án Client (net10.0)
│   ├── common/                         # Thư viện logic dùng chung giữa Client và Server
│   │   ├── Board.cs                    # Mô hình bàn cờ 10x9
│   │   ├── ChessRules.cs               # Luật cờ tướng, kiểm tra chiếu & chiếu bí
│   │   ├── GameTimer.cs                # Đồng hồ đếm ngược thời gian suy nghĩ
│   │   ├── MoveValidator.cs            # Kiểm tra tính hợp lệ của nước đi
│   │   ├── Piece.cs                    # Mô hình quân cờ
│   │   ├── Protocol.cs                 # Giao thức truyền thông mạng Big-Endian
│   │   └── common.csproj               # Cấu hình thư viện common (net10.0)
│   └── server/                         # Dự án Console Server
│       ├── ChatHandler.cs              # Xử lý chat, chống spam và lọc từ cấm
│       ├── GameRoom.cs                 # Phòng thi đấu độc lập, Authoritative Validation
│       ├── Logger.cs                   # Hệ thống ghi log console và file
│       ├── Program.cs                  # Entry point của Server
│       ├── RoomManager.cs              # Quản lý hàng chờ và ghép cặp phòng
│       ├── ServerManager.cs            # Quản lý TcpListener, mở cổng lắng nghe kết nối
│       ├── UserManager.cs              # Quản lý danh sách người dùng Online
│       └── server.csproj               # Cấu hình dự án Server (net10.0)
├── DOCX/                               
│   └── Bao_cao.docx                    # File báo cáo
├── Extra/                              # Tài nguyên bổ sung
│   ├── images/                         # Bộ hình ảnh đồ họa 32 quân cờ tướng
│   ├── tasks/                          # Bảng phân công nhiệm vụ
│   └── TC/                             # Bảng kiểm thử (Test Cases.xlsx)
├── PPTX/                               
│   └── Thuyet_trinh.pptx               # File trình chiếu 
├── .gitignore                          # Cấu hình loại bỏ file build, cache, secret
├── NET_09-CO-TUONG.sln                 # File giải pháp Visual Studio Solution
└── README.md                           # Tài liệu tổng quan và hướng dẫn dự án
```
---

_Đồ án môn học Lập trình Mạng — Trường Đại học Giao Thông Vận Tải TP. Hồ Chí Minh (UTH)_  

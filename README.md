# Sticky Note Premium

Ứng dụng đếm ngược dạng sticky note cho Windows, viết bằng **C# + WPF + .NET 8**. Giao diện chính nằm ngang theo phong cách ảnh mẫu, nhưng phần countdown giữa đã được đổi sang **Glassmorphism**: nền bán trong suốt, viền kính và lớp blur lấy từ chính hình nền phía sau.

## Tính năng

- Quản lý **nhiều ngày lễ / sự kiện cố định một lần** trong cùng một ứng dụng.
- Chế độ **AUTO** tự chọn sự kiện tương lai gần nhất và cập nhật lại mỗi giây.
- Khi một lễ đã qua, AUTO tự chuyển sang sự kiện tiếp theo; sự kiện cũ vẫn được giữ trong danh sách để sửa hoặc xóa.
- Nút `‹` và `›` cho phép chuyển thủ công qua tất cả sự kiện theo thứ tự thời gian, kể cả sự kiện đã qua.
- Bấm nút trạng thái `THỦ CÔNG • Bấm để AUTO` để quay lại chế độ AUTO.
- Cửa sổ không viền, kéo thả tự do và có thể ghim nổi (`Always on top`).
- Đồng hồ đếm ngược thời gian thực, cập nhật mỗi giây.
- Khung countdown Glassmorphism dùng `VisualBrush` + `BlurEffect`, nền kính bán trong suốt, viền sáng và shadow mềm.
- Đổi màu nền bằng mã HEX hoặc các màu mẫu.
- Chọn ảnh nền JPG/JPEG/PNG/BMP và WebP nếu bộ giải mã ảnh của Windows hỗ trợ.
- Chỉnh độ rõ ảnh, lớp phủ tối, cách co ảnh (`UniformToFill`, `Uniform`, `Fill`) và màu chữ (`Auto`, `Light`, `Dark`).
- Tự lưu danh sách sự kiện, hình nền, vị trí và kích thước cửa sổ.
- Tự migrate cấu hình cũ chỉ có một `EventName` + `TargetDateTime` sang danh sách sự kiện mới.

## Yêu cầu để build source

Bạn **không cần Visual Studio 2022**. Chỉ cần Windows và **.NET 8 SDK**. Project target `net8.0-windows` và không dùng package NuGet bên thứ ba.

Mở PowerShell **ngay tại thư mục có file `StickyNotePremium.csproj`**, rồi chạy:

```powershell
dotnet restore .\StickyNotePremium.csproj
dotnet build .\StickyNotePremium.csproj -c Release
dotnet run --project .\StickyNotePremium.csproj -c Release
```

Nếu chạy `dotnet build` ở thư mục cha không chứa `.csproj`/`.sln`, MSBuild sẽ báo `MSB1003`. Kiểm tra nhanh bằng:

```powershell
dir *.csproj
```

## Cách dùng nhiều sự kiện

Mở `⚙` để vào phần **Sự kiện / ngày lễ**.

1. Bấm **`+ Sự kiện mới`**.
2. Nhập tên, chọn ngày và nhập giờ dạng `HH:mm`.
3. Bấm **`Lưu`**.
4. Muốn sửa: chọn sự kiện trong danh sách, sửa thông tin rồi bấm `Lưu`.
5. Muốn xóa: chọn sự kiện rồi bấm `Xóa`.

Danh sách được sắp theo ngày giờ. Sự kiện đã qua không bị xóa tự động.

### AUTO và chuyển thủ công

- `AUTO • Lễ gần nhất`: app chọn sự kiện có thời gian lớn hơn hiện tại và gần nhất.
- Khi mốc đó qua, lần refresh tiếp theo sẽ tự chọn sự kiện tương lai kế tiếp.
- Bấm `‹` hoặc `›` sẽ chuyển sang chế độ thủ công.
- Trong chế độ thủ công, app giữ nguyên sự kiện đang xem dù thời gian trôi qua.
- Bấm nút trạng thái phía dưới để trở lại AUTO.
- Nếu không còn sự kiện tương lai, đồng hồ về `00:00:00` nhưng các sự kiện cũ vẫn còn trong danh sách.

## Hình nền và Glassmorphism

Trong cài đặt:

- `Chọn hình...`: chọn ảnh nền.
- `Xóa hình nền`: quay về màu nền đơn.
- `Độ rõ hình nền`: chỉnh opacity ảnh.
- `Lớp phủ tối`: tăng khả năng đọc chữ.
- Khung countdown Glassmorphism lấy mẫu phần nền phía sau bằng WPF `VisualBrush`, sau đó dùng `BlurEffect` và lớp trắng bán trong suốt để tạo cảm giác kính liền mạch.

> **WebP:** WPF dựa vào bộ giải mã hình ảnh có trên Windows. JPG/PNG/BMP hoạt động mặc định. WebP chỉ hoạt động khi Windows/bộ codec trên máy hỗ trợ; nếu không, app dùng màu nền dự phòng và hiển thị lỗi không chặn ứng dụng.

## Dữ liệu cấu hình

Ứng dụng lưu tại:

```text
%LocalAppData%\StickyNotePremium\settings.json
```

Dữ liệu gồm danh sách `Events`, màu/ảnh nền, độ trong suốt, lớp phủ, kiểu co ảnh, màu chữ, ghim nổi, vị trí và kích thước cửa sổ.

Nếu bạn đang dùng bản cũ, file JSON có `EventName` và `TargetDateTime` sẽ được tự động migrate thành phần tử đầu tiên trong `Events` khi app khởi động.

## Publish bản chạy Windows

Bản framework-dependent `win-x64`:

```powershell
.\build-release.ps1
```

Bản self-contained, máy đích không cần cài .NET Runtime:

```powershell
.\build-release.ps1 -SelfContained
```

Output nằm trong:

```text
publish\win-x64\
```

## Kiểm tra logic

Chạy chương trình verification bằng .NET SDK:

```powershell
dotnet run --project .\verification\StickyNotePremium.Verification.csproj
```

Nó kiểm tra countdown, AUTO chọn lễ gần nhất, bỏ qua sự kiện đã hết hạn khi tự chọn, lưu/đọc nhiều sự kiện và migration cấu hình cũ.

Trong môi trường không có .NET SDK, có thể chạy static regression checks:

```powershell
python verification\verify_source.py
```

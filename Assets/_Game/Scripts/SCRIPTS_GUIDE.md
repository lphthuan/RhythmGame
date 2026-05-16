# 📁 Hướng dẫn cấu trúc thư mục Scripts

> **BẮT BUỘC ĐỌC** trước khi tạo file script mới. Đặt đúng thư mục module để không xung đột và dễ bảo trì.

---

## Quy tắc chung

1. **Đặt script đúng module** — Không tạo thư mục riêng mang tên cá nhân.
2. **Một class = một file** — Tên file phải trùng với tên class.
3. **Không tạo thư mục mới** trong module mà chưa được Lead duyệt.
4. **Script test/debug** luôn nằm trong `_Debug/`, không để lẫn với production code.

---

## Tổng quan cấu trúc

```
Scripts/
├── Common/          ← Enum, Interface, Struct dùng chung cho mọi module
├── Core/            ← Quản lý vòng đời game (GameManager, SceneLoader, ...)
├── Gameplay/        ← Logic gameplay chính (Note, NoteManager, ...)
├── Chart/           ← Dữ liệu beatmap, đọc/ghi/tạo chart
├── Scoring/         ← Judgment, Combo, Score, Accuracy
├── Input/           ← Xử lý input (touch, keyboard) trừu tượng hoá
├── Audio/           ← Quản lý âm thanh, nhạc, SFX
├── UI/              ← Giao diện người dùng (Menu, HUD, Result, ...)
├── Config/          ← ScriptableObject cấu hình game
├── Save/            ← Lưu/đọc dữ liệu người chơi, leaderboard
└── _Debug/          ← Script test, debug (KHÔNG dùng trong bản chính thức)
```

---

## Chi tiết từng module

### 📦 `Common/` — Dữ liệu dùng chung

Chứa enum, interface, struct được **≥ 2 module** sử dụng. Không đặt MonoBehaviour hay logic ở đây.

- `Enums/` — `NoteType`, `FlickDirection`, `NoteResult`, `HoldNoteState`
- `Interfaces/` — `INoteResultReceiver`
- `Data/` — `NotePointer`, `NoteRuntimeData`

---

### 🎮 `Gameplay/` — Logic gameplay chính

Toàn bộ logic gameplay cốt lõi: note, di chuyển, quản lý note.

- `Notes/` — `NoteBase` (abstract) + `TapNote`, `HoldNote`, `FlickNote`, `SlideNote`
- `Systems/` — `NoteMovement`, `HoldNoteStateMachine`, `SlideCheckpointSystem`
- `Manager/` — `NoteManager`

Thêm loại note mới → tạo file trong `Notes/` kế thừa `NoteBase`.

---

### 🎼 `Chart/` — Beatmap & Chart Data

Dữ liệu beatmap, đọc/ghi file, tạo chart tự động.

- `Data/` — `ChartData`, `NoteData`
- `IO/` — `ChartSaveLoad`, `BeatmapParser`
- `Generator/` — `SimpleChartGenerator`
- `Visualizer/` — `ChartVisualizer`

---

### ⚙️ `Core/` — Quản lý vòng đời game *(chưa triển khai)*

Chỉ chứa Manager **toàn cục**: `GameManager`, `SceneLoader`, `AppLifecycle`.

---

### 📊 `Scoring/` — Hệ thống tính điểm *(chưa triển khai)*

Judgment (Perfect/Great/Good/Miss), combo, score, accuracy, xếp hạng.

---

### 🎯 `Input/` — Xử lý Input *(chưa triển khai)*

Touch input, keyboard, calibration. Hiện logic input đang nằm trong `NoteManager`, sau này tách ra đây.

---

### 🔊 `Audio/` — Quản lý âm thanh *(chưa triển khai)*

AudioManager, MusicPlayer, SFX, đồng bộ audio-game clock.

---

### 🖥️ `UI/` — Giao diện người dùng

Script điều khiển UI. Logic tính toán (scoring, combo) đặt ở module riêng, UI chỉ hiển thị.

- `SongSelect/` — `SongData`, `SongItemUI`, `SongListManager`, `SelectedSongManager`
- Tạo thêm khi cần: `HUD/`, `Result/`, `Settings/`, `MainMenu/`

---

### 💾 `Config/` — Cấu hình game *(chưa triển khai)*

ScriptableObject chứa thông số cấu hình: tốc độ note, timing window, ngưỡng judgment, preset độ khó.

---

### 📂 `Save/` — Lưu trữ dữ liệu *(chưa triển khai)*

Lưu/đọc dữ liệu người chơi, high score, settings, leaderboard.

---

### 🐛 `_Debug/` — Script test & debug

**KHÔNG** đưa vào build chính thức. **KHÔNG** import từ `_Debug/` vào code production.

- `TestRuntimeNoteSpawner.cs` — Tự sinh note ngẫu nhiên để test
- `TestNoteResultLogger.cs` — Log kết quả note ra Console
- `ChartGeneratorTester.cs` — Test generate → save → load chart

---


- `Common` → không phụ thuộc module nào
- `Gameplay`, `Chart`, `Scoring` → chỉ phụ thuộc `Common`
- `Core` → điều phối `Gameplay`, `Chart`, `Scoring`
- `UI`, `Audio`, `Save` → phụ thuộc `Core` và `Common`
- `Config` → được dùng bởi mọi module (chứa ScriptableObject)
- `_Debug` → có thể dùng mọi thứ, nhưng **không ai được phụ thuộc vào `_Debug`**

# 📁 Hướng dẫn cấu trúc thư mục Scripts

> **BẮT BUỘC ĐỌC** trước khi tạo file script mới. Đặt đúng thư mục module giúp 21 thành viên làm việc không xung đột và dễ bảo trì.

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

**Mục đích:** Chứa các kiểu dữ liệu (enum, interface, struct) được **nhiều module khác sử dụng**. Đây là "nền tảng" mà mọi module đều có thể phụ thuộc vào.

**Thư mục con:**

| Thư mục | Chứa gì | Ví dụ hiện tại |
|---------|---------|----------------|
| `Enums/` | Các enum dùng chung | `NoteType`, `FlickDirection`, `NoteResult`, `HoldNoteState` |
| `Interfaces/` | Các interface dùng chung | `INoteResultReceiver` |
| `Data/` | Struct / class dữ liệu thuần | `NotePointer`, `NoteRuntimeData` |

**Quy tắc:**
- ✅ Đặt vào đây nếu kiểu dữ liệu được **≥ 2 module** sử dụng.
- ❌ Không đặt class MonoBehaviour, Manager, hoặc logic gameplay ở đây.

---

### 🎮 `Gameplay/` — Logic gameplay chính

**Mục đích:** Chứa toàn bộ logic gameplay cốt lõi — note, di chuyển, input xử lý, quản lý note.

**Thư mục con:**

| Thư mục | Chứa gì | Ví dụ hiện tại |
|---------|---------|----------------|
| `Notes/` | Abstract base note + các loại note cụ thể | `NoteBase`, `TapNote`, `HoldNote`, `FlickNote`, `SlideNote` |
| `Systems/` | Hệ thống hỗ trợ gameplay (movement, state machine) | `NoteMovement`, `HoldNoteStateMachine`, `SlideCheckpointSystem` |
| `Manager/` | Manager điều phối gameplay | `NoteManager` |

**Quy tắc:**
- ✅ Thêm loại note mới → tạo file trong `Notes/` kế thừa `NoteBase`.
- ✅ Hệ thống gameplay phụ trợ (object pool, lane system, ...) → `Systems/`.
- ❌ UI hiển thị điểm/combo → đặt ở `UI/`, không phải ở đây.

---

### 🎼 `Chart/` — Beatmap & Chart Data

**Mục đích:** Quản lý dữ liệu beatmap — cấu trúc chart, đọc/ghi file, tạo chart tự động.

**Thư mục con:**

| Thư mục | Chứa gì | Ví dụ hiện tại |
|---------|---------|----------------|
| `Data/` | Class dữ liệu chart (Serializable) | `ChartData`, `NoteData` |
| `IO/` | Đọc/ghi chart từ file JSON | `ChartSaveLoad`, `BeatmapParser` |
| `Generator/` | Thuật toán tạo chart tự động | `SimpleChartGenerator` |
| `Editor/` | Công cụ editor cho chart (chỉ dùng trong Unity Editor) | `ChartVisualizer` |

**Quy tắc:**
- ✅ Thêm format chart mới (MIDI, OSU, ...) → tạo parser trong `IO/`.
- ✅ Thuật toán AI generate chart → `Generator/`.
- ❌ Logic spawn note runtime → đặt ở `Gameplay/`, không phải `Chart/`.

---

### 📊 `Scoring/` — Hệ thống tính điểm *(chưa triển khai)*

**Mục đích:** Judgment, combo, score calculation, accuracy, ranking.

**Script nên đặt ở đây:**
- `JudgmentSystem.cs` — Đánh giá Perfect/Great/Good/Bad/Miss dựa trên timing
- `ComboManager.cs` — Quản lý combo streak
- `ScoreManager.cs` — Tính tổng điểm
- `AccuracyCalculator.cs` — Tính accuracy %
- `RankCalculator.cs` — Xếp hạng (S/A/B/C/D)

---

### 🎯 `Input/` — Xử lý Input *(chưa triển khai)*

**Mục đích:** Lớp trừu tượng xử lý input, tách khỏi gameplay logic.

**Script nên đặt ở đây:**
- `TouchInputHandler.cs` — Xử lý multi-touch trên mobile
- `InputMapper.cs` — Map input → action (cho phép remap)
- `CalibrationManager.cs` — Calibration offset cho audio/visual sync

**Lưu ý:** Hiện tại logic input đang nằm trong `NoteManager` (Gameplay). Khi refactor, nên tách ra module này.

---

### 🔊 `Audio/` — Quản lý âm thanh *(chưa triển khai)*

**Mục đích:** Quản lý nhạc nền, SFX, audio timing.

**Script nên đặt ở đây:**
- `AudioManager.cs` — Singleton quản lý tất cả âm thanh
- `MusicPlayer.cs` — Play/pause/seek nhạc trong gameplay
- `SFXPlayer.cs` — Phát sound effect (hit, miss, combo)
- `AudioSyncTimer.cs` — Đồng bộ game clock với audio time

---

### 🖥️ `UI/` — Giao diện người dùng

**Mục đích:** Tất cả script liên quan đến UI (Canvas, Button, Panel, ...).

**Thư mục con:**

| Thư mục | Chứa gì | Ví dụ hiện tại |
|---------|---------|----------------|
| `SongSelect/` | Màn hình chọn bài nhạc | `SongData`, `SongItemUI`, `SongListManager`, `SelectedSongManager` |

**Thư mục con nên tạo thêm khi cần:**
- `HUD/` — Hiển thị in-game (score, combo, progress bar)
- `Result/` — Màn hình kết quả sau khi chơi xong
- `Settings/` — Giao diện cài đặt (volume, speed, calibration)
- `MainMenu/` — Màn hình chính

**Quy tắc:**
- ✅ Script điều khiển UI element (Button, Text, Panel) → đặt ở đây.
- ❌ Logic tính toán (scoring, combo) → đặt ở `Scoring/`, UI chỉ hiển thị kết quả.

---

### ⚙️ `Core/` — Quản lý vòng đời game *(chưa triển khai)*

**Mục đích:** Các Manager cấp cao nhất, quản lý vòng đời ứng dụng.

**Script nên đặt ở đây:**
- `GameManager.cs` — Singleton quản lý state tổng thể (Menu → Playing → Result)
- `SceneLoader.cs` — Chuyển scene với loading screen
- `AppLifecycle.cs` — Xử lý pause/resume/quit trên mobile

**Quy tắc:**
- ✅ Chỉ chứa các Manager **toàn cục** ảnh hưởng toàn bộ game.
- ❌ Manager chuyên biệt (ScoreManager, AudioManager) → đặt ở module riêng.

---

### 💾 `Config/` — Cấu hình game *(chưa triển khai)*

**Mục đích:** ScriptableObject chứa thông số cấu hình, cho phép designer chỉnh từ Inspector.

**Script nên đặt ở đây:**
- `GameplayConfig.cs` — Tốc độ note, timing window, hitline position
- `ScoringConfig.cs` — Ngưỡng judgment (Perfect ≤ 0.04s, Great ≤ 0.08s, ...)
- `DifficultyPreset.cs` — Preset cho các mức độ khó

---

### 📂 `Save/` — Lưu trữ dữ liệu *(chưa triển khai)*

**Mục đích:** Lưu/đọc dữ liệu người chơi, high score, settings.

**Script nên đặt ở đây:**
- `PlayerDataManager.cs` — Quản lý dữ liệu người chơi
- `HighScoreRepository.cs` — Lưu/đọc high score cho từng bài
- `SettingsManager.cs` — Lưu/đọc cài đặt người chơi
- `LeaderboardManager.cs` — Bảng xếp hạng

---

### 🐛 `_Debug/` — Script test & debug

**Mục đích:** Script chỉ dùng để test trong quá trình phát triển. **KHÔNG** đưa vào bản build chính thức.

| File hiện tại | Mô tả |
|--------------|-------|
| `TestRuntimeNoteSpawner.cs` | Tự sinh note ngẫu nhiên để test gameplay |
| `TestNoteResultLogger.cs` | Log kết quả note ra Console |
| `ChartGeneratorTester.cs` | Test generate → save → load chart |

**Quy tắc:**
- ✅ Mọi script có prefix `Test` hoặc chỉ dùng trong sandbox → đặt ở đây.
- ✅ Prefix `_` để thư mục luôn nằm đầu tiên, dễ phân biệt.
- ❌ **TUYỆT ĐỐI KHÔNG** import script từ `_Debug/` vào code production.

---

## Sơ đồ phụ thuộc giữa các module

```
                    ┌──────────┐
                    │  Common  │  ← Mọi module đều có thể dùng
                    └────┬─────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
    ┌────▼────┐    ┌─────▼─────┐   ┌─────▼─────┐
    │Gameplay │    │   Chart   │   │  Scoring  │
    └────┬────┘    └─────┬─────┘   └─────┬─────┘
         │               │               │
         │         ┌─────▼─────┐         │
         └────────►│   Core    │◄────────┘
                   └─────┬─────┘
                         │
              ┌──────────┼──────────┐
              │          │          │
         ┌────▼──┐  ┌───▼───┐  ┌──▼───┐
         │  UI   │  │ Audio │  │ Save │
         └───────┘  └───────┘  └──────┘
```

**Quy tắc phụ thuộc:**
- `Common` → không phụ thuộc module nào khác
- `Gameplay`, `Chart`, `Scoring` → chỉ phụ thuộc `Common`
- `Core` → điều phối `Gameplay`, `Chart`, `Scoring`
- `UI`, `Audio`, `Save` → phụ thuộc `Core` và `Common`
- `Config` → được dùng bởi mọi module (chứa ScriptableObject)
- `_Debug` → có thể dùng mọi thứ, nhưng **không ai được phụ thuộc vào `_Debug`**

---

## Câu hỏi thường gặp

### Q: Tôi tạo script mới nhưng không biết đặt ở đâu?
**A:** Hỏi trong nhóm hoặc dựa theo bảng trên. Nếu script phục vụ **nhiều module** → `Common/`. Nếu phục vụ **một module cụ thể** → đặt trong module đó.

### Q: Tôi cần tạo thư mục con mới trong module?
**A:** Tạo Pull Request và ghi rõ lý do. Lead sẽ review trước khi merge.

### Q: Script của tôi vừa có logic vừa có UI?
**A:** **Tách ra.** Logic → module tương ứng (`Gameplay/`, `Scoring/`). UI → `UI/`. Giao tiếp qua event hoặc interface.

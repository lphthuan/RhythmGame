# Contributing to RhythmGame

> **LƯU Ý QUAN TRỌNG:** Đây là tài liệu quy chuẩn kỹ thuật **BẮT BUỘC** tuân thủ đối với tất cả thành viên tham gia phát triển dự án. Mọi vi phạm quy tắc trong tài liệu này có thể dẫn đến việc yêu cầu chỉnh sửa lại toàn bộ mã nguồn liên quan.

---

## Mục lục
1. [Quy trình báo cáo vấn đề (Issues)](#1-quy-trình-báo-cáo-vấn-đề-issues)
2. [Quy trình gửi Pull Request (PR)](#2-quy-trình-gửi-pull-request-pr)
3. [Quản lý đồng bộ qua Prefab & Core Systems](#3-quản-lý-đồng-bộ-qua-prefab--core-systems)
4. [Quy chuẩn mã nguồn (Coding Standards)](#4-quy-chuẩn-mã-nguồn-coding-standards)
5. [Cấu trúc thư mục (Directory Structure)](#5-cấu-trúc-thư-mục-directory-structure)
6. [Lưu ý khi làm việc với Unity](#6-lưu-ý-khi-làm-việc-với-unity)
7. [Quy trình Revert, Reapply & Discard (GitHub Desktop)](#7-quy-trình-revert-reapply--discard-github-desktop)
8. [Tài liệu tham khảo (References)](#8-tài-liệu-tham-khảo-references)

---

## 1. Quy trình báo cáo vấn đề (Issues)

Trước khi bắt đầu sửa lỗi hoặc thêm tính năng, **bắt buộc** tạo một **Issue** trên repository để theo dõi và phân công công việc.

### 1.1. Khi nào cần tạo Issue?
- Phát hiện **bug** trong game (lỗi logic, crash, hiển thị sai, v.v.)
- Đề xuất **tính năng mới** hoặc cải tiến tính năng hiện có.
- Yêu cầu **cải thiện hiệu năng** (lag, giật, load chậm).
- Cần **thảo luận kỹ thuật** trước khi triển khai một thay đổi lớn.

### 1.2. Quy tắc tạo Issue
- **Tiêu đề Issue** phải rõ ràng, mô tả ngắn gọn vấn đề. Ví dụ: `[Bug] Note không hiển thị khi BPM > 200` hoặc `[Feature] Thêm hiệu ứng combo x50`.
- **Gắn Label** phân loại: `bug`, `feature`, `enhancement`, `question`, v.v.
- **Assign** cho người phụ trách hoặc để trống nếu chưa xác định.
- **Liên kết Milestone/Sprint** nếu có.

### 1.3. Template Bug Report

```markdown
## Mô tả lỗi
[Mô tả ngắn gọn lỗi xảy ra]

## Mức độ nghiêm trọng
[Critical / High / Medium / Low]

## Các bước tái hiện
1. Mở scene [tên scene]
2. Bắt đầu chơi bài [tên bài]
3. Thực hiện [hành động cụ thể]
4. Quan sát [kết quả thực tế]

## Kết quả mong đợi
[Mô tả điều đáng lẽ phải xảy ra]

## Kết quả thực tế
[Mô tả lỗi cụ thể đã xảy ra]

## Thông tin môi trường
- **Scene:** [tên scene]
- **Unity version:** [phiên bản]
- **OS:** [Windows / Mac]

## Tài liệu đính kèm
[Screenshot, video, hoặc log Console nếu có]
```

### 1.4. Template Feature Request

```markdown
## Tính năng đề xuất
[Mô tả tính năng muốn thêm]

## Lý do cần thiết
[Giải thích vì sao tính năng này cần cho game]

## Đề xuất cách triển khai
[Mô tả hướng giải quyết kỹ thuật nếu có ý tưởng]

## Tài liệu tham khảo
[Link tới game hoặc tính năng tương tự nếu có]
```

### 1.5. Quy trình xử lý Issue
1. **Tạo Issue** theo template phù hợp.
2. **Leader/Lead** xem xét, gắn label, assign cho thành viên phụ trách.
3. Thành viên được assign **tạo nhánh** từ Issue đó để làm việc.
4. Sau khi hoàn thành, **tạo PR** và liên kết Issue (ví dụ: `Closes #12`).
5. Issue tự động đóng khi PR được merge.

---

## 2. Quy trình gửi Pull Request (PR)

> **🔴 QUY TẮC VÀNG:** Thành viên **CHỈ ĐƯỢC GỬI PR**, **KHÔNG ĐƯỢC TỰ Ý MERGE**. Chỉ Lead hoặc người được chỉ định mới có quyền merge PR vào nhánh chính.

### 2.1. Quy tắc đặt tên nhánh (Branch Naming)

**YÊU CẦU:** Tên nhánh phải được viết bằng **Tiếng Anh (English)**, chữ thường, phân cách bằng dấu gạch ngang `-`.

Cấu trúc: `type/short-description`

| Prefix | Mục đích | Ví dụ |
|--------|----------|-------|
| `feat/` | Thêm tính năng mới | `feat/add-hold-note-logic` |
| `bugfix/` | Sửa lỗi thông thường | `bugfix/fix-note-miss-detection` |
| `hotfix/` | Sửa lỗi khẩn cấp | `hotfix/crash-on-song-select` |
| `refactor/` | Tái cấu trúc mã nguồn | `refactor/optimize-note-spawner` |
| `docs/` | Cập nhật tài liệu | `docs/update-contributing` |
| `chore/` | Config, build, cleanup | `chore/update-gitignore` |

**❌ Sai:** `myBranch`, `test123`, `thuanfix`, `new-feature`
**✅ Đúng:** `feat/implement-scoring-system`, `bugfix/fix-audio-desync`

### 2.2. Quy tắc viết Commit (Commit Messages)

**YÊU CẦU:** Nội dung commit phải được viết bằng **Tiếng Anh (English)** và tuân thủ **Conventional Commits**.

**Định dạng:**
Một commit message bao gồm tiêu đề ngắn và mô tả chi tiết (tùy chọn).
- **Summary:** Giữ dưới 72 ký tự. Sử dụng thì mệnh lệnh.
- **Description:** Mô tả chi tiết qua các gạch đầu dòng. Giải thích *cái gì* đã thay đổi và *tại sao*.

Cấu trúc Summary: `prefix: short description`

| Prefix | Ý nghĩa |
|--------|---------|
| `feat` | Thêm tính năng mới |
| `fix` | Sửa lỗi |
| `refactor` | Thay đổi mã nguồn nhưng không sửa lỗi hay thêm tính năng |
| `docs` | Cập nhật tài liệu |
| `chore` | Các công việc lặt vặt (build scripts, thêm thư viện, dọn rác) |
| `style` | Thay đổi về format, không ảnh hưởng tới logic |
| `test` | Thêm hoặc sửa các bài test |


**Ví dụ:**
```
✅ feat: implement tap note hit detection
✅ fix: resolve audio desync after pausing
✅ asset: add new drum SFX pack

❌ fix bug
❌ update
❌ sửa lỗi note
```

### 2.3. Quy trình gửi PR từng bước

```
1. git checkout main
2. git pull origin main                ← Cập nhật nhánh chính mới nhất
3. git checkout -b feat/your-task      ← Tạo nhánh mới theo quy tắc đặt tên
4. ... code, test ...
5. git add . && git commit -m "feat: mô tả ngắn"
6. git push origin feat/your-task
7. Lên GitHub → Tạo Pull Request → main
8. ⏳ CHỜ Lead review và merge        ← KHÔNG TỰ MERGE
```

### 2.4. Yêu cầu khi tạo PR

1. **Tiêu đề PR:** Tuân theo format commit message, ví dụ: `feat: implement hold note mechanic`
2. **Mô tả PR:** Bắt buộc ghi rõ:
   - Tóm tắt những gì đã thay đổi và lý do
   - Screenshot/GIF nếu là thay đổi UI hoặc gameplay
   - Liên kết Issue: `Closes #12` hoặc `Relates to #15`
3. **Assignees:** Gán Lead hoặc người review phù hợp.
4. **Labels:** Gắn label phân loại (`gameplay`, `ui`, `audio`, `bugfix`, v.v.)

### 2.5. Checklist trước khi tạo PR
- [ ] Code biên dịch thành công (không có lỗi đỏ trong Unity Console)
- [ ] Đã test trên Unity Editor, chức năng hoạt động đúng
- [ ] Không có file rác, log `Debug.Log` thừa, hoặc code comment-out
- [ ] Không chỉnh sửa file ngoài phạm vi task được giao
- [ ] Đã pull `main` mới nhất, không có conflict
- [ ] Commit messages tuân thủ Conventional Commits

**Mẫu Pull Request hoàn chỉnh:**
```text
Branch Name:
  feat/implement-hold-note

Commit Message:
  feat: implement hold note mechanic with sustained scoring

  - Added HoldNote class extending BaseNote with duration tracking
  - Implemented sustained score calculation based on hold accuracy
  - Added visual feedback (progress bar) during hold phase
  - Updated NoteSpawner to support hold note type from beatmap data

Closes #25
```

---

## 3. Quản lý đồng bộ qua Prefab & Core Systems

### 3.1. Prefab là gì?

**Prefab** (Prefabricated Object) là một template (bản mẫu) của một GameObject trong Unity. Khi bạn tạo một Prefab, Unity lưu lại toàn bộ cấu hình của GameObject đó (components, giá trị, children) thành một file `.prefab` có thể tái sử dụng.

**Tại sao dùng Prefab?**
- **Tái sử dụng:** Tạo một lần, dùng ở nhiều Scene khác nhau.
- **Đồng bộ:** Khi sửa Prefab gốc, tất cả các instance trong các Scene đều tự động cập nhật.
- **Giảm conflict Git:** Thay vì sửa trực tiếp trong Scene (file `.unity` rất khó merge), sửa trên Prefab (file `.prefab` nhỏ hơn, dễ quản lý hơn).
- **Làm việc nhóm hiệu quả:** Nhiều người có thể làm việc trên các Prefab khác nhau mà không đụng độ Scene.

### 3.2. Quy tắc chuyển đổi Object thành Prefab

Mọi GameObject được setup sẵn trong Scene **phải được chuyển thành Prefab** để quản lý và tái sử dụng. Quy trình:

1. **Tạo Prefab:** Kéo GameObject từ **Hierarchy** vào thư mục `Assets/_Game/Prefabs/` trong **Project** window.
2. **Phân loại Prefab:** Đặt vào đúng thư mục con:
   - `Prefabs/UI/` — Các Prefab liên quan giao diện (HUD, Menu, Popup)
   - `Prefabs/Gameplay/` — Các Prefab gameplay (Notes, Effects, Player)
   - `Prefabs/Environment/` — Các Prefab môi trường, background
   - `Prefabs/System/` — Các Prefab hệ thống (Managers, Core)
3. **Chỉnh sửa qua Prefab Mode:** Double-click vào Prefab trong Project window để mở **Prefab Mode** → chỉnh sửa → lưu. **Không** chỉnh sửa trực tiếp instance trong Scene rồi quên Apply.
4. **Apply Override:** Nếu đã chỉnh sửa instance trong Scene, nhớ click **Overrides → Apply All** trên Inspector để đồng bộ về Prefab gốc.

### 3.3. Quản lý Core Systems

- **Core Systems** là các hệ thống cốt lõi của game (GameManager, AudioManager, ScoreManager, v.v.). **KHÔNG** thay đổi trực tiếp trừ khi có sự đồng ý của Lead. Mọi thay đổi phải được kiểm tra kỹ lưỡng vì nó ảnh hưởng đến toàn bộ dự án.
- **Canvas & UI:** Sử dụng Prefab cho tất cả thành phần UI (HUD, Menu, Result Screen). Khi cập nhật UI, hãy cập nhật vào Prefab gốc thay vì thay đổi trên Scene.
- **Prefab Variant:** Khi cần tạo biến thể của một Prefab (ví dụ: các loại Note khác nhau), sử dụng **Prefab Variant** (chuột phải Prefab → Create → Prefab Variant) thay vì duplicate và sửa riêng.

---

## 4. Quy chuẩn mã nguồn (Coding Standards)

### 4.1. C# Naming Conventions

| Loại | Quy tắc | Ví dụ |
|------|---------|-------|
| Classes, Structs | `PascalCase` | `NoteSpawner`, `ScoreManager` |
| Public Methods | `PascalCase` | `CalculateScore()`, `SpawnNote()` |
| Public Properties | `PascalCase` | `CurrentCombo`, `TotalScore` |
| Private/Protected Fields | `_camelCase` | `_noteSpeed`, `_isPlaying` |
| Local Variables | `camelCase` | `hitResult`, `noteIndex` |
| Constants | `UPPER_SNAKE_CASE` | `MAX_COMBO`, `PERFECT_WINDOW` |
| Enums | `PascalCase` | `NoteType.Tap`, `HitResult.Perfect` |
| Interfaces | `IPascalCase` | `IHittable`, `IScoreable` |

### 4.2. Lập trình hướng đối tượng & Nguyên tắc SOLID (BẮT BUỘC)

> **Tất cả code trong dự án phải tuân thủ OOP và nguyên tắc SOLID.** PR vi phạm sẽ bị yêu cầu refactor trước khi merge.

#### A. Lập trình hướng đối tượng (OOP)

Mọi thành viên phải áp dụng đúng **4 tính chất OOP** khi viết code:

| Tính chất | Yêu cầu | Ví dụ trong dự án |
|-----------|---------|-------------------|
| **Encapsulation** (Đóng gói) | Sử dụng `private`/`protected` cho field, expose qua `public` property hoặc method. **KHÔNG** dùng `public` field trừ khi có lý do chính đáng. | `ScoreManager`: field `_totalScore` là `private`, expose qua `public int TotalScore { get; private set; }` |
| **Abstraction** (Trừu tượng) | Sử dụng `abstract class` hoặc `interface` để định nghĩa hành vi chung. Các module bên ngoài chỉ tương tác qua interface/abstract, không phụ thuộc vào class cụ thể. | `BaseNote` (abstract) → `TapNote`, `HoldNote`, `SlideNote` kế thừa |
| **Inheritance** (Kế thừa) | Sử dụng kế thừa khi các class có quan hệ **"is-a"**. Không lạm dụng kế thừa khi composition phù hợp hơn. | `HoldNote : BaseNote` — vì HoldNote **là** một loại Note |
| **Polymorphism** (Đa hình) | Override method ở lớp con để thay đổi hành vi. Sử dụng `virtual`/`override` đúng cách. | `BaseNote.OnHit()` là `virtual` → `TapNote` và `HoldNote` override để xử lý khác nhau |

#### B. Nguyên tắc SOLID

| # | Nguyên tắc | Mô tả | Quy tắc áp dụng |
|---|-----------|-------|-----------------|
| **S** | **Single Responsibility** | Mỗi class chỉ có **MỘT trách nhiệm duy nhất**, một lý do để thay đổi. | `ScoreManager` chỉ quản lý điểm. `ComboManager` chỉ quản lý combo. **KHÔNG** gộp scoring + combo + rank vào cùng 1 class. |
| **O** | **Open/Closed** | Class phải **mở cho mở rộng** (extension), **đóng cho sửa đổi** (modification). | Khi thêm loại Note mới (ví dụ: `FlickNote`), chỉ cần tạo class mới kế thừa `BaseNote`. **KHÔNG** sửa `NoteManager` hay `NoteSpawner` để thêm `if/else`. |
| **L** | **Liskov Substitution** | Lớp con phải **thay thế được** lớp cha mà không làm sai logic chương trình. | Mọi class kế thừa `BaseNote` đều phải hoạt động đúng khi `NoteManager` gọi `note.OnHit()`, bất kể đó là `TapNote`, `HoldNote`, hay `SlideNote`. |
| **I** | **Interface Segregation** | Không ép class implement interface mà nó không cần. Tách interface lớn thành các interface nhỏ, chuyên biệt. | Tách `IHittable`, `IHoldable`, `ISlideable` thay vì một `INoteActions` chứa tất cả method. `TapNote` chỉ cần implement `IHittable`. |
| **D** | **Dependency Inversion** | Module cấp cao không phụ thuộc module cấp thấp. Cả hai phụ thuộc vào **abstraction** (interface/abstract class). | `NoteSpawner` phụ thuộc vào `BaseNote` (abstract), không phụ thuộc trực tiếp vào `TapNote` hay `HoldNote`. |
---

#### C. Design Patterns bắt buộc trong dự án

Dựa theo Task List, các pattern sau **phải được áp dụng đúng**:

| Pattern | Áp dụng cho | Yêu cầu |
|---------|------------|---------|
| **Singleton** | `TimeManager`, `AudioManager` | Chỉ dùng cho các Manager toàn cục. Phải có kiểm tra instance trùng lặp. |
| **Object Pool** | Note Spawning | Tái sử dụng Note object thay vì Instantiate/Destroy liên tục. Giảm GC allocation. |
| **State Machine** | `HoldNote` (Idle → Hit → Holding → Finished) | Quản lý vòng đời object qua các trạng thái rõ ràng. |
| **Observer / Event** | Giao tiếp giữa các Manager | Sử dụng C# `event`/`Action` hoặc EventBus. **KHÔNG** để các Manager gọi trực tiếp lẫn nhau. |

> **Tham khảo thêm:** [SOLID là gì? — TopDev](https://topdev.vn/blog/solid-la-gi/)


### 4.3. Quy tắc chung

- **Một class = một file.** Tên file phải trùng với tên class.
- **Không hardcode giá trị.** Sử dụng `ScriptableObject` hoặc `const` cho các thông số cấu hình.
- **Không dùng `Find()` hay `GetComponent()` trong `Update()`.** Cache reference trong `Awake()` hoặc `Start()`.
- **Không commit code đã comment-out.** Xóa sạch trước khi tạo PR.
- **Sử dụng `[SerializeField]`** thay vì `public` field để expose trong Inspector.

---

## 5. Cấu trúc thư mục (Directory Structure)

```text
D:\GameProjects\RhythmGame\
├── Assets\
│   ├── _Game\                          # ★ Thư mục chính của dự án
│   │   ├── Animation\                  # Animator Controllers, Animation Clips
│   │   ├── Audio\                      # Âm thanh
│   │   │   ├── Music\                  # File nhạc (.ogg / .wav)
│   │   │   └── SFX\                    # Sound effects (hit, miss, combo)
│   │   ├── Data\                       # ScriptableObjects, Configs, Beatmap data
│   │   ├── Prefabs\                    # Prefabs (Notes, UI, Effects, System)
│   │   ├── Scenes\                     # Game scenes
│   │   ├── Scripts\                    # C# Scripts phân theo module
│   │   └── Sprites\                    # Textures, UI sprites, note skins
│   ├── _ThirdParty\                    # Thư viện bên thứ ba & Plugins
│   └── Settings\                       # URP / Render Pipeline settings
├── ProjectSettings\                    # Cấu hình dự án Unity
└── Packages\                           # Package manifest của Unity
```

> **⚠️ Quy tắc:** Mọi asset của dự án **phải nằm trong `_Game/`**. Không tạo folder riêng ngoài `_Game/` trừ thư viện bên thứ ba (`_ThirdParty/`).

---

## 6. Lưu ý khi làm việc với Unity

- **.meta Files:** Tuyệt đối **không xóa** hoặc bỏ qua các file `.meta`. Chúng quản lý GUID và liên kết giữa các Asset. Nếu mất file `.meta`, toàn bộ reference đến Asset đó sẽ bị hỏng.
- **Scene Work:** Hạn chế nhiều người cùng chỉnh sửa trên một Scene. Ưu tiên làm việc trên **Prefab** để giảm conflict. Nếu cần sửa Scene, **báo trước trên nhóm** để tránh đụng độ.
- **Validation:** Chạy **Play Mode test** trước mỗi commit. Định kỳ build **Standalone** để đảm bảo tính năng hoạt động trên môi trường đóng gói.
- **Unity Version:** Sử dụng **đúng phiên bản Unity** được chỉ định. Kiểm tra `ProjectSettings/ProjectVersion.txt`. **KHÔNG** mở project bằng phiên bản khác.

---

## 7. Quy trình Revert, Reapply & Discard (GitHub Desktop)

Hướng dẫn xử lý các thay đổi mã nguồn an toàn bằng **GitHub Desktop**.

### 7.1. Discard — Hủy bỏ thay đổi chưa commit

| Bước | Thao tác |
|------|----------|
| 1 | Mở GitHub Desktop → chuyển sang tab **Changes** |
| 2 | Chuột phải vào file muốn hủy → chọn **Discard changes...** |
| 3 | Xác nhận hủy bỏ |

> **⚠️ Cảnh báo:** Discard **KHÔNG THỂ hoàn tác**. Kiểm tra kỹ trước khi thực hiện.

### 7.2. Undo — Hoàn tác commit cục bộ (chưa Push)

| Bước | Thao tác |
|------|----------|
| 1 | Vừa commit xong nhưng **chưa Push** |
| 2 | Nhấn nút **Undo** ở phía dưới tab **Changes** |
| 3 | Commit bị hoàn tác, các thay đổi quay lại trạng thái **unstaged** |

> **Lưu ý:** Chỉ hoạt động với commit **gần nhất** và **chưa được Push** lên remote.

### 7.3. Revert — Đảo ngược commit đã Push

| Bước | Thao tác |
|------|----------|
| 1 | Chuyển sang tab **History** |
| 2 | Chuột phải vào commit gây lỗi → chọn **Revert changes in commit** |
| 3 | GitHub Desktop tạo một commit mới đảo ngược toàn bộ thay đổi |
| 4 | Push commit revert lên remote |

### 7.4. Reapply — Áp dụng lại sau Revert

Khi muốn khôi phục lại tính năng đã bị Revert:
1. Vào tab **History** → tìm commit **Revert** đã tạo ở bước trước.
2. Chuột phải → chọn **Revert changes in commit** lần nữa (revert của revert).
3. Kết quả: tính năng được khôi phục về trạng thái ban đầu.

### 7.5. Partial Revert — Đảo ngược một phần

**Tình huống:** Một PR có nhiều file thay đổi, nhưng chỉ một vài file gây lỗi. Muốn giữ file tốt, chỉ revert file lỗi.

| Bước | Thao tác | Kết quả |
|------|----------|---------|
| 1 | **Revert** toàn bộ commit/PR | Tất cả file bị đảo ngược |
| 2 | Nhấn **Undo** (commit revert chưa push) | Các thay đổi đảo ngược hiện lên trong Changes |
| 3 | **Discard** các file muốn **giữ lại** (file hoạt động tốt) | Chỉ còn revert cho file gây lỗi |
| 4 | Commit → Push | File lỗi bị revert, file tốt vẫn giữ nguyên |

**Ví dụ minh họa:**
- PR thay đổi **File A** (tốt) và **File B** (lỗi).
- Revert toàn bộ → Undo → Discard File A → Commit.
- ✅ Kết quả: File A giữ tính năng mới, File B quay về trạng thái trước commit.

---

## 8. Tài liệu tham khảo (References)

| Tài liệu | Link |
|-----------|------|
| **Danh sách công việc & Story Points** | [Google Sheets — Task List](https://docs.google.com/spreadsheets/d/149fjPmffEF0XHNB5Hp_FG1De26Y5tYRKuVEqwi2bgcI/edit?gid=392276454#gid=392276454) |
| **Tài liệu dự án (Google Drive)** | [RhythmGame Resources](https://drive.google.com/drive/folders/13jTsJrvAK-AXpCxbp9j8T8Yt7g7gWZ8Z) |
| **Unity C# Style Guide** | [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) |

---

Mọi chi tiết thắc mắc vui lòng liên hệ Tech Lead [lphthuan](https://github.com/lphthuan) hoặc Project Manager [khoatnd223](https://github.com/khoatnd223).

Cảm ơn bạn đã tuân thủ các quy tắc để dự án phát triển bền vững!

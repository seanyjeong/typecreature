# 알람 기능 구현 계획

> **Goal:** 시계 위젯에 알람 기능 추가 - 설정한 시간에 팝업으로 메시지 표시

## 기능 요약

- 시계 클릭 → 알람 설정 창
- 여러 개 알람 저장 가능
- 시간 되면 큰 팝업으로 알람 메시지 표시
- 알람 목록 관리 (추가/삭제/활성화/비활성화)

---

## Task 1: 데이터베이스 스키마

**Files:**
- Modify: `Services/DatabaseService.cs`

**Step 1:** alarms 테이블 추가

```csharp
CREATE TABLE IF NOT EXISTS alarms (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    hour INTEGER NOT NULL,
    minute INTEGER NOT NULL,
    message TEXT NOT NULL,
    is_enabled INTEGER DEFAULT 1,
    repeat_daily INTEGER DEFAULT 0,
    created_at TEXT NOT NULL
);
```

**Step 2:** CRUD 메서드 추가

```csharp
public List<Alarm> GetAlarms()
public void AddAlarm(int hour, int minute, string message, bool repeatDaily)
public void UpdateAlarm(int id, bool isEnabled)
public void DeleteAlarm(int id)
```

---

## Task 2: Alarm 모델

**Files:**
- Create: `Models/Alarm.cs`

```csharp
public class Alarm
{
    public int Id { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public string Message { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public bool RepeatDaily { get; set; } = false;
    public DateTime CreatedAt { get; set; }

    public string TimeText => $"{Hour:D2}:{Minute:D2}";
}
```

---

## Task 3: AlarmService

**Files:**
- Create: `Services/AlarmService.cs`

**기능:**
- 매 초마다 현재 시간과 알람 시간 비교
- 알람 시간 도달 시 이벤트 발생
- 반복 알람이 아니면 자동 비활성화

```csharp
public class AlarmService
{
    public event Action<Alarm>? AlarmTriggered;

    private Timer _checkTimer;
    private HashSet<int> _triggeredToday; // 오늘 이미 울린 알람

    public void Start() { }
    public void Stop() { }
    private void CheckAlarms() { }
}
```

---

## Task 4: 알람 설정 창 UI

**Files:**
- Create: `Views/AlarmWindow.axaml`
- Create: `Views/AlarmWindow.axaml.cs`
- Create: `ViewModels/AlarmViewModel.cs`

**UI 구성:**
```
┌─────────────────────────────────┐
│  ⏰ 알람 설정                    │
├─────────────────────────────────┤
│  시간: [HH] : [MM]              │
│  메시지: [________________]      │
│  [ ] 매일 반복                   │
│  [추가]                         │
├─────────────────────────────────┤
│  알람 목록:                      │
│  ┌───────────────────────────┐  │
│  │ 🔔 09:00 - 아침 회의      │  │
│  │ 🔕 12:00 - 점심시간       │  │
│  │ 🔔 18:00 - 퇴근!          │  │
│  └───────────────────────────┘  │
│                    [삭제] [닫기] │
└─────────────────────────────────┘
```

---

## Task 5: 알람 팝업 창

**Files:**
- Create: `Views/AlarmPopup.axaml`
- Create: `Views/AlarmPopup.axaml.cs`

**UI 구성:**
```
┌─────────────────────────────────────┐
│                                     │
│           ⏰ 09:00                  │
│                                     │
│        🔔 아침 회의 🔔              │
│                                     │
│           [확인]                    │
│                                     │
└─────────────────────────────────────┘
```

**특징:**
- 화면 중앙에 크게 표시
- 반투명 어두운 배경
- 확인 버튼 클릭 시 닫힘
- 효과음 재생 (선택사항)

---

## Task 6: MiniWidget 연동

**Files:**
- Modify: `Views/MiniWidget.axaml` - 시계 클릭 이벤트
- Modify: `Views/MiniWidget.axaml.cs` - 알람 창 열기, 알람 트리거 처리

**변경사항:**
1. 시계 영역에 클릭 이벤트 추가
2. AlarmService 초기화 및 이벤트 구독
3. AlarmTriggered 이벤트 → AlarmPopup 표시

---

## 구현 순서

1. Task 1: DB 스키마 (기반)
2. Task 2: Alarm 모델 (기반)
3. Task 3: AlarmService (핵심 로직)
4. Task 4: 알람 설정 창 (UI)
5. Task 5: 알람 팝업 (UI)
6. Task 6: MiniWidget 연동 (통합)

---

## 추후 확장 가능

- [ ] 스누즈 기능 (5분 뒤 다시 알림)
- [ ] 알람 소리 선택
- [ ] 요일별 반복 설정 (평일만, 주말만 등)
- [ ] 알람 아이콘 시계 옆에 표시 (다음 알람까지 남은 시간)

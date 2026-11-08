# Lobby và màn chọn level

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

Mục tiêu: mở game vào `Lobby.unity` có PLAY, LEVELS, Settings; chọn level mở khóa tuần tự; gameplay có Home và ghi tiến độ bằng `WoolProgress`. Không đổi luật gameplay.

**Kiến trúc:** dữ liệu thuần C# (`IWoolPrefs`, `WoolProgress`, `WoolSettings`, `WoolSession`) kiểm bằng harness .NET; UI Lobby là một scene uGUI dựng bằng editor builder; `WoolScene` đọc `WoolSession` và ghi `WoolProgress`.

**Tech:** Unity 6000.3.19f1, URP 17.3, uGUI + `Text` legacy, Input System UI module, R3 1.3.1, C# 9, harness .NET 10.

**Spec:** `docs/superpowers/specs/2026-11-08-lobby-level-select-design.md`

## Ràng buộc chung

- Không comment trong source, kể cả XML comment.
- Method trả reference dùng interface/abstract; factory dùng void + out.
- Timer lặp dùng R3 `Observable.Interval` có Dispose; Lobby bỏ qua timeScale bằng `UnityTimeProvider.UpdateIgnoreTimeScale`.
- Giữ `.meta`/GUID và mọi phần tử UI scene-owned đang có trong `WoolJam.unity`.
- KHÔNG chạy `JamJam/Build Wool Jam Scene` trên project chính.
- Không stage/commit; bước "Commit" của skill được thay bằng "dừng lại báo user".
- Key PlayerPrefs: `JamJam.Cleared`, `JamJam.Level` (cũ), `JamJam.Sound`, `JamJam.Music`, `JamJam.Vibration`.
- Scene: `Lobby` = `Assets/Scenes/Lobby.unity` index 0, `WoolJam` = `Assets/Scenes/WoolJam.unity` index 1.

## Review Focus

- Key cũ ngoài khoảng (`JamJam.Level = 99` hoặc `-5`) → kẹp vào `[0, levelCount]`, không crash. Test ở Task 1.
- Số level giảm (đã lưu `Cleared = 5` nhưng chỉ còn 3 level) → `Cleared == 3`, `Current == 2`. Test ở Task 1.
- Index ngoài khoảng (`-1`, `levelCount`) cho `IsUnlocked`/`IsCleared`/`MarkCleared` → false, không đổi dữ liệu. Test ở Task 1.
- Bấm PLAY/thẻ hai lần liên tiếp → chỉ load scene một lần (cờ `loading`). Kiểm ở Task 4.
- `WoolSession.SelectedLevel` ngoài khoảng → `LoadLevel` đã kẹp sẵn; scene WoolJam cũ chưa có `HomeButtons` → null-guard. Kiểm ở Task 3.

## Bản đồ file

| File | Việc |
|---|---|
| `Assets/JamJam/Runtime/IWoolPrefs.cs` | Tạo — interface lưu trữ |
| `Assets/JamJam/Runtime/WoolPlayerPrefs.cs` | Tạo — bản PlayerPrefs |
| `Assets/JamJam/Runtime/WoolProgress.cs` | Tạo — tiến độ mở khóa |
| `Assets/JamJam/Runtime/WoolSettings.cs` | Tạo — toggle |
| `Assets/JamJam/Runtime/WoolSession.cs` | Tạo — level đã chọn, tên scene |
| `Assets/JamJam/Runtime/WoolLevelCard.cs` | Tạo — view thẻ level |
| `Assets/JamJam/Runtime/WoolLobby.cs` | Tạo — controller Lobby |
| `Assets/JamJam/Runtime/WoolScene.cs` | Sửa — Start, thắng, Next, Home |
| `Assets/JamJam/Editor/WoolSceneBuilder.cs` | Sửa — helper internal, nút Home, menu Add Home Buttons, build scenes |
| `Assets/JamJam/Editor/WoolLobbyBuilder.cs` | Tạo — dựng Lobby + LevelCard |
| `Assets/JamJam/Editor/WoolEditorChecks.cs` | Sửa — lưu/khôi phục `JamJam.Cleared` |
| `Tools/WoolJamChecks/ProgressChecks.cs` | Tạo — test tiến độ/settings |
| `Tools/WoolJamChecks/WoolJamChecks.csproj`, `Program.cs` | Sửa — đưa file mới vào harness |

Đã xác minh các file "Tạo" chưa tồn tại (2026-11-08).

---

### Task 1: Dữ liệu tiến độ và settings (TDD trong harness)

**Interfaces — Produces:**
- `interface IWoolPrefs { int GetInt(string key,int fallback); void SetInt(string key,int value); bool HasKey(string key); void DeleteKey(string key); void Save(); }`
- `WoolProgress(IWoolPrefs prefs,int levelCount)`; `int LevelCount`, `int Cleared`, `int Current`; `bool IsUnlocked(int)`, `bool IsCleared(int)`, `bool MarkCleared(int)`, `void Reset()`; hằng `ClearedKey`, `LegacyKey`.
- `WoolSettings(IWoolPrefs prefs)`; `bool Sound`, `bool Music`, `bool Vibration` (get/set).

- [ ] **Bước 1: Viết test RED** — tạo `Tools/WoolJamChecks/ProgressChecks.cs`:

```csharp
using System;
using System.Collections.Generic;
using JamJam;

sealed class MemoryPrefs : IWoolPrefs
{
    readonly Dictionary<string,int> values=new Dictionary<string,int>();
    public int Saves { get; private set; }
    public int GetInt(string key,int fallback) { return values.TryGetValue(key,out var value)?value:fallback; }
    public void SetInt(string key,int value) { values[key]=value; }
    public bool HasKey(string key) { return values.ContainsKey(key); }
    public void DeleteKey(string key) { values.Remove(key); }
    public void Save() { Saves++; }
}

static class ProgressChecks
{
    static int passed;
    public static int Assertions => passed;

    static void Check(bool value,string name)
    {
        if(!value) throw new InvalidOperationException(name);
        passed++;
    }

    public static void Run()
    {
        var prefs=new MemoryPrefs();
        var progress=new WoolProgress(prefs,3);
        Check(progress.Cleared==0 && progress.Current==0,"Fresh progress starts at level one");
        Check(progress.IsUnlocked(0) && !progress.IsUnlocked(1) && !progress.IsCleared(0),"Only level one is open at first");
        Check(!progress.MarkCleared(1) && progress.Cleared==0,"Locked level cannot be cleared");
        Check(progress.MarkCleared(0) && progress.Cleared==1 && progress.IsUnlocked(1) && progress.IsCleared(0),"Winning level one opens level two");
        Check(!progress.MarkCleared(0) && progress.Cleared==1,"Replaying a cleared level keeps progress");
        Check(new WoolProgress(prefs,3).Cleared==1,"Progress persists across instances");
        progress.MarkCleared(1);
        Check(progress.MarkCleared(2) && progress.Cleared==3 && progress.IsCleared(2) && progress.Current==2,"Winning the last level clears it and keeps Current in range");
        Check(!progress.MarkCleared(3) && progress.Cleared==3,"Cannot clear past the last level");
        Check(!progress.IsUnlocked(-1) && !progress.IsUnlocked(3) && !progress.IsCleared(-1) && !progress.MarkCleared(-1),"Out of range indices are rejected");

        var settings=new WoolSettings(prefs);
        Check(settings.Sound && settings.Music && settings.Vibration,"Settings default to on");
        settings.Music=false;
        Check(!new WoolSettings(prefs).Music && new WoolSettings(prefs).Sound,"Settings persist");
        progress.Reset();
        Check(progress.Cleared==0 && new WoolProgress(prefs,3).Cleared==0,"Reset locks everything again");
        Check(!new WoolSettings(prefs).Music,"Reset keeps settings");

        var legacy=new MemoryPrefs();
        legacy.SetInt(WoolProgress.LegacyKey,2);
        Check(new WoolProgress(legacy,3).Cleared==2 && !legacy.HasKey(WoolProgress.LegacyKey) && legacy.GetInt(WoolProgress.ClearedKey,-1)==2,"Legacy level key migrates once");
        legacy.SetInt(WoolProgress.LegacyKey,0);
        Check(new WoolProgress(legacy,3).Cleared==2,"Legacy key is ignored after migration");

        var high=new MemoryPrefs(); high.SetInt(WoolProgress.LegacyKey,99);
        Check(new WoolProgress(high,3).Cleared==3,"Legacy value above range is clamped");
        var low=new MemoryPrefs(); low.SetInt(WoolProgress.LegacyKey,-5);
        Check(new WoolProgress(low,3).Cleared==0,"Legacy value below range is clamped");
        var shrink=new MemoryPrefs(); shrink.SetInt(WoolProgress.ClearedKey,5);
        var shrunk=new WoolProgress(shrink,3);
        Check(shrunk.Cleared==3 && shrunk.Current==2,"Stored progress is clamped to level count");
    }
}
```

- [ ] **Bước 2: Nối vào harness**

**Trước**
```8:13:Tools/WoolJamChecks/WoolJamChecks.csproj
  <ItemGroup>
    <Compile Include="Program.cs" />
    <Compile Include="RouteChecks.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolRouting.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolGame.cs" />
  </ItemGroup>
```

**Sau**
```xml
  <ItemGroup>
    <Compile Include="Program.cs" />
    <Compile Include="RouteChecks.cs" />
    <Compile Include="ProgressChecks.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolRouting.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolGame.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/IWoolPrefs.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolProgress.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolSettings.cs" />
  </ItemGroup>
```

**Trước**
```79:81:Tools/WoolJamChecks/Program.cs
    static int Main()
    {
        RouteChecks.Run();
```

**Sau**
```csharp
    static int Main()
    {
        ProgressChecks.Run();
        RouteChecks.Run();
```

**Trước**
```230:231:Tools/WoolJamChecks/Program.cs
        var routeAssertions = RouteChecks.Assertions;
        Console.WriteLine("PASS: " + passed + " gameplay assertions; " + routeAssertions + " route assertions");
```

**Sau**
```csharp
        var routeAssertions = RouteChecks.Assertions;
        Console.WriteLine("PASS: " + passed + " gameplay assertions; " + routeAssertions + " route assertions; " + ProgressChecks.Assertions + " progress assertions");
```

- [ ] **Bước 3: Chạy RED**

Run: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet`
Kỳ vọng: build FAIL vì thiếu file `IWoolPrefs.cs`/`WoolProgress.cs`/`WoolSettings.cs`.

- [ ] **Bước 4: Tạo `Assets/JamJam/Runtime/IWoolPrefs.cs`**

```csharp
namespace JamJam
{
    public interface IWoolPrefs
    {
        int GetInt(string key,int fallback);
        void SetInt(string key,int value);
        bool HasKey(string key);
        void DeleteKey(string key);
        void Save();
    }
}
```

- [ ] **Bước 5: Tạo `Assets/JamJam/Runtime/WoolProgress.cs`**

```csharp
using System;

namespace JamJam
{
    public sealed class WoolProgress
    {
        public const string ClearedKey="JamJam.Cleared";
        public const string LegacyKey="JamJam.Level";
        readonly IWoolPrefs prefs;
        public int LevelCount { get; }
        public int Cleared { get; private set; }
        public int Current => Math.Min(Cleared,LevelCount-1);

        public WoolProgress(IWoolPrefs prefs,int levelCount)
        {
            if(prefs==null) throw new ArgumentNullException(nameof(prefs));
            if(levelCount<1) throw new ArgumentOutOfRangeException(nameof(levelCount));
            this.prefs=prefs;
            LevelCount=levelCount;
            if(!prefs.HasKey(ClearedKey) && prefs.HasKey(LegacyKey))
            {
                prefs.SetInt(ClearedKey,Clamp(prefs.GetInt(LegacyKey,0)));
                prefs.DeleteKey(LegacyKey);
                prefs.Save();
            }
            Cleared=Clamp(prefs.GetInt(ClearedKey,0));
        }

        public bool IsUnlocked(int index) { return index>=0 && index<LevelCount && index<=Cleared; }
        public bool IsCleared(int index) { return index>=0 && index<Cleared; }

        public bool MarkCleared(int index)
        {
            if(index!=Cleared || index>=LevelCount) return false;
            Cleared++;
            prefs.SetInt(ClearedKey,Cleared);
            prefs.Save();
            return true;
        }

        public void Reset()
        {
            Cleared=0;
            prefs.DeleteKey(ClearedKey);
            prefs.Save();
        }

        int Clamp(int value) { return Math.Max(0,Math.Min(LevelCount,value)); }
    }
}
```

- [ ] **Bước 6: Tạo `Assets/JamJam/Runtime/WoolSettings.cs`**

```csharp
using System;

namespace JamJam
{
    public sealed class WoolSettings
    {
        public const string SoundKey="JamJam.Sound";
        public const string MusicKey="JamJam.Music";
        public const string VibrationKey="JamJam.Vibration";
        readonly IWoolPrefs prefs;

        public WoolSettings(IWoolPrefs prefs) { this.prefs=prefs ?? throw new ArgumentNullException(nameof(prefs)); }

        public bool Sound { get => Get(SoundKey); set => Set(SoundKey,value); }
        public bool Music { get => Get(MusicKey); set => Set(MusicKey,value); }
        public bool Vibration { get => Get(VibrationKey); set => Set(VibrationKey,value); }

        bool Get(string key) { return prefs.GetInt(key,1)!=0; }

        void Set(string key,bool value)
        {
            prefs.SetInt(key,value?1:0);
            prefs.Save();
        }
    }
}
```

- [ ] **Bước 7: Chạy GREEN**

Run: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet`
Kỳ vọng: `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions`, exit 0. Số gameplay/route phải giữ nguyên.

- [ ] **Bước 8: Dừng, báo user kết quả RED/GREEN.**

---

### Task 2: Session, PlayerPrefs adapter và nối `WoolScene`

**Interfaces — Consumes:** Task 1. **Produces:**
- `WoolPlayerPrefs : IWoolPrefs`.
- `static class WoolSession { const string LobbyScene="Lobby"; const string GameScene="WoolJam"; static int SelectedLevel {get;set;} = -1; static void Clear(); }`
- `WoolScene.HomeButtons : Button[]`, `WoolScene.Home()`.

- [ ] **Bước 1: Tạo `Assets/JamJam/Runtime/WoolPlayerPrefs.cs`**

```csharp
using UnityEngine;

namespace JamJam
{
    public sealed class WoolPlayerPrefs : IWoolPrefs
    {
        public int GetInt(string key,int fallback) { return PlayerPrefs.GetInt(key,fallback); }
        public void SetInt(string key,int value) { PlayerPrefs.SetInt(key,value); }
        public bool HasKey(string key) { return PlayerPrefs.HasKey(key); }
        public void DeleteKey(string key) { PlayerPrefs.DeleteKey(key); }
        public void Save() { PlayerPrefs.Save(); }
    }
}
```

- [ ] **Bước 2: Tạo `Assets/JamJam/Runtime/WoolSession.cs`**

```csharp
namespace JamJam
{
    public static class WoolSession
    {
        public const string LobbyScene="Lobby";
        public const string GameScene="WoolJam";
        public static int SelectedLevel { get; set; } = -1;
        public static void Clear() { SelectedLevel=-1; }
    }
}
```

- [ ] **Bước 3: Sửa `WoolScene.cs`**

**Trước**
```1:5:Assets/JamJam/Runtime/WoolScene.cs
using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;
```

**Sau**
```csharp
using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
```

**Trước**
```25:28:Assets/JamJam/Runtime/WoolScene.cs
        public Button NextButton;
        public WoolGame State;
        public int CurrentLevel { get; private set; }
        readonly List<WoolBlockView> blockViews = new List<WoolBlockView>();
```

**Sau**
```csharp
        public Button NextButton;
        public Button[] HomeButtons;
        public WoolGame State;
        public int CurrentLevel { get; private set; }
        WoolProgress progress;
        readonly List<WoolBlockView> blockViews = new List<WoolBlockView>();
```

**Trước**
```50:58:Assets/JamJam/Runtime/WoolScene.cs
        void Start()
        {
            PauseButton.onClick.AddListener(Pause);
            ResumeButton.onClick.AddListener(Resume);
            RetryButton.onClick.AddListener(Replay);
            ResultRetryButton.onClick.AddListener(Replay);
            NextButton.onClick.AddListener(Next);
            LoadLevel(Mathf.Clamp(PlayerPrefs.GetInt("JamJam.Level", 0), 0, Levels.Length - 1));
        }
```

**Sau**
```csharp
        void Start()
        {
            progress = new WoolProgress(new WoolPlayerPrefs(), Levels.Length);
            PauseButton.onClick.AddListener(Pause);
            ResumeButton.onClick.AddListener(Resume);
            RetryButton.onClick.AddListener(Replay);
            ResultRetryButton.onClick.AddListener(Replay);
            NextButton.onClick.AddListener(Next);
            if (HomeButtons != null)
                foreach (var button in HomeButtons)
                    if (button != null) button.onClick.AddListener(Home);
            var selected = WoolSession.SelectedLevel;
            WoolSession.Clear();
            LoadLevel(selected >= 0 ? selected : progress.Current);
        }
```

**Trước**
```160:162:Assets/JamJam/Runtime/WoolScene.cs
        public void Resume() { if (State == null) return; State.SetPaused(false); PausePanel.SetActive(false); lastTime = Time.timeAsDouble; }
        public void Replay() { LoadLevel(CurrentLevel); }
        public void Next() { if (State != null && State.Status == RoundStatus.Won) LoadLevel((CurrentLevel + 1) % Levels.Length); }
```

**Sau**
```csharp
        public void Resume() { if (State == null) return; State.SetPaused(false); PausePanel.SetActive(false); lastTime = Time.timeAsDouble; }
        public void Replay() { LoadLevel(CurrentLevel); }
        public void Next()
        {
            if (State == null || State.Status != RoundStatus.Won) return;
            if (CurrentLevel >= Levels.Length - 1) Home(); else LoadLevel(CurrentLevel + 1);
        }
        public void Home() { clock?.Dispose(); clock = null; SceneManager.LoadScene(WoolSession.LobbyScene); }
```

**Trước**
```179:184:Assets/JamJam/Runtime/WoolScene.cs
                if (State.Status == RoundStatus.Won)
                {
                    PlayerPrefs.SetInt("JamJam.Level", Mathf.Min(CurrentLevel + 1, Levels.Length - 1));
                    PlayerPrefs.Save();
                    Burst();
                }
```

**Sau**
```csharp
                if (State.Status == RoundStatus.Won)
                {
                    progress.MarkCleared(CurrentLevel);
                    Burst();
                }
```

- [ ] **Bước 4: Sửa `WoolEditorChecks.cs` để batch lưu/khôi phục key mới**

**Trước**
```68:69:Assets/JamJam/Editor/WoolEditorChecks.cs
                if(SessionState.GetBool("WoolJam.HadProgress",false)) PlayerPrefs.SetInt("JamJam.Level",SessionState.GetInt("WoolJam.OldProgress",0));
                else PlayerPrefs.DeleteKey("JamJam.Level");
```

**Sau**
```csharp
                if(SessionState.GetBool("WoolJam.HadProgress",false)) PlayerPrefs.SetInt(WoolProgress.ClearedKey,SessionState.GetInt("WoolJam.OldProgress",0));
                else PlayerPrefs.DeleteKey(WoolProgress.ClearedKey);
```

**Trước**
```131:132:Assets/JamJam/Editor/WoolEditorChecks.cs
                SessionState.SetBool("WoolJam.HadProgress",PlayerPrefs.HasKey("JamJam.Level"));
                SessionState.SetInt("WoolJam.OldProgress",PlayerPrefs.GetInt("JamJam.Level",0));
```

**Sau**
```csharp
                SessionState.SetBool("WoolJam.HadProgress",PlayerPrefs.HasKey(WoolProgress.ClearedKey));
                SessionState.SetInt("WoolJam.OldProgress",PlayerPrefs.GetInt(WoolProgress.ClearedKey,0));
```

- [ ] **Bước 5: Compile trong Editor đang mở**

Run: `unity command --caller plugin --skill unity-cli` với lệnh compile/recompile mà Editor liệt kê (`scripts/compile`), rồi đọc console (`observability/console`).
Kỳ vọng: 0 lỗi compile mới. Harness vẫn PASS.

- [ ] **Bước 6: Kiểm hành vi cũ** — mở `WoolJam.unity`, Play, `eval` đọc `FindFirstObjectByType<JamJam.WoolScene>().CurrentLevel` bằng `new WoolProgress(new WoolPlayerPrefs(),3).Current`. Thoát Play. Dừng, báo user.

---

### Task 3: Nút Home trong gameplay

**Interfaces — Consumes:** `WoolScene.HomeButtons` (Task 2). **Produces:** `WoolSceneBuilder` helper `internal static`: `LoadUiResources()`, `Overlay`, `Panel`, `Label`, `MakeButton` (signature giữ nguyên), menu `JamJam/Add Home Buttons`; `WoolLobbyBuilder.ApplyBuildScenes()` được dùng ở Task 4 (Task 3 gọi nó, nên tạo cùng file ở Task 4 trước khi compile — xem bước 1).

- [ ] **Bước 1: Sửa `WoolSceneBuilder.cs`**

**Trước**
```60:62:Assets/JamJam/Editor/WoolSceneBuilder.cs
            PlayerSettings.defaultScreenWidth=540; PlayerSettings.defaultScreenHeight=960;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[] { new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity",false) };
```

**Sau**
```csharp
            PlayerSettings.defaultScreenWidth=540; PlayerSettings.defaultScreenHeight=960;
            EditorSceneManager.SaveScene(scene,ScenePath);
            ApplyBuildScenes();
```

**Trước**
```185:186:Assets/JamJam/Editor/WoolSceneBuilder.cs
            result.gameObject.SetActive(false);
        }
```

**Sau**
```csharp
            result.gameObject.SetActive(false);
            AddHome(game,pause,result);
        }

        [MenuItem("JamJam/Add Home Buttons")]
        public static void AddHomeButtons()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before editing the scene");
            for(var i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before adding Home buttons");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ScenePath) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var game=UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            if(game==null) throw new InvalidOperationException("WoolScene is missing");
            LoadUiResources();
            AddHome(game,game.PausePanel.transform,game.ResultPanel.transform);
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            ApplyBuildScenes();
            Debug.Log("Home buttons ready");
        }

        internal static void LoadUiResources()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panelSprite=AssetDatabase.LoadAssetAtPath<Sprite>(WoolAssetsBuilder.Root+"/Textures/RoundedPanel.png");
            if(panelSprite==null) throw new InvalidOperationException("RoundedPanel sprite is missing");
        }

        internal static void ApplyBuildScenes()
        {
            var scenes=new List<EditorBuildSettingsScene>();
            if(File.Exists(WoolLobbyBuilder.ScenePath)) scenes.Add(new EditorBuildSettingsScene(WoolLobbyBuilder.ScenePath,true));
            scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity",false));
            EditorBuildSettings.scenes=scenes.ToArray();
        }

        static void AddHome(WoolScene game,Transform pause,Transform result)
        {
            var buttons=new List<Button>();
            foreach(var (parent,position) in new[] { (pause,new Vector2(0,-330)),(result,new Vector2(0,-455)) })
            {
                var existing=parent.Find("Home");
                if(existing!=null) { buttons.Add(existing.GetComponent<Button>()); continue; }
                MakeButton(parent,"Home","HOME",position,new Vector2(400,100),new Color(1,0.98f,0.91f),ink,out var button);
                buttons.Add(button);
            }
            game.HomeButtons=buttons.ToArray();
        }
```

Đổi bốn helper từ `static void` sang `internal static void`, thân giữ nguyên:

**Trước**
```188:188:Assets/JamJam/Editor/WoolSceneBuilder.cs
        static void Overlay(Transform parent,string name,out RectTransform rect)
```
```196:196:Assets/JamJam/Editor/WoolSceneBuilder.cs
        static void Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color,out RectTransform rect)
```
```203:203:Assets/JamJam/Editor/WoolSceneBuilder.cs
        static void Label(Transform parent,string name,string value,Vector2 position,Vector2 size,int fontSize,Color color,out Text text)
```
```210:210:Assets/JamJam/Editor/WoolSceneBuilder.cs
        static void MakeButton(Transform parent,string name,string value,Vector2 position,Vector2 size,Color color,Color textColor,out Button button)
```

**Sau**
```csharp
        internal static void Overlay(Transform parent,string name,out RectTransform rect)
        internal static void Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color,out RectTransform rect)
        internal static void Label(Transform parent,string name,string value,Vector2 position,Vector2 size,int fontSize,Color color,out Text text)
        internal static void MakeButton(Transform parent,string name,string value,Vector2 position,Vector2 size,Color color,Color textColor,out Button button)
```

`ApplyBuildScenes` tham chiếu `WoolLobbyBuilder.ScenePath`, nên làm Task 4 bước 3 (tạo `WoolLobbyBuilder.cs`) trước khi compile Task 3.

- [ ] **Bước 2: Compile** (sau khi có `WoolLobbyBuilder.cs`). Kỳ vọng 0 lỗi.

- [ ] **Bước 3: Chạy menu trên Editor đang mở** — `unity command eval --caller plugin --skill unity-cli 'JamJam.Editor.WoolSceneBuilder.AddHomeButtons();'`. Kỳ vọng log "Home buttons ready"; chạy lần hai không tạo nút trùng (đếm `PausePanel.transform.Find("Home")` duy nhất, `HomeButtons.Length==2`).

- [ ] **Bước 4: Kiểm `git diff --stat Assets/Scenes/WoolJam.unity`** chỉ có thêm object Home và field `HomeButtons`; các Text cũ không đổi. Dừng, báo user.

---

### Task 4: Lobby scene, Level Select, Settings

**Interfaces — Consumes:** Task 1–3. **Produces:** `Lobby.unity`, `Generated/Prefabs/LevelCard.prefab`, `WoolLobbyBuilder.ScenePath`, `WoolLobbyBuilder.Build()`.

- [ ] **Bước 1: Tạo `Assets/JamJam/Runtime/WoolLevelCard.cs`**

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolLevelCard : MonoBehaviour
    {
        public Button Button;
        public RectTransform Content;
        public CanvasGroup Group;
        public Text Number;
        public Text Title;
        public GameObject Check;
        public GameObject Lock;
        public GameObject Current;
        public float Shake;

        public void Bind(int index,string title,bool unlocked,bool cleared,bool current)
        {
            Number.text=(index+1).ToString("00");
            Title.text=unlocked?title:"Locked";
            Check.SetActive(cleared);
            Lock.SetActive(!unlocked);
            Current.SetActive(current);
            Group.alpha=unlocked?1:0.55f;
        }

        public void Animate(float delta)
        {
            Shake=Mathf.Max(0,Shake-delta);
            Content.anchoredPosition=new Vector2(Mathf.Sin(Shake*95)*Shake*40,0);
        }
    }
}
```

- [ ] **Bước 2: Tạo `Assets/JamJam/Runtime/WoolLobby.cs`**

```csharp
using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolLobby : MonoBehaviour
    {
        public WoolLevel[] Levels;
        public WoolLevelCard CardPrefab;
        public RectTransform CardContainer;
        public RectTransform Kitten;
        public GameObject LobbyPanel;
        public GameObject LevelsPanel;
        public GameObject SettingsPanel;
        public GameObject ConfirmPanel;
        public Button PlayButton;
        public Text PlayLabel;
        public Button LevelsButton;
        public Button SettingsButton;
        public Button BackButton;
        public Button CloseSettingsButton;
        public Button ResetButton;
        public Button ConfirmYesButton;
        public Button ConfirmNoButton;
        public Toggle SoundToggle;
        public Toggle MusicToggle;
        public Toggle VibrationToggle;
        readonly List<WoolLevelCard> cards=new List<WoolLevelCard>();
        WoolProgress progress;
        WoolSettings settings;
        IDisposable clock;
        Vector2 kittenRest;
        double lastTime;
        float elapsed;
        bool loading;

        void Awake()
        {
            var prefs=new WoolPlayerPrefs();
            progress=new WoolProgress(prefs,Levels.Length);
            settings=new WoolSettings(prefs);
            kittenRest=Kitten.anchoredPosition;
        }

        void OnEnable()
        {
            lastTime=Time.unscaledTimeAsDouble;
            clock?.Dispose();
            clock=Observable.Interval(TimeSpan.FromSeconds(1d/60d),UnityTimeProvider.UpdateIgnoreTimeScale).Subscribe(_=>Tick());
        }

        void OnDisable() { clock?.Dispose(); clock=null; }

        void Start()
        {
            PlayButton.onClick.AddListener(Play);
            LevelsButton.onClick.AddListener(ShowLevels);
            BackButton.onClick.AddListener(ShowLobby);
            SettingsButton.onClick.AddListener(OpenSettings);
            CloseSettingsButton.onClick.AddListener(CloseSettings);
            ResetButton.onClick.AddListener(AskReset);
            ConfirmYesButton.onClick.AddListener(ConfirmReset);
            ConfirmNoButton.onClick.AddListener(CancelReset);
            SoundToggle.onValueChanged.AddListener(value=>settings.Sound=value);
            MusicToggle.onValueChanged.AddListener(value=>settings.Music=value);
            VibrationToggle.onValueChanged.AddListener(value=>settings.Vibration=value);
            for(var i=0;i<Levels.Length;i++)
            {
                var card=Instantiate(CardPrefab,CardContainer);
                card.name="LevelCard_"+(i+1).ToString("00");
                var index=i;
                card.Button.onClick.AddListener(()=>Choose(index));
                cards.Add(card);
            }
            SettingsPanel.SetActive(false);
            ConfirmPanel.SetActive(false);
            ShowLobby();
        }

        public void Play() { Load(progress.Current); }

        public void Choose(int index)
        {
            if(index<0 || index>=cards.Count) return;
            if(!progress.IsUnlocked(index)) { cards[index].Shake=0.35f; return; }
            Load(index);
        }

        public void ShowLobby() { LobbyPanel.SetActive(true); LevelsPanel.SetActive(false); Refresh(); }
        public void ShowLevels() { LobbyPanel.SetActive(false); LevelsPanel.SetActive(true); Refresh(); }

        public void OpenSettings()
        {
            SoundToggle.SetIsOnWithoutNotify(settings.Sound);
            MusicToggle.SetIsOnWithoutNotify(settings.Music);
            VibrationToggle.SetIsOnWithoutNotify(settings.Vibration);
            ConfirmPanel.SetActive(false);
            SettingsPanel.SetActive(true);
        }

        public void CloseSettings() { ConfirmPanel.SetActive(false); SettingsPanel.SetActive(false); }
        public void AskReset() { ConfirmPanel.SetActive(true); }
        public void CancelReset() { ConfirmPanel.SetActive(false); }

        public void ConfirmReset()
        {
            progress.Reset();
            ConfirmPanel.SetActive(false);
            Refresh();
        }

        void Load(int index)
        {
            if(loading) return;
            loading=true;
            WoolSession.SelectedLevel=index;
            SceneManager.LoadScene(WoolSession.GameScene);
        }

        void Refresh()
        {
            PlayLabel.text="PLAY  "+(progress.Current+1).ToString("00");
            for(var i=0;i<cards.Count;i++)
            {
                var cleared=progress.IsCleared(i);
                cards[i].Bind(i,Levels[i].Title,progress.IsUnlocked(i),cleared,i==progress.Current && !cleared);
            }
        }

        void Tick()
        {
            var now=Time.unscaledTimeAsDouble;
            var delta=Mathf.Clamp((float)(now-lastTime),0,0.1f);
            lastTime=now;
            elapsed+=delta;
            Kitten.anchoredPosition=kittenRest+Vector2.up*(Mathf.Sin(elapsed*3)*12);
            foreach(var card in cards) card.Animate(delta);
        }
    }
}
```

- [ ] **Bước 3: Tạo `Assets/JamJam/Editor/WoolLobbyBuilder.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static class WoolLobbyBuilder
    {
        public const string ScenePath="Assets/Scenes/Lobby.unity";
        const string CardPath=WoolAssetsBuilder.Root+"/Prefabs/LevelCard.prefab";
        const string KittenPath="Assets/JamJam/Art/Sprites/Kitten-Wool.png";
        static readonly Color ink=new Color(0.16f,0.23f,0.28f);
        static readonly Color cream=new Color(1,0.98f,0.91f);
        static readonly Color sand=new Color(0.94f,0.87f,0.7f);
        static readonly Color green=new Color(0.23f,0.71f,0.54f);
        static readonly Color orange=new Color(0.95f,0.62f,0.25f);
        static readonly Color grey=new Color(0.55f,0.57f,0.6f);

        [MenuItem("JamJam/Build Lobby Scene")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building the lobby");
            for(var i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before building the lobby");
            WoolSceneBuilder.LoadUiResources();
            var levels=new List<WoolLevel>();
            for(var i=1;;i++)
            {
                var level=AssetDatabase.LoadAssetAtPath<WoolLevel>(WoolAssetsBuilder.Root+"/Levels/Level_"+i+".asset");
                if(level==null) break;
                levels.Add(level);
            }
            if(levels.Count==0) throw new InvalidOperationException("No Wool levels found");
            BuildCard(out var card);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Main Camera"); cameraObject.tag="MainCamera";
            var camera=cameraObject.AddComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(0.97f,0.91f,0.78f);
            cameraObject.AddComponent<AudioListener>();
            var events=new GameObject("EventSystem"); events.AddComponent<EventSystem>(); events.AddComponent<InputSystemUIInputModule>();
            var lobby=new GameObject("Lobby").AddComponent<WoolLobby>();
            lobby.Levels=levels.ToArray(); lobby.CardPrefab=card;
            BuildCanvas(lobby);
            EditorSceneManager.SaveScene(scene,ScenePath);
            WoolSceneBuilder.ApplyBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Lobby scene generated");
        }

        static void BuildCanvas(WoolLobby lobby)
        {
            var go=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1080,1920); scaler.matchWidthOrHeight=0.5f;
            var parent=go.transform;

            FullScreen(parent,"LobbyPanel",out var home); lobby.LobbyPanel=home.gameObject;
            WoolSceneBuilder.MakeButton(home,"Settings","SETTINGS",new Vector2(380,830),new Vector2(240,100),cream,ink,out lobby.SettingsButton);
            WoolSceneBuilder.Label(home,"Brand","WOOL JAM",new Vector2(0,580),new Vector2(960,170),130,ink,out var brand); brand.fontStyle=FontStyle.Bold;
            WoolSceneBuilder.Label(home,"Tagline","UNWIND THE DRAGON. SAVE THE KITTEN.",new Vector2(0,470),new Vector2(960,50),30,new Color(0.29f,0.32f,0.28f),out _);
            var kitten=new GameObject("Kitten",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
            kitten.SetParent(home,false); kitten.anchoredPosition=new Vector2(0,110); kitten.sizeDelta=new Vector2(600,600);
            var kittenImage=kitten.GetComponent<Image>(); kittenImage.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(KittenPath); kittenImage.preserveAspect=true; kittenImage.raycastTarget=false;
            lobby.Kitten=kitten;
            WoolSceneBuilder.MakeButton(home,"Play","PLAY  01",new Vector2(0,-340),new Vector2(660,160),green,Color.white,out lobby.PlayButton);
            lobby.PlayLabel=lobby.PlayButton.GetComponentInChildren<Text>(); lobby.PlayLabel.fontSize=60;
            WoolSceneBuilder.MakeButton(home,"Levels","LEVELS",new Vector2(0,-540),new Vector2(660,120),cream,ink,out lobby.LevelsButton);
            lobby.LevelsButton.GetComponentInChildren<Text>().fontSize=44;

            FullScreen(parent,"LevelsPanel",out var list); lobby.LevelsPanel=list.gameObject;
            WoolSceneBuilder.MakeButton(list,"Back","BACK",new Vector2(-390,830),new Vector2(220,100),cream,ink,out lobby.BackButton);
            WoolSceneBuilder.Label(list,"Title","LEVELS",new Vector2(0,830),new Vector2(500,110),72,ink,out var listTitle); listTitle.fontStyle=FontStyle.Bold;
            var scroll=new GameObject("Scroll",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect)).GetComponent<RectTransform>();
            scroll.SetParent(list,false); scroll.anchoredPosition=new Vector2(0,-70); scroll.sizeDelta=new Vector2(920,1560);
            scroll.GetComponent<Image>().color=new Color(1,1,1,0);
            var content=new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(scroll,false); content.anchorMin=new Vector2(0,1); content.anchorMax=Vector2.one; content.pivot=new Vector2(0.5f,1); content.anchoredPosition=Vector2.zero; content.sizeDelta=Vector2.zero;
            var layout=content.GetComponent<VerticalLayoutGroup>(); layout.spacing=28; layout.padding=new RectOffset(20,20,20,20); layout.childAlignment=TextAnchor.UpperCenter;
            layout.childControlWidth=true; layout.childControlHeight=false; layout.childForceExpandWidth=true; layout.childForceExpandHeight=false;
            content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scrollRect=scroll.GetComponent<ScrollRect>(); scrollRect.content=content; scrollRect.horizontal=false; scrollRect.movementType=ScrollRect.MovementType.Elastic;
            lobby.CardContainer=content;
            list.gameObject.SetActive(false);

            WoolSceneBuilder.Overlay(parent,"SettingsPanel",out var settings); lobby.SettingsPanel=settings.gameObject;
            WoolSceneBuilder.Panel(settings,"Card",Vector2.zero,new Vector2(820,900),new Color(1,0.98f,0.9f),out var card);
            WoolSceneBuilder.Label(card,"Title","SETTINGS",new Vector2(0,350),new Vector2(700,100),60,ink,out var settingsTitle); settingsTitle.fontStyle=FontStyle.Bold;
            MakeToggle(card,"Sound","SOUND",new Vector2(0,200),out lobby.SoundToggle);
            MakeToggle(card,"Music","MUSIC",new Vector2(0,80),out lobby.MusicToggle);
            MakeToggle(card,"Vibration","VIBRATION",new Vector2(0,-40),out lobby.VibrationToggle);
            WoolSceneBuilder.MakeButton(card,"Reset","RESET PROGRESS",new Vector2(0,-200),new Vector2(600,100),sand,ink,out lobby.ResetButton);
            WoolSceneBuilder.MakeButton(card,"Close","CLOSE",new Vector2(0,-340),new Vector2(600,110),green,Color.white,out lobby.CloseSettingsButton);
            WoolSceneBuilder.Overlay(settings,"ConfirmPanel",out var confirm); lobby.ConfirmPanel=confirm.gameObject;
            WoolSceneBuilder.Panel(confirm,"Card",Vector2.zero,new Vector2(760,460),cream,out var confirmCard);
            WoolSceneBuilder.Label(confirmCard,"Title","RESET PROGRESS?",new Vector2(0,120),new Vector2(700,90),50,ink,out var confirmTitle); confirmTitle.fontStyle=FontStyle.Bold;
            WoolSceneBuilder.Label(confirmCard,"Body","All levels will be locked again.",new Vector2(0,30),new Vector2(700,60),30,ink,out _);
            WoolSceneBuilder.MakeButton(confirmCard,"Yes","RESET",new Vector2(-170,-130),new Vector2(300,100),new Color(0.82f,0.41f,0.25f),Color.white,out lobby.ConfirmYesButton);
            WoolSceneBuilder.MakeButton(confirmCard,"No","CANCEL",new Vector2(170,-130),new Vector2(300,100),sand,ink,out lobby.ConfirmNoButton);
            confirm.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);
        }

        static void BuildCard(out WoolLevelCard card)
        {
            var root=new GameObject("LevelCard",typeof(RectTransform),typeof(CanvasGroup)).GetComponent<RectTransform>();
            root.sizeDelta=new Vector2(860,170);
            var view=root.gameObject.AddComponent<WoolLevelCard>(); view.Group=root.GetComponent<CanvasGroup>();
            WoolSceneBuilder.Panel(root,"Content",Vector2.zero,Vector2.zero,cream,out var content);
            content.anchorMin=Vector2.zero; content.anchorMax=Vector2.one; content.offsetMin=content.offsetMax=Vector2.zero;
            var image=content.GetComponent<Image>(); image.raycastTarget=true; view.Content=content;
            view.Button=content.gameObject.AddComponent<Button>(); view.Button.targetGraphic=image;
            WoolSceneBuilder.Label(content,"Number","01",new Vector2(-330,0),new Vector2(140,110),64,ink,out view.Number); view.Number.fontStyle=FontStyle.Bold;
            WoolSceneBuilder.Label(content,"Title","First Threads",new Vector2(-20,0),new Vector2(440,90),38,ink,out view.Title); view.Title.alignment=TextAnchor.MiddleLeft;
            Badge(content,"Check","CLEAR",green,out view.Check);
            Badge(content,"Lock","LOCKED",grey,out view.Lock);
            Badge(content,"Current","PLAY",orange,out view.Current);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,CardPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            card=AssetDatabase.LoadAssetAtPath<GameObject>(CardPath).GetComponent<WoolLevelCard>();
        }

        static void Badge(Transform parent,string name,string value,Color color,out GameObject badge)
        {
            WoolSceneBuilder.Panel(parent,name,new Vector2(310,0),new Vector2(180,70),color,out var rect);
            WoolSceneBuilder.Label(rect,"Label",value,Vector2.zero,new Vector2(170,60),28,Color.white,out var label); label.fontStyle=FontStyle.Bold;
            badge=rect.gameObject;
        }

        static void MakeToggle(Transform parent,string name,string caption,Vector2 position,out Toggle toggle)
        {
            WoolSceneBuilder.Panel(parent,name,position,new Vector2(640,100),sand,out var row);
            row.GetComponent<Image>().raycastTarget=true;
            WoolSceneBuilder.Label(row,"Label",caption,new Vector2(-110,0),new Vector2(380,80),34,ink,out var label); label.alignment=TextAnchor.MiddleLeft; label.fontStyle=FontStyle.Bold;
            WoolSceneBuilder.Panel(row,"Box",new Vector2(230,0),new Vector2(130,70),cream,out var box);
            WoolSceneBuilder.Panel(box,"Check",Vector2.zero,new Vector2(110,50),green,out var check);
            toggle=row.gameObject.AddComponent<Toggle>(); toggle.targetGraphic=box.GetComponent<Image>(); toggle.graphic=check.GetComponent<Image>(); toggle.isOn=true;
        }

        static void FullScreen(Transform parent,string name,out RectTransform rect)
        {
            rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
```

- [ ] **Bước 4: Compile** (cùng Task 3 bước 2). Kỳ vọng 0 lỗi.

- [ ] **Bước 5: Build Lobby** — `unity command eval --caller plugin --skill unity-cli 'JamJam.Editor.WoolLobbyBuilder.Build();'`. Kỳ vọng: có `Assets/Scenes/Lobby.unity`, `LevelCard.prefab`; `EditorBuildSettings.scenes` = Lobby, WoolJam, SampleScene(off).

- [ ] **Bước 6: Kiểm Play Mode bằng `eval`** (mở Lobby, `editor_play`, mỗi bước đọc kết quả):
  1. Đặt PlayerPrefs `JamJam.Cleared=1` trước Play. Trong Lobby: `PlayLabel.text=="PLAY  02"`; `ShowLevels()` → `CardContainer.childCount==3`; thẻ 3 có `Lock` active.
  2. `Choose(2)` → scene vẫn là Lobby, `cards[2].Shake>0`.
  3. `OpenSettings()`; `MusicToggle.isOn=false` → `PlayerPrefs.GetInt("JamJam.Music",1)==0`.
  4. `AskReset()` → `ConfirmPanel.activeSelf`; `ConfirmReset()` → `PlayerPrefs.HasKey("JamJam.Cleared")==false`, `PlayLabel.text=="PLAY  01"`.
  5. `Play()` gọi hai lần liên tiếp → `SceneManager.GetActiveScene().name=="WoolJam"`, `CurrentLevel==0`.
  6. Trong WoolJam: `Pause()` rồi bấm `HomeButtons[0].onClick.Invoke()` → scene `Lobby`.
  7. Đặt `JamJam.Cleared=2`, vào level 3 bằng `Choose(2)`; đặt thắng bằng reflection `typeof(JamJam.WoolGame).GetProperty("Status").SetValue(scene.State,JamJam.RoundStatus.Won)`, chờ 2 frame để `Tick` ghi tiến độ, rồi `NextButton.onClick.Invoke()`; kỳ vọng `PlayerPrefs.GetInt("JamJam.Cleared",0)==3` và scene về `Lobby`.
  8. Dừng Play; khôi phục PlayerPrefs `JamJam.Cleared`, `JamJam.Music` về giá trị ban đầu đã ghi lại ở đầu bước.

- [ ] **Bước 7: Chụp ảnh** Lobby, Level Select, Settings ở 1080×1920 và Lobby ở 1080×2400 bằng nhóm lệnh `capture`; lưu vào `docs/lobby-*.png`.

- [ ] **Bước 8: Harness lần cuối** — `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet` PASS. Dừng, báo user kèm ảnh; không commit.

## Tóm tắt

- Thêm 7 runtime script (`IWoolPrefs`, `WoolPlayerPrefs`, `WoolProgress`, `WoolSettings`, `WoolSession`, `WoolLevelCard`, `WoolLobby`) và 1 editor builder (`WoolLobbyBuilder`).
- Sửa `WoolScene` (đọc level đã chọn, ghi tiến độ bằng `WoolProgress`, Home, Next về Lobby ở level cuối), `WoolSceneBuilder` (helper internal, menu Add Home Buttons idempotent, build scenes có Lobby), `WoolEditorChecks` (key `JamJam.Cleared`).
- Harness thêm 18 assertion tiến độ/settings; check cũ giữ nguyên.
- Trạng thái thẻ dùng nhãn chữ CLEAR / LOCKED / PLAY thay cho icon vì font `LegacyRuntime` không chắc có glyph ✓/🔒.
- Không chạy `Build Wool Jam Scene`; WoolJam.unity chỉ được chèn hai nút Home.

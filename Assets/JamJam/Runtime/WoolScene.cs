using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolScene : MonoBehaviour
    {
        public WoolArt Art;
        public WoolLevel[] Levels;
        public Transform World;
        public Transform Cushion;
        public Text LevelNumber;
        public Text ProgressNumber;
        public Image ProgressFill;
        public GameObject ResultPanel;
        public GameObject WonContent;
        public GameObject LostContent;
        public GameObject PausePanel;
        public Button PauseButton;
        public Button ResumeButton;
        public Button RetryButton;
        public Button ResultRetryButton;
        public Button NextButton;
        public Button[] HomeButtons;
        public GameObject ResultRays;
        public Button SpeedButton;
        public Text SpeedLabel;
        public Button UnlockTemplate;
        public WoolGame State;
        public static int Speed { get; private set; } = 1;
        public const float StepLimit = 0.1f;
        public const float UnlockDisabledAlpha = 0.5f;
        public static readonly Vector3 UnlockOffset = new Vector3(0, 0, -0.75f);
        public int CurrentLevel { get; private set; }
        public const float WinResultDelay = 0.9f;
        public const float LoseResultDelay = 0.6f;
        public bool ResultPending => resultDelay != null;
        public double ResultQueuedAt { get; private set; }
        WoolProgress progress;
        bool leaving;
        readonly List<WoolBlockView> blockViews = new List<WoolBlockView>();
        readonly List<Transform> units = new List<Transform>();
        readonly List<Transform> spoolViews = new List<Transform>();
        readonly List<TextMesh> spoolLabels = new List<TextMesh>();
        readonly List<TextMesh> spoolShadows = new List<TextMesh>();
        readonly List<LineRenderer> threads = new List<LineRenderer>();
        readonly List<Vector3> threadEnds = new List<Vector3>();
        readonly List<int> threadTargets = new List<int>();
        readonly List<WoolSlotView> slots = new List<WoolSlotView>();
        readonly List<Button> unlockButtons = new List<Button>();
        readonly List<CanvasGroup> unlockGroups = new List<CanvasGroup>();
        readonly List<Transform> confetti = new List<Transform>();
        readonly List<Vector3> confettiVelocity = new List<Vector3>();
        readonly List<float> pathDistances = new List<float>();
        readonly List<Mesh> roadMeshes = new List<Mesh>();
        readonly List<float> blockFlashShown = new List<float>();
        readonly List<float> slotFlashShown = new List<float>();
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock blockTint;
        WoolFeel feel;
        IDisposable clock;
        IDisposable resultDelay;
        Transform dragon;
        Transform kitten;
        Transform kittenBody;
        Camera gameCamera;
        float fittedAspect;
        float fittedSize;
        Vector3 fittedPosition;
        Quaternion fittedRotation = Quaternion.identity;
        Vector3 kittenRest;
        RoundStatus lastStatus;
        double lastTime;
        float celebration;
        float displayedHead;
        public const float CushionPad = 0.5f;
        const float ThreadStartWidth = 0.2f;
        const float ThreadEndWidth = 0.15f;

        public float CameraFitAspect => fittedAspect;

        public void FitCamera()
        {
            if (gameCamera == null) return;
            var aspect = gameCamera.aspect;
            var rotation = Quaternion.Euler(WoolStage.Pitch, 0, 0);
            var position = new Vector3(0, 0, WoolStage.TargetZ) - rotation * Vector3.forward * WoolStage.Distance;
            var size = WoolStage.Size(aspect);
            fittedAspect = aspect;
            fittedPosition = position;
            fittedRotation = rotation;
            fittedSize = size;
            if (feel != null) feel.Rebase(position, rotation, size);
            else { gameCamera.transform.SetPositionAndRotation(position, rotation); gameCamera.orthographicSize = size; }
            PlaceUnlockButtons();
        }

        void PlaceUnlockButtons()
        {
            if (gameCamera == null || fittedSize <= 0 || fittedAspect <= 0) return;
            var pixels = gameCamera.pixelRect;
            var inverse = Quaternion.Inverse(fittedRotation);
            for (var i = 0; i < unlockButtons.Count; i++)
            {
                var rect = (RectTransform)unlockButtons[i].transform;
                var parent = rect.parent as RectTransform;
                if (parent == null) continue;
                var view = inverse * (SlotPosition(i) + UnlockOffset - fittedPosition);
                var screen = new Vector2(pixels.x + (0.5f + view.x / (2 * fittedSize * fittedAspect)) * pixels.width, pixels.y + (0.5f + view.y / (2 * fittedSize)) * pixels.height);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local)) rect.localPosition = new Vector3(local.x, local.y, 0);
            }
        }

        public void ToggleSpeed()
        {
            Speed = Speed == 1 ? 2 : 1;
            ShowSpeed();
        }

        public static void ResetSpeed() { Speed = 1; }

        void ShowSpeed()
        {
            if (SpeedLabel != null) SpeedLabel.text = "x" + Speed;
        }

        public void UnlockButton(int slot, out Button button)
        {
            button = slot >= 0 && slot < unlockButtons.Count ? unlockButtons[slot] : null;
        }

        void Start()
        {
            progress = new WoolProgress(new WoolPlayerPrefs(), Levels.Length);
            if (SpeedButton != null) SpeedButton.onClick.AddListener(ToggleSpeed);
            ShowSpeed();
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

        void OnEnable()
        {
            lastTime = Time.timeAsDouble;
            clock?.Dispose();
            clock = Observable.Interval(TimeSpan.FromSeconds(1d / 60d), UnityTimeProvider.Update).Subscribe(_ => Tick());
        }

        void OnDisable() { clock?.Dispose(); clock = null; CancelResult(); }
        void OnDestroy()
        {
            CancelResult();
            foreach(var mesh in roadMeshes) if(mesh!=null) Destroy(mesh);
            roadMeshes.Clear();
        }

        public void LoadLevel(int index)
        {
            clock?.Dispose();
            clock = null;
            CancelResult();
            foreach (Transform child in World) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach(var mesh in roadMeshes) Destroy(mesh);
            roadMeshes.Clear();
            blockViews.Clear(); units.Clear(); spoolViews.Clear(); spoolLabels.Clear(); threads.Clear(); threadEnds.Clear(); threadTargets.Clear(); slots.Clear(); confetti.Clear(); confettiVelocity.Clear();
            spoolShadows.Clear();
            blockFlashShown.Clear(); slotFlashShown.Clear();
            CurrentLevel = Mathf.Clamp(index, 0, Levels.Length - 1);
            var level = Levels[CurrentLevel];
            gameCamera=Camera.main;
            State = new WoolGame(level.Data);
            if (feel == null) feel = gameObject.AddComponent<WoolFeel>();
            feel.Begin(this, gameCamera);
            feel.Restart(State);
            BuildUnlockButtons();
            ShowSpeed();
            FitCamera();
            displayedHead = State.Head;
            pathDistances.Clear(); pathDistances.Add(0);
            for (var i = 1; i < level.Path.Length; i++) pathDistances.Add(pathDistances[i - 1] + Vector3.Distance(level.Path[i - 1], level.Path[i]));
            ResultPanel.SetActive(false); PausePanel.SetActive(false);
            lastStatus = RoundStatus.Playing;
            celebration = 0;
            LevelNumber.text = (CurrentLevel + 1).ToString("00");
            BuildRoad(level.Path);
            if (Cushion != null) Cushion.localScale = new Vector3(level.Data.Columns * WoolRouting.CellSize + CushionPad, Cushion.localScale.y, level.Data.Rows * WoolRouting.CellSize + CushionPad);
            if (Cushion != null) Cushion.position = new Vector3(Cushion.position.x, Cushion.position.y, WoolRouting.BoardCenterZ);
            for (var i = 0; i < State.Blocks.Count; i++)
            {
                var data = State.Blocks[i].Definition;
                var go = Instantiate(Art.Block, World);
                go.name = "Block_" + i;
                var view = go.GetComponent<WoolBlockView>();
                view.Owner = this; view.Index = i;
                WoolBlockMesh.Get(data.Width, data.Height, out var bodyMesh);
                view.Body.GetComponent<MeshFilter>().sharedMesh = bodyMesh;
                blockTint ??= new MaterialPropertyBlock();
                blockTint.SetColor(BaseColor, Art.Yarn[data.Color].GetColor(BaseColor));
                view.Body.GetComponent<Renderer>().SetPropertyBlock(blockTint);
                var quarter=WoolShape.ArrowQuarter(data.Direction);
                var vertical=quarter%2==0;
                view.Arrow.localRotation=Quaternion.Euler(90,0,0)*Quaternion.Euler(0,0,-quarter*90);
                view.Arrow.localScale=new Vector3(0.56f*WoolRouting.CellSize,(vertical?data.Height:data.Width)*WoolRouting.CellSize*0.78f,1);
                var collider = go.GetComponent<BoxCollider>();
                collider.size = new Vector3((data.Width - WoolBlockMesh.Inset * 2) * WoolRouting.CellSize, WoolBlockMesh.Thickness + 0.2f, (data.Height - WoolBlockMesh.Inset * 2) * WoolRouting.CellSize);
                collider.center = new Vector3(0, (WoolBlockMesh.Thickness + 0.2f) * 0.5f, 0);
                go.transform.SetPositionAndRotation(BlockPosition(data, level.Data), Quaternion.Euler(0, WoolShape.RestYaw(data.Direction), 0));
                blockViews.Add(view);
                blockFlashShown.Add(0);
            }
            for (var i = 0; i < State.Spools.Count; i++)
            {
                var slot = Instantiate(Art.Slot, World).GetComponent<WoolSlotView>();
                slot.transform.position = SlotPosition(i); slot.Owner = this; slot.Index = i;
                slots.Add(slot);
                slotFlashShown.Add(0);
                var spool = Instantiate(Art.Spool, World).transform;
                spool.position = SlotPosition(i) + Vector3.up * 0.33f;
                spoolViews.Add(spool);
                var label = spool.GetComponentInChildren<TextMesh>();
                spoolLabels.Add(label);
                var shadow = label.transform.Find("Shadow");
                spoolShadows.Add(shadow != null ? shadow.GetComponent<TextMesh>() : null);
                var lineObject = new GameObject("Thread_" + i);
                lineObject.transform.SetParent(World, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.positionCount = 20; line.startWidth = ThreadStartWidth; line.endWidth = ThreadEndWidth; line.textureMode = LineTextureMode.Tile;
                line.numCapVertices = 3; line.numCornerVertices = 3; line.generateLightingData = true;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                threads.Add(line);
                threadEnds.Add(Vector3.zero);
                threadTargets.Add(-1);
            }
            for (var i = 0; i < State.Yarn.Count; i++)
            {
                var unit = Instantiate(Art.Segment, World).transform;
                Dye(unit, State.Yarn[i].Color);
                units.Add(unit);
            }
            dragon = Instantiate(Art.Dragon, World).transform;

            kitten = Instantiate(Art.Kitten, World).transform;
            kittenBody = kitten.Find("Body");
            Evaluate(level.Data.PathLength, out kittenRest, out var endDirection);
            kittenRest += endDirection * 0.85f;
            kitten.position = kittenRest;
            kitten.localScale=Vector3.one*1.25f;
            kitten.rotation=gameCamera.transform.rotation;
            lastTime = Time.timeAsDouble;
            if (isActiveAndEnabled) clock = Observable.Interval(TimeSpan.FromSeconds(1d / 60d), UnityTimeProvider.Update).Subscribe(_ => Tick());
            RenderFrame(0);
        }

        void BuildUnlockButtons()
        {
            foreach (var button in unlockButtons)
                if (button != null) { button.gameObject.SetActive(false); Destroy(button.gameObject); }
            unlockButtons.Clear();
            unlockGroups.Clear();
            if (UnlockTemplate == null) return;
            var template = UnlockTemplate.transform;
            for (var i = 0; i < State.Spools.Count; i++)
            {
                var button = Instantiate(UnlockTemplate, template.parent);
                button.name = "Unlock_" + i;
                button.transform.SetSiblingIndex(template.GetSiblingIndex() + 1 + i);
                var slot = i;
                button.onClick.AddListener(() => UnlockAt(slot));
                var group = button.GetComponent<CanvasGroup>();
                if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
                unlockButtons.Add(button);
                unlockGroups.Add(group);
            }
        }

        public void Select(int index)
        {
            if (State == null || State.Paused || State.Status != RoundStatus.Playing) return;
            if (State.Select(index) || index < 0 || index >= blockViews.Count) return;
            blockViews[index].Shake = 0.35f;
            if (State.Blocks[index].Phase == 0) feel.Blocked(index);
        }

        public Vector3 BlockAt(int index) { return index >= 0 && index < blockViews.Count ? blockViews[index].transform.position : kittenRest; }
        public Vector3 UnitAt(int index) { return index >= 0 && index < units.Count ? units[index].position : kittenRest; }
        public Vector3 KittenAt => kittenRest;
        public bool ThreadVisible(int slot) { return slot >= 0 && slot < threads.Count && threads[slot] != null && threads[slot].enabled; }
        public Vector3 ThreadEnd(int slot) { return slot >= 0 && slot < threadEnds.Count ? threadEnds[slot] : kittenRest; }

        public void Unlock() { if (State == null || State.Paused) return; State.UnlockSlot(); }
        public void UnlockAt(int slot) { if (State != null && slot == State.OpenSlots) Unlock(); }
        public void Pause() { if (State == null || State.Status != RoundStatus.Playing) return; State.SetPaused(true); feel.SetPaused(true); PausePanel.SetActive(true); }
        public void Resume() { if (State == null) return; State.SetPaused(false); feel.SetPaused(false); PausePanel.SetActive(false); lastTime = Time.timeAsDouble; }
        public void Replay() { LoadLevel(CurrentLevel); }
        public void Next()
        {
            if (State == null || State.Status != RoundStatus.Won) return;
            if (CurrentLevel >= Levels.Length - 1) Home(); else LoadLevel(CurrentLevel + 1);
        }
        public void Home() { if (leaving) return; leaving = true; clock?.Dispose(); clock = null; CancelResult(); SceneManager.LoadScene(WoolSession.LobbyScene); }

        void Tick()
        {
            var now = Time.timeAsDouble;
            var delta = Mathf.Clamp((float)(now - lastTime), 0, StepLimit);
            lastTime = now;
            if (gameCamera != null && Mathf.Abs(gameCamera.aspect - fittedAspect) > 1e-4f) FitCamera();
            if (State == null || State.Paused) return;
            var scaled = delta * Speed;
            var steps = Mathf.Max(1, Mathf.CeilToInt(scaled / StepLimit - 1e-4f));
            for (var s = 0; s < steps; s++) State.Step(scaled / steps);
            feel.Step(State, scaled);
            RenderFrame(scaled);
            if (State.Status != lastStatus)
            {
                lastStatus = State.Status;
                QueueResult(State.Status);
                if (State.Status == RoundStatus.Won)
                {
                    progress.MarkCleared(CurrentLevel);
                    Burst();
                }
            }
        }

        void QueueResult(RoundStatus status)
        {
            CancelResult();
            ResultQueuedAt = Time.timeAsDouble;
            var delay = status == RoundStatus.Won ? WinResultDelay : LoseResultDelay;
            resultDelay = Observable.Timer(TimeSpan.FromSeconds(delay), UnityTimeProvider.Update).Subscribe(_ => ShowResult(status));
        }

        void ShowResult(RoundStatus status)
        {
            CancelResult();
            ResultPanel.SetActive(true);
            WonContent.SetActive(status == RoundStatus.Won);
            LostContent.SetActive(status == RoundStatus.Lost);
            NextButton.gameObject.SetActive(status == RoundStatus.Won);
            if (ResultRays != null) ResultRays.SetActive(status == RoundStatus.Won);
        }

        public void ShowProgress(float fill)
        {
            fill = Mathf.Clamp01(fill);
            if (ProgressFill.type == Image.Type.Filled)
            {
                ProgressFill.fillAmount = fill;
                return;
            }
            var rect = ProgressFill.rectTransform;
            var track = rect.parent as RectTransform;
            var span = track != null ? track.rect.width - 2 * rect.anchoredPosition.x : 0;
            span = Mathf.Max(0, span);
            var sprite = ProgressFill.sprite;
            var minWidth = sprite != null ? (sprite.border.x + sprite.border.z) / (sprite.pixelsPerUnit * ProgressFill.pixelsPerUnitMultiplier) : 0;
            var width = fill <= 0 ? 0 : Mathf.Min(span, Mathf.Max(minWidth, span * fill));
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }

        void CancelResult()
        {
            resultDelay?.Dispose();
            resultDelay = null;
        }

        void RenderFrame(float delta)
        {
            var data = Levels[CurrentLevel].Data;
            var fill = 1f - (float)State.Remaining / State.Total;
            ProgressNumber.text = Mathf.RoundToInt(fill * 100) + "%";
            ShowProgress(fill);
            for (var i = 0; i < blockViews.Count; i++)
            {
                var block = State.Blocks[i];
                var view = blockViews[i];
                view.gameObject.SetActive(block.Phase != 2);
                if (block.Phase == 2) continue;
                var start = BlockPosition(block.Definition, data);
                if (block.Phase == 1)
                {
                    WoolRouting.Evaluate(block.Route,block.Travel*WoolRouting.Speed,out var point,out var movement);
                    view.transform.position=new Vector3(point.X,0.12f,point.Z);
                    view.transform.localScale=Vector3.one*feel.BlockScale(i);
                    var heading=Quaternion.Euler(0,Mathf.Atan2(movement.X,movement.Z)*Mathf.Rad2Deg-WoolShape.Angle(block.Definition.Direction)+WoolShape.RestYaw(block.Definition.Direction),0);
                    view.transform.rotation=delta==0?heading:Quaternion.RotateTowards(view.transform.rotation,heading,1080*delta);
                }
                else
                {
                    view.Shake = Mathf.Max(0, view.Shake - delta);
                    view.transform.position = start + Vector3.right * Mathf.Sin(view.Shake * 95) * view.Shake * 0.26f;
                }
                var flash = feel.BlockFlash(i);
                if (flash == blockFlashShown[i]) continue;
                blockFlashShown[i] = flash;
                blockTint ??= new MaterialPropertyBlock();
                blockTint.SetColor(BaseColor, Color.Lerp(Art.Yarn[block.Definition.Color].GetColor(BaseColor), WoolFeel.FlashRed, flash));
                view.Body.GetComponent<Renderer>().SetPropertyBlock(blockTint);
            }
            displayedHead=delta==0?State.Head:Mathf.Lerp(displayedHead,State.Head,1-Mathf.Exp(-12*delta));
            for (var i = 0; i < units.Count; i++)
            {
                var unit = State.Yarn[i];
                var visible = !unit.Consumed && unit.Distance >= 0 && unit.Distance < State.PathLength;
                var wasVisible=units[i].gameObject.activeSelf;
                units[i].gameObject.SetActive(visible);
                if (!visible) continue;
                Evaluate(unit.Distance+displayedHead-State.Head, out var p, out var direction);
                p.y += 0.31f;
                if (delta == 0 || !wasVisible) units[i].position = p; else units[i].position = Vector3.Lerp(units[i].position, p, 1-Mathf.Exp(-22*delta));
                var rotation=Quaternion.LookRotation(direction);
                units[i].rotation=delta==0 || !wasVisible?rotation:Quaternion.RotateTowards(units[i].rotation,rotation,360*delta);
                var scale = 1f;
                if (unit.ReservedBy >= 0) scale = Mathf.Lerp(0.25f, 1, Mathf.Clamp01(State.Spools[unit.ReservedBy].Pull / WoolGame.PullDuration));
                units[i].localScale = new Vector3(1.15f, scale*1.1f, scale);
            }
            Evaluate(displayedHead, out var headPosition, out var headDirection);
            dragon.position = headPosition + Vector3.up * (0.36f + Mathf.Sin(State.Elapsed * 5) * 0.025f);
            var horizontal=Vector3.Dot(headDirection,gameCamera.transform.right);
            var verticalDirection=Vector3.Dot(headDirection,gameCamera.transform.up);
            var headAngle=Mathf.Atan2(verticalDirection,horizontal)*Mathf.Rad2Deg+55;
            var headRotation=gameCamera.transform.rotation*Quaternion.AngleAxis(headAngle,Vector3.forward);
            dragon.rotation=delta==0?headRotation:Quaternion.RotateTowards(dragon.rotation,headRotation,240*delta);
            dragon.localScale=Vector3.one*feel.DragonScale;
            for (var i = 0; i < spoolViews.Count; i++)
            {
                var spool = State.Spools[i];
                slots[i].Label.gameObject.SetActive(false);
                var pad = slots[i].GetComponent<Renderer>();
                pad.sharedMaterial = i < State.OpenSlots ? Art.Pad : Art.PadLocked;
                slots[i].transform.localScale = Vector3.one * feel.SlotScale(i);
                var padFlash = feel.SlotFlash(i);
                if (padFlash != slotFlashShown[i])
                {
                    slotFlashShown[i] = padFlash;
                    if (padFlash <= 0) pad.SetPropertyBlock(null);
                    else
                    {
                        blockTint ??= new MaterialPropertyBlock();
                        blockTint.SetColor(BaseColor, Color.Lerp(Art.Pad.GetColor(BaseColor), WoolFeel.FlashRed, padFlash));
                        pad.SetPropertyBlock(blockTint);
                    }
                }
                var visible = spool.Block >= 0 && spool.Arrival <= 0;
                spoolViews[i].gameObject.SetActive(visible);
                var show = visible && spool.Remaining > 0 && spool.Target >= 0;
                threads[i].enabled = show;
                if (show)
                {
                    var goal = units[spool.Target].position + Vector3.up * 0.22f;
                    if (threadTargets[i] < 0 || delta == 0) threadEnds[i] = goal;
                    else threadEnds[i] = Vector3.Lerp(threadEnds[i], goal, 1 - Mathf.Exp(-18 * delta));
                    threadTargets[i] = spool.Target;
                }
                else threadTargets[i] = -1;
                if (!visible) continue;
                Dye(spoolViews[i], spool.Color);
                spoolLabels[i].text = spool.Remaining.ToString();
                if (spoolShadows[i] != null) spoolShadows[i].text = spoolLabels[i].text;
                var finish = spool.Remaining > 0 ? 1f : Mathf.Clamp01(spool.Finish / WoolGame.FinishDuration);
                spoolViews[i].localScale = Vector3.one * (finish * feel.SpoolScale(i));
                var wrap = spoolViews[i].Find("YarnWrap");
                if (wrap != null)
                {
                    wrap.localScale = new Vector3(Mathf.Lerp(0.12f, 1, 1f - (float)spool.Remaining / spool.Capacity), 1, 1);
                    wrap.localRotation = Quaternion.Euler(State.Elapsed * (spool.Target >= 0 ? 600 : 0), 0, 0);
                }
                if (!show) continue;
                threads[i].sharedMaterial = Art.Thread[spool.Color];
                var a = threadEnds[i];
                var b = spoolViews[i].position + Vector3.up * 0.15f;
                var span = Vector3.Distance(a, b);
                for (var j = 0; j < 20; j++)
                {
                    var t = j / 19f;
                    var sag = Mathf.Sin(t * Mathf.PI);
                    var point = Vector3.Lerp(a, b, t) + Vector3.up * (0.35f * sag) + Vector3.back * (0.12f * span * sag) + Vector3.right * (Mathf.Sin(t * 22 + State.Elapsed * 35) * 0.018f * sag);
                    threads[i].SetPosition(19 - j, point);
                }
            }
            for (var i = 0; i < unlockButtons.Count; i++)
            {
                var button = unlockButtons[i];
                var locked = i >= State.OpenSlots;
                if (button.gameObject.activeSelf != locked) button.gameObject.SetActive(locked);
                var next = i == State.OpenSlots;
                if (button.interactable != next) button.interactable = next;
                var alpha = next ? 1f : UnlockDisabledAlpha;
                if (unlockGroups[i].alpha != alpha) unlockGroups[i].alpha = alpha;
            }
            var danger = Mathf.InverseLerp(0.8f, 1, State.Head / State.PathLength);
            kitten.position = kittenRest + Vector3.up * (Mathf.Sin(State.Elapsed * 3) * 0.025f);
            kitten.rotation=gameCamera.transform.rotation;
            if (State.Status == RoundStatus.Won)
            {
                celebration += delta;
                kitten.position += Vector3.up * Mathf.Abs(Mathf.Sin(celebration * 6)) * 0.38f;
                kitten.rotation=gameCamera.transform.rotation*Quaternion.AngleAxis(Mathf.Sin(celebration*5)*8,Vector3.forward);
            }
            else kitten.position += Vector3.right * Mathf.Sin(State.Elapsed * 45) * danger * 0.05f;
            if (kittenBody != null) kittenBody.localScale = new Vector3(1, 1 + Mathf.Sin(State.Elapsed * 3) * 0.025f, 1);
            for (var i = 0; i < confetti.Count; i++)
            {
                confettiVelocity[i] += Vector3.down * delta * 2.8f;
                confetti[i].position += confettiVelocity[i] * delta;
                confetti[i].Rotate(new Vector3(110, 170, 80) * delta);
                if (celebration > 4) confetti[i].gameObject.SetActive(false);
            }
        }

        void Dye(Transform root, int color)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.gameObject.name.StartsWith("Yarn", StringComparison.Ordinal)) renderer.sharedMaterial = Art.Yarn[color];
        }

        void Burst()
        {
            for (var i = 0; i < 48; i++)
            {
                var item = Instantiate(Art.Confetti, World).transform;
                item.position = kittenRest + Vector3.up;
                item.GetComponentInChildren<Renderer>().sharedMaterial = Art.Yarn[i % 6];
                item.localScale = Vector3.one * UnityEngine.Random.Range(0.08f, 0.16f);
                confetti.Add(item);
                confettiVelocity.Add(new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(3f, 6f), UnityEngine.Random.Range(-2f, 2f)));
            }
        }

        public void Evaluate(float distance, out Vector3 position, out Vector3 direction)
        {
            var path = Levels[CurrentLevel].Path;
            var s = Mathf.Clamp(distance, 0, pathDistances[pathDistances.Count - 1]);
            var i = 1;
            while (i < pathDistances.Count - 1 && pathDistances[i] < s) i++;
            var t = Mathf.InverseLerp(pathDistances[i - 1], pathDistances[i], s);
            position = Vector3.Lerp(path[i - 1], path[i], t);
            var incoming=(path[i]-path[Mathf.Max(0,i-2)]).normalized;
            var outgoing=(path[Mathf.Min(path.Length-1,i+1)]-path[i-1]).normalized;
            direction=Vector3.Slerp(incoming,outgoing,t).normalized;
        }

        void BuildRoad(IReadOnlyList<Vector3> path)
        {
            RoadMesh(path, 0.66f, 0.025f, Art.Rim);
            RoadMesh(path, 0.55f, 0.038f, Art.Road);
        }

        void RoadMesh(IReadOnlyList<Vector3> path, float halfWidth, float height, Material material)
        {
            var vertices = new Vector3[path.Count * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(path.Count - 1) * 6];
            var distance = 0f;
            for (var i = 0; i < path.Count; i++)
            {
                if (i > 0) distance += Vector3.Distance(path[i - 1], path[i]);
                var tangent = path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)];
                var right = Vector3.Cross(Vector3.up, tangent).normalized * halfWidth;
                vertices[i * 2] = path[i] - right + Vector3.up * height;
                vertices[i * 2 + 1] = path[i] + right + Vector3.up * height;
                uv[i * 2] = new Vector2(0, distance); uv[i * 2 + 1] = new Vector2(1, distance);
                if (i == path.Count - 1) continue;
                var k = i * 6; var v = i * 2;
                triangles[k] = v; triangles[k + 1] = v + 2; triangles[k + 2] = v + 1;
                triangles[k + 3] = v + 1; triangles[k + 4] = v + 2; triangles[k + 5] = v + 3;
            }
            var mesh = new Mesh { name = "LevelRoad" };
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateNormals();
            roadMeshes.Add(mesh);
            var go = new GameObject("DragonPath"); go.transform.SetParent(World, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        public static Vector3 SlotPosition(int index)
        {
            var point=WoolRouting.SlotCenter(index);
            return new Vector3(point.X,0.08f,point.Z);
        }
        public static Vector3 BlockPosition(BlockDefinition b, LevelDefinition level)
        {
            var point=WoolRouting.BlockCenter(b,level);
            return new Vector3(point.X,0.12f,point.Z);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace JamJam
{
    public sealed class WoolFeel : MonoBehaviour
    {
        public const float LaunchPop = 0.08f;
        public const float LaunchPopTime = 0.18f;
        public const float BlockedFlash = 0.45f;
        public const float BlockedFlashTime = 0.3f;
        public const float ArrivePopTime = 0.28f;
        public const float CompletePunch = 0.25f;
        public const float CompletePunchTime = 0.2f;
        public const float SlotPop = 0.2f;
        public const float SlotPopTime = 0.25f;
        public const float TrayFlash = 0.35f;
        public const float TrayFlashTime = 0.5f;
        public const float EatPunch = 0.08f;
        public const float EatPunchTime = 0.15f;
        public const float LostPunch = 0.3f;
        public const float LostPunchTime = 0.4f;
        public const float WinZoom = 0.06f;
        public const float WinZoomTime = 0.6f;
        public const float BlockedTrauma = 0.18f;
        public const float ArriveTrauma = 0.05f;
        public const float CompleteTrauma = 0.08f;
        public const float DangerTrauma = 0.25f;
        public const float LostTrauma = 0.6f;
        public const float PuffSpeed = 4f;
        public const float PuffSize = 0.6f;
        public const float PuffLife = 0.5f;
        public const float SparkleSpeed = 4.5f;
        public const float SparkleSize = 0.5f;
        public const float SparkleLife = 0.7f;
        public const float WinSpeed = 9f;
        public const float WinSize = 0.7f;
        public const float WinLife = 1.3f;
        public const float LostSpeed = 4f;
        public const float LostSize = 0.8f;
        public const float LostLife = 0.7f;
        public const int PuffLaunchBurst = 10;
        public const int PuffArriveBurst = 14;
        public const int PuffEatBurst = 5;
        public const int SparkleCompleteBurst = 12;
        public const int SparkleUnlockBurst = 16;
        public const int WinBurst = 60;
        public const int LostBurst = 28;
        public const float EmitLift = 0.4f;
        public static readonly Color FlashRed = new Color32(0xFF, 0x5A, 0x5A, 0xFF);
        static readonly Color LostOrange = new Color32(0xFF, 0x6A, 0x3D, 0xFF);
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        const float Idle = 1000f;
        readonly WoolFeelTracker tracker = new WoolFeelTracker();
        readonly WoolShake shake = new WoolShake();
        readonly List<WoolFeelEvent> events = new List<WoolFeelEvent>();
        WoolScene owner;
        WoolGame game;
        Camera view;
        Vector3 cameraPosition;
        Quaternion cameraRotation;
        float cameraSize;
        WoolSettings settings;
        IWoolHaptics haptics;
        bool vibration;
        ParticleSystem puff;
        ParticleSystem sparkle;
        float[] blockPop = Array.Empty<float>();
        float[] blockFlash = Array.Empty<float>();
        float[] spoolArrive = Array.Empty<float>();
        float[] spoolPunch = Array.Empty<float>();
        float[] slotPop = Array.Empty<float>();
        float[] slotFlash = Array.Empty<float>();
        float dragonAge = Idle;
        float dragonAmount;
        float dragonTime = 1;
        float zoomAge = Idle;
        int seed;

        public float Trauma => shake.Trauma;
        public int PuffCount => puff != null ? puff.particleCount : 0;
        public int SparkleCount => sparkle != null ? sparkle.particleCount : 0;
        public float DragonScale => 1 + dragonAmount * WoolEase.Punch(dragonAge / dragonTime);
        public float CameraZoom => WinZoom * WoolEase.Punch(zoomAge / WinZoomTime);

        public float BlockScale(int index) { return 1 + LaunchPop * WoolEase.Punch(Age(blockPop, index) / LaunchPopTime); }
        public float BlockFlash(int index) { return BlockedFlash * WoolEase.Arc(Age(blockFlash, index) / BlockedFlashTime); }
        public float SlotScale(int index) { return 1 + SlotPop * WoolEase.Punch(Age(slotPop, index) / SlotPopTime); }
        public float SlotFlash(int index) { return TrayFlash * WoolEase.Arc(Age(slotFlash, index) / TrayFlashTime); }

        public float SpoolScale(int index)
        {
            var arrive = Age(spoolArrive, index);
            if (arrive < ArrivePopTime) return WoolEase.OutBack(arrive / ArrivePopTime);
            return 1 + CompletePunch * WoolEase.Punch(Age(spoolPunch, index) / CompletePunchTime);
        }

        public void Begin(WoolScene scene, Camera camera)
        {
            owner = scene;
            if (settings == null) settings = new WoolSettings(new WoolPlayerPrefs());
            if (camera != view)
            {
                Restore();
                view = camera;
                if (view != null)
                {
                    cameraPosition = view.transform.position;
                    cameraRotation = view.transform.rotation;
                    cameraSize = view.orthographicSize;
                }
            }
            if (scene == null || scene.Art == null) return;
            if (puff == null && scene.Art.Puff != null) Spawn(scene.Art.Puff, out puff);
            if (sparkle == null && scene.Art.Sparkle != null) Spawn(scene.Art.Sparkle, out sparkle);
        }

        public void Rebase(Vector3 position, Quaternion rotation, float size)
        {
            cameraPosition = position;
            cameraRotation = rotation;
            cameraSize = size;
            if (view == null) return;
            if (Trauma <= 0 && CameraZoom <= 0) Restore();
        }

        public void Restart(WoolGame state)
        {
            game = state;
            tracker.Reset(state);
            shake.Reset();
            events.Clear();
            var blocks = state != null ? state.Blocks.Count : 0;
            var spools = state != null ? state.Spools.Count : 0;
            Rest(ref blockPop, blocks);
            Rest(ref blockFlash, blocks);
            Rest(ref spoolArrive, spools);
            Rest(ref spoolPunch, spools);
            Rest(ref slotPop, spools);
            Rest(ref slotFlash, spools);
            dragonAge = Idle;
            zoomAge = Idle;
            SetPaused(false);
            if (puff != null) puff.Clear();
            if (sparkle != null) sparkle.Clear();
            Restore();
            var enabled = settings != null && settings.Vibration;
            if (haptics != null && enabled == vibration) return;
            if (haptics != null) haptics.Dispose();
            WoolHaptics.Create(enabled, out haptics);
            vibration = enabled;
        }

        public void Blocked(int index)
        {
            if (game == null || index < 0 || index >= game.Blocks.Count) return;
            events.Clear();
            tracker.Blocked(index, game.Blocks[index].Definition.Color, events);
            Play();
        }

        public void Step(WoolGame state, float delta)
        {
            if (state != game) Restart(state);
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            Advance(delta);
            events.Clear();
            tracker.Collect(state, events);
            Play();
            shake.Step(delta, out var x, out var y, out var roll);
            Apply(x, y, roll);
        }

        public void SetPaused(bool paused)
        {
            Freeze(puff, paused);
            Freeze(sparkle, paused);
        }

        static void Freeze(ParticleSystem system, bool paused)
        {
            if (system == null) return;
            if (paused) system.Pause(true); else system.Play(true);
        }

        void OnDisable() { Restore(); }

        void OnDestroy()
        {
            if (haptics != null) haptics.Dispose();
            haptics = null;
        }

        void Spawn(GameObject prefab, out ParticleSystem system)
        {
            var go = Instantiate(prefab, transform);
            go.name = prefab.name;
            system = go.GetComponent<ParticleSystem>();
        }

        static float Age(float[] ages, int index) { return index >= 0 && index < ages.Length ? ages[index] : Idle; }

        static void Rest(ref float[] ages, int count)
        {
            if (ages.Length != count) ages = new float[count];
            for (var i = 0; i < count; i++) ages[i] = Idle;
        }

        static void Trigger(float[] ages, int index) { if (index >= 0 && index < ages.Length) ages[index] = 0; }

        static void Grow(float[] ages, float delta)
        {
            for (var i = 0; i < ages.Length; i++) ages[i] = Mathf.Min(Idle, ages[i] + delta);
        }

        void Advance(float delta)
        {
            Grow(blockPop, delta);
            Grow(blockFlash, delta);
            Grow(spoolArrive, delta);
            Grow(spoolPunch, delta);
            Grow(slotPop, delta);
            Grow(slotFlash, delta);
            dragonAge = Mathf.Min(Idle, dragonAge + delta);
            zoomAge = Mathf.Min(Idle, zoomAge + delta);
        }

        void Play()
        {
            var strongest = -1;
            var strength = 0;
            for (var i = 0; i < events.Count; i++)
            {
                React(events[i]);
                var value = WoolHapticPattern.Strength(events[i].Kind);
                if (value <= strength) continue;
                strength = value;
                strongest = i;
            }
            if (strongest < 0 || haptics == null) return;
            WoolHapticPattern.Get(events[strongest].Kind, out var timings, out var amplitudes);
            haptics.Play(timings, amplitudes);
        }

        void React(WoolFeelEvent e)
        {
            if (owner == null) return;
            switch (e.Kind)
            {
                case WoolFeelKind.BlockLaunched:
                    Trigger(blockPop, e.Index);
                    Emit(puff, owner.BlockAt(e.Index) + Vector3.up * 0.3f, Color.white, PuffLaunchBurst, PuffSpeed, PuffSize, PuffLife);
                    break;
                case WoolFeelKind.BlockBlocked:
                    Trigger(blockFlash, e.Index);
                    shake.Add(BlockedTrauma);
                    break;
                case WoolFeelKind.SpoolArrived:
                    Trigger(spoolArrive, e.Index);
                    Emit(puff, WoolScene.SlotPosition(e.Index) + Vector3.up * 0.45f, Yarn(e.Color), PuffArriveBurst, PuffSpeed, PuffSize, PuffLife);
                    shake.Add(ArriveTrauma);
                    break;
                case WoolFeelKind.YarnConsumed:
                    if (dragonAge >= dragonTime || dragonAmount <= EatPunch) Dragon(EatPunch, EatPunchTime);
                    Emit(puff, owner.UnitAt(e.Index) + Vector3.up * 0.3f, Yarn(e.Color), PuffEatBurst, PuffSpeed, PuffSize, PuffLife);
                    break;
                case WoolFeelKind.SpoolCompleted:
                    Trigger(spoolPunch, e.Index);
                    var top = WoolScene.SlotPosition(e.Index) + Vector3.up * 0.6f;
                    Emit(sparkle, top, Yarn(e.Color), SparkleCompleteBurst, SparkleSpeed, SparkleSize, SparkleLife);
                    Emit(sparkle, top, Color.white, SparkleCompleteBurst, SparkleSpeed, SparkleSize, SparkleLife);
                    shake.Add(CompleteTrauma);
                    break;
                case WoolFeelKind.SlotUnlocked:
                    Trigger(slotPop, e.Index);
                    Emit(sparkle, WoolScene.SlotPosition(e.Index) + Vector3.up * 0.3f, Color.white, SparkleUnlockBurst, SparkleSpeed, SparkleSize, SparkleLife);
                    break;
                case WoolFeelKind.TrayFull:
                    for (var i = 0; game != null && i < game.OpenSlots; i++) Trigger(slotFlash, i);
                    break;
                case WoolFeelKind.Danger:
                    shake.Add(DangerTrauma);
                    break;
                case WoolFeelKind.Won:
                    zoomAge = 0;
                    for (var i = 0; i < WinBurst; i++) Emit(sparkle, owner.KittenAt + Vector3.up, Yarn(i % 6), 1, WinSpeed, WinSize, WinLife);
                    break;
                case WoolFeelKind.Lost:
                    Dragon(LostPunch, LostPunchTime);
                    Emit(puff, owner.KittenAt + Vector3.up * 0.6f, LostOrange, LostBurst, LostSpeed, LostSize, LostLife);
                    shake.Add(LostTrauma);
                    break;
            }
        }

        void Dragon(float amount, float time)
        {
            dragonAmount = amount;
            dragonTime = time;
            dragonAge = 0;
        }

        Color Yarn(int color)
        {
            var yarn = owner != null && owner.Art != null ? owner.Art.Yarn : null;
            return yarn != null && color >= 0 && color < yarn.Length && yarn[color] != null ? yarn[color].GetColor(BaseColor) : Color.white;
        }

        void Emit(ParticleSystem system, Vector3 position, Color color, int count, float speed, float size, float life)
        {
            if (system == null) return;
            for (var i = 0; i < count; i++)
            {
                seed++;
                var angle = seed * 2.39996f;
                var rise = 0.45f + 0.5f * Mathf.Repeat(seed * 0.618034f, 1);
                var direction = new Vector3(Mathf.Cos(angle), rise, Mathf.Sin(angle)).normalized;
                var emit = new ParticleSystem.EmitParams
                {
                    position = position + Vector3.up * EmitLift,
                    velocity = direction * speed * (0.5f + 0.5f * Mathf.Repeat(seed * 0.7548777f, 1)),
                    startColor = color,
                    startSize = size * (0.75f + 0.5f * Mathf.Repeat(seed * 0.381966f, 1)),
                    startLifetime = life,
                    rotation = Mathf.Repeat(angle * Mathf.Rad2Deg, 360),
                    applyShapeToPosition = false
                };
                system.Emit(emit, 1);
            }
        }

        void Apply(float x, float y, float roll)
        {
            if (view == null) return;
            view.transform.SetPositionAndRotation(cameraPosition + cameraRotation * new Vector3(x, y, 0), cameraRotation * Quaternion.AngleAxis(roll, Vector3.forward));
            view.orthographicSize = cameraSize * (1 - CameraZoom);
        }

        void Restore()
        {
            if (view == null) return;
            view.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            view.orthographicSize = cameraSize;
        }
    }
}

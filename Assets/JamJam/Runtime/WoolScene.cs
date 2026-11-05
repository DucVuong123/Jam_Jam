using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolScene : MonoBehaviour
    {
        public WoolArt Art;
        public WoolLevel[] Levels;
        public Transform World;
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
        public WoolGame State;
        public int CurrentLevel { get; private set; }
        readonly List<WoolBlockView> blockViews = new List<WoolBlockView>();
        readonly List<Transform> units = new List<Transform>();
        readonly List<Transform> spoolViews = new List<Transform>();
        readonly List<TextMesh> spoolLabels = new List<TextMesh>();
        readonly List<LineRenderer> threads = new List<LineRenderer>();
        readonly List<WoolSlotView> slots = new List<WoolSlotView>();
        readonly List<Transform> confetti = new List<Transform>();
        readonly List<Vector3> confettiVelocity = new List<Vector3>();
        readonly List<float> pathDistances = new List<float>();
        readonly List<Mesh> roadMeshes = new List<Mesh>();
        IDisposable clock;
        Transform dragon;
        Transform kitten;
        Transform kittenBody;
        Camera gameCamera;
        Vector3 kittenRest;
        RoundStatus lastStatus;
        double lastTime;
        float celebration;
        float displayedHead;
        const float Cell = 1f;

        void Start()
        {
            PauseButton.onClick.AddListener(Pause);
            ResumeButton.onClick.AddListener(Resume);
            RetryButton.onClick.AddListener(Replay);
            ResultRetryButton.onClick.AddListener(Replay);
            NextButton.onClick.AddListener(Next);
            LoadLevel(Mathf.Clamp(PlayerPrefs.GetInt("JamJam.Level", 0), 0, Levels.Length - 1));
        }

        void OnEnable()
        {
            lastTime = Time.timeAsDouble;
            clock?.Dispose();
            clock = Observable.Interval(TimeSpan.FromSeconds(1d / 60d), UnityTimeProvider.Update).Subscribe(_ => Tick());
        }

        void OnDisable() { clock?.Dispose(); clock = null; }
        void OnDestroy()
        {
            foreach(var mesh in roadMeshes) if(mesh!=null) Destroy(mesh);
            roadMeshes.Clear();
        }

        public void LoadLevel(int index)
        {
            clock?.Dispose();
            clock = null;
            foreach (Transform child in World) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach(var mesh in roadMeshes) Destroy(mesh);
            roadMeshes.Clear();
            blockViews.Clear(); units.Clear(); spoolViews.Clear(); spoolLabels.Clear(); threads.Clear(); slots.Clear(); confetti.Clear(); confettiVelocity.Clear();
            CurrentLevel = Mathf.Clamp(index, 0, Levels.Length - 1);
            var level = Levels[CurrentLevel];
            gameCamera=Camera.main;
            State = new WoolGame(level.Data);
            displayedHead = State.Head;
            pathDistances.Clear(); pathDistances.Add(0);
            for (var i = 1; i < level.Path.Length; i++) pathDistances.Add(pathDistances[i - 1] + Vector3.Distance(level.Path[i - 1], level.Path[i]));
            ResultPanel.SetActive(false); PausePanel.SetActive(false);
            lastStatus = RoundStatus.Playing;
            celebration = 0;
            LevelNumber.text = (CurrentLevel + 1).ToString("00");
            BuildRoad(level.Path);
            for (var i = 0; i < State.Blocks.Count; i++)
            {
                var data = State.Blocks[i].Definition;
                var go = Instantiate(Art.Block, World);
                go.name = "Block_" + i;
                var view = go.GetComponent<WoolBlockView>();
                view.Owner = this; view.Index = i;
                var body=view.Body.GetComponent<SpriteRenderer>();
                body.size=new Vector2(data.Width*Cell+0.02f,data.Height*Cell+0.02f);
                var color=Art.Yarn[data.Color].GetColor("_BaseColor");
                Color.RGBToHSV(color,out var hue,out var saturation,out var value);
                body.color=Color.HSVToRGB(hue,Mathf.Min(1,saturation*1.25f),Mathf.Min(1,value*1.12f));
                var vertical=data.Direction==ExitDirection.Up || data.Direction==ExitDirection.Down;
                view.Arrow.localRotation=Quaternion.Euler(90,0,0)*Quaternion.Euler(0,0,-(int)data.Direction*90);
                view.Arrow.localScale=new Vector3(0.56f,(vertical?data.Height:data.Width)*Cell*0.78f,1);
                var collider = go.GetComponent<BoxCollider>();
                collider.size = new Vector3(data.Width * Cell - 0.035f, 0.55f, data.Height * Cell - 0.035f);
                collider.center = new Vector3(0, 0.28f, 0);
                go.transform.position = BlockPosition(data, level.Data);
                blockViews.Add(view);
            }
            for (var i = 0; i < State.Spools.Count; i++)
            {
                var slot = Instantiate(Art.Slot, World).GetComponent<WoolSlotView>();
                slot.transform.position = SlotPosition(i); slot.Owner = this; slot.Index = i;
                slots.Add(slot);
                var spool = Instantiate(Art.Spool, World).transform;
                spool.position = SlotPosition(i) + Vector3.up * 0.33f;
                spoolViews.Add(spool);
                spoolLabels.Add(spool.GetComponentInChildren<TextMesh>());
                var lineObject = new GameObject("Thread_" + i);
                lineObject.transform.SetParent(World, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.positionCount = 20; line.startWidth = 0.045f; line.endWidth = 0.025f;
                line.numCapVertices = 3; line.numCornerVertices = 3; line.generateLightingData = true;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                threads.Add(line);
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

        public void Select(int index)
        {
            if (State == null || State.Paused || State.Status != RoundStatus.Playing) return;
            if (!State.Select(index) && index >= 0 && index < blockViews.Count) blockViews[index].Shake = 0.35f;
        }

        public void Unlock() { if (State != null) State.UnlockSlot(); }
        public void Pause() { if (State == null || State.Status != RoundStatus.Playing) return; State.SetPaused(true); PausePanel.SetActive(true); }
        public void Resume() { if (State == null) return; State.SetPaused(false); PausePanel.SetActive(false); lastTime = Time.timeAsDouble; }
        public void Replay() { LoadLevel(CurrentLevel); }
        public void Next() { if (State != null && State.Status == RoundStatus.Won) LoadLevel((CurrentLevel + 1) % Levels.Length); }

        void Tick()
        {
            var now = Time.timeAsDouble;
            var delta = Mathf.Clamp((float)(now - lastTime), 0, 0.1f);
            lastTime = now;
            if (State == null || State.Paused) return;
            State.Step(delta);
            RenderFrame(delta);
            if (State.Status != lastStatus)
            {
                lastStatus = State.Status;
                ResultPanel.SetActive(true);
                WonContent.SetActive(State.Status == RoundStatus.Won);
                LostContent.SetActive(State.Status == RoundStatus.Lost);
                NextButton.gameObject.SetActive(State.Status == RoundStatus.Won);
                if (State.Status == RoundStatus.Won)
                {
                    PlayerPrefs.SetInt("JamJam.Level", Mathf.Min(CurrentLevel + 1, Levels.Length - 1));
                    PlayerPrefs.Save();
                    Burst();
                }
            }
        }

        void RenderFrame(float delta)
        {
            var data = Levels[CurrentLevel].Data;
            var progress = 1f - (float)State.Remaining / State.Total;
            ProgressNumber.text = Mathf.RoundToInt(progress * 100) + "%";
            ProgressFill.fillAmount = progress;
            for (var i = 0; i < blockViews.Count; i++)
            {
                var block = State.Blocks[i];
                var view = blockViews[i];
                view.gameObject.SetActive(block.Phase != 2);
                if (block.Phase == 2) continue;
                var start = BlockPosition(block.Definition, data);
                if (block.Phase == 1)
                {
                    view.Body.GetComponent<SpriteRenderer>().sortingOrder=10;
                    view.Arrow.GetComponent<SpriteRenderer>().sortingOrder=11;
                    var distance=block.Travel*WoolRouting.Speed;
                    WoolRouting.Evaluate(block.Route,distance,out var point,out var movement);
                    view.transform.position=new Vector3(point.X,0.12f,point.Z);
                    view.transform.localScale=Vector3.one*WoolRouting.ScaleAt(distance);
                    if(block.Travel>0)
                    {
                        var angle=Mathf.Atan2(movement.X,movement.Z)*Mathf.Rad2Deg;
                        view.Arrow.localRotation=Quaternion.Euler(90,0,0)*Quaternion.Euler(0,0,-angle);
                        var length=Mathf.Abs(movement.Z)>0.5f?block.Definition.Height:block.Definition.Width;
                        view.Arrow.localScale=new Vector3(0.56f,length*Cell*0.78f,1);
                    }
                }
                else
                {
                    view.Shake = Mathf.Max(0, view.Shake - delta);
                    view.transform.position = start + Vector3.right * Mathf.Sin(view.Shake * 95) * view.Shake * 0.26f;
                }
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
            for (var i = 0; i < spoolViews.Count; i++)
            {
                var spool = State.Spools[i];
                slots[i].Label.gameObject.SetActive(i >= State.OpenSlots);
                slots[i].GetComponent<Renderer>().sharedMaterial = i < State.OpenSlots ? Art.Rim : Art.Road;
                var visible = spool.Block >= 0 && spool.Arrival <= 0;
                spoolViews[i].gameObject.SetActive(visible);
                threads[i].enabled = visible && spool.Target >= 0;
                if (!visible) continue;
                Dye(spoolViews[i], spool.Color);
                spoolLabels[i].text = spool.Remaining.ToString();
                var finish = spool.Remaining > 0 ? 1f : Mathf.Clamp01(spool.Finish / WoolGame.FinishDuration);
                spoolViews[i].localScale = Vector3.one * finish;
                var wrap = spoolViews[i].Find("YarnWrap");
                if (wrap != null)
                {
                    wrap.localScale = new Vector3(Mathf.Lerp(0.12f, 1, 1f - (float)spool.Remaining / spool.Capacity), 1, 1);
                    wrap.localRotation = Quaternion.Euler(State.Elapsed * (spool.Target >= 0 ? 600 : 0), 0, 0);
                }
                if (spool.Target < 0) continue;
                threads[i].sharedMaterial = Art.Thread[spool.Color];
                var a = units[spool.Target].position + Vector3.up * 0.22f;
                var b = spoolViews[i].position + Vector3.up * 0.15f;
                for (var j = 0; j < 20; j++)
                {
                    var t = j / 19f;
                    var point = Vector3.Lerp(a, b, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.6f) + Vector3.right * (Mathf.Sin(t * 22 + State.Elapsed * 35) * 0.018f * Mathf.Sin(t * Mathf.PI));
                    threads[i].SetPosition(j, point);
                }
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
            for (var i = 0; i < path.Count; i++)
            {
                var tangent = path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)];
                var right = Vector3.Cross(Vector3.up, tangent).normalized * halfWidth;
                vertices[i * 2] = path[i] - right + Vector3.up * height;
                vertices[i * 2 + 1] = path[i] + right + Vector3.up * height;
                uv[i * 2] = new Vector2(0, i * 0.15f); uv[i * 2 + 1] = new Vector2(1, i * 0.15f);
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

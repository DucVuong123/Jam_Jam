using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using R3;
using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    [InitializeOnLoad]
    public static class WoolEditorChecks
    {
        static readonly string Command = Path.GetFullPath("Temp/WoolJam.command");
        static readonly string Report = Path.GetFullPath("Logs/WoolJam.verify.txt");
        static FileSystemWatcher watcher;
        static volatile bool pending;
        static IDisposable solver;
        static IDisposable capture;
        static int completed;
        static IDisposable motionProbe;
        static IDisposable motionCapture;
        static Quaternion previousHeadRotation;
        static double previousProbeTime;
        static float maximumTurnSpeed;
        static int motionFrames;
        static int arrowExitSamples;
        static int arrowExitDirections;
        static bool motionFailed;

        static WoolEditorChecks()
        {
            Directory.CreateDirectory("Temp");
            Directory.CreateDirectory("Logs");
            if(!Application.isBatchMode) return;
            watcher=new FileSystemWatcher(Path.GetDirectoryName(Command),Path.GetFileName(Command));
            watcher.Changed+=Changed; watcher.Created+=Changed; watcher.EnableRaisingEvents=true;
            pending=File.Exists(Command);
            EditorApplication.update+=Dispatch;
            EditorApplication.playModeStateChanged+=PlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
            Application.logMessageReceived+=Log;
        }

        static void Changed(object sender,FileSystemEventArgs args) { pending=true; }
        static void Cleanup() { solver?.Dispose(); capture?.Dispose(); motionProbe?.Dispose(); motionCapture?.Dispose(); watcher?.Dispose(); }
        static void Log(string message,string stack,LogType type)
        {
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) File.AppendAllText(Report,"ERROR: "+message+"\n"+stack+"\n");
        }

        static void PlayChanged(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("WoolJam.Batch",false) && UnityEngine.Object.FindFirstObjectByType<WoolScene>()!=null)
            {
                capture?.Dispose();
                capture=Observable.Timer(TimeSpan.FromSeconds(2),UnityTimeProvider.Update).Subscribe(_=>
                {
                    Capture("initial");
                    if(Application.isBatchMode) BeginRuntimeChecks();
                });
            }
            if(state==PlayModeStateChange.ExitingPlayMode) { solver?.Dispose(); solver=null; capture?.Dispose(); capture=null; motionProbe?.Dispose(); motionProbe=null; motionCapture?.Dispose(); motionCapture=null; }
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("WoolJam.Batch",false))
            {
                SessionState.SetBool("WoolJam.Batch",false);
                if(SessionState.GetBool("WoolJam.HadProgress",false)) PlayerPrefs.SetInt("JamJam.Level",SessionState.GetInt("WoolJam.OldProgress",0));
                else PlayerPrefs.DeleteKey("JamJam.Level");
                PlayerPrefs.Save();
                EditorApplication.Exit(0);
            }
        }

        static void Dispatch()
        {
            if(!pending || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            pending=false;
            try
            {
                if(!File.Exists(Command)) return;
                var command=File.ReadAllText(Command).Trim(); File.Delete(Command);
                if(command=="build") { WoolSceneBuilder.Build(); ValidateLevels(); File.AppendAllText(Report,"BUILD PASS\n"); }
                else if(command=="validate") ValidateLevels();
                else if(command=="play") { SetGameView(); EditorApplication.isPlaying=true; }
                else if(command=="capture") Capture("runtime");
                else if(command=="solve") StartSolver();
                else if(command=="lose")
                {
                    var scene=UnityEngine.Object.FindFirstObjectByType<WoolScene>(); scene.LoadLevel(0);
                    for(var i=0;i<30000 && scene.State.Status==RoundStatus.Playing;i++) scene.State.Step(0.02f);
                    if(scene.State.Status!=RoundStatus.Lost) throw new InvalidOperationException("Loss did not trigger");
                    File.AppendAllText(Report,"LOSS PASS\n");
                    capture?.Dispose(); capture=Observable.Timer(TimeSpan.FromSeconds(0.5),UnityTimeProvider.Update).Subscribe(_=>Capture("loss"));
                }
                else if(command=="retry")
                {
                    var scene=UnityEngine.Object.FindFirstObjectByType<WoolScene>(); scene.ResultRetryButton.onClick.Invoke();
                    if(scene.State.Status!=RoundStatus.Playing || scene.State.Remaining!=scene.State.Total) throw new InvalidOperationException("Retry failed");
                    scene.PauseButton.onClick.Invoke(); if(!scene.State.Paused || !scene.PausePanel.activeSelf) throw new InvalidOperationException("Pause failed");
                    scene.ResumeButton.onClick.Invoke(); if(scene.State.Paused || scene.PausePanel.activeSelf) throw new InvalidOperationException("Resume failed");
                    File.AppendAllText(Report,"RETRY PAUSE RESUME PASS\n");
                }
                else if(command=="stop") EditorApplication.isPlaying=false;
                else throw new InvalidOperationException("Unknown WoolJam command");
            }
            catch(Exception ex) { File.AppendAllText(Report,"COMMAND FAILED: "+ex+"\n"); Debug.LogException(ex); }
        }

        public static void RunBatch()
        {
            try
            {
                File.WriteAllText(Report,"UNITY VERIFICATION STARTED\n");
                WoolSceneBuilder.Build();
                ValidateLevels();
                var scaleMesh=AssetDatabase.LoadAssetAtPath<Mesh>(WoolAssetsBuilder.Root+"/Meshes/KnittedScale.asset");
                var vertices=scaleMesh.vertices; var normals=scaleMesh.normals;
                var outer=0; for(var i=1;i<vertices.Length;i++) if(vertices[i].x>vertices[outer].x) outer=i;
                if(normals[outer].x<=0) throw new InvalidOperationException("Scale mesh has inward facing triangles");
                File.AppendAllText(Report,"MESH FACING PASS\n");
                var art=AssetDatabase.LoadAssetAtPath<WoolArt>(WoolAssetsBuilder.Root+"/WoolArt.asset");
                if(art.Kitten.GetComponentInChildren<SpriteRenderer>()==null || art.Dragon.GetComponentInChildren<SpriteRenderer>()==null || art.Block.GetComponent<WoolBlockView>().Body.GetComponent<SpriteRenderer>()==null)
                    throw new InvalidOperationException("Character or block sprite is missing");
                if(art.Kitten.GetComponentsInChildren<MeshRenderer>().Length!=0 || art.Dragon.GetComponentsInChildren<MeshRenderer>().Length!=0)
                    throw new InvalidOperationException("Legacy character model is still present");
                var spriteShader=art.Dragon.GetComponentInChildren<SpriteRenderer>().sharedMaterial.shader;
                if(ShaderUtil.ShaderHasError(spriteShader)) throw new InvalidOperationException("Sprite shader has compilation errors");
                File.AppendAllText(Report,"SPRITE PREFABS AND SHADER PASS\n");
                File.AppendAllText(Report,"BUILD COMPILE PASS\n");
                SessionState.SetBool("WoolJam.HadProgress",PlayerPrefs.HasKey("JamJam.Level"));
                SessionState.SetInt("WoolJam.OldProgress",PlayerPrefs.GetInt("JamJam.Level",0));
                SessionState.SetBool("WoolJam.Batch",true);
                EditorApplication.isPlaying=true;
            }
            catch(Exception ex) { File.AppendAllText(Report,"BATCH FAILED: "+ex+"\n"); Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        static void BeginRuntimeChecks()
        {
            var scene=UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            var minimumDot=1f;
            var blockedClicks=0;
            foreach(var level in scene.Levels)
            {
                scene.LoadLevel(Array.IndexOf(scene.Levels,level));
                Physics.SyncTransforms();
                if(CheckBlockedPointer(scene)) blockedClicks++;
                var distance=0f;
                for(var i=1;i<level.Path.Length-1;i++)
                {
                    distance+=Vector3.Distance(level.Path[i-1],level.Path[i]);
                    scene.Evaluate(distance-0.0001f,out _,out var before);
                    scene.Evaluate(distance+0.0001f,out _,out var after);
                    minimumDot=Mathf.Min(minimumDot,Vector3.Dot(before,after));
                }
            }
            if(blockedClicks==0) throw new InvalidOperationException("No blocked arrow was verified through EventSystem");
            File.AppendAllText(Report,"BLOCKED ARROW POINTER PASS / "+blockedClicks+" levels\n");
            if(minimumDot<0.999f) throw new InvalidOperationException("Curve direction is discontinuous at a path sample: "+minimumDot);
            File.AppendAllText(Report,"CURVE TANGENT CONTINUITY PASS / minimum dot "+minimumDot.ToString("F6")+"\n");
            scene.LoadLevel(0);
            Physics.SyncTransforms();
            var blockViews=UnityEngine.Object.FindObjectsByType<WoolBlockView>(FindObjectsSortMode.None);
            var clicked=false;
            foreach(var view in blockViews)
            {
                var direction=scene.State.Blocks[view.Index].Definition.Direction;
                var expected=direction==ExitDirection.Up?Vector3.forward:direction==ExitDirection.Down?Vector3.back:direction==ExitDirection.Left?Vector3.left:Vector3.right;
                if(Vector3.Dot(view.Arrow.up,expected)<0.99f) throw new InvalidOperationException("Sprite arrow does not match its exit direction");
            }
            File.AppendAllText(Report,"SPRITE ARROW DIRECTIONS PASS\n");
            foreach(var block in blockViews)
            {
                if(!scene.State.CanSelect(block.Index)) continue;
                var pointer=new PointerEventData(EventSystem.current) { position=Camera.main.WorldToScreenPoint(block.transform.position),button=PointerEventData.InputButton.Left };
                var results=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,results);
                if(results.Count==0 || results[0].gameObject!=block.gameObject) continue;
                ExecuteEvents.Execute(results[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
                clicked=scene.State.Blocks[block.Index].Phase==1;
                break;
            }
            if(!clicked) throw new InvalidOperationException("EventSystem physics click did not select a clear arrow");
            File.AppendAllText(Report,"RUNTIME POINTER ROUTE PASS\n");
            scene.PauseButton.onClick.Invoke();
            if(!scene.State.Paused || !scene.PausePanel.activeSelf) throw new InvalidOperationException("Runtime pause failed");
            scene.ResumeButton.onClick.Invoke();
            if(scene.State.Paused || scene.PausePanel.activeSelf) throw new InvalidOperationException("Runtime resume failed");
            for(var i=0;i<30000 && scene.State.Status==RoundStatus.Playing;i++) scene.State.Step(0.02f);
            if(scene.State.Status!=RoundStatus.Lost) throw new InvalidOperationException("Runtime loss failed");
            capture?.Dispose();
            capture=Observable.Timer(TimeSpan.FromSeconds(0.4f),UnityTimeProvider.Update).Subscribe(_=>
            {
                Capture("loss");
                scene.ResultRetryButton.onClick.Invoke();
                if(scene.State.Status!=RoundStatus.Playing || scene.State.Remaining!=scene.State.Total) throw new InvalidOperationException("Runtime retry failed");
                File.AppendAllText(Report,"RUNTIME LOSS RETRY PAUSE RESUME PASS\n");
                StartSolver();
            });
        }

        static bool CheckBlockedPointer(WoolScene scene)
        {
            foreach(var view in UnityEngine.Object.FindObjectsByType<WoolBlockView>(FindObjectsSortMode.None))
            {
                if(scene.State.CanSelect(view.Index)) continue;
                var pointer=new PointerEventData(EventSystem.current) { position=Camera.main.WorldToScreenPoint(view.transform.position),button=PointerEventData.InputButton.Left };
                var results=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,results);
                if(results.Count==0 || results[0].gameObject!=view.gameObject) continue;
                ExecuteEvents.Execute(results[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
                var block=scene.State.Blocks[view.Index];
                if(block.Phase!=0 || block.Slot!=-1 || block.Route.Count!=0)
                    throw new InvalidOperationException("Blocked arrow moved after a pointer click");
                foreach(var spool in scene.State.Spools)
                    if(spool.Block>=0) throw new InvalidOperationException("Blocked pointer click reserved a tray slot");
                Capture("blocked_arrow_"+(scene.CurrentLevel+1));
                return true;
            }
            return false;
        }

        [MenuItem("JamJam/Validate All Levels")]
        public static void ValidateLevels()
        {
            for(var level=1;level<=3;level++)
            {
                var asset=AssetDatabase.LoadAssetAtPath<WoolLevel>(WoolAssetsBuilder.Root+"/Levels/Level_"+level+".asset");
                var game=new WoolGame(asset.Data);
                for(var frame=0;frame<30000 && game.Status==RoundStatus.Playing;frame++)
                {
                    for(var b=0;b<game.Blocks.Count;b++)
                        if(game.CanSelect(b) && game.HasVisibleColor(game.Blocks[b].Definition.Color)) { game.Select(b); break; }
                    game.Step(0.02f);
                }
                if(game.Status!=RoundStatus.Won) throw new InvalidOperationException("No verified solution for level "+level);
                File.AppendAllText(Report,"LEVEL "+level+" SOLUTION PASS / "+game.Total+" units / "+game.Elapsed.ToString("F2")+" s\n");
            }
        }

        static void StartSolver()
        {
            solver?.Dispose(); motionProbe?.Dispose(); motionCapture?.Dispose(); completed=0;
            maximumTurnSpeed=0; motionFrames=0; motionFailed=false; arrowExitSamples=0; arrowExitDirections=0;
            var scene=UnityEngine.Object.FindFirstObjectByType<WoolScene>(); scene.LoadLevel(0);
            ResetMotionProbe();
            motionProbe=Observable.Interval(TimeSpan.FromSeconds(1d/60d),UnityTimeProvider.Update).Subscribe(_=>ProbeMotion());
            motionCapture=Observable.Interval(TimeSpan.FromSeconds(0.1f),UnityTimeProvider.Update).Subscribe(_=>
            {
                Capture("motion_"+motionFrames.ToString("D2"));
                motionFrames++;
                if(motionFrames>=30) { motionCapture?.Dispose(); motionCapture=null; }
            });
            solver=Observable.Interval(TimeSpan.FromSeconds(0.15f),UnityTimeProvider.Update).Subscribe(_=>Solve());
            capture?.Dispose();
            capture=Observable.Timer(TimeSpan.FromSeconds(2),UnityTimeProvider.Update).Subscribe(_=>Capture("collecting"));
        }

        static void Solve()
        {
            var scene=UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            if(scene==null) { solver?.Dispose(); solver=null; return; }
            if(motionFailed) { EditorApplication.Exit(1); return; }
            var game=scene.State;
            if(game.Status==RoundStatus.Lost) { File.AppendAllText(Report,"RUNTIME SOLVE FAILED\n"); solver?.Dispose(); solver=null; return; }
            if(game.Status==RoundStatus.Won)
            {
                if(!scene.ResultPanel.activeSelf) return;
                Capture("won_"+(scene.CurrentLevel+1));
                File.AppendAllText(Report,"RUNTIME LEVEL "+(scene.CurrentLevel+1)+" WIN PASS\n");
                completed++;
                if(completed>=3)
                {
                    solver?.Dispose(); solver=null; motionProbe?.Dispose(); motionProbe=null;
                    if(arrowExitSamples==0 || arrowExitDirections!=15) { File.AppendAllText(Report,"ARROW EXIT COVERAGE FAILED\n"); EditorApplication.Exit(1); return; }
                    File.AppendAllText(Report,"INITIAL ARROW MOVE PASS / "+arrowExitSamples+" samples / all four directions\n");
                    File.AppendAllText(Report,"RUNTIME MOTION PASS / maximum head turn "+maximumTurnSpeed.ToString("F2")+" deg/s / flat cardinal blocks\n");
                    File.AppendAllText(Report,"RUNTIME ALL LEVELS PASS\n");
                    if(Application.isBatchMode)
                    {
                        capture?.Dispose();
                        capture=Observable.Timer(TimeSpan.FromSeconds(0.4f),UnityTimeProvider.Update).Subscribe(_=>EditorApplication.isPlaying=false);
                    }
                    return;
                }
                scene.NextButton.onClick.Invoke(); ResetMotionProbe(); Capture("initial_"+(scene.CurrentLevel+1)); return;
            }
            for(var i=0;i<game.Blocks.Count;i++)
                if(game.CanSelect(i) && game.HasVisibleColor(game.Blocks[i].Definition.Color))
                {
                    scene.Select(i);
                    break;
                }
        }

        static void ResetMotionProbe()
        {
            var head=GameObject.Find("DragonHead(Clone)");
            if(head==null) throw new InvalidOperationException("Dragon head is missing");
            previousHeadRotation=head.transform.rotation;
            previousProbeTime=Time.timeAsDouble;
        }

        static void ProbeMotion()
        {
            try { InspectMotion(); }
            catch(Exception ex)
            {
                motionFailed=true;
                solver?.Dispose(); solver=null;
                motionProbe?.Dispose(); motionProbe=null;
                File.AppendAllText(Report,"RUNTIME MOTION FAILED: "+ex+"\n");
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        static void InspectMotion()
        {
            var scene=UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            var head=GameObject.Find("DragonHead(Clone)");
            if(scene==null || head==null) return;
            var now=Time.timeAsDouble;
            var elapsed=(float)(now-previousProbeTime);
            if(elapsed>0.001f)
            {
                var speed=Quaternion.Angle(previousHeadRotation,head.transform.rotation)/elapsed;
                maximumTurnSpeed=Mathf.Max(maximumTurnSpeed,speed);
                if(speed>250) throw new InvalidOperationException("Dragon head exceeded bounded turn speed: "+speed);
            }
            previousHeadRotation=head.transform.rotation;
            previousProbeTime=now;
            foreach(var view in UnityEngine.Object.FindObjectsByType<WoolBlockView>(FindObjectsSortMode.None))
            {
                var block=scene.State.Blocks[view.Index];
                if(block.Phase!=1) continue;
                WoolRouting.Evaluate(block.Route,block.Travel*WoolRouting.Speed,out var point,out _);
                if(Mathf.Abs(view.transform.position.y-0.12f)>0.0001f ||
                    Mathf.Abs(view.transform.position.x-point.X)>0.001f || Mathf.Abs(view.transform.position.z-point.Z)>0.001f)
                    throw new InvalidOperationException("Moving block left its flat cardinal route");
                var distance=block.Travel*WoolRouting.Speed;
                if(distance>0 && distance<=block.ExitDistance)
                {
                    var direction=block.Definition.Direction;
                    var expected=direction==ExitDirection.Up?Vector3.forward:direction==ExitDirection.Down?Vector3.back:direction==ExitDirection.Left?Vector3.left:Vector3.right;
                    var start=WoolScene.BlockPosition(block.Definition,scene.Levels[scene.CurrentLevel].Data);
                    if(Vector3.Distance(view.transform.position,start+expected*distance)>0.001f || Vector3.Dot(view.Arrow.up,expected)<0.99f)
                        throw new InvalidOperationException("Block turned before its initial arrow movement finished");
                    arrowExitSamples++;
                    arrowExitDirections|=1<<(int)direction;
                }
            }
        }

        static void Capture(string name)
        {
            Directory.CreateDirectory("Logs/WoolJamCaptures");
            var camera=Camera.main;
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if(camera==null || canvas==null) return;
            var previousTarget=camera.targetTexture;
            var previousActive=RenderTexture.active;
            var previousMode=canvas.renderMode;
            var previousCamera=canvas.worldCamera;
            var previousAspect=camera.aspect;
            var render=new RenderTexture(540,960,24);
            var image=new Texture2D(540,960,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=render; camera.aspect=540f/960f;
                canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=render;
                image.ReadPixels(new Rect(0,0,540,960),0,0); image.Apply();
                File.WriteAllBytes("Logs/WoolJamCaptures/"+name+".png",image.EncodeToPNG());
                File.AppendAllText(Report,"CAPTURE "+name+" saved\n");
            }
            finally
            {
                camera.targetTexture=previousTarget; camera.aspect=previousAspect; RenderTexture.active=previousActive;
                canvas.renderMode=previousMode; canvas.worldCamera=previousCamera;
                UnityEngine.Object.DestroyImmediate(image); render.Release(); UnityEngine.Object.DestroyImmediate(render);
            }
        }

        static void SetGameView()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=sizesType.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group=sizesType.GetMethod("GetGroup").Invoke(singleton,new[] { Enum.Parse(groupType,"Standalone") });
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            var modeType=assembly.GetType("UnityEditor.GameViewSizeType");
            var size=Activator.CreateInstance(sizeType,new[] { Enum.Parse(modeType,"FixedResolution"),(object)540,960,"Wool Jam 9:16" });
            group.GetType().GetMethod("AddCustomSize").Invoke(group,new[] { size });
            var count=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null)+(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);
            var viewType=assembly.GetType("UnityEditor.GameView"); var view=EditorWindow.GetWindow(viewType);
            viewType.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,count-1);
            view.Focus();
        }
    }
}


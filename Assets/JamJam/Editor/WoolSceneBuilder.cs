using System;
using System.Collections.Generic;
using System.IO;
using JamJam;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static class WoolSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/WoolJam.unity";
        static Font font;
        static Sprite panelSprite;
        static Color ink = new Color(0.16f,0.23f,0.28f);

        [MenuItem("JamJam/Build Wool Jam Scene")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before rebuilding the scene");
            if(!Application.isBatchMode)
                for(var i=0;i<SceneManager.sceneCount;i++)
                    if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before rebuilding Wool Jam");
            WoolAssetsBuilder.Build(out var art);
            BuildLevels(out var levels);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildPanelSprite();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("Wool Jam");
            var controller = root.AddComponent<WoolScene>(); controller.Art=art; controller.Levels=levels;
            controller.World = new GameObject("Gameplay").transform; controller.World.SetParent(root.transform,false);
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag="MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=9.9f;
            camera.transform.position=new Vector3(0,19,-14); camera.transform.LookAt(new Vector3(0,0,0.8f));
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(0.97f,0.91f,0.78f); camera.nearClipPlane=0.1f; camera.farClipPlane=80;
            cameraObject.AddComponent<AudioListener>(); cameraObject.AddComponent<PhysicsRaycaster>();
            var lightObject=new GameObject("Soft Sun"); var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional;
            light.intensity=1.05f; light.color=new Color(1,0.94f,0.83f); light.shadows=LightShadows.Soft; light.shadowStrength=0.35f; light.shadowBias=0.03f;
            light.transform.rotation=Quaternion.Euler(48,-30,0);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(0.5f,0.55f,0.6f); RenderSettings.ambientIntensity=0.8f;
            RenderSettings.skybox=null;
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name="Knitted Ground";
            ground.transform.position=new Vector3(0,-0.12f,0); ground.transform.localScale=new Vector3(35,0.16f,40);
            var floor=new Material(art.Road); floor.name="PastelGround"; floor.SetColor("_BaseColor",new Color(0.86f,0.8f,0.67f)); floor.SetTextureScale("_BaseMap",new Vector2(15,20)); floor.SetTextureScale("_BumpMap",new Vector2(15,20));
            SaveMaterial(floor,"Ground",out var floorMaterial); ground.GetComponent<Renderer>().sharedMaterial=floorMaterial;
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(WoolAssetsBuilder.Root+"/Meshes/Rounded.asset");
            WoolAssetsBuilder.Part(null,"Board Cushion",mesh,new Vector3(0,-0.01f,-5.05f),new Vector3(8.7f,0.18f,6.3f),art.Rim,out _);
            var events=new GameObject("EventSystem"); events.AddComponent<EventSystem>(); events.AddComponent<InputSystemUIInputModule>();
            BuildCanvas(controller);
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth=540; PlayerSettings.defaultScreenHeight=960;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[] { new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity",false) };
            if(previous.IsValid() && previous.isLoaded && !previous.isDirty) EditorSceneManager.CloseScene(previous,true);
            AssetDatabase.SaveAssets();
            Debug.Log("WoolJam scene and three levels generated");
        }

        static void SaveMaterial(Material source,string name,out Material material)
        {
            var path=WoolAssetsBuilder.Root+"/Materials/"+name+".mat";
            material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=source; AssetDatabase.CreateAsset(material,path); }
            else { EditorUtility.CopySerialized(source,material); UnityEngine.Object.DestroyImmediate(source); }
        }

        static void BuildLevels(out WoolLevel[] levels)
        {
            Directory.CreateDirectory(WoolAssetsBuilder.Root+"/Levels");
            levels=new WoolLevel[3];
            for(var levelIndex=0;levelIndex<3;levelIndex++)
            {
                var path=WoolAssetsBuilder.Root+"/Levels/Level_"+(levelIndex+1)+".asset";
                var level=AssetDatabase.LoadAssetAtPath<WoolLevel>(path);
                if(level==null) { level=ScriptableObject.CreateInstance<WoolLevel>(); AssetDatabase.CreateAsset(level,path); }
                level.Title=new[] { "First Threads", "Color Traffic", "The Great Unravel" }[levelIndex];
                var columns=4+levelIndex*2; var rows=4+levelIndex;
                var blocks=new List<BlockDefinition>();
                for(var y=0;y<rows;y++)
                    for(var pair=0;pair<columns/2;pair++)
                    {
                        var x=pair*2;
                        blocks.Add(new BlockDefinition { X=x,Y=y,Width=2,Height=1,Direction=x<columns/2?ExitDirection.Left:ExitDirection.Right,
                            Color=(y+pair*2+levelIndex)%(levelIndex==0?3:6),Capacity=4+(pair+y)%3 });
                    }
                if(levelIndex>0)
                {
                    var first=blocks[0]; var above=blocks[columns/2];
                    first.Width=1; first.Height=2; first.Direction=ExitDirection.Down;
                    above.X=1; above.Y=0; above.Width=1; above.Height=2; above.Direction=ExitDirection.Up;
                    blocks[0]=first; blocks[columns/2]=above;
                }
                var yarn=new List<int>();
                var sorted=new List<BlockDefinition>(blocks);
                sorted.Sort((a,b)=>
                {
                    var ay=a.Y*10+Mathf.Min(a.X,columns-a.X-a.Width);
                    var by=b.Y*10+Mathf.Min(b.X,columns-b.X-b.Width);
                    return ay.CompareTo(by);
                });
                foreach(var b in sorted) for(var i=0;i<b.Capacity;i++) yarn.Add(b.Color);
                var points=new List<Vector3>();
                var control=new[] { new Vector3(-4,0,8),new Vector3(2.8f,0,8),new Vector3(3.75f,0,6.8f),new Vector3(2.8f,0,5.55f),new Vector3(-2.8f,0,5.55f),new Vector3(-3.75f,0,4.3f),new Vector3(-2.8f,0,3.05f),new Vector3(2.5f,0,3.05f),new Vector3(3.35f,0,1.8f),new Vector3(2.15f,0,0.75f),new Vector3(-1.65f,0,0.75f) };
                for(var i=0;i<control.Length-1;i++)
                {
                    var a=control[Mathf.Max(0,i-1)]; var b=control[i]; var c=control[i+1]; var d=control[Mathf.Min(control.Length-1,i+2)];
                    for(var sample=0;sample<20;sample++)
                    {
                        var t=sample/20f;
                        points.Add(0.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
                    }
                }
                points.Add(control[control.Length-1]);
                var length=0f; for(var i=1;i<points.Count;i++) length+=Vector3.Distance(points[i-1],points[i]);
                level.Path=points.ToArray();
                level.Data=new LevelDefinition { Columns=columns,Rows=rows,Slots=4,PathLength=length,InitialHead=13.5f+levelIndex*5,Speed=0.21f+levelIndex*0.035f,Spacing=levelIndex==0?0.36f:0.24f,Blocks=blocks.ToArray(),Yarn=yarn.ToArray() };
                var errors=WoolGame.Validate(level.Data); if(errors.Count>0) throw new InvalidOperationException(string.Join("; ",errors));
                levels[levelIndex]=level; EditorUtility.SetDirty(level);
            }
        }

        static void BuildPanelSprite()
        {
            const int size=128; const float radius=26;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            for(var y=0;y<size;y++) for(var x=0;x<size;x++)
            {
                var q=new Vector2(Mathf.Abs(x-(size-1)*0.5f)-(size*0.5f-radius),Mathf.Abs(y-(size-1)*0.5f)-(size*0.5f-radius));
                var distance=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
                texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(0.5f-distance)));
            }
            texture.Apply(); var path=WoolAssetsBuilder.Root+"/Textures/RoundedPanel.png";
            File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.spriteBorder=new Vector4(30,30,30,30); importer.mipmapEnabled=false; importer.SaveAndReimport();
            panelSprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void BuildCanvas(WoolScene game)
        {
            var go=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1080,1920); scaler.matchWidthOrHeight=0.5f;
            var parent=go.transform;
            Label(parent,"Brand","WOOL JAM",new Vector2(0,810),new Vector2(640,100),68,ink,out var title); title.fontStyle=FontStyle.Bold;
            Label(parent,"Goal","UNWIND THE DRAGON. SAVE THE KITTEN.",new Vector2(0,737),new Vector2(860,45),25,new Color(0.29f,0.32f,0.28f),out _);
            Panel(parent,"LevelBadge",new Vector2(-420,820),new Vector2(124,124),new Color(1,0.98f,0.91f),out var badge);
            Label(badge,"LevelCaption","LEVEL",new Vector2(0,28),new Vector2(120,35),20,ink,out _);
            Label(badge,"LevelNumber","01",new Vector2(0,-18),new Vector2(120,66),48,ink,out game.LevelNumber);
            MakeButton(parent,"Pause","II",new Vector2(427,820),new Vector2(115,115),new Color(1,0.98f,0.91f),ink,out game.PauseButton);
            Panel(parent,"ProgressTrack",new Vector2(0,662),new Vector2(790,28),new Color(0.83f,0.77f,0.62f),out var track);
            Panel(track,"ProgressFill",Vector2.zero,new Vector2(778,18),new Color(0.23f,0.72f,0.57f),out var fill);
            game.ProgressFill=fill.GetComponent<Image>(); game.ProgressFill.type=Image.Type.Filled; game.ProgressFill.fillMethod=Image.FillMethod.Horizontal; game.ProgressFill.fillAmount=0;
            Label(parent,"ProgressNumber","0%",new Vector2(455,661),new Vector2(100,50),28,ink,out game.ProgressNumber);
            Label(parent,"TrayCaption","THREAD TRAY",new Vector2(0,-55),new Vector2(500,35),20,new Color(0.29f,0.32f,0.28f),out _);
            Label(parent,"Instructions","TAP A CLEAR ARROW TO COLLECT ITS THREAD",new Vector2(0,-730),new Vector2(1000,48),25,ink,out _);
            MakeButton(parent,"Retry","RETRY",new Vector2(-320,-833),new Vector2(265,95),new Color(1,0.97f,0.86f),ink,out game.RetryButton);
            Label(parent,"Help","Need room? Tap a + slot",new Vector2(160,-833),new Vector2(610,70),28,new Color(0.29f,0.32f,0.28f),out _);
            Overlay(parent,"PausePanel",out var pause); game.PausePanel=pause.gameObject;
            Panel(pause,"Card",Vector2.zero,new Vector2(820,500),new Color(1,0.98f,0.9f),out var pauseCard);
            Label(pauseCard,"Title","TAKE A BREATHER",new Vector2(0,130),new Vector2(760,90),54,ink,out _);
            Label(pauseCard,"Body","Your kitten is waiting for you.",new Vector2(0,30),new Vector2(700,70),30,ink,out _);
            MakeButton(pauseCard,"Resume","KEEP UNRAVELING",new Vector2(0,-130),new Vector2(640,110),new Color(0.25f,0.72f,0.56f),Color.white,out game.ResumeButton);
            pause.gameObject.SetActive(false);
            Overlay(parent,"ResultPanel",out var result); game.ResultPanel=result.gameObject;
            Panel(result,"Card",new Vector2(0,-50),new Vector2(850,650),new Color(1,0.98f,0.9f),out var card);
            var won=new GameObject("WonContent",typeof(RectTransform)).GetComponent<RectTransform>(); won.SetParent(card,false); won.sizeDelta=new Vector2(800,350); won.anchoredPosition=new Vector2(0,100); game.WonContent=won.gameObject;
            Label(won,"Title","PURRFECT!",new Vector2(0,100),new Vector2(760,100),76,new Color(0.19f,0.62f,0.46f),out var win); win.fontStyle=FontStyle.Bold;
            Label(won,"Subtitle","A little less yarn. A little more joy.",new Vector2(0,-5),new Vector2(740,80),31,ink,out _);
            var lost=new GameObject("LostContent",typeof(RectTransform)).GetComponent<RectTransform>(); lost.SetParent(card,false); lost.sizeDelta=new Vector2(800,350); lost.anchoredPosition=new Vector2(0,100); game.LostContent=lost.gameObject;
            Label(lost,"Title","SO CLOSE!",new Vector2(0,100),new Vector2(760,100),70,new Color(0.82f,0.41f,0.25f),out _);
            Label(lost,"Subtitle","Clear the arrows and try another thread.",new Vector2(0,-5),new Vector2(760,80),30,ink,out _);
            MakeButton(card,"Next","NEXT ADVENTURE",new Vector2(0,-105),new Vector2(670,108),new Color(0.23f,0.71f,0.54f),Color.white,out game.NextButton);
            MakeButton(card,"PlayAgain","PLAY AGAIN",new Vector2(0,-240),new Vector2(670,100),new Color(0.94f,0.87f,0.7f),ink,out game.ResultRetryButton);
            result.gameObject.SetActive(false);
        }

        static void Overlay(Transform parent,string name,out RectTransform rect)
        {
            Panel(parent,name,Vector2.zero,Vector2.zero,new Color(0.16f,0.2f,0.25f,0.62f),out rect);
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            rect.GetComponent<Image>().sprite=null;
            rect.GetComponent<Image>().raycastTarget=true;
        }

        static void Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color,out RectTransform rect)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f); rect.anchoredPosition=position; rect.sizeDelta=size;
            var image=go.GetComponent<Image>(); image.sprite=panelSprite; image.type=Image.Type.Sliced; image.color=color; image.raycastTarget=false;
        }

        static void Label(Transform parent,string name,string value,Vector2 position,Vector2 size,int fontSize,Color color,out Text text)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f); rect.anchoredPosition=position; rect.sizeDelta=size;
            text=go.GetComponent<Text>(); text.text=value; text.font=font; text.fontSize=fontSize; text.color=color; text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false;
        }

        static void MakeButton(Transform parent,string name,string value,Vector2 position,Vector2 size,Color color,Color textColor,out Button button)
        {
            Panel(parent,name,position,size,color,out var rect); var image=rect.GetComponent<Image>(); image.raycastTarget=true;
            button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.highlightedColor=new Color(1,0.97f,0.88f); colors.pressedColor=new Color(0.85f,0.83f,0.77f); button.colors=colors;
            Label(rect,"Label",value,Vector2.zero,size-new Vector2(25,10),32,textColor,out var label); label.fontStyle=FontStyle.Bold;
        }
    }
}

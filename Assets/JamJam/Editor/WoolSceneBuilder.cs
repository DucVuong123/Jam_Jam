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
            WoolUiStyle.LoadFont(out font, out _);
            BuildPanelSprite();
            WoolUiStyle.BuildSprites();
            WoolUiArt.Import();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("Wool Jam");
            var controller = root.AddComponent<WoolScene>(); controller.Art=art; controller.Levels=levels;
            controller.World = new GameObject("Gameplay").transform; controller.World.SetParent(root.transform,false);
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag="MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>(); cameraObject.AddComponent<PhysicsRaycaster>();
            var lightObject=new GameObject("Soft Sun"); var light=lightObject.AddComponent<Light>();
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name="Knitted Ground";
            ground.transform.position=new Vector3(0,-0.12f,0); ground.transform.localScale=new Vector3(35,0.16f,40);
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            WoolArtBuilder.EnsureMeadow(out _);
            WoolArtBuilder.EnsureShelf(out _);
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(WoolAssetsBuilder.Root+"/Meshes/Rounded.asset");
            WoolAssetsBuilder.Part(null,"Board Cushion",mesh,new Vector3(0,-0.01f,WoolRouting.BoardCenterZ),new Vector3(8.7f,0.18f,6.3f),art.Rim,out var cushion);
            controller.Cushion=cushion;
            if(levels.Length>0 && levels[0]!=null) cushion.localScale=new Vector3(levels[0].Data.Columns*WoolRouting.CellSize+WoolScene.CushionPad,cushion.localScale.y,levels[0].Data.Rows*WoolRouting.CellSize+WoolScene.CushionPad);
            var volume=new GameObject("Art Volume").AddComponent<Volume>();
            WoolArtBuilder.Apply(camera,light,ground.GetComponent<Renderer>(),cushion.GetComponent<Renderer>(),volume);
            var events=new GameObject("EventSystem"); events.AddComponent<EventSystem>(); events.AddComponent<InputSystemUIInputModule>();
            BuildCanvas(controller);
            WoolUiBuilder.StyleGame(controller.PausePanel.transform.parent);
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth=540; PlayerSettings.defaultScreenHeight=960;
            EditorSceneManager.SaveScene(scene,ScenePath);
            ApplyBuildScenes();
            if(previous.IsValid() && previous.isLoaded && !previous.isDirty) EditorSceneManager.CloseScene(previous,true);
            AssetDatabase.SaveAssets();
            Debug.Log("WoolJam scene and " + levels.Length + " levels generated");
        }

        static void BuildLevels(out WoolLevel[] levels)
        {
            WoolLevelAssets.Write(out levels);
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
            Panel(pause,"Card",Vector2.zero,new Vector2(820,640),new Color(1,0.98f,0.9f),out var pauseCard);
            Label(pauseCard,"Title","TAKE A BREATHER",new Vector2(0,160),new Vector2(760,90),54,ink,out _);
            Label(pauseCard,"Body","Your kitten is waiting for you.",new Vector2(0,60),new Vector2(700,70),30,ink,out _);
            MakeButton(pauseCard,"Resume","KEEP UNRAVELING",new Vector2(0,-100),new Vector2(640,110),new Color(0.25f,0.72f,0.56f),Color.white,out game.ResumeButton);
            pause.gameObject.SetActive(false);
            Overlay(parent,"ResultPanel",out var result); game.ResultPanel=result.gameObject;
            Panel(result,"Card",new Vector2(0,-50),new Vector2(850,800),new Color(1,0.98f,0.9f),out var card);
            var won=new GameObject("WonContent",typeof(RectTransform)).GetComponent<RectTransform>(); won.SetParent(card,false); won.sizeDelta=WoolUiBuilder.ResultContentSize; won.anchoredPosition=new Vector2(0,150); game.WonContent=won.gameObject;
            Label(won,"Title","PURRFECT!",new Vector2(0,100),new Vector2(760,100),76,new Color(0.19f,0.62f,0.46f),out var win); win.fontStyle=FontStyle.Bold;
            Label(won,"Subtitle","A little less yarn. A little more joy.",new Vector2(0,-5),new Vector2(740,80),31,ink,out _);
            var lost=new GameObject("LostContent",typeof(RectTransform)).GetComponent<RectTransform>(); lost.SetParent(card,false); lost.sizeDelta=WoolUiBuilder.ResultContentSize; lost.anchoredPosition=new Vector2(0,150); game.LostContent=lost.gameObject;
            Label(lost,"Title","SO CLOSE!",new Vector2(0,100),new Vector2(760,100),70,new Color(0.82f,0.41f,0.25f),out _);
            Label(lost,"Subtitle","Clear the arrows and try another thread.",new Vector2(0,-5),new Vector2(760,80),30,ink,out _);
            MakeButton(card,"Next","NEXT ADVENTURE",new Vector2(0,-55),new Vector2(670,108),new Color(0.23f,0.71f,0.54f),Color.white,out game.NextButton);
            MakeButton(card,"PlayAgain","PLAY AGAIN",new Vector2(0,-183),new Vector2(670,100),new Color(0.94f,0.87f,0.7f),ink,out game.ResultRetryButton);
            result.gameObject.SetActive(false);
            AddHome(game,pause,result);
            WoolUiBuilder.EnsureHud(parent,out game.SpeedButton,out game.SpeedLabel,out game.UnlockTemplate,out _);
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
            WoolUiStyle.LoadFont(out font,out _);
            panelSprite=AssetDatabase.LoadAssetAtPath<Sprite>(WoolAssetsBuilder.Root+"/Textures/RoundedPanel.png");
            if(panelSprite==null) throw new InvalidOperationException("RoundedPanel sprite is missing");
        }

        internal static void ApplyBuildScenes()
        {
            var scenes=new List<EditorBuildSettingsScene>();
            if(File.Exists(WoolLoadingBuilder.ScenePath)) scenes.Add(new EditorBuildSettingsScene(WoolLoadingBuilder.ScenePath,true));
            if(File.Exists(WoolLobbyBuilder.ScenePath)) scenes.Add(new EditorBuildSettingsScene(WoolLobbyBuilder.ScenePath,true));
            scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            const string samplePath="Assets/Scenes/SampleScene.unity";
            if(File.Exists(samplePath)) scenes.Add(new EditorBuildSettingsScene(samplePath,false));
            foreach(var existing in EditorBuildSettings.scenes)
                if(existing.path!=WoolLoadingBuilder.ScenePath && existing.path!=WoolLobbyBuilder.ScenePath && existing.path!=ScenePath && existing.path!=samplePath) scenes.Add(new EditorBuildSettingsScene(existing.path,existing.enabled));
            EditorBuildSettings.scenes=scenes.ToArray();
        }

        static void AddHome(WoolScene game,Transform pause,Transform result)
        {
            var buttons=new List<Button>();
            foreach(var (parent,position) in new[] { (pause.Find("Card"),WoolUiBuilder.PauseHomePosition),(result.Find("Card"),WoolUiBuilder.ResultHomePosition) })
            {
                var existing=parent.Find("Home"); if(existing==null) existing=parent.parent.Find("Home");
                if(existing!=null) { existing.SetParent(parent,false); ((RectTransform)existing).anchoredPosition=position; buttons.Add(existing.GetComponent<Button>()); continue; }
                MakeButton(parent,"Home","HOME",position,new Vector2(400,100),new Color(1,0.98f,0.91f),ink,out var button);
                buttons.Add(button);
            }
            game.HomeButtons=buttons.ToArray();
        }

        internal static void Overlay(Transform parent,string name,out RectTransform rect)
        {
            Panel(parent,name,Vector2.zero,Vector2.zero,new Color(0.16f,0.2f,0.25f,0.62f),out rect);
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            rect.GetComponent<Image>().sprite=null;
            rect.GetComponent<Image>().raycastTarget=true;
        }

        internal static void Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color,out RectTransform rect)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f); rect.anchoredPosition=position; rect.sizeDelta=size;
            var image=go.GetComponent<Image>(); image.sprite=panelSprite; image.type=Image.Type.Sliced; image.color=color; image.raycastTarget=false;
        }

        internal static void Label(Transform parent,string name,string value,Vector2 position,Vector2 size,int fontSize,Color color,out Text text)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f); rect.anchoredPosition=position; rect.sizeDelta=size;
            text=go.GetComponent<Text>(); text.text=value; text.font=font; text.fontSize=fontSize; text.color=color; text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false;
        }

        internal static void MakeButton(Transform parent,string name,string value,Vector2 position,Vector2 size,Color color,Color textColor,out Button button)
        {
            Panel(parent,name,position,size,color,out var rect); var image=rect.GetComponent<Image>(); image.raycastTarget=true;
            button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.highlightedColor=new Color(1,0.97f,0.88f); colors.pressedColor=new Color(0.85f,0.83f,0.77f); button.colors=colors;
            Label(rect,"Label",value,Vector2.zero,size-new Vector2(25,10),32,textColor,out var label); label.fontStyle=FontStyle.Bold;
        }
    }
}

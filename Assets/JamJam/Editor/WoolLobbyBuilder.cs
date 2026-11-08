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
            WoolUiStyle.BuildSprites();
            WoolUiArt.Import();
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
            WoolUiBuilder.StyleLobby(lobby.LobbyPanel.transform.parent,camera);
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
            WoolSceneBuilder.MakeButton(home,"Settings","SETTINGS",new Vector2(340,830),new Vector2(240,100),cream,ink,out lobby.SettingsButton);
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
            WoolSceneBuilder.MakeButton(list,"Back","BACK",new Vector2(-350,830),new Vector2(220,100),cream,ink,out lobby.BackButton);
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
            lobby.Scroll=scrollRect; scrollRect.scrollSensitivity=40; scrollRect.inertia=true; scrollRect.decelerationRate=0.135f;
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
            WoolUiBuilder.StyleCard(root.gameObject);
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
            WoolSceneBuilder.Panel(box,"Check",Vector2.zero,new Vector2(130,70),green,out var check);
            toggle=row.gameObject.AddComponent<Toggle>(); toggle.targetGraphic=box.GetComponent<Image>(); toggle.graphic=check.GetComponent<Image>(); toggle.isOn=true;
        }

        static void FullScreen(Transform parent,string name,out RectTransform rect)
        {
            rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}

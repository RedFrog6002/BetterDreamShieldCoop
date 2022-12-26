using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;
using System.Reflection;
using Modding.Converters;
using InControl;
using Newtonsoft.Json;
using Satchel.BetterMenus;
using System.Linq;

namespace BetterDreamShieldCoop
{
    public class BetterDreamShieldCoop : Mod, IGlobalSettings<Dictionary<string, CoopData>>, ICustomMenuMod
    {
        internal const string VersionNotice = "Now with CustomKnight and Keybind support!";

        internal static BetterDreamShieldCoop Instance;

        internal static Dictionary<string, CoopData> settings = new();

        internal static Dictionary<string, Func<ICoop>> CoopList = new();
        internal static Dictionary<string, ICoop> ActiveCoopList = new();

        internal static Menu menuModel;
        internal static MenuScreen menuRef;

        public static void AddCoopObject(Func<ICoop> CreateCoop, string id, Func<string, string> OptionNameMap)
        {
            if (!settings.ContainsKey(id))
                settings[id] = new() { enabled = false, Keybinds = new(), OptionNameMap = OptionNameMap };
            else
                settings[id].OptionNameMap = OptionNameMap;
            CoopList[id] = CreateCoop;
        }

        internal static void Enable(string name)
        {
            if (Instance.listener)
                ActiveCoopList[name] = CoopList[name]();
        }

        internal static void Disable(string name)
        {
            if (Instance.listener)
            {
                ActiveCoopList[name].DestroyCoop();
                ActiveCoopList.Remove(name);
            }
        }

        private void CreateCoops()
        {
            foreach ((string name, Func<ICoop> coop) in CoopList)
                if (settings[name].enabled)
                    ActiveCoopList[name] = coop();
        }

        private void DestroyCoops()
        {
            foreach (ICoop coop in ActiveCoopList.Values)
                coop.DestroyCoop();
            ActiveCoopList.Clear();
        }

        public BetterDreamShieldCoop() : base("Dreamshield Coop")
        {
            Instance = this;
        }

        public override string GetVersion()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string ver = asm.GetName().Version.ToString();

            return VersionNotice == "" ? ver : $"{ver} - {VersionNotice}";
        }

        private bool CKInstalled => ModHooks.GetMod("CustomKnight") is Mod;

        public override void Initialize()
        {
            Log("Initializing");

            Instance = this;
            On.HeroController.Awake += HeroController_Awake;
            On.QuitToMenu.Start += QuitToMenu_Start;

            if (CKInstalled)
            {
                Log("Initializing CustomKnight Support");
                CustomKnightCompatibility.AddCustomKnightHandlers();
            }

            AddCoopObject(Dreamshield.Create, "Dreamshield", Dreamshield.GetOptionName);
            AddCoopObject(Grimmchild.Create, "Grimmchild", Grimmchild.GetOptionName);

            menuModel = new Menu("Coop Settings", settings.Select(pair => pair.Value.GetMenuElement(pair.Key)).ToArray());

            Log("Initialized");
        }

        private void HeroController_Awake(On.HeroController.orig_Awake orig, HeroController self)
        {
            Dreamshield.CreatePrefab();
            Grimmchild.CreatePrefab();
            CreateCoops();
            listener = new();
            listener.AddComponent<KeyListener>();
            GameObject.DontDestroyOnLoad(listener);
            orig(self);
        }

        private IEnumerator QuitToMenu_Start(On.QuitToMenu.orig_Start orig, QuitToMenu self)
        {
            GameObject.Destroy(listener);
            DestroyCoops();
            yield return orig(self);
        }

        private GameObject listener;

        public bool ToggleButtonInsideMenu => false;

        private class KeyListener : MonoBehaviour
        {
            private void Update()
            {
                foreach ((string name, ICoop coop) in ActiveCoopList)
                {
                    KeyBinds keys = settings[name].Keybinds;
                    if (keys.Up.IsPressed)
                        coop.Up(!keys.Up.WasPressed);
                    if (keys.Down.IsPressed)
                        coop.Down(!keys.Down.WasPressed);
                    if (keys.Left.IsPressed)
                        coop.Left(!keys.Left.WasPressed);
                    if (keys.Right.IsPressed)
                        coop.Right(!keys.Right.WasPressed);
                    if (keys.Teleport.IsPressed)
                        coop.Teleport(!keys.Teleport.WasPressed);
                    if (keys.Special1.IsPressed)
                        coop.Special1(!keys.Special1.WasPressed);
                    if (keys.Special2.IsPressed)
                        coop.Special2(!keys.Special2.WasPressed);
                    if (keys.Special3.IsPressed)
                        coop.Special3(!keys.Special3.WasPressed);
                    if (keys.Special4.IsPressed)
                        coop.Special4(!keys.Special4.WasPressed);
                }
            }
        }

        public void OnLoadGlobal(Dictionary<string, CoopData> s) => settings = s;

        public Dictionary<string, CoopData> OnSaveGlobal() => settings;

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            if (!menuRef)
            {
                menuRef = menuModel.GetMenuScreen(modListMenu);
                foreach ((string _, CoopData data) in settings)
                    data.GetMenuScreen(menuRef);
            }
            return menuRef;
        }
    }
    public class CoopData
    {
        [NonSerialized]
        public Func<string, string> OptionNameMap;
        public bool enabled;
        [JsonConverter(typeof(PlayerActionSetConverter))]
        public KeyBinds Keybinds = new KeyBinds();

        private void Enable(bool enable, string name)
        {
            if (enabled != enable)
            {
                if (enable)
                    BetterDreamShieldCoop.Enable(name);
                else
                    BetterDreamShieldCoop.Disable(name);
            }
            enabled = enable;
        }

        public Element GetMenuElement(string name)
        {
            menuModel = new Menu(name, new Element[]
            {
                Blueprints.HorizontalBoolOption("Enabled", "", b => Enable(b, name), () => enabled),
                Blueprints.KeyAndButtonBind(OptionNameMap("Up"), Keybinds.Up, Keybinds.Up),
                Blueprints.KeyAndButtonBind(OptionNameMap("Down"), Keybinds.Down, Keybinds.Down),
                Blueprints.KeyAndButtonBind(OptionNameMap("Left"), Keybinds.Left, Keybinds.Left),
                Blueprints.KeyAndButtonBind(OptionNameMap("Right"), Keybinds.Right, Keybinds.Right),
                Blueprints.KeyAndButtonBind(OptionNameMap("Teleport"), Keybinds.Teleport, Keybinds.Teleport),
                Blueprints.KeyAndButtonBind(OptionNameMap("Special1"), Keybinds.Special1, Keybinds.Special1),
                Blueprints.KeyAndButtonBind(OptionNameMap("Special2"), Keybinds.Special2, Keybinds.Special2),
                Blueprints.KeyAndButtonBind(OptionNameMap("Special3"), Keybinds.Special3, Keybinds.Special3),
                Blueprints.KeyAndButtonBind(OptionNameMap("Special4"), Keybinds.Special4, Keybinds.Special4)
            });
            return Blueprints.NavigateToMenu(name, "Enabled and Keybind options", () => menuRef);
        }
        internal Menu menuModel;
        internal MenuScreen menuRef;
        public MenuScreen GetMenuScreen(MenuScreen upperMenu)
        {
            if (!menuRef)
                menuRef = menuModel.GetMenuScreen(upperMenu);
            return menuRef;
        }
    }
    public class KeyBinds : PlayerActionSet
    {
        public PlayerAction Up;
        public PlayerAction Down;
        public PlayerAction Left;
        public PlayerAction Right;
        public PlayerAction Teleport;
        public PlayerAction Special1;
        public PlayerAction Special2;
        public PlayerAction Special3;
        public PlayerAction Special4;
        public KeyBinds()
        {
            Up = CreatePlayerAction("Up");
            Down = CreatePlayerAction("Down");
            Left = CreatePlayerAction("Left");
            Right = CreatePlayerAction("Right");
            Teleport = CreatePlayerAction("Teleport");
            Special1 = CreatePlayerAction("Special1");
            Special2 = CreatePlayerAction("Special2");
            Special3 = CreatePlayerAction("Special3");
            Special4 = CreatePlayerAction("Special4");
        }
    }
}
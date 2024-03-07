using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using Buttplug.Core;
using HPUI;
using Il2Cpp;
using Il2CppCrazyMinnow.SALSA;
using Il2CppEekCharacterEngine;
using Il2CppEekEvents;
using Il2CppEekUI;
using MelonLoader;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RumbleParty;

public class RumbleParty : MelonMod
{
    #region dirtyStuff

    static RumbleParty()
    {
        SetOurResolveHandlerAtFront();
    }
    private static Assembly AssemblyResolveEventListener(object sender, ResolveEventArgs args)
    {
        if (args is null) return null!;
        string dllName = args.Name[..args.Name.IndexOf(',')];
        var name = "RumbleParty.Resources.resources" + dllName + ".dll";
        string path = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly()?.Location!)!.Parent!.FullName, "UserLibs", dllName + ".dll");
        foreach (var field in typeof(Properties.Resources).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {

            if (field.Name == dllName.Replace('.', '_'))
            {
                var context = new AssemblyLoadContext(name, false);
                MelonLogger.Warning($"Loaded {args.Name} from our embedded resources, saving to userlibs for next time");
                File.WriteAllBytes(path, (byte[])field.GetValue(null)!);
                Stream s = File.OpenRead(path);
                var asm = context.LoadFromStream(s);
                s.Close();
                return asm;
            }
        }
        return null!;
    }
    private static void SetOurResolveHandlerAtFront()
    {
        BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        FieldInfo? field = null;

        Type domainType = typeof(AssemblyLoadContext);

        while (field is null)
        {
            if (domainType is not null)
            {
                field = domainType.GetField("AssemblyResolve", flags);
            }
            else
            {
                MelonLogger.Error("domainType got set to null for the AssemblyResolve event was null");
                return;
            }
            if (field is null)
                domainType = domainType.BaseType!;
        }

        MulticastDelegate resolveDelegate = (MulticastDelegate)field.GetValue(null)!;
        Delegate[] subscribers = resolveDelegate.GetInvocationList();

        Delegate currentDelegate = resolveDelegate;
        for (int i = 0; i < subscribers.Length; i++)
            currentDelegate = Delegate.RemoveAll(currentDelegate, subscribers[i])!;

        Delegate[] newSubscriptions = new Delegate[subscribers.Length + 1];
        newSubscriptions[0] = (ResolveEventHandler)AssemblyResolveEventListener!;
        Array.Copy(subscribers, 0, newSubscriptions, 1, subscribers.Length);

        currentDelegate = Delegate.Combine(newSubscriptions)!;

        field.SetValue(null, currentDelegate);
    }
    #endregion

    // set 0 to disable
    private AudioSource? CutsceneSource;
    private bool cutsceneBoost = false;
    private bool inGameMain = false;
    private bool messageAcknowledged = false;
    private bool rumbleStopped = false;
    private bool rumbleUpdating = false;
    private bool sexBoost = false;
    private bool dialgoueBoost = false;
    private ButtplugClient? client;
    private Canvas canvas = null!;
    private Character? partner1;
    private const int QSamples = 64;
    private const int rumbleUpdatems = 100;
    private GameObject CanvasGO;
    private int _baseRumble = 0;
    private int _currentMaxRumble = 50;
    private int _oldbaseRumble = 0;
    private int _oldcurrentMaxRumble = 50;
    private int _rumble = 0;
    private int inventoryCount = 0;
    private int oldbaseRumble = 0;
    private int oldRumble = 0;
    private int secondCounter = 0;
    private int valueAverageIterator = 0;
    private MelonPreferences_Entry<bool> intifaceDebug = new();
    private MelonPreferences_Entry<bool> onlyIncreaseBaseRumble = new();
    private MelonPreferences_Entry<bool> rotateClockwise = new();
    private MelonPreferences_Entry<bool> useDynamicCutsceneBoost = new();
    private MelonPreferences_Entry<bool> useDynamicDialogue = new();
    private MelonPreferences_Entry<bool> useDynamicSexBoost = new();
    private MelonPreferences_Entry<float> blowJobValue = new();
    private MelonPreferences_Entry<float> cowGirlValue = new();
    private MelonPreferences_Entry<float> cunnilingusValue = new();
    private MelonPreferences_Entry<float> dialogueRumbleScaler = new();
    private MelonPreferences_Entry<float> dynamicCutsceneRumbleScaler = new();
    private MelonPreferences_Entry<float> dynamicDialogueRumbleScaler = new();
    private MelonPreferences_Entry<float> defaultRumbleTime = new();
    private MelonPreferences_Entry<float> doggieStyleValue = new();
    private MelonPreferences_Entry<float> dynamicSexBaseRumbleScaler = new();
    private MelonPreferences_Entry<float> fingeringValue = new();
    private MelonPreferences_Entry<float> hotTubHandjobValue = new();
    private MelonPreferences_Entry<float> linearDivisor = new();
    private MelonPreferences_Entry<float> linearMovementTimeBase = new();
    private MelonPreferences_Entry<float> linearMovementTimeScaler = new();
    private MelonPreferences_Entry<float> masturbatingValue = new();
    private MelonPreferences_Entry<float> maxRumbleTime = new();
    private MelonPreferences_Entry<float> missionaryValue = new();
    private MelonPreferences_Entry<float> oscillatorDivisor = new();
    private MelonPreferences_Entry<float> rotateSpeedScaler = new();
    private MelonPreferences_Entry<float> rotatorDivisor = new();
    private MelonPreferences_Entry<float> scissoringValue = new();
    private MelonPreferences_Entry<float> sixtyNineValue = new();
    private MelonPreferences_Entry<float> vibrationDivisor = new();
    private MelonPreferences_Entry<float> wallSex2Value = new();
    private MelonPreferences_Entry<float> wallSex3Value = new();
    private MelonPreferences_Entry<float> wallSexValue = new();
    private MelonPreferences_Entry<int> baseRumbleDecayScaler = new();
    private MelonPreferences_Entry<int> maxRumble = new();
    private MelonPreferences_Entry<int> scanCount = new();
    private MelonPreferences_Entry<int> staticCutsceneRumble = new();
    private MelonPreferences_Entry<int> staticSexRumble = new();
    private MelonPreferences_Entry<string> intifaceArguments = new();
    private MelonPreferences_Entry<string> intifacePath = new();
    private PlayerCharacter? player = null;
    private readonly float[] _spectrum = new float[QSamples];
    private readonly float[] valueAverage = new float[20];
    private readonly Timer rumbleUpdate;
    private Salsa3D salsa;
    private SexualActs currentAct = SexualActs.None;
    private Task buttConnection = Task.FromResult(-1);
    private Text text = null!;
    private Vector3 lastPosition = Vector3.zero;

    public RumbleParty()
    {
        rumbleUpdate = new Timer(UpdateRumble, null, 0, rumbleUpdatems);
    }

    private int BaseRumble { get => _baseRumble; set => _baseRumble = Math.Clamp(value, 0, CurrentMaxRumble); }
    private int CurrentMaxRumble { get => _currentMaxRumble; set => _currentMaxRumble = Math.Clamp(value, 0, maxRumble.Value); }
    private int Rumble { get => _rumble; set => _rumble = Math.Clamp(value, 0, CurrentMaxRumble); }
    private float RumbleBurstTime { get => defaultRumbleTime.Value; set => defaultRumbleTime.Value = Math.Clamp(value, 0, maxRumbleTime.Value == 0.0f ? float.MaxValue : maxRumbleTime.Value); }

    public override void OnDeinitializeMelon()
    {
        client?.DisconnectAsync();
        client?.Dispose();
        buttConnection.Dispose();
    }

    public override void OnInitializeMelon()
    {
        var preferences = MelonPreferences.CreateCategory("Rumblemod");

        //todo add missing entries

        baseRumbleDecayScaler = preferences.CreateEntry("baseRumbleDecayScaler", 5, "How fast to decrease base rumble", "set to 0 to disable, is in factors of " + rumbleUpdatems.ToString() + "ms per value, so 5 -> " + (5 * rumbleUpdatems / 1000.0f).ToString() + "s for a decrease of 1%");
        defaultRumbleTime = preferences.CreateEntry("defaultRumbleTime", 1.0f, "starting rumble burst time", "in seconds, set the duration of each rumble burst.");
        dialogueRumbleScaler = preferences.CreateEntry("dialogueRumbleScaler", 5.0f, "base rumble modifier", "current dialogue partners love * this is the base rumble in 0-100");
        dynamicCutsceneRumbleScaler = preferences.CreateEntry("dynamicCutsceneRumbleScaler", 500.0f, "Dynamic cutscene volume rumble scale", "Multiplied with the volume from the game during cutscenes");
        dynamicDialogueRumbleScaler = preferences.CreateEntry("dynamicDialogueRumbleScaler", 500.0f, "Dynamic dialogue volume rumble scale", "Multiplied with the volume of the character you're talking to");
        dynamicSexBaseRumbleScaler = preferences.CreateEntry("dynamicSexBaseRumbleScaler", 0.3f, "Base rumble scale during dynamic sex", "Multiplied with the orgasm value(0-100) for the base rumble");
        intifaceArguments = preferences.CreateEntry("intifaceArguments", "--use-bluetooth-le", "what protocols to use for device discovery", "Just combine these as you want: \r\n --use-bluetooth-le\tUse the Bluetooth LE Buttplug Device Communication Manager\r\n --use-serial\tUse the Serial Port Buttplug Device Communication Manager\r\n --use-hid\tUse the HID Buttplug Device Communication Manager\r\n --use-lovense-dongle\tUse the HID Lovense Dongle Buttplug Device Communication Manager\r\n --use-xinput\tUse the XInput Buttplug Device Communication Manager\r\n --use-lovense-connect\tUse the Lovense Connect Buttplug Device Communication Manager\r\n --use-device-websocket-server\tUse the Device Websocket Server Buttplug Device Communication Manager\r\n --device-websocket-server-port\tPort for the device websocket server");
        intifaceDebug = preferences.CreateEntry("intifaceDebug", false, "show internal intiface engine output", "when true shows the internal intiface-engine output in the melonloader console");
        intifacePath = preferences.CreateEntry("intifaceEngine", "internal", "Path to Intiface Engine", "The path to the intiface Engine. Either the path or empty/none/internal for the embedded engine inside the mod. should look something like this: \"ws://localhost:12345/buttplug\"");
        linearDivisor = preferences.CreateEntry("linearDivisor", 100.0f, "linear actuator divisor", "should scale 0-100 to a value 0-1.0, can be made larger than 100 to actuate less");
        linearMovementTimeBase = preferences.CreateEntry("linearMovementTimeBase", 10.0f, "Time for the linear actuator to move", "how long the actuator should take for its movement. make smaller for faster thrusts");
        linearMovementTimeScaler = preferences.CreateEntry("linearMovementTimeScaler", 5.0f, "Multiplier from the burst time", "the larger the faster it is, based on rumblebursttime");
        maxRumble = preferences.CreateEntry("maxRumble", 100, "Maximum rumble you want", "0 to 100%, mostly useful for limiting fucking machines or other such devices");
        maxRumbleTime = preferences.CreateEntry("maxRumbleTime", 0.0f, "Maximum continous rumble time", "in seconds, set a maximum limit for continous rumble or 0.0 for no max rumble time.");
        onlyIncreaseBaseRumble = preferences.CreateEntry("onlyIncreaseBaseRumble", false, "allow decreasing the base Rumble", "true: BaseRumble gets set on each character you talk to. false: it only increases, but never decreases");
        oscillatorDivisor = preferences.CreateEntry("oscillatorDivisor", 100.0f, "oscillation actuator divisor", "should scale 0-100 to a value 0-1.0, can be made larger than 100 to actuate less");
        rotateClockwise = preferences.CreateEntry("rotateClockwise", true, "Rotation direction", "true for clockwise rotation, false for counterclockwise. only need on devices with rotating parts");
        rotateSpeedScaler = preferences.CreateEntry("rotateSpeedScaler", 5.0f, "Multiplier from the burst time", "the larger the faster it rotates, based on rumblebursttime");
        rotatorDivisor = preferences.CreateEntry("rotatorDivisor", 100.0f, "rotation actuator divisor", "should scale 0-100 to a value 0-1.0, can be made larger than 100 to actuate less");
        scanCount = preferences.CreateEntry("scanCount", 1, "Max device count", "how many devices to search for, set to 0 for infinite");
        staticCutsceneRumble = preferences.CreateEntry("staticCutsceneRumble", 40, "Rumble for cutscenes when dynamic disabled", "value between 0 and 100, gets added to the base rumble");
        staticSexRumble = preferences.CreateEntry("staticSexRumble", 40, "Rumble for sex when dynamic disabled", "value between 0 and 100, gets added to the base rumble");
        useDynamicCutsceneBoost = preferences.CreateEntry("useDynamicCutsceneBoost", true, "Rumble based on audio/movement", "true: rumble is based on the action in a cutscene. false: rumble is static");
        useDynamicDialogue = preferences.CreateEntry("useDynamicDialogue", true, "Rumble based on characters talking", "true: rumble is based on the characters voice your talking to. false: off");
        useDynamicSexBoost = preferences.CreateEntry("useDynamicSexBoost", true, "Rumble based on movement", "true: rumble is based on movement during sex. false: rumble is static during sex");
        vibrationDivisor = preferences.CreateEntry("vibrationDivisor", 100.0f, "vibration actuator divisor", "should scale 0-100 to a value 0-1.0, can be made larger than 100 to actuate less");
        blowJobValue = preferences.CreateEntry("blowJobValue", 18000.0f, "Multiplier for blow job intensity", "the larger the more intense. default 18000");
        cowGirlValue = preferences.CreateEntry("cowGirlValue", 8000.0f, "Multiplier for cowgirl intensity", "the larger the more intense. default 18000");
        cunnilingusValue = preferences.CreateEntry("cunnilingusValue", 1.0f, "Multiplier for cunnilingus intensity", "the larger the more intense. default 18000");
        doggieStyleValue = preferences.CreateEntry("doggieStyleValue", 10000.0f, "Multiplier for fingering intensity", "the larger the more intense. default 18000");
        fingeringValue = preferences.CreateEntry("fingeringValue", 5000.0f, "Multiplier for doggiestyle intensity", "the larger the more intense. default 18000");
        hotTubHandjobValue = preferences.CreateEntry("hotTubHandjobValue", 18000.0f, "Multiplier for hot tub handjob intensity", "the larger the more intense. default 18000");
        masturbatingValue = preferences.CreateEntry("masturbatingValue", 18000.0f, "Multiplier for masturbating intensity", "the larger the more intense. default 18000");
        missionaryValue = preferences.CreateEntry("missionaryValue", 20000.0f, "Multiplier for missionary intensity", "the larger the more intense. default 18000");
        scissoringValue = preferences.CreateEntry("scissoringValue", 20000.0f, "Multiplier for scissoring intensity", "the larger the more intense. default 18000");
        sixtyNineValue = preferences.CreateEntry("sixtyNineValue", 18000.0f, "Multiplier for sixty nine intensity", "the larger the more intense. default 18000");
        wallSexValue = preferences.CreateEntry("wallSexValue", 15000.0f, "Multiplier for wallsex 1 intensity", "the larger the more intense. default 18000");
        wallSex2Value = preferences.CreateEntry("wallSex2Value", 20000.0f, "Multiplier for wallsex 2 intensity", "the larger the more intense. default 18000");
        wallSex3Value = preferences.CreateEntry("wallSex3Value", 12000.0f, "Multiplier for wallsex 3 intensity", "the larger the more intense. default 18000");

        MelonLogger.Msg($"[RumbleParty] Initializing RumbleParty with {intifacePath.Value} {intifaceArguments.Value}");
        buttConnection = Task.Run(InitializeButtConnections).ContinueWith((Task t) => { if (t.Exception != null) MelonLogger.Error(t.Exception.Message); });
        MelonLogger.Msg("[RumbleParty] Connection completed");
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (sceneName == "MainMenu")
        {
            // Canvas
            CanvasGO = new()
            {
                name = "Rumbler UI",
                active = true,
                layer = LayerMask.NameToLayer("UI")
            };
            canvas = CanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = CanvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            CanvasGO.AddComponent<GraphicRaycaster>();
            canvas.scaleFactor = 1.0f;

            _ = UIBuilder.CreatePanel("Rumbler UI Container", CanvasGO, new(0.3f, 0.1f), new(Screen.width * 0.5f, Screen.height * 0.5f), out var contentHolder);
            text = UIBuilder.CreateLabel(contentHolder, "Rumbler warning text", "", TextAnchor.MiddleCenter, Color.red);
            text.fontSize = 40;
            text.text = "TO STOP THE RUMBLE AT ANY TIME HIT ALT + X\n(also hides this text)";
        }
        else
        {
            inGameMain = sceneName == "GameMain";
            if (inGameMain)
            {
                rumbleUpdating = true;
                player = UnityEngine.Object.FindObjectOfType<PlayerCharacter>();
            }
        }
    }

    public override void OnUpdate()
    {
        if (client?.Devices.Length == 0) return;

        //rumble stop code
        if (Keyboard.current.altKey.isPressed && Keyboard.current.xKey.wasPressedThisFrame)
        {
            ToggleRumble();
            foreach (var item in CharacterManager._characters.CharacterList)
            {
                MelonLogger.Msg(item.Name);
            }
        }

        //we have at least one device, lets go rumble :D
        if (!inGameMain) return;
        player ??= UnityEngine.Object.FindObjectOfType<PlayerCharacter>();
        if (player is null) return;

        rumbleUpdating = !GameManager.Singleton.IsPaused;

        //love value based base rumble
        if (DialogueUI.Singleton.IsShowing && !cutsceneBoost && !sexBoost)
        {
            //dialogue rumble based on voice
            if (onlyIncreaseBaseRumble.Value)
            {
                if (BaseRumble < (int)(DialogueUI.Singleton.currentLoveValue * dialogueRumbleScaler.Value))
                {
                    BaseRumble = Math.Max((int)(DialogueUI.Singleton.currentLoveValue * dialogueRumbleScaler.Value), BaseRumble);
                }
            }
            else
            {
                if (BaseRumble != (int)(DialogueUI.Singleton.currentLoveValue * dialogueRumbleScaler.Value))
                {
                    BaseRumble = Math.Max((int)(DialogueUI.Singleton.currentLoveValue * dialogueRumbleScaler.Value), BaseRumble);
                }
            }

            if (useDynamicDialogue.Value && !dialgoueBoost)
            {
                dialgoueBoost = true;
                salsa = GameObject.Find(DialogueUI.Singleton.nameText.m_Text).GetComponentInChildren<Salsa3D>();
            }

            if (dialgoueBoost)
            {
                float value = salsa.sample.Sum() * dynamicDialogueRumbleScaler.Value;
                value += value * 1.5f * Il2Cpp.AudioSettings.Singleton.CurrentAudioVolume / -40;
                Rumble = (int)value;
            }

            if (intifaceDebug.Value) MelonLogger.Msg($"set base rumble to {BaseRumble + Rumble} due to love status");
        }
        else
        {
            if (useDynamicDialogue.Value && dialgoueBoost)
            {
                Rumble = 0;
                dialgoueBoost = false;
            }
        }

        if (inventoryCount != CharacterManager.Singleton.PlayerInventory.Count)
        {
            inventoryCount = CharacterManager.Singleton.PlayerInventory.Count;
            _ = Task.Run(RumbleBurst);
            if (intifaceDebug.Value) MelonLogger.Msg($"started a burst with {CurrentMaxRumble} for {RumbleBurstTime} seconds because of an inventory change");
        }

        if (player.Intimacy.CurrentSexualActivity != SexualActs.None && player.Intimacy.CurrentSexualActivity != SexualActs.InSexCutScene)
        {
            if (!sexBoost)
            {
                sexBoost = true;
                partner1 = player.Intimacy.CurrentSexPartner;
                oldRumble = Rumble;
                oldbaseRumble = BaseRumble;
            }
            if (currentAct != player.Intimacy.CurrentSexualActivity)
            {
                currentAct = player.Intimacy.CurrentSexualActivity;
                //MelonLogger.Msg(player.Intimacy.CurrentSexualRole);
            }

            if (useDynamicSexBoost.Value)
                Rumble = RumbleValueForAct(player.Intimacy.CurrentSexualActivity);
            else
                Rumble = staticSexRumble.Value;
        }
        else if (sexBoost)
        {
            Rumble = oldRumble;
            BaseRumble = oldbaseRumble;
            sexBoost = false;
            partner1 = null;
            currentAct = SexualActs.None;
        }

        //do vibrations in cutscene baded on hip velocity of either partner or on sound, whatever is higher
        if (CutSceneManager.IsAnySexCutScenePlaying())
        {
            if (!cutsceneBoost)
            {
                oldRumble = Rumble;
                oldbaseRumble = BaseRumble;
                cutsceneBoost = true;

                foreach (var item in CutSceneManager.CurrentPlayerScene.gameObject.GetComponentsInChildren<AudioSource>())
                {
                    if (item.gameObject.name == "CutsceneMusic_AudioSource")
                    {
                        CutsceneSource = item;
                        break;
                    }
                }

                //MelonLogger.Msg(CutsceneSource?.time.ToString() ?? "no cutscene music audiosource found");
                //CutsceneMusic_AudioSource
            }

            if (useDynamicCutsceneBoost.Value)
                Rumble = AnalyzeAudioLoudness();
            else
                Rumble = staticCutsceneRumble.Value;
        }
        else if (cutsceneBoost)
        {
            Rumble = oldRumble;
            BaseRumble = oldbaseRumble;
            cutsceneBoost = false;
        }
    }

    private int AnalyzeAudioLoudness()
    {
        //todo fix
        if (CutsceneSource is null) return 0;

        AudioListener.GetSpectrumData(_spectrum, 0, FFTWindow.Blackman);

        MelonLogger.Msg("spectrum: " + _spectrum.Sum() / QSamples);
        return (int)(_spectrum.Sum() * dynamicCutsceneRumbleScaler.Value);
    }

    private async Task InitializeButtConnections()
    {
        IButtplugClientConnector connector;
        if (string.IsNullOrWhiteSpace(intifacePath.Value) || intifacePath.Value == "none" || intifacePath.Value == "internal")
        {
            //start intiface engine ourselves
            string intifaceExePath = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location!)!.FullName, "intiface-engine");
            string intifaceExeFilePath = Path.Combine(intifaceExePath, "intiface-engine.exe");
            if (!Directory.Exists(intifaceExePath) || !File.Exists(intifaceExeFilePath))
            {
                MelonLogger.Msg("extracting internal intiface-engine to " + intifaceExeFilePath);
                Directory.CreateDirectory(intifaceExePath);
                File.WriteAllBytes(intifaceExeFilePath, Properties.Resources.intiface_engine);
            }

            MelonLogger.Msg("Starting internal intiface-engine");
            var app = Process.Start(new ProcessStartInfo(intifaceExeFilePath, "--websocket-port 12345 " + intifaceArguments.Value) { RedirectStandardOutput = true, RedirectStandardError = true })!;
            bool started = false;

            if (intifaceDebug.Value)
            {
                MelonLogger.Msg("intiface debug enabled");
                app.OutputDataReceived += (object _, DataReceivedEventArgs args) =>
                {
                    MelonLogger.Msg(args.Data);
                    if (args.Data?.Contains("intiface_engine::engine\u001b[0m\u001b[2m:\u001b[0m Starting server", StringComparison.InvariantCultureIgnoreCase) ?? false) started = true;
                };
            }
            else
            {
                MelonLogger.Msg("intiface debug disabled");
                app.OutputDataReceived += (object _, DataReceivedEventArgs args) =>
                {
                    if (args.Data?.Contains("intiface_engine::engine\u001b[0m\u001b[2m:\u001b[0m Starting server", StringComparison.InvariantCultureIgnoreCase) ?? false) started = true;
                };
            }
            app.ErrorDataReceived += (object _, DataReceivedEventArgs args) => MelonLogger.Error(args.Data);
            app.BeginOutputReadLine();
            app.BeginErrorReadLine();

            MelonLogger.Msg("waiting on iniface-server to start");
            while (!started) { } //wait on server to start
            MelonLogger.Msg("internal intiface-server started");
            connector = new ButtplugWebsocketConnector(new Uri("ws://localhost:12345/buttplug"));
        }
        else
        {
            MelonLogger.Msg($"Connecting to intiface-engine/app at {intifacePath.Value}");
            //use configured intiface engine
            connector = new ButtplugWebsocketConnector(new Uri(intifacePath.Value));
        }

        MelonLogger.Msg("Creating buttplug client");
        client = new ButtplugClient("House Party");
        client.DeviceAdded += OnDeviceAdded!;
        client.DeviceRemoved += OnDeviceRemoved!;

        try
        {
            MelonLogger.Msg("Connecting to intiface...");
            await client.ConnectAsync(connector);
        }
        catch (ButtplugClientConnectorException ex)
        {
            MelonLogger.Error(
                "Can't connect to Buttplug Server, exiting!" +
                $"Message: {ex.InnerException!.Message}");
        }
        catch (ButtplugHandshakeException ex)
        {
            MelonLogger.Error(
                "Couldn't shake hands with the Buttplug Server, exiting!" +
                $"Message: {ex.InnerException!.Message}");
        }

        try
        {
            MelonLogger.Msg("[RumbleParty] Connected to the intiface");
            MelonLogger.Msg("[RumbleParty] Started scanning");
            _ = client.StartScanningAsync();
        }
        catch (ButtplugException ex)
        {
            MelonLogger.Error(
                $"Scanning failed: {ex.InnerException!.Message}");
        }
    }

    private void OnDeviceAdded(object _, DeviceAddedEventArgs args)
    {
        MelonLogger.Msg($"[RumbleParty] Device ({client?.Devices.Length}/{scanCount.Value}) {args.Device.Name} connected");
        MelonLogger.Msg($"[RumbleParty]    it has: {args.Device.VibrateAttributes.Count} vibration motors, {args.Device.RotateAttributes.Count} rotation motors, {args.Device.LinearAttributes.Count} linear motors, {args.Device.OscillateAttributes.Count} oscillators");
        if (client?.Devices.Length >= scanCount.Value && scanCount.Value != 0)
        {
            client?.StopScanningAsync();
            MelonLogger.Msg($"[RumbleParty] max device count of {scanCount.Value} reached, stopping scan.");
        }
    }

    private void OnDeviceRemoved(object _, DeviceRemovedEventArgs args)
    {
        MelonLogger.Msg($"[RumbleParty] Device {args.Device.Name} disconnected");
        if (client?.Devices.Length < scanCount.Value)
        {
            client?.StartScanningAsync();
            MelonLogger.Msg($"[RumbleParty] max device count of {scanCount.Value} no longer met, starting scan.");
        }
    }

    private void RumbleBurst()
    {
        if (client?.Devices.Length == 0 || !rumbleUpdating) return;
        int rumble = CurrentMaxRumble;

        UpdateDeviceActuators(rumble);

        Thread.Sleep((int)RumbleBurstTime * 1000);
        UpdateRumble(null);
    }

    //todo check for female, but should be the same
    private int RumbleValueForAct(SexualActs act)
    {
        if (player is null) return 0;
        partner1 = player.Intimacy.CurrentSexPartner;
        float value = 10;
        switch (act)
        {
            case SexualActs.Masturbating:
            {
                if (player.Gender == Genders.Male)
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.lHand.position)
                    {
                        value = (player.lHand.position - lastPosition).magnitude * masturbatingValue.Value * (2.0f / 3.0f);
                        lastPosition = player.lHand.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.lHand.position)
                    {
                        value = (player.lHand.position - lastPosition).magnitude * masturbatingValue.Value;
                        lastPosition = player.lHand.position;
                    }
                }
                break;
            }
            case SexualActs.BlowJob:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Primary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.rHand.position)
                    {
                        value = (partner1.rHand.position - lastPosition).magnitude;
                        lastPosition = partner1.rHand.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.rHand.position)
                    {
                        value = (player.rHand.position - lastPosition).magnitude;
                        lastPosition = player.rHand.position;
                    }
                }
                value *= blowJobValue.Value;
                break;
            case SexualActs.MissionarySex:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Primary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.PuppetHip.position)
                    {
                        value = (partner1.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = partner1.PuppetHip.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.PuppetHip.position)
                    {
                        value = (player.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = player.PuppetHip.position;
                    }
                }
                value *= missionaryValue.Value;
                break;
            case SexualActs.DoggieStyleSex:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Primary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.PuppetHip.position)
                    {
                        value = (partner1.PuppetHip.position - lastPosition).magnitude * 1.5f;
                        lastPosition = partner1.PuppetHip.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.PuppetHip.position)
                    {
                        value = (player.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = player.PuppetHip.position;
                    }
                }
                value *= doggieStyleValue.Value;
                break;
            case SexualActs.Cowgirl:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Secondary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.PuppetHip.position)
                    {
                        value = (partner1.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = partner1.PuppetHip.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.PuppetHip.position)
                    {
                        value = (player.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = player.PuppetHip.position;
                    }
                }
                value *= cowGirlValue.Value;
                break;
            case SexualActs.Cunnilingus:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Primary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.Head.rotation.eulerAngles)
                    {
                        value = (partner1.Head.rotation.eulerAngles - lastPosition).magnitude;
                        lastPosition = partner1.Head.rotation.eulerAngles;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != Camera.main.transform.rotation.eulerAngles)
                    {
                        value = (Camera.main.transform.rotation.eulerAngles - lastPosition).magnitude;
                        lastPosition = Camera.main.transform.rotation.eulerAngles;
                    }
                }
                value *= cunnilingusValue.Value;
                break;
            case SexualActs.HotTubHandJob:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Primary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.rHand.position)
                    {
                        value = (partner1.rHand.position - lastPosition).magnitude;
                        lastPosition = partner1.rHand.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.rHand.position)
                    {
                        value = (player.rHand.position - lastPosition).magnitude;
                        lastPosition = player.rHand.position;
                    }
                }
                value *= hotTubHandjobValue.Value;
                break;
            case SexualActs.SixtyNine:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Secondary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.rHand.position)
                    {
                        value = (partner1.rHand.position - lastPosition).magnitude;
                        lastPosition = partner1.rHand.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.Head.position)
                    {
                        value = (player.rHand.position - lastPosition).magnitude;
                        lastPosition = player.rHand.position;
                    }
                }
                value *= sixtyNineValue.Value;
                break;
            case SexualActs.WallSex:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Primary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.PuppetHip.position)
                    {
                        value = (partner1.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = partner1.PuppetHip.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.PuppetHip.position)
                    {
                        value = (player.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = player.PuppetHip.position;
                    }
                }
                value *= wallSexValue.Value;
                break;
            case SexualActs.WallSex2:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Primary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.PuppetHip.position)
                    {
                        value = (partner1.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = partner1.PuppetHip.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.PuppetHip.position)
                    {
                        value = (player.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = player.PuppetHip.position;
                    }
                }
                value *= wallSex2Value.Value;
                break;
            case SexualActs.WallSex3:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Secondary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.PuppetHip.position)
                    {
                        value = (partner1.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = partner1.PuppetHip.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.PuppetHip.position)
                    {
                        value = (player.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = player.PuppetHip.position;
                    }
                }
                value *= wallSex3Value.Value;
                break;
            case SexualActs.Fingering:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Primary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.rHand.position)
                    {
                        value = (partner1.rHand.position - lastPosition).magnitude;
                        lastPosition = partner1.rHand.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.rHand.position)
                    {
                        value = (player.rHand.position - lastPosition).magnitude;
                        lastPosition = player.rHand.position;
                    }
                }
                value *= fingeringValue.Value;
                break;
            case SexualActs.Scissoring:
                if (player.Intimacy.CurrentSexualRole == SexualRoles.Primary && partner1 is not null)
                {
                    if (lastPosition == Vector3.zero || lastPosition != partner1.PuppetHip.position)
                    {
                        value = (partner1.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = partner1.PuppetHip.position;
                    }
                }
                else
                {
                    if (lastPosition == Vector3.zero || lastPosition != player.PuppetHip.position)
                    {
                        value = (player.PuppetHip.position - lastPosition).magnitude;
                        lastPosition = player.PuppetHip.position;
                    }
                }
                value *= scissoringValue.Value;
                break;
        }
        valueAverage[valueAverageIterator] = value;
        if (valueAverageIterator < 19) valueAverageIterator++;
        else valueAverageIterator = 0;
        //if (intifaceDebug.Value) MelonLogger.Msg(valueAverage.Sum() / 20);
        BaseRumble = (int)(player.Intimacy.Orgasm / dynamicSexBaseRumbleScaler.Value);
        return (int)valueAverage.Sum() / 20;
    }
    private void ToggleRumble()
    {
        if (!messageAcknowledged)
        {
            //CanvasGO.active = false;
            //text.text = "";
            messageAcknowledged = true;
        }
        else if (!rumbleStopped)
        {
            MelonLogger.Warning("Rumble stopped");
            _oldbaseRumble = BaseRumble;
            _oldcurrentMaxRumble = CurrentMaxRumble;
            CurrentMaxRumble = 0;
            BaseRumble = 0;
            rumbleStopped = true;
        }
        else
        {
            MelonLogger.Msg("Rumble resumed");
            CurrentMaxRumble = _oldcurrentMaxRumble;
            BaseRumble = _oldbaseRumble;
            rumbleStopped = false;
        }
    }

    private void UpdateRumble(object? _)
    {
        if (client?.Devices.Length == 0 || !rumbleUpdating) return;
        int rumble = Math.Clamp(Rumble + BaseRumble, 0, 100);

        UpdateDeviceActuators(rumble);

        secondCounter++;
        if (secondCounter >= baseRumbleDecayScaler.Value && baseRumbleDecayScaler.Value != 0)
        {
            BaseRumble--;
            secondCounter = 0;
        }
    }

    private void UpdateDeviceActuators(int rumble)
    {
        foreach (var device in client?.Devices ?? Array.Empty<ButtplugClientDevice>())
        {
            List<double> speeds = new();
            for (int i = 0; i < device.VibrateAttributes.Count; i++)
            {
                speeds.Add((double)rumble / vibrationDivisor.Value);
            }
            device.VibrateAsync(speeds);
            speeds.Clear();

            if (device.LinearAttributes.Count > 0)
            {
                device.LinearAsync((uint)Math.Clamp((int)(linearMovementTimeBase.Value - (RumbleBurstTime * linearMovementTimeScaler.Value)), 0, linearMovementTimeBase.Value), (double)rumble / linearDivisor.Value);
                device.LinearAsync((uint)Math.Clamp((int)(linearMovementTimeBase.Value - (RumbleBurstTime * linearMovementTimeScaler.Value)), 0, linearMovementTimeBase.Value), 0);
            }

            for (int i = 0; i < device.OscillateAttributes.Count; i++)
            {
                speeds.Add((double)rumble / oscillatorDivisor.Value);
            }
            device.OscillateAsync(speeds);
            speeds.Clear();

            if (device.RotateAttributes.Count > 0)
                device.RotateAsync((double)rumble / rotatorDivisor.Value * rotateSpeedScaler.Value, rotateClockwise.Value);
        }
    }
}

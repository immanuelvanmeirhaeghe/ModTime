using ModManager;
using ModTime.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModTime
{
	public class ModTime : MonoBehaviour
	{
		private static ModTime Instance;

		private static readonly string ModName = "ModTime";

		private static readonly float ModScreenTotalWidth = 500f;

		private static readonly float ModScreenTotalHeight = 150f;

		private static readonly float ModScreenMinWidth = 450f;

		private static readonly float ModScreenMaxWidth = 550f;

		private static readonly float ModScreenMinHeight = 50f;

		private static readonly float ModScreenMaxHeight = 200f;

		private bool ShowUI;

		public static Rect ModTimeScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

		private static Player LocalPlayer;

		private static HUDManager LocalHUDManager;

		private static float ModScreenStartPositionX { get; set; } = ((float)Screen.width - ModScreenMaxWidth) % ModScreenTotalWidth;

		private static float ModScreenStartPositionY { get; set; } = ((float)Screen.height - ModScreenMaxHeight) % ModScreenTotalHeight;

		public bool RainEnabled { get; private set; } = false;

		private static bool IsMinimized { get; set; } = false;

		private Color DefaultGuiColor = GUI.color;

		public static string DayTimeScaleInMinutes { get; set; } = "20";


		public static string NightTimeScaleInMinutes { get; set; } = "10";


		public static string InGameDay { get; set; } = MainLevel.Instance.m_TODSky.Cycle.Day.ToString();


		public static string InGameMonth { get; set; } = MainLevel.Instance.m_TODSky.Cycle.Month.ToString();


		public static string InGameYear { get; set; } = MainLevel.Instance.m_TODSky.Cycle.Year.ToString();


		public static string InGameHour { get; set; } = MainLevel.Instance.m_TODSky.Cycle.Hour.ToString();


		public bool IsModActiveForMultiplayer { get; private set; }

		public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

		public ModTime()
		{
            useGUILayout = true;
			Instance = this;
		}

		public static ModTime Get()
		{
			return Instance;
		}

		public static string OnlyForSinglePlayerOrHostMessage()
		{
			return "Only available for single player or when host. Host can activate using ModManager.";
		}

		public static string DayTimeSetMessage(string day, string month, string year, string hour)
		{
			return "Date time set:\nToday is " + day + "/" + month + "/" + year + "\nat " + hour + " o'clock.";
		}

		public static string TimeScalesSetMessage(string dayTimeScale, string nightTimeScale)
		{
			return "Time scales set:\nDay time passes in " + dayTimeScale + " realtime minutes\nand night time in " + nightTimeScale + " realtime minutes.";
		}

		public static string PermissionChangedMessage(string permission, string reason) => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
		public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
			=> $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

		public void Start()
		{
			ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
			ModKeybindingId = GetConfigurableKey();
		}

		private void HandleException(Exception exc, string methodName)
		{
			string info = $"[{ModName}:{methodName}] throws exception:\n{exc.Message}";
			ModAPI.Log.Write(info);
			ShowHUDBigInfo(HUDBigInfoMessage(info, MessageType.Error, Color.red));
		}

		private void ModManager_onPermissionValueChanged(bool optionValue)
		{
			string reason = optionValue ? "the game host allowed usage" : "the game host did not allow usage";
			IsModActiveForMultiplayer = optionValue;

			ShowHUDBigInfo(
						  (optionValue ?
							HUDBigInfoMessage(PermissionChangedMessage($"granted", $"{reason}"), MessageType.Info, Color.green)
							: HUDBigInfoMessage(PermissionChangedMessage($"revoked", $"{reason}"), MessageType.Info, Color.yellow))
							);
		}

		public void ShowHUDBigInfo(string text)
		{
			string header = ModName + " Info";
			string textureName = HUDInfoLogTextureType.Reputation.ToString();
			HUDBigInfo obj = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
			HUDBigInfoData.s_Duration = 2f;
			HUDBigInfoData data = new HUDBigInfoData
			{
				m_Header = header,
				m_Text = text,
				m_TextureName = textureName,
				m_ShowTime = Time.time
			};
			obj.AddInfo(data);
			obj.Show(show: true);
		}

		public void ShowHUDInfoLog(string itemID, string localizedTextKey)
		{
			Localization localization = GreenHellGame.Instance.GetLocalization();
			((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
		}

		private void EnableCursor(bool blockPlayer = false)
		{
			CursorManager.Get().ShowCursor(blockPlayer);
			if (blockPlayer)
			{
				LocalPlayer.BlockMoves();
				LocalPlayer.BlockRotation();
				LocalPlayer.BlockInspection();
			}
			else
			{
				LocalPlayer.UnblockMoves();
				LocalPlayer.UnblockRotation();
				LocalPlayer.UnblockInspection();
			}
		}

		private void Update()
		{
			if (Input.GetKeyDown(ModKeybindingId))
			{
				if (!ShowUI)
				{
					InitData();
					EnableCursor(blockPlayer: true);
				}
				ToggleShowUI();
				if (!ShowUI)
				{
					EnableCursor();
				}
			}
		}

		private void ToggleShowUI()
		{
			ShowUI = !ShowUI;
		}

		private void OnGUI()
		{
			if (ShowUI)
			{
				InitData();
				InitSkinUI();
				InitWindow();
			}
		}

		private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
		private static KeyCode ModKeybindingId { get; set; } = KeyCode.Pause;
		private KeyCode GetConfigurableKey()
		{
			KeyCode configuredKeyCode = default;
			string configuredKeybinding = string.Empty;

			try
			{
				//ModAPI.Log.Write($"Searching XML runtime configuration file {RuntimeConfigurationFile}...");
				if (File.Exists(RuntimeConfigurationFile))
				{
					using (var xmlReader = XmlReader.Create(new StreamReader(RuntimeConfigurationFile)))
					{
						//ModAPI.Log.Write($"Reading XML runtime configuration file...");
						while (xmlReader.Read())
						{
							//ModAPI.Log.Write($"Searching configuration for Button for Mod with ID = {ModName}...");
							if (xmlReader["ID"] == ModName)
							{
								if (xmlReader.ReadToFollowing(nameof(Button)))
								{
									//ModAPI.Log.Write($"Found configuration for Button for Mod with ID = {ModName}!");
									configuredKeybinding = xmlReader.ReadElementContentAsString();
									//ModAPI.Log.Write($"Configured keybinding = {configuredKeybinding}.");
								}
							}
						}
					}
					//ModAPI.Log.Write($"XML runtime configuration\n{File.ReadAllText(RuntimeConfigurationFile)}\n");
				}

				configuredKeybinding = configuredKeybinding?.Replace("NumPad", "Alpha").Replace("Oem", "");

				configuredKeyCode = !string.IsNullOrEmpty(configuredKeybinding)
															? (KeyCode)Enum.Parse(typeof(KeyCode), configuredKeybinding)
															: ModKeybindingId;
				//ModAPI.Log.Write($"Configured key code: { configuredKeyCode }");
				return configuredKeyCode;
			}
			catch (Exception exc)
			{
				HandleException(exc, nameof(GetConfigurableKey));
				return configuredKeyCode;
			}
		}

		private void InitData()
		{
			LocalHUDManager = HUDManager.Get();
			LocalPlayer = Player.Get();
		}

		private void InitSkinUI()
		{
			GUI.skin = ModAPI.Interface.Skin;
		}

		private void InitWindow()
		{
			ModTimeScreen = GUILayout.Window(GetHashCode(), ModTimeScreen, InitModTimeScreen, ModName, GUI.skin.window, GUILayout.ExpandWidth(expand: true), GUILayout.MinWidth(ModScreenMinWidth), GUILayout.MaxWidth(ModScreenMaxWidth), GUILayout.ExpandHeight(expand: true), GUILayout.MinHeight(ModScreenMinHeight), GUILayout.MaxHeight(ModScreenMaxHeight));
		}

		private void ScreenMenuBox()
		{
			if (GUI.Button(new Rect(ModTimeScreen.width - 40f, 0f, 20f, 20f), "-", GUI.skin.button))
			{
				CollapseWindow();
			}
			if (GUI.Button(new Rect(ModTimeScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
			{
				CloseWindow();
			}
		}

		private void CollapseWindow()
		{
			if (!IsMinimized)
			{
				ModTimeScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenMinHeight);
				IsMinimized = true;
			}
			else
			{
				ModTimeScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
				IsMinimized = false;
			}
			InitWindow();
		}

		private void CloseWindow()
		{
			ShowUI = false;
			EnableCursor();
		}

		private void InitModTimeScreen(int windowID)
		{
			ModScreenStartPositionX = ModTimeScreen.x;
			ModScreenStartPositionY = ModTimeScreen.y;
			using (new GUILayout.VerticalScope(GUI.skin.box))
			{
				ScreenMenuBox();
				if (!IsMinimized)
				{
					ModOptionsBox();
					TimeScalesBox();
					DayTimeBox();
				}
			}
			GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
		}

		private void ModOptionsBox()
		{
			if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
			{
				using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
				{
					StatusForMultiplayer();
					GUI.color = Color.cyan;
					string rainOptionMessage = $"Rain is currently { (RainEnabled ? "enabled" : "disabled") }]";
					GUILayout.Label(rainOptionMessage, GUI.skin.label);
					GUI.color = DefaultGuiColor;
					GUILayout.Label($"To toggle the mod main UI, press [{ModKeybindingId}]", GUI.skin.label);
					bool _toggled = RainEnabled;
					RainEnabled = GUILayout.Toggle(RainEnabled, $"Change the weather?", GUI.skin.toggle);
                    if (_toggled != RainEnabled)
                    {
                        if (RainEnabled)
                        {
							RainManager.Get().ScenarioStartRain();
                        }
                        else
                        {
							RainManager.Get().ScenarioStopRain();
						}
						ShowHUDBigInfo(HUDBigInfoMessage(rainOptionMessage, MessageType.Info, Color.cyan));
                    }
				}
			}
			else
			{
				OnlyForSingleplayerOrWhenHostBox();
			}
		}

		private void OnlyForSingleplayerOrWhenHostBox()
		{
			using (var infoScope = new GUILayout.HorizontalScope(GUI.skin.box))
			{
				GUI.color = Color.yellow;
				GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
			}
		}

		private void StatusForMultiplayer()
		{
			string reason = string.Empty;
			if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
			{
				GUI.color = Color.green;
				if (IsModActiveForSingleplayer)
				{
					reason = "you are the game host";
				}
				if (IsModActiveForMultiplayer)
				{
					reason = "the game host allowed usage";
				}
				GUILayout.Toggle(true, PermissionChangedMessage($"granted", $"{reason}"), GUI.skin.toggle);
			}
			else
			{
				GUI.color = Color.yellow;
				if (!IsModActiveForSingleplayer)
				{
					reason = "you are not the game host";
				}
				if (!IsModActiveForMultiplayer)
				{
					reason = "the game host did not allow usage";
				}
				GUILayout.Toggle(false, PermissionChangedMessage($"revoked", $"{reason}"), GUI.skin.toggle);
			}
		}

		private void DayTimeBox()
		{
			if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
			{
				using (new GUILayout.VerticalScope(GUI.skin.box))
				{
					GUILayout.Label("Set the current date and time in game. Day starts at 5AM. Night starts at 10PM", GUI.skin.label);
					using (new GUILayout.HorizontalScope(GUI.skin.box))
					{
						GUILayout.Label("Day: ", GUI.skin.label);
						InGameDay = GUILayout.TextField(InGameDay, GUI.skin.textField);
						GUILayout.Label("Month: ", GUI.skin.label);
						InGameMonth = GUILayout.TextField(InGameMonth, GUI.skin.textField);
						GUILayout.Label("Year: ", GUI.skin.label);
						InGameYear = GUILayout.TextField(InGameYear, GUI.skin.textField);
						GUILayout.Label("Hour: ", GUI.skin.label);
						InGameHour = GUILayout.TextField(InGameHour, GUI.skin.textField);
						if (GUILayout.Button("Set daytime", GUI.skin.button, GUILayout.MaxWidth(200f)))
						{
							OnClickSetDayTimeButton();
						}
					}
				}
			}
			else
			{
				using (new GUILayout.VerticalScope(GUI.skin.box))
				{
					GUI.color = Color.yellow;
					GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
					GUI.color = Color.white;
				}
			}
		}

		private void TimeScalesBox()
		{
			if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
			{
				using (new GUILayout.VerticalScope(GUI.skin.box))
				{
					GUILayout.Label("Set how many real-time minutes a day or night takes in game. Min. 5 and max. 30. Default scales: Day time: 20 minutes. Night time: 10 minutes.", GUI.skin.label);
					using (new GUILayout.HorizontalScope(GUI.skin.box))
					{
						GUILayout.Label("Day time: ", GUI.skin.label);
						DayTimeScaleInMinutes = GUILayout.TextField(DayTimeScaleInMinutes, GUI.skin.textField);
						GUILayout.Label("Night time: ", GUI.skin.label);
						NightTimeScaleInMinutes = GUILayout.TextField(NightTimeScaleInMinutes, GUI.skin.textField);
					}
					if (GUILayout.Button("Set time scales", GUI.skin.button))
					{
						OnClickSetTimeScalesButton();
					}
				}
			}
			else
			{
				using (new GUILayout.VerticalScope(GUI.skin.box))
				{
					GUI.color = Color.yellow;
					GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
					GUI.color = Color.white;
				}
			}
		}

		private void OnClickSetTimeScalesButton()
		{
			try
			{
				float num = ValidateTimeScale(DayTimeScaleInMinutes);
				float num2 = ValidateTimeScale(NightTimeScaleInMinutes);
				if (num > 0f && num2 > 0f)
				{
					SetTimeScales(num, num2);
				}
			}
			catch (Exception exc)
			{
				HandleException(exc, "OnClickSetTimeScalesButton");
			}
		}

		private void OnClickSetDayTimeButton()
		{
			try
			{
				DateTime dateTime = ValidateDay(InGameDay, InGameMonth, InGameYear, InGameHour);
				if (dateTime != DateTime.MinValue)
				{
					SetDayTime(dateTime.Day, dateTime.Month, dateTime.Year, dateTime.Hour);
				}
			}
			catch (Exception exc)
			{
				HandleException(exc, "OnClickSetDayTimeButton");
			}
		}

		private DateTime ValidateDay(string inGameDay, string inGameMonth, string inGameYear, string inGameHour)
		{
			if (int.TryParse(inGameYear, out var result) && int.TryParse(inGameMonth, out var result2) && int.TryParse(inGameDay, out var result3) && float.TryParse(inGameHour, out var result4))
			{
				if (DateTime.TryParse($"{result}-{result2}-{result3}", out var result5))
				{
					return new DateTime(result5.Year, result5.Month, result5.Day, Convert.ToInt32(result4), 0, 0);
				}
				ShowHUDBigInfo(HUDBigInfoMessage($"Invalid input {result}-{result2}-{result3}T{result4}:00:00:000Z: please input  a valid date and time", MessageType.Error, Color.red));
				return DateTime.MinValue;
			}
			ShowHUDBigInfo(HUDBigInfoMessage("Invalid input " + inGameYear + "-" + inGameMonth + "-" + inGameDay + "T" + inGameHour + ":00:00:000Z: please input valid date and time", MessageType.Error, Color.red));
			return DateTime.MinValue;
		}

		private float ValidateTimeScale(string toValidate)
		{
			if (float.TryParse(toValidate, out var result))
			{
				if (result <= 5f)
				{
					result = 5f;
				}
				if (result > 30f)
				{
					result = 30f;
				}
				return result;
			}
			ShowHUDBigInfo(HUDBigInfoMessage("Invalid input " + toValidate + ": please input numbers only - min. 5 and max. 30", MessageType.Error, Color.red));
			return -1f;
		}

		private void SetTimeScales(float dayTimeScale, float nightTimeScale)
		{
			try
			{
				TOD_Time tODTime = MainLevel.Instance.m_TODTime;
				tODTime.m_DayLengthInMinutes = dayTimeScale;
				tODTime.m_NightLengthInMinutes = nightTimeScale;
				MainLevel.Instance.m_TODTime = tODTime;
				ShowHUDBigInfo(HUDBigInfoMessage(TimeScalesSetMessage(dayTimeScale.ToString(), nightTimeScale.ToString()), MessageType.Info, Color.green));
			}
			catch (Exception exc)
			{
				HandleException(exc, "SetTimeScales");
			}
		}

		private void SetDayTime(int gameDay, int gameMonth, int gameYear, float gameHour)
		{
			try
			{
				TOD_Sky tODSky = MainLevel.Instance.m_TODSky;
				tODSky.Cycle.Day = gameDay;
				tODSky.Cycle.Hour = gameHour;
				tODSky.Cycle.Month = gameMonth;
				tODSky.Cycle.Year = gameYear;
				MainLevel.Instance.m_TODSky = tODSky;
				MainLevel.Instance.SetTimeConnected(tODSky.Cycle);
				MainLevel.Instance.UpdateCurentTimeInMinutes();
				ShowHUDBigInfo(HUDBigInfoMessage(DayTimeSetMessage(InGameDay, InGameMonth, InGameYear, InGameHour), MessageType.Info, Color.green));
			}
			catch (Exception exc)
			{
				HandleException(exc, "SetDayTime");
			}
		}
	}
}

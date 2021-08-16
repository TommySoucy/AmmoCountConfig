using Comfort.Common;
using ComponentAce.Compression.Libs.zlib;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using Harmony;
using MelonLoader;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static EFT.Player;
using static EFT.Player.FirearmController;
using ClientConfig = GClass333;

namespace AmmoCountConfig
{
    public class AmmoCountConfigMod : MelonMod
    {
        // For config request
        private static bool patched = false;
        private static string backEndSessionID;
        public static string backendUrl;

        // Config settings
        public static bool alwaysShow = true;
        public static float fireModeDelay = 1.5f;
        public static float zeroingDelay = 1.5f;
        public static int level = 2;
        public static bool forceLevel = false;
        public static bool showMax = true;
        public static bool showChamber = true;

        // Live data
        public static CustomTextMeshProUGUI ammoCountMessageUI;
        public static CustomTextMeshProUGUI ammoCountDetailsUI;
        public static GameWorld gameWorldInstance;
        public static Player player;
        public static BattleUIScreen battleUIScreen;
        public static FirearmController firearmController;
        public static bool isFiremode;
        public static string ammoCountMessage = "";
        public static string ammoCountDetails = "";
        public static float fireModeTimer = 0;
        public static bool hasChamber;
        public static bool isZeroing;
        public static bool shouldBeZeroing;
        public static float zeroingTimer = 0;

        public override void OnUpdate()
        {
            if (!patched)
            {
                try
                {
                    backEndSessionID = Singleton<ClientApplication>.Instance.GetClientBackEndSession().GetPhpSessionId();
                    backendUrl = ClientConfig.Config.BackendUrl;

                    patched = true;

                    Init();

                    DoPatching();
                }
                catch { }
            }

            if(fireModeTimer > 0)
            {
                fireModeTimer -= Time.deltaTime;
            }
            else if(isFiremode)
            {
                isFiremode = false;

                UpdateAmmoCountText();
            }

            if(zeroingTimer > 0)
            {
                zeroingTimer -= Time.deltaTime;
            }
            else if(isZeroing)
            {
                isZeroing = false;

                UpdateAmmoCountText();
            }
        }

        private static void Init()
        {
            LoadConfig();

            gameWorldInstance = Singleton<GameWorld>.Instance;
        }

        private static void LoadConfig()
        {
            // ONCE INTEGRATED INTO SINGLEPLAYER PATCHES OF JET, THIS SHOULD USE Request CLASS FROM JET HTTP UTILITIES TO GET CONFIG FROM SERVERSIDE
            // I took what was essential to communicate with server because I didn't want to copy the whole thing here
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var fullUri = "/client/config/ammoCount";
            if (!Uri.IsWellFormedUriString(fullUri, UriKind.Absolute))
            {
                fullUri = backendUrl + fullUri;
            }
            WebRequest request = WebRequest.Create(new Uri(fullUri));

            if (!string.IsNullOrEmpty(backEndSessionID))
            {
                request.Headers.Add("Cookie", $"PHPSESSID={backEndSessionID}");
                request.Headers.Add("SessionId", backEndSessionID);
            }

            request.Headers.Add("Accept-Encoding", "deflate");

            request.Method = "GET";

            string json = "";

            try
            {
                WebResponse response = request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        if (stream == null)
                        {
                            json = "";
                        }
                        stream.CopyTo(ms);
                        json = SimpleZlib.Decompress(ms.ToArray(), null);
                    }
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(json))
            {
                MelonLogger.Msg("Failed to fetch serverside config, loading local instead");

                LoadLocalConfig();
            }

            try
            {
                var jObject = JObject.Parse(json);
                alwaysShow = bool.Parse(jObject["alwaysShow"].ToString());
                fireModeDelay = float.Parse(jObject["fireModeDelay"].ToString());
                zeroingDelay = float.Parse(jObject["zeroingDelay"].ToString());
                level = int.Parse(jObject["level"].ToString());
                forceLevel = bool.Parse(jObject["forceLevel"].ToString());
                showMax = bool.Parse(jObject["showMax"].ToString());
                showChamber = bool.Parse(jObject["showChamber"].ToString());

                MelonLogger.Msg("Configs loaded from serverside");
            }
            catch
            {
                MelonLogger.Msg("Failed to fetch serverside config, loading local instead if possible");

                LoadLocalConfig();
            }
        }

        private static void LoadLocalConfig()
        {
            try
            {
                string[] lines = File.ReadAllLines("Mods/AmmoCountConfig.txt");

                foreach (string line in lines)
                {
                    if (line.Length == 0 || line[0] == '#')
                    {
                        continue;
                    }

                    string trimmedLine = line.Trim();
                    string[] tokens = trimmedLine.Split('=');

                    if (tokens.Length == 0)
                    {
                        continue;
                    }

                    if (tokens[0].IndexOf("alwaysShow") == 0)
                    {
                        if (tokens[1].IndexOf("false") > -1)
                        {
                            alwaysShow = false;
                        }
                    }
                    else if (tokens[0].IndexOf("fireModeDelay") == 0)
                    {
                        fireModeDelay = float.Parse(tokens[1].Trim());
                    }
                    else if (tokens[0].IndexOf("zeroingDelay") == 0)
                    {
                        zeroingDelay = float.Parse(tokens[1].Trim());
                    }
                    else if (tokens[0].IndexOf("level") == 0)
                    {
                        level = int.Parse(tokens[1].Trim());
                    }
                    else if (tokens[0].IndexOf("forceLevel") == 0)
                    {
                        if (tokens[1].IndexOf("true") > -1)
                        {
                            forceLevel = true;
                        }
                    }
                    else if (tokens[0].IndexOf("showMax") == 0)
                    {
                        if (tokens[1].IndexOf("false") > -1)
                        {
                            showMax = false;
                        }
                    }
                    else if (tokens[0].IndexOf("showChamber") == 0)
                    {
                        if (tokens[1].IndexOf("false") > -1)
                        {
                            showChamber = false;
                        }
                    }
                }

                MelonLogger.Msg("Configs loaded from local");
            }
            catch (FileNotFoundException) { /* In case of file not found, we don't want to do anything, user prob deleted it for a reason */ }
            catch (Exception ex) { MelonLogger.Msg("Couldn't read AmmoCountConfig.txt, using default settings instead. Error: " + ex.Message); }
        }

        private static void DoPatching()
        {
            var harmony = new HarmonyLib.Harmony("VIP.TommySoucy.AmmoCountConfig");

            harmony.PatchAll();
        }

        public static void Player_OnHandsControllerChanged(AbstractHandsController arg1, AbstractHandsController arg2)
        {
            if (firearmController != null)
            {
                firearmController.OnShot -= AmmoCountConfigMod_OnShot;
                firearmController.OnReadyToOperate -= AmmoCountConfigMod_OnReadyToOperate;
            }

            firearmController = player.HandsController as FirearmController;

            if (firearmController != null)
            {
                firearmController.OnShot += AmmoCountConfigMod_OnShot;
                firearmController.OnReadyToOperate += AmmoCountConfigMod_OnReadyToOperate;

                Weapon weapon = firearmController.Item;

                if (weapon != null && weapon.GetCurrentMagazine() != null)
                {
                    int count = weapon.GetCurrentMagazine().Count;
                    int maxCount = weapon.GetCurrentMagazine().MaxCount;
                    int mastering = Mathf.Max(player.Profile.MagDrillsMastering, weapon.GetCurrentMagazine().CheckOverride);
                    string details = (weapon.GetCurrentMagazine().Cartridges.Peek() as GClass1709) != null ? weapon.GetCurrentMagazine().Cartridges.Peek().Name.Localized() : null;
                    battleUIScreen.ShowAmmoDetails(count, maxCount, mastering, details);

                    isFiremode = false;
                    ammoCountMessage = AmmoCountPanel.GetAmmoCountByLevel(count, maxCount, mastering);
                    ammoCountDetails = details;
                }
            }
            else if(ammoCountMessageUI != null)
            {
                ammoCountMessageUI.text = "";
                ammoCountDetailsUI.text = "";
            }
        }

        public static void AmmoCountConfigMod_OnShot()
        {
            UpdateAmmoCountText();
        }

        public static void AmmoCountConfigMod_OnReadyToOperate(FirearmController obj)
        {
            UpdateAmmoCountText();
        }

        public static void UpdateAmmoCountText()
        {
            if (!isFiremode && !isZeroing)
            {
                if (alwaysShow && firearmController != null)
                {
                    GClass1663 currentMagazine = firearmController.Item.GetCurrentMagazine();
                    if (currentMagazine != null)
                    {
                        ammoCountMessage = AmmoCountPanel.GetAmmoCountByLevel(currentMagazine.Count, currentMagazine.MaxCount, Mathf.Max(player.Profile.MagDrillsMastering, currentMagazine.CheckOverride)).ToString();
                        ammoCountDetails = (currentMagazine.Cartridges.Peek() as GClass1709) != null ? currentMagazine.Cartridges.Peek().Name.Localized() : null;
                    }
                    else
                    {
                        ammoCountMessage = "";
                        ammoCountDetails = "";
                    }
                }
                else
                {
                    ammoCountMessage = "";
                    ammoCountDetails = "";
                }

                if (ammoCountMessageUI != null)
                {
                    ammoCountMessageUI.text = ammoCountMessage;
                    ammoCountDetailsUI.text = ammoCountDetails;
                }
            }
        }
    }

    [HarmonyPatch(typeof(AmmoCountPanel), nameof(AmmoCountPanel.GetAmmoCountByLevel))]
    class GetAmmoCountByLevelPatch
    {
        // This patch sets level to given one if necessary
        static void Prefix(ref int level)
        {
            if (AmmoCountConfigMod.level != -1)
            {
                // Set level to our level if higher than in-game level
                level = AmmoCountConfigMod.forceLevel ? AmmoCountConfigMod.level : AmmoCountConfigMod.level <= level ? level : AmmoCountConfigMod.level;
            }
        }

        // This patch adds max and chamber ammo count to output if necessary
        static void Postfix(int maxAmmoCount, int level, ref string __result)
        {
            if (!AmmoCountConfigMod.isFiremode && !AmmoCountConfigMod.isZeroing)
            {
                if (AmmoCountConfigMod.showMax)
                {
                    __result += " / " + maxAmmoCount.ToString();
                }

                if(AmmoCountConfigMod.showChamber && AmmoCountConfigMod.firearmController != null)
                {
                    int chamberAmmoCount = AmmoCountConfigMod.firearmController.Item.ChamberAmmoCount;
                    __result += " +" + chamberAmmoCount;
                }
            }
        }
    }

    [HarmonyPatch(typeof(BattleUIScreen), "Show", new Type[] { typeof(GamePlayerOwner) })]
    class BattleUIScreenShowPatch
    {
        // This patches one of the Show() methods of BattleUIScreen to intialize player and its instance
        // There is probably a better way to access player but this works for now because we won't need it unless BattleUIScreen is shown anyway
        static void Postfix(ref Player ___player_0, ref BattleUIScreen __instance)
        {
            if (AmmoCountConfigMod.player == null)
            {
                AmmoCountConfigMod.battleUIScreen = __instance;
                AmmoCountConfigMod.player = ___player_0;

                AmmoCountConfigMod.player.OnHandsControllerChanged += AmmoCountConfigMod.Player_OnHandsControllerChanged;
            }
            else if(AmmoCountConfigMod.alwaysShow && AmmoCountConfigMod.firearmController != null)
            {
                // Getting here would mean that the battle ui screen has already been shown once
                // So we got here by closing our inventory/menu/etc
                // Details won't be shown on their own in this case so have to do it manually:

                Weapon weapon = AmmoCountConfigMod.firearmController.Item;

                if (weapon != null && weapon.GetCurrentMagazine() != null)
                {
                    int count = weapon.GetCurrentMagazine().Count;
                    int maxCount = weapon.GetCurrentMagazine().MaxCount;
                    int mastering = Mathf.Max(___player_0.Profile.MagDrillsMastering, weapon.GetCurrentMagazine().CheckOverride);
                    string details = (weapon.GetCurrentMagazine().Cartridges.Peek() as GClass1709) != null ? weapon.GetCurrentMagazine().Cartridges.Peek().Name.Localized() : null;
                    __instance.ShowAmmoDetails(count, maxCount, mastering, details);

                    AmmoCountConfigMod.isFiremode = false;
                    AmmoCountConfigMod.ammoCountMessage = AmmoCountPanel.GetAmmoCountByLevel(count, maxCount, mastering);
                    AmmoCountConfigMod.ammoCountDetails = details;
                }
            }
        }
    }

    [HarmonyPatch(typeof(BattleUIScreen), "ShowAmmoCountZeroingPanel")]
    class BattleUIScreenShowZeroingPatch
    {
        // This patches ShowAmmoCountZeroingPanel() or BattleUIScreen just so we can know when the ammo details are called to be shown for zeroing specifically
        // We set zeroing to true in prefix and false in postfix
        // In prefix, we set it to true so that when AmmoCountPanel.Show() is called we know it was to show zeroing
        // In postfix we set it back to false because it might be true once it makes it there because original might fail to call AmmoCountPanel.Show() and zeroing would still be true
        static void Prefix()
        {
            AmmoCountConfigMod.shouldBeZeroing = true;
        }

        static void Postfix()
        {
            AmmoCountConfigMod.shouldBeZeroing = false;
        }
    }

    [HarmonyPatch(typeof(AmmoCountPanel), "Show", new Type[] { typeof(string), typeof(string) })]
    class AmmoCountPanelShowPatch
    {
        // This patches one of the Show() methods of AmmoCountPanel to remove the ammo count panel fade animation if necessary
        static bool Prefix(string message, string details, ref CustomTextMeshProUGUI ____ammoCount, ref CustomTextMeshProUGUI ____ammoDetails, ref BattleUIComponentAnimation ___battleUIComponentAnimation_0, ref AmmoCountPanel __instance)
        {
            // If we always want to show then show without fade out animation
            if (AmmoCountConfigMod.alwaysShow)
            {
                if (AmmoCountConfigMod.ammoCountMessageUI == null)
                {
                    AmmoCountConfigMod.ammoCountMessageUI = ____ammoCount;
                    AmmoCountConfigMod.ammoCountDetailsUI = ____ammoDetails;
                }

                // If not in firemode
                if (!AmmoCountConfigMod.isFiremode)
                {
                    // Check if should be
                    if (message.Equals("fullauto") || message.Equals("single") || message.Equals("doublet") || message.Equals("burst"))
                    {
                        AmmoCountConfigMod.isFiremode = true;
                        AmmoCountConfigMod.fireModeTimer = AmmoCountConfigMod.fireModeDelay;

                        // If currently zeroing, don't want to save message because the latest ammo count has already been saved
                        if (!AmmoCountConfigMod.isZeroing && !AmmoCountConfigMod.isFiremode)
                        {
                            AmmoCountConfigMod.ammoCountMessage = ____ammoCount.text;
                            AmmoCountConfigMod.ammoCountDetails = ____ammoDetails.text;
                        }
                    }
                }

                if (AmmoCountConfigMod.shouldBeZeroing)
                {
                    AmmoCountConfigMod.shouldBeZeroing = false;
                    AmmoCountConfigMod.isZeroing = true;
                    AmmoCountConfigMod.zeroingTimer = AmmoCountConfigMod.zeroingDelay;

                    if (!AmmoCountConfigMod.isFiremode && !AmmoCountConfigMod.isZeroing)
                    {
                        AmmoCountConfigMod.ammoCountMessage = ____ammoCount.text;
                        AmmoCountConfigMod.ammoCountDetails = ____ammoDetails.text;
                    }
                }

                (__instance as EFT.UI.UIElement).ShowGameObject(false); 
                if (___battleUIComponentAnimation_0 == null)
                {
                    ___battleUIComponentAnimation_0 = (__instance as EFT.UI.UIElement).gameObject.GetComponent<BattleUIComponentAnimation>();
                }

                ____ammoCount.text = message;
                ____ammoDetails.gameObject.SetActive(true); // Ammo details will always be visible
                ____ammoDetails.text = details;
                ___battleUIComponentAnimation_0.Show(false, 0f).HandleExceptions();

                return false; // Skip original
            }

            return true; // Don't skip original
        }
    }

    [HarmonyPatch(typeof(AmmoCountPanel), "Hide")]
    class AmmoCountPanelHidePatch
    {
        // This patches the Hide() method of the ammo count panel to ensure we don't hide it if necessary
        static bool Prefix()
        {
            return !AmmoCountConfigMod.alwaysShow; // If want to always show, will return false, skipping original
        }
    }

    [HarmonyPatch(typeof(MovementState), "OnReload")]
    class ReloadActionPatch
    {
        // This patches the method that indicates a completed reload
        static void Postfix()
        {
            AmmoCountConfigMod.UpdateAmmoCountText();
        }
    }
}

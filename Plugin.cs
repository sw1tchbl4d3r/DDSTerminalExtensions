using System;
using System.IO;
using System.Collections.Generic;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements.Collections;

namespace DDSTerminalExtensions
{
    [BepInPlugin("sw1tchbl4d3.DDSTerminalExtensions", "DDSTerminalExtensions", PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("DeadeyeDeepfakeSimulacrum.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Plugin is loaded!");

            Harmony.CreateAndPatchAll(typeof(TerminalPatches));
            Log.LogInfo($"Harmony patches are loaded!");
        }
    }

    [HarmonyPatch(typeof(Terminal))]
    class TerminalPatches
    {
        public static void HelpGeneral(Terminal __instance, string[] args)
        {
            __instance.helpBasic = "\n";
            __instance.helpBasic += "Type HELPLOCAL for a list of localhost commands.\n";
            __instance.helpBasic += "Type HELPREMOTE for a list of commands unique to your remotehost.\n";
            __instance.helpBasic += "Type HELPSECRET for a list of secret commands.\n";
            __instance.helpBasic += "Type HELPEXT for a list of commands added by DDSTerminalExtensions.\n";
        }

        public static void HelpSecret(Terminal __instance, string[] args)
        {
            string helpSecret = "\n";
            helpSecret += "SECRET COMMANDS\n";
            helpSecret += "--------------------\n";
            helpSecret += "Hidden commands built into the game.\n";
            helpSecret += "--------------------\n";
            helpSecret += "RENAME_ME [NEW_NAME] -- renames the save/player\n";
            helpSecret += "CHIPS_ -- gain 100 chips\n";
            helpSecret += "DATUM_ -- gain 10 data\n";
            helpSecret += "GODD_ -- enables godmode (until new scene)\n";
            helpSecret += "GO_TO_SCENE_ [SCENE_NAME] -- loads given unity scene\n";
            helpSecret += "HAVE_SEX_ -- uh\n";
            helpSecret += "LOCKTOBER_ -- force unequip necklace\n";
            helpSecret += "MIDAS_ -- gain $100,000\n";
            helpSecret += "MOB_ -- gain 100 ego\n";
            helpSecret += "SKIPP_ -- skips current mission\n";
            helpSecret += "SPEND_ -- set spending level to 8 (WILL override level 9)\n";

            __instance.WriteToTerminal(helpSecret);
        }

        public static void HelpExt(Terminal __instance, string[] args)
        {
            string helpExt = "\n";
            helpExt += "TERMINALEXTENSIONS COMMANDS\n";
            helpExt += "--------------------\n";
            helpExt += "Commands added by the DDSTerminalExtensions mod.\n";
            helpExt += "--------------------\n";
            helpExt += "DUMP_MAP_ <FILENAME> -- dumps current scene's map\n";
            helpExt += "UNGODD_ -- disables godmode\n";
            helpExt += "COORDS_COMPUTER_ [COMPUTER_ID] -- gets coordinates of computer\n";
            helpExt += "COORDS_PLAYER_ -- gets coordinates of the player\n";
            helpExt += "TELEPORT_ [X] [Y] -- teleports player to x, y\n";

            __instance.WriteToTerminal(helpExt);
        }

        public static readonly Dictionary<string, string> textureLookup = new()
        {
            {"Vidya", "obj/gamer_station"},
            {"fridge", "obj/fridge"},
            {"Keyboard", "obj/desktop"},
            {"AmmoStation", "obj/ammo_station"},
            {"RepairStation", "obj/repair_station"},
            {"toilet", "obj/toilet"},
            {"SlowRefill", "obj/slug_needle"},
            {"CommunicationsArray", "obj/com_hub"},
        };

        public static void DumpMap(Terminal __instance, string[] args)
        {
            string filename;

            if (args.Length > 1)
                filename = args[1];
            else
                filename = "extracted.map";

            __instance.WriteToTerminal("Starting map dump...");

            var sr = File.CreateText(filename);
            sr.NewLine = "\n";

            sr.WriteLine($"MAP");
            sr.WriteLine($"name: {SceneManager.GetActiveScene().name}");
            sr.WriteLine($"cam_start: 0, 0");
            sr.WriteLine($"");

            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                Computer computer = obj.GetComponent<Computer>();
                if (computer != null)
                {
                    if (computer is Terminal || computer is GrenadeComputer)
                        continue;

                    sr.WriteLine($"OBJ");
                    sr.WriteLine($"name: {computer.computerID}");
                    sr.WriteLine($"position: {Math.Round(obj.transform.position.x, 4)}, {Math.Round(obj.transform.position.y, 4)}");
                    sr.WriteLine($"texture_name: {textureLookup.Get(computer.displaySprite.name, computer.displaySprite.name)}");
                    sr.WriteLine($"color: #{ColorUtility.ToHtmlStringRGB(computer.displayColor)}");
                    sr.WriteLine($"");
                }

                bool isDoor = obj.GetComponent<Door>() != null;
                if (obj.tag == "Wall" || isDoor)
                {
                    sr.WriteLine($"WALL");

                    if (isDoor)
                        sr.WriteLine($"type: door");

                    sr.WriteLine($"position: {Math.Round(obj.transform.position.x, 4)}, {Math.Round(obj.transform.position.y, 4)}");

                    Vector3 scale = obj.transform.lossyScale;
                    if (isDoor)
                    {
                        for (int i = 0; i < obj.transform.childCount; i++)
                        {
                            Transform child = obj.transform.GetChild(i);
                            if (child.tag != "Door")
                                continue;

                            scale.x = child.lossyScale.x * 2;
                            scale.y = child.lossyScale.y;
                            break;
                        }

                        if (obj.transform.rotation.z != 0)
                        {
                            scale.z = scale.y;
                            scale.y = scale.x;
                            scale.x = scale.z;
                        }
                    }
                    else
                    {
                        if (obj.transform.parent.transform.rotation.z != 0)
                        {
                            scale.z = scale.y;
                            scale.y = scale.x;
                            scale.x = scale.z;
                        }
                    }

                    sr.WriteLine($"scale: {Math.Round(scale.x, 2)}, {Math.Round(scale.y, 2)}");
                    sr.WriteLine($"");
                }
            }

            sr.Close();
            __instance.WriteToTerminal($"Map dumped to {filename}!");
        }

        public static void GetCoordsComputer(Terminal __instance, string[] args)
        {
            string computerID;

            if (args.Length > 1)
                computerID = args[1];
            else
            {
                __instance.WriteToTerminal("Please specify target computer name.");
                return;
            }

            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                Computer computer = obj.GetComponent<Computer>();
                if (computer != null)
                {
                    if (computer.computerID == computerID)
                        __instance.WriteToTerminal($"{computer.computerID} -- {obj.transform.position.x}, {obj.transform.position.y}");
                }
            }
        }

        public static void GetCoordsPlayer(Terminal __instance, string[] args)
        {
            GameObject player = __instance.playerCyborg.gameObject;
            __instance.WriteToTerminal($"player -- {player.transform.position.x}, {player.transform.position.y}");
        }

        public static void UnGod(Terminal __instance, string[] args)
        {
            __instance.playerCyborg.godMode = 0;
        }

        public static void Teleport(Terminal __instance, string[] args)
        {
            float x, y;

            if (args.Length > 2) {
                if (!float.TryParse(args[1], out x))
                {
                    __instance.WriteToTerminal("x coordinate is not a number.");
                    return;
                }

                if (!float.TryParse(args[2], out y))
                {
                    __instance.WriteToTerminal("y coordinate is not a number.");
                    return;
                }
            }
            else
            {
                __instance.WriteToTerminal("Please specify x and y coordinates.");
                return;
            }

            GameObject player = __instance.playerCyborg.gameObject;
            player.transform.position = new Vector3(x, y, player.transform.position.z);
        }

        public delegate void Command(Terminal __instance, string[] args);
        public static readonly Dictionary<string, Command> commandLookup = new()
        {
            {"hs", HelpSecret},
            {"helpsecret", HelpSecret},
            {"he", HelpExt},
            {"helpext", HelpExt},

            {"dump_map_", DumpMap},
            {"coords_computer_", GetCoordsComputer},
            {"coords_player_", GetCoordsPlayer},
            {"ungodd_", UnGod},
            {"teleport_", Teleport},

            // COMMAND OVERRIDES
            {"help", HelpGeneral},
        };

        [HarmonyPrefix]
        [HarmonyPatch("ProcessInput")]
        public static bool ProcessInputPatch(Terminal __instance, string input)
        {
            string[] cmdArray = input.ToLower().Split(' ');
            commandLookup.Get(cmdArray[0])?.Invoke(__instance, cmdArray);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        public static void StartPost(Terminal __instance)
        {
            int fontSize = __instance.inputField.textComponent.fontSize;
            FontStyle fontStyle = __instance.inputField.textComponent.fontStyle;

            __instance.inputField.textComponent.font.RequestCharactersInTexture("x", fontSize, fontStyle);
            __instance.inputField.textComponent.font.GetCharacterInfo('x', out CharacterInfo characterInfo, fontSize);

            __instance.inputField.caretWidth = characterInfo.advance;
            __instance.inputField.customCaretColor = true;
            __instance.inputField.caretColor = new Color(1, 1, 1, 0.4f);
        }

        [HarmonyPrefix]
        [HarmonyPatch("GoBackInHistory")]
        public static bool GoBackInHistoryPatch(Terminal __instance)
        {
            var traverse = Traverse.Create(__instance);
            int inputHistoryIndex = traverse.Field("inputHistoryIndex").GetValue() as int? ?? 0;
            List<string> inputHistory = traverse.Field("inputHistory").GetValue() as List<string>;

            if (inputHistoryIndex > 0)
            {
                inputHistoryIndex--;

                if (inputHistoryIndex < inputHistory.Count)
                    __instance.inputField.text = inputHistory[inputHistoryIndex];
            }
            else
            {
                inputHistoryIndex = -1;
                __instance.inputField.text = "";
            }

            __instance.inputField.caretPosition = __instance.inputField.text.Length;

            traverse.Field("inputHistoryIndex").SetValue(inputHistoryIndex);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("GoForwardInHistory")]
        public static bool GoForwardInHistoryPatch(Terminal __instance)
        {
            var traverse = Traverse.Create(__instance);
            int inputHistoryIndex = traverse.Field("inputHistoryIndex").GetValue() as int? ?? 0;
            List<string> inputHistory = traverse.Field("inputHistory").GetValue() as List<string>;

            if (inputHistoryIndex < inputHistory.Count - 1)
            {
                inputHistoryIndex++;

                if (inputHistoryIndex < inputHistory.Count)
                    __instance.inputField.text = inputHistory[inputHistoryIndex];
            }
            else
            {
                inputHistoryIndex = inputHistory.Count;
                __instance.inputField.text = "";
            }

            __instance.inputField.caretPosition = __instance.inputField.text.Length;

            traverse.Field("inputHistoryIndex").SetValue(inputHistoryIndex);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void UpdatePost(Terminal __instance)
        {
            var traverse = Traverse.Create(__instance);
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
            {
                __instance.inputField.text = "";

                List<string> inputHistory = traverse.Field("inputHistory").GetValue() as List<string>;
                traverse.Field("inputHistoryIndex").SetValue(inputHistory.Count);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("LateUpdate")]
        public static bool LateUpdatePatch(Terminal __instance)
        {
            var traverse = Traverse.Create(__instance);
            bool inputFieldActive = traverse.Field("inputFieldActive").GetValue() as bool? ?? false;
            bool hackingOpen = (traverse.Field("hackManager").GetValue() as HackManager).hackingOpen;

            if (inputFieldActive && hackingOpen)
            {
                traverse.Field("savedInput").SetValue(__instance.inputField.text);
                __instance.inputField.enabled = true;
                __instance.inputField.Select();
                __instance.inputField.ActivateInputField();
            }
            else
                __instance.inputField.DeactivateInputField();

            return false;
        }
    }
}

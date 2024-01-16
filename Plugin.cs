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
            string help_secret = "\n";
            help_secret += "SECRET COMMANDS\n";
            help_secret += "--------------------\n";
            help_secret += "Hidden commands built into the game.\n";
            help_secret += "--------------------\n";
            help_secret += "RENAME_ME [NEW_NAME] -- renames the save/player\n";
            help_secret += "CHIPS_ -- gain 100 chips\n";
            help_secret += "DATUM_ -- gain 10 data\n";
            help_secret += "GODD_ -- enables godmode (until new scene)\n";
            help_secret += "GO_TO_SCENE_ [SCENE_NAME] -- loads given unity scene\n";
            help_secret += "HAVE_SEX_ -- uh\n";
            help_secret += "LOCKTOBER_ -- force unequip necklace\n";
            help_secret += "MIDAS_ -- gain $100,000\n";
            help_secret += "MOB_ -- gain 100 ego\n";
            help_secret += "SKIPP_ -- skips current mission\n";
            help_secret += "SPEND_ -- set spending level to 8 (WILL override level 9)\n";

            __instance.WriteToTerminal(help_secret);
        }

        public static void HelpExt(Terminal __instance, string[] args)
        {
            string help_ext = "\n";
            help_ext += "TERMINALEXTENSIONS COMMANDS\n";
            help_ext += "--------------------\n";
            help_ext += "Commands added by the DDSTerminalExtensions mod.\n";
            help_ext += "--------------------\n";
            help_ext += "AK_DUMP_MAP_ <FILENAME> -- dumps current scene's map\n";
            help_ext += "AK_COORDS_COMPUTER_ [COMPUTER_ID] -- gets coordinates of computer\n";
            help_ext += "AK_COORDS_PLAYER_ -- gets coordinates of the player\n";
            help_ext += "AK_TELEPORT_ [x] [y] -- teleports player to x, y\n";

            __instance.WriteToTerminal(help_ext);
        }

        public static GameObject GetPlayerCyborg()
        {
            GameObject[] all_objects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in all_objects)
            {
                if (obj.name == "PlayerCyborg")
                    return obj;
            }
            return null;
        }

        public static readonly Dictionary<string, string> texture_lookup = new()
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

            GameObject[] all_objects = UnityEngine.Object.FindObjectsOfType<GameObject>();

            var sr = File.CreateText(filename);
            sr.NewLine = "\n";

            sr.WriteLine($"MAP");
            sr.WriteLine($"name: {SceneManager.GetActiveScene().name}");
            sr.WriteLine($"cam_start: 0, 0");
            sr.WriteLine($"");

            foreach (GameObject obj in all_objects)
            {
                Computer computer = obj.GetComponent<Computer>();
                if (computer != null)
                {
                    if (computer is Terminal || computer is GrenadeComputer)
                        continue;

                    sr.WriteLine($"OBJ");
                    sr.WriteLine($"name: {computer.computerID}");
                    sr.WriteLine($"position: {Math.Round(obj.transform.position.x, 4)}, {Math.Round(obj.transform.position.y, 4)}");
                    sr.WriteLine($"texture_name: {texture_lookup.Get(computer.displaySprite.name, computer.displaySprite.name)}");
                    sr.WriteLine($"color: #{ColorUtility.ToHtmlStringRGB(computer.displayColor)}");
                    sr.WriteLine($"");
                }

                bool is_door = obj.GetComponent<Door>() != null;
                if (obj.tag == "Wall" || is_door)
                {
                    sr.WriteLine($"WALL");

                    if (is_door)
                        sr.WriteLine($"type: door");

                    sr.WriteLine($"position: {Math.Round(obj.transform.position.x, 4)}, {Math.Round(obj.transform.position.y, 4)}");

                    Vector3 scale = obj.transform.lossyScale;
                    if (is_door)
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
            string computer_id;

            if (args.Length > 1)
                computer_id = args[1];
            else
            {
                __instance.WriteToTerminal("Please specify target computer name.");
                return;
            }

            GameObject[] all_objects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in all_objects)
            {
                Computer computer = obj.GetComponent<Computer>();
                if (computer != null)
                {
                    if (computer.computerID == computer_id)
                        __instance.WriteToTerminal($"{computer.computerID} -- {obj.transform.position.x}, {obj.transform.position.y}");
                }
            }
        }

        public static void GetCoordsPlayer(Terminal __instance, string[] args)
        {
            GameObject player = GetPlayerCyborg();
            if (player)
                __instance.WriteToTerminal($"Player is at {player.transform.position.x} {player.transform.position.y}");
            else
                __instance.WriteToTerminal($"Player not found (???)");
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

            GameObject player = GetPlayerCyborg();
            if (player)
                player.transform.position = new Vector3(x, y, player.transform.position.z);
        }

        public delegate void Command(Terminal __instance, string[] args);
        public static readonly Dictionary<string, Command> command_lookup = new()
        {
            {"hs", HelpSecret},
            {"helpsecret", HelpSecret},
            {"he", HelpExt},
            {"helpext", HelpExt},

            {"ak_dump_map_", DumpMap},
            {"ak_coords_computer_", GetCoordsComputer},
            {"ak_coords_player_", GetCoordsPlayer},
            {"ak_teleport_", Teleport},

            // COMMAND OVERRIDES
            {"help", HelpGeneral},
        };

        [HarmonyPrefix]
        [HarmonyPatch("ProcessInput")]
        public static bool ProcessInputPatch(Terminal __instance, string input)
        {
            string[] cmd_array = input.ToLower().Split(' ');
            command_lookup.Get(cmd_array[0])?.Invoke(__instance, cmd_array);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void UpdatePatch(Terminal __instance)
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
                __instance.inputField.text = "";
        }

        [HarmonyPrefix]
        [HarmonyPatch("LateUpdate")]
        public static bool LateUpdatePatch(Terminal __instance)
        {
            var traverse = Traverse.Create(__instance);
            bool input_field_active = traverse.Field("inputFieldActive").GetValue() as bool? ?? false;
            bool hacking_open = (traverse.Field("hackManager").GetValue() as HackManager).hackingOpen;

            if (input_field_active && hacking_open)
            {
                traverse.Field("savedInput").SetValue(__instance.inputField.text);
                __instance.inputField.enabled = true;
                __instance.inputField.Select();
                __instance.inputField.ActivateInputField();

                // NOTE: the original function is identical up to here
                //       original forces MoveTextEnd, we don't do this anymore

                // TODO: do this on object init somehow not on every update
                // TODO: find a stable source of the actual width required
                __instance.inputField.caretWidth = 8;
                __instance.inputField.customCaretColor = true;
                __instance.inputField.caretColor = new Color(1, 1, 1, 0.4f);

                // TODO: make this work with history
            }
            else
                __instance.inputField.DeactivateInputField();

            return false;
        }
    }
}

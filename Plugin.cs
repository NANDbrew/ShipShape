using BepInEx;
using HarmonyLib;
using System.Reflection;


namespace ShipShape
{
    [BepInPlugin(PLUGIN_ID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_ID = "com.nandbrew.shipshape";
        public const string PLUGIN_NAME = "ShipShape";
        public const string PLUGIN_VERSION = "1.2.0";

        //--settings--
        //internal ConfigEntry<bool> someSetting;


        private void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_ID);
            //someSetting = Config.Bind("Settings", "Some setting", false);
        }
    }
}

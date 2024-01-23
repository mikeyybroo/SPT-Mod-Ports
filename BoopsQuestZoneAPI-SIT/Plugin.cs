using BepInEx;

namespace QuestZoneAPI
{
    [BepInPlugin(Helpers.Constants.pluginGuid, Helpers.Constants.pluginName, Helpers.Constants.pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Starting");

            new Patches.QuestZoneAPICoopPlayerPatch().Enable();
            new Patches.QuestZoneAPILocalPlayerPatch().Enable();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {Helpers.Constants.pluginName} is loaded!");
        }
    }

    public enum ZoneType
    {
        PlaceItem,
        Visit
    }

    public class ZoneTransform
    {
        public string x { get; set; }
        public string y { get; set; }
        public string z { get; set; }

        public ZoneTransform(string x, string y, string z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    public class ZoneClass
    {
        public string zoneId { get; set; }
        public string zoneName { get; set; }
        public string zoneLocation { get; set; }
        public string zoneType { get; set; }
        public ZoneType zoneTypeEnum { get; set; }
        public ZoneTransform position { get; set; }
        public ZoneTransform rotation { get; set; } = new ZoneTransform("0", "0", "0");
        public ZoneTransform scale { get; set; } = new ZoneTransform("1", "1", "1");
    }
}

using StayInTarkov;
using EFT;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR;
using System.Threading.Tasks;

namespace QuestZoneAPI.Patches
{

    public class QuestZoneAPICoopPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(StayInTarkovPlugin).Assembly.GetType("StayInTarkov.Coop.CoopGame").GetMethod("vmethod_2", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void WaitForCoopGame(Task<LocalPlayer> task)
        {
            task.Wait();

            LocalPlayer localPlayer = task.Result;

            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                Logger.LogInfo("Coop player is ready");
                Player player = localPlayer.GetPlayer;
                string loc = player.Location;
                List<ZoneClass> zones = GetZones();
                AddZones(zones, loc);
            }
        }
        public static List<ZoneClass> GetZones()
        {
            var zones = Helpers.WebRequestHelper.Get<List<ZoneClass>>("/quests/zones/getZones");
            Logger.LogInfo(zones.First().zoneName);
            return zones;
        }

        public static void CreatePlaceItemZone(ZoneClass zone)
        {
            GameObject questZone = new GameObject();

            BoxCollider collider = questZone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            Vector3 position = new Vector3(float.Parse(zone.position.x), float.Parse(zone.position.y), float.Parse(zone.position.z));
            questZone.transform.position = position;
            EFT.Interactive.PlaceItemTrigger scriptComp = questZone.AddComponent<EFT.Interactive.PlaceItemTrigger>();
            scriptComp.SetId(zone.zoneId);

            questZone.layer = LayerMask.NameToLayer("Triggers");
            questZone.name = zone.zoneId;
        }

        public static void CreateVisitZone(ZoneClass zone)
        {
            GameObject questZone = new GameObject();

            BoxCollider collider = questZone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            Vector3 position = new Vector3(float.Parse(zone.position.x), float.Parse(zone.position.y), float.Parse(zone.position.z));
            questZone.transform.position = position;
            EFT.Interactive.ExperienceTrigger scriptComp = questZone.AddComponent<EFT.Interactive.ExperienceTrigger>();
            scriptComp.SetId(zone.zoneId);

            questZone.layer = LayerMask.NameToLayer("Triggers");
            questZone.name = zone.zoneId;
        }

        public static void AddZones(List<ZoneClass> zones, string currentLocation)
        {
            foreach (ZoneClass zone in zones)
            {
                if (zone.zoneLocation.ToLower() == currentLocation.ToLower())
                {
                    switch (Enum.Parse(typeof(ZoneType), zone.zoneType))
                    {
                        case ZoneType.PlaceItem:
                            Logger.LogInfo(zone.position.x);
                            Logger.LogInfo(zone.rotation.y);
                            CreatePlaceItemZone(zone);

                            break;
                        case ZoneType.Visit:
                            Logger.LogInfo(zone.position.x);
                            Logger.LogInfo(zone.rotation.y);
                            CreateVisitZone(zone);
                            break;
                        default:
                            Logger.LogInfo(zone.position.x);
                            Logger.LogInfo(zone.rotation.y);
                            CreateVisitZone(zone);
                            break;
                    }
                }
                else
                {
                    Logger.LogInfo("Zone not in current location");
                }
            }
        }

        [PatchPostfix]
        private static void PatchPostFix(Task<LocalPlayer> __result)
        {
            Task.Run(() => WaitForCoopGame(__result));
        }
    }

    public class QuestZoneAPILocalPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalPlayer).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
        }
        public static List<ZoneClass> GetZones()
        {
            var zones = Helpers.WebRequestHelper.Get<List<ZoneClass>>("/quests/zones/getZones");
            Logger.LogInfo(zones.First().zoneName);
            return zones;
        }

        public static void CreatePlaceItemZone(ZoneClass zone)
        {
            GameObject questZone = new GameObject();

            BoxCollider collider = questZone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            Vector3 position = new Vector3(float.Parse(zone.position.x), float.Parse(zone.position.y), float.Parse(zone.position.z));
            questZone.transform.position = position;
            EFT.Interactive.PlaceItemTrigger scriptComp = questZone.AddComponent<EFT.Interactive.PlaceItemTrigger>();
            scriptComp.SetId(zone.zoneId);

            questZone.layer = LayerMask.NameToLayer("Triggers");
            questZone.name = zone.zoneId;
        }

        public static void CreateVisitZone(ZoneClass zone)
        {
            GameObject questZone = new GameObject();

            BoxCollider collider = questZone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            Vector3 position = new Vector3(float.Parse(zone.position.x), float.Parse(zone.position.y), float.Parse(zone.position.z));
            questZone.transform.position = position;
            EFT.Interactive.ExperienceTrigger scriptComp = questZone.AddComponent<EFT.Interactive.ExperienceTrigger>();
            scriptComp.SetId(zone.zoneId);

            questZone.layer = LayerMask.NameToLayer("Triggers");
            questZone.name = zone.zoneId;
        }

        public static void AddZones(List<ZoneClass> zones, string currentLocation)
        {
            foreach (ZoneClass zone in zones)
            {
                if (zone.zoneLocation.ToLower() == currentLocation.ToLower())
                {
                    switch (Enum.Parse(typeof(ZoneType), zone.zoneType))
                    {
                        case ZoneType.PlaceItem:
                            Logger.LogInfo(zone.position.x);
                            Logger.LogInfo(zone.rotation.y);
                            CreatePlaceItemZone(zone);

                            break;
                        case ZoneType.Visit:
                            Logger.LogInfo(zone.position.x);
                            Logger.LogInfo(zone.rotation.y);
                            CreateVisitZone(zone);
                            break;
                        default:
                            Logger.LogInfo(zone.position.x);
                            Logger.LogInfo(zone.rotation.y);
                            CreateVisitZone(zone);
                            break;
                    }
                }
                else
                {
                    Logger.LogInfo("Zone not in current location");
                }
            }
        }

        [PatchPostfix]
        private static void PatchPostFix(ref Task<LocalPlayer> __result)
        {
            LocalPlayer localPlayer = __result.Result;
            if (localPlayer != null && localPlayer.IsYourPlayer)
            {
                Logger.LogInfo("Local player is ready");
                Player player = localPlayer.GetPlayer;
                string loc = player.Location;
                List<ZoneClass> zones = GetZones();
                AddZones(zones, loc);
            }
            else
            {
                Logger.LogInfo("Local player is not ready");
            }
        }
    }
}

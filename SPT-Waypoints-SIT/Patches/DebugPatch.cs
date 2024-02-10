using DrakiaXYZ.Waypoints.Components;
using EFT;
using MultiplayerTarkov;
using System.Reflection;
using Aki.Reflection.Patching;

namespace DrakiaXYZ.Waypoints.Patches
{
    public class DebugPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPrefix]
        public static void PatchPrefix()
        {
            //BotZoneDebugComponent.Enable();
            NavMeshDebugComponent.Enable();
        }
    }
}

using EFT;
using System.Reflection;
using Aki.Reflection.Patching;

namespace DrakiaXYZ.Waypoints.Patches
{
    public class EditorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPrefix]
        public static void PatchPrefix()
        {
            //EditorComponent.Enable();
        }
    }
}

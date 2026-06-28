using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace ShipShape
{
    internal class Patches
    {
        internal static Material damageMat;
        internal static AnimationCurve damageCurve;

        [HarmonyPatch(typeof(BoatDamageWater))]
        private static class SomeClassPatch
        {
            [HarmonyPatch("Start")]
            [HarmonyPrefix]
            public static void SomeMethodPatch(BoatDamageWater __instance)
            {
                if (!damageMat)
                {
                    var dam = SaveLoadManager.instance.GetCurrentObjects()[10].GetComponentInChildren<HullDamageTexture>();
                    damageMat = dam.GetComponent<Renderer>().material;
                    damageCurve = dam.alphaCurve;
                }

                if (__instance.damage.durabilityDays == 0)
                {
                    float mass = __instance.damage.GetComponent<Rigidbody>().mass;
                    float input = mass / 12 + 50;
                    if (input > 500)
                    {
                        input = mass / 30 + 300;
                    }
                    __instance.damage.waterUnitsCapacity = Mathf.Round(input);

                    float dur = Mathf.InverseLerp(60, 2000, mass / 20 + 50);
                    dur = Mathf.Lerp(120, 60, dur);
                    __instance.damage.durabilityDays = Mathf.Round(dur);
                    __instance.damage.wearSteepness = 0.03f;
                    __instance.damage.impactDamageMult = 0.014f;
                    __instance.damage.minimumImpactVelocity = 1.5f;
                }

                if (__instance.GetComponent<BoatDamageWaterButton>() == null)
                {

                    var col = __instance.gameObject.AddComponent<MeshCollider>();
                    col.isTrigger = true;
                    col.convex = true;
                    __instance.gameObject.AddComponent<BoatDamageWaterButton>();

                    var hull = __instance.damage.gameObject.GetComponent<SaveableObject>().GetCleanable().gameObject;
                    var damageOverlay = GameObject.Instantiate(hull, hull.transform.parent);
                    damageOverlay.gameObject.layer = 2;
                    damageOverlay.gameObject.SetActive(false);
                    damageOverlay.GetComponent<Renderer>().material = damageMat;
                    Component.Destroy(damageOverlay.GetComponent<CleanableObject>());
                    Component.Destroy(damageOverlay.GetComponent<Collider>());
                    Component.Destroy(damageOverlay.GetComponent<HullPlayerCollider>());

                    var overlayComp = damageOverlay.AddComponent<HullDamageTexture>();
                    overlayComp.damage = __instance.damage;
                    //overlayComp.alphaCurve = damageCurve;
                    overlayComp.alphaCurve = new AnimationCurve();
                    overlayComp.alphaCurve.AddKey(new Keyframe());
                    overlayComp.alphaCurve.AddKey(new Keyframe { time = 1, value = 1, inTangent = 2, outTangent = 2 });

                    damageOverlay.gameObject.SetActive(true);

                }


            }
        }

        [HarmonyPatch(typeof(BoatDamage))]
        private static class EmbarkPatch
        {
            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            public static void SomeMethodPatch(BoatDamage __instance, CapsuleCollider ___boatCol)
            {
                ___boatCol.gameObject.layer = 13;
                BoatEmbarkCollider[] cols = __instance.transform.GetComponentsInChildren<BoatEmbarkCollider>();

                if (cols.Length == 1)
                {
                    var col2 = UnityEngine.GameObject.Instantiate(cols[0].gameObject, cols[0].transform.parent, false);
                    col2.tag = "EmbarkColPlayer";
                    col2.name += " (player)";
                    col2.transform.localScale *= 1.1f;
                    cols[0].walkCollider.tag = "WalkColBoat";
                }
            }
        }

        [HarmonyPatch(typeof(BoatHorizon))]
        private static class BoatHorizonPatch
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            public static void SomeMethodPatch(BoatHorizon __instance)
            {
                var horizonSwitcher = SaveLoadManager.instance.gameObject.GetComponent<BoatHorizonPerformanceSwitcher>();
                if (!horizonSwitcher.horizons.Contains(__instance))
                {
                    Debug.Log("Adding " + __instance.transform.parent.name + " to BoatHorizonPerformanceSwitcher");
                    horizonSwitcher.horizons = horizonSwitcher.horizons.AddToArray(__instance);
                }
            }
        }
    }
}

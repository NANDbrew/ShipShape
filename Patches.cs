using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;

namespace ShipShape
{
    [HarmonyPatch(typeof(BoatDamageWater), "Start")]
    internal static class DamageWaterPatches
    {
        internal static Material damageMat;
        internal static AnimationCurve damageCurve;

        public static void Postfix(BoatDamageWater __instance)
        {
            // get reference material for damage overlay
            if (!damageMat)
            {
                var dam = SaveLoadManager.instance.GetCurrentObjects()[10].GetComponentInChildren<HullDamageTexture>();
                damageMat = dam.GetComponent<Renderer>().material;
                damageCurve = dam.alphaCurve;
            }


            // add bailing button
            if (__instance.GetComponent<BoatDamageWaterButton>() == null)
            {
                Debug.Log("ShipShape: adding bailing button for " + __instance.damage.name);

                var col = __instance.gameObject.AddComponent<MeshCollider>();
                col.convex = true;
                col.isTrigger = true;
                __instance.gameObject.AddComponent<BoatDamageWaterButton>();

                var hull = __instance.damage.gameObject.GetComponent<SaveableObject>().GetCleanable().gameObject;
                hull.SetActive(false);
                var damageOverlay = GameObject.Instantiate(hull, hull.transform.parent);
                hull.SetActive(true);
                damageOverlay.name = "hull_damage_overlay";
                damageOverlay.gameObject.layer = 2;
                damageOverlay.GetComponent<Renderer>().material = damageMat;
                Component.Destroy(damageOverlay.GetComponent<CleanableObject>());
                Component.Destroy(damageOverlay.GetComponent<HullPlayerCollider>());
                Component.Destroy(damageOverlay.GetComponent<Collider>());

                var overlayComp = damageOverlay.AddComponent<HullDamageTexture>();
                overlayComp.damage = __instance.damage;
                overlayComp.alphaCurve = new AnimationCurve();
                overlayComp.alphaCurve.AddKey(new Keyframe());
                overlayComp.alphaCurve.AddKey(new Keyframe { time = 1, value = 1, inTangent = 2, outTangent = 2 });

                damageOverlay.gameObject.SetActive(true);

            }


        }
    }

    [HarmonyPatch(typeof(BoatDamage), "Start")]
    internal static class EmbarkColPatch
    {
        public static void Postfix(BoatDamage __instance, CapsuleCollider ___boatCol)
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

            // set damage values
            if (Mathf.Approximately(__instance.durabilityDays, 0))
            {
                Debug.Log("ShipShape: setting damage values for " + __instance.name);
                float mass = __instance.GetComponent<Rigidbody>().mass;
                float input = mass / 12 + 50;
                if (input > 500)
                {
                    input = mass / 30 + 300;
                }
                __instance.waterUnitsCapacity = Mathf.Round(input);

                float days = Mathf.InverseLerp(60, 2000, mass / 20 + 50);
                days = Mathf.Lerp(120, 60, days);
                __instance.durabilityDays = Mathf.Round(days);
                __instance.wearSteepness = 0.03f;
                __instance.impactDamageMult = 0.014f;
                __instance.minimumImpactVelocity = 1.5f;
            }
        }
    }

    [HarmonyPatch(typeof(BoatHorizon), "Awake")]
    internal static class BoatHorizonPatch
    {
        public static void Postfix(BoatHorizon __instance, Rigidbody ___rigidbody)
        {
            if (___rigidbody == null)
            {
                Debug.Log("BoatHorizon " + __instance.name + " has no rigidbody. Skipping");
                return;
            }
            var horizonSwitcher = SaveLoadManager.instance.gameObject.GetComponent<BoatHorizonPerformanceSwitcher>();
            if (!horizonSwitcher.horizons.Contains(__instance))
            {
                Debug.Log("Adding " + __instance.name + " to BoatHorizonPerformanceSwitcher");
                horizonSwitcher.horizons = horizonSwitcher.horizons.AddToArray(__instance);
            }
        }
    }
    [HarmonyPatch(typeof(SaveLoadManager), "Awake")]
    internal static class SaveLoadManagerPatch
    {
        public static void Postfix(ref SaveableObject[] ___currentObjects)
        {
            if (___currentObjects.Length < 1024)
            {
                Debug.Log("ShipShape: resizing array at SaveLoadManager.currentObjects");
                Array.Resize(ref ___currentObjects, 1024);
            }
        }
    }
    [HarmonyPatch(typeof(Anchor), "Awake")]
    internal static class AnchorPatch
    {
        static Transform shiftingWorld;

        internal static void Prefix(Anchor __instance)
        {

            if (__instance.GetComponent<SaveableObject>() == null)
            {
                Debug.Log("ShipShape: Adding SaveableObject to " + __instance.name);

                int index = __instance.GetComponent<ConfigurableJoint>().connectedBody.GetComponent<SaveableObject>().sceneIndex;
                if (SaveLoadManager.instance.GetCurrentObjects()[index] != null)
                {
                    Debug.LogError($"ShipShape: Cannot add SaveableObject; Index {index} is already occupied!");
                    return;
                }
                __instance.gameObject.SetActive(false);

                var saveable = __instance.gameObject.AddComponent<SaveableObject>();
                saveable.anchor = __instance;
                saveable.sceneIndex = index + 512;

                __instance.gameObject.SetActive(true);
                Debug.Log("ShipShape: Added SaveableObject to " + __instance.name + ", with index " + saveable.sceneIndex);

            }
        }

        // Fix for Old Chronian anchors. Will also fix Jong Anchor if NANDFixes hasn't already
        internal static void Postfix(Anchor __instance)
        {
            if (__instance.transform.parent == null || (__instance.transform.parent != null && __instance.transform.parent.name != "_shifting world"))
            {
                if (shiftingWorld == null)
                {
                    shiftingWorld = GameObject.Find("_shifting world").transform;
                }
                __instance.transform.parent = shiftingWorld;
                Debug.Log("AnchorParent for " + __instance.name + " is now " + __instance.transform.parent.name);
            }
        }
    }
}


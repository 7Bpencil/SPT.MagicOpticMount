//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using Diz.Utils;
using EFT;
using EFT.CameraControl;
using EFT.GameTriggers;
using EFT.Impostors;
using EFT.Interactive;
using EFT.Settings.Graphics;
using EFT.UI.Settings;
using EFT;
using EFT.Animations;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using EFT.Visual;
using EFT.UI;
using EFT.UI.WeaponModding;
using EFT.Utilities;
using Newtonsoft.Json;
using HarmonyLib;
using SevenBoldPencil.Common;
using SPT.Reflection.Patching;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using JsonType;
using GPUInstancer;
using Koenigz.PerfectCulling.EFT;
using FirearmController = EFT.Player.FirearmController;
using NightVision = BSG.CameraEffects.NightVision;

namespace SevenBoldPencil.ModularSights;

[BepInPlugin("7Bpencil.ModularSights", "7Bpencil.ModularSights", "0.0.1")]
public class Plugin : BaseUnityPlugin
{
	public static Plugin Instance;

    private void Awake()
	{
		Instance = this;
		new Patch_OpticComponentUpdater_CopyComponentFromOptic().Enable();
	}
}

public class Patch_OpticComponentUpdater_CopyComponentFromOptic : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(OpticComponentUpdater), nameof(OpticComponentUpdater.CopyComponentFromOptic));
    }

    [PatchPostfix]
    public static void Postfix(OpticSight opticSight, ThermalVision ___thermalVision, NightVision ___nightVision, ref Transform ___cameraPivot)
	{
        if (!Singleton<GameWorld>.Instantiated)
        {
            return;
        }

        var player = Singleton<GameWorld>.Instance.MainPlayer;
		if (!player)
		{
			return;
		}
		if (!player.IsYourPlayer)
		{
			return;
		}

		var pwa = player.ProceduralWeaponAnimation;

		Transform opticBone;
		{
			var isOptic = pwa.CurrentScope.IsOptic;
			if (!isOptic)
			{
				return;
			}

			var scope = pwa.CurrentScope.ScopePrefabCache;
			if (!scope)
			{
				return;
			}

			var mode = scope._scopeModeInfos[scope.CurrentModeId];
			if (mode.OpticSight != opticSight)
			{
				// not us? no way
				return;
			}

			var scopeData = mode.OpticSight.ScopeData;

			var thermalData = scopeData.ThermalVisionData;
			if (thermalData && thermalData.ThermalVision)
			{
				// no need to adjust thermals
				return;
			}

			var nightVisionData = scopeData.NightVisionData;
			if (nightVisionData && nightVisionData.NightVision)
			{
				// no need to adjust nv
				return;
			}

			opticBone = pwa.CurrentScope.Bone;
		}

		// check if there is a thermal scope or night vision in front

		var _pwa = new ProceduralWeaponAnimation_Proxy(pwa);
		var firearmController = _pwa._firearmController;
		if (!firearmController)
		{
			return;
		}

		var firearms = firearmController.Firearms;
		var weaponRoot = firearms.WeaponPrefab.Hierarchy.GetTransform(ECharacterWeaponBones.weapon);
		var weaponForward = -weaponRoot.up;

		foreach (var scopeAimTransform in pwa.ScopeAimTransforms)
		{
			if (!scopeAimTransform.IsOptic)
			{
				continue;
			}

			var scope = scopeAimTransform.ScopePrefabCache;
			if (!scope)
			{
				return;
			}

			var scopeData = scope._scopeModeInfos[scope.CurrentModeId].OpticSight.ScopeData;

   			var thermalVisionData = scopeData.ThermalVisionData;
			if (thermalVisionData && thermalVisionData.ThermalVision && AreSightsAligned(opticBone, scopeAimTransform.Bone, weaponForward))
			{
				___cameraPivot = scopeData.transform;
				CopyThermalData(___thermalVision, thermalVisionData);
				break;
			}

			var nightVisionData = scopeData.NightVisionData;
			if (nightVisionData && nightVisionData.NightVision && AreSightsAligned(opticBone, scopeAimTransform.Bone, weaponForward))
			{
				___cameraPivot = scopeData.transform;
				CopyNightVisionData(___nightVision, nightVisionData);
				break;
			}
		}
	}

	public static bool AreSightsAligned(Transform opticBone, Transform specialOpticBone, Vector3 weaponForward)
	{
		var angle = Vector3.Angle(specialOpticBone.position - opticBone.position, weaponForward);
		return angle < 1;
	}

	public static void CopyThermalData(ThermalVision thermalVision, ScopeThermalVisionData thermalVisionData)
	{
		thermalVision.enabled = true;
		thermalVision.On = thermalVisionData.ThermalVision;
		thermalVision.IsGlitch = thermalVisionData.ThermalVisionIsGlitch;
		thermalVision.IsPixelated = thermalVisionData.ThermalVisionIsPixelated;
		thermalVision.IsNoisy = thermalVisionData.ThermalVisionIsNoisy;
		thermalVision.IsMotionBlurred = thermalVisionData.ThermalVisionIsMotionBlurred;
		thermalVision.IsFpsStuck = thermalVisionData.ThermalVisionIsFpsStuck;
		thermalVision.ThermalVisionUtilities = thermalVisionData.ThermalVisionUtilities;
		thermalVision.StuckFpsUtilities = thermalVisionData.StuckFPSUtilities;
		thermalVision.MotionBlurUtilities = thermalVisionData.MotionBlurUtilities;
		thermalVision.GlitchUtilities = thermalVisionData.GlitchUtilities;
		thermalVision.PixelationUtilities = thermalVisionData.PixelationUtilities;
		thermalVision.ChromaticAberrationThermalShift = thermalVisionData.ChromaticAberrationThermalShift;
		thermalVision.UnsharpBias = thermalVisionData.UnsharpBias;
		thermalVision.UnsharpRadiusBlur = thermalVisionData.UnsharpRadiusBlur;
	}

	public static void CopyNightVisionData(NightVision nightVision, ScopeNightVisionData nightVisionData)
	{
		nightVision.enabled = true;
		nightVision.On = nightVisionData.NightVision;
		nightVision.Intensity = nightVisionData.Intensity;
		nightVision.MaskSize = nightVisionData.MaskSize;
		nightVision.NoiseIntensity = nightVisionData.NoiseIntensity;
		nightVision.NoiseScale = nightVisionData.NoiseScale;
		nightVision.Color = nightVisionData.Color;
	}
}

public struct ProceduralWeaponAnimation_Proxy(ProceduralWeaponAnimation instance)
{
    private readonly ProceduralWeaponAnimation __instance = instance;

    private static TypedFieldInfo<ProceduralWeaponAnimation, FirearmController> __firearmController = new("_firearmController");

    public FirearmController _firearmController { get { return __firearmController.Get(__instance); } set { __firearmController.Set(__instance, value); } }
}

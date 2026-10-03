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
    public static void Postfix(OpticSight opticSight, ThermalVision ___thermalVision, ref Transform ___cameraPivot)
	{
		var thermalVision = ___thermalVision;

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
		var _pwa = new ProceduralWeaponAnimation_Proxy(pwa);
		var firearmController = _pwa._firearmController;
		if (!firearmController)
		{
			return;
		}

		var firearms = firearmController.Firearms;

		// TODO no need to go around PWA to search for current scope and shit,
		// its right there in method parameters, also opticBone is just ___cameraPivot

		Transform opticBone;
		{
			var isOptic = pwa.CurrentScope.IsOptic;
			if (!isOptic)
			{
				return;
			}

			var scope = pwa.CurrentScope.ScopePrefabCache;
			var currentModeIndex = scope.CurrentModeId;
			var scopeModes = scope._scopeModeInfos;
			var mode = scopeModes[currentModeIndex];

			var thermalData = mode.OpticSight.ScopeData.ThermalVisionData;
			if (thermalData)
			{
				// no need to adjust thermals
				return;
			}

			var nightVisionData = mode.OpticSight.ScopeData.NightVisionData;
			if (nightVisionData)
			{
				// no need to adjust nv
				return;
			}

			opticBone = pwa.CurrentScope.Bone;
		}

		// check if there is a thermal scope in front

		var weaponRoot = firearms.WeaponPrefab.Hierarchy.GetTransform(ECharacterWeaponBones.weapon);
		var weaponForward = -weaponRoot.up;

		foreach (var scopeAimTransform in pwa.ScopeAimTransforms)
		{
			if (!scopeAimTransform.IsOptic)
			{
				continue;
			}

			var scope = scopeAimTransform.ScopePrefabCache;
			var currentModeIndex = scope.CurrentModeId;
			var scopeModes = scope._scopeModeInfos;
			var mode = scopeModes[currentModeIndex];

			var thermalVisionData = mode.OpticSight.ScopeData.ThermalVisionData;
			if (!thermalVisionData)
			{
				continue;
			}
			if (!thermalVisionData.ThermalVision)
			{
				continue;
			}

			var thermalBone = scopeAimTransform.Bone;
			var angle = Vector3.Angle(thermalBone.position - opticBone.position, weaponForward);
			if (Vector3.Angle(thermalBone.position - opticBone.position, weaponForward) < 0.5f)
			{
				// copypaste from original method

				// TODO switching camera pivot probably wont work for magnifier-reflex-thermal combo
				___cameraPivot = mode.OpticSight.ScopeData.transform;

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

				break;
			}
		}
	}
}

public struct ProceduralWeaponAnimation_Proxy(ProceduralWeaponAnimation instance)
{
    private readonly ProceduralWeaponAnimation __instance = instance;

    private static TypedFieldInfo<ProceduralWeaponAnimation, FirearmController> __firearmController = new("_firearmController");
    private static TypedFieldInfo<ProceduralWeaponAnimation, bool> __isAiming = new("_isAiming");

    public FirearmController _firearmController { get { return __firearmController.Get(__instance); } set { __firearmController.Set(__instance, value); } }
    public bool _isAiming { get { return __isAiming.Get(__instance); } set { __isAiming.Set(__instance, value); } }
}

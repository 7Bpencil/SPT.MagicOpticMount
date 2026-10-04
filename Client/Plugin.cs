//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using SevenBoldPencil.Common;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using FirearmController = EFT.Player.FirearmController;
using NightVision = BSG.CameraEffects.NightVision;

// TODO add setting to multiply optic zoom by special optic zoom to keep it "realistic"

namespace SevenBoldPencil.MagicOpticMount;

[BepInPlugin("7Bpencil.MagicOpticMount", "7Bpencil.MagicOpticMount", "0.1.0")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
	{
		new Patch_OpticComponentUpdater_CopyComponentFromOptic().Enable();
		new Patch_ScopeZoomHandler_UpdateScope().Enable();
	}
}

public class Patch_OpticComponentUpdater_CopyComponentFromOptic : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(OpticComponentUpdater), nameof(OpticComponentUpdater.CopyComponentFromOptic));
    }

    [PatchPostfix]
    public static void Postfix(
		ref Transform ___cameraPivot,
		Camera ___opticCamera,
		ChromaticAberration ___chromaticAberration,
		BloomOptimized ___bloomOptimized,
	    ThermalVision ___thermalVision,
		CC_FastVignette ___cc_FastVignette_0,
		UltimateBloom ___ultimateBloom,
		Fisheye ___fisheye,
		Tonemapping ___tonemapping,
		NightVision ___nightVision,
		OpticSight opticSight
	) {
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
		if (pwa.ScopeAimTransforms.Count == 0)
		{
			// happens in hideout
			return;
		}

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
				return;
			}

			var nightVisionData = scopeData.NightVisionData;
			if (nightVisionData && nightVisionData.NightVision)
			{
				return;
			}

			opticBone = pwa.CurrentScope.Bone;
		}

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

			var mode = scope._scopeModeInfos[scope.CurrentModeId];
			var scopeData = mode.OpticSight.ScopeData;
			var cameraData = mode.OpticSight.CameraData;

   			var thermalVisionData = scopeData.ThermalVisionData;
			var nightVisionData = scopeData.NightVisionData;

			if (thermalVisionData && thermalVisionData.ThermalVision || nightVisionData && nightVisionData.NightVision)
			{
				if (AreSightsAligned(opticBone, scopeAimTransform.Bone, weaponForward))
				{
					CopySightData(ref ___cameraPivot, ___opticCamera, ___chromaticAberration, ___bloomOptimized, ___thermalVision, ___cc_FastVignette_0, ___ultimateBloom, ___fisheye, ___tonemapping, ___nightVision, scopeData, cameraData);
					break;
				}
			}
		}
	}

	public static bool AreSightsAligned(Transform opticBone, Transform specialOpticBone, Vector3 weaponForward)
	{
		const float maxAngle = 45f;
		const float maxDistance = 0.006f; // 6mm

		// device is in front of optic
		var angle = Vector3.Angle(specialOpticBone.position - opticBone.position, weaponForward);
		if (angle > maxAngle)
		{
			return false;
		}

		// optic and device view axes are reasonable close
		var squaredDistance = SqDistPointSegment(opticBone.position, opticBone.position + weaponForward, specialOpticBone.position);
		if (squaredDistance > maxDistance * maxDistance)
		{
			return false;
		}

		return true;
	}

	public static float SqDistPointSegment(Vector3 a, Vector3 b, Vector3 c)
	{
		var ab = b - a;
		var ac = c - a;

		// Handle cases where c projects outside ab
		var e = Vector3.Dot(ac, ab);
		if (e <= 0f)
		{
			return Vector3.Dot(ac, ac);
		}

		var f = Vector3.Dot(ab, ab);
		if (e >= f)
		{
			var bc = c - b;
			return Vector3.Dot(bc, bc);
		}

		// Handle cases where c projects onto ab
		return Vector3.Dot(ac, ac) - e * e / f;
	}

	// copypaste of OpticComponentUpdater.CopyComponentFromOptic
	public static void CopySightData(
		ref Transform cameraPivot,
		Camera opticCamera,
		ChromaticAberration chromaticAberration,
		BloomOptimized bloomOptimized,
	    ThermalVision thermalVision,
		CC_FastVignette cc_FastVignette_0,
		UltimateBloom ultimateBloom,
		Fisheye fisheye,
		Tonemapping tonemapping,
		NightVision nightVision,
		ScopeData scopeData,
		IScopeCameraData cameraData
	) {
		cameraPivot = scopeData.transform;

		var postEffectsData = scopeData.PostEffectsData;

		chromaticAberration.enabled = postEffectsData && postEffectsData.ChromaticAberration;
		if (chromaticAberration.enabled)
		{
			chromaticAberration.Aniso = postEffectsData.ChromaticAberrationAniso;
			chromaticAberration.Shift = postEffectsData.ChromaticAberrationShift;
		}

		bloomOptimized.enabled = postEffectsData && postEffectsData.BloomOptimized;
		if (bloomOptimized.enabled)
		{
			bloomOptimized.intensity = postEffectsData.BloomOptimizedIntensity;
			bloomOptimized.threshold = postEffectsData.BloomOptimizedThreshold;
			bloomOptimized.blurSize = postEffectsData.BloomOptimizedBlurSize;
		}

		var thermalVisionData = scopeData.ThermalVisionData;
		var hasThermal = thermalVisionData && thermalVisionData.ThermalVision;
		opticCamera.farClipPlane = hasThermal ? cameraData.FarClipPlane : CameraManager.Instance.Camera.farClipPlane;
		thermalVision.enabled = hasThermal;
		if (thermalVision.enabled)
		{
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

		cc_FastVignette_0.enabled = postEffectsData && postEffectsData.FastVignette;
		ultimateBloom.enabled = postEffectsData && postEffectsData.UltimateBloom;
		fisheye.enabled = postEffectsData && postEffectsData.Fisheye;
		tonemapping.enabled = postEffectsData && postEffectsData.Tonemapping;

		if (tonemapping.enabled)
		{
			tonemapping.white = postEffectsData.White;
			tonemapping.adaptionSpeed = postEffectsData.AdaptionSpeed;
			tonemapping.exposureAdjustment = postEffectsData.ExposureAdjustment;
			tonemapping.middleGrey = postEffectsData.MiddleGrey;
			tonemapping.adaptiveTextureSize = postEffectsData.AdaptiveTextureSize;
			tonemapping.type = postEffectsData.Type;
		}

		var nightVisionData = scopeData.NightVisionData;
		nightVision.enabled = nightVisionData && nightVisionData.NightVision;
		if (nightVision.enabled)
		{
			nightVision.On = nightVisionData.NightVision;
			nightVision.Intensity = nightVisionData.Intensity;
			nightVision.MaskSize = nightVisionData.MaskSize;
			nightVision.NoiseIntensity = nightVisionData.NoiseIntensity;
			nightVision.NoiseScale = nightVisionData.NoiseScale;
			nightVision.Color = nightVisionData.Color;
		}
	}
}

public class Patch_ScopeZoomHandler_UpdateScope : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ScopeZoomHandler), nameof(ScopeZoomHandler.UpdateScope));
    }

    [PatchPrefix]
    public static bool Prefix(IAdjustableOpticData ____adjustableOpticData)
	{
		// UpdateScope is a method relative to variable zoom scopes, but shit breaks
		// and for some reason it gets called even when _adjustableOpticData is null,
		// I guess game doesnt like when theres two optic scopes on the gun
		return ____adjustableOpticData != null;
	}
}

public struct ProceduralWeaponAnimation_Proxy(ProceduralWeaponAnimation instance)
{
    private readonly ProceduralWeaponAnimation __instance = instance;

    private static TypedFieldInfo<ProceduralWeaponAnimation, FirearmController> __firearmController = new("_firearmController");

    public FirearmController _firearmController { get { return __firearmController.Get(__instance); } set { __firearmController.Set(__instance, value); } }
}

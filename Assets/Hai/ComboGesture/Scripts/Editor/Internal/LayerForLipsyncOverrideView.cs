﻿using System;
using System.Collections.Generic;
using System.Linq;
using Hai.ComboGesture.Scripts.Components;
using Hai.ComboGesture.Scripts.Editor.Internal.Reused;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Hai.ComboGesture.Scripts.Editor.Internal
{
    internal class LayerForLipsyncOverrideView
    {
        private const string LipsyncLayerName = "Hai_GestureLipsync";

        private readonly float _analogBlinkingUpperThreshold;
        private readonly AvatarMask _logicalAvatarMask;
        private readonly AnimatorGenerator _animatorGenerator;
        private readonly VRCAvatarDescriptor _avatarDescriptor;
        private readonly ComboGestureLimitedLipsync _limitedLipsync;
        private readonly AssetContainer _assetContainer;
        private readonly AnimationClip _emptyClip;
        private readonly List<ManifestBinding> _manifestBindings;
        private readonly bool _writeDefaultsForLipsyncBlendshapes;

        public LayerForLipsyncOverrideView(float analogBlinkingUpperThreshold,
            AvatarMask logicalAvatarMask,
            AnimatorGenerator animatorGenerator,
            VRCAvatarDescriptor avatarDescriptor,
            ComboGestureLimitedLipsync limitedLipsync,
            AssetContainer assetContainer,
            AnimationClip emptyClip,
            List<ManifestBinding> manifestBindings,
            bool writeDefaults)
        {
            _analogBlinkingUpperThreshold = analogBlinkingUpperThreshold;
            _logicalAvatarMask = logicalAvatarMask;
            _animatorGenerator = animatorGenerator;
            _avatarDescriptor = avatarDescriptor;
            _limitedLipsync = limitedLipsync;
            _assetContainer = assetContainer;
            _emptyClip = emptyClip;
            _manifestBindings = manifestBindings;
            _writeDefaultsForLipsyncBlendshapes = writeDefaults;
        }

        public void Create()
        {
            EditorUtility.DisplayProgressBar("GestureCombo", "Clearing lipsync override layer", 0f);
            var machine = ReinitializeLayer();

            if (!_manifestBindings.Any(manifest => manifest.Manifest.RequiresLimitedLipsync()))
            {
                return;
            }

            var none = machine.AddState("None", SharedLayerUtils.GridPosition(0, 0));
            none.motion = _emptyClip;
            none.writeDefaultValues = false;

            var noneTransition = machine.AddAnyStateTransition(none);
            SetupLipsyncTransition(noneTransition);
            noneTransition.canTransitionToSelf = false;
            noneTransition.duration = _limitedLipsync.transitionDuration;
            noneTransition.AddCondition(AnimatorConditionMode.Less, _analogBlinkingUpperThreshold, "_Hai_GestureAnimLSWide");

            _assetContainer.RemoveAssetsStartingWith("zAutogeneratedLipsync_", typeof(AnimationClip));
            var regularVisemeClips = CreateRegularClips();
            var wideVisemeClips = CreateWideClips();

            AssetContainer.GlobalSave();
            for (var visemeNumber = 0; visemeNumber < wideVisemeClips.Count; visemeNumber++)
            {
                var state = machine.AddState("Wide " + visemeNumber, SharedLayerUtils.GridPosition(4, 2 + visemeNumber));
                state.motion = wideVisemeClips[visemeNumber];
                state.writeDefaultValues = _writeDefaultsForLipsyncBlendshapes;

                var transition = machine.AddAnyStateTransition(state);
                SetupLipsyncTransition(transition);
                transition.canTransitionToSelf = false;
                transition.duration = _limitedLipsync.transitionDuration * FindVisemeTransitionTweak(visemeNumber);
                transition.AddCondition(AnimatorConditionMode.Equals, visemeNumber, "Viseme");
                transition.AddCondition(AnimatorConditionMode.Greater, _analogBlinkingUpperThreshold, "_Hai_GestureAnimLSWide");
            }
            for (var visemeNumber = 0; visemeNumber < regularVisemeClips.Count; visemeNumber++)
            {
                var state = machine.AddState("Regular " + visemeNumber, SharedLayerUtils.GridPosition(-4, 2 + visemeNumber));
                state.motion = regularVisemeClips[visemeNumber];
                state.writeDefaultValues = _writeDefaultsForLipsyncBlendshapes;

                var transition = machine.AddAnyStateTransition(state);
                SetupLipsyncTransition(transition);
                transition.canTransitionToSelf = false;
                transition.duration = _limitedLipsync.transitionDuration * FindVisemeTransitionTweak(visemeNumber);
                transition.AddCondition(AnimatorConditionMode.Equals, visemeNumber, "Viseme");
                transition.AddCondition(AnimatorConditionMode.Less, _analogBlinkingUpperThreshold, "_Hai_GestureAnimLSWide");
            }
        }

        private void SetupLipsyncTransition(AnimatorStateTransition transition)
        {
            SharedLayerUtils.SetupDefaultTransition(transition);
            transition.exitTime = 0f;
            transition.hasExitTime = true;
        }

        private List<AnimationClip> CreateWideClips()
        {
            var visemeClips = Enumerable.Range(0, 15)
                .Select(visemeNumber =>
                {
                    var finalAmplitude = _limitedLipsync.amplitudeScale * FindVisemeAmplitudeTweak(visemeNumber);
                    var clip = new AnimationClip {name = "zAutogeneratedLipsync_ " + visemeNumber};
                    new VisemeAnimationMaker(_avatarDescriptor).OverrideAnimation(clip, visemeNumber, finalAmplitude);
                    return clip;
                })
                .ToList();
            foreach (var visemeClip in visemeClips)
            {
                _assetContainer.AddAnimation(visemeClip);
            }

            return visemeClips;
        }

        private List<AnimationClip> CreateRegularClips()
        {
            var visemeClips = Enumerable.Range(0, 15)
                .Select(visemeNumber =>
                {
                    var clip = new AnimationClip {name = "zAutogeneratedLipsync_ " + visemeNumber};
                    new VisemeAnimationMaker(_avatarDescriptor).OverrideAnimation(clip, visemeNumber, 1f);
                    return clip;
                })
                .ToList();
            foreach (var visemeClip in visemeClips)
            {
                _assetContainer.AddAnimation(visemeClip);
            }

            return visemeClips;
        }

        private float FindVisemeAmplitudeTweak(int visemeNumber)
        {
            switch (visemeNumber)
            {
                case 0: return _limitedLipsync.amplitude0;
                case 1: return _limitedLipsync.amplitude1;
                case 2: return _limitedLipsync.amplitude2;
                case 3: return _limitedLipsync.amplitude3;
                case 4: return _limitedLipsync.amplitude4;
                case 5: return _limitedLipsync.amplitude5;
                case 6: return _limitedLipsync.amplitude6;
                case 7: return _limitedLipsync.amplitude7;
                case 8: return _limitedLipsync.amplitude8;
                case 9: return _limitedLipsync.amplitude9;
                case 10: return _limitedLipsync.amplitude10;
                case 11: return _limitedLipsync.amplitude11;
                case 12: return _limitedLipsync.amplitude12;
                case 13: return _limitedLipsync.amplitude13;
                case 14: return _limitedLipsync.amplitude14;
                default: throw new IndexOutOfRangeException();
            }
        }

        private float FindVisemeTransitionTweak(int visemeNumber)
        {
            switch (visemeNumber)
            {
                case 0: return _limitedLipsync.transition0;
                case 1: return _limitedLipsync.transition1;
                case 2: return _limitedLipsync.transition2;
                case 3: return _limitedLipsync.transition3;
                case 4: return _limitedLipsync.transition4;
                case 5: return _limitedLipsync.transition5;
                case 6: return _limitedLipsync.transition6;
                case 7: return _limitedLipsync.transition7;
                case 8: return _limitedLipsync.transition8;
                case 9: return _limitedLipsync.transition9;
                case 10: return _limitedLipsync.transition10;
                case 11: return _limitedLipsync.transition11;
                case 12: return _limitedLipsync.transition12;
                case 13: return _limitedLipsync.transition13;
                case 14: return _limitedLipsync.transition14;
                default: throw new IndexOutOfRangeException();
            }
        }

        private AnimatorStateMachine ReinitializeLayer()
        {
            return _animatorGenerator.CreateOrRemakeLayerAtSameIndex(LipsyncLayerName, 1f, _logicalAvatarMask).ExposeMachine();
        }

        public static void Delete(AnimatorGenerator animatorGenerator)
        {
            animatorGenerator.RemoveLayerIfExists(LipsyncLayerName);
        }
    }
}
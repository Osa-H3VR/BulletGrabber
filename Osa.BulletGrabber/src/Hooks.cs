using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Deli.Setup;
using On.FistVR;
using UnityEngine;
using UnityEngine.SceneManagement;
using FVRPhysicalObject = FistVR.FVRPhysicalObject;
using FVRViveHand = FistVR.FVRViveHand;

namespace Osa.BulletGrabber
{
    public class Hooks
    {
        private readonly int _delay;
        private readonly float _range;
        private readonly string _hand;
        private readonly ManualLogSource _manualLogSource;

        public Hooks(int delay, float range, string hand, ManualLogSource manualLogSource)
        {
            _delay = delay;
            _range = range;
            _hand = hand;
            _manualLogSource = manualLogSource;
            Hook();
        }

        public void Dispose()
        {
            Unhook();
        }

        private void Unhook()
        {
        }

        private void Hook()
        {
            On.FistVR.FVRViveHand.RetrieveObject += FVRViveHandOnRetrieveObject;
            On.FistVR.FVRFireArmRound.UpdateInteraction += FVRFireArmRoundOnUpdateInteraction;

            if (_range > 3.2)
            {
                // TODO! Disable scoring when more than 3.2
            }
        }

        private void FVRFireArmRoundOnUpdateInteraction(FVRFireArmRound.orig_UpdateInteraction orig,
            FistVR.FVRFireArmRound self, FVRViveHand hand)
        {
            orig(self, hand);

            // Check if grabber is active
            if (_active)
            {
                // Check for hand mode
                if ((hand.IsThisTheRightHand && (_hand != "left")) || (!hand.IsThisTheRightHand && (_hand != "right")))
                {
                    if (self.HoveredOverRound == null)
                        return;

                    _manualLogSource.LogInfo("Palming the round");
                    self.PalmRound(self.HoveredOverRound, false, true);
                }
            }
        }

        private void FVRViveHandOnRetrieveObject(On.FistVR.FVRViveHand.orig_RetrieveObject orig,
            FistVR.FVRViveHand self, FVRPhysicalObject obj)
        {
            orig(self, obj);
            _manualLogSource.LogInfo($"Called");
            if (obj is FistVR.FVRFireArmRound round)
            {
                _manualLogSource.LogInfo($"Is round!");
                
                if (!round.isPalmable || round.MaxPalmedAmount <= 1)
                    return;
                
                FistVR.FVRFireArmRound[] array = UnityEngine.Object.FindObjectsOfType<FistVR.FVRFireArmRound>();
                
                _manualLogSource.LogInfo($"Found {array.Length} rounds");
                if (array.Length >= round.MaxPalmedAmount - 1)
                {
                    _manualLogSource.LogWarning($"Reached palm limit of {round.MaxPalmedAmount}!");
                }
                
                // Filter out bullets
                SortedList<float, FistVR.FVRFireArmRound> pickupAble = new SortedList<float, FistVR.FVRFireArmRound>();
                foreach (FistVR.FVRFireArmRound bulet in array)
                {
                    _manualLogSource.LogInfo($"Round type {bulet.RoundType} vs {round.RoundType}");
                    // Only find compatible and unspent ammo
                    if (bulet.RoundType == round.RoundType && !bulet.IsSpent)
                    {
                        _manualLogSource.LogInfo($"Round type is compatible");
                        // Check for rounds you dont want to select
                        if (!bulet.IsHeld && bulet.QuickbeltSlot == null)
                        {
                            // Check the distance
                            var distance = Vector3.Distance(round.Transform.position, bulet.Transform.position);
                            _manualLogSource.LogInfo($"Too far!");
                            if (distance < _range){
                                _manualLogSource.LogInfo($"Adding to the list");
                                pickupAble.Add(distance, bulet);
                            }
                        }
                    }
                }

                _manualLogSource.LogInfo($"Adding to palm {pickupAble.Count} rounds");
                _lastCall = DateTime.Now;
                AnvilManager.Run(GetBullets(pickupAble.Take(round.MaxPalmedAmount - 1).Select(x=>x.Value).ToList(), round));
            }
        }

        private DateTime _lastCall;
        private bool _active;

        private IEnumerator GetBullets(List<FistVR.FVRFireArmRound> list, FistVR.FVRFireArmRound round)
        {
            _active = true;

            foreach (FistVR.FVRFireArmRound armRound in list)
            {
                if (_lastCall + TimeSpan.FromMilliseconds(_delay) < DateTime.Now)
                {
                    // Wait more
                    yield return null;
                }
                else
                {
                    _lastCall = DateTime.Now;

                    round.HoveredOverRound = armRound;
                    yield return null;
                }
            }

            _active = false;
        }
    }
}
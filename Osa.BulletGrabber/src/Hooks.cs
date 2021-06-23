using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly string _buletGrab;

        private readonly ManualLogSource _manualLogSource;

        //This is the range of the pickup laser + 0.2m, so should be fair.
        public const float MaxLegitRange = 3.2f;

        public Hooks(int delay, float range, string hand, string buletGrab, ManualLogSource manualLogSource)
        {
            _delay = delay;
            _range = range;
            _hand = hand;
            _buletGrab = buletGrab;
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
            On.FistVR.TNH_ScoreDisplay.SubmitScoreAndGoToBoard += TNH_ScoreDisplayOnSubmitScoreAndGoToBoard;
        }

        private void TNH_ScoreDisplayOnSubmitScoreAndGoToBoard(TNH_ScoreDisplay.orig_SubmitScoreAndGoToBoard orig,
            FistVR.TNH_ScoreDisplay self, int score)
        {
            if (_range > MaxLegitRange)
            {
                _manualLogSource.LogWarning(
                    $"Configured range:{_range} is higher than maximum legit allowed: {MaxLegitRange}, TNH score will not be uploaded!");
            }
            else
            {
                orig(self, score);
            }
        }

        private void FVRFireArmRoundOnUpdateInteraction(FVRFireArmRound.orig_UpdateInteraction orig,
            FistVR.FVRFireArmRound self, FVRViveHand hand)
        {
            orig(self, hand);

            string currentHand = hand.IsThisTheRightHand ? "right" : "left";
            // Check if grabber is active
            if (_active[currentHand])
            {
                // Check for hand mode
                if ((hand.IsThisTheRightHand && (_hand != "left")) || (!hand.IsThisTheRightHand && (_hand != "right")))
                {
                    if (bulet == null)
                        return;

                    _manualLogSource.LogInfo("Palming the round");
                    self.PalmRound(bulet[currentHand], false, true);
                    bulet = null;
                }
                else
                {

                    _manualLogSource.LogWarning(
                        $"Cant palm round, HandMode is: {_hand} and current hand is {currentHand}!");
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
                string currentHand = self.IsThisTheRightHand ? "right" : "left";
                string otherHand = currentHand=="left" ? "right" : "left";
                string currentItemId = obj.ObjectWrapper.ItemID;

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
                SortedList<float, FistVR.FVRFireArmRound> pickupAbleSameType =
                    new SortedList<float, FistVR.FVRFireArmRound>();
                SortedList<float, FistVR.FVRFireArmRound> pickupAbleArmRounds =
                    new SortedList<float, FistVR.FVRFireArmRound>();
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
                            // Is it already being processed by the other hand?
                            if (!_alreadyTaken[otherHand].Contains(bulet))
                            {
                                // Check the distance
                                var distance = Vector3.Distance(round.Transform.position, bulet.Transform.position);

                                if (distance < _range)
                                {
                                    _manualLogSource.LogInfo($"Adding to the list");

                                    // Separated into the same bullets and the other types
                                    if (bulet.ObjectWrapper.ItemID == currentItemId && _buletGrab != "closest")
                                    {
                                        pickupAbleSameType.Add(distance, bulet);
                                    }
                                    else
                                    {
                                        pickupAbleArmRounds.Add(distance, bulet);
                                    }
                                }
                                else
                                {
                                    _manualLogSource.LogInfo($"Too far! {distance}>{_range}");
                                }
                            }
                        }
                    }
                }

                _manualLogSource.LogInfo($"Found: {pickupAbleSameType.Count} same rounds");
                _manualLogSource.LogInfo($"Found: {pickupAbleArmRounds.Count} compatible");

                List<FistVR.FVRFireArmRound>? selected;

                // This defines how it should work for different settings, 
                switch (_buletGrab)
                {
                    case "onlythesame":
                        selected = pickupAbleSameType.Take(round.MaxPalmedAmount - 1).Select(x => x.Value).ToList();
                        break;
                    case "firstthesame":
                        selected = pickupAbleSameType.Take(round.MaxPalmedAmount - 1).Select(x => x.Value).ToList();
                        //Not enough of the same type, add other, compatible bullets
                        if (selected.Count < round.MaxPalmedAmount - 1)
                        {
                            selected.AddRange(pickupAbleArmRounds.Take(round.MaxPalmedAmount - selected.Count)
                                .Select(x => x.Value).ToList());
                        }

                        break;
                    case "closest":
                        selected = pickupAbleArmRounds.Take(round.MaxPalmedAmount - 1).Select(x => x.Value).ToList();
                        break;
                    default:
                        throw new NotSupportedException($"BulletGrabMode of: {_buletGrab} is not supported!");
                }

                _manualLogSource.LogInfo($"Selected rounds: {selected.Count}");

                AnvilManager.Run(GetBullets(selected, currentHand));
            }
        }

        private Stopwatch? _watch;

        // For "left" and "right" hand correlattion
        private Dictionary<string, bool> _active = new Dictionary<string, bool>()
        {
            {"left", false},
            {"right", false}
        };

        private Dictionary<string,FistVR.FVRFireArmRound?> bulet =  new Dictionary<string, FistVR.FVRFireArmRound?>()
        {
            {"left", null},
            {"right", null}
        };
        
        // This is probably not needed, but if someone just spams it can select already selected bullets, so this should circumvent that.
        private Dictionary<string, List<FistVR.FVRFireArmRound>> _alreadyTaken = new Dictionary<string, List<FistVR.FVRFireArmRound>>()
        {
            {"left", new List<FistVR.FVRFireArmRound>(){}},
            {"right", new List<FistVR.FVRFireArmRound>(){}}
        };

        private IEnumerator GetBullets(List<FistVR.FVRFireArmRound> list, string hand)
        {
            _alreadyTaken[hand] = list;
            _watch = new Stopwatch();
            _active[hand] = true;
            _watch.Start();

            int i = 1;

            _manualLogSource.LogInfo($"Delay is set to: {_delay}");

            foreach (FistVR.FVRFireArmRound armRound in list)
            {
                while ((_watch.ElapsedMilliseconds < _delay || bulet != null) && _active[hand])
                {
                    _manualLogSource.LogInfo($"Not yet, time is:{_delay} vs {_watch.ElapsedMilliseconds} ");
                    // Wait more
                    yield return null;
                }
                
                // Secondary active check, just in case
                if (_active[hand])
                {
                    _manualLogSource.LogInfo($"Palmed round: {i}");
                    bulet[hand] = armRound;
                    i++;
                    _watch.Reset();
                    _watch.Start();
                    yield return null;
                }
            }

            _alreadyTaken[hand] = new List<FistVR.FVRFireArmRound>();
            _active[hand] = false;
            _watch.Reset();
        }
    }
}
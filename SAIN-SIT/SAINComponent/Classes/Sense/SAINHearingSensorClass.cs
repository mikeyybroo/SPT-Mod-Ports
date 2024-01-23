﻿using BepInEx.Logging;
using Comfort.Common;
using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINHearingSensorClass : SAINBase, ISAINClass
    {
        public SAINHearingSensorClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            Singleton<GClass595>.Instance.OnSoundPlayed += HearSound;
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            Singleton<GClass595>.Instance.OnSoundPlayed -= HearSound;
        }

        public void HearSound(IAIDetails player, Vector3 position, float power, AISoundType type)
        {
            if (BotOwner == null || SAIN == null) return;

            if (!SAIN.GameIsEnding)
            {
                EnemySoundHeard(player, position, power, type);
            }
        }

        private void EnemySoundHeard(IAIDetails player, Vector3 position, float power, AISoundType type)
        {
            if (player != null)
            {
                if (BotOwner.ProfileId == player.ProfileId)
                {
                    return;
                }

                if (CheckSoundHeardAfterModifiers(player, position, power, type, out float distance))
                {
                    bool gunsound = type == AISoundType.gun || type == AISoundType.silencedGun;

                    if (!BotOwner.BotsGroup.IsEnemy(player) && BotOwner.BotsGroup.Neutrals.ContainsKey(player))
                    {
                        //BotOwner.BotGroupClass.LastSoundsController.AddNeutralSound(player, DrawPosition);
                        return;
                    }

                    if (gunsound || IsSoundClose(distance))
                    {
                        if (distance < 5f)
                        {
                            try
                            {
                                // BotOwner.Memory.Spotted(false, null, null);
                                BotOwner.WeaponManager?.Stationary?.Spotted();
                            }
                            catch { }
                        }

                        ReactToSound(player, position, distance, true, type);
                    }
                }
            }
            else
            {
                float distance = (BotOwner.Transform.position - position).magnitude;
                ReactToSound(null, position, distance, distance < power, type);
            }
        }

        private bool CheckSoundHeardAfterModifiers(IAIDetails player, Vector3 position, float power, AISoundType type, out float distance)
        {
            float range = power;

            var globalHearing = GlobalSettings.Hearing;
            if (type == AISoundType.step)
            {
                range *= globalHearing.FootstepAudioMultiplier;
            }
            else
            {
                range *= globalHearing.GunshotAudioMultiplier;
            }
            range = Mathf.Round(range * 10) / 10;

            bool wasHeard = DoIHearSound(player, position, range, type, out distance, true);

            if (wasHeard)
            {
                if (SAIN.Equipment.HasEarPiece)
                {
                    range *= 1.33f;
                }
                else
                {
                    range *= 0.9f;
                }
                if (SAIN.Equipment.HasHeavyHelmet)
                {
                    range *= 0.66f;
                }
                if (SAIN.Memory.HealthStatus == ETagStatus.Dying)
                {
                    range *= 0.55f;
                }
                var move = Player.MovementContext;
                float speed = move.ClampedSpeed / move.MaxSpeed;
                if (Player.IsSprintEnabled && speed >= 0.9f)
                {
                    range *= 0.66f;
                }
                else if (speed > 0.66f)
                {
                    range *= 0.85f;
                }
                else if (speed <= 0.1f)
                {
                    range *= 1.25f;
                }

                range *= SAIN.Info.FileSettings.Core.HearingSense;

                range = Mathf.Round(range * 10f) / 10f;

                range = Mathf.Clamp(range, power / 5f, power * 2f);

                return DoIHearSound(player, position, range, type, out distance, false);
            }

            return false;
        }

        private void ReactToSound(IAIDetails person, Vector3 pos, float power, bool wasHeard, AISoundType type)
        {
            if (person != null && person.AIData.IsAI && BotOwner.BotsGroup.Contains(person.AIData.BotOwner))
            {
                return;
            }

            float bulletfeeldistance = 500f;
            Vector3 shooterDirection = BotOwner.Transform.position - pos;
            float shooterDistance = shooterDirection.magnitude;
            bool isGunSound = type == AISoundType.gun || type == AISoundType.silencedGun;
            bool bulletfelt = shooterDistance < bulletfeeldistance;

            float dispersion = (type == AISoundType.gun) ? shooterDistance / 15f : shooterDistance / 10f;
            float dispNum = EFTMath.Random(-dispersion, dispersion);
            Vector3 vector = new Vector3(pos.x + dispNum, pos.y, pos.z + dispNum);

            if (wasHeard)
            {
                try
                {
                    BotOwner.BotsGroup.AddPointToSearch(vector, power, BotOwner);
                }
                catch { }

                if (shooterDistance < BotOwner.Settings.FileSettings.Hearing.RESET_TIMER_DIST)
                {
                    BotOwner.LookData.ResetUpdateTime();
                }

                if (person != null && isGunSound)
                {
                    Vector3 to = vector + person.LookDirection;
                    bool soundclose = IsSoundClose(out var firedAtMe, vector, to, 10f);

                    if (soundclose && firedAtMe)
                    {
                        try
                        {
                            SAIN.Memory.UnderFireFromPosition = vector;
                            BotOwner.Memory.SetUnderFire(person);
                        }
                        catch (System.Exception) { }

                        if (shooterDistance > 50f)
                        {
                            SAIN.Talk.Say(EPhraseTrigger.SniperPhrase);
                        }
                    }
                }

                if (!BotOwner.Memory.GoalTarget.HavePlaceTarget() && BotOwner.Memory.GoalEnemy == null)
                {
                    try
                    {
                        BotOwner.BotsGroup.CalcGoalForBot(BotOwner);
                    }
                    catch { }
                    return;
                }
            }
            else if (person != null && isGunSound && bulletfelt)
            {
                Vector3 to = vector + person.LookDirection;
                bool soundclose = IsSoundClose(out var firedAtMe, vector, to, 10f);

                if (firedAtMe && soundclose)
                {
                    var estimate = GetEstimatedPoint(vector);

                    SAIN.Memory.UnderFireFromPosition = estimate;

                    try
                    {
                        BotOwner.BotsGroup.AddPointToSearch(estimate, 50f, BotOwner);
                    }
                    catch (System.Exception) { }
                }
            }
        }

        public LastHeardSound LastHeardSound { get; private set; }

        private Vector3 GetEstimatedPoint(Vector3 source)
        {
            Vector3 randomPoint = Random.onUnitSphere * (Vector3.Distance(source, BotOwner.Transform.position) / 5f);
            randomPoint.y = Mathf.Clamp(randomPoint.y, -5f, 5f);
            return source + randomPoint;
        }

        private bool IsSoundClose(out bool firedAtMe, Vector3 from, Vector3 to, float maxDist)
        {
            var projectionPoint = GetProjectionPoint(BotOwner.Position + Vector3.up, from, to);

            bool closeSound = (projectionPoint - BotOwner.Position).magnitude < maxDist;

            var direction = projectionPoint - from;

            firedAtMe = !Physics.Raycast(from, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);

            return closeSound;
        }

        public static Vector3 GetProjectionPoint(Vector3 p, Vector3 p1, Vector3 p2)
        {
            //CalculateRecoil the difference between the z-coordinates of points p1 and p2
            float num = p1.z - p2.z;

            //If the difference is 0, return a vector with the x-coordinate of point p and the y and z-coordinates of point p1
            if (num == 0f)
            {
                return new Vector3(p.x, p1.y, p1.z);
            }

            //CalculateRecoil the difference between the x-coordinates of points p1 and p2
            float num2 = p2.x - p1.x;

            //If the difference is 0, return a vector with the x-coordinate of point p1 and the y and z-coordinates of point p
            if (num2 == 0f)
            {
                return new Vector3(p1.x, p1.y, p.z);
            }

            //CalculateRecoil the values of num3, num4, and num5
            float num3 = p1.x * p2.z - p2.x * p1.z;
            float num4 = num2 * p.x - num * p.z;
            float num5 = -(num2 * num3 + num * num4) / (num2 * num2 + num * num);

            //Return a vector with the calculated x-coordinate, the y-coordinate of point p1, and the calculated z-coordinate
            return new Vector3(-(num3 + num2 * num5) / num, p1.y, num5);
        }

        private bool IsSoundClose(float d)
        {
            //Modify the close hearing and far hearing variables
            float closehearing = 10f;
            float farhearing = SAIN.Info.FileSettings.Hearing.MaxFootstepAudioDistance;

            //Check if the Distance is less than or equal to the close hearing
            if (d <= closehearing)
            {
                return true;
            }

            if (d > farhearing)
            {
                return false;
            }

            float num = farhearing - closehearing;

            //CalculateRecoil the difference between the Distance and close hearing
            float num2 = d - closehearing;

            //CalculateRecoil the ratio of the difference between the Distance and close hearing to the difference between the far hearing and close hearing
            float num3 = 1f - num2 / num;

            return EFTMath.Random(0f, 1f) < num3;
        }

        public bool DoIHearSound(IAIDetails enemy, Vector3 position, float power, AISoundType type, out float distance, bool withOcclusionCheck)
        {
            distance = (BotOwner.Transform.position - position).magnitude;

            // Is sound within hearing Distance at all?
            if (distance < power)
            {
                if (!withOcclusionCheck)
                {
                    return distance < power;
                }
                // if so, is sound blocked by obstacles?
                if (OcclusionCheck(enemy, position, power, distance, type, out float occludedpower))
                {
                    return distance < occludedpower;
                }
            }

            // Sound not heard
            distance = 0f;
            return false;
        }

        private bool OcclusionCheck(IAIDetails player, Vector3 position, float power, float distance, AISoundType type, out float occlusion)
        {
            // Raise up the vector3's to match head level
            Vector3 botheadpos = BotOwner.MyHead.position;
            //botheadpos.y += 1.3f;
            if (type == AISoundType.step)
            {
                position.y += 0.1f;
            }

            Vector3 direction = (botheadpos - position).normalized;
            float soundDistance = direction.magnitude;
            bool PMC = SAIN.Info.Profile.IsPMC;
            // Checks if something is within line of sight
            if (Physics.Raycast(botheadpos, direction, power, LayerMaskClass.HighPolyWithTerrainNoGrassMask))
            {
                if (soundDistance > 125f && !PMC)
                {
                    occlusion = 0f;
                    return false;
                }
                // If the sound source is the player, raycast and find number of collisions
                if (player.IsYourPlayer)
                {
                    // Check if the sound originates from an environment other than the BotOwner's
                    float environmentmodifier = EnvironmentCheck(player);

                    // Raycast check
                    float finalmodifier = RaycastCheck(botheadpos, position, environmentmodifier);

                    // Reduce occlusion for unsuppressed guns
                    if (type == AISoundType.gun) finalmodifier = Mathf.Sqrt(finalmodifier);

                    // Apply Modifier
                    occlusion = power * finalmodifier;

                    return distance < occlusion;
                }
                else
                {
                    // Only check environment for bots vs bots
                    occlusion = power * EnvironmentCheck(player);
                    return distance < occlusion;
                }
            }
            else
            {
                // Direct line of sight, no occlusion
                occlusion = distance;
                return false;
            }
        }

        private float EnvironmentCheck(IAIDetails enemy)
        {
            int botlocation = BotOwner.AIData.EnvironmentId;
            int enemylocation = enemy.AIData.EnvironmentId;
            return botlocation == enemylocation ? 1f : 0.66f;
        }

        public float RaycastCheck(Vector3 botpos, Vector3 enemypos, float environmentmodifier)
        {
            if (raycasttimer < Time.time)
            {
                raycasttimer = Time.time + 0.25f;

                occlusionmodifier = 1f;

                LayerMask mask = LayerMaskClass.HighPolyWithTerrainNoGrassMask;

                // AddColor a RaycastHit array and set it to the Physics.RaycastAll
                var direction = botpos - enemypos;
                RaycastHit[] hits = Physics.RaycastAll(enemypos, direction, direction.magnitude, mask);

                int hitCount = 0;

                // Loop through each hit in the hits array
                for (int i = 0; i < hits.Length; i++)
                {
                    // Check if the hitCount is 0
                    if (hitCount == 0)
                    {
                        // If the hitCount is 0, set the occlusionmodifier to 0.8f multiplied by the environmentmodifier
                        occlusionmodifier *= 0.75f * environmentmodifier;
                    }
                    else
                    {
                        // If the hitCount is not 0, set the occlusionmodifier to 0.95f multiplied by the environmentmodifier
                        occlusionmodifier *= 0.9f * environmentmodifier;
                    }

                    // Increment the hitCount
                    hitCount++;
                }
            }

            return occlusionmodifier;
        }

        private float occlusionmodifier = 1f;

        private float raycasttimer = 0f;

        public delegate void GDelegate4(Vector3 vector, float bulletDistance, AISoundType type);

        public event GDelegate4 OnEnemySounHearded;
    }
}
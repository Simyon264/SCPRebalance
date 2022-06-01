/*
 * Made by Simyon#6969 
 * ==============
 * Made for DayLight Gaming discord.gg/RxzaN3jGeb
 * 
 * This plugin adds: 
 *      - A multiplier for SCP-079 (Idea came from the Moderation staff)
 *      - Random events in the facility (Some doors will not close, some will open randomly or the light goes out for some time. (That Guy#3982)
 *      - A slowdown for SCP-939 on hit. (From staff team)
 * ==================================================================================================================================================================
*/
using System;
using Exiled.API.Interfaces;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.API.Enums;
using Handlers = Exiled.Events.Handlers;
using MEC;
using System.Collections.Generic;

namespace SCPRebalance
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        
        public bool Scp079ExpBoost { get; set; } = true;

        public float Scp079BoostMultiplier { get; set; } = 1.5f;

        public bool Scp939SlowdownOnHit { get; set; } = true;

        public bool RandomFacilityEvents { get; set; } = true;
        
        public float TimeBetweenEvents { get; set; } = 60f;

        public int RandomEventChance { get; set; } = 20;

        public string lightsOutCassieMessage { get; set; } = ".g7 Facility Power Failure. Systems are Reactivating.";

        public string LightsOutCassieNormalMessage { get; set; } = "Facility Back In Operation.";
        public int LightsOutMaxDuration { get; set; } = 30;
        public int LightsOutMinimumDuration { get; set; } = 10;
        public int MaxOpenedDoorsDoorEvent { get; set; } = 10;

        public int DoorFailChance { get; set; } = 10;
    }

    public class Plugin : Plugin<Config>
    {
        public override string Name { get; } = "SCPRebalance";
        public override string Prefix { get; } = "scpbalance";
        public override string Author { get; } = "Simyon";
        public override Version Version { get; } = new Version(1, 0, 0);
        public override PluginPriority Priority { get; } = PluginPriority.High;

        private bool EventActive = false;
        private Random random = new Random();

        private bool IsInChance(int chance)
        {
            if (random == null)
                random = new Random();
            if (random.Next(100) < chance)
            {
                return true;
            }
            return false;
        }

        public override void OnEnabled()
        {
            Handlers.Scp079.GainingExperience += OnSCP079Exp;
            Handlers.Player.Hurting += Scp939hit;
            Handlers.Server.RoundStarted += OnRoundStart;
            Handlers.Player.InteractingDoor += OnDoingTheDoorLenny;
            
            if (Round.IsStarted)
            {
                Timing.RunCoroutine(EventThing(), "EventThingLightsOutOrSomething");
            }

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Handlers.Scp079.GainingExperience -= OnSCP079Exp;
            Handlers.Player.Hurting -= Scp939hit;
            Handlers.Server.RoundStarted -= OnRoundStart;
            Handlers.Player.InteractingDoor -= OnDoingTheDoorLenny;

            Timing.KillCoroutines("EventThingLightsOutOrSomething");
            base.OnDisabled();
        }

        // Eventhandlers
        
        public void OnRoundStart()
        {
            Timing.RunCoroutine(EventThing(), "EventThingLightsOutOrSomething");
        }

        public void OnDoingTheDoorLenny(InteractingDoorEventArgs ev)
        {
            if (ev.Player.Role.Team == Team.SCP) return;
            if (IsInChance(Config.DoorFailChance))
            {
                ev.IsAllowed = false;
                ev.Door.Lock(random.Next(1, 5), DoorLockType.AdminCommand);
                ev.Door.PlaySound(DoorBeepType.PermissionDenied);
            }
        }

        public IEnumerator<float> EventThing()
        {
            for (;;)
            {
                yield return Timing.WaitForSeconds(Config.TimeBetweenEvents);
                if (!EventActive)
                {
                    if (IsInChance(Config.RandomEventChance))
                    {
                        if (IsInChance(50))
                        {
                            EventActive = true;
                            Log.Info("Lights Out Event");
                            // Lights out
                            int duration = random.Next(Config.LightsOutMinimumDuration, Config.LightsOutMaxDuration);
                            Cassie.Message(Config.lightsOutCassieMessage, false, true, true);
                            Timing.CallDelayed(Cassie.CalculateDuration(Config.lightsOutCassieMessage), () =>
                            {
                                Map.TurnOffAllLights(duration);
                            });
                            Timing.CallDelayed(duration, () =>
                            {
                                EventActive = false;
                                Cassie.Message(Config.LightsOutCassieNormalMessage, false, true, true);
                            });
                        } else
                        {
                            Log.Info("Random Doors Event");
                            EventActive = true;
                            int currentDoorsOpened = 0;
                            // Random door opens
                            foreach (Door door in Door.List)
                            {
                                if (!(currentDoorsOpened > Config.MaxOpenedDoorsDoorEvent))
                                {
                                    Door.Random(ZoneType.Unspecified, true).IsOpen = true;
                                    currentDoorsOpened++;
                                }
                            }
                            EventActive = false;
                        }
                    }
                } 
            }
        }

        public void OnSCP079Exp(GainingExperienceEventArgs ev)
        {
            if (Config.Scp079ExpBoost)
            {
                ev.Amount = (float)(ev.Amount * Config.Scp079BoostMultiplier);
            }
        }

        public void Scp939hit(HurtingEventArgs ev)
        {
            if (ev.Attacker == null) return;
            if (!Config.Scp939SlowdownOnHit) return;
            if (ev.Attacker.Role.Type == RoleType.Scp93953 || ev.Attacker.Role.Type == RoleType.Scp93989)
            {
                ev.Attacker.EnableEffect(EffectType.SinkHole, 3, true);
                ev.Attacker.EnableEffect(EffectType.Amnesia, 3, true);
            }
        }
    }
}

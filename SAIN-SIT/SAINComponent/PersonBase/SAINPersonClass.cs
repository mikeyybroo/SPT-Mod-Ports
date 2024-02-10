﻿using EFT;
using EFT.NextObservedPlayer;
using SAIN.SAINComponent.Classes.Info;
using System;
using UnityEngine;

namespace SAIN.SAINComponent.BaseClasses
{
    public class SAINPersonClass : PersonBaseClass, ISAINPerson
    {
        public SAINPersonClass(IPlayer person) : base(person)
        {
            Transform = new SAINPersonTransformClass(this);
            Profile = person.Profile;
            if (person?.AIData?.BotOwner != null)
            {
                SAIN = person.AIData.BotOwner.GetComponent<SAINComponentClass>();
            }
        }

        public void Update()
        {
            Transform.Update();
        }

        public bool IsActive => PlayerNull == false && (IsAI == false || BotOwner?.BotState == EBotState.Active);
        public Vector3 Position => Transform.Position;
        public SAINPersonTransformClass Transform { get; private set; }
        public Profile Profile { get; private set; }
        public string ProfileId => Profile?.ProfileId;
        public string Nickname => Profile?.Nickname;
        public string Name => Player?.name;
        public bool IsAI => BotOwner != null;
        public bool IsSAINBot => SAIN != null;
        public AIData AIData => IAIDetails?.AIData;
        public BotOwner BotOwner => AIData?.BotOwner;
        public SAINComponentClass SAIN { get; private set; }
        public SAINBotInfoClass SAINBotInfo => SAIN?.Info;
        public SAINSquadClass SAINSquad => SAIN?.Squad;
    }
}
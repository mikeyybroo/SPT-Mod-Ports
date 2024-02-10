﻿using EFT;
using EFT.NextObservedPlayer;
using SAIN.SAINComponent.Classes.Info;
using UnityEngine;

namespace SAIN.SAINComponent.BaseClasses
{
    public interface ISAINPerson
    {
        void Update();
        bool PlayerNull { get; }
        IPlayer IAIDetails { get; }
        Player Player { get; }
        Vector3 Position { get; }
        SAINPersonTransformClass Transform { get; }
        Profile Profile { get; }
        string ProfileId { get; }
        string Nickname { get; }
        string Name { get; }
        bool IsAI { get; }
        bool IsSAINBot { get; }
        AIData AIData { get; }
        BotOwner BotOwner { get; }
        SAINComponentClass SAIN { get; }
        SAINBotInfoClass SAINBotInfo { get; }
        SAINSquadClass SAINSquad { get; }
    }
}
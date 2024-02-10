using EFT;
using EFT.NextObservedPlayer;
using System;

namespace SAIN.SAINComponent.BaseClasses
{
    public abstract class PersonBaseClass
    {
        public PersonBaseClass(IPlayer iPlayer)
        {
            IAIDetails = iPlayer;
        }

        public IPlayer IAIDetails { get; private set; }
        public bool PlayerNull => IAIDetails == null;
        public Player Player => IAIDetails as Player;
    }
}
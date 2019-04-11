using AElf.Common;
using AElf.Sdk.CSharp.State;

namespace Ballot
{
    public class BallotState : ContractState
    {
        public MappedState<Address, Voter> Voters { get; set; }
        public MappedState<uint, Proposal> Proposals { get; set; }
        public UInt32State ProposalCount { get; set; }
        public SingletonState<Address> ChairPerson { get; set; }
    }
}
using System;
using AElf.Common;
using AElf.Sdk.CSharp;
using Ballot;
using Google.Protobuf.WellKnownTypes;

namespace Ballot
{
    public class Ballot : BallotContainer.BallotBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.ChairPerson.Value == null, "Already intialized.");
            State.ChairPerson.Value = Context.Sender;
            State.Voters[Context.Sender] = new Voter()
            {
                Weight = 1
            };
            var count = State.ProposalCount.Value;
            foreach (var name in input.ProposalNames)
            {
                State.Proposals[count] = new Proposal()
                {
                    Name = name,
                    VoteCount = 0
                };
                count = count.Add(1);
            }

            State.ProposalCount.Value = count;
            return new Empty();
        }

        public override Empty GiveRightToVote(Address input)
        {
            Assert(Context.Sender == State.ChairPerson.Value, "Only chairperson can give right to vote.");
            var voter = State.Voters[input] ?? new Voter();
            Assert(!voter.Voted, "The voter already voted.");
            Assert(voter.Weight == 0);
            voter.Weight = 1;
            State.Voters[input] = voter;
            return new Empty();
        }

        public override Empty Delegate(Address input)
        {
            var to = input;
            var sender = State.Voters[Context.Sender] ?? new Voter();
            Assert(!sender.Voted, "You already voted.");
            Assert(to != Context.Sender, "Self-delegation is disallowed.");
            var voter = State.Voters[to] ?? new Voter();
            while (voter.Delegate != null)
            {
                to = voter.Delegate;
                voter = State.Voters[to] ?? new Voter();
                Assert(to != Context.Sender, "Found loop in delegation.");
            }

            sender.Voted = true;
            sender.Delegate = to;
            State.Voters[Context.Sender] = sender;
            var delegateVoter = State.Voters[to];
            if (delegateVoter.Voted)
            {
                var proposal = State.Proposals[delegateVoter.Vote];
                proposal.VoteCount = proposal.VoteCount.Add(sender.Weight);
            }
            else
            {
                delegateVoter.Weight = delegateVoter.Weight.Add(sender.Weight);
                State.Voters[to] = delegateVoter;
            }

            return new Empty();
        }

        public override Empty Vote(UInt32Value input)
        {
            var proposal = input;
            var sender = State.Voters[Context.Sender] ?? new Voter();
            Assert(sender.Weight != 0, "Has no right to vote.");
            Assert(!sender.Voted, "Already voted.");
            sender.Voted = true;
            sender.Vote = proposal.Value;
            State.Voters[Context.Sender] = sender;
            var proposalRecord = State.Proposals[proposal.Value];
            proposalRecord.VoteCount = proposalRecord.VoteCount.Add(sender.Weight);
            State.Proposals[proposal.Value] = proposalRecord;
            return new Empty();
        }


        public override Proposal GetWinningProposal(Empty input)
        {
            var winningProposal = new Proposal();
            var proposalCount = State.ProposalCount.Value;
            for (uint p = 0; p < proposalCount; p++)
            {
                var proposal = State.Proposals[p];
                if (proposal.VoteCount > winningProposal.VoteCount)
                {
                    winningProposal = proposal;
                }
            }

            return winningProposal;
        }

        public override Address GetChainPerson(Empty input)
        {
            return State.ChairPerson.Value;
        }
    }
}
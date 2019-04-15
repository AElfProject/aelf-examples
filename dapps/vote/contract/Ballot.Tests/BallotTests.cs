using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace Ballot.Tests
{
    public class BallotTests : ContractTestBase<ContractTestModule>
    {
        private Address _contractAddress;

        private BallotContainer.BallotStub _defaultStub;

        #region Accounts

        private ECKeyPair DefaultSender { get; } = SampleECKeyPairs.KeyPairs[0];
        private ECKeyPair[] Voters { get; } = SampleECKeyPairs.KeyPairs.Where((kp, i) => i >= 1 && i <= 3).ToArray();
        private ECKeyPair NonChairPerson { get; } = SampleECKeyPairs.KeyPairs.Last();
        private ECKeyPair VoterOne => Voters[0];
        private ECKeyPair VoterTwo => Voters[1];
        private ECKeyPair VoterThree => Voters[2];

        #endregion


        private async Task PrepareAsync()
        {
            var category = 0;
            var code = File.ReadAllBytes(typeof(Ballot).Assembly.Location);
            _contractAddress = await DeployContractAsync(category, code, DefaultSender);
            _defaultStub = GetTester<BallotContainer.BallotStub>(_contractAddress, DefaultSender);
        }

        [Fact]
        public async Task Initialize_Success()
        {
            await PrepareAsync();
            var initialInput = new InitializeInput()
            {
                ProposalNames =
                {
                    "ProposalA", "ProposalB", "ProposalC"
                }
            };
            var init = await _defaultStub.Initialize.SendAsync(initialInput);
            init.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var chairPerson = await _defaultStub.GetChainPerson.CallAsync(new Empty());
            chairPerson.ShouldBe(DefaultSender.Address());

            var expected = new ProposalsData();
            foreach (var (name, i) in initialInput.ProposalNames.Select((n, i) => (n, i)))
            {
                expected.Proposals.Add(new Proposal()
                {
                    Id = (uint) i,
                    Name = name
                });
            }

            var proposals = await _defaultStub.GetProposals.CallAsync(new Empty());
            proposals.ShouldBe(expected);
        }

        [Fact]
        public async Task Initialize_Failed_DueTo_Reinitialize()
        {
            await Initialize_Success();
            var initialInput = new InitializeInput()
            {
                ProposalNames =
                {
                    "AnotherProposalA", "AnotherProposalB", "AnotherProposalC"
                }
            };
            var res = await _defaultStub.Initialize.SendAsync(initialInput);
            res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            res.TransactionResult.Error.ShouldContain("Already intialized.");
        }

        [Fact]
        public async Task GiveRight_Success()
        {
            await Initialize_Success();

            foreach (var v in Voters)
            {
                var res = await _defaultStub.GiveRightToVote.SendAsync(v.Address());
                res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var retrieved = await _defaultStub.GetVoter.CallAsync(v.Address());
                retrieved.ShouldNotBe(new Voter());
            }
        }

        [Fact]
        public async Task GiveRight_Failed_DueTo_NonChairPerson()
        {
            await PrepareAsync();
            var nonChairPersonStub = GetTester<BallotContainer.BallotStub>(_contractAddress, NonChairPerson);
            var voter = Voters.First().Address();
            var res = await nonChairPersonStub.GiveRightToVote.SendAsync(voter);
            res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            res.TransactionResult.Error.ShouldContain("Only chairperson can give right to vote.");
        }

        [Fact]
        public async Task GiveRight_Failed_DueTo_Regive()
        {
            await Vote_Success();

            // voter1 already votes, cannot give right again
            var res = await _defaultStub.GiveRightToVote.SendAsync(VoterOne.Address());
            res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            res.TransactionResult.Error.ShouldContain("The voter already voted.");
        }

        [Fact]
        public async Task Vote_Success()
        {
            await GiveRight_Success();
            // voter1 votes for proposal 0 and voter2 and voter3 vote for proposal 1
            foreach (var (v, p) in Voters.Zip(new[] {0u, 1u, 1u}, Tuple.Create))
            {
                var voterStub = GetTester<BallotContainer.BallotStub>(_contractAddress, v);
                var res = await voterStub.Vote.SendAsync(new UInt32Value()
                {
                    Value = p
                });
                res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var proposalsData = await _defaultStub.GetProposals.CallAsync(new Empty());
            var p0 = proposalsData.Proposals.Single(p => p.Id == 0);
            p0.VoteCount.ShouldBe(1u);
            var p1 = proposalsData.Proposals.Single(p => p.Id == 1);
            p1.VoteCount.ShouldBe(2u);
        }

        [Fact]
        public async Task Vote_Failed_DueTo_Revote()
        {
            await GiveRight_Success();
            var voterOneStub = GetTester<BallotContainer.BallotStub>(_contractAddress, VoterOne);
            var res1 = await voterOneStub.Vote.SendAsync(new UInt32Value() {Value = 1});
            res1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var res2 = await voterOneStub.Vote.SendAsync(new UInt32Value() {Value = 2});
            res2.TransactionResult.Error.ShouldContain("Already voted.");
        }

        [Fact]
        public async Task Vote_Failed_DueTo_NoRight()
        {
            await GiveRight_Success();
            var nonVoter = SampleECKeyPairs.KeyPairs[50];
            var nonVoterStub = GetTester<BallotContainer.BallotStub>(_contractAddress, nonVoter);
            var res = await nonVoterStub.Vote.SendAsync(new UInt32Value() {Value = 1});
            res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            res.TransactionResult.Error.ShouldContain("Has no right to vote");
        }

        [Fact]
        public async Task Delegate_Success()
        {
            // voter1 -> voter2 -> voter3
            // voter1 (voted) -> voter2 (voted) -> voter3 (weight 3)
            await GiveRight_Success();
            {
                // voter1 delegates to voter2
                var voterOneStub = GetTester<BallotContainer.BallotStub>(_contractAddress, VoterOne);
                var res = await voterOneStub.Delegate.SendAsync(VoterTwo.Address());
                res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                // after delegation, v1's status will be Voted and its delegate will be VoterTwo, and
                // v2's weight will increased to 2
                var v1 = await _defaultStub.GetVoter.CallAsync(VoterOne.Address());
                v1.Voted.ShouldBeTrue();
                v1.Delegate.ShouldBe(VoterTwo.Address());
                var v2 = await _defaultStub.GetVoter.CallAsync(VoterTwo.Address());
                v2.Weight.ShouldBe(2u);
            }
            {
                // voter2 delegates to voter3
                var voterTwoStub = GetTester<BallotContainer.BallotStub>(_contractAddress, VoterTwo);
                var res = await voterTwoStub.Delegate.SendAsync(VoterThree.Address());
                res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                // after delegation, v2's status will be Voted and its delegate will be VoterThree, and
                // v3's weight will increased to 3 (by 2 which is v2's weight)
                var v2 = await _defaultStub.GetVoter.CallAsync(VoterTwo.Address());
                v2.Voted.ShouldBeTrue();
                v2.Delegate.ShouldBe(VoterThree.Address());
                var v3 = await _defaultStub.GetVoter.CallAsync(VoterThree.Address());
                v3.Weight.ShouldBe(3u);
            }
            {
                // voter3 votes for proposal id 2 with weight 3
                var voterThreeStub = GetTester<BallotContainer.BallotStub>(_contractAddress, VoterThree);
                await voterThreeStub.Vote.SendAsync(new UInt32Value() {Value = 2});
                var proposalsData = await _defaultStub.GetProposals.CallAsync(new Empty());
                var p2 = proposalsData.Proposals.Single(p => p.Id == 2);
                p2.VoteCount.ShouldBe(3u);
            }
        }

        [Fact]
        public async Task Delegate_Failed_DueTo_SelfDelegation()
        {
            await GiveRight_Success();
            // voter1 delegates to voter1 (fail dur to self delegation)
            var voterOneStub = GetTester<BallotContainer.BallotStub>(_contractAddress, VoterOne);
            var res = await voterOneStub.Delegate.SendAsync(VoterOne.Address());
            res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            res.TransactionResult.Error.ShouldContain("Self-delegation is disallowed.");
        }


        [Fact]
        public async Task Delegate_Failed_DueTo_CircularDelegation()
        {
            await GiveRight_Success();
            // voter1 delegates to voter2, voter2 delegates back to voter1 (not allowed)
            var voterOneStub = GetTester<BallotContainer.BallotStub>(_contractAddress, VoterOne);
            var res1 = await voterOneStub.Delegate.SendAsync(VoterTwo.Address());
            res1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var voterTwoStub = GetTester<BallotContainer.BallotStub>(_contractAddress, VoterTwo);
            var res2 = await voterTwoStub.Delegate.SendAsync(VoterOne.Address());
            res2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            res2.TransactionResult.Error.ShouldContain("Found loop in delegation.");
        }
    }
}
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace Ballot.Tests
{
    public class BallotTests:ContractTestBase<ContractTestModule>
    {
        protected Address ContractAddress;

        internal BallotContainer.BallotStub DefaultStub;

        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        
        protected async Task PrepareAsync()
        {
            var category = 0;
            var code = File.ReadAllBytes(typeof(Ballot).Assembly.Location);
            ContractAddress = await DeployContractAsync(category, code, DefaultSenderKeyPair);
            DefaultStub = GetTester<BallotContainer.BallotStub>(ContractAddress, DefaultSenderKeyPair);
        }

        [Fact]
        public async Task Initialize_Success()
        {
            await PrepareAsync();
            var init = await DefaultStub.Initialize.SendAsync(new InitializeInput()
            {
                ProposalNames =
                {
                    "ProposalA", "ProposalB", "ProposalC"
                }
            });
            init.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var chairPerson = await DefaultStub.GetChainPerson.CallAsync(new Empty());
            chairPerson.ShouldBe(DefaultSender);
        }
    }
}
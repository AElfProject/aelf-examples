using AElf.Common;
using AElf.Cryptography.ECDSA;

namespace Ballot.Tests
{
    public static class AddressExtensions
    {
        public static Address Address(this ECKeyPair kp)
        {
            return AElf.Common.Address.FromPublicKey(kp.PublicKey);
        }
    }
}
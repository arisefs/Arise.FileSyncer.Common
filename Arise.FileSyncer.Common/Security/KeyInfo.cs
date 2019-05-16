using System.Security.Cryptography;

namespace Arise.FileSyncer.Common.Security
{
    public class KeyInfo
    {
        // Public Key Info
        public byte[] Modulus { get; set; }
        public byte[] Exponent { get; set; }

        // Private Key Info
        private byte[] D { get; set; }
        private byte[] DP { get; set; }
        private byte[] DQ { get; set; }
        private byte[] InverseQ { get; set; }
        private byte[] P { get; set; }
        private byte[] Q { get; set; }

        public KeyInfo() { }

        public RSAParameters GetParameters()
        {
            return new RSAParameters
            {
                Modulus = Modulus,
                Exponent = Exponent,
                D = D,
                DP = DP,
                DQ = DQ,
                InverseQ = InverseQ,
                P = P,
                Q = Q,
            };
        }

        public static KeyInfo Generate()
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                RSAParameters rsaKeyInfo = rsa.ExportParameters(true);

                return new KeyInfo()
                {
                    Modulus = rsaKeyInfo.Modulus,
                    Exponent = rsaKeyInfo.Exponent,
                    D = rsaKeyInfo.D,
                    DP = rsaKeyInfo.DP,
                    DQ = rsaKeyInfo.DQ,
                    InverseQ = rsaKeyInfo.InverseQ,
                    P = rsaKeyInfo.P,
                    Q = rsaKeyInfo.Q,
                };
            }
        }
    }
}

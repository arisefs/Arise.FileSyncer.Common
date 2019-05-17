using System.Security.Cryptography;

namespace Arise.FileSyncer.Common.Security
{
    public class KeyInfo
    {
        // Public Key Info
        public byte[] Modulus { get; set; }
        public byte[] Exponent { get; set; }

        // Private Key Info
        public byte[] D { get; set; }
        public byte[] DP { get; set; }
        public byte[] DQ { get; set; }
        public byte[] InverseQ { get; set; }
        public byte[] P { get; set; }
        public byte[] Q { get; set; }

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

        public bool Check()
        {
            return Modulus != null
                && Exponent != null
                && D != null
                && DP != null
                && DQ != null
                && InverseQ != null
                && P != null
                && Q != null;
        }

        public static KeyInfo Generate(int keySize)
        {
            using (var rsa = new RSACryptoServiceProvider(keySize))
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

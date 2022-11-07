using System;
using System.IO;
using System.Security.Cryptography;
using Arise.FileSyncer.Serializer;

namespace Arise.FileSyncer.Common.Security
{
    public class KeyInfo : IBinarySerializable
    {
        // Public Key Info
        public byte[]? Modulus { get; set; }
        public byte[]? Exponent { get; set; }

        // Private Key Info
        private byte[]? D;
        private byte[]? DP;
        private byte[]? DQ;
        private byte[]? InverseQ;
        private byte[]? P;
        private byte[]? Q;

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

        /// <summary>
        /// Checks the key if it has any mising data
        /// </summary>
        /// <returns>The key has no issues</returns>
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

        /// <summary>
        /// Generates a new key using the specified size
        /// </summary>
        /// <param name="keySize">The RSA key size</param>
        /// <returns>Key</returns>
        public static KeyInfo Generate(int keySize)
        {
            using var rsa = new RSACryptoServiceProvider(keySize);
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

        public void Deserialize(Stream stream)
        {
            Modulus = stream.ReadByteArray();
            Exponent = stream.ReadByteArray();
            D = stream.ReadByteArray();
            DP = stream.ReadByteArray();
            DQ = stream.ReadByteArray();
            InverseQ = stream.ReadByteArray();
            P = stream.ReadByteArray();
            Q = stream.ReadByteArray();
        }

        public void Serialize(Stream stream)
        {
            stream.WriteAFS(Modulus ?? throw new NullReferenceException("Modulus is null"));
            stream.WriteAFS(Exponent ?? throw new NullReferenceException("Exponent is null"));
            stream.WriteAFS(D ?? throw new NullReferenceException("D is null"));
            stream.WriteAFS(DP ?? throw new NullReferenceException("DP is null"));
            stream.WriteAFS(DQ ?? throw new NullReferenceException("DQ is null"));
            stream.WriteAFS(InverseQ ?? throw new NullReferenceException("InverseQ is null"));
            stream.WriteAFS(P ?? throw new NullReferenceException("P is null"));
            stream.WriteAFS(Q ?? throw new NullReferenceException("Q is null"));
        }
    }
}

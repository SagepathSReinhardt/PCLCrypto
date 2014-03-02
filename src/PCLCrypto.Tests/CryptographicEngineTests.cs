﻿namespace PCLCrypto.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PCLTesting;

    [TestClass]
    public class CryptographicEngineTests
    {
        private readonly byte[] data = new byte[] { 0x3, 0x5, 0x8 };
        private readonly ICryptographicKey rsaKey = WinRTCrypto.AsymmetricKeyAlgorithmProvider
            .OpenAlgorithm(AsymmetricAlgorithm.RsaSignPkcs1Sha1)
            .CreateKeyPair(512);

        private readonly ICryptographicKey macKey = WinRTCrypto.MacAlgorithmProvider
            .OpenAlgorithm(MacAlgorithm.HmacSha1)
            .CreateKey(new byte[] { 0x2, 0x4, 0x6 });

        private readonly ICryptographicKey aesKey = WinRTCrypto.SymmetricKeyAlgorithmProvider
            .OpenAlgorithm(SymmetricAlgorithm.AesCbcPkcs7)
            .CreateSymmetricKey(Convert.FromBase64String("T1kMUiju2rHiRyhJKfo/Jg=="));

        [TestMethod]
        public void Sign_NullInputs()
        {
            ExceptionAssert.Throws<ArgumentNullException>(
                () => WinRTCrypto.CryptographicEngine.Sign(null, this.data));
            ExceptionAssert.Throws<ArgumentNullException>(
                () => WinRTCrypto.CryptographicEngine.Sign(this.rsaKey, null));
        }

        [TestMethod]
        public void VerifySignature_NullInputs()
        {
            ExceptionAssert.Throws<ArgumentNullException>(
                () => WinRTCrypto.CryptographicEngine.VerifySignature(null, this.data, new byte[2]));
            ExceptionAssert.Throws<ArgumentNullException>(
                () => WinRTCrypto.CryptographicEngine.VerifySignature(this.rsaKey, null, new byte[2]));
            ExceptionAssert.Throws<ArgumentNullException>(
                () => WinRTCrypto.CryptographicEngine.VerifySignature(this.rsaKey, this.data, null));
        }

        [TestMethod]
        public void SignAndVerifySignatureRsa()
        {
            byte[] signature = WinRTCrypto.CryptographicEngine.Sign(this.rsaKey, this.data);
            Assert.IsTrue(WinRTCrypto.CryptographicEngine.VerifySignature(this.rsaKey, this.data, signature));
        }

        [TestMethod]
        public void SignatureAndVerifyTamperedSignatureRsa()
        {
            byte[] signature = WinRTCrypto.CryptographicEngine.Sign(this.rsaKey, this.data);

            // Tamper with the signature.
            signature[signature.Length - 1] += 1;
            Assert.IsFalse(WinRTCrypto.CryptographicEngine.VerifySignature(this.rsaKey, this.data, signature));
        }

        [TestMethod]
        public void SignatureAndVerifyTamperedDataRsa()
        {
            byte[] signature = WinRTCrypto.CryptographicEngine.Sign(this.rsaKey, this.data);

            // Tamper with the data.
            byte[] tamperedData = new byte[this.data.Length];
            Array.Copy(this.data, tamperedData, this.data.Length);
            tamperedData[tamperedData.Length - 1] += 1;
            Assert.IsFalse(WinRTCrypto.CryptographicEngine.VerifySignature(this.rsaKey, tamperedData, signature));
        }

        [TestMethod]
        public void SignAndVerifySignatureMac()
        {
            byte[] signature = WinRTCrypto.CryptographicEngine.Sign(this.macKey, this.data);
            Assert.IsTrue(WinRTCrypto.CryptographicEngine.VerifySignature(this.macKey, this.data, signature));
        }

        [TestMethod]
        public void SignatureAndVerifyTamperedSignatureMac()
        {
            byte[] signature = WinRTCrypto.CryptographicEngine.Sign(this.macKey, this.data);

            // Tamper with the signature.
            signature[signature.Length - 1] += 1;
            Assert.IsFalse(WinRTCrypto.CryptographicEngine.VerifySignature(this.macKey, this.data, signature));
        }

        [TestMethod]
        public void SignatureAndVerifyTamperedDataMac()
        {
            byte[] signature = WinRTCrypto.CryptographicEngine.Sign(this.macKey, this.data);

            // Tamper with the data.
            byte[] tamperedData = new byte[this.data.Length];
            Array.Copy(this.data, tamperedData, this.data.Length);
            tamperedData[tamperedData.Length - 1] += 1;
            Assert.IsFalse(WinRTCrypto.CryptographicEngine.VerifySignature(this.macKey, tamperedData, signature));
        }

        [TestMethod]
        public void Encrypt_InvalidInputs()
        {
            ExceptionAssert.Throws<ArgumentNullException>(
                () => WinRTCrypto.CryptographicEngine.Encrypt(null, this.data, null));
            ExceptionAssert.Throws<ArgumentNullException>(
                () => WinRTCrypto.CryptographicEngine.Encrypt(this.aesKey, null, null));
        }

        [TestMethod]
        public void Decrypt_InvalidInputs()
        {
            ExceptionAssert.Throws<ArgumentNullException>(
                () => WinRTCrypto.CryptographicEngine.Decrypt(null, this.data, null));
            ExceptionAssert.Throws<ArgumentNullException>(
                () => WinRTCrypto.CryptographicEngine.Decrypt(this.aesKey, null, null));
        }

        [TestMethod]
        public void EncryptAndDecrypt()
        {
            byte[] cipherText = WinRTCrypto.CryptographicEngine.Encrypt(this.aesKey, this.data, null);
            CollectionAssertEx.AreNotEqual(this.data, cipherText);
            Assert.AreEqual("oCSAA4sUCGa5ukwSJdeKWw==", Convert.ToBase64String(cipherText));
            byte[] plainText = WinRTCrypto.CryptographicEngine.Decrypt(this.aesKey, cipherText, null);
            CollectionAssertEx.AreEqual(this.data, plainText);
        }
    }
}

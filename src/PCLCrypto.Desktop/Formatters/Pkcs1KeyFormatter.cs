﻿//-----------------------------------------------------------------------
// <copyright file="Pkcs1KeyFormatter.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace PCLCrypto.Formatters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Validation;

    /// <summary>
    /// Encodes/decodes public keys and private keys in the PKCS#1 format
    /// (rsaPublicKey and rsaPrivateKey).
    /// </summary>
    /// <remarks>
    /// http://tools.ietf.org/html/rfc3447#page-46
    /// </remarks>
    internal class Pkcs1KeyFormatter : KeyFormatter
    {
        /// <summary>
        /// If set to <c>true</c> certain parameters will have a 0x00 prepended to their binary representations: Modulus, P, Q, DP, InverseQ.
        /// </summary>
        private readonly bool prependLeadingZeroOnCertainElements;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pkcs1KeyFormatter"/> class.
        /// </summary>
        /// <param name="prependLeadingZeroOnCertainElements">If set to <c>true</c> certain parameters will have a 0x00 prepended to their binary representations: Modulus, P, Q, DP, InverseQ.</param>
        internal Pkcs1KeyFormatter(bool prependLeadingZeroOnCertainElements = false)
        {
            this.prependLeadingZeroOnCertainElements = prependLeadingZeroOnCertainElements;
        }

        protected override RSAParameters ReadCore(Stream stream)
        {
            var keyBlobElement = Asn.ReadAsn1Elements(stream).First();
            VerifyFormat(
                keyBlobElement.Class == Asn.BerClass.Universal &&
                keyBlobElement.PC == Asn.BerPC.Constructed &&
                keyBlobElement.Tag == Asn.BerTag.Sequence);

            stream = new MemoryStream(keyBlobElement.Content);
            var sequence = Asn.ReadAsn1Elements(stream).ToList();

            switch (sequence.Count)
            {
                case 2:
                    return new RSAParameters
                    {
                        Modulus = TrimLeadingZero(sequence[0].Content),
                        Exponent = TrimLeadingZero(sequence[1].Content),
                    };
                case 9:
                    VerifyFormat(sequence[0].Content.Length == 1 && sequence[0].Content[0] == 0, "Unsupported version.");
                    return new RSAParameters
                    {
                        Modulus = TrimLeadingZero(sequence[1].Content),
                        Exponent = TrimLeadingZero(sequence[2].Content),
                        D = TrimLeadingZero(sequence[3].Content),
                        P = TrimLeadingZero(sequence[4].Content),
                        Q = TrimLeadingZero(sequence[5].Content),
                        DP = TrimLeadingZero(sequence[6].Content),
                        DQ = TrimLeadingZero(sequence[7].Content),
                        InverseQ = TrimLeadingZero(sequence[8].Content),
                    };
                default:
                    throw FailFormat();
            }
        }

        protected override void WriteCore(Stream stream, RSAParameters value)
        {
            Requires.NotNull(stream, "stream");

            var sequence = new MemoryStream();

            if (HasPrivateKey(value))
            {
                // Only include the version element if this is a private key.
                sequence.WriteAsn1Element(new Asn.DataElement(Asn.BerClass.Universal, Asn.BerPC.Primitive, Asn.BerTag.Integer, new byte[1]));
            }

            sequence.WriteAsn1Element(new Asn.DataElement(Asn.BerClass.Universal, Asn.BerPC.Primitive, Asn.BerTag.Integer, this.prependLeadingZeroOnCertainElements ? PrependLeadingZero(value.Modulus) : value.Modulus));
            sequence.WriteAsn1Element(new Asn.DataElement(Asn.BerClass.Universal, Asn.BerPC.Primitive, Asn.BerTag.Integer, value.Exponent));
            if (HasPrivateKey(value))
            {
                sequence.WriteAsn1Element(new Asn.DataElement(Asn.BerClass.Universal, Asn.BerPC.Primitive, Asn.BerTag.Integer, value.D));
                sequence.WriteAsn1Element(new Asn.DataElement(Asn.BerClass.Universal, Asn.BerPC.Primitive, Asn.BerTag.Integer, this.prependLeadingZeroOnCertainElements ? PrependLeadingZero(value.P) : value.P));
                sequence.WriteAsn1Element(new Asn.DataElement(Asn.BerClass.Universal, Asn.BerPC.Primitive, Asn.BerTag.Integer, this.prependLeadingZeroOnCertainElements ? PrependLeadingZero(value.Q) : value.Q));
                sequence.WriteAsn1Element(new Asn.DataElement(Asn.BerClass.Universal, Asn.BerPC.Primitive, Asn.BerTag.Integer, this.prependLeadingZeroOnCertainElements ? PrependLeadingZero(value.DP) : value.DP));
                sequence.WriteAsn1Element(new Asn.DataElement(Asn.BerClass.Universal, Asn.BerPC.Primitive, Asn.BerTag.Integer, value.DQ));
                sequence.WriteAsn1Element(new Asn.DataElement(Asn.BerClass.Universal, Asn.BerPC.Primitive, Asn.BerTag.Integer, this.prependLeadingZeroOnCertainElements ? PrependLeadingZero(value.InverseQ) : value.InverseQ));
            }

            stream.WriteAsn1Element(new Asn.DataElement(Asn.BerClass.Universal, Asn.BerPC.Constructed, Asn.BerTag.Sequence, sequence.ToArray()));
        }
    }
}
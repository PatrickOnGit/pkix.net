﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PKI.Exceptions;
using PKI.Utils;
using SysadminsLV.Asn1Parser;
using SysadminsLV.Asn1Parser.Universal;

namespace SysadminsLV.PKI.Utils.CLRExtensions {
    /// <summary>
    /// Contains extension methods for <see cref="X509Extension"/> class.
    /// </summary>
    public static class X509ExtensionExtensions {
        /// <summary>
        /// Encodes current extension to ASN.1-encoded byte array.
        /// </summary>
        /// <param name="extension">Extension to encode.</param>
        /// <exception cref="ArgumentNullException"><strong>extension</strong> parameter is null.</exception>
        /// <exception cref="UninitializedObjectException">Extension object is not properly initialized.</exception>
        /// <returns></returns>
        public static Byte[] Encode(this X509Extension extension) {
            if (extension == null) {
                throw new ArgumentNullException(nameof(extension));
            }
            if (String.IsNullOrEmpty(extension.Oid.Value)) {
                throw new UninitializedObjectException();
            }
            List<Byte> rawData = new List<Byte>(Asn1Utils.EncodeObjectIdentifier(extension.Oid));
            if (extension.Critical) {
                rawData.AddRange(Asn1Utils.EncodeBoolean(true));
            }

            rawData.AddRange(Asn1Utils.Encode(extension.RawData, (Byte)Asn1Type.OCTET_STRING));
            return Asn1Utils.Encode(rawData.ToArray(), 48);
        }
        /// <summary>
        /// Decodes ASN.1-encoded byte array to an instance of <see cref="X509Extension"/> class.
        /// </summary>
        /// <param name="rawData">ASN.1-encoded byte array that represents full extension information.</param>
        /// <exception cref="ArgumentNullException"><strong>rawData</strong> parameter is null.</exception>
        /// <exception cref="Asn1InvalidTagException">Decoder encountered an unexpected ASN.1 type identifier.</exception>
        /// <returns>Decoded extension object.</returns>
        public static X509Extension Decode(Byte[] rawData) {
            if (rawData == null) { throw new ArgumentNullException(nameof(rawData)); }

            Asn1Reader asn = new Asn1Reader(rawData);
            if (asn.Tag != 48) { throw new Asn1InvalidTagException(asn.Offset); }

            asn.MoveNext();
            if (asn.Tag != (Byte)Asn1Type.OBJECT_IDENTIFIER) { throw new Asn1InvalidTagException(asn.Offset); }

            Oid oid = new Asn1ObjectIdentifier(asn).Value;
            Boolean critical = false;
            asn.MoveNext();
            if (asn.Tag == (Byte)Asn1Type.BOOLEAN) {
                critical = Asn1Utils.DecodeBoolean(asn.GetTagRawData());
                asn.MoveNext();
            }
            if (asn.Tag != (Byte)Asn1Type.OCTET_STRING) { throw new Asn1InvalidTagException(asn.Offset); }

            return CryptographyUtils.ConvertExtension(new X509Extension(oid, asn.GetPayload(), critical));
        }
    }
}

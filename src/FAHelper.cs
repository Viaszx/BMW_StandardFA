using System;
using System.Text;
using System.Linq;

namespace StandardFA
{
	public class FAHelper
	{
		private static string BitArray6BitDecode(byte[] pBytes, int pBitOffset, int pDigits, bool pCheckEOL)
		{
			StringBuilder sb = new StringBuilder(pDigits);
			int bitLength = pDigits * 6;
			int sixbit = 0;
			for (int i = 0; i < bitLength; i++) {
				if (i > 0 && i % 6 == 0) {
					AppendDigit(sb, sixbit);
				}

				byte b = pBytes[(pBitOffset + i) / 8];
				int bitPos = 7 - (pBitOffset + i) % 8;
				int mask = 1 << bitPos;
				int bit = b & mask;
				sixbit <<= 1;
				sixbit |= (bit > 0 ? 1 : 0);

				if (pCheckEOL && i == 1 && sixbit == 0) {
					return null;
				}
			}

			AppendDigit(sb, sixbit);
			return sb.ToString();
		}

		private static void AppendDigit(StringBuilder pSB, int pSixBit)
		{
			int prefix = (pSixBit & 0x30) >> 4;

			int lowNibble = pSixBit & 0xF;
			int highNibble;
			if (prefix == 1) {
				highNibble = 48;
			} else if (prefix == 2) {
				highNibble = 64;
			} else if (prefix == 3) {
				highNibble = 80;
			} else {
				highNibble = 0;
			}
			int digit = highNibble + lowNibble;
			pSB.Append((char)digit);
		}

		private static string BitArraySingleBitDecode(byte[] pBytes, int pBitOffset, int pDigits, bool pCheckEOL)
		{
			StringBuilder sb = new StringBuilder(pDigits);
			for (int i = 0; i < pDigits; i++) {
				byte b = pBytes[(pBitOffset + i) / 8];
				int bitPos = 7 - (pBitOffset + i) % 8;
				int mask = 1 << bitPos;
				int bit = b & mask;
				sb.Append(bit != 0 ? '1' : '0');
				if (pCheckEOL && i == 1) {
					return null;
				}
			}
			return sb.ToString();
		}

		public static void DecodingStream(byte[] pFA, FAData faData)
		{
			int bitOffset = 0;

			faData.FaVersion = pFA[0].ToString("X2");
			bitOffset += 8;

			int digits = 4;
			faData.Zeitkriterium = BitArray6BitDecode(pFA, bitOffset, digits, false);
			bitOffset += digits * 6;

			faData.Series = BitArray6BitDecode(pFA, bitOffset, digits, false);
			bitOffset += digits * 6;

			faData.Type = BitArray6BitDecode(pFA, bitOffset, digits, false);
			bitOffset += digits * 6;

			faData.Lackcode = BitArray6BitDecode(pFA, bitOffset, digits, false);
			bitOffset += digits * 6;

			faData.Polstercode = BitArray6BitDecode(pFA, bitOffset, digits, false);
			bitOffset += digits * 6;

			string code = BitArraySingleBitDecode(pFA, bitOffset, 4, false);
			bitOffset += 4;

			if (code == "1000") {
				string salapa;
				while ((salapa = BitArray6BitDecode(pFA, bitOffset, 3, true)) != null) {
					faData.Salapas.Add(salapa);
					bitOffset += 18;
				}
				bitOffset += 2;
			}

			code = BitArraySingleBitDecode(pFA, bitOffset, 4, false);
			bitOffset += 4;
			
			if (code == "0100") {
				string eWord;
				while ((eWord = BitArray6BitDecode(pFA, bitOffset, 4, true)) != null) {
					faData.EWords.Add(eWord);
					bitOffset += 24;
				}
				bitOffset += 2;
			}

			code = BitArraySingleBitDecode(pFA, bitOffset, 4, false);
			bitOffset += 4;
			
			if (code == "1100") {
				string hoWord;
				while ((hoWord = BitArray6BitDecode(pFA, bitOffset, 4, true)) != null) {
					faData.HoWords.Add(hoWord);
					bitOffset += 24;
				}
				bitOffset += 2;
			}
		}
		public static byte[] GetAsStream(FAData faData)
		{      
			int saBits = faData.Salapas.Any() ? 4 + 18 * faData.Salapas.Count + 2 : 0;
			int eBits = faData.EWords.Any() ? 4 + 24 * faData.EWords.Count + 2 : 0;
			int hoBits = faData.HoWords.Any() ? 4 + 24 * faData.HoWords.Count + 2 : 0;

			int variableBits = saBits + eBits + hoBits;
			int totalBits = 136 + variableBits;
			byte[] bitsAsBytes = new byte[totalBits];
			int offset = 0;

			int faVersionInt = int.Parse(faData.FaVersion);
		
			bitsAsBytes[offset++] = (byte)(faVersionInt & 0x80);
			bitsAsBytes[offset++] = (byte)(faVersionInt & 0x40);
			bitsAsBytes[offset++] = (byte)(faVersionInt & 0x20);
			bitsAsBytes[offset++] = (byte)(faVersionInt & 0x10);
			bitsAsBytes[offset++] = (byte)(faVersionInt & 0x8);
			bitsAsBytes[offset++] = (byte)(faVersionInt & 0x4);
			bitsAsBytes[offset++] = (byte)(faVersionInt & 0x2);
			bitsAsBytes[offset++] = (byte)(faVersionInt & 0x1);
			
			offset = Append(bitsAsBytes, offset, faData.Zeitkriterium);
			offset = Append(bitsAsBytes, offset, faData.Series);
			offset = Append(bitsAsBytes, offset, faData.Type);
			offset = Append(bitsAsBytes, offset, faData.Lackcode);
			offset = Append(bitsAsBytes, offset, faData.Polstercode);

			if (faData.Salapas.Any()) {
				bitsAsBytes[offset++] = 1;
				bitsAsBytes[offset++] = 0;
				bitsAsBytes[offset++] = 0;
				bitsAsBytes[offset++] = 0;

				foreach (var salapa in faData.Salapas) {
					offset = Append(bitsAsBytes, offset, salapa);
				}

				bitsAsBytes[offset++] = 0;
				bitsAsBytes[offset++] = 0;
			}

			if (faData.EWords.Any()) {
				bitsAsBytes[offset++] = 0;
				bitsAsBytes[offset++] = 1;
				bitsAsBytes[offset++] = 0;
				bitsAsBytes[offset++] = 0;

				foreach (var eWord in faData.EWords) {
					offset = Append(bitsAsBytes, offset, eWord);
				}

				bitsAsBytes[offset++] = 0;
				bitsAsBytes[offset++] = 0;
			}

			if (faData.HoWords.Any()) {
				bitsAsBytes[offset++] = 1;
				bitsAsBytes[offset++] = 1;
				bitsAsBytes[offset++] = 0;
				bitsAsBytes[offset++] = 0;

				foreach (var hoWord in faData.HoWords) {
					offset = Append(bitsAsBytes, offset, hoWord);
				}

				bitsAsBytes[offset++] = 0;
				bitsAsBytes[offset++] = 0;
			}

			for (int i = 0; i < 8; i++) {
				bitsAsBytes[offset++] = 0;
			}

			byte[] back = new byte[(totalBits + 7) / 8];

			for (int i = 0; i < back.Length; i++) {
				int byteOffset = i * 8;
				byte b = 0;

				for (int bitPos = 0; bitPos < 8; bitPos++) {
					if (bitPos + byteOffset == totalBits) {
						int shift = 8 - bitPos;
						b = (byte)(b << shift);
						break;
					}

					b = (byte)(b << 1);
					b = (byte)(b | (bitsAsBytes[byteOffset + bitPos] > 0 ? 1 : 0));
				}

				back[i] = b;
			}
			return back;
		}

		private static int Append(byte[] pVariablePart, int pOffset, string pData)
		{
			int offset = pOffset;

			foreach (char digit in pData) {
				int highNibble = (digit & 0xF0) >> 4;
				int lowNibble = digit & 0xF;
				int prefix = (highNibble & 0x4) >> 1 | highNibble & 0x1;

				pVariablePart[offset++] = (byte)(prefix & 0x2);
				pVariablePart[offset++] = (byte)(prefix & 0x1);
				pVariablePart[offset++] = (byte)(lowNibble & 0x8);
				pVariablePart[offset++] = (byte)(lowNibble & 0x4);
				pVariablePart[offset++] = (byte)(lowNibble & 0x2);
				pVariablePart[offset++] = (byte)(lowNibble & 0x1);
			}

			return offset;
		}
	}
}
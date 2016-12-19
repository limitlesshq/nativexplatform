using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Xml.XPath;

namespace Akeeba.Unarchiver.Encrypt
{
    /// <summary>
    /// Implements Rijndael-128 in CTR (Counter) mode.
    ///
    /// This is here for legacy crypto support only.
    /// </summary>
    public class AesCounter
    {
        /// <summary>
        /// Pre-computed multiplicative inverse in GF(2^8) used in SubBytes and KeyExpansion [section 5.1.1]
        /// </summary>
        private static readonly byte[] _sBox =
        {
            0x63, 0x7c, 0x77, 0x7b, 0xf2, 0x6b, 0x6f, 0xc5, 0x30, 0x01, 0x67, 0x2b, 0xfe, 0xd7, 0xab, 0x76,
            0xca, 0x82, 0xc9, 0x7d, 0xfa, 0x59, 0x47, 0xf0, 0xad, 0xd4, 0xa2, 0xaf, 0x9c, 0xa4, 0x72, 0xc0,
            0xb7, 0xfd, 0x93, 0x26, 0x36, 0x3f, 0xf7, 0xcc, 0x34, 0xa5, 0xe5, 0xf1, 0x71, 0xd8, 0x31, 0x15,
            0x04, 0xc7, 0x23, 0xc3, 0x18, 0x96, 0x05, 0x9a, 0x07, 0x12, 0x80, 0xe2, 0xeb, 0x27, 0xb2, 0x75,
            0x09, 0x83, 0x2c, 0x1a, 0x1b, 0x6e, 0x5a, 0xa0, 0x52, 0x3b, 0xd6, 0xb3, 0x29, 0xe3, 0x2f, 0x84,
            0x53, 0xd1, 0x00, 0xed, 0x20, 0xfc, 0xb1, 0x5b, 0x6a, 0xcb, 0xbe, 0x39, 0x4a, 0x4c, 0x58, 0xcf,
            0xd0, 0xef, 0xaa, 0xfb, 0x43, 0x4d, 0x33, 0x85, 0x45, 0xf9, 0x02, 0x7f, 0x50, 0x3c, 0x9f, 0xa8,
            0x51, 0xa3, 0x40, 0x8f, 0x92, 0x9d, 0x38, 0xf5, 0xbc, 0xb6, 0xda, 0x21, 0x10, 0xff, 0xf3, 0xd2,
            0xcd, 0x0c, 0x13, 0xec, 0x5f, 0x97, 0x44, 0x17, 0xc4, 0xa7, 0x7e, 0x3d, 0x64, 0x5d, 0x19, 0x73,
            0x60, 0x81, 0x4f, 0xdc, 0x22, 0x2a, 0x90, 0x88, 0x46, 0xee, 0xb8, 0x14, 0xde, 0x5e, 0x0b, 0xdb,
            0xe0, 0x32, 0x3a, 0x0a, 0x49, 0x06, 0x24, 0x5c, 0xc2, 0xd3, 0xac, 0x62, 0x91, 0x95, 0xe4, 0x79,
            0xe7, 0xc8, 0x37, 0x6d, 0x8d, 0xd5, 0x4e, 0xa9, 0x6c, 0x56, 0xf4, 0xea, 0x65, 0x7a, 0xae, 0x08,
            0xba, 0x78, 0x25, 0x2e, 0x1c, 0xa6, 0xb4, 0xc6, 0xe8, 0xdd, 0x74, 0x1f, 0x4b, 0xbd, 0x8b, 0x8a,
            0x70, 0x3e, 0xb5, 0x66, 0x48, 0x03, 0xf6, 0x0e, 0x61, 0x35, 0x57, 0xb9, 0x86, 0xc1, 0x1d, 0x9e,
            0xe1, 0xf8, 0x98, 0x11, 0x69, 0xd9, 0x8e, 0x94, 0x9b, 0x1e, 0x87, 0xe9, 0xce, 0x55, 0x28, 0xdf,
            0x8c, 0xa1, 0x89, 0x0d, 0xbf, 0xe6, 0x42, 0x68, 0x41, 0x99, 0x2d, 0x0f, 0xb0, 0x54, 0xbb, 0x16
        };

        /// <summary>
        /// Round Constant used for the Key Expansion [1st col is 2^(r-1) in GF(2^8)] [section 5.2]
        /// </summary>
        private static readonly byte[][] _rCon =
        {
            new byte[] {0x00, 0x00, 0x00, 0x00},
            new byte[] {0x01, 0x00, 0x00, 0x00},
            new byte[] {0x02, 0x00, 0x00, 0x00},
            new byte[] {0x04, 0x00, 0x00, 0x00},
            new byte[] {0x08, 0x00, 0x00, 0x00},
            new byte[] {0x10, 0x00, 0x00, 0x00},
            new byte[] {0x20, 0x00, 0x00, 0x00},
            new byte[] {0x40, 0x00, 0x00, 0x00},
            new byte[] {0x80, 0x00, 0x00, 0x00},
            new byte[] {0x1b, 0x00, 0x00, 0x00},
            new byte[] {0x36, 0x00, 0x00, 0x00}
        };

        private static byte[][] AddRoundKey(byte[][] state, byte[][] w, byte rnd, byte Nb)
        {
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c <= Nb - 1; c++)
                {
                    state[r][c] = (byte) (state[r][c] ^ w[rnd * 4 + c][r]);
                }
            }

            return state;
        }

        private static byte[][] MixColumns(byte[][] s, byte Nb)
        {
            byte[] a = {0, 0, 0, 0}, b = {0, 0, 0, 0};

            for (int c = 0; c <= 3; c++)
            {
                for (int i = 0; i <= 3; i++)
                {
                    a[i] = s[i][c];

                    if ((s[i][c] & 0x80) > 0)
                    {
                        b[i] = (byte) ((s[i][c] << 1) ^ 0x011b);
                    }
                    else
                    {
                        b[i] = (byte) (s[i][c] << 1);
                    }
                }

                s[0][c] = (byte) (b[0] ^ a[1] ^ b[1] ^ a[2] ^ a[3]);
                s[1][c] = (byte) (a[0] ^ b[1] ^ a[2] ^ b[2] ^ a[3]);
                s[2][c] = (byte) (a[0] ^ a[1] ^ b[2] ^ a[3] ^ b[3]);
                s[3][c] = (byte) (a[0] ^ b[0] ^ a[1] ^ a[2] ^ b[3]);
            }

            return s;
        }

        private static byte[] RotWord(byte[] w)
        {
            byte tmp;

            tmp = w[0];

            for (int i = 0; i <= 2; i++)
            {
                w[i] = w[i + 1];
            }

            w[3] = tmp;

            return w;
        }

        private static byte[][] ShiftRows(byte[][] s, byte Nb)
        {
            byte[] t = {0, 0, 0, 0};

            for (int r = 1; r <= 3; r++)
            {
                for (int c = 0; c <= 3; c++)
                {
                    t[c] = s[r][(c + r) % Nb];
                }

                for (int c = 0; c <= 3; c++)
                {
                    s[r][c] = t[c];
                }
            }

            return s;
        }

        private static byte[][] SubBytes(byte[][] s, byte Nb)
        {
            for (int r = 0; r <= 3; r++)
            {
                for (int c = 0; c <= Nb - 1; c++)
                {
                    s[r][c] = _sBox[s[r][c]];
                }
            }

            return s;
        }

        private static byte[] SubWord(byte[] w)
        {
            for (int i = 0; i <= 3; i++)
            {
                w[i] = _sBox[w[i]];
            }

            return w;
        }

        public static byte[] Cipher(byte[] input, byte[][] w)
        {
            byte Nb = 4, Nr = 10;
            var result = new byte[16];
            byte[][] state =
            {
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}
            };
            byte round;

            // Sanity check: the message to encrypt must be a 16 byte array
            if (input.Length != 16)
            {
                throw new ArgumentOutOfRangeException();
            }

            // Sanity check: the expanded key w must be a 45x4 byte array
            if (w.Length != 45)
            {
                throw new ArgumentOutOfRangeException();
            }

            for (int i = 0; i < 45; i++)
            {
                if (w[i].Length != 4)
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            // Run the cipher
            for (int i = 0; i < 4 * Nb; i++)
            {
                state[i % 4][(int) Math.Floor((double) (i / 4))] = input[i];
            }

            state = AddRoundKey(state, w, 0, Nb);

            for (round = 1; round < Nr; round++)
            {
                state = SubBytes(state, Nb);
                state = ShiftRows(state, Nb);
                state = MixColumns(state, Nb);
                state = AddRoundKey(state, w, round, Nb);
            }

            state = SubBytes(state, Nb);
            state = ShiftRows(state, Nb);
            state = AddRoundKey(state, w, Nr, Nb);

            for (int i = 0; i < 4 * Nb; i++)
            {
                result[i] = state[i % 4][(int) Math.Floor((double) (i / 4))];
            }

            return result;
        }

        private static byte[][] KeyExpansion(byte[] key)
        {
            byte Nb = 4, Nr = 10, Nk = 4;
            byte[][] w =
            {
                new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0}, new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}
            };
            byte[] temp = {0, 0, 0, 0};

            // Sanity check: key must be 16 bytes long
            if (key.Length != 16)
            {
                throw new ArgumentOutOfRangeException();
            }

            for (int i = 0; i < Nk; i++)
            {
                w[i][0] = key[4 * i];
                w[i][1] = key[4 * i + 1];
                w[i][2] = key[4 * i + 2];
                w[i][3] = key[4 * i + 3];
            }

            for (int i = Nk; i < Nb * (Nr + 1); i++)
            {
                for (int t = 0; t < 4; t++)
                {
                    temp[t] = w[i - 1][t];
                }

                if ((i % Nk) == 0)
                {
                    temp = SubWord(RotWord(temp));

                    for (int t = 0; t < 4; t++)
                    {
                        temp[t] = (byte) (temp[t] ^ _rCon[(int) Math.Round((double) (i / Nk))][t]);
                    }
                }
                else if ((Nk > 6) && ((i % Nk) == 4))
                {
                    temp = SubWord(temp);
                }

                for (int t = 0; t < 4; t++)
                {
                    w[i][t] = (byte) (w[i - Nk][t] ^ temp[t]);
                }
            }

            return w;
        }

        /// <summary>
        /// Create a legacy decryption key from the given password. The key expansion uses only the first sixteen
        /// characters of the password and uses it to encrypt itself using AES-128 in CTR mode. Therefore it's not
        /// really strong. This is the default key expansion used in the first two published versions of the JPS
        /// format, 1.9 and 1.10.
        /// </summary>
        /// <param name="password">The password to convert to a key</param>
        /// <returns></returns>
        public static byte[] makeKey(string password)
        {
            byte[] pwBytes = new byte[16];

            if (password.Length > 0)
            {
                for (int i = 0; i < Math.Min(16, password.Length); i++)
                {
                    pwBytes[i] = (byte) (password[i] & 0xff);
                }

                if (password.Length < 15)
                {
                    for (int i = password.Length; i < 16; i++)
                    {
                        pwBytes[i] = 0;
                    }
                }
            }

            return Cipher(pwBytes, KeyExpansion(pwBytes));
        }
    }
}
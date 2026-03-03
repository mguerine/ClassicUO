// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Globalization;

namespace ClassicUO.Utility
{
    public static class UInt16Converter
    {
        public static ushort Parse(string str) => Parse(str.AsSpan());

        public static ushort Parse(ReadOnlySpan<char> str)
        {
            var style = NumberStyles.Integer;
            if (str.StartsWith("0x".AsSpan()))
            {
                str = str.Slice(2);
                style = NumberStyles.HexNumber;
            }
            else if (str.Length > 1 && str[0] == '-')
            {
                if (short.TryParse(str.ToString(), out var res))
                {
                    return (ushort)res;
                }
            }

            uint.TryParse(str.ToString(), style, null, out uint v);

            return (ushort) v;
        }
    }
}
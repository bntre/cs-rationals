using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Avalonia;
using Avalonia.Input;


namespace Rationals.Explorer
{
    public static class Keyboard
    {
        public struct Coords { 
            public int x; 
            public int y; 
        }

        public static Dictionary<Key, Coords> KeyCoords = new Dictionary<Key, Coords>();

        private static void InitKeyMap() {
            // https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
            // https://www.codeproject.com/KB/miscctrl/Virtual-WPF/VKwKeynames100.png
            const string keys = @"
                D1 D2 D3 D4 D5 D6 D7 D8 D9 D0 OemMinus OemPlus
                Q  W  E  R  T  Y  U  I  O  P  Oem4 OemCloseBrackets
                A  S  D  F  G  H  J  K  L  OemSemicolon OemQuotes OemPipe
                Z  X  C  V  B  N  M  OemComma OemPeriod Oem2
            ";
            Key[][] matrix = keys
                .Split('\n')
                .Where(l => !String.IsNullOrWhiteSpace(l))
                .Select(l => l
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => Enum.Parse<Key>(s))
                    .ToArray()
                )
                .ToArray();
            for (int i = 0; i < matrix.Length; ++i) {
                for (int j = 0; j < matrix[i].Length; ++j) {
                    KeyCoords[matrix[i][j]] = new Coords {
                        x = j - 3,  // F for origin,
                        y = 2 - i,  //  direct upward
                    };
                }
            }
        }

        static Keyboard() {
            InitKeyMap();
        }

    }
}
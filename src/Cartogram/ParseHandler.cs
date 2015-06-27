﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cartogram
{
    public static class ParseHandler
    {

        /// <summary>
        /// Checks the clipboard to determine if it contains PoE Related data or not
        /// </summary>
        /// <returns> TRUE if it contains 'Rarity:' on the first line </returns>
        internal static bool CheckClipboard()
        {
            var clipboardContents = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text).Replace("\r", "").Split('\n');
            return clipboardContents.Length != -1 && clipboardContents[0].StartsWith("Rarity:");
        }

        /// <summary>
        /// Parses the information gained off the keyboard to construct a Map Object
        /// </summary>
        /// <returns>Map object with details from the clipboard</returns>
        internal static Map ParseClipboard()
        {
            var clipboardValue = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
            var clipboardContents = clipboardValue.Replace("\r", "").Split('\n');
            Map newMap;
            if (!clipboardValue.Contains("Map") || clipboardValue.Contains("Sacrifice at")) return null;

            if (clipboardContents[0].Contains("Normal") || clipboardContents[0].Contains("Magic"))
            {
                newMap = new Map
                {
                    Rarity = clipboardContents[0].Replace("Rarity: ", ""),
                    Level = int.Parse(clipboardContents[3].Replace("Map Level:", "")),
                    Name = MapName(clipboardContents[1]),
                    Affixes = GetAffixes(clipboardContents),
                };
                if (clipboardValue.Contains("Item Quantity:"))
                {
                    int quantity;
                    if (int.TryParse(clipboardContents[4].Replace("Item Quantity: +", "").Replace("% (augmented)", ""), out quantity))
                        newMap.Quantity = quantity;
                }
                if (clipboardValue.Contains("Quality:"))
                {
                    int quality;
                    if (int.TryParse(clipboardContents[5].Replace("Quality: +", "").Replace("% (augmented)", ""), out quality))
                        newMap.Quality = quality;
                }
                return newMap;
            }

            if (clipboardContents[0].Replace("Rarity: ", "") == "Rare" || clipboardContents[0].Replace("Rarity: ", "") == "Unique")
            {
                var i = 0;
                if (clipboardValue.Contains("Unidentified")) i = 1;

                newMap = new Map
                {
                    Rarity = clipboardContents[0].Replace("Rarity: ", ""),
                    Level = int.Parse(clipboardContents[4 - i].Replace("Map Level:", "")),
                    Affixes = GetAffixes(clipboardContents),
                };
                newMap.Name = newMap.Rarity == "Rare" ? MapName(clipboardContents[2 - i]) : MapName(clipboardContents[1]);

                if (clipboardValue.Contains("Item Quantity:"))
                {
                    int quantity;
                    if (int.TryParse(clipboardContents[5].Replace("Item Quantity: +", "").Replace("% (augmented)", ""), out quantity))
                        newMap.Quantity = quantity;
                }
                if (clipboardValue.Contains("Quality:"))
                {
                    int quality;
                    if (int.TryParse(clipboardContents[6].Replace("Quality: +", "").Replace("% (augmented)", ""), out quality))
                        newMap.Quality = quality;
                }
                return newMap;
            }
            return null;
        }

        /// <summary>
        /// Parses currency off the clipboard into a KVP of stack size and currency name
        /// </summary>
        /// <returns>Stack Count and Name</returns>
        internal static KeyValuePair<int, string> ParseCurrency()
        {
            var clipboardContents = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text).Replace("\r", "").Split(new[] { '\n' });
            if (clipboardContents[0] != "Rarity: Currency") return new KeyValuePair<int, string>(-1, "");

            var currency = clipboardContents[1].Replace("Orb", "").Replace("of", "").Trim();
            var size = Regex.Match(clipboardContents[3].Replace("Stack Size: ", ""), @"^.*?(?=/)");

            return new KeyValuePair<int, string>(int.Parse(size.ToString()), currency);
        }

        /// <summary>
        /// Parses the name out of a unique non-map item off the clipboard
        /// </summary>
        /// <returns>Name of unique item</returns>
        internal static string ParseUnique()
        {
            var clipboardContents = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text).Replace("\r", "").Split(new[] { '\n' });
            if (clipboardContents[0] != "Rarity: Unique") return "";

            var item = clipboardContents[1];
            return item;
        }

        /// <summary>
        /// Gets the affixes from the clipboard and puts them into a list
        /// </summary>
        /// <param name="clipboardContents">The map item as it appears on the clipboard</param>
        /// <returns>List containing each parameter</returns>
        private static List<string> GetAffixes(string[] clipboardContents)
        {
            var affixes = new List<string>();
            if (clipboardContents.Count(line => line == "--------") == 4 || clipboardContents.Count(line => line == "--------") == 5)
            {
                var lineCount = 0;
                foreach (var line in clipboardContents)
                {
                    if (line == "--------")
                    {
                        lineCount++;
                        continue;
                    }
                    if (lineCount < 3) continue;
                    if (lineCount == 4) break;
                    affixes.Add(line);
                }
            }
            return affixes;
        }

        /// <summary> Returns the map name given the complete name </summary>
        /// <param name="inputLine"> Line containing map name + affixes</param>
        /// <returns> Map Name eg. "Vaal Pyramid" </returns>
        private static string MapName(string inputLine)
        {
            var maps = Maps.MapArray();

            foreach (var x in maps.Where(inputLine.Contains))
            {
                return x;
            }
            return inputLine;
        }
    }
}
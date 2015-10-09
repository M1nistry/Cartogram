using System;
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
            if (!System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.Text)) return false;

            var clipboardContents = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
            if (clipboardContents.Length <= 0) return false;
            var splitClipboard = clipboardContents.Replace("\r", "").Split('\n');
            return clipboardContents.Length != -1 && splitClipboard[0].StartsWith("Rarity:");
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
                    Level = int.Parse(clipboardContents[3].Replace("Map Tier: ", "")),
                    Name = MapName(clipboardContents[1]),
                    Affixes = GetAffixes(clipboardContents),
                };

                foreach (var row in clipboardContents)
                {
                    if (row.Contains("Item Quantity:"))
                    {
                        int quantity;
                        if (int.TryParse(row.Replace("Item Quantity: +", "").Replace("% (augmented)", ""), out quantity))
                            newMap.Quantity = quantity;
                    }
                    if (row.Contains("Quality:"))
                    {
                        int quality;
                        if (int.TryParse(row.Replace("Quality: +", "").Replace("% (augmented)", ""), out quality))
                            newMap.Quality = quality;
                    }
                    if (row.Contains("Monster Pack Size:"))
                    {
                        int packSize;
                        if (int.TryParse(row.Replace("Monster Pack Size: +", "").Replace("% (augmented)", ""), out packSize))
                            newMap.PackSize = packSize;
                    }
                    if (row.Contains("Item Rarity:"))
                    {
                        int itemRarity;
                        if (int.TryParse(row.Replace("Item Rarity: +", "").Replace("% (augmented)", ""), out itemRarity))
                            newMap.ItemRarity = itemRarity;
                    }
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
                    Level = int.Parse(clipboardContents[4 - i].Replace("Map Tier: ", "")),
                    Affixes = GetAffixes(clipboardContents),
                };
                newMap.Name = newMap.Rarity == "Rare" ? MapName(clipboardContents[2 - i]) : MapName(clipboardContents[1]);

                foreach (var row in clipboardContents)
                {
                    if (row.Contains("Item Quantity:"))
                    {
                        int quantity;
                        if (int.TryParse(row.Replace("Item Quantity: +", "").Replace("% (augmented)", ""), out quantity))
                            newMap.Quantity = quantity;
                    }
                    if (row.Contains("Quality:"))
                    {
                        int quality;
                        if (int.TryParse(row.Replace("Quality: +", "").Replace("% (augmented)", ""), out quality))
                            newMap.Quality = quality;
                    }
                    if (row.Contains("Monster Pack Size:"))
                    {
                        int packSize;
                        if (int.TryParse(row.Replace("Monster Pack Size: +", "").Replace("% (augmented)", ""), out packSize))
                            newMap.PackSize = packSize;
                    }
                    if (row.Contains("Item Rarity:"))
                    {
                        int itemRarity;
                        if (int.TryParse(row.Replace("Item Rarity: +", "").Replace("% (augmented)", ""), out itemRarity))
                            newMap.ItemRarity = itemRarity;
                    }
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
        /// Parses the information for a Divination card including stack size.
        /// </summary>
        /// <returns>Keyvalue pair of stack size and card name</returns>
        internal static KeyValuePair<int, string> ParseDivination()
        {
            var clipboardContents = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text).Replace("\r", "").Split(new[] {'\n'});
            if (clipboardContents[0] != "Rarity: Normal" && clipboardContents[3].Contains("Stack Size:")) return new KeyValuePair<int, string>(-1, "");

            var divination = clipboardContents[1].Trim();
            var amount = Regex.Match(clipboardContents[3].Replace("Stack Size: ", ""), @"^.*?(?=/)");

            return new KeyValuePair<int, string>(int.Parse(amount.ToString()), divination);
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

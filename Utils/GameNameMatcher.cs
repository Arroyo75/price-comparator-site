using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace price_comparator_site.Utils.GameMatching
{
    public class GameNameMatcher
    {
        // Dictionary to handle common abbreviations in game editions
        private static readonly Dictionary<string, string> EditionAbbreviations = new()
        {
            { "GOTY", "Game of the Year" },
            { "GOTYE", "Game of the Year" },
            { "DE", "Definitive Edition" },
            { "EE", "Enhanced Edition" },
            { "CE", "Collector's Edition" },
            { "SE", "Special Edition" }
        };

        // Distinct editions that should be treated as separate versions
        private static readonly HashSet<string> DistinctEditionMarkers = new()
        {
            "vr",
            "anniversary edition",
            "legendary edition",
            "special edition",
            "standard edition",
            "deluxe edition",
            "premium edition"
        };

        // Patterns to identify different types of game numbers
        private static readonly string[] NumberPatterns = new[]
        {
            @"\b\d+\b",           // Standard numbers like "2" in "Red Alert 2"
            @"\b[IVXLC]+\b",      // Roman numerals like "III" in "Dark Souls III"
            @"\bone\b|\btwo\b|\bthree\b|\bfour\b|\bfive\b"  // Written numbers
        };

        public static bool AreGamesMatching(string name1, string name2)
        {
            // Normalize both game names for comparison
            var normalized1 = NormalizeGameName(name1);
            var normalized2 = NormalizeGameName(name2);

            // Extract series name and number for both games
            var (series1, number1) = ExtractSeriesAndNumber(normalized1);
            var (series2, number2) = ExtractSeriesAndNumber(normalized2);

            // If both games have numbers and they're different, they're different games
            if (number1 != null && number2 != null && number1 != number2)
            {
                return false;
            }

            // If base names don't match, they're different games
            if (!string.Equals(series1, series2, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Check edition information
            var edition1 = ExtractEditionInfo(normalized1);
            var edition2 = ExtractEditionInfo(normalized2);

            // If either game has a distinct edition marker, they must match exactly
            if (HasDistinctEditionMarker(edition1) || HasDistinctEditionMarker(edition2))
            {
                return NormalizeEditionName(edition1) == NormalizeEditionName(edition2);
            }

            // Check if editions are equivalent (like GOTY and Game of the Year)
            return AreEditionsEquivalent(edition1, edition2);
        }

        private static string NormalizeGameName(string name)
        {
            // Convert to lowercase and remove special characters
            return name.ToLowerInvariant()
                .Replace("™", "")
                .Replace("®", "")
                .Replace("©", "")
                .Replace(":", "")
                .Trim();
        }

        private static (string SeriesName, string? Number) ExtractSeriesAndNumber(string gameName)
        {
            foreach (var pattern in NumberPatterns)
            {
                var match = Regex.Match(gameName, pattern);
                if (match.Success)
                {
                    var numberIndex = match.Index;
                    var number = match.Value;

                    // Get the part before the number (series name)
                    var seriesName = gameName.Substring(0, numberIndex).Trim();

                    return (seriesName, number);
                }
            }

            // If no number found, return the whole name and null for number
            return (gameName, null);
        }

        private static string ExtractEditionInfo(string fullName)
        {
            // Get everything after the base game name
            var baseName = ExtractBaseName(fullName);
            return fullName.Replace(baseName, "").Trim();
        }

        private static string ExtractBaseName(string fullName)
        {
            // Remove common prefixes like "The" or series names
            var name = fullName;

            // Extract the base name before any edition markers
            var baseName = name;
            foreach (var marker in DistinctEditionMarkers.Concat(EditionAbbreviations.Keys)
                                                        .Concat(EditionAbbreviations.Values))
            {
                var index = baseName.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index > 0)
                {
                    baseName = baseName.Substring(0, index);
                }
            }

            return baseName.Trim();
        }

        private static bool HasDistinctEditionMarker(string edition)
        {
            return DistinctEditionMarkers.Any(marker =>
                edition.Contains(marker, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeEditionName(string edition)
        {
            var normalized = edition.ToLowerInvariant();
            foreach (var abbr in EditionAbbreviations)
            {
                normalized = normalized.Replace(abbr.Key.ToLower(), abbr.Value.ToLower());
            }
            return normalized.Trim();
        }

        private static bool AreEditionsEquivalent(string edition1, string edition2)
        {
            var normalized1 = NormalizeEditionName(edition1);
            var normalized2 = NormalizeEditionName(edition2);
            return normalized1 == normalized2;
        }
    }
}
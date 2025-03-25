using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MaintainEase.Infrastructure.Hebrew
{
    /// <summary>
    /// Handler for Hebrew text operations
    /// </summary>
    public class HebrewTextHandler
    {
        // RTL mark and LTR mark Unicode characters
        public const char RLM = '\u200F';
        public const char LRM = '\u200E';

        /// <summary>
        /// Apply RTL formatting to text containing Hebrew
        /// </summary>
        public static string ApplyRtlFormatting(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            // Check if text contains Hebrew characters
            if (ContainsHebrew(text))
            {
                // Add RTL mark at the beginning
                return RLM + text;
            }
            
            return text;
        }

        /// <summary>
        /// Check if text contains Hebrew characters
        /// </summary>
        public static bool ContainsHebrew(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
                
            // Hebrew Unicode range: 0x0590-0x05FF
            return Regex.IsMatch(text, @"[\u0590-\u05FF]");
        }

        /// <summary>
        /// Normalize Hebrew text for search
        /// </summary>
        public static string NormalizeForSearch(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            // Remove diacritics (nikud)
            text = RemoveNikud(text);
            
            // Convert different forms of some letters
            text = NormalizeLetterForms(text);
            
            // Convert to lowercase
            return text.ToLowerInvariant();
        }

        /// <summary>
        /// Remove Hebrew diacritics (nikud)
        /// </summary>
        public static string RemoveNikud(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            // Hebrew nikud (diacritics) Unicode range: 0x0591-0x05C7
            return Regex.Replace(text, @"[\u0591-\u05C7]", "");
        }

        /// <summary>
        /// Normalize different forms of Hebrew letters
        /// </summary>
        public static string NormalizeLetterForms(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            // Replace final forms with regular forms
            var sb = new StringBuilder(text);
            sb.Replace('ך', 'כ'); // Final Kaf -> Kaf
            sb.Replace('ם', 'מ'); // Final Mem -> Mem
            sb.Replace('ן', 'נ'); // Final Nun -> Nun
            sb.Replace('ף', 'פ'); // Final Pe -> Pe
            sb.Replace('ץ', 'צ'); // Final Tsadi -> Tsadi
            
            return sb.ToString();
        }

        /// <summary>
        /// Format a date according to the Hebrew calendar
        /// </summary>
        public static string FormatHebrewDate(DateTime date)
        {
            // Create Hebrew calendar
            var hebrewCalendar = new HebrewCalendar();
            
            // Get Hebrew date components
            int hebrewYear = hebrewCalendar.GetYear(date);
            int hebrewMonth = hebrewCalendar.GetMonth(date);
            int hebrewDay = hebrewCalendar.GetDayOfMonth(date);
            
            // Hebrew month names
            string[] hebrewMonthNames = {
                "ניסן", "אייר", "סיון", "תמוז", "אב", "אלול",
                "תשרי", "חשון", "כסלו", "טבת", "שבט", "אדר"
            };
            
            // Handle Adar II in leap years
            string monthName = hebrewMonth == 13 ? "אדר ב'" : hebrewMonthNames[(hebrewMonth - 1) % 12];
            
            // Format Hebrew date
            return $"{hebrewDay} {monthName} {NumberToHebrewString(hebrewYear)}";
        }

        /// <summary>
        /// Convert a number to Hebrew string representation
        /// </summary>
        private static string NumberToHebrewString(int number)
        {
            // This is a simple implementation - a full version would be more complex
            char[] hebrewNumerals = {
                'א', 'ב', 'ג', 'ד', 'ה', 'ו', 'ז', 'ח', 'ט',    // 1-9
                'י', 'כ', 'ל', 'מ', 'נ', 'ס', 'ע', 'פ', 'צ',    // 10-90
                'ק', 'ר', 'ש', 'ת'                            // 100-400
            };
            
            if (number < 1)
                return "";
                
            if (number >= 1000)
            {
                // Add thousands (usually just a quote mark for brevity)
                return "ה'" + NumberToHebrewString(number % 1000);
            }
            
            var sb = new StringBuilder();
            
            // Add hundreds
            while (number >= 100)
            {
                if (number >= 400)
                {
                    sb.Append("ת");
                    number -= 400;
                }
                else
                {
                    sb.Append(hebrewNumerals[18 + (number / 100)]);
                    number %= 100;
                }
            }
            
            // Add tens
            if (number >= 10)
            {
                sb.Append(hebrewNumerals[9 + (number / 10)]);
                number %= 10;
            }
            
            // Add ones
            if (number > 0)
            {
                sb.Append(hebrewNumerals[number - 1]);
            }
            
            // Add geresh (apostrophe)
            return sb.ToString() + "'";
        }
    }
}

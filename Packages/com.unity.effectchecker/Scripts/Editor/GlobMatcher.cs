#if UNITY_EDITOR
using System;


using System.Linq;


namespace SY.EffectChecker
{
    public class GlobMatcher
    {
        public static bool IsMatch(string glob, string text)
        {
            //UnityEngine.Debug.Log(text);
            //foreach(var item in text)
            //{
            //    if (item == '/') Debug.Log("//");
            //}
            return Match(glob, text, 0, 0);
        }

        private static bool Match(string pattern, string text, int patternIndex, int textIndex)
        {
            while (patternIndex < pattern.Length && textIndex < text.Length)
            {
                char patternChar = pattern[patternIndex];

                switch (patternChar)
                {
                    case '*':
                        if (patternIndex + 1 < pattern.Length && pattern[patternIndex + 1] == '*')
                        {
                            // Handling ** pattern
                            patternIndex++;
                            while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
                            {
                                patternIndex++;
                            }

                            for (int i = textIndex; i <= text.Length; i++)
                            {
                                if (Match(pattern, text, patternIndex, i))
                                {
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            for (int i = textIndex; i <= text.Length; i++)
                            {
                                if (i < text.Length && text[i] == '/') break;
                                if (Match(pattern, text, patternIndex + 1, i))
                                {
                                    return true;
                                }
                            }
                        }
                        return false;

                    case '?':
                        // Handling ? pattern
                        if (text[textIndex] == '/') return false;
                        break;

                    case '[':
                        // Handling [] pattern
                        int closingBracketIndex = pattern.IndexOf(']', patternIndex + 1);
                        if (closingBracketIndex < 0)
                        {
                            throw new ArgumentException("Invalid pattern: missing closing ']'");
                        }
                        if (!MatchCharSet(pattern.Substring(patternIndex + 1, closingBracketIndex - patternIndex - 1), text[textIndex]))
                        {
                            return false;
                        }
                        patternIndex = closingBracketIndex;
                        break;

                    case '(':
                        // Handling (pa|pb) pattern
                        int closingParenIndex = pattern.IndexOf(')', patternIndex + 1);
                        if (closingParenIndex < 0)
                        {
                            throw new ArgumentException("Invalid pattern: missing closing ')'");
                        }
                        string[] options = pattern.Substring(patternIndex + 1, closingParenIndex - patternIndex - 1).Split('|');
                        bool optionMatch = options.Any(option => Match(option + pattern.Substring(closingParenIndex + 1), text, 0, textIndex));
                        return optionMatch;

                    default:
                        // Direct character match
                        if (patternChar != text[textIndex])
                        {
                            return false;
                        }
                        break;
                }

                patternIndex++;
                textIndex++;
            }

            // Handle end of string cases
            return patternIndex == pattern.Length && textIndex == text.Length;
        }

        private static bool MatchCharSet(string charSet, char character)
        {
            bool negated = charSet.StartsWith("!");
            if (negated) charSet = charSet.Substring(1);

            bool match = charSet.Contains(character);
            if (charSet.Contains('-'))
            {
                var ranges = charSet.Split('-');
                for (int i = 0; i < ranges.Length - 1; i += 2)
                {
                    match |= character >= ranges[i][0] && character <= ranges[i + 1][0];
                }
            }

            return negated ? !match : match;
        }
    }
}

#endif
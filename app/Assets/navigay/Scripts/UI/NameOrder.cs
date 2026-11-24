using System.Collections.Generic;

namespace Navigay.UI
{
    public sealed class NameOrder : IComparer<string>
    {
        public static readonly NameOrder Instance = new NameOrder();

        public int Compare(string left, string right)
        {
            left ??= string.Empty;
            right ??= string.Empty;
            var i = 0;
            var j = 0;
            while (i < left.Length && j < right.Length)
            {
                if (char.IsDigit(left[i]) && char.IsDigit(right[j]))
                {
                    var leftNumber = ReadNumber(left, ref i);
                    var rightNumber = ReadNumber(right, ref j);
                    var compared = leftNumber.CompareTo(rightNumber);
                    if (compared != 0)
                    {
                        return compared;
                    }
                }
                else
                {
                    var compared = char.ToUpperInvariant(left[i]).CompareTo(char.ToUpperInvariant(right[j]));
                    if (compared != 0)
                    {
                        return compared;
                    }

                    i++;
                    j++;
                }
            }

            return (left.Length - i).CompareTo(right.Length - j);
        }

        static int ReadNumber(string text, ref int index)
        {
            var value = 0;
            while (index < text.Length && char.IsDigit(text[index]))
            {
                value = (value * 10) + (text[index] - '0');
                index++;
            }

            return value;
        }
    }
}

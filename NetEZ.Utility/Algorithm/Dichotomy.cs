using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetEZ.Utility.Algorithm
{
    public class Dichotomy
    {
        public const int FIND_DIRECTIVE_GREATER = 1; 
        public const int FIND_DIRECTIVE_LESS = 2;
        public const int FIND_DIRECTIVE_EQUAL = 4;

        /// <summary>
        /// 二分法查找
        /// </summary>
        /// <param name="searchArray"></param>
        /// <param name="val"></param>
        /// <param name="directive"></param>
        /// <returns></returns>
        public static int FindIndexUsingBinaryAlgorithm(int[] searchArray, int val, int directive)
        {
            int index = -1;

            try
            {
                if (searchArray == null || searchArray.Length < 1)
                    return index;

                int count = searchArray.Length;
                int lowIndex = 0;
                int highIndex = count - 1;
                int middle = -1;
                int middleVal = 0;
                while (lowIndex <= highIndex)
                {
                    middle = (lowIndex + highIndex) / 2;
                    middleVal = searchArray[middle];
                    if (middleVal == val)
                    {
                        index = middle;
                        break;
                    }

                    if (val > middleVal)
                        lowIndex = middle + 1;
                    else
                        highIndex = middle - 1;
                }
                if (index >= 0)
                {
                    //  找到了该数值
                    if ((directive & FIND_DIRECTIVE_EQUAL) == FIND_DIRECTIVE_EQUAL)
                        return index;
                    else if ((directive & FIND_DIRECTIVE_GREATER) == FIND_DIRECTIVE_GREATER)               //  要求大于该数
                        return index + 1;
                    else if ((directive & FIND_DIRECTIVE_LESS) == FIND_DIRECTIVE_LESS)               //  要求小于该数
                        return index - 1;             //  要求小于该数
                    else
                        return -1;
                }
                else
                {
                    if ((directive & FIND_DIRECTIVE_GREATER) == FIND_DIRECTIVE_GREATER)               //  要求大于该数
                        return lowIndex;
                    else if ((directive & FIND_DIRECTIVE_LESS) == FIND_DIRECTIVE_LESS)               //  要求小于该数
                        return highIndex;           //  要求小于该数
                    else
                        return -1;
                }
            }
            catch { }

            return index;
        }
    }
}

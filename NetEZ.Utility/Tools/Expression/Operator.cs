using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Utility.Tools.Expression
{
    public enum Operator
    {
        None = 0,
        Equal = 1,
        NotEqual = 2,
        Plus = 3,
        Subtract = 4,
        Multiply = 5,
        Divide = 6,
        Mod = 7,
        Greater = 8,
        GreaterOrEqual = 9,
        Less = 10,
        LessOrEqual = 11,
        BitAnd = 12,
        BitOr = 13,
        BitXOr = 14,
        LogicAnd = 15,
        LogicOr = 16
    }
}

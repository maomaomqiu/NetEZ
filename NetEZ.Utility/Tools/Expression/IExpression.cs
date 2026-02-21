using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Utility.Tools.Expression
{
    public interface IExpression
    {
        ValueType ExpValType { get; set; }
        object ExpValue { get; set; }

        //IExpression Val1 { get; set; }
        //Operator Oper { get; set; }
        //IExpression Val2 { get; set; }

        
    }
}

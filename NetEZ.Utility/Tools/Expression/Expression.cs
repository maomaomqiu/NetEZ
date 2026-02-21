using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetEZ.Utility.Tools.Expression
{
    public enum ExpressionMode
    { 
        Const = 0,
        SingleVariable = 1,
        Expression = 2
    }

    public class Expression 
    {
        public Expression Var1;
        public Operator Oper;
        public Expression Var2;

        /// <summary>
        /// 表达式模型：常量、单变量、多变量表达式
        /// </summary>
        public ExpressionMode ExpMode;
        
        /// <summary>
        /// 表达式的返回值类型
        /// </summary>
        public ValueType ExpValType;

        /// <summary>
        /// 单变量名称
        /// </summary>
        public string SingleVarName;

        /// <summary>
        /// 常量值
        /// </summary>
        public object ConstValue;

        public bool LogicalCalc(NameValueCollection args, out bool retVal)
        {
            retVal = false;

            if (ExpMode == ExpressionMode.Const)
            {
                //  常量
                retVal = (bool)ConstValue;
                return true;
            }
            else if (ExpMode == ExpressionMode.SingleVariable)
            {
                //  单变量
                //  从args里找到匹配变量值
                if (args == null || args.Count < 1)
                    return false;

                string val = args[SingleVarName];
                //  可以为空串，但不能为null
                if (val == null)
                    return false;

                if (string.Compare(val, "true", true) == 0)
                    retVal = true;
                else if (string.Compare(val, "false", true) == 0)
                    retVal = false;
                else
                    return false;

                return true;
            }
            else
            {
                if (ExpValType != ValueType.Boolean)
                    return false;

                if (Oper == Operator.None)
                {
                    if (Var1 == null)
                        return false;

                    return Var1.LogicalCalc(args, out retVal);
                }

                if (Var1 == null || Var2 == null)
                    return false;

                bool var1CalcRet = false;
                bool var2CalcRet = false;

                //  Var2值类型与Var1相同，这里只用Var1值类型
                if (Var1.ExpValType == ValueType.Int64)     
                {
                    //  值类型的bool运算：'>' , '<' , '>=' ...
                    long var1Value;
                    long var2Value;

                    var1CalcRet = Var1.Int64Calc(args, out var1Value);
                    var2CalcRet = Var2.Int64Calc(args, out var2Value);

                    if (!var1CalcRet || !var2CalcRet)
                        return false;

                    return ProcLogicalIntCalc(var1Value, var2Value,out retVal);
                }
                else// if (Var1.ExpValType == ValueType.Boolean)
                {
                    //  布尔类型的bool运算：'&&' , '||'
                    bool var1Value;
                    bool var2Value;

                    var1CalcRet = Var1.LogicalCalc(args,out var1Value);
                    var2CalcRet = Var2.LogicalCalc(args, out var2Value);

                    if (!var1CalcRet || !var2CalcRet)
                        return false;

                    return ProcLogicalBoolCalc(var1Value, var2Value, out retVal);
                }
            }
        }

        public bool Int64Calc(NameValueCollection args,out long retVal)
        {
            retVal = 0;

            if (ExpMode == ExpressionMode.Const)
            {
                //  常量
                retVal = (long)ConstValue;
                return true;
            }
            else if (ExpMode == ExpressionMode.SingleVariable)
            {
                //  单变量
                //  从args里找到匹配变量值
                if (args == null || args.Count < 1)
                    return false;

                string val = args[SingleVarName];
                //  可以为空串，但不能为null
                if (val == null)
                    return false;

                return Int64.TryParse(val,out retVal);
            }
            else
            { 
                //  ExpType == ExpressionType.Expression
                if (ExpValType != ValueType.Int64)
                    return false;

                if (Oper == Operator.None)
                {
                    if (Var1 == null)
                        return false;

                    return Var1.Int64Calc(args, out retVal);
                }

                if (Var1 == null || Var2 == null)
                    return false;

                long val1Ret = 0;
                long val2Ret = 0;

                //  分别计算两个变量值
                bool val1CalcRet = Var1.Int64Calc(args, out val1Ret);
                bool val2CalcRet = Var2.Int64Calc(args, out val2Ret);

                if (!val1CalcRet || !val2CalcRet)
                    return false;

                return ProcInt64Calc(val1Ret, val2Ret, out retVal);    
            }
        }

        private bool ProcLogicalIntCalc(long v1,long v2, out bool retVal)
        {
            retVal = false;

            if (Oper == Operator.Greater)
            {
                retVal = v1 > v2 ? true : false;
                return true;
            }
            else if (Oper == Operator.GreaterOrEqual)
            {
                retVal = v1 >= v2 ? true : false;
                return true;
            }
            else if (Oper == Operator.Less)
            {
                retVal = v1 < v2 ? true : false;
                return true;
            }
            else if (Oper == Operator.LessOrEqual)
            {
                retVal = v1 <= v2 ? true : false;
                return true;
            }
            else if (Oper == Operator.Equal)
            {
                retVal = v1 == v2 ? true : false;
                return true;
            }
            else if (Oper == Operator.NotEqual)
            {
                retVal = v1 != v2 ? true : false;
                return true;
            }

            return false;
        }

        private bool ProcLogicalBoolCalc(bool v1, bool v2, out bool retVal)
        {
            retVal = false;

            if (Oper == Operator.LogicAnd)
            {
                retVal = v1 && v2 ? true : false;
                return true;
            }
            else if (Oper == Operator.LogicOr)
            {
                retVal = v1 || v2 ? true : false;
                return true;
            }

            return false;

        }

        private bool ProcInt64Calc(long v1,long v2, out long retVal)
        {
            retVal = 0;

            if (Oper == Operator.BitAnd)
            {
                //  位与
                retVal = v1 & v2;
                return true;
            }
            else if (Oper == Operator.BitOr)
            {
                //  位或
                retVal = v1 | v2;
                return true;
            }
            else if (Oper == Operator.BitXOr)
            {
                //  位或
                retVal = v1 ^ v2;
                return true;
            }
            else if (Oper == Operator.Plus)
            {
                retVal = v1 + v2;
                return true;
            }
            else if (Oper == Operator.Subtract)
            {
                retVal = v1 - v2;
                return true;
            }
            else if (Oper == Operator.Multiply)
            {
                retVal = v1 * v2;
                return true;
            }
            else if (Oper == Operator.Divide)
            {
                if (v2 == 0)
                    return false;

                retVal = v1 / v2;
                return true;
            }
            else if (Oper == Operator.Mod)
            {
                if (v2 == 0)
                    return false;

                retVal = v1 % v2;
                return true;
            }

            
            return false;
        }

        private static bool ParseOperString(string operString,ref int offset, out Operator oper)
        {
            oper = Operator.None;

            if (string.IsNullOrEmpty(operString) || offset < 0 || offset >= operString.Length)
                return false;

            if (operString[offset] == '=')
            {
                if (offset + 2 <= operString.Length && operString[offset + 1] == '=')
                    oper = Operator.Equal;
            }
            else if (operString[offset] == '!')
            {
                if (offset + 2 <= operString.Length && operString[offset + 1] == '=')
                    oper = Operator.NotEqual;
            }
            else if (operString[offset] == '+')
                oper = Operator.Plus;
            else if (operString[offset] == '-')
                oper = Operator.Subtract;
            else if (operString[offset] == '*')
                oper = Operator.Multiply;
            else if (operString[offset] == '/')
                oper = Operator.Divide;
            else if (operString[offset] == '%')
                oper = Operator.Mod;
            else if (operString[offset] == '>')
            {
                if (offset + 2 <= operString.Length && operString[offset + 1] == '=')
                    oper = Operator.GreaterOrEqual;
                else
                    oper = Operator.Greater;
            }
            else if (operString[offset] == '<')
            {
                if (offset + 2 <= operString.Length && operString[offset + 1] == '=')
                    oper = Operator.LessOrEqual;
                else
                    oper = Operator.Less;
            }
            else if (operString[offset] == '&')
            {
                if (offset + 2 <= operString.Length && operString[offset + 1] == '&')
                    oper = Operator.LogicAnd;
                else
                    oper = Operator.BitAnd;
            }
            else if (operString[offset] == '|')
            {
                if (offset + 2 <= operString.Length && operString[offset + 1] == '|')
                    oper = Operator.LogicOr;
                else
                    oper = Operator.BitOr;
            }
            else if (operString[offset] == '^')
                oper = Operator.BitXOr;

            if (oper != Operator.None)
            {
                if (oper == Operator.Equal || oper == Operator.NotEqual || oper == Operator.GreaterOrEqual || oper == Operator.LessOrEqual || oper == Operator.LogicAnd || oper == Operator.LogicOr)
                    offset += 2;
                else
                    offset += 1;

                return true;
            }

            return false;
        }

        private void PrepareExpressReturnValueType()
        {
            if (ExpValType != ValueType.Unknown)
                return;

            if (Oper == Operator.None)
            {
                //  单变量/常量时，尝试用该变量/常量的类型
                if (Var1.ExpValType != ValueType.Unknown)
                    ExpValType = Var1.ExpValType;

                //  无法预知类型
                return;
            }

            //  有运算符时，通过运算符判断表达式的返回值类型

            if (Oper == Operator.Plus ||
                Oper == Operator.Subtract ||
                Oper == Operator.Multiply ||
                Oper == Operator.Divide ||
                Oper == Operator.Mod ||
                Oper == Operator.BitAnd ||
                Oper == Operator.BitOr ||
                Oper == Operator.BitXOr
                )
            {
                ExpValType = ValueType.Int64;
                //  这时var1,var2都是int类型
                if (Var1.ExpValType != ValueType.Int64)
                    Var1.ExpValType = ValueType.Int64;

                if (Var2.ExpValType != ValueType.Int64)
                    Var2.ExpValType = ValueType.Int64;
            }
            else
            {
                ExpValType = ValueType.Boolean;
                if (Oper == Operator.Greater || Oper == Operator.GreaterOrEqual ||
                    Oper == Operator.Less || Oper == Operator.LessOrEqual || 
                    Oper == Operator.Equal || Oper == Operator.NotEqual
                    )
                { 
                    //  这时运算符两边都是int
                    if (Var1.ExpValType != ValueType.Int64)
                        Var1.ExpValType = ValueType.Int64;

                    if (Var2.ExpValType != ValueType.Int64)
                        Var2.ExpValType = ValueType.Int64;
                }
                else if (Oper == Operator.LogicAnd || Oper == Operator.LogicOr)
                {
                    //  这时运算符两边都是boolean
                    if (Var1.ExpValType != ValueType.Boolean)
                        Var1.ExpValType = ValueType.Boolean;

                    if (Var2.ExpValType != ValueType.Boolean)
                        Var2.ExpValType = ValueType.Boolean;
                }
            }
                
        }

        public static bool TryParse(string input, out Expression exp)
        { 
            int end = 0;
            return TryParse(input, 0, ref end, out exp);
        }

        private static bool TryParse(string input, int start, ref int end, out Expression exp)
        {
            exp = null;
            //  intput格式
            //  1. 表达式类型:常量、单变量、双变量（及运算符）
            //  2. 表达式使用括号
            //  3. 括号/变量/常量与运算符之间必须有空格
            //  example: ((@userid % 10) = 0)
            if (string.IsNullOrEmpty(input))
                return false;
            
            if (start == 0)
                input = input.Trim().ToLower();

            if (input.Length == 0 || start >= input.Length)
                return false;
            try 
            {
                Operator oper = Operator.None;
                Expression subExp = null;
                int offset = start;

                while (offset < input.Length)
                {
                    if (input[offset] == ' ')
                    {
                        offset ++;
                        continue;
                    }

                    //  遇到变量/常量
                    if (
                        (input[offset] >= 'a' && input[offset] <= 'z') ||
                        (input[offset] >= '0' && input[offset] <= '9') ||
                        (input[offset] == '@')
                        )
                    {
                        int p1 = offset;
                        while (p1 < input.Length &&
                            (
                                (input[p1] >= 'a' && input[p1] <= 'z') ||
                                (input[p1] >= '0' && input[p1] <= '9') ||
                                (input[p1] == '@')
                            )
                        )
                        {
                            p1++;
                            continue;
                        }

                        string varTmp = input.Substring(offset, p1 - offset);

                        subExp = new Expression();
                        if (varTmp[0] == '@')
                        {
                            //  变量
                            subExp.ExpMode = ExpressionMode.SingleVariable;
                            subExp.SingleVarName = varTmp;
                        }
                        else
                        {
                            //  常量
                            subExp.ExpMode = ExpressionMode.Const;
                            long intVal = 0;
                            if (Int64.TryParse(varTmp, out intVal))
                            {
                                subExp.ExpValType = ValueType.Int64;
                                subExp.ConstValue = intVal;
                            }
                            else
                            {
                                if (string.Compare(varTmp, "true", true) == 0)
                                {
                                    subExp.ExpValType = ValueType.Boolean;
                                    subExp.ConstValue = true;
                                }
                                else if (string.Compare(varTmp, "true", true) == 0)
                                {
                                    subExp.ExpValType = ValueType.Boolean;
                                    subExp.ConstValue = false;
                                }
                                else
                                    return false;
                            }
                        }

                        offset = p1;

                    }
                    else if (input[offset] == '(')
                    {
                        //  嵌套表达式

                        int endTmp = 0;
                        if (!TryParse(input, offset + 1, ref endTmp, out subExp))
                            return false;

                        if (exp == null)
                        {
                            exp = new Expression();
                            exp.Oper = Operator.None;
                            exp.ExpMode = ExpressionMode.Expression;
                        }
                        offset = endTmp;
                    }
                    else if (input[offset] == ')')
                    {
                        //  当前表达式结束了

                        end = offset + 1;
                        
                        exp.PrepareExpressReturnValueType();
                        return true;
                    }
                    else if (ParseOperString(input,ref offset, out oper))
                    {
                        //  运算符
                        if (exp.Var1 == null)
                            return false;

                        exp.ExpMode = ExpressionMode.Expression;

                        exp.Oper = oper;
                    }
                    else
                        offset++;

                    if (subExp != null)
                    {
                        if (exp == null)
                        {
                            exp = new Expression();
                            exp.Oper = Operator.None;
                        }

                        if (exp.Var1 == null)
                            exp.Var1 = subExp;
                        else if (exp.Var2 == null)
                            exp.Var2 = subExp;
                        else
                            return false;

                        subExp = null;
                    }
                }

                exp.PrepareExpressReturnValueType();

                return true;
            }
            catch { }
            finally { }

            return false;
        }
    }
}

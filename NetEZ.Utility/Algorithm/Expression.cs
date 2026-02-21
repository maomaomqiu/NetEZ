using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetEZ.Utility.Algorithm
{
    public enum VariableType
    {
        Unknow = 0,
        UserId = 1,
        ClassId = 2,
        SchoolId = 4,
        IpAddress = 8,
        ConstInt32 = 1024,
        Expression = 2048
    }

    public enum Operator
    {
        Unknow = 0,
        Equal = 1,
        Plus = 2,
        Subtract = 3,
        Multiply = 4,
        Divide = 5,
        Mod = 6,
        Greater = 7,
        GreaterOrEqual = 8,
        Less = 9,
        LessOrEqual = 10,
        And = 11,
        Or = 12,
    }

    public enum ExpressionProcState
    { 
        Initial = 0,
        WaitingVar1 = 1,
        ReadingVar1 = 2,
        WaitingOperator = 3,
        ReadingOperator = 4,
        WaitingVar2 = 5,
        ReadingVar2 = 6,
        Completed = 7,
    }

    public class Expression
    {
        private ExpressionProcState _State;
        private int _VariableNeeded = 0;

        public ExpressionProcState State { get { return _State; } }
        public bool IsCompleted { get { return _State == ExpressionProcState.Completed ? true : false; } }
        public VariableType Var1Type { get; set; }
        public object Var1 { get; set; }
        public Operator Oper { get; set; }
        public VariableType Var2Type { get; set; }
        public object Var2 { get; set; }
        public int VariableNeededTag { get { return _VariableNeeded; } set { _VariableNeeded = value; } }

        public Expression()
        {
            SetState(ExpressionProcState.WaitingVar1);
        }

        public void SetState(ExpressionProcState state)
        {
            _State = state;
        }

        private VariableType ParseVariableTypeFromString(string input,out int constValue)
        {
            constValue = 0;

            if (string.Compare(input, VariableType.UserId.ToString(), true) == 0)
                return VariableType.UserId;
            else if (string.Compare(input, VariableType.ClassId.ToString(), true) == 0)
                return VariableType.ClassId;
            else if (string.Compare(input, VariableType.SchoolId.ToString(), true) == 0)
                return VariableType.SchoolId;
            else if (string.Compare(input, VariableType.IpAddress.ToString(), true) == 0)
                return VariableType.IpAddress;
            else
            { 
                if (Int32.TryParse(input, out constValue))
                    return VariableType.ConstInt32;
            }
            return VariableType.Unknow;
        }

        private void SetVar1(VariableType type, object var)
        {
            Var1Type = type;
            if (type == VariableType.ConstInt32 || type == VariableType.Expression)
                Var1 = var;
        }

        private void SetVar2(VariableType type, object var)
        {
            Var2Type = type;
            if (type == VariableType.ConstInt32 || type == VariableType.Expression)
                Var2 = var;
        }

        public bool SetOperator(Operator oper)
        {
            if (oper == Operator.Unknow)
                return false;

            Oper = oper;
            SetState(ExpressionProcState.WaitingVar2);

            return true;
        }

        public bool SetOperator(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return false;

            raw = raw.Trim().ToLower();
            if (raw.Length < 1)
                return false;

            if (string.Compare(raw, "+") == 0)
                return SetOperator(Operator.Plus);
            else if (string.Compare(raw, "-") == 0)
                return SetOperator(Operator.Subtract);
            else if (string.Compare(raw, "*") == 0)
                return SetOperator(Operator.Multiply);
            else if (string.Compare(raw, "/") == 0)
                return SetOperator(Operator.Divide);
            else if (string.Compare(raw, "%") == 0)
                return SetOperator(Operator.Mod);
            else if (string.Compare(raw, ">") == 0)
                return SetOperator(Operator.Greater);
            else if (string.Compare(raw, ">=") == 0)
                return SetOperator(Operator.GreaterOrEqual);
            else if (string.Compare(raw, "<") == 0)
                return SetOperator(Operator.Less);
            else if (string.Compare(raw, "<=") == 0)
                return SetOperator(Operator.LessOrEqual);
            else if (string.Compare(raw, "=") == 0)
                return SetOperator(Operator.Equal);
            else if (string.Compare(raw, "and", true) == 0)
                return SetOperator(Operator.And);
            else if (string.Compare(raw, "or") == 0)
                return SetOperator(Operator.Or);
            else
                return SetOperator(Operator.Unknow);
        }

        public bool SetVariable(Expression exp)
        {
            if (exp == null)
                return false;

            if (State == ExpressionProcState.ReadingVar1)
            {
                SetVar1(VariableType.Expression, exp);
                SetState(ExpressionProcState.WaitingOperator);
            }
            else
            {
                SetVar2(VariableType.Expression, exp);
                SetState(ExpressionProcState.Completed);
            }

            return true;
        }

        public bool SetVariable(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return false;

            raw = raw.Trim().ToLower();
            if (raw.Length < 1)
                return false;

            string rawNoTag = "";
            if (raw.StartsWith("#", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!raw.EndsWith("#", StringComparison.CurrentCultureIgnoreCase) || raw.Length < 3)
                    return false;

                rawNoTag = raw.Substring(1, raw.Length - 2);
            }
            else
                rawNoTag = raw;

            int constVal = 0;
            VariableType varType = ParseVariableTypeFromString(rawNoTag, out constVal);
            
            NeedVariable(varType);

            if (State == ExpressionProcState.ReadingVar1)
            {
                SetVar1(varType, constVal);
                SetState(ExpressionProcState.WaitingOperator);
            }
            else
            {
                SetVar2(varType, constVal);
                SetState(ExpressionProcState.Completed);
            }

            return true;
        }

        public void NeedVariable(VariableType type)
        {
            _VariableNeeded = _VariableNeeded | (int)type;
        }

        /// <summary>
        /// 有变量（赋值）的计算
        /// </summary>
        /// <param name="vars"></param>
        /// <returns></returns>
        public int CalcIntResult(Dictionary<VariableType,int> vars)
        { 
            //if ((_VariableNeeded & (int)VariableType.UserId) > 0 && vars.ContainsKey())
            int result = 0;

            int var1Value = 0;
            int var2Value = 0;

            switch (Var1Type)
            { 
                case VariableType.Expression:
                    Expression var1Exp = (Expression)Var1;
                    var1Value = var1Exp.CalcIntResult(vars);
                    break;
                case VariableType.ConstInt32:
                    var1Value = (int)Var1;
                    break;
                default:
                    if (vars == null || !vars.ContainsKey(Var1Type))
                        throw new Exception("variable is null.");
                    else
                    {
                        var1Value = (int)vars[Var1Type];
                    }
                    break;
            }

            switch (Var2Type)
            {
                case VariableType.Expression:
                    Expression var2Exp = (Expression)Var2;
                    var2Value = var2Exp.CalcIntResult(vars);
                    break;
                case VariableType.ConstInt32:
                    var2Value = (int)Var2;
                    break;
                default:
                    if (vars == null || !vars.ContainsKey(Var2Type))
                        throw new Exception("variable is null.");
                    else
                    {
                        var2Value = (int)vars[Var2Type];
                    }
                    break;
            }

            switch (Oper)
            {
                case Operator.Equal:
                    if (var1Value == var2Value)
                        result = 1;
                    break;
                case Operator.Plus:
                    result = var1Value + var2Value;
                    break;
                case Operator.Subtract:
                    result = var1Value - var2Value;
                    break;
                case Operator.Multiply:
                    result = var1Value * var2Value;
                    break;
                case Operator.Divide:
                    result = var1Value / var2Value;
                    break;
                case Operator.Mod:
                    result = var1Value % var2Value;
                    break;
                case Operator.Greater:
                    if (var1Value > var2Value)
                        result = 1;
                    break;
                case Operator.Less:
                    if (var1Value < var2Value)
                        result = 1;
                    break;
                case Operator.GreaterOrEqual:
                    if (var1Value >= var2Value)
                        result = 1;
                    break;
                case Operator.LessOrEqual:
                    if (var1Value <= var2Value)
                        result = 1;
                    break;
                case Operator.And:
                    if (var1Value > 0 && var2Value > 0)
                        result = 1;
                    break;
                case Operator.Or:
                    if (var1Value > 0 || var2Value > 0)
                        result = 1;
                    break;
            }

            return result;
        }

        /// <summary>
        /// 无变量的计算
        /// </summary>
        /// <returns></returns>
        public int CalcIntResult()
        {
            return CalcIntResult(null);
        }
    }

    public class ExpressionHelper
    {

        public ExpressionHelper()
        {

        }

        public bool LoadExpression(string expString, ref int index, out Expression newExpression)
        {
            /*
             (((#user_id# % 10) = 1) or ((#school# % 10) = 2))
             
             */
            Expression currExpression = null;

            newExpression = null;

            if (string.IsNullOrEmpty(expString))
                return false;

            expString = expString.Trim();
            int len = expString.Length;
            bool closed = false;
            int offset = index;
            string expPart = "";

            currExpression = new Expression();

            while (offset < len && expString[offset] != '(')
                offset++;

            if (offset >= len)
                return false;

            offset++;
            index = offset;

            while (offset < len && !closed)
            {
                if (expString[offset] == ')')
                {
                    closed = true;
                    break;
                }

                if (expString[offset] == '(')
                {
                    //  遇见新的表达式
                    //offset++;
                    if (currExpression.State == ExpressionProcState.WaitingVar1)
                        currExpression.SetState(ExpressionProcState.ReadingVar1);
                    else if (currExpression.State == ExpressionProcState.WaitingVar2)
                        currExpression.SetState(ExpressionProcState.ReadingVar2);
                    else
                        return false;

                    bool loadRet = LoadExpression(expString, ref offset, out newExpression);
                    if (!loadRet || newExpression == null)
                        return false;
                    //  读取了该表达式, offset停在')'位置
                    currExpression.SetVariable(newExpression);
                    currExpression.VariableNeededTag = currExpression.VariableNeededTag | newExpression.VariableNeededTag;
                    offset++;
                    //  忽略后面可能的无效空格
                    while (offset < len && expString[offset] == ' ')
                        offset++;
                    index = offset;
                }
                else if (expString[offset] == ' ')
                {
                    if (currExpression.State == ExpressionProcState.WaitingVar1 || currExpression.State == ExpressionProcState.WaitingOperator || currExpression.State == ExpressionProcState.WaitingVar2)
                    {
                        //  忽略无效空格
                        offset++;
                        index = offset;
                        continue;
                    }

                    expPart = expString.Substring(index, offset - index);
                    if (currExpression.State == ExpressionProcState.ReadingVar1 || currExpression.State == ExpressionProcState.ReadingVar2)
                    {
                        if (!currExpression.SetVariable(expPart))
                            return false;
                    }
                    else if (currExpression.State == ExpressionProcState.ReadingOperator)
                    {
                        if (!currExpression.SetOperator(expPart))
                            return false;
                    }
                    else
                        return false;               //  目前只支持两个变量

                    offset++;
                    index = offset;
                }
                else
                {
                    if (currExpression.State == ExpressionProcState.WaitingVar1)
                        currExpression.SetState(ExpressionProcState.ReadingVar1);
                    else if (currExpression.State == ExpressionProcState.WaitingOperator)
                        currExpression.SetState(ExpressionProcState.ReadingOperator);
                    else if (currExpression.State == ExpressionProcState.WaitingVar2)
                        currExpression.SetState(ExpressionProcState.ReadingVar2);

                    offset++;
                }
            }

            if (!closed)
                return false;

            if (currExpression.State == ExpressionProcState.ReadingVar2)
            {
                expPart = expString.Substring(index, offset - index);
                if (!currExpression.SetVariable(expPart))
                    return false;
            }

            index = offset;

            if (!currExpression.IsCompleted)
                return false;

            newExpression = currExpression;

            return true;
        }

        public static bool CalcExpressionIntValue(string exp, out int value)
        {
            value = 0;

            ExpressionHelper core = new ExpressionHelper();
            int index = 0;

            Expression expression = null;

            try
            {
                bool valid = core.LoadExpression(exp, ref index, out expression);
                if (!valid || expression == null)
                    return false;
                value = expression.CalcIntResult();

                return true;
            }
            catch { }

            return false;

        }
    }
}

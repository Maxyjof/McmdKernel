

using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace McmdKernel
{
    public class Parser
    {
        /// <summary>
        /// 词素序列
        /// </summary>
        public List<Token> Tokens = null;

        /// <summary>
        /// 词素序列下标
        /// </summary>
        public int Position = 0;

        public string CurrentText
        {
            get { return Tokens[Position].Text; }
        }

        public int Row = 1;

        public List<string> Results = new();
        public List<string> Dialogue = new();

        public Parser(List<Token> tokens)
        {
            Tokens = tokens;
        }

        public List<string> Scan()
        {
            while (Position + 1 < Tokens.Count)
                Results.AddRange(NextCode());

            return Results;
        }

        public List<string> NextCode()
        {
            List<string> results = new List<string>();

            if (Match(Kind.LineFeed))
                Next();

            if (Match(Kind.Word))
            {
                Statement();
            }

            return results;
        }

        #region 常用方法

        public void Next()
        {
            if (Position + 1 < Tokens.Count)
                Position++;
        }

        public string MakeError(string str) => $"ERROR<第{Row}行>:{str}";

        /// <summary>
        /// 判断当前是不是对应的词素
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        private bool Match(Kind kind) => Tokens[Position].Kind == kind;

        /// <summary>
        /// 判断下一个是不是对应的词素
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        private bool NextMatch(Kind kind)
        {
            if (Position + 1 < Tokens.Count)
                if (Tokens[Position + 1].Kind == kind)
                {
                    Position++;
                    return true;
                }
                else
                    return false;
            else
                return false;
        }

        private bool NextMatch(string str)
        {
            if (Position + 1 < Tokens.Count)
                if (Tokens[Position + 1].Text == str)
                {
                    Position++;
                    return true;
                }
                else
                    return false;
            else
                return false;
        }

        /// <summary>
        /// 如果下一个是空格，就跳过
        /// </summary>
        private void Skip() => NextMatch(Kind.WhiteSpace);

        /// <summary>
        /// 如果有空格，就跳过，然后再判断下一个是不是对应的词素
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        private bool SkipAndNextMatch(Kind kind)
        {
            NextMatch(Kind.WhiteSpace);
            return NextMatch(kind);
        }

        private bool SkipAndNextMatch(string str)
        {
            NextMatch(Kind.WhiteSpace);
            return NextMatch(str);
        }
        #endregion

        /// <summary>
        /// 表达式分析
        /// </summary>
        public List<string> Expression(string expression)
        {
            List<string> results = new List<string>();

            int tN = 0;
            string T = "";

            Regex plus = new(@"([\d]+|[@#a-zA-Z_][a-zA-Z0-9_]*)(\+|\-)([\d]+|[@#a-zA-Z_][a-zA-Z0-9_]*)");
            Regex star = new(@"([\d]+|[@#a-zA-Z_][a-zA-Z0-9_]*)(\*|/)([\d]+|[@#a-zA-Z_][a-zA-Z0-9_]*)");
            #region 分析阶段
            while (expression.Contains('('))//进入最深层的小括号
            {
                string str = expression[expression.IndexOf('(')..];
                T = str[..(str.IndexOf(')') + 1)];
                string oldT = T;

                while (star.IsMatch(T))//将所有的乘除法替换
                {
                    string s = star.Match(T).Value;
                    results.Add($"t{tN} = {s}");
                    T = T.Replace(s, $"t{tN++}");
                }
                while (plus.IsMatch(T))//将所有的加减法替换
                {
                    string s = plus.Match(T).Value;
                    results.Add($"t{tN} = {s}");
                    T = T.Replace(s, $"t{tN++}");
                }
                //(t1)
                //删除括号
                T = T[1..(T.Length - 1)];
                expression = expression.Replace(oldT, T);
            }

            while (star.IsMatch(expression))//将所有的乘除法替换
            {
                string s = star.Match(expression).Value;
                results.Add($"t{tN} = {s}");
                expression = expression.Replace(s, $"t{tN++}");
            }
            while (plus.IsMatch(expression))//将所有的加减法替换
            {
                string s = plus.Match(expression).Value;
                results.Add($"t{tN} = {s}");
                expression = expression.Replace(s, $"t{tN++}");
            }
            #endregion


            while (NeedToImprove(results))
            {
                #region 优化阶段

                Regex tRegex = new(@"(?<name>t[\d]+) = (?<content>[+\-]?[\d]+\s*.\s*[\+\-*/\|<>^%&]?[\d]+)");
                //常量合并
                for (int i = 0; i < results.Count; i++)
                {
                    if (tRegex.IsMatch(results[i]))
                    {
                        Match m = tRegex.Match(results[i]);
                        string name = m.Groups["name"].Value;
                        string content = m.Groups["content"].Value;
                        results[i] = $"{name} = {Evaluate(content)}";
                    }
                }

                //收集 变量=常量 的情况
                Dictionary<string, string> keyList = new();
                for (int i = 0; i < results.Count; i++)
                {
                    Regex regex = new(@"(?<name>t[\d]+) = (?<value>[\+\-]?[\d]+)$");
                    if (regex.IsMatch(results[i]))
                    {
                        Match m = regex.Match(results[i]);
                        keyList.Add(m.Groups["name"].Value, m.Groups["value"].Value);
                        //results.RemoveAt(i);
                    }
                }

                //将那些已知的变量替换为对应的值
                for (int i = 0; i < results.Count; i++)
                {
                    Regex regex = new(@"(?<name>\w+) = (?<left>[+\-]?\w+)\s*(?<symbol>.)\s*(?<right>[+\-]?\w+)");
                    if (regex.IsMatch(results[i]))
                    {
                        Match m = regex.Match(results[i]);
                        string left = m.Groups["left"].Value;
                        string right = m.Groups["right"].Value;
                        if (keyList.ContainsKey(left))
                            results[i] = $"{m.Groups["name"].Value} = {keyList[left]} {m.Groups["symbol"]} {right}";
                        if (keyList.ContainsKey(right))
                            results[i] = $"{m.Groups["name"].Value} = {left} {m.Groups["symbol"]} {keyList[right]}";
                        for (int j = 0; j < results.Count; j++)
                        {
                            if (results[j].StartsWith(right) || results[j].StartsWith(left))
                            {
                                results.RemoveAt(j);
                            }
                        }
                    }
                }
                #endregion
                //if (results.Count == 1)
                //    break;
            }

            return results;
        }

        private bool NeedToImprove(List<string> list)
        {
            Regex regex1 = new(@"t[\d]+ = [+\-]?[\d]+\s*[\+\-*/\|<>^%&]\s*[+\-]?[\d]+");
            Regex regex2 = new(@"(?<name>t[\d]+) = (?<value>[\+\-]?[\d]+)$");
            Regex regex3 = new(@"(?<name>\w+) = (?<left>[+\-]?\w+)\s*(?<symbol>.)\s*(?<right>[+\-]?\w+)");
            //Regex tRegex = new(@"t[\d]+ = [+\-]?[\d]+\s*.\s*[+\-]?[\d]+");
            bool isTrue = false;
            foreach (string item in list)
            {
                if (regex1.IsMatch(item))
                    isTrue = true;
                else if (regex2.IsMatch(item))
                {
                    if (list.Count != 1)
                        isTrue = true;
                    else
                        isTrue = false;
                }
                else if (regex3.IsMatch(item))
                    isTrue = true;
            }
            return isTrue;
        }

        public string Evaluate(string content)
        {
            Regex regex = new(@"(?<left>[+\-]?[0-9]+)\s*(?<symbol>[+\-*/%])\s*(?<right>[+\-]?[0-9]+)");
            string str = content;
            if (regex.IsMatch(content))
            {
                Match m = regex.Match(content);
                string symbol = m.Groups["symbol"].Value;
                int left = int.Parse(m.Groups["left"].Value);
                int right = int.Parse(m.Groups["right"].Value);
                int result = 0;
                switch (symbol)
                {
                    case "+":
                        result = left + right;
                        break;
                    case "-":
                        result = left - right;
                        break;
                    case "*":
                        result = left * right;
                        break;
                    case "/":
                        result = left / right;
                        break;
                    case "%":
                        result = left % right;
                        break;
                }
                str = result.ToString();
            }
            return str;
        }

        #region 语法分析步骤
        private void Statement()
        {
            List<string> results = new List<string>();
            string scoreboard = CurrentText;
            string name = null;
            string score = null;
            if (SkipAndNextMatch(Kind.Word))
            {
                name = CurrentText;
                if (SkipAndNextMatch("="))
                {
                    if (SkipAndNextMatch(Kind.Number))
                    {
                        score = CurrentText;
                        Next();
                    }
                }
            }
            if (score == null)
                if (name == null)
                    Dialogue.Add(MakeError("声明语法错误!"));
                else
                    results.Add($"\"{scoreboard}\".\"{name}\" = 0");
            else
                results.Add($"\"{scoreboard}\".\"{name}\" = {score}");
        }
        #endregion
    }
}

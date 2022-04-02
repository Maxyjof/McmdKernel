

using System.Text;
using System.Text.RegularExpressions;

namespace McmdKernel
{
    /// <summary>
    /// 对单个文件的字符序列处理的类
    /// </summary>
    public class Lexer
    {
        /// <summary>
        /// 文件的字符序列，会逐渐缩减
        /// </summary>
        public StringBuilder Text = null;

        public Lexer(string fileText)
        {
            Text = new StringBuilder(fileText);
        }

        /// <summary>
        /// 从字符序列中获取到下一个词素
        /// </summary>
        /// <returns></returns>
        public Token NextToken()
        {
            if (Text.Length <= 0)
                return new Token(null, "", Kind.EOF);

            Token token = new Token(null, "", Kind.BadToken);
            string text = Text.ToString();
            int length = 0;

            Regex symbol = new Regex("^[+\\-*/\\\\\\;\\:\\[\\]{}()|.,=<>^%!&]+");
            Regex number = new Regex(@"^[\d]+");
            Regex word = new Regex(@"^[@#a-zA-Z_][a-zA-Z0-9_]*");
            Regex whiteSpace = new Regex(@"^[\s]+");
            Regex LineFeed = new Regex(@"^(\r\n)+");

            #region 词素分析
            if (LineFeed.IsMatch(text))
            {
                length = LineFeed.Match(text).Value.Length;
                token = new Token("\r\n", "\r\n", Kind.LineFeed);
            }
            else if (whiteSpace.IsMatch(text))
            {
                length = whiteSpace.Match(text).Value.Length;
                token = new Token(" ", " ", Kind.WhiteSpace);
            }
            else if (symbol.IsMatch(text))
            {
                string str = symbol.Match(text).Value;
                length = str.Length;
                token = new Token(str, str, Kind.Symbol);
            }
            else if (number.IsMatch(text))
            {
                string str = number.Match(text).Value;
                length = str.Length;
                token = new Token(str, str, Kind.Number);
            }
            else if (text[0] == '\"')
            {
                int start = 0, pos = 0;
                do//"123"
                {
                    pos++;
                } while (text.Substring(pos - 1, 2) != "\\\"" || text[pos] == '\"');
                //是"且不是\"
                //!(是"&&不是\")
                pos++;
                length = pos - start;
                token = new Token(text.Substring(0 + 1, length - 1), text.Substring(0, length), Kind.ConstString);
            }
            else
            {
                List<string> keyWordList = GetKeyWord();
                bool isKeyWord = false;
                foreach (var item in keyWordList)
                {
                    if (text.StartsWith(item))
                    {
                        length = item.Length;
                        token = new Token(item, item, Kind.KeyWord);
                        isKeyWord = true;
                        break;
                    }
                }

                if (!isKeyWord && word.IsMatch(text))
                {
                    string str = word.Match(text).Value;
                    length = str.Length;
                    token = new Token(str, str, Kind.Word);
                }
            }
            #endregion
            Text.Remove(0, length);
            return token;
        }

        private List<string> GetKeyWord()
        {
            return new List<string>
            {
                "def",
                "var",
                "class",

            };
        }
    }
}

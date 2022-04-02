

namespace McmdKernel
{
    /// <summary>
    /// 词素，一个基本单元
    /// </summary>
    public class Token
    {
        public object Attributes = null;
        public string Text = "";
        public Kind Kind = Kind.BadToken;
        public Token()
        {

        }
        public Token(object a, string text, Kind kind)
        {
            Attributes = a;
            Text = text;
            Kind = kind;
        }
    }
}

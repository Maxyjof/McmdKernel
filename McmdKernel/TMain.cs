

namespace McmdKernel
{
    public class TMain
    {
        static void Main()
        {
            //Lexer lexer = new Lexer("int a = 1\r\nint b =3");

            //List<Token> tokens = new();
            //do
            //    tokens.Add(lexer.NextToken());
            //while (tokens.Last().Text != "");

            //Parser parser = new(tokens);

            //foreach (var item in parser.Scan())
            //{
            //    Console.WriteLine(item);
            //}
            Parser parser = new(new List<Token>());
            string str = "12+52*75-(69*8)";
            Console.WriteLine($"{str} =>");
            foreach (var item in parser.Expression(str))
            {
                Console.WriteLine(item);
            }
        }

        static void Test()
        {

        }
    }
}
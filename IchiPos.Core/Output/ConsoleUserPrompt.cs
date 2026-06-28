namespace IchiPos.Output;

public class ConsoleUserPrompt : IUserPrompt
{
    public string? Ask(string question)
    {
        Console.Write(question);
        return Console.ReadLine();
    }
}

namespace Otm.Server.Helpers;

public class LogObject
{
    public LogObject(string codigo, string mensagem, LogType logType)
    {
        Codigo = codigo;
        Mensagem = mensagem;
        LogType = logType;
    }

    public string Codigo { get; private set; }
    public string Mensagem { get; private set; }
    public LogType LogType { get; private set; }
}
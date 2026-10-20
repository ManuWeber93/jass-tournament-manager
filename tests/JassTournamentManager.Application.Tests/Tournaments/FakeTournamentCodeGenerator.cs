using JassTournamentManager.Application.Tournaments;

namespace JassTournamentManager.Application.Tests.Tournaments
{
    internal sealed class FakeTournamentCodeGenerator : ITournamentCodeGenerator
    {
        public Queue<string> Candidates { get; } = new(["CODE01", "CODE02", "CODE03", "CODE04", "CODE05", "CODE06"]);
        public List<string> GeneratedCodes { get; } = [];

        public string Generate()
        {
            string code = Candidates.Dequeue();
            GeneratedCodes.Add(code);
            return code;
        }
    }
}
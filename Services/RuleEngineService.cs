using System.Text.Json;
using PNETGuard.Models;
namespace PNETGuard.Services;
public sealed class RuleEngineService
{
    private readonly IReadOnlyList<DetectionRule> _rules;
    public RuleEngineService(){_rules=LoadRules();}
    public GuardianEvaluation Evaluate(bool steamValidated,bool accessLimited,IReadOnlyList<AntiCheatFinding> findings)
    {
        int score=100; var reasons=new List<string>();
        if(!steamValidated){score-=25;reasons.Add("Instalação Steam não validada: -25");}
        if(accessLimited){score-=15;reasons.Add("Acesso incompleto durante o scan: -15");}
        foreach(var f in findings)
        {
            int impact=f.Severity.Equals("confirmed_cheat",StringComparison.OrdinalIgnoreCase)?60:f.Severity.Equals("suspect",StringComparison.OrdinalIgnoreCase)?25:10;
            var rule=_rules.FirstOrDefault(r=>r.Enabled && (f.Code.Equals(r.Pattern,StringComparison.OrdinalIgnoreCase)||f.Summary.Contains(r.Pattern,StringComparison.OrdinalIgnoreCase)));
            if(rule is not null) impact=Math.Max(0,rule.ScoreImpact);
            score-=impact; reasons.Add($"{f.Code}: -{impact}");
        }
        score=Math.Clamp(score,0,100);
        string classification=score>=96?"approved":score>=80?"review":"rejected";
        if(findings.Any(f=>f.Severity.Equals("confirmed_cheat",StringComparison.OrdinalIgnoreCase)))classification="confirmed_cheat";
        return new(score,classification,reasons);
    }
    private static IReadOnlyList<DetectionRule> LoadRules()
    {
        string dir=Path.Combine(AppContext.BaseDirectory,"rules"); string path=Path.Combine(dir,"guardian_rules.json");
        try{if(File.Exists(path))return JsonSerializer.Deserialize<List<DetectionRule>>(File.ReadAllText(path),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??new();}catch{}
        return new List<DetectionRule>();
    }
}

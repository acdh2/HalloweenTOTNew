using System;
using System.Collections.Generic;
using System.Diagnostics;

public class Rule
{
    // When you shouldn't open the door: "Don't open the door if <Description>".
    public string Description;
    // What went wrong when the rule is broken, used as feedback.
    public string Feedback;
    // True when the visitors are dangerous according to this rule.
    public Func<bool> Condition;

    public Rule(string description, string feedback, Func<bool> condition)
    {
        Description = description;
        Feedback = feedback;
        Condition = condition;
    }

    public bool Test() => Condition?.Invoke() ?? false;
}

public class RuleSet
{
    public static List<Rule> all = new List<Rule>
    {

    };

    // Every rule that has been in play, in the order it was introduced: rule #n
    // is at index n - 1. Turned off rules stay in here, so their number never
    // changes and a new rule always gets a higher number.
    private readonly List<Rule> _rules = new List<Rule>();
    private readonly HashSet<Rule> _inactive = new HashSet<Rule>();

    // Number of active rules.
    public int Count => _rules.Count - _inactive.Count;
    public int InactiveCount => _inactive.Count;
    // Whether there is a rule in all that hasn't been in play yet.
    public bool CanAddRule => _rules.Count < all.Count;

    // Brings a random rule into play that hasn't been used before.
    public string AddRule(out int number)
    {
        List<Rule> available = all.FindAll(r => !_rules.Contains(r));
        number = -1;
        if (available.Count == 0) return "";

        Rule newRule = available[UnityEngine.Random.Range(0, available.Count)];
        _rules.Add(newRule);
        number = _rules.Count;
        return newRule.Description;
    }

    // Turns off a random active rule. It keeps its number, ActivateRule can turn it on again.
    public string DeactivateRule(out int number)
    {
        return PickAndSwitch(_rules.FindAll(r => !_inactive.Contains(r)), false, out number);
    }

    // Turns a random turned off rule on again, with its old number.
    public string ActivateRule(out int number)
    {
        return PickAndSwitch(new List<Rule>(_inactive), true, out number);
    }

    private string PickAndSwitch(List<Rule> candidates, bool activate, out int number)
    {
        number = -1;
        if (candidates.Count == 0) return "";

        Rule rule = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        if (activate) _inactive.Remove(rule);
        else _inactive.Add(rule);
        number = _rules.IndexOf(rule) + 1;
        return rule.Description;
    }

    public IEnumerable<string> GetRules()
    {
        foreach (var rule in _rules)
        {
            if (!_inactive.Contains(rule)) yield return rule.Description;
        }
    }

    // Active rules whose condition is true, meaning you shouldn't have opened the door.
    public List<string> BrokenRules()
    {
        List<string> broken = new List<string>();
        foreach (var rule in _rules)
        {
            if (!_inactive.Contains(rule) && rule.Test()) broken.Add(rule.Feedback);
        }
        return broken;
    }
}
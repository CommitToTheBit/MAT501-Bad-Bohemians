/* Narrative (used interchangably with 'Puzzle' in the report): Class for containing clues */ 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Narrative
{
    public List<Clue> clues;
    public Dictionary<string, Dictionary<string, Element>> data;

    public int unknowns;
    public int ambiguities;
    public int obviousness;

    public Narrative()
    {
        clues = new List<Clue>();

        data = new Dictionary<string, Dictionary<string, Element>>();

        unknowns = 0;
        ambiguities = 0;
        obviousness = int.MaxValue;
    }

    public Narrative(List<Clue> init_clues, Dictionary<string, Dictionary<string, Element>> init_data, int init_unknowns, int init_ambiguities, int init_obviousness)
    {
        clues = new List<Clue>();
        foreach (Clue clue in init_clues)
            clues.Add(new Clue(clue));

        data = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_data.Keys)
        {
            data.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_data[category].Keys)
                data[category].Add(element, new Element(init_data[category][element]));
        }

        unknowns = init_unknowns;
        ambiguities = init_ambiguities;
        obviousness = init_obviousness;
    }

    public Narrative(Narrative init_narrative)
    {
        clues = new List<Clue>();
        foreach (Clue clue in init_narrative.clues)
            clues.Add(new Clue(clue));

        data = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_narrative.data.Keys)
        {
            data.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_narrative.data[category].Keys)
                data[category].Add(element, new Element(init_narrative.data[category][element]));
        }

        unknowns = init_narrative.unknowns;
        ambiguities = init_narrative.ambiguities;
        obviousness = init_narrative.obviousness;
    }

    public bool IsEqual(Narrative narrative)
    {
        foreach (Clue clue in clues)
            if (!narrative.ContainsClue(clue))
                return false;

        foreach (Clue clue in narrative.clues)
            if (!this.ContainsClue(clue))
                return false;

        return true;
    }

    public bool ContainsClue(Clue clue)
    {
        return clues.FindAll(x => clue.clue.Equals(x.clue)).Count > 0;
    }

    public bool ContainsTemplate(Clue clue)
    {
        return clues.FindAll(x => clue.template.Equals(x.template)).Count > 0;
    }
}


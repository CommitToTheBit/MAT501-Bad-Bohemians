/* Character: Class for containing elements and clues related to one specific scandal */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class Character
{
    // Element handling
    public string identity;
    public bool redHerring;

    public Dictionary<string, Element> elements;

    // Complete list of all clues *this character* can deliver 
    public Dictionary<string,List<Clue>> clues;

    // Constructor
    public Character()
    {
        identity = "";
        redHerring = false;

        elements = new Dictionary<string, Element>();

        clues = new Dictionary<string, List<Clue>>();
    }

    // Copy constructor
    public Character(Character init_character)
    {
        identity = init_character.identity;
        redHerring = false;

        elements = new Dictionary<string, Element>();
        foreach (string category in init_character.elements.Keys)
            elements.Add(category, init_character.elements[category]);

        clues = new Dictionary<string, List<Clue>>();
        foreach (string template in init_character.clues.Keys)
        {
            clues.Add(template, new List<Clue>());
            foreach (Clue clue in init_character.clues[template])
                clues[template].Add(new Clue(clue));
        }

    }

    // Get clues available (without reusing any templates)
    public List<Clue> GetCluesAvailable(List<Clue> init_clues, Dictionary<string, Dictionary<string, Element>> init_firstOrderFinalData)
    {
        // 'HARD' STYLE RULE #1: NO REPEATED TEMPLATES
        List<Clue> cluesAvailable = new List<Clue>();
        foreach (string template in clues.Keys)
            if (init_clues.FindIndex(x => x.template.Equals(template)) < 0)
                foreach (Clue clue in clues[template])
                    cluesAvailable.Add(new Clue(clue));

        // 'HARD' STYLE RULE #2: FEW REPEATED ELEMENTS
        // If an element appears exactly n times, then:
        // - If any 2n clue characters =>& the element, disallow any clues that =>& the same element?
        // - If any n clue characters => the element alongside something, disallow any clues that =>& the same alongside anything else...?
        // FIXME: Could probably make this stronger/harsher, but this should allow players to pick out character threads more easily...
        List<Dictionary<string, string>> firstOrderAssociations = new List<Dictionary<string, string>>();
        foreach (Clue clue in init_clues)
        {
            foreach (string element in clue.clueElements[clue.template].Keys)
            {
                if (element.Equals("UNDEFINED") || element.Equals("") || clue.clueElements[clue.template][element].ambiguous)
                    continue;

                Dictionary<string, string> firstOrderAssociation = new Dictionary<string, string>();
                foreach (List<string> relation in clue.clueElements[clue.template][element].relations)
                {
                    if (!init_firstOrderFinalData.ContainsKey(relation[0]) || !("=>&").Equals(relation[1])) 
                        continue;

                    if (relation[2].Equals("UNDEFINED") || relation[2].Equals(""))
                        continue;

                    firstOrderAssociation.Add(relation[0], relation[2]);
                }
                if (firstOrderAssociation.Count > 0)
                    firstOrderAssociations.Add(firstOrderAssociation);
            }
        }

        Dictionary<string, Dictionary<string, string>> firstOrderProtocols = new Dictionary<string, Dictionary<string, string>>();
        foreach (string category in init_firstOrderFinalData.Keys)
        {
            firstOrderProtocols.Add(category, new Dictionary<string, string>());
            foreach (string element in init_firstOrderFinalData[category].Keys)
            {
                if (element.Equals("UNDEFINED") || element.Equals(""))
                    continue;

                List<Dictionary<string, string>> knowns = firstOrderAssociations.FindAll(x => x.ContainsKey(category) && x[category].Equals(element));
                List<Dictionary<string, string>> links = knowns.FindAll(x => x.Count > 1);

                if (knowns.Count < 2 * init_firstOrderFinalData[category][element].minimum)
                {
                    if (links.Count < 1 * init_firstOrderFinalData[category][element].minimum)
                        firstOrderProtocols[category].Add(element, "Links");
                    else
                        firstOrderProtocols[category].Add(element, "Negations");
                }
                else
                {
                    firstOrderProtocols[category].Add(element, "None");
                }
            }
        }

        for (int i = cluesAvailable.Count-1; i >= 0; i--)
        {
            Clue clue = cluesAvailable[i];

            bool removed = false;
            foreach (string element in clue.clueElements[clue.template].Keys)
            {
                if (element.Equals("UNDEFINED") || element.Equals("") || clue.clueElements[clue.template][element].ambiguous)
                    continue;

                Dictionary<string, string> firstOrderAssociation = new Dictionary<string, string>();
                foreach (List<string> relation in clue.clueElements[clue.template][element].relations)
                {
                    if (!init_firstOrderFinalData.ContainsKey(relation[0]) || !("=>&").Equals(relation[1]))
                        continue;

                    if (relation[2].Equals("UNDEFINED") || relation[2].Equals(""))
                        continue;

                    firstOrderAssociation.Add(relation[0], relation[2]);
                }

                if (firstOrderAssociation.Count >= 2)
                {
                    foreach (string category in firstOrderAssociation.Keys)
                    {
                        if (!firstOrderProtocols[category][firstOrderAssociation[category]].Equals("Links"))
                        {
                            cluesAvailable.RemoveAt(i);
                            removed = true;
                            break;
                        }
                    }
                }
                else if (firstOrderAssociation.Count == 1)
                {
                    string category = firstOrderAssociation.Keys.ElementAt(0);
                    if (firstOrderProtocols[category][firstOrderAssociation[category]].Equals("None"))
                    {
                        cluesAvailable.RemoveAt(i);
                        removed = true;
                    }
                }
                if (removed)
                    break;
            }
        }

        return cluesAvailable;
    }

    // Integration
    public bool IntegrateIdentity(string init_identity = "#Christian Name# #Family Name#, #Nobility# of #Domain#")
    {
        string[] hashParse = init_identity.Split("#");
        for (int i = 1; i < hashParse.Count(); i += 2)
        {
            if (!elements.ContainsKey(hashParse[i]))
            {
                identity = "UNDEFINED";
                return false;
            }

            hashParse[i] = elements[hashParse[i]].element;
        }

        identity = string.Join("", hashParse);
        redHerring = !elements.ContainsKey("Headline");
        return true;
    }
}

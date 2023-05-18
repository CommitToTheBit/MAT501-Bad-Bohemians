/* Cast: Class for containing data about the answer to our generated puzzles */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Cast
{
    // List of all characters involved; the complete solution, essentially
    public List<Character> characters;

    // Initial/Final data, used by Logic...
    public Dictionary<string, Dictionary<string, Element>> initialData;
    public Dictionary<string, Dictionary<string, Element>> intermediaryData;
    public Dictionary<string, Dictionary<string, Element>> finalData;

    public List<string> firstOrderCategories;
    public Dictionary<string, Dictionary<string, Element>> firstOrderFinalData;

    public int unknowns;
    public int ambiguities;

    public int bohemians;
    public int clues;

    // Constructor
    public Cast()
    {
        characters = new List<Character>();

        initialData = new Dictionary<string, Dictionary<string, Element>>();
        intermediaryData = new Dictionary<string, Dictionary<string, Element>>();
        finalData = new Dictionary<string, Dictionary<string, Element>>();

        firstOrderCategories = new List<string>();
        firstOrderFinalData = new Dictionary<string, Dictionary<string, Element>>();

        unknowns = int.MaxValue;
        ambiguities = int.MaxValue;

        bohemians = 0;
        clues = 0;
    }

    // Copy constructor
    public Cast(Cast init_cast)
    {
        characters = new List<Character>();
        foreach (Character character in init_cast.characters)
            characters.Add(new Character(character));

        initialData = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_cast.initialData.Keys)
        {
            initialData.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_cast.initialData[category].Keys)
                initialData[category].Add(element, new Element(init_cast.initialData[category][element]));
        }

        intermediaryData = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_cast.intermediaryData.Keys)
        {
            intermediaryData.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_cast.intermediaryData[category].Keys)
                intermediaryData[category].Add(element, new Element(init_cast.intermediaryData[category][element]));
        }

        finalData = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_cast.finalData.Keys)
        {
            finalData.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_cast.finalData[category].Keys)
            {
                finalData[category].Add(element, new Element(init_cast.finalData[category][element]));
            }
        }

        firstOrderCategories = new List<string>(init_cast.firstOrderCategories);

        firstOrderFinalData = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_cast.firstOrderFinalData.Keys)
        {
            firstOrderFinalData.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_cast.firstOrderFinalData[category].Keys)
            {
                firstOrderFinalData[category].Add(element, new Element(init_cast.firstOrderFinalData[category][element]));
            }
        }

        unknowns = init_cast.unknowns;
        ambiguities = init_cast.ambiguities;

        bohemians = init_cast.bohemians;
        clues = init_cast.clues;
    }

    // Data handling
    public void InitialiseData(Dictionary<string, Dictionary<string, Element>> fixedData, bool firstOrderPredefined = false, bool firstOrderUnique = false)
    {
        initialData = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in fixedData.Keys)
        {
            initialData.Add(category, new Dictionary<string, Element>());
            foreach (string element in fixedData[category].Keys)
                initialData[category].Add(element, new Element(fixedData[category][element]));
        }

        Dictionary<string, Dictionary<string, int>> minMaxes = new Dictionary<string, Dictionary<string, int>>();
        foreach (Character character in characters)
        {
            foreach (string category in character.elements.Keys)
            {
                if (!initialData.ContainsKey(category))
                    initialData.Add(category, new Dictionary<string, Element>());

                if (!initialData[category].ContainsKey(character.elements[category].element))
                    initialData[category].Add(character.elements[category].element, new Element(character.elements[category]));

                if (firstOrderCategories.Contains(category) && firstOrderPredefined && !character.elements[category].element.Equals("UNDEFINED"))
                    initialData[category][character.elements[category].element].IntegrateMinimum(1); // Set to '1' for elements to be 'known in advance'

                if (firstOrderCategories.Contains(category) && firstOrderUnique && !character.elements[category].element.Equals("UNDEFINED"))
                    initialData[category][character.elements[category].element].IntegrateMaximum(1); // Set to '1' for elements to be unique

                if (!minMaxes.ContainsKey(category))
                    minMaxes.Add(category, new Dictionary<string, int>());

                if (!minMaxes[category].ContainsKey(character.elements[category].element))
                    minMaxes[category].Add(character.elements[category].element, 1); 
                else
                    minMaxes[category][character.elements[category].element]++;
            }
        }

        // Scrubbing unused elements...
        foreach (string category in initialData.Keys)
        {
            foreach (string element in initialData[category].Keys)
            {
                for (int i = initialData[category][element].relations.Count - 1; i >= 0; i--)
                {
                    List<string> relation = initialData[category][element].relations[i];
                    List<string> relationSet = new List<string>(relation[2].Split("/"));

                    if (!initialData.ContainsKey(relation[0]))
                    {
                        initialData[category][element].relations.RemoveAt(i);
                        continue;
                    }

                    for (int j = relationSet.Count - 1; j >= 0; j--)
                    {
                        if (!initialData[relation[0]].ContainsKey(relationSet[j]) && !"UNDEFINED".Equals(relationSet[j]))
                        {
                            relationSet.RemoveAt(j);
                        }
                    }

                    if (relationSet.Count > 0)
                        initialData[category][element].relations[i][2] = string.Join("/", relationSet);
                    else
                        initialData[category][element].relations[i][2] = "UNDEFINED";
                }
            }
        }
        initialData = Logic.ExtrapolateRelations(initialData, characters.FindAll(x => !x.identity.Equals("UNDEFINED")).ToList().Count, characters.Count);

        intermediaryData = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in initialData.Keys)
        {
            intermediaryData.Add(category, new Dictionary<string, Element>());
            foreach (string element in initialData[category].Keys)
            {
                intermediaryData[category].Add(element, new Element(initialData[category][element]));
                foreach (Character character in characters)
                {
                    if (character.elements.ContainsKey(category) && character.elements[category].element.Equals(element))
                    {
                        intermediaryData[category][element].IntegrateMinimum(1);
                        break;
                    }
                }
            }
        }
        intermediaryData = Logic.ExtrapolateRelations(intermediaryData, characters.FindAll(x => !x.identity.Equals("UNDEFINED")).ToList().Count, characters.Count);

        finalData = new Dictionary<string, Dictionary<string, Element>>();
        foreach (Character character in characters)
        {
            foreach (string category in character.elements.Keys)
            {
                if (!finalData.ContainsKey(category))
                    finalData.Add(category, new Dictionary<string, Element>());

                if (!finalData[category].ContainsKey(character.elements[category].element))
                    finalData[category].Add(character.elements[category].element, new Element(category, character.elements[category].element, new List<List<string>>(), minMaxes[category][character.elements[category].element], minMaxes[category][character.elements[category].element]));

                foreach (string relatedCategory in character.elements.Keys)
                {
                    int index = finalData[category][character.elements[category].element].relations.FindIndex(x => x[0].Equals(relatedCategory) && x[1].Equals("=>|"));
                    if (index >= 0)
                        finalData[category][character.elements[category].element].relations[index][2] += "/" + character.elements[relatedCategory].element;
                    else
                        finalData[category][character.elements[category].element].relations.Add(new List<string>() { relatedCategory, "=>|", character.elements[relatedCategory].element });
                }
            }
        }
        finalData = Logic.ExtrapolateRelations(new List<Dictionary<string, Dictionary<string, Element>>>() { intermediaryData, finalData }, characters.FindAll(x => !x.identity.Equals("UNDEFINED")).ToList().Count, characters.Count);

        List<string> forwardImplications = new List<string>() { "=>&", "=>|" };
        firstOrderFinalData = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in firstOrderCategories)
        {
            // FIXME: Pass in variable for first order data!
            if (!firstOrderCategories.Contains(category))
                continue;

            firstOrderFinalData.Add(category, new Dictionary<string, Element>());
            foreach (string element in finalData[category].Keys)
            {
                if (element.Equals("UNDEFINED") || element.Equals("") || (finalData[category][element].minimum == 0 && finalData[category][element].maximum == 0))
                    continue;

                if (characters.FindAll(x => !x.redHerring && x.elements[category].element.Equals(element)).Count == 0)
                    continue;

                firstOrderFinalData[category].Add(element, new Element(finalData[category][element]));
                for (int i = firstOrderFinalData[category][element].relations.Count - 1; i >= 0; i--)
                    if (!firstOrderCategories.Contains(firstOrderFinalData[category][element].relations[i][0]) || !forwardImplications.Contains(firstOrderFinalData[category][element].relations[i][1]))
                        firstOrderFinalData[category][element].relations.RemoveAt(i);
            }
        }

        unknowns = EvaluateUnknowns(new List<Clue>());
        ambiguities = EvaluateAmbiguities(intermediaryData);

        bohemians = characters.FindAll(x => !x.identity.Equals("UNDEFINED")).Count;
    }

    // Evaluation
    public int EvaluateUnknowns(List<Clue> clues)
    {
        int unknowns = 0;

        foreach (string category in firstOrderFinalData.Keys)
        {
            foreach (string element in firstOrderFinalData[category].Keys)
            {
                // Include fixed data here!

                bool unknown = true;
                foreach (Clue clue in clues)
                {
                    foreach (string subject in clue.clueElements[clue.template].Keys)
                    {
                        int index = clue.clueElements[clue.template][subject].relations.FindIndex(x => category.Equals(x[0]) && "MIN:1".Equals(x[1]) && x[2].Split("/").Contains(element));
                        if (index >= 0)
                        {
                            unknown = false;
                            break;
                        }
                    }
                    if (!unknown)
                        break;
                }
                if (unknown)
                    unknowns++;
            }
        }

        return unknowns;
    }

    public int EvaluateAmbiguities(Dictionary<string, Dictionary<string, Element>> partialData)
    {
        int ambiguity = 0; // FIXME: Should make this multiplicative...?

        List<string> forwardImplications = new List<string>() { "=>&", "=>|" };
        foreach (string category in firstOrderFinalData.Keys)
        {
            foreach (string element in firstOrderFinalData[category].Keys)
            {
                // Are the implications of this element ambiguous in our partial solution?
                foreach (List<string> finalRelation in firstOrderFinalData[category][element].relations)
                {
                    if (!forwardImplications.Contains(finalRelation[1]))
                        continue;

                    int index = partialData[category][element].relations.FindIndex(x => finalRelation[0].Equals(x[0]) && forwardImplications.Contains(x[1]));
                    List<string> partialRelation = partialData[category][element].relations[index];

                    List<string> finalRelationSet = new List<string>(finalRelation[2].Split("/"));
                    List<string> partialRelationSet = new List<string>(partialRelation[2].Split("/"));
                    ambiguity += partialRelationSet.Count - finalRelationSet.Count; // NB: Assumption of no contradiction!
                }
            }
        }

        return ambiguity;
    }

    public int EvaluateObviousness(List<Clue> clues)
    {
        int obviousness = 0;
        for (int i = 0; i < clues.Count; i++)
            obviousness += clues[i].obviousness;

        return obviousness;
    }

    // Printing
    public void PrintCast(bool includeIdentity = true, bool includeData = true)
    {
        for (int i = 0; i < characters.Count; i++)
        {
            string headline = (includeIdentity && characters[i].elements.ContainsKey("Headline")) ? characters[i].elements["Headline"].element : "CHARACTER " + (i + 1) + "/" + characters.Count;
            Console.WriteLine(headline + ": " + characters[i].identity);

            if (includeData)
            {
                Console.WriteLine("This character has elements...");
                foreach (string category in characters[i].elements.Keys)
                    Console.WriteLine(" * " + category + ": " + characters[i].elements[category].element);

                Console.WriteLine();
            }
        }
        if (!includeData)
        {
            Console.WriteLine();
        }
    }

    public void PrintData(Dictionary<string, Dictionary<string, Element>> data)
    {
        foreach (string category in data.Keys)
        {
            Console.WriteLine("CATEGORY: " + category);
            Console.WriteLine();

            foreach (string element in data[category].Keys)
            {
                data[category][element].PrintElement(true, true);
            }

            Console.WriteLine();
        }
    }

    public void PrintClues(bool includeIdentity = false, bool includeText = false, bool includeData = false)
    {
        List<int> counts = new List<int>();
        for (int i = 0; i < characters.Count; i++)
        {
            int count = 0;
            foreach (string template in characters[i].clues.Keys)
                count += characters[i].clues[template].Count;
            counts.Add(count);

            string identity = (includeIdentity) ? ": " + characters[i].identity : "";
            Console.WriteLine("CHARACTER " + (i + 1) + "/" + characters.Count + identity + " has " + count + " possible clues.");
            Console.WriteLine(" * These are spread across " + characters[i].clues.Count + " templates.");

            if (includeText)
                foreach (string template in characters[i].clues.Keys)
                    foreach (Clue clue in characters[i].clues[template])
                        Console.Write(" * ("+clue.obviousness+") "+clue.clue + "\n");

            if (includeData)
                foreach (string template in characters[i].clues.Keys)
                    foreach (Clue clue in characters[i].clues[template])
                        PrintData(clue.clueElements);

            Console.WriteLine();
        }

        long solutionSpace = 1;
        counts = counts.FindAll(x => x > 0);
        Console.Write("THIS GIVES A TOTAL OF ");
        if (counts.Count > 1)
        {
            for (int i = 0; i < counts.Count; i++)
            {
                solutionSpace *= counts[i];
                Console.Write(counts[i] + ((i < counts.Count - 1) ? "×" : "="));
            }
        }
        else if (counts.Count > 0)
        {
            solutionSpace *= counts[0];
        }
        Console.WriteLine(solutionSpace + " NARRATIVES...");
        Console.WriteLine();
    }
}


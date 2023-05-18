/* Logic: Robust rules-based system for understanding logical implications */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Logic
{
    static Logic()
    {

    }

    public static Dictionary<string, Dictionary<string, Element>> ExtrapolateRelations(Dictionary<string, Dictionary<string, Element>> init_elements, int init_min = 0, int init_max = int.MaxValue, bool DEBUG = false)
    {
        return ExtrapolateRelations(new List<Dictionary<string, Dictionary<string, Element>>>() { init_elements }, init_min, init_max, DEBUG);
    }

    public static Dictionary<string, Dictionary<string, Element>> ExtrapolateRelations(List<Dictionary<string, Dictionary<string, Element>>> init_elements, int init_min = 0, int init_max = int.MaxValue, bool DEBUG = false)
    {
        // STEP 0: Add any categories/elements alluded to in relations to our dictionary
        Dictionary<string, Dictionary<string, Element>> elements = PreProcessing(init_elements);

        // STEP 1: Make deductions until no longer possible
        bool extrapolating;
        bool contradiction;

        // DEBUG:
        int count = 0;

        do
        {
             if (DEBUG)
                Console.Write("PARSE " + (++count) + " : ");

            (contradiction, extrapolating, elements) = SimplifyForwardImplications(elements, false, false, DEBUG);
            if (contradiction)
            {
                if (DEBUG)
                    Console.WriteLine("Contradiction found!\n");

                return new Dictionary<string, Dictionary<string, Element>>(); // The empty dictionary: shorthand for contradiction!
            }
            if (extrapolating)
            {
                elements = SpecialCaseForAmbiguityWithUniqueness(elements);

                if (DEBUG)
                    Console.WriteLine("Simplifies forward...");

                continue;
            }

            (extrapolating, elements) = DeduceNegations(elements, extrapolating);
            if (extrapolating)
            {
                if (DEBUG)
                    Console.WriteLine("Deduces negations...");

                continue;
            }

            (extrapolating, elements) = DeduceAmbiguity(elements, extrapolating);
            if (extrapolating)
            {
                if (DEBUG)
                    Console.WriteLine("Deduces equivalences...");

                continue;
            }

            (extrapolating, elements) = DeduceBounds(elements, extrapolating, init_min, init_max);
            if (extrapolating)
            {
                if (DEBUG)
                    Console.WriteLine("Deduces tighter bounds...");

                continue;
            }

            (extrapolating, elements) = DeduceByHallsTheorem(elements, extrapolating);
            if (extrapolating)
            {
                if (DEBUG)
                    Console.WriteLine("Deduces by Hall's theorem...");

                continue;
            }

            if (DEBUG)
            Console.WriteLine("Finishes extrapolating...\n");
        }
        while (extrapolating);

        return elements;
    }

    private static Dictionary<string, Dictionary<string, Element>> PreProcessing(List<Dictionary<string, Dictionary<string, Element>>> init_elements)
    {
        Dictionary<string, Dictionary<string, Element>> elements = new Dictionary<string, Dictionary<string, Element>>();
        for (int i = 0; i < init_elements.Count; i++)
        {
            foreach (string category in init_elements[i].Keys)
            {
                if (!elements.ContainsKey(category))
                    elements.Add(category, new Dictionary<string, Element>());

                foreach (string element in init_elements[i][category].Keys)
                {
                    if (!elements[category].ContainsKey(element))
                    {
                        elements[category].Add(element, new Element(init_elements[i][category][element]));
                        if (!elements[category][element].ambiguous)
                            elements[category][element].relations.Add(new List<string>() { category, "=>&", element }); // Initialised with dummy relation, useful in special cases...
                    }
                    else
                    {
                        foreach (List<string> relation in init_elements[i][category][element].relations)
                        {
                            if (!elements[category][element].ContainsRelation(relation))
                                elements[category][element].relations.Add(new List<string>(relation));
                        }

                        elements[category][element].IntegrateMinimum(init_elements[i][category][element].minimum);
                        elements[category][element].IntegrateMaximum(init_elements[i][category][element].maximum);
                    }
                }
            }
        }

        // STEP 1: 
        foreach (string category in new List<string>(elements.Keys)) // CHECKME: This is... legit, right?
        {
            foreach (string element in new List<string>(elements[category].Keys))
            {
                foreach (List<string> relation in elements[category][element].relations)
                {
                    // Does this relation use an unmentioned category?
                    if (!elements.ContainsKey(relation[0]))
                    {
                        elements.Add(relation[0], new Dictionary<string, Element>());
                    }

                    // Does our relation set use any unmentioned element?
                    string[] relationSet = relation[2].Split("/");
                    for (int i = 0; i < relationSet.Count(); i++)
                    {
                        // FIXME: In future, references would be split up here (?)

                        if (!elements[relation[0]].ContainsKey(relationSet[i]))
                        {
                            elements[relation[0]].Add(relationSet[i], new Element(relation[0], relationSet[i], new List<List<string>>()));
                            elements[relation[0]][relationSet[i]].relations.Add(new List<string>() { relation[0], "=>&", relationSet[i] });
                        }
                    }
                }
            }
        }

        // STEP 2: Add an 'UNDEFINED' element to every category missing one...
        // FIXME: AND AN EMPTY STRING FOR OUR 'VOID' ELEMENTS...
        foreach (string categoryA in elements.Keys)
        {
            if (!elements[categoryA].ContainsKey("UNDEFINED"))
                elements[categoryA].Add("UNDEFINED", new Element(categoryA, "UNDEFINED", new List<List<string>>()));

            if (!elements[categoryA].ContainsKey(""))
                elements[categoryA].Add("", new Element(categoryA, "", new List<List<string>>(), 0, 0));
        }
        foreach (string categoryA in elements.Keys)
        {
            foreach (string categoryB in elements.Keys)
            {
                if (categoryA.Equals(categoryB))
                {
                    elements[categoryA]["UNDEFINED"].relations.Add(new List<string>() { categoryB, "=>&", "UNDEFINED" });
                    continue;
                }

                List<string> elementsB = new List<string>();
                foreach (string elementB in elements[categoryB].Keys)
                {
                    elementsB.Add(elementB);
                }
                elements[categoryA]["UNDEFINED"].relations.Add(new List<string>() { categoryB, "=>|", string.Join("/", elementsB) });

            }
        }

        // STEP 3: Add MIN/MAX relations
        foreach (string category in elements.Keys)
        {
            foreach (string element in elements[category].Keys)
            {
                int index = -1;

                index = elements[category][element].relations.FindIndex(x => x[1].Split(":")[0].Equals("MIN"));
                while (index >= 0)
                {
                    List<string> minimumRelation = elements[category][element].relations[index];
                    List<string> minimumRelationSet = new List<string>(minimumRelation[2].Split("/"));

                    int minimum = int.Parse(minimumRelation[1].Split(":")[1]);
                    foreach (string minimumRelationElement in minimumRelationSet)
                        elements[minimumRelation[0]][minimumRelationElement].IntegrateMinimum(minimum);

                    elements[category][element].relations.RemoveAt(index);
                    index = elements[category][element].relations.FindIndex(x => x[1].Split(":")[0].Equals("MIN"));
                }

                index = elements[category][element].relations.FindIndex(x => x[1].Split(":")[0].Equals("MAX"));
                while (index >= 0)
                {
                    List<string> maximumRelation = elements[category][element].relations[index];
                    List<string> maximumRelationSet = new List<string>(maximumRelation[2].Split("/"));

                    int maximum = int.Parse(maximumRelation[1].Split(":")[1]);
                    foreach (string maximumRelationElement in maximumRelationSet)
                        elements[maximumRelation[0]][maximumRelationElement].IntegrateMaximum(maximum);

                    elements[category][element].relations.RemoveAt(index);
                    index = elements[category][element].relations.FindIndex(x => x[1].Split(":")[0].Equals("MAX"));
                }
            }
        }

        // STEP 4: Break down any 'high-level' relations
        // FIXME: Include a 'forgiveness' check for writers, so that "a == b", "a => c" results in "a =>& b/c", "a =>{ b/c"?
        foreach (string category in elements.Keys)
        {
            foreach (string element in elements[category].Keys)
            {
                for (int i = elements[category][element].relations.Count - 1; i >= 0; i--)
                {
                    List<string> relation = elements[category][element].relations[i];

                    List<string> orShorthands = new List<string>() { "==", "=>" };
                    List<string> andShorthands = new List<string>() { "=<", "!=", "!!=" };

                    if (orShorthands.Contains(relation[1]))
                    {
                        string suffix = (relation[2].Split("/").Count() == 1) ? "&" : "|"; // "At least one of" reduces to "at least" when there is only one element // FIXME: 'Forgiveness' for multiple "==" on same category
                        List<string> orRelation = new List<string>() { relation[0], relation[1] + suffix, relation[2] };
                        elements[category][element].relations.Add(orRelation);
                    }
                    else if (andShorthands.Contains(relation[1]))
                    {
                        List<string> minRelation = new List<string>() { relation[0], relation[1] + "&", relation[2] };
                        elements[category][element].relations.Add(minRelation);

                    }
                    if (orShorthands.Contains(relation[1]) || andShorthands.Contains(relation[1]))
                    {
                        elements[category][element].relations.RemoveAt(i);
                    }
                }
            }
        }

        // STEP 5: Flip bijections into forward/backward components
        foreach (string category in elements.Keys)
        {
            foreach (string element in elements[category].Keys)
            {
                for (int i = elements[category][element].relations.Count - 1; i >= 0; i--)
                {
                    List<string> relation = elements[category][element].relations[i];

                    Dictionary<string, string> reverses = new Dictionary<string, string>() { { "==&", "==&" }, { "!!=&", "!!=&" }, { ">", "<" }, { "=", "=" }, { "<", ">" } };

                    if (reverses.Keys.Contains(relation[1]))
                    {
                        List<string> reversedRelation = new List<string>() { category, reverses[relation[1]], element };

                        foreach (string relatedElement in relation[2].Split("/"))
                            if (!elements[relation[0]][relatedElement].ContainsRelation(reversedRelation))
                                elements[relation[0]][relatedElement].relations.Add(reversedRelation);
                    }
                }
            }
        }

        return elements;
    }

    private static (bool contradiction, bool extrapolation, Dictionary<string, Dictionary<string, Element>> elements) SimplifyForwardImplications(Dictionary<string, Dictionary<string, Element>> init_elements, bool init_extrapolation, bool init_contradiction, bool DEBUG)
    {

        // STEP 0: Copy init_elements, without any elementRelations
        bool contradiction = init_contradiction;
        bool extrapolation = init_extrapolation;
        Dictionary<string, Dictionary<string, Element>> elements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_elements.Keys)
        {
            elements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_elements[category].Keys)
            {
                elements[category].Add(element, new Element(category, element, new List<List<string>>(), init_elements[category][element].minimum, init_elements[category][element].maximum, init_elements[category][element].ambiguous));
            }
        }

        // STEP 1: Create a forward implication from every element onto every category
        Dictionary<string, List<string>> categories = new Dictionary<string, List<string>>();
        foreach (string category in elements.Keys)
            categories.Add(category, elements[category].Keys.ToList<string>());

        Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> relationSets = new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>>();
        foreach (string category in elements.Keys)
        {
            relationSets.Add(category, new Dictionary<string, Dictionary<string, List<string>>>());
            foreach (string element in elements[category].Keys)
            {
                relationSets[category].Add(element, new Dictionary<string, List<string>>());
            }

            foreach (string relationCategory in categories.Keys)
            {
                foreach (string element in elements[category].Keys)
                {
                    relationSets[category][element].Add(relationCategory, new List<string>(categories[relationCategory]));

                    // NB: Ambiguity clause; no unambiguous element may consider an ambiguous element...
                    // CHECKME: Ambiguous on ambiguous?
                    if (!elements[category][element].ambiguous)
                    {
                        for (int i = relationSets[category][element][relationCategory].Count - 1; i >= 0; i--)
                        {
                            if (elements[relationCategory][relationSets[category][element][relationCategory][i]].ambiguous)
                            {
                                relationSets[category][element][relationCategory].RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }

        // STEP 2: Use forward/backward implications of any logical relations to reduce relationSet sizes
        foreach (string category in elements.Keys)
        {
            foreach (string element in elements[category].Keys)
            {
                foreach (List<string> init_relation in init_elements[category][element].relations)
                {
                    // DEBUG
                    /*if (category.Equals("\"#0:Christian Name# is #1:Family Name#\"") && element.Equals("1"))
                        Console.WriteLine(init_relation[0] + " " + init_relation[1] + " " + init_relation[2]);*/

                    if (init_relation[1] == "==&" || init_relation[1] == "==|" || init_relation[1] == "=>&" || init_relation[1] == "=>|")
                    {
                        List<string> init_relationSet = new List<string>(init_relation[2].Split("/"));

                        for (int i = relationSets[category][element][init_relation[0]].Count - 1; i >= 0; i--)
                            if (!init_relationSet.Contains(relationSets[category][element][init_relation[0]][i]))
                                relationSets[category][element][init_relation[0]].RemoveAt(i);

                        // CHECKME: This would not *ever* be an extrapolation, would it?
                    }
                    else if (init_relation[1] == "==&" || init_relation[1] == "=<&") // Note: there isn't really a symmetry forwards and backwards...
                    {
                        List<string> init_relationSet = new List<string>(init_relation[2].Split("/"));

                        foreach (string init_relationElement in init_relationSet)
                            for (int i = relationSets[init_relation[0]][init_relationElement][category].Count - 1; i >= 0; i--)
                                if (!element.Equals(relationSets[init_relation[0]][init_relationElement][category][i]))
                                    relationSets[init_relation[0]][init_relationElement][category].RemoveAt(i);

                        extrapolation |= true;
                    }
                }
            }
        }
        foreach (string category in elements.Keys) // NB: Order of operations matters to avoid never ending while loop of extrapolation!
        {
            foreach (string element in elements[category].Keys)
            {
                foreach (List<string> init_relation in init_elements[category][element].relations)
                {
                    if (init_relation[1] == "!=&" || init_relation[1] == "!!=&")
                    {
                        List<string> init_relationSet = new List<string>(init_relation[2].Split("/"));

                        for (int i = relationSets[category][element][init_relation[0]].Count - 1; i >= 0; i--)
                            if (init_relationSet.Contains(relationSets[category][element][init_relation[0]][i]))
                            {

                                // DEBUG:
                                if (init_relation[1] == "!!=&")
                                    Console.WriteLine(element + ": Removed " + relationSets[category][element][init_relation[0]][i]);

                                relationSets[category][element][init_relation[0]].RemoveAt(i);

                                // DEBUG:
                                //Console.WriteLine(element+": "+init_relation[1]+" "+init_relation[2]);


                                extrapolation |= true;
                            }

                        foreach (string init_relationElement in init_relationSet)
                            for (int i = relationSets[init_relation[0]][init_relationElement][category].Count - 1; i >= 0; i--)
                                if (element.Equals(relationSets[init_relation[0]][init_relationElement][category][i]))
                                {
                                    // DEBUG:
                                    if (init_relation[1] == "!!=&")
                                        Console.WriteLine(init_relationElement + ": Removed* " + relationSets[init_relation[0]][init_relationElement][category][i]);

                                    relationSets[init_relation[0]][init_relationElement][category].RemoveAt(i);

                                    // DEBUG:
                                    //Console.WriteLine(element + ": " + init_relation[1] + " " + init_relation[2]);
                                    //foreach (string s in relationSets[init_relation[0]][init_relationElement][category])
                                    //    Console.WriteLine(s);

                                    extrapolation |= true;
                                }
                    }
                }
            }
        }

        // STEP 3: Recombine relationSets into singular forward implications
        foreach (string category in elements.Keys)
        {
            foreach (string element in elements[category].Keys)
            {
                foreach (string relationCategory in relationSets[category][element].Keys)
                {
                    string suffix = (relationSets[category][element][relationCategory].Count <= 1) ? "&" : "|";
                    elements[category][element].relations.Add(new List<string>() { relationCategory, "=>" + suffix, string.Join("/", relationSets[category][element][relationCategory]) }); // 'Redundant' relation that unlocks a special case of Type 1 Deduction... 
                }
            }
        }

        // STEP 4: Add all unused relations
        List<string> redundancies = new List<string>() { "==&", "=>&", "=>|", "=<&", "!=&" }; // "==|", "=<|" will still contain valuable information...
        foreach (string category in elements.Keys)
            foreach (string element in elements[category].Keys)
                foreach (List<string> init_relation in init_elements[category][element].relations)
                    if (!redundancies.Contains(init_relation[1]))
                        elements[category][element].relations.Add(new List<string>(init_relation));

        // STEP 5: Check if we have any empty forward implications
        // NB: The thinking here is, subbing UNDEFINEDs into otherwise empty sets is all that led to 'UNDEFINED looping'...
        // ...This might be a useful way of distinguishing between hard contradiction and just not meeting the demands of a solution...
        foreach (string category in new List<string>(elements.Keys))
        {
            foreach (string element in new List<string>(elements[category].Keys))
            {
                foreach (string relationCategory in relationSets[category][element].Keys)
                {
                    if (relationSets[category][element][relationCategory].Count == 0)
                    {
                        if (elements[category][element].minimum > 0)
                        {
                            // DEBUG:
                            if (DEBUG)
                                Console.WriteLine("CONTRADICTION: " + category + ", " + element + " has no elements in " + relationCategory + ". This element is " + ((elements[category][element].ambiguous) ? "" : "not ") + "ambiguous");

                            contradiction = true;
                        }
                        else
                        {
                            foreach (string categoryB in elements.Keys)
                            {
                                elements[category][element].IntegrateMaximum(0);

                                List<string> exhaustiveRelation = new List<string>() { categoryB, "=>&", "" };
                                if (!elements[category][element].ContainsRelation(exhaustiveRelation))
                                {
                                    elements[category][element].relations.Add(exhaustiveRelation);
                                    extrapolation |= true;
                                }
                            }
                        }
                    }
                }
            }
        }

        return (contradiction, extrapolation, elements);
    }

    private static (bool extrapolation, Dictionary<string, Dictionary<string, Element>> elements) SimplifyBackwardImplications(Dictionary<string, Dictionary<string, Element>> init_elements, bool init_extrapolation)
    {
        bool extrapolation = init_extrapolation;
        Dictionary<string, Dictionary<string, Element>> elements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_elements.Keys)
        {
            elements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_elements[category].Keys)
            {
                elements[category].Add(element, new Element(init_elements[category][element]));
            }
        }

        List<string> forwardImplications = new List<string>() { "=>&", "=>|" };
        List<string> backwardImplications = new List<string>() { "==|", "=<|" };
        foreach (string categoryA in elements.Keys)
        {
            foreach (string elementA in elements[categoryA].Keys)
            {
                for (int i = 0; i < elements[categoryA][elementA].relations.Count; i++)
                {
                    List<string> implicationAB = elements[categoryA][elementA].relations[i];

                    if (!backwardImplications.Contains(implicationAB[1]))
                        continue;

                    string categoryB = implicationAB[0];
                    List<string> elementsAB = new List<string>(implicationAB[2].Split("/"));

                    for (int j = elementsAB.Count - 1; j >= 0; j--)
                    {
                        string elementAB = elementsAB[j];

                        int index = -1;
                        for (int k = 0; k < elements[categoryB][elementAB].relations.Count; k++)
                        {
                            if (elements[categoryB][elementAB].relations[k][0].Equals(categoryA) && forwardImplications.Contains(elements[categoryB][elementAB].relations[k][1]))
                            {
                                index = k;
                                break;
                            }
                        }

                        try
                        {
                            List<string> elementsBA = new List<string>(elements[categoryB][elementAB].relations[index][2].Split("/"));
                            if (!elementsBA.Contains(elementA))
                            {
                                elementsAB.RemoveAt(j);
                                extrapolation |= true;
                            }
                        }
                        catch // DEBUG: There should be no case in which index < 0
                        {
                            Console.WriteLine();
                            Console.WriteLine("-----------------------------------------------------------------------------");
                            Console.WriteLine("--- LOGIC ERROR: BACK SIMPLIFICATION RECEIVED POOR FORWARD SIMPLIFICATION ---");
                            Console.WriteLine("-----------------------------------------------------------------------------");
                            Console.WriteLine();
                        }
                    }

                    string prefix = implicationAB[1].Substring(0, implicationAB[1].Length - 1);
                    string suffix = (elementsAB.Count == 1) ? "&" : "|";
                    elements[categoryA][elementA].relations[i][1] = prefix + suffix;
                    elements[categoryA][elementA].relations[i][2] = string.Join("/", elementsAB);
                }
            }
        }


        return (extrapolation, elements);
    }
    
    private static (bool extrapolation, Dictionary<string, Dictionary<string, Element>> elements) DeduceAmbiguity(Dictionary<string, Dictionary<string, Element>> init_elements, bool init_extrapolation)
    {
        // Copy init_elements
        bool extrapolation = init_extrapolation;
        Dictionary<string, Dictionary<string, Element>> elements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_elements.Keys)
        {
            elements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_elements[category].Keys)
            {
                elements[category].Add(element, new Element(init_elements[category][element]));
            }
        }

        foreach (string category in elements.Keys)
        {
            foreach (string element in elements[category].Keys)
            {
                if (!elements[category][element].ambiguous || elements[category][element].maximum > 0)
                    continue;

                int index = elements[category][element].relations.FindIndex(x => category.Equals(x[0]) && "=>&".Equals(x[1]));
                if (index < 0)
                    continue;

                string unambiguousElement = elements[category][element].relations[index][2]; // NB: Despite the name, this could still be ambiguous!
                for (int i = 0; i < elements[category][element].relations.Count; i++)
                {
                    if (i == index)
                        continue;

                    elements[category][unambiguousElement].relations.Add(new List<string>(elements[category][element].relations[i]));
                }

                // FIXME: ALSO NEED TO TRANSFER ALL RELATIONS TO NOW-DELETED ELEMENT
                foreach (string categoryB in elements.Keys)
                {
                    foreach (string elementB in elements[categoryB].Keys)
                    {
                        // NB: Assuming no unambiguous element can point to an ambiguous element...
                        if (!elements[categoryB][elementB].ambiguous)
                            continue;

                        if (category.Equals(categoryB) && element.Equals(elementB))
                            continue;

                        int indexB = elements[categoryB][elementB].relations.FindIndex(0, x => category.Equals(x[0]));
                        while (indexB >= 0)
                        {
                            List<string> relationSet = new List<string>(elements[categoryB][elementB].relations[indexB][2].Split("/"));
                            for (int i = 0; i < relationSet.Count; i++)
                                if (relationSet[i].Equals(element))
                                    relationSet[i] = unambiguousElement;
                            elements[categoryB][elementB].relations[indexB][2] = String.Join("", relationSet);

                            if (indexB == elements[categoryB][elementB].relations.Count - 1)
                                break;

                            indexB = elements[categoryB][elementB].relations.FindIndex(indexB + 1, x => category.Equals(x[0]));
                        }
                    }
                }

                // NB: Turning ambiguity off throws a contradiction, so we leave this on but set our minMax to 0
                // CHECKME: This fix shouldn't throw, should it?
                elements[category][element].minimum = 0;
                elements[category][element].maximum = 0;

                extrapolation |= true;
            }
        }

        return (extrapolation, elements);
    }

    private static (bool extrapolation, Dictionary<string, Dictionary<string, Element>> elements) DeduceNegations(Dictionary<string, Dictionary<string, Element>> init_elements, bool init_extrapolation)
    {
        // Copy init_elements
        bool extrapolation = init_extrapolation;
        Dictionary<string, Dictionary<string, Element>> elements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_elements.Keys)
        {
            elements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_elements[category].Keys)
            {
                elements[category].Add(element, new Element(init_elements[category][element]));
            }
        }

        // Let Element a in Category A =>| Elements c in Category C
        // For each c, if a =>& b in any Category B, and b does not appear in c's =>| relation onto B...
        // ...then a =>{ onto C cannot contain c
        // (Note also the special case a =>& a)
        List<string> forwardImplications = new List<string>() { "=>&", "=>|" };
        foreach (string categoryA in elements.Keys)
        {
            foreach (string elementA in elements[categoryA].Keys)
            {
                if (elements[categoryA][elementA].maximum == 0)// || elements[categoryA][elementA].ambiguous)
                    continue;

                for (int i = elements[categoryA][elementA].relations.Count - 1; i >= 0; i--)
                {
                    List<string> implicationAB = elements[categoryA][elementA].relations[i];

                    if (!forwardImplications.Contains(implicationAB[1]))
                        continue;

                    string categoryB = implicationAB[0];
                    List<string> elementsAB = new List<string>(implicationAB[2].Split("/"));
                    bool ambiguityB = elementsAB.FindIndex(x => elements[categoryB][x].ambiguous) >= 0;

                    for (int j = elements[categoryA][elementA].relations.Count - 1; j >= 0; j--)
                    {
                        List<string> implicationAC = elements[categoryA][elementA].relations[j];

                        if (!forwardImplications.Contains(implicationAC[1])) // DEBUG: Should only ever extrapolate on "=>|" relations... otherwise, we're just checking nothing breaks down...
                            continue;

                        string categoryC = implicationAC[0];
                        List<string> elementsAC = new List<string>(implicationAC[2].Split("/"));

                        if (categoryB.Equals(categoryC))
                            continue;

                        // a implies every b, so if c cannot imply (i.e. link to) b, then a cannot imply c
                        foreach (string elementAC in elementsAC)
                        {
                            // NB: Unambiguous elements cannot see the ambiguous elements they may map to...
                            if (ambiguityB && !elements[categoryC][elementAC].ambiguous)
                                continue;

                            //if (elements[categoryC][elementAC].ambiguous)
                            //    continue;

                            int index = -1;
                            for (int k = 0; k < elements[categoryC][elementAC].relations.Count; k++)
                            {
                                if (elements[categoryC][elementAC].relations[k][0].Equals(categoryB) && forwardImplications.Contains(elements[categoryC][elementAC].relations[k][1]))
                                {
                                    index = k;
                                    break;
                                }
                            }

                            List<string> elementsCB = new List<string>(elements[categoryC][elementAC].relations[index][2].Split("/"));
                            if (elementsCB.Intersect(elementsAB).ToList().Count == 0)
                            {
                                elements[categoryA][elementA].relations.Add(new List<string>() { categoryC, "!=&", elementAC });
                                extrapolation |= true;

                                // DEBUG:
                                //Console.WriteLine(categoryA + " " + elementA + ", via " + categoryB + ": " + categoryC + " !=& " + elementAC);
                            }
                        }
                    }
                }
            }
        }

        return (extrapolation, elements);
    }

    private static (bool extrapolation, Dictionary<string, Dictionary<string, Element>> elements) DeduceBounds(Dictionary<string, Dictionary<string, Element>> init_elements, bool init_extrapolation, int init_min, int init_max)
    {
        // Copy init_elements
        bool extrapolation = init_extrapolation;
        Dictionary<string, Dictionary<string, Element>> elements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_elements.Keys)
        {
            elements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_elements[category].Keys)
            {
                elements[category].Add(element, new Element(init_elements[category][element]));
            }
        }

        // STEP 1: Raise lower bounds, based on "how few instances of the element are needed to satisfy all conditions"
        // FIXME: Need to implement, for gender alone...
        foreach (string categoryA in elements.Keys)
        {
            foreach (string elementA in elements[categoryA].Keys)
            {
                if (elements[categoryA][elementA].ambiguous)
                    continue;

                int exhaustiveMin = init_min;
                foreach (string elementB in elements[categoryA].Keys)
                {
                    if (elementB.Equals(elementA) || elements[categoryA][elementB].ambiguous)
                        continue;

                    if (exhaustiveMin > 0)
                        exhaustiveMin -= Math.Min(elements[categoryA][elementB].maximum, exhaustiveMin);
                }

                elements[categoryA][elementA].IntegrateMinimum(exhaustiveMin);
            }
        }

        // STEP 2: Lower upper bounds, based on whether "init_max-all other category mins" is lower than current max...
        foreach (string categoryA in elements.Keys)
        {
            foreach (string elementA in elements[categoryA].Keys)
            {
                if (elements[categoryA][elementA].ambiguous)
                    continue;

                int exhaustiveMax = init_max;
                foreach (string elementB in elements[categoryA].Keys)
                {
                    if (elementB.Equals(elementA) || elements[categoryA][elementB].ambiguous)
                        continue;

                    exhaustiveMax -= elements[categoryA][elementB].minimum;
                }

                elements[categoryA][elementA].IntegrateMaximum(exhaustiveMax);
            }
        }

        // STEP 3: If we know... exactly (?) how many times an element appears... deduce any... bijections (?)... 
        // NB: This is the only point in the entire class at which we make deductions based on MIN/MAXes...
        // ...and hence the only place in the function at which we need set extrapolation |= true;
        // FIXME: Obvious shakiness on how we're generalising!
        // CHECKME: Is there actually any more needed than just setting zeroes as undefined?
        foreach (string categoryA in elements.Keys)
        {
            foreach (string elementA in elements[categoryA].Keys)
            {
                if (elements[categoryA][elementA].ambiguous)
                    continue;

                if (elements[categoryA][elementA].maximum == 0)
                {
                    foreach (string categoryB in elements.Keys)
                    {
                        List<string> exhaustiveRelation = new List<string>() { categoryB, "=>&", "" };
                        if (!elements[categoryA][elementA].ContainsRelation(exhaustiveRelation))
                        {
                            elements[categoryA][elementA].relations.Add(exhaustiveRelation);
                            extrapolation |= true;
                        }
                    }
                }
            }
        }

        return (extrapolation, elements);
    }

    // FIXME: This is only a very basic implementation - but maybe also the most 'human' way of doing things?
    // Certainly needs a bit more to account for gender... though this is doing that, isn't it?
    private static (bool extrapolation, Dictionary<string, Dictionary<string, Element>> elements) DeduceByHallsTheorem(Dictionary<string, Dictionary<string, Element>> init_elements, bool init_extrapolation)
    {
        // Copy init_elements
        bool extrapolation = init_extrapolation;
        Dictionary<string, Dictionary<string, Element>> elements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_elements.Keys)
        {
            elements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_elements[category].Keys)
            {
                elements[category].Add(element, new Element(init_elements[category][element]));
            }
        }

        List<string> forwardImplications = new List<string>() { "=>&", "=>|" };
        foreach (string categoryA in elements.Keys)
        {
            List<List<string>> powerSetA = new List<List<string>>() { new List<string>() };
            foreach (string elementA in elements[categoryA].Keys)
            {
                // NB: Handles the special case in which our deduction breaks down
                // If an element (whether defined or UNDEFINED) appears 0 times, matching it uniquely with 0 elements will lead to contradiction!
                // This is a special case/exception that makes most sense intuitively, but it should be convincing; besides, the proof is in the pudding!
                if (elements[categoryA][elementA].maximum == 0)
                    continue;

                foreach (List<string> init_setA in new List<List<string>>(powerSetA))
                {
                    List<string> setA = new List<string>(init_setA);
                    setA.Add(elementA);
                    powerSetA.Add(setA);
                }
            }
            powerSetA = powerSetA.FindAll(x => x.Count > 0 && x.Count <= 5); // An upper bound on sizes will make this far more efficient...

            foreach (List<string> setA in powerSetA)
            {
                List<string> elementsAA = new List<string>();
                foreach (string elementA in setA)
                {
                    int index = elements[categoryA][elementA].relations.FindIndex(x => categoryA.Equals(x[0]) && forwardImplications.Contains(x[1]));
                    List<string> relationSetAA = new List<string>(elements[categoryA][elementA].relations[index][2].Split("/"));
                    foreach (string relationElementAA in relationSetAA)
                        if (!elementsAA.Contains(relationElementAA))
                            elementsAA.Add(relationElementAA);
                }

                int matchedMax = 0;
                foreach (string elementAA in elementsAA)
                    matchedMax += Math.Min(elements[categoryA][elementAA].maximum, int.MaxValue - matchedMax);

                foreach (string categoryB in elements.Keys)
                {
                    if (categoryA.Equals(categoryB))
                        continue;

                    List<string> elementsAB = new List<string>();
                    foreach (string elementA in setA)
                    {
                        int indexAB = elements[categoryA][elementA].relations.FindIndex(x => categoryB.Equals(x[0]) && forwardImplications.Contains(x[1]));
                        List<string> relationSetAB = new List<string>(elements[categoryA][elementA].relations[indexAB][2].Split("/"));
                        foreach (string relationElementAB in relationSetAB)
                            if (!elementsAB.Contains(relationElementAB))
                                elementsAB.Add(relationElementAB);
                    }

                    int matchedMin = 0;
                    List<string> matchedElementsAB = new List<string>();
                    foreach (string elementAB in elementsAB)
                    {
                        List<string> implicationBA = elements[categoryB][elementAB].relations[elements[categoryB][elementAB].relations.FindIndex(x => categoryA.Equals(x[0]) && forwardImplications.Contains(x[1]))];
                        List<string> elementsBA = new List<string>(implicationBA[2].Split("/"));

                        foreach (string elementAA in elementsAA)
                            elementsBA.Remove(elementAA);

                        if (elementsBA.Count > 0 || elements[categoryB][elementAB].ambiguous)
                            continue;

                        matchedElementsAB.Add(elementAB);
                        matchedMin += elements[categoryB][elementAB].minimum;
                    }

                    if (elementsAA.Count == 0 && elements[categoryA][elementsAA[0]].minimum < matchedMin)
                        extrapolation |= elements[categoryA][elementsAA[0]].IntegrateMinimum(matchedMin);

                    if (matchedMin < matchedMax)
                        continue;

                    // We now know every element exclusively matched with element AA appears exactly that many times...
                    if (elementsAA.Count == 0)
                        extrapolation |= elements[categoryA][elementsAA[0]].IntegrateMinimum(elements[categoryA][elementsAA[0]].maximum);

                    foreach (string elementAB in elementsAB)
                    {
                        if (elements[categoryB][elementAB].ambiguous)
                            continue;

                        if (matchedElementsAB.Contains(elementAB))
                        {
                            extrapolation |= elements[categoryB][elementAB].IntegrateMaximum(elements[categoryB][elementAB].minimum);
                        }
                        else
                        {
                            int implicationBAIndex = elements[categoryB][elementAB].relations.FindIndex(x => categoryA.Equals(x[0]) && forwardImplications.Contains(x[1]));
                            List<string> implicationBAElements = new List<string>(elements[categoryB][elementAB].relations[implicationBAIndex][2].Split("/"));
                            implicationBAElements.RemoveAll(x => elementsAA.Contains(x));// && !elements[categoryA][x].ambiguous); 
                            elements[categoryB][elementAB].relations[implicationBAIndex][2] = string.Join("/", implicationBAElements);
                            extrapolation |= true;

                            //Console.WriteLine(elements[categoryB][elementAB].relations[implicationBAIndex][2]);
                        }
                    }
                }
            }
        }

        return (extrapolation, elements);
    }

    // FIXME: This works, and is necessary for costing, but only a consequence of narrowly implemented ambiguity...
    // NB: Has no extrapolation value... due to low-effort implementation!
    private static Dictionary<string, Dictionary<string, Element>> SpecialCaseForAmbiguityWithUniqueness(Dictionary<string, Dictionary<string, Element>> init_elements)
    {
        // Copy init_elements
        Dictionary<string, Dictionary<string, Element>> elements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_elements.Keys)
        {
            elements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_elements[category].Keys)
            {
                elements[category].Add(element, new Element(init_elements[category][element]));
            }
        }

        // If an ambiguous element points with =>& to a unique element, we can pass into this all of the ambiguous elements relations (by bijectivity...)
        List<string> forwardImplications = new List<string>() { "=>&", "=>|" };
        foreach (string category in elements.Keys)
        {
            foreach (string element in elements[category].Keys)
            {
                if (!elements[category][element].ambiguous)
                    continue;

                // For each banned element...
                // For each unique "=>&"...
                // Remove the implied element from own possibilities...
                foreach (List<string> relationA in elements[category][element].relations)
                {
                    if (!"!!=&".Equals(relationA[1]))
                        continue;

                    List<string> relationSetA = new List<string>(relationA[2].Split("/"));
                    foreach (string relationElement in relationSetA)
                    {
                        List<List<string>> uniqueImplications = elements[relationA[0]][relationElement].relations.FindAll(x => "=>&".Equals(x[1]) && elements[x[0]][x[2]].maximum == 1);
                        foreach (List<string> relationB in uniqueImplications)
                        {
                            int index = elements[category][element].relations.FindIndex(x => relationB[0].Equals(x[0]) && forwardImplications.Contains(x[1]));
                            List<String> relationSetB = new List<string>(elements[category][element].relations[index][2].Split("/"));
                            relationSetB.RemoveAll(x => x.Equals(relationB[2]));
                            elements[category][element].relations[index][2] = String.Join("/", relationSetB);
                        }
                    }
                }

                foreach (List<string> relationA in elements[category][element].relations)
                {
                    if (!"=>&".Equals(relationA[1]))
                        continue;

                    // FIXME: Why so specific here?
                    if (elements[relationA[0]][relationA[2]].maximum != 1)// || elements[relationA[0]][relationA[2]].ambiguous)
                        continue;

                    // DEBUG:
                    //Console.WriteLine(element + " used! "+relationA[1]+" "+relationA[2]);

                    foreach (List<string> relationB in elements[category][element].relations)
                    {
                        if ("!!=&".Equals(relationB[1]))
                            continue; // NB: Do not propagate ambiguity-specific relations...

                        List<string> relationSet = new List<string>(relationB[2].Split("/"));

                        int indexB = relationSet.FindIndex(x => elements[relationB[0]][x].ambiguous);
                        while (indexB >= 0) // NB: Requires sensible clue writing to avoid infinite while loop
                        {
                            int indexBB = elements[relationB[0]][relationSet[indexB]].relations.FindIndex(x => relationB[0].Equals(x[0]) && forwardImplications.Contains(x[1]));
                            List<string> relationSetBB = new List<string>(elements[relationB[0]][relationSet[indexB]].relations[indexBB][2].Split("/"));
                            foreach (string relationElementBB in relationSetBB)
                                if (!relationSet.Contains(relationElementBB))
                                    relationSet.Add(relationElementBB);

                            relationSet.RemoveAt(indexB);
                            indexB = relationSet.FindIndex(x => elements[relationB[0]][x].ambiguous);
                        }

                        List<string> newRelation = new List<string>(relationB);
                        newRelation[2] = string.Join("/", relationSet);

                        if (!elements[relationA[0]][relationA[2]].ContainsRelation(newRelation))
                            elements[relationA[0]][relationA[2]].relations.Add(new List<string>(newRelation));
                    }
                }
            }
        }

        return elements;
    }
}
/* Genetic Algorithm Processor: Creates clues out of clue templates as part of the genetic algorithm */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class GeneticAlgorithmProcessor
{
    private List<Template> templates;
    public Dictionary<string, Dictionary<string, List<Clue>>> clues;

    private Random rng;

    // Constructor
    public GeneticAlgorithmProcessor(Cast init_cast, int init_seed)
    {
        rng = new Random(init_seed);

        // DEBUG:
        long init_ticks = DateTime.UtcNow.Ticks;

        templates = InitialiseTemplates(Environment.CurrentDirectory.Split("bin")[0]+"Genetic Algorithm/template_data.json");
        clues = InitialiseClues(init_cast, (long)(60 * Math.Pow(10, 7)));
    }

    private List<Template> InitialiseTemplates(string init_path)
    {
        List<Template> templates = new List<Template>();

        // STEP 1: Read all jsonData elements into a single list
        Dictionary<string, Dictionary<string, List<List<string>>>> jsonData = new Dictionary<string, Dictionary<string, List<List<string>>>>();
        using (StreamReader streamReader = File.OpenText(@init_path)) // FIXME: Get relative path right!
        {
            JsonSerializer jsonSerializer = new JsonSerializer();
            try
            {
                jsonData = (Dictionary<string, Dictionary<string, List<List<string>>>>)jsonSerializer.Deserialize(streamReader, typeof(Dictionary<string, Dictionary<string, List<List<string>>>>)); // If exception is being thrown here - check the JSON is valid!
            }
            catch
            {
                Console.WriteLine("ERROR: Invalid Character Descriptor JSON...");
            }
        }
        if (jsonData is null)
        {
            jsonData = new Dictionary<string, Dictionary<string, List<List<string>>>>();
        }

        List<string> forwardImplicationSigns = new List<string>() { "=>", "=>&", "=>|" }; // FIXME: Move equivalence check into processor?
        foreach (string template in jsonData.Keys)
        {
            // DEBUG: Used to test specific templates for logical consistency!
            //if (jsonData.Keys.ToList().IndexOf(template) != 29)
            //    continue;

            string processedTemplate = "#Culprit::Headline# " + template;

            Dictionary<string, Dictionary<string, Element>> templateElements = new Dictionary<string, Dictionary<string, Element>>();
            templateElements.Add(processedTemplate, new Dictionary<string, Element>());

            // First, add a FIRST PERSON to deliver every line...
            // NB: This is tailored to the needs of our specific whodunnit; it's entirely based on a unique set of testimonies...
            //templateElements[template].Add("Target", new Element(processedTemplate, "Target", new List<List<string>>(), 1, 1, false));

            // Then, read in all template relations
            foreach (string element in jsonData[template].Keys)
            {
                for (int i = 0; i < jsonData[template][element].Count; i++)
                {
                    if (jsonData[template][element][i][0] == "TEMPLATE ID")
                    {
                        jsonData[template][element][i][0] = processedTemplate;
                        if (jsonData[template][element][i][1] == "!=")
                            jsonData[template][element][i][1] = "!!="; // A 'fixed' !=
                    }
                }

                if (templateElements[processedTemplate].ContainsKey(element))
                {
                    foreach (List<string> relation in jsonData[processedTemplate][element])
                    {
                        if (!templateElements[processedTemplate][element].ContainsRelation(relation))
                        {
                            templateElements[processedTemplate][element].relations.Add(relation);
                        }
                    }
                }
                else
                {
                    int minimum = (element.Equals("UNDEFINED")) ? 0 : 1;
                    int maximum = (element.Equals("UNDEFINED")) ? int.MaxValue : 1;
                    bool ambiguous = jsonData[template][element].FindIndex(x => processedTemplate.Equals(x[0]) && forwardImplicationSigns.Contains(x[1]) && !element.Equals(x[2]) && !element.Equals("")) >= 0;
                    templateElements[processedTemplate].Add(element, new Element(processedTemplate, element, jsonData[template][element], minimum, maximum, ambiguous));
                }
            }

            // Finally, include all parsed characters (who may not have specific relations past the bare minimum}
            string[] parse = processedTemplate.Split("#");
            for (int j = 1; j < parse.Count(); j += 2)
            {
                string[] insert = parse[j].Split("::");
                if (insert.Count() != 2)
                    continue;

                if (templateElements[processedTemplate].ContainsKey(insert[0]))
                    continue;

                templateElements[processedTemplate].Add(insert[0], new Element(processedTemplate, insert[0], new List<List<string>>(), 1, 1));
            }

            templates.Add(new Template(processedTemplate, templateElements));
        }

        return templates;
    }

    private Dictionary<string, Dictionary<string, List<Clue>>> InitialiseClues(Cast init_cast, long period)
    {
        Dictionary<string, Dictionary<string, List<Clue>>> clues = new Dictionary<string, Dictionary<string, List<Clue>>>();

        List<string> forwardImplicationSigns = new List<string>() { "=>", "=>&", "=>|" };
        List<string> negationSigns = new List<string>() { "!=", "!!=" };

        long templatePeriod = period / Math.Max(templates.Count, 1);

        long init_ticks = DateTime.UtcNow.Ticks;
        for (int t = 0; t < templates.Count; t++)
        {
            Template template = templates[t];

            clues.Add(template.template, new Dictionary<string,List<Clue>>());

            // STEP 1: Find all possible mappings from our unambiguous subjects to our characters
            List<string> subjects = template.templateElements[template.template].Keys.ToList().FindAll(x => !template.templateElements[template.template][x].ambiguous && !"UNDEFINED".Equals(x)); // NB: Assuming no "" mentioned in our templates...
            List<string> ambiguousSubjects = template.templateElements[template.template].Keys.ToList().FindAll(x => template.templateElements[template.template][x].ambiguous && !"UNDEFINED".Equals(x));

            List<int> characters = new List<int>();
            for (int i = 0; i < init_cast.characters.Count; i++)
                characters.Add(i);

            List<Dictionary<string, int>> subjectMappings = new List<Dictionary<string, int>>();
            subjectMappings.Add(new Dictionary<string, int>());

            List<Dictionary<string, Dictionary<string, Element>>> subjectDatas = new List<Dictionary<string, Dictionary<string, Element>>>();
            subjectDatas.Add(new Dictionary<string, Dictionary<string, Element>>());
            subjectDatas[0].Add(template.template, new Dictionary<string, Element>());

            while (subjects.Count > 0)
            {
                string subject = subjects[0];

                List<Dictionary<string, int>> iteratedSubjectMappings = new List<Dictionary<string, int>>();
                List<Dictionary<string, Dictionary<string, Element>>> iteratedSubjectDatas = new List<Dictionary<string, Dictionary<string, Element>>>();
                for (int i = 0; i < subjectMappings.Count; i++)
                {
                    // LOGIC PROCESSING: No two non-ambiguous characters can be the same...
                    List<int> unmappedCharacters = characters.FindAll(x => !subjectMappings[i].ContainsValue(x));
                    foreach (int character in unmappedCharacters)
                    {
                        // STORY-SPECIFIC PROCESSING: Inelegant condition, but actually our best way of tailoring who speaks *without making clear who they are*...
                        if (subject.Equals("Culprit") && init_cast.characters[character].identity.Equals("UNDEFINED"))
                            continue;

                        Dictionary<string, int> iteratedSubjectMapping = new Dictionary<string, int>(subjectMappings[i]);
                        iteratedSubjectMapping.Add(subject, character);

                        bool contradiction = false;
                        foreach (List<string> relation in template.templateElements[template.template][subject].relations)
                        {
                            if (!init_cast.characters[character].elements.ContainsKey(relation[0]))
                                continue;

                            List<string> relationSet = new List<string>(relation[2].Split("/"));

                            if (forwardImplicationSigns.Contains(relation[1]) && !relationSet.Contains(init_cast.characters[character].elements[relation[0]].element) && relationSet.FindAll(x => x.Split("::").Count() == 2).Count == 0) // NB: Accomodates inline references!
                            {
                                contradiction = true;
                                break;
                            }
                            else if (negationSigns.Contains(relation[1]) && relationSet.Contains(init_cast.characters[character].elements[relation[0]].element) && relationSet.FindAll(x => x.Split("::").Count() == 2).Count == 0) // NB: Accomodates inline references!
                            {
                                contradiction = true;
                                break;
                            }
                        }
                        if (contradiction)
                        {
                            continue;
                        }

                        Dictionary<string, Dictionary<string, Element>> iteratedData = new Dictionary<string, Dictionary<string, Element>>();
                        foreach (string category in subjectDatas[i].Keys)
                        {
                            iteratedData.Add(category, new Dictionary<string, Element>());
                            foreach (string element in subjectDatas[i][category].Keys)
                                iteratedData[category].Add(element, new Element(subjectDatas[i][category][element]));
                        }
                        iteratedData[template.template].Add(subject, new Element(template.template, subject, new List<List<string>>(), 1, 1, false));
                        iteratedData[template.template][subject].relations.Add(new List<string>() { template.template, "=>&", subject });
                        foreach (string category in init_cast.characters[character].elements.Keys)
                        {
                            iteratedData[template.template][subject].relations.Add(new List<string>() { category, "=>&", init_cast.characters[character].elements[category].element });
                        }

                        iteratedSubjectMappings.Add(iteratedSubjectMapping);
                        iteratedSubjectDatas.Add(iteratedData);
                    }
                }
                subjectMappings = iteratedSubjectMappings;
                subjectDatas = iteratedSubjectDatas;
                subjects.RemoveAt(0);

                if (subjectMappings.Count == 0)
                    break;
            }
            //if (subjectMappings.Count == 0)
            //    continue;

            // STEP 2: Extend to find all possible mappings from our defined subjects to our characters
            while (ambiguousSubjects.Count > 0)
            {
                List<int> order = new List<int>();
                foreach (string ambiguousSubject in ambiguousSubjects)
                {
                    List<string> unusedSubjects = new List<string>();
                    List<List<String>> relations = template.templateElements[template.template][ambiguousSubject].relations.FindAll(x => template.template.Equals(x[0]) && (forwardImplicationSigns.Contains(x[1]) || "!=".Equals(x[1])));
                    foreach (List<string> relation in relations)
                    {
                        foreach (string unusedSubject in new List<string>(relation[2].Split("/")))
                        {
                            if (ambiguousSubjects.Contains(unusedSubject) && !unusedSubjects.Contains(unusedSubject))
                            {
                                unusedSubjects.Add(unusedSubject);
                            }
                        }
                    }
                    order.Add(unusedSubjects.Count);
                }

                ambiguousSubjects = ambiguousSubjects.OrderBy(x => order[ambiguousSubjects.IndexOf(x)]).ToList();
                string subject = ambiguousSubjects[0];

                int impliedIndex = template.templateElements[template.template][subject].relations.FindIndex(x => template.template.Equals(x[0]) && forwardImplicationSigns.Contains(x[1])); // NB: This must exist for our subject to have been registered as ambiguous...
                List<string> impliedSubjects = new List<string>(template.templateElements[template.template][subject].relations[impliedIndex][2].Split("/")); // NB: UNDEFINED subjects, by definition, aren't referenced in any way in a clue...

                int negatedIndex = template.templateElements[template.template][subject].relations.FindIndex(0, x => template.template.Equals(x[0]) && "!!=".Contains(x[1]));
                List<string> negatedSubjects = new List<string>();
                while (negatedIndex >= 0)
                {
                    foreach (string negatedSubject in new List<string>(template.templateElements[template.template][subject].relations[negatedIndex][2].Split("/")))
                        negatedSubjects.Add(negatedSubject);

                    if (negatedIndex == template.templateElements[template.template][subject].relations.Count - 1)
                        break;

                    negatedIndex = template.templateElements[template.template][subject].relations.FindIndex(negatedIndex + 1, x => template.template.Equals(x[0]) && "!!=".Contains(x[1]));
                }

                List <Dictionary<string, int>> iteratedSubjectMappings = new List<Dictionary<string, int>>();
                List<Dictionary<string, Dictionary<string, Element>>> iteratedSubjectDatas = new List<Dictionary<string, Dictionary<string, Element>>>();
                for (int i = 0; i < subjectMappings.Count; i++)
                {
                    // LOGIC PROCESSING: Every ambiguous subject maps to an existing, 'key' character...
                    List<string> unambiguousSubjects = subjectMappings[i].Keys.ToList().FindAll(x => impliedSubjects.Contains(x));

                    List<int> negatedCharacters = new List<int>();
                    foreach (string negatedSubject in negatedSubjects)
                        if (subjectMappings[i].ContainsKey(negatedSubject))
                            negatedCharacters.Add(subjectMappings[i][negatedSubject]);

                    foreach (string unambiguousSubject in unambiguousSubjects)
                    {
                        if (negatedCharacters.Contains(subjectMappings[i][unambiguousSubject]))
                            continue;

                        int character = subjectMappings[i][unambiguousSubject];

                        Dictionary<string, int> iteratedSubjectMapping = new Dictionary<string, int>(subjectMappings[i]);
                        iteratedSubjectMapping.Add(subject, character);

                        bool contradiction = false;
                        foreach (List<string> relation in template.templateElements[template.template][subject].relations)
                        {
                            if (!init_cast.characters[character].elements.ContainsKey(relation[0]))
                                continue;

                            List<string> relationSet = new List<string>(relation[2].Split("/"));
                            if (forwardImplicationSigns.Contains(relation[1]) && !relationSet.Contains(init_cast.characters[character].elements[relation[0]].element) && relationSet.FindAll(x => x.Split("::").Count() == 2).Count == 0) // NB: Accomodates inline references!
                            {
                                contradiction = true;
                                break;
                            }
                            else if (negationSigns.Contains(relation[1]) && relationSet.Contains(init_cast.characters[character].elements[relation[0]].element) && relationSet.FindAll(x => x.Split("::").Count() == 2).Count == 0) // NB: Accomodates inline references!
                            {
                                contradiction = true;
                                break;
                            }
                        }
                        if (contradiction)
                        {
                            continue;
                        }

                        Dictionary<string, Dictionary<string, Element>> iteratedData = new Dictionary<string, Dictionary<string, Element>>();
                        foreach (string category in subjectDatas[i].Keys)
                        {
                            iteratedData.Add(category, new Dictionary<string, Element>());
                            foreach (string element in subjectDatas[i][category].Keys)
                                iteratedData[category].Add(element, new Element(subjectDatas[i][category][element]));
                        }
                        iteratedData[template.template].Add(subject, new Element(template.template, subject, new List<List<string>>(), 1, 1, true));
                        iteratedData[template.template][subject].relations.Add(new List<string>() { template.template, "=>&", unambiguousSubject });
                        foreach (string category in init_cast.characters[character].elements.Keys)
                        {
                            iteratedData[template.template][subject].relations.Add(new List<string>() { category, "=>&", init_cast.characters[character].elements[category].element });
                        }

                        iteratedSubjectMappings.Add(iteratedSubjectMapping);
                        iteratedSubjectDatas.Add(iteratedData);
                    }
                }
                subjectMappings = iteratedSubjectMappings;
                subjectDatas = iteratedSubjectDatas;
                ambiguousSubjects.RemoveAt(0);

                if (subjectMappings.Count == 0)
                    break;
            }
            //if (subjectMappings.Count == 0)
            //    continue;

            // STEP 4: Check none of these possible mappings lead to a contradiction (with our final data or their own data)
            // NB: Include time-out clause to ensure variety of templates...
            List<int> reorder = new List<int>();
            for (int i = 0; i < subjectMappings.Count; i++)
                reorder.Add(i);
            reorder = reorder.OrderBy(x => rng.Next()).ToList();
            subjectMappings = subjectMappings.OrderBy(x => reorder[subjectMappings.IndexOf(x)]).ToList();
            subjectDatas = subjectDatas.OrderBy(x => reorder[subjectDatas.IndexOf(x)]).ToList();

            // DEBUG:
            Console.WriteLine("TEMPLATE " + (t+1) + "/" + templates.Count + ": There are " + subjectMappings.Count + " possible clues...");
            Console.Write(" * 0/0 of these are non-contradictory...");

            int count = 0;
            long init_templateTicks = DateTime.UtcNow.Ticks;
            long templateTicks = init_templateTicks - init_ticks;
            long templateSurplus = Math.Clamp((t + 1) * templatePeriod - templateTicks, 0, Math.Min(templatePeriod, Math.Max(period - templateTicks, 0))); // Math.Clamp((t + 1) * templatePeriod - templateTicks, 0, Math.Max(period - templateTicks, 0)); // Are we ahead of even our very best expectations? FIXME: Turned off to avoid 'spamming' the processor with similar clues... need a way to filter these earlier...
            for (int i = 0; i < subjectMappings.Count; i++)
            {
                Clue clue = new Clue(template.template, "", template.templateElements);
                Dictionary<string, int> subjectMapping = subjectMappings[i];
                Dictionary<string, Dictionary<string, Element>> subjectData = subjectDatas[i];

                // PARSE MAPPINGS INTO CLUE
                bool parsed = true;
                string[] parse = template.template.Split("#");
                for (int j = 1; j < parse.Count(); j += 2)
                {
                    string[] insert = parse[j].Split("::");
                    if (insert.Count() != 2)
                    {
                        // DEBUG
                        Console.WriteLine(j+" "+insert.Count());

                        parsed = false;
                        break;
                    }

                    if (!init_cast.characters[subjectMapping[insert[0]]].elements.ContainsKey(insert[1]))
                    {
                        parsed = false;
                        break;
                    }

                    parse[j] = init_cast.characters[subjectMapping[insert[0]]].elements[insert[1]].element;
                    if (parse[j].Equals("UNDEFINED"))
                    {
                        parsed = false;
                        break; // No one can comment on an UNDEFINED quantity...
                    }

                    List<string> parsedRelation = new List<string>() { insert[1], "=>&", parse[j] };
                    if (!clue.clueElements[template.template][insert[0]].ContainsRelation(parsedRelation))
                        clue.clueElements[template.template][insert[0]].relations.Add(parsedRelation);

                    List<string> minRelation = new List<string>() { insert[1], "MIN:1", parse[j] };
                    if (!clue.clueElements[template.template][insert[0]].ContainsRelation(minRelation))
                        clue.clueElements[template.template][insert[0]].relations.Add(minRelation);
                }
                if (!parsed)
                    continue;

                for (int j = 0; j < clue.clueElements[template.template].Count; j++)
                {
                    string element = clue.clueElements[template.template].Keys.ElementAt(j);
                    for (int k = 0; k < clue.clueElements[template.template][element].relations.Count; k++)
                    {
                        List<string> relationSet = new List<string>(clue.clueElements[template.template][element].relations[k][2].Split("/"));
                        for (int l = 0; l < relationSet.Count; l++)
                        {
                            string[] insert = relationSet[l].Split("::");
                            if (insert.Count() == 1)
                                continue;

                            string relationParse = init_cast.characters[subjectMapping[insert[0]]].elements[insert[1]].element;
                            if (relationParse.Equals("UNDEFINED"))
                            {
                                // Do we allow the *implication* of an UNDEFINED quantity?
                            }

                            relationSet[l] = relationParse;
                        }

                        // FIXME: Do more with distinctness?
                        clue.clueElements[template.template][element].relations[k][2] = string.Join("/", relationSet.Distinct());
                    }
                }
                if (!parsed)
                    continue;

                // CHECK FOR CONTRADICTION WITH FINAL DATA
                // NB: Does this only exist because of possible qualifiers on undefineds?
                Dictionary<string, Dictionary<string, Element>> clueInContext = Logic.ExtrapolateRelations(new List<Dictionary<string, Dictionary<string, Element>>>() { clue.clueElements, init_cast.finalData, subjectData }, init_cast.characters.FindAll(x => !x.identity.Equals("UNDEFINED")).ToList().Count, init_cast.characters.Count);
                if (clueInContext.Count == 0)
                    continue;

                clue.clue = string.Join("", parse);
                clue.character = subjectMapping["Culprit"];
                // CHECKME: Is the GA more efficient with this left on? // NB: Inefficient to put this ahead...
                clue.obviousness = init_cast.ambiguities - init_cast.EvaluateAmbiguities(Logic.ExtrapolateRelations(new List<Dictionary<string, Dictionary<string, Element>>>() { clue.clueElements, init_cast.intermediaryData }, init_cast.characters.FindAll(x => !x.identity.Equals("UNDEFINED")).ToList().Count, init_cast.characters.Count));

                if (!clues[template.template].ContainsKey(init_cast.characters[subjectMapping["Culprit"]].identity))
                    clues[template.template].Add(init_cast.characters[subjectMapping["Culprit"]].identity, new List<Clue>());
                clues[template.template][init_cast.characters[subjectMapping["Culprit"]].identity].Add(clue);
                count++;

                Console.Write("\r * " + count+"/"+(i+1)+" of these are non-contradictory...");

                if (DateTime.UtcNow.Ticks - init_templateTicks > templatePeriod+templateSurplus)
                    break;
            }

            Console.WriteLine();
            Console.WriteLine(" * It takes " + (((float)(DateTime.UtcNow.Ticks - init_templateTicks) / Math.Pow(10, 7))) + "s to check these...");
            Console.WriteLine();

            if (count == 0)
                clues.Remove(template.template);
        }

        return clues;
    }
}

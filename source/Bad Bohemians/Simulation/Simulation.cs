/* Simulation: Generates answers to the puzzles the genetic algorithm will later generate */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Simulation
{
    const int BOHEMIANS = 5;
    const int RED_HERRINGS = 0;
    Dictionary<string, bool> FIRST_ORDER_IDENTITIES = new Dictionary<string, bool>() { { "Christian Name", true } , { "Family Name", true }, { "Nobility", true }, { "Domain", true }, { "Headline", true } };
    Dictionary<string, bool> SECOND_ORDER_IDENTITIES = new Dictionary<string, bool>() { { "Gender", false } };
    Dictionary<string, bool> IDENTITIES = new Dictionary<string, bool>();
    public Cast cast;

    public SimulationProcessor processor;
    private Random rng;

    // Constructor
    public Simulation(int seed)
    {
        foreach (KeyValuePair<string, bool> firstOrderIdentity in FIRST_ORDER_IDENTITIES)
            IDENTITIES.Add(firstOrderIdentity.Key, firstOrderIdentity.Value);
        foreach (KeyValuePair<string, bool> secondOrderIdentity in SECOND_ORDER_IDENTITIES)
            IDENTITIES.Add(secondOrderIdentity.Key, secondOrderIdentity.Value);

        rng = new Random(seed);

        // DEBUG:
        Console.WriteLine("--------------------------------------");
        Console.WriteLine("--- STEP 1: RUNNING THE SIMULATION ---");
        Console.WriteLine("--------------------------------------");
        Console.WriteLine();

        Console.WriteLine("--- OUR PROCESSOR IS INITIALISING ---\n");
        processor = new SimulationProcessor();

        // DEBUG:
        //Console.WriteLine("--- OUR PROCESSOR CONTAINS THE FOLLOWING ELEMENTS AND RELATIONS ---\n");
        //processor.PrintCharacterDescriptors();

        // DEBUG:
        //Console.WriteLine("--- OUR GAME ALWAYS GIVES THE FOLLOWING ELEMENTS (AND THEIR RELATIONS TO ALL OTHER ELEMENTS) ---\n");
        //processor.PrintFixedElements();

        Console.WriteLine("--- OUR SIMULATOR IS RUNNING ---\n");
        cast = InitialiseCast();

        // DEBUG:
        //Console.WriteLine("--- OUR SIMULATOR GENERATES THE FOLLOWING CAST OF CHARACTERS ---\n");
        //cast.PrintCast();

        // DEBUG:
        //Console.WriteLine("--- OUR GAME USES (ONLY) THE FOLLOWING ELEMENTS AND RELATIONS ---\n");
        //cast.PrintData(cast.initialData);

        // DEBUG:
        //Console.WriteLine("--- OUR GAME ENDS UP WITH THE FOLLOWING ELEMENTS AND RELATIONS ---\n");
        //cast.PrintData(cast.finalData);

        // DEBUG:
        //Console.WriteLine("--- OUR GAME WILL BE SOLVED WHEN IT CONTAINS THE FOLLOWING ELEMENTS AND RELATIONS ---\n");
        //cast.PrintData(cast.firstOrderFinalData);
    }

    public Cast InitialiseCast()
    {
        Cast cast = new Cast();
        Dictionary<string, Dictionary<string, Element>> castElements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in processor.characterDescriptors.Keys)
        {
            castElements.Add(category, new Dictionary<string, Element>());
            foreach (string element in processor.characterDescriptors[category].Keys)
            {
                castElements[category].Add(element, new Element(processor.characterDescriptors[category][element]));
            }
        }

        // STEP 1: Getting consistent, individual identifiers...
        const int MAX_ATTEMPTS = 1;
        int attempt = 0;
        for (; attempt < MAX_ATTEMPTS; attempt++)
        {
            // DEBUG:
            Console.WriteLine("GENERATING CHARACTERS: ");

            bool attemptFailed = false;
            for (int i = 0; i < BOHEMIANS + RED_HERRINGS; i++)
            {
                // DEBUG:
                long init_ticks = DateTime.UtcNow.Ticks;

                // FIXME: Uniqueness assumed throughout this...
                Character character = new Character();
                Dictionary<string, Dictionary<string, Element>> characterElements = Logic.ExtrapolateRelations(castElements, 0, 1);
               

                foreach (string category in IDENTITIES.Keys)
                {
                    if (i >= BOHEMIANS && category.Equals("Headline"))
                        continue;

                    List<string> choices = characterElements[category].Keys.ToList();
                    choices.RemoveAll(x => x.Equals("") || x.Equals("UNDEFINED") || characterElements[category][x].maximum == 0);
                    choices = choices.OrderBy(x => rng.Next()).ToList();

                    string choice = "";
                    for (int j = 0; j < choices.Count; j++)
                    {
                        Element chosenElement = new Element(category, choices[j], new List<List<string>>(), 1, 1);
                        Dictionary<string, Dictionary<string, Element>> chosenElements = new Dictionary<string, Dictionary<string, Element>>();
                        chosenElements.Add(category, new Dictionary<string, Element>());
                        chosenElements[category].Add(choices[j], chosenElement);

                        Dictionary<string, Dictionary<string, Element>> extrapolation = Logic.ExtrapolateRelations(new List<Dictionary<string, Dictionary<string, Element>>>() { characterElements, chosenElements }, 0, 1);
                        if (extrapolation.Count > 0)
                        {
                            choice = choices[j];
                            if (IDENTITIES[category])
                                castElements[category][choices[j]].IntegrateMaximum(0);

                            characterElements = new Dictionary<string, Dictionary<string, Element>>(extrapolation);
                            break;
                        }
                    }
                    if (choice.Equals(""))
                    {
                        attemptFailed = true;
                        break;
                    }
                }
                if (attemptFailed)
                {
                    break;
                }

                // We have successfully assembled a non-contradictory character...

                foreach (string category in characterElements.Keys)
                    foreach (string element in characterElements[category].Keys)
                        if (characterElements[category][element].minimum == 1)
                            character.elements.Add(category, new Element(processor.characterDescriptors[category][element]));

                character.IntegrateIdentity();
                cast.characters.Add(character);

                // DEBUG:
                Console.WriteLine(" * It takes " + (((float)(DateTime.UtcNow.Ticks - init_ticks)) / Math.Pow(10, 7)) + "s to generate CHARACTER " + (i + 1) + "/" + BOHEMIANS + "...");
            }

            if (attemptFailed)
            {
                // DEBUG:
                Console.WriteLine(" * ATTEMPT " + (attempt + 1) + " HAS FAILED...");
                Console.WriteLine();

                cast = new Cast();
                continue;
            }

            break;
        }

        // STEP 2: Postprocessing
        cast.firstOrderCategories = new List<string>();
        foreach (string firstOrderIdentity in FIRST_ORDER_IDENTITIES.Keys)
            cast.firstOrderCategories.Add(firstOrderIdentity);

        List<string> categories = new List<string>(cast.firstOrderCategories);
        for (int i = 0; i < cast.characters.Count; i++)
            foreach (string category in cast.characters[i].elements.Keys)
                if (!categories.Contains(category))
                    categories.Add(category);

        for (int i = 0; i < cast.characters.Count; i++)
            foreach (string category in categories)
                if (!cast.characters[i].elements.ContainsKey(category))
                    cast.characters[i].elements.Add(category, new Element(category, "UNDEFINED", new List<List<string>>()));

        cast.InitialiseData(processor.GetFixedElements());

        // DEBUG:
        Console.WriteLine();
        Console.WriteLine("INITIAL NARRATIVE: " + cast.unknowns + " unknowns, " + cast.ambiguities + " ambiguities...");
        Console.WriteLine();

        return cast;
    }
}

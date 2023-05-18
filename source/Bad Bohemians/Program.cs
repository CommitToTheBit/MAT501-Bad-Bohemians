/* Program: Main script, which runs the genetic algorithm on a very basic console */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        Console.Title = "Bad Bohemians";

        // DEBUG: Pause to start recording recording
        //Console.Write("(Press any key to start...)");
        //Console.ReadKey();
        //Console.Write("\r"+new string(' ', Console.WindowWidth)+"\r");

        // DEBUG: Pause for recording
        //Console.Write("(Press any key to unpause...)");
        //Console.ReadKey();
        //Console.Write("\r"+new string(' ', Console.WindowWidth)+"\r");

        while (true)
        {
            // DEBUG:
            long init_ticks = DateTime.UtcNow.Ticks;

            // STEP 1: Running the simulation (this is all handled when the simulation is initialised)
            Simulation simulation = new Simulation((int)DateTime.UtcNow.Ticks);
            if (simulation.cast.characters.Count == 0)
                return;

            // DEBUG: Pause for recording
            //Console.Write("(Press any key to unpause...)");
            //Console.ReadKey();
            //Console.Write("\r"+new string(' ', Console.WindowWidth)+"\r");

            // STEP 2: Running the genetic algorithm (again, all handled when intialised)
            GeneticAlgorithm geneticAlgorithm = new GeneticAlgorithm(simulation.cast, (int)DateTime.UtcNow.Ticks);

            // STEP 3: Presenting the story
            Console.WriteLine("--- - THE FOLLOWING WHODUNNIT TOOK A TOTAL " + (((float)(DateTime.UtcNow.Ticks - init_ticks)) / Math.Pow(10, 7)) + "s TO GENERATE ---\n");
            PrintGame(geneticAlgorithm.cast, geneticAlgorithm.clues);

            // STEP 4: Checking the solution
            Console.WriteLine("--- PRESS ANY KEY TO SEE THE SOLUTION ---\n");
            Console.ReadKey();
            Console.Write("\r"+new string(' ', Console.WindowWidth)+"\r");
            geneticAlgorithm.cast.PrintCast(true, false);

            // DEBUG: Useful for checking if 'expert system' is deducing what it is meant to!
            /*Console.WriteLine("--- PRESS ANY KEY TO SEE WHAT WE CAN DEDUCE FROM THESE CLUES ---\n");
            Console.ReadKey();
            PlayGame(geneticAlgorithm.cast, geneticAlgorithm.clues);*/

            // STEP 5: Continuing...
            Console.WriteLine("--- PRESS ANY KEY TO GENERATE ANOTHER PUZZLE ---\n");
            Console.ReadKey();
            Console.Write("\r"+new string(' ', Console.WindowWidth)+"\r");
        }
    }

    static void PrintGame(Cast init_cast, List<Clue> init_clues)
    {
        Random rng = new Random(0);

        // STEP 0: Handling 'fixed' rules... unnecessary for current narrative structure
        /*Dictionary<string, List<string>> predefineds = new Dictionary<string, List<string>>();
        foreach (Character character in init_cast.characters)
        {
            foreach (string category in character.elements.Keys)
            {
                if (!predefineds.ContainsKey(category))
                    predefineds.Add(category, new List<string>());

                if (!predefineds[category].Contains(character.elements[category].element) && !character.elements[category].element.Equals("UNDEFINED"))
                    predefineds[category].Add(character.elements[category].element);
            }
        }*/

        // DEBUG: Lists all elements - uneccessary, since algorithm optimises for unknowns
        /*Console.WriteLine("");
        foreach (string category in predefineds.Keys)
        {
            if (predefineds[category].Count == 0)
                continue;

            List<string> elements = predefineds[category].OrderBy(x => rng.Next()).ToList();

            string formattedCategory = category.Substring(0, Math.Min(16, category.Length));
            while (formattedCategory.Length < 16)
                formattedCategory += " ";

            Console.Write(" * " + formattedCategory + ": ");
            for (int i = 0; i < elements.Count - 1; i++)
                Console.Write(elements[i] + ", ");
            Console.WriteLine(elements[elements.Count - 1]);

        }*/

        // STEP 1: Printing story...
        Console.WriteLine("Has the tabloid always held such sway over this sceptred isle? You ponder this over coffee and the latest copy of Ladies' Fortnightly (Date of Issue: Sunday, 22nd of April, Year of Our Lord 1900). Subscribed to the stifling moralism of the time, and written with the insufferable flourishes of an Oxbridge humourist, you will grant the rag one modicum of credit: it has a unyielding desire to libel the ruling classes.\n");
        Console.WriteLine("Following a string of high-profile High Court appearances, 'Fortnightly' now plays a deft game with its readership. On each page, it provides a series of scandals, and *just so happens* to mention a corresponding series of nobles.  Whether or not the reader infers exactly *one* headline corresponds to each noble is up to them; on account of the oblique nature of the writing, this is not recognised *legally* as defamation.\n");
        Console.WriteLine("You come upon such a page now:\n");

        // STEP 2: Handling story-specific clues...
        for (int i = 0; i < init_clues.Count; i++)
            Console.WriteLine(" * " + init_clues[i].clue + "\n");

        Console.WriteLine("Can you match each headline to each noble's forename, surname, title *and* county?\n");
    }

    static void PlayGame(Cast init_cast, List<Clue> init_clues)
    {
        List<Dictionary<string, Dictionary<string, Element>>> elements = new List<Dictionary<string, Dictionary<string, Element>>>();
        //elements.Add(init_cast.initialData);
        elements.Add(init_cast.intermediaryData); // DEBUG: A fuller picture of what we'd deduce 'if we knew all our elements'...
        foreach (Clue clue in init_clues)
            elements.Add(clue.clueElements);

        Dictionary<string, Dictionary<string, Element>> solution = Logic.ExtrapolateRelations(elements, init_cast.characters.FindAll(x => !x.identity.Equals("UNDEFINED")).ToList().Count, init_cast.characters.Count);
        foreach (string category in solution.Keys)
        {
            Console.WriteLine("CATEGORY: " + category);
            Console.WriteLine();

            foreach (string element in solution[category].Keys)
            {
                solution[category][element].PrintElement(false, false);
            }

            Console.WriteLine();
        }
    }
}
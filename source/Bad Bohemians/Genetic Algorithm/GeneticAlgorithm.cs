/* Genetic Algorithm: Learning AI that procedurally generates narratives from pre-made clues */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GeneticAlgorithm
{
    const int PARENTS = 3;
    int CHILDREN;

    public GeneticAlgorithmProcessor processor;

    public Cast cast;

    public List<List<Narrative>> population;
    public List<Clue> clues;

    private Random rng;

    public GeneticAlgorithm(Cast init_cast, int seed)
    {
        CHILDREN = 0;
        for (int i = 0; i < PARENTS; i++)
            for (int j = i + 1; j < PARENTS; j++)
                CHILDREN++;
        CHILDREN = Math.Max(CHILDREN, 1);
        
        rng = new Random(seed);

        // DEBUG:
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine("--- STEP 2: RUNNING THE GENETIC ALGORITHM ---");
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine();

        // DEBUG:
        Console.WriteLine("--- OUR PROCESSOR IS INITIALISING ---\n");
        processor = new GeneticAlgorithmProcessor(init_cast, rng.Next());

        cast = new Cast(init_cast);
        cast.clues = 0; /// NB: Unnecessary; included for safety
        foreach (string template in processor.clues.Keys)
        {
            foreach (string firstPerson in processor.clues[template].Keys)
            {
                int index = cast.characters.FindIndex(x => x.identity.Equals(firstPerson));
                cast.characters[index].clues.Add(template, new List<Clue>());
                foreach (Clue clue in processor.clues[template][firstPerson])
                    cast.characters[index].clues[template].Add(new Clue(clue));
            }
            cast.clues += processor.clues.Count;
        }

        // DEBUG:
        Console.WriteLine("--- OUR PROCESSOR GENERATES THE FOLLOWING CLUES ---\n");
        cast.PrintClues(false, false);

        // DEBUG: Pause for recording
        //Console.Write("(Press any key to unpause...)");
        //Console.ReadKey();
        //Console.Write("\r"+new string(' ', Console.WindowWidth)+"\r");

        // DEBUG:
        Console.WriteLine("--- OUR GENETIC ALGORITHM IS INTIIALISING POPULATION 0 ---\n");
        population = InitialisePopulation(PARENTS);

        long minIterationPeriod = (long)(0*Math.Pow(10, 7));
        long maxIterationPeriod = (long)(240*Math.Pow(10, 7));

        long init_IterationTicks = DateTime.UtcNow.Ticks;

        for (int i = 0; ; i++)
        {
            // STEP 1: Evaluation
            long evaluationTicks = DateTime.UtcNow.Ticks-init_IterationTicks;
            Console.WriteLine("--- OUR GENETIC ALGORITHM EVALUATES " + population[i].Count + " NARRATIVES FROM GENERATION " + i + " (" + ((long)(evaluationTicks/Math.Pow(10,7))) + "s) ---\n");

            if (!EvaluateGeneration(i) && evaluationTicks > minIterationPeriod) // Have we 'stabled out' and started wasting time?
                break;
            else if (evaluationTicks > maxIterationPeriod) // Have we timed out?
                break;

            // STEP 2: Selection
            long selectionTicks = DateTime.UtcNow.Ticks-init_IterationTicks;
            Console.WriteLine("--- OUR GENETIC ALGORITHM SELECTS " + PARENTS + " NARRATIVES FROM GENERATION " + i + " (" + ((long)(selectionTicks/Math.Pow(10, 7))) + "s) ---\n");

            SelectGeneration(i, PARENTS);

            PrintGeneration(i);

            // STEP 3: Mutation
            long mutationTicks = DateTime.UtcNow.Ticks-init_IterationTicks;
            Console.WriteLine("--- OUR GENETIC ALGORITHM MUTATES " + PARENTS + " NARRATIVES FROM GENERATION " + i + " (" + ((long)(mutationTicks/Math.Pow(10, 7))) + "s) ---\n");
            //Console.Write("Our algorithm allocates "+(PARENTS+(long)((maxMutationPeriod+mutationSurplus)/Math.Pow(10, 7)))+"s for mutation...");

            MutateGeneration(i, init_IterationTicks+maxIterationPeriod-DateTime.UtcNow.Ticks);

            Console.Write("\r");
            PrintGeneration(i);

            // STEP 4: Crossover
            long crossoverTicks = DateTime.UtcNow.Ticks-init_IterationTicks;
            Console.WriteLine("--- OUR GENETIC ALGORITHM SPAWNS " + PARENTS + " NARRATIVES FROM GENERATION " + i + " (" + ((long)(crossoverTicks / Math.Pow(10, 7))) + "s) ---\n");
            //Console.Write("Our algorithm allocates " + (CHILDREN + (long)((maxCrossoverPeriod + crossoverSurplus) / Math.Pow(10, 7))) + "s for crossover...");

            CrossoverGeneration(i, init_IterationTicks+maxIterationPeriod-DateTime.UtcNow.Ticks);

            Console.Write("\r");
            PrintGeneration(i+1);

            if (i+1 > 0)
            {
                Console.WriteLine("Our narrative was last improved "+RedundantGenerations(i+1)+" generations ago...");
                Console.WriteLine();
            }
        }

        SelectGeneration(population.Count - 1, 1);

        Console.WriteLine("--- OUR GENETIC ALGORITHM DECIDES ON A NARRATIVE AFTER " + (population.Count - 1) + " GENERATIONS ---\n");
        PrintGeneration(population.Count - 1);

        clues = population[population.Count - 1][0].clues;
    }

    // STEP 0: INITIALISATION:
    public List<List<Narrative>> InitialisePopulation(int N)
    {
        List<List<Narrative>> population = new List<List<Narrative>>();
        population.Add(new List<Narrative>());

        for (int n = 0; n < N; n++)
        {
            // DEBUG:
            Console.WriteLine("INITIALISING NARRATIVE " + (n + 1) + "/" + N + ":");
            population[0].Add(InitialiseNarrative());
            Console.WriteLine();
        }

        return population;
    }

    public Narrative InitialiseNarrative(bool characterRandomness = true, bool DEBUG = true)
    {
        Narrative narrative = new Narrative(new List<Clue>(), cast.intermediaryData, cast.unknowns, cast.ambiguities, 0);
        narrative.data = Logic.ExtrapolateRelations(narrative.data);

        List<Character> characters = cast.characters.FindAll(x => x.clues.Count > 0);
        while (characters.Count > 0)
        {
            long init_ticks = DateTime.UtcNow.Ticks;

            // STEP 1: Find all possible choices for the 'most optimal' character remaining...
            characters = characters.OrderBy(x => rng.Next()).ToList();
            if (!characterRandomness) // NB: Aims to maximise total clues found, but reduces randomness across initialisations...
                characters = characters.OrderBy(x => x.GetCluesAvailable(narrative.clues, cast.firstOrderFinalData).Count).ToList();

            while (characters[0].GetCluesAvailable(narrative.clues, cast.firstOrderFinalData).Count == 0)
            {
                characters.RemoveAt(0);
                if (characters.Count == 0)
                    return narrative;
            }

            List<Narrative> choices = new List<Narrative>();
            foreach (Clue clue in characters[0].GetCluesAvailable(narrative.clues, cast.firstOrderFinalData))
            {
                choices.Add(new Narrative(narrative));
                choices[choices.Count - 1].clues.Add(new Clue(clue));
            }

            if (DEBUG)
            {
                int count = 0;
                foreach (string template in characters[0].clues.Keys)
                    count += characters[0].clues[template].Count;

                Console.WriteLine("CLUE " + (narrative.clues.Count + 1) + ": This character has a total of " + count + " clues...");
                Console.Write(" * ");
            }

            narrative = new Narrative(ChooseNarrative(choices, narrative, 1, true, true, int.MaxValue));
            characters.RemoveAt(0);
        }

        return narrative;
    }

    // STEP 1: Evaluation
    public bool EvaluateGeneration(int g)
    {
        // First, determine which narrative is optimal
        population[g] = population[g].OrderBy(x => x.unknowns).ThenBy(x => x.ambiguities).ThenBy(x => x.obviousness).ThenBy(x => rng.Next()).ToList();

        return RedundantGenerations(g) < cast.bohemians;
    }

    // STEP 2: Selection
    public void SelectGeneration(int g, int s)
    {
        while (population[g].Count > s)
        {
            population[g].RemoveAt(population[g].Count - 1);
        }
    }

    // STEP 2: Mutation
    public void MutateGeneration(int g, long period)
    {
        long timeout = DateTime.UtcNow.Ticks+period;

        bool redundancy = RedundantGenerations(g-1) > 0;
        for (int n = 0; n < PARENTS; n++)
        {
            population[g][n] = MutateNarrative(population[g][n], redundancy, timeout-DateTime.UtcNow.Ticks);
        }
    }

    public Narrative MutateNarrative(Narrative narrative, bool random, long period)
    {
        // STEP 1: Randomly choose a 'direction' for mutation
        Narrative mutation = new Narrative(narrative);
        List<int> integers = new List<int>();
        for (int i = 0; i < narrative.clues.Count; i++)
            integers.Add(i);
        integers = integers.OrderBy(x => rng.Next()).ToList();
        integers = integers.FindAll(x => integers.IndexOf(x) < 2).OrderBy(x => x).ToList();

        mutation.clues.RemoveAt(integers[1]);
        mutation.clues.RemoveAt(integers[0]);

        List<Dictionary<string, Dictionary<string, Element>>> dataUnion = new List<Dictionary<string, Dictionary<string, Element>>>();
        dataUnion.Add(cast.intermediaryData);
        foreach (Clue clue in mutation.clues)
            dataUnion.Add(clue.clueElements);

        mutation.data = Logic.ExtrapolateRelations(dataUnion);

        // STEP 2: Find all possible mutations in this direction
        List<Narrative> choices = new List<Narrative>();
        foreach (Clue partialClue in cast.characters[narrative.clues[integers[0]].character].GetCluesAvailable(mutation.clues, cast.firstOrderFinalData))
        {
            Narrative partialChoice = new Narrative(mutation);
            partialChoice.clues.Add(new Clue(partialClue));

            foreach (Clue clue in cast.characters[narrative.clues[integers[1]].character].GetCluesAvailable(partialChoice.clues, cast.firstOrderFinalData))
            {
                Narrative choice = new Narrative(partialChoice);
                choice.clues.Add(new Clue(clue));

                choice.unknowns = cast.EvaluateUnknowns(choice.clues);
                if (choice.unknowns <= narrative.unknowns)
                    choices.Add(choice);
            }
        }

        return new Narrative(ChooseNarrative(choices, narrative, 2, false, true, period, true));
    }

    // STEP 3: Cross Generation
    public void CrossoverGeneration(int g, long period)
    {
        if (population.Count <= g)
            return;

        // STEP 1: Set generation g+1 as empty
        if (population.Count == g + 1)
            population.Add(new List<Narrative>());
        else
            population[g + 1] = new List<Narrative>();

        // STEP 2: Copy best narrative to generation g+1 each time
        EvaluateGeneration(g);
        for (int n = 0; n < population[g].Count; n++)
            population[g + 1].Add(new Narrative(population[g][0]));

        /* MORE ADVANCED CROSSOVER THAT, ULTIMATELY, IS INEFFICIENT
        // STEP 2: Pass all generation g parents down to generation g+1
        for (int n = 0; n < population[g].Count; n++)
            population[g + 1].Add(new Narrative(population[g][n]));

        // STEP 3: Add the most optimal 'child' of each pairing of parents
        long timeout = DateTime.UtcNow.Ticks+period;
        int count = 0;

        bool random = RedundantGenerations(g) >= 0;
        for (int n = 0; n < population[g].Count; n++)
        {
            for (int m = n + 1; m < population[g].Count; m++)
            {
                Narrative narrative = CrossNarratives(population[g][n], population[g][m], random, timeout-DateTime.UtcNow.Ticks);
                if (!population[g][n].IsEqual(narrative) && !population[g][m].IsEqual(narrative))        
                    population[g+1].Add(narrative);
            }
        }*/
    }

    public Narrative CrossNarratives(Narrative narrativeA, Narrative narrativeB, bool random, long period)
    {
        // We can assume narrativeA is the 'preferred' narrative...
        if (narrativeB.clues.Count < Math.Max(narrativeA.clues.Count, cast.bohemians))
            return narrativeA;
        else if (narrativeA.clues.Count < narrativeB.clues.Count)
            return narrativeB;

        // STEP 1: Find all non-trivial crossovers between narratives
        List<List<int>> crossoverBinaries = new List<List<int>>();
        crossoverBinaries.Add(new List<int>());

        List<int> characters = new List<int>();
        foreach (Character character in cast.characters.FindAll(x => !x.identity.Equals("UNDEFINED")))
            characters.Add(cast.characters.IndexOf(character));

        foreach (int character in characters)
        {
            List<List<int>> iteratedCrossoverBinaries = new List<List<int>>();
            foreach (List<int> crossoverBinary in crossoverBinaries)
            {
                if (narrativeA.clues.FindAll(x => x.character == character).Count > 0)
                {
                    iteratedCrossoverBinaries.Add(new List<int>(crossoverBinary));
                    iteratedCrossoverBinaries[iteratedCrossoverBinaries.Count - 1].Add(0);
                }
                if (narrativeA.clues.FindAll(x => x.character == character).Count > 0)
                {
                    iteratedCrossoverBinaries.Add(new List<int>(crossoverBinary));
                    iteratedCrossoverBinaries[iteratedCrossoverBinaries.Count - 1].Add(1);
                }
            }

            crossoverBinaries = iteratedCrossoverBinaries;
        }
        crossoverBinaries = crossoverBinaries.FindAll(x => x.Contains(0) && x.Contains(1));

        // STEP 2: Create all non-trivial crossovers between narratives
        List<Narrative> choices = new List<Narrative>();
        foreach (List<int> crossoverBinary in crossoverBinaries)
        {
            choices.Add(new Narrative());
            for (int i = 0; i < characters.Count; i++)
            {
                if (crossoverBinary[i] == 0)
                {
                    int index = narrativeA.clues.FindIndex(x => x.character == characters[i]);
                    choices[choices.Count - 1].clues.Add(new Clue(narrativeA.clues[index]));
                }
                else
                {
                    int index = narrativeB.clues.FindIndex(x => x.character == characters[i]);
                    choices[choices.Count - 1].clues.Add(new Clue(narrativeB.clues[index]));
                }
            }
        }
        for (int i = choices.Count - 1; i >= 0; i--)
        {
            if (narrativeA.IsEqual(choices[i]) || narrativeB.IsEqual(choices[i]))
            {
                choices.RemoveAt(i);
                continue;
            }

            choices[i].data = cast.intermediaryData;
        }

        return new Narrative(ChooseNarrative(choices, narrativeA, cast.bohemians, random, false, period, true));
    }

    // CHOOSING
    public Narrative ChooseNarrative(List<Narrative> choices, Narrative narrative, int variations, bool incremental, bool random, long period, bool DEBUG = false)
    {
        // STEP 1: Filling in 'cheap' qualities of each choice
        for (int i = 0; i < choices.Count; i++)
        {
            choices[i].unknowns = cast.EvaluateUnknowns(choices[i].clues);
            choices[i].obviousness = cast.EvaluateObviousness(choices[i].clues);
        }

        // STEP 2: Reduce choices available based on 'level of optimality' of original narrative
        int maxChoices = (int)Math.Pow(2, cast.bohemians);
        choices = choices.FindAll(x => x.unknowns < narrative.unknowns || x.unknowns == 0);
        if (narrative.unknowns > 0)
        {
            if (incremental)
                choices = choices.OrderByDescending(x => x.unknowns).ThenBy(x => rng.Next()).ToList();
            else
                choices = choices.OrderBy(x => x.unknowns).ThenBy(x => rng.Next()).ToList();
        }
        else if (narrative.ambiguities > 0)
        {
            choices = choices.FindAll(x => x.unknowns < narrative.unknowns || x.unknowns == 0);

            // Reduce choices randomly, if necessary...
            if (choices.Count > maxChoices)
            {
                choices = choices.OrderBy(x => rng.Next()).ToList();
                choices = choices.FindAll(x => choices.IndexOf(x) < maxChoices);
            }

            if (random)
                choices = choices.OrderBy(x => rng.Next()).ToList();
            else if (incremental)
                choices = choices.OrderByDescending(x => x.unknowns).ThenBy(x => rng.Next()).ToList();
            else
                choices = choices.OrderBy(x => x.unknowns).ThenBy(x => rng.Next()).ToList();
        }
        else
        {
            choices = choices.FindAll(x => x.unknowns == 0 && x.obviousness < narrative.obviousness);

            if (random)
                choices = choices.OrderBy(x => rng.Next()).ToList();
            else if (incremental)
                choices = choices.OrderByDescending(x => x.obviousness).ThenBy(x => rng.Next()).ToList();
            else
                choices = choices.OrderBy(x => x.obviousness).ThenBy(x => rng.Next()).ToList();
        }

        if (choices.Count == 0)
            return narrative;

        // STEP 3: Calculate the remaining 'expensive quality of ambiguity...
        // ...returning the first choice that in any way improves on the original narrative!
        // If the program times out/otherwise doesn't return a choice, it will just return the original narrative...
        long init_ticks = DateTime.UtcNow.Ticks;

        int bestUnknowns = int.MaxValue;
        int bestAmbiguities = int.MaxValue;
        int bestObviousness = int.MaxValue;
        if (DEBUG)
            Console.Write("0/"+choices.Count+" choices analysed...");

        Narrative failsafe = narrative;
        for (int i = 0; i < choices.Count; i++)
        {
            // Finding all 'new' clues provided by choice[i]: the assumption here is that choices[i].data is all old data from the original narrative
            List<Dictionary<string, Dictionary<string, Element>>> dataUnion = new List<Dictionary<string, Dictionary<string, Element>>>();
            dataUnion.Add(choices[i].data);
            for (int j = Math.Clamp(choices[i].clues.Count-variations, 0, choices[i].clues.Count); j < choices[i].clues.Count; j++) 
                dataUnion.Add(choices[i].clues[j].clueElements);

            choices[i].data = Logic.ExtrapolateRelations(dataUnion);
            choices[i].ambiguities = cast.EvaluateAmbiguities(choices[i].data);

            if ((choices[i].unknowns < narrative.unknowns) || (choices[i].unknowns == 0 && choices[i].ambiguities < narrative.ambiguities) || (choices[i].unknowns == 0 && choices[i].ambiguities == 0 && choices[i].obviousness < narrative.obviousness))
            {
                Console.Write("\r"+new string(' ', Console.WindowWidth)+"\r");
                return choices[i];
            }

            // Failsafe handling
            if (choices[i].unknowns == 0 && choices[i].ambiguities == failsafe.ambiguities && choices[i].clues.FindAll(x => narrative.ContainsTemplate(x)).Count < failsafe.clues.FindAll(x => narrative.ContainsTemplate(x)).Count)
            {
                failsafe = choices[i];
            }

            if (DateTime.UtcNow.Ticks - init_ticks > period)
                break;

            // DEBUG:
            if ((choices[i].unknowns < bestUnknowns) || (bestUnknowns == 0 && choices[i].ambiguities < bestAmbiguities) || (bestUnknowns == 0 && bestAmbiguities == 0 && choices[i].obviousness < bestObviousness))
            {
                bestUnknowns = Math.Min(choices[i].unknowns, bestUnknowns);
                bestAmbiguities = Math.Min(choices[i].ambiguities, bestAmbiguities);
                bestObviousness = Math.Min(choices[i].obviousness, bestObviousness);
            }
            if (DEBUG)
                Console.Write("\r"+new string(' ', Console.WindowWidth)+"\r"+(i+1)+"/"+choices.Count+" choices analysed: "+bestUnknowns+"/"+narrative.unknowns+" unknowns, "+bestAmbiguities+"/"+narrative.ambiguities+" ambiguities, "+bestObviousness+"/"+narrative.obviousness+" obviousness...");

        }

        if (DEBUG)
            Console.Write("\r"+new string(' ', Console.WindowWidth)+"\r");

        return narrative;
    }

    // PRINTING
    public void PrintGeneration(int g)
    {
        for (int n = 0; n < population[g].Count; n++)
        {
            Console.WriteLine("GENERATION " + g + ", NARRATIVE " + (n + 1) + ": " + population[g][n].unknowns + "/" + cast.unknowns + " unknowns, " + population[g][n].ambiguities + "/" + cast.ambiguities + " ambiguities...");
            Console.WriteLine(" * This narrative has an obviousness of " + population[g][n].obviousness + "...");
            Console.WriteLine();
        }
    }

    // COUNTING REDUNDANT ITERATIONS

    public int RedundantGenerations(int g)
    {
        if (g < 0)
            return 0;

        population[g] = population[g].OrderBy(x => x.unknowns).ThenBy(x => x.ambiguities).ThenBy(x => x.obviousness).ToList();

        if (population[g][0].unknowns > 0)
        {
            for (int i = g-1; i >= 0; i--)
            {
                if (population[i][0].unknowns > population[g][0].unknowns)
                {
                    return Math.Max(g-i-2, 0);
                }
            }
        }
        else if (population[g][0].ambiguities > 0)
        {
            for (int i = g-1; i >= 0; i--)
            {
                if (population[i][0].ambiguities > population[g][0].ambiguities || population[i][0].unknowns > population[g][0].unknowns)
                {
                    return Math.Max(g-i-2,0);
                }
            }
        }
        else
        {
            for (int i = g-1; i >= 0; i--)
            {
                if (population[i][0].obviousness > population[g][0].obviousness || population[i][0].ambiguities > population[g][0].ambiguities || population[i][0].unknowns > population[g][0].unknowns)
                {
                    return Math.Max(g-i-2, 0);
                }
            }
        }

        return Math.Max(g-1, 0);
    }
}

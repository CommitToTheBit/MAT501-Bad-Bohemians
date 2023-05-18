/* Genetic Algorithm Processor: Reads in categories and elements as part of the initial simulation */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class SimulationProcessor
{
    public Dictionary<string, Dictionary<string, Element>> characterDescriptors;

    // Constructor
    public SimulationProcessor()
    {
        // DEBUG:
        long init_ticks = DateTime.UtcNow.Ticks;

        characterDescriptors = InitialiseDescriptors(Environment.CurrentDirectory.Split("bin")[0]+"Simulation/character_descriptor_data.json");

        // DEBUG:
        Console.WriteLine("It takes "+(((float)(DateTime.UtcNow.Ticks - init_ticks)) / Math.Pow(10, 7))+"s to initialise all elements.");
        Console.WriteLine();
    }

    public Dictionary<string, Dictionary<string, Element>> InitialiseDescriptors(string init_path)
    {
        Dictionary<string, Dictionary<string, Element>> elements = new Dictionary<string, Dictionary<string, Element>>();

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

        //List<string> exactCategories = new List<string>() { "Christian Name", "Family Name", "Domain" };
        foreach (string category in jsonData.Keys)
        {
            elements.Add(category, new Dictionary<string, Element>());
            foreach (string element in jsonData[category].Keys)
            {
                elements[category].Add(element, new Element(category, element, jsonData[category][element]));

                // NB: Hard-coding in uniqueness here... is there a more flexible option?
                //if (exactCategories.Contains(category))
                //    elements[category][element].relations.Add(new List<string>() { category, "has exactly one", element }); // FIXME: Maybe work this is retroactively, using our solutionData... a case of 'max. one, unless we say otherwise...'
            }
        }
        elements = Logic.ExtrapolateRelations(elements);

        return elements;
    }

    public Dictionary<string,Element> GetCharacterDescriptorCategory(string init_category)
    {
        Dictionary<string, Element> category = new Dictionary<string, Element>();
        if (characterDescriptors.ContainsKey(init_category))
            foreach (string element in characterDescriptors[init_category].Keys)
                category.Add(element, new Element(characterDescriptors[init_category][element]));

        return category;
    }

    public Dictionary<string,Dictionary<string,Element>> GetFixedElements()
    {
        // FIXME: Can make this more flexible by writing to character_descriptor_data.json...
        Dictionary<string, Dictionary<string, Element>> descriptors = new Dictionary<string, Dictionary<string, Element>>();

        List<string> givenCategories = new List<string>() { "Nobility", "Gender" };
        foreach (string category in givenCategories)
        {
            if (characterDescriptors.ContainsKey(category))
            {
                descriptors.Add(category, new Dictionary<string, Element>());

                foreach (string element in characterDescriptors[category].Keys)
                {
                    descriptors[category].Add(element, new Element(characterDescriptors[category][element]));
                }
            }
        }

        return descriptors;
    }

    public void PrintCharacterDescriptors()
    {
        foreach (string category in characterDescriptors.Keys)
        {
            Console.WriteLine("CATEGORY: " + category);
            Console.WriteLine();

            foreach (string element in characterDescriptors[category].Keys)
            {
                characterDescriptors[category][element].PrintElement(true, true);
            }
        }
    }

    public void PrintFixedElements()
    {
        Dictionary<string, Dictionary<string, Element>> givenDescriptors = GetFixedElements();
        foreach (string category in givenDescriptors.Keys)
        {
            Console.WriteLine("CATEGORY: " + category);
            Console.WriteLine();

            foreach (string element in characterDescriptors[category].Keys)
            {
                Console.WriteLine("ELEMENT: " + element);
            }

            Console.WriteLine();
        }
    }
}

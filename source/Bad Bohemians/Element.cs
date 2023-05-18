/* Element: Class representing a small unit of information used by the inference engine */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Element
{
    public string category;
    public string element;

    public List<List<string>> relations;
    public int minimum;
    public int maximum;
    public bool ambiguous;

    // Constructor
    public Element(string init_category, string init_element, List<List<string>> init_elementRelations, int init_minimum = 0, int init_maximum = int.MaxValue, bool init_ambiguous = false)
    {
        category = init_category;
        element = init_element;

        relations = new List<List<string>>();
        foreach (List<string> init_relation in init_elementRelations)
            relations.Add(new List<string>(init_relation));

        minimum = init_minimum;
        maximum = init_maximum;
        ambiguous = init_ambiguous;
    }

    // Copy constructor
    public Element(Element init_element)
    {
        category = init_element.category;
        element = init_element.element;

        relations = new List<List<string>>();
        foreach (List<string> init_relation in init_element.relations)
            relations.Add(new List<string>(init_relation));

        minimum = init_element.minimum;
        maximum = init_element.maximum;
        ambiguous = init_element.ambiguous;
    }

    // CHECKME: Anything this doesn't catch, something else in Logic will?
    public bool ContainsRelation(List<string> init_relation)
    {
        foreach (List<string> relation in relations)
        {
            if (!relation[0].Equals(init_relation[0]))
                continue;

            if (!relation[1].Equals(init_relation[1]))
                continue;

            if (!relation[2].Equals(init_relation[2]))
                continue;

            return true;
        }

        return false;
    }

    // Integrations
    public bool IntegrateRelation(List<string> init_relation)
    {
        // If init_minimum is not contained in our current relations, add it and return true
        // Otherwise, return false

        int index = relations.FindIndex(x => x[0].Equals(init_relation[0]) && x[1].Equals(init_relation[1]) && x[2].Equals(init_relation[2])); // FIXME: Shouldn't we intersect x[2]s?
        if (index < 0)
            return false;

        relations.Add(new List<string>(init_relation));
        return true;
    }

    public bool IntegrateMinimum(int init_minimum)
    {
        // If init_minimum is higher than our current minimum, raise minimum to init_minimum and return true
        // Otherwise, return false

        init_minimum = Math.Max(init_minimum, 0);
        if (init_minimum <= minimum)
            return false;

        minimum = init_minimum;
        return true;
    }

    public bool IntegrateMaximum(int init_maximum)
    {
        // If init_maximum is lower than our current minimum, lower maximum to init_maxximum and return true
        // Otherwise, return false

        init_maximum = Math.Max(init_maximum, 0); // FIXME: minimum, not 0?
        if (init_maximum >= maximum)
            return false;

        maximum = init_maximum;
        return true;
    }

    public bool IntegrateExhaustion()
    {
        // Safely exhausts elements with regards to Logic.



        /*bool exhaustion = false;
        foreach (string category in elements.Keys)
        {
            List<string> exhaustiveRelation = new List<string>() { category, "=>&", "" };
            if (!elements[categoryA][elementA].ContainsRelation(exhaustiveRelation))
            {
                elements[categoryA][elementA].relations.Add(exhaustiveRelation);
                extrapolation |= true;
            }
        }*/

        return true;
    }

    // Printing
    public void PrintElement(bool printMaxZeroes = false, bool printMinZeroes = false, bool printForwardImplications = true, bool printEquivalences = true)
    {
        List<string> forwardImplicationSigns = new List<string>() { "=>&", "=>|" };
        List<List<string>> forwardImplications = relations.FindAll(x => true || forwardImplicationSigns.Contains(x[1]));

        if (maximum == 0 && !printMaxZeroes && !ambiguous)
            return;

        if (minimum == 0 && !printMinZeroes && !ambiguous)
            return;

        // Printing Element counts...
        Console.Write("Element \"" + element + "\" appears ");
        if (minimum == maximum)
            Console.WriteLine("exactly " + maximum + " times...");
        else
            Console.WriteLine("from " + minimum + " to " + maximum + " times...");

        // Printing Element relations...
        if (printForwardImplications)
        {
            if (printMaxZeroes || forwardImplications.FindIndex(x => !x[2].Equals("")) >= 0)
            {
                Console.WriteLine("It implies the following...");

                foreach (List<string> forwardImplication in forwardImplications)
                {
                    string forwardImplicationCategory = forwardImplication[0].Substring(0, Math.Min(forwardImplication[0].Length, 16));
                    while (forwardImplicationCategory.Length < 16)
                        forwardImplicationCategory += " ";

                    Console.WriteLine(" * " + forwardImplicationCategory + "  " + forwardImplication[1] + "  " + forwardImplication[2]);
                }
            }

            // Sorting relations...
            //List<string> printingRelations
        }

        // Printing (unresolved) equivalences...
        if (printEquivalences)
        {
            Console.WriteLine("This element is " + ((ambiguous) ? "" : "not ") + "ambiguous...");


        }

        Console.WriteLine();
    }
}
/* Clue: Class containing the clue text provided to players, and a dictionary of the clue's raw data for use by the inferenc engine */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Clue
{
    public string template;
    public string clue;

    public Dictionary<string, Dictionary<string, Element>> clueElements;

    public int character;
    public int obviousness;

    // Constructor
    public Clue(string init_template, string init_clue, Dictionary<string, Dictionary<string, Element>> init_clueElements)
    {
        template = init_template;
        clue = init_clue;

        clueElements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_clueElements.Keys)
        {
            clueElements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_clueElements[category].Keys)
            {
                clueElements[category].Add(element, new Element(init_clueElements[category][element]));
            }
        }

        character = -1;
        obviousness = int.MaxValue;
    }

    // Copy constructor
    public Clue(Clue init_clue)
    {
        template = init_clue.template;
        clue = init_clue.clue;

        clueElements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_clue.clueElements.Keys)
        {
            clueElements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_clue.clueElements[category].Keys)
            {
                clueElements[category].Add(element, new Element(init_clue.clueElements[category][element]));
            }
        }

        character = init_clue.character;
        obviousness = init_clue.obviousness;
    }
}

public class Template
{
    public string template;

    public Dictionary<string,Dictionary<string,Element>> templateElements; // FIXME: '4 ={ 2/3' rubs up against issue of elimination? No - not if we *assign* 4 == 2, 4 == 3 as we go, then check for contradiction; ID is used as the 'first parse'
    // Consider... a "Man ={ 2/3" relation - again, all about non-uniqueness!

    // Constructor
    public Template(string init_template, Dictionary<string, Dictionary<string, Element>> init_templateElements)
    {
        template = init_template;

        templateElements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_templateElements.Keys)
        {
            templateElements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_templateElements[category].Keys)
            {
                templateElements[category].Add(element, new Element(init_templateElements[category][element]));
            }
        }
    }

    // Copy constructor
    public Template(Template init_template)
    {
        template = init_template.template;

        templateElements = new Dictionary<string, Dictionary<string, Element>>();
        foreach (string category in init_template.templateElements.Keys)
        {
            templateElements.Add(category, new Dictionary<string, Element>());
            foreach (string element in init_template.templateElements[category].Keys)
            {
                templateElements[category].Add(element, new Element(init_template.templateElements[category][element]));
            }
        }
    }
}


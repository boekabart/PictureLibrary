using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SmartMirror
{
    public class PicasaPerson
    {
        public string Name { get; set; }
        public string[] FocusIds { get; set; }

        private static readonly Dictionary<string, PicasaPerson> AllPersons;

        public static PicasaPerson TryGetPerson(string name)
        {
            if (AllPersons == null)
                return null;
            PicasaPerson retVal;
            if (AllPersons.TryGetValue(name, out retVal))
                return retVal;
            return null;
        }

        static PicasaPerson()
        {
            try
            {
                var doc = XDocument.Load(@"C:\Users\MediaCenter\AppData\Local\Google\Picasa2\contacts\contacts.xml");
                var persons = doc.Root.Elements("contact")
                    .Where(el => el.Attributes("name").Any())
                    .Select( el =>new PicasaPerson
                        {
                            Name = el.Attribute("name").Value,
                            FocusIds =
                                el.Attributes("id").Take(1).Concat(
                                el.Elements("subject").SelectMany(sel => sel.Attributes("id").Take(1))).Select(a => a.Value).ToArray()
                        }
                    );
                AllPersons = persons.GroupBy(p => p.Name).Select(MergePersons).ToDictionary(p => p.Name, p => p);
                foreach( var p in AllPersons.Values.OrderBy(p => p.Name))
                    Console.WriteLine("{0}\t{1}", p.Name, string.Join("|",p.FocusIds));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Couldn't load Picasa contacts db: {0}", e);
                throw;
            }
        }

        private static PicasaPerson MergePersons(IGrouping<string, PicasaPerson> picasaPersons)
        {
            return new PicasaPerson
            {
                Name = picasaPersons.Key,
                FocusIds = picasaPersons.SelectMany(p => p.FocusIds).Distinct().ToArray()
            };
        }
    }
}
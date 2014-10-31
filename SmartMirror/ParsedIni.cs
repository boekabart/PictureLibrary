using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SmartMirror
{
    public class ParsedIni
    {
        private Dictionary<string, Dictionary<string, string>> contents = new Dictionary<string, Dictionary<string, string>>(); 
        private String iniFilePath;

        /// <summary>
        /// Opens the INI file at the given path and enumerates the values in the ParsedIni.
        /// </summary>
        /// <param name="iniPath">Full path to INI file.</param>
        public ParsedIni(String iniPath)
        {
            iniFilePath = iniPath;

            if (!File.Exists(iniPath))
                throw new FileNotFoundException("Unable to locate " + iniPath);

            var lines = File.ReadAllLines(iniPath).Where(line => !string.IsNullOrWhiteSpace(line));
            var currentRoot = string.Empty;
            foreach (var strLine in lines.Select(l=>l.Trim()))
            {
                if (strLine.StartsWith("[") && strLine.EndsWith("]"))
                {
                    currentRoot = strLine.Substring(1, strLine.Length - 2).Trim();
                }
                else if (strLine.Contains("="))
                {
                    var keyPair = strLine.Split(new[] {'='}, 2);
                    var sectionDict = GetOrCreateSection(currentRoot);

                    var key = keyPair[0].Trim();
                    var value = keyPair[1].Trim();
                    if (sectionDict.ContainsKey(key))
                        Console.Error.WriteLine("Warning: Duplicate key {0} in section [{1}] of {2} (Previous value {3}, ignoring value {4})", key, currentRoot, iniPath, sectionDict[key], value);
                    else
                        sectionDict[key] = value;
                }
            }
        }

        /// <summary>
        /// Returns the value for the given section, key pair.
        /// </summary>
        /// <param name="sectionName">Section name.</param>
        /// <param name="settingName">Key name.</param>
        public String GetSetting(String sectionName, String settingName)
        {
            return contents[sectionName][settingName];
        }

        /// <summary>
        /// Returns the value for the given section, key pair.
        /// </summary>
        /// <param name="sectionName">Section name.</param>
        /// <param name="settingName">Key name.</param>
        public String TryGetSetting(String sectionName, String settingName)
        {
            try
            {
                return GetSetting(sectionName, settingName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Enumerates all lines for given section.
        /// </summary>
        /// <param name="sectionName">Section to enum.</param>
        public IEnumerable<string> EnumSection(String sectionName)
        {
            return !contents.ContainsKey(sectionName) ? new string[0] : contents[sectionName].Keys.AsEnumerable();
        }

        public IEnumerable<KeyValuePair<string,string>> EnumSectionPairs(String sectionName)
        {
            return !contents.ContainsKey(sectionName) ? new KeyValuePair<string,string>[0] : contents[sectionName].AsEnumerable();
        }

        /// <summary>
        /// Enumerates all sections
        /// </summary>
        public IEnumerable<string> EnumSections()
        {
            return contents.Keys;
        }

        /// <summary>
        /// Adds or replaces a setting to the table to be saved.
        /// </summary>
        /// <param name="sectionName">Section to add under.</param>
        /// <param name="settingName">Key name to add.</param>
        /// <param name="settingValue">Value of key.</param>
        public void AddSetting(String sectionName, String settingName, String settingValue)
        {
            var section = GetOrCreateSection(sectionName);
            section[settingName] = settingValue;
        }

        private Dictionary<string,string> GetOrCreateSection(string sectionName)
        {
            Dictionary<string, string> retval;
            if (contents.TryGetValue(sectionName, out retval)) return retval;
            retval = new Dictionary<string, string>();
            contents[sectionName] = retval;
            return retval;
        }

        /// <summary>
        /// Adds or replaces a setting to the table to be saved with a null value.
        /// </summary>
        /// <param name="sectionName">Section to add under.</param>
        /// <param name="settingName">Key name to add.</param>
        public void AddSetting(String sectionName, String settingName)
        {
            AddSetting(sectionName, settingName, null);
        }

        /// <summary>
        /// Remove a setting.
        /// </summary>
        /// <param name="sectionName">Section to add under.</param>
        /// <param name="settingName">Key name to add.</param>
        public void DeleteSetting(String sectionName, String settingName)
        {
            Dictionary<string, string> section;
            if (!contents.TryGetValue(sectionName, out section)) return;
            if (!section.ContainsKey(settingName))
                return;
            section.Remove(settingName);
            if (!section.Keys.Any())
                contents.Remove(sectionName);
        }
/*
        /// <summary>
        /// Save settings to new file.
        /// </summary>
        /// <param name="newFilePath">New file path.</param>
        public void SaveSettings(String newFilePath)
        {
            ArrayList sections = new ArrayList();
            String tmpValue = "";
            String strToSave = "";

            foreach (SectionPair sectionPair in keyPairs.Keys)
            {
                if (!sections.Contains(sectionPair.Section))
                    sections.Add(sectionPair.Section);
            }

            foreach (String section in sections)
            {
                strToSave += ("[" + section + "]\r\n");

                foreach (SectionPair sectionPair in keyPairs.Keys)
                {
                    if (sectionPair.Section == section)
                    {
                        tmpValue = (String)keyPairs[sectionPair];

                        if (tmpValue != null)
                            tmpValue = "=" + tmpValue;

                        strToSave += (sectionPair.Key + tmpValue + "\r\n");
                    }
                }

                strToSave += "\r\n";
            }

            try
            {
                TextWriter tw = new StreamWriter(newFilePath);
                tw.Write(strToSave);
                tw.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Save settings back to ini file.
        /// </summary>
        public void SaveSettings()
        {
            SaveSettings(iniFilePath);
        }*/
    }
}
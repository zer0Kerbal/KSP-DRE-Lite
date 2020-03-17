using System;
using System.Text.RegularExpressions;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;


using KSP.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI;
using KSP.UI.Screens;

namespace PDPN
{

	public class Tuple<T1, T2>
	{
		public T1 First { get; private set; }
		public T2 Second { get; set; }
		internal Tuple (T1 first, T2 second)
		{
			First = first;
			Second = second;
		}
	}

	public class Tuple<T1, T2, T3>
	{
		public T1 First { get; private set; }
		public T2 Second { get; private set; }
		public T3 Third { get; set; }
		internal Tuple (T1 first, T2 second, T3 third)
		{
			First = first;
			Second = second;
			Third = third;
		}
	}


	/*
	 * Layout of the templates list is as follows:
	 * KeyValuePair consisting of the key and the data for the key
	 * 		key:	name of the template
	 * 		data:	Tuple consisting of a string and a NameValueCollection
	 * 			string:					field name
	 * 			NameValueCollection:	Data items for the field, consisting of:
	 * 				First entry:	template definition (template, templdef)
	 * 				Following entries are in sets:
	 * 					First entry of set is the field definition from the file (the "values" entry in the file)
	 * 					Following entries are the individual fields values from the file
	 * 
	 * 
	 * 
	 */



	public class Configuration
	{
		public static Constants constants = new Constants();
		string template;
		public List<KeyValuePair<string, Tuple<string, NameValueCollection, bool>>> templates = new List<KeyValuePair<string, Tuple<string, NameValueCollection, bool>>>();

        public static bool NewTemplatesAreActive = true;
        public static string TemplateFile = "";


        public static Texture2D LoadPNG(string filePath) {

			Texture2D tex = null;
			byte[] fileData;

			if (System.IO.File.Exists(filePath))     {
				fileData = System.IO.File.ReadAllBytes(filePath);
				tex = new Texture2D(2, 2);
				tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
			}
			return tex;
		}

        static string SafeLoad(string value, bool oldvalue)
        {
            if (value == null)
                return oldvalue.ToString();
            return value;
        }
        static string SafeLoad(string value, string oldvalue)
        {
            if (value == null)
                return oldvalue.ToString();
            return value;
        }

        void SaveTemplateFile()
        {
            ConfigNode root = new ConfigNode();

            ConfigNode top = new ConfigNode(Constants.PDPN_NODENAME);
            root.SetNode(Constants.PDPN_NODENAME, top, true);
            root.Save(constants.PDPN_TEMPLATES_FILE);
        }

        public void LoadConfiguration()
        {
            ConfigNode configFile = null;
           

            Log.Info("LoadConfiguration: " + constants.PDPN_CFG_FILE);
            configFile = ConfigNode.Load(constants.PDPN_CFG_FILE);

            if (configFile != null)
            {
                ConfigNode node = configFile.GetNode("PersistentDynamicPodNames");
                if (node != null)
                {
                    NewTemplatesAreActive = bool.Parse(SafeLoad(node.GetValue("NewTemplatesAreActive"), NewTemplatesAreActive));
                    Log.Info("NewTemplatesAreActive: " + NewTemplatesAreActive.ToString());

                    constants.PDPN_TEMPLATES_FILE = SafeLoad(node.GetValue("TemplateFile"), Constants.PDPN_BASE_FOLDER + "PluginData/PDPN_Templates.cfg");
                    Log.Info("PDPN_TEMPLATES_FILE: " + constants.PDPN_TEMPLATES_FILE);
                }
            }
        } 

        public void LoadTemplates()
		{
			NameValueCollection pairs;

            ConfigNode configFile = null;
            ConfigNode configFileNode = null;
            ConfigNode templateNode = null;


            Log.Info("LoadTemplates: " + constants.PDPN_TEMPLATES_FILE);
            configFile = ConfigNode.Load (constants.PDPN_TEMPLATES_FILE);
			
			if (configFile == null) {
                SaveTemplateFile();
                // use default file

                configFile = ConfigNode.Load (constants.PDPN_DEFAULT_TEMPLATES_FILE);
			}
            else
            {
                Log.Info("PDPN_TEMPLATES_FILE found");
                configFileNode = configFile.GetNode(Constants.PDPN_NODENAME);
                if (configFileNode != null)
                {
                    string[] allTemplateNames = configFileNode.GetValues("templateName");
                    Log.Info("allTemplateNames count: " + allTemplateNames.Length.ToString());
                    if (allTemplateNames == null || allTemplateNames.Length == 0)
                    {
                        Log.Info("Loading default templates file");
                        configFile = ConfigNode.Load(constants.PDPN_DEFAULT_TEMPLATES_FILE);
                    }
                } else
                    configFile = ConfigNode.Load(constants.PDPN_DEFAULT_TEMPLATES_FILE);
            }


            // Log.Info ("PDPN_CFG_FILE: " + configFile.ToString ());
            configFileNode = configFile.GetNode (Constants.PDPN_NODENAME);
			if (configFileNode != null) {
					
//					templateName = configFileNode.GetValue ("templateName");
				string[] allTemplateNames = configFileNode.GetValues ("templateName");
                string templateName;
                for (int i = allTemplateNames.Length - 1; i >= 0; --i )
                {
                    templateName = allTemplateNames[i];

					if (templateName != null) {
//							Log.Info ("templateName: " + templateName);
						templateNode = configFileNode.GetNode (templateName);
						if (templateNode != null) {
							template = templateNode.GetValue ("template");
//								Log.Info ("Template name: " + templateName + " = " + template);
							// Parse the template, and get the values for the defined fields in the template

							string pattern = "";
						//	char prefix = ' ';
						//	char suffix = ' ';
//								Log.Info("new pairs");
							pairs = new NameValueCollection ();
							pairs.Add ("template", template);
							Log.Info ("pairs:  template=" + template);
							for (int p = 0; p < 3; p++) {
								switch (p) {
								case 0:
									pattern = "\\[.*?\\]";
						//			prefix = '[';
						//			suffix = ']';
									break;
								case 1:
									pattern = "<.*?>";
						//			prefix = '<';
						//			suffix = '>';
									break;

								case 2:
									pattern = "\\(.*?\\)";
						//			prefix = '(';
						//			suffix = ')';
									break;

								}
								Regex r = new Regex (pattern);

								Match m = r.Match (template);
								while (m.Success) {
									string id = m.Value.Substring (1, m.Value.Length - 2);

									ConfigNode valueNode = templateNode.GetNode (id);
//										Log.Info ("templateNode.GetValue(" + id.ToString () + ")");
									if (valueNode != null) {
										string values = valueNode.GetValue ("values");
										pairs.Add (id.ToString () + ":values", values);
//											Log.Info ("pairs " + id.ToString() + ":values=" + values);
										string[] valueAR = values.Split (' ');

                                        string s;
                                        for (int i1 = valueAR.Length - 1; i1 >= 0; --i1)
                                        {
                                            s = valueAR[i1];
											string s1 = valueNode.GetValue (s);

											if (s1 != "") {

												pairs.Add (id.ToString () + ":" + s, s1);
//													Log.Info ("pairs " + id.ToString() + ":" + s.ToString() + "=" + s1);
											}
										}
									}
									m = m.NextMatch ();
								}
							}
							
//								Log.Info ("new Tuple for pairs: " + templateName);
							var tuple = new Tuple<string, NameValueCollection, bool> (templateName, pairs, NewTemplatesAreActive);
							templates.Add (new KeyValuePair<string, Tuple<string, NameValueCollection, bool>> (templateName, tuple));
//								Log.Info ("Added:  " + templateName + "   tuple: " + tuple.First); // + " " + tuple.Second);

//								foreach (string s in tuple.Second.AllKeys) {
//  								Log.Info ("Key: " + s + "     Value: " + tuple.Second [s]);
//								}
						}
					}
				}
			}
			
		}
	}
}


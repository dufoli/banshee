using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

public class GSettingsSchemaExtractorProgram
{
    private static Dictionary<string, List<StringBuilder>> entries;
    private static int schema_count;

    public static void Main(string [] args)
    {
        if (args.Length != 1) {
            Console.Error.WriteLine ("Usage: gsettings-schema-extractor.exe /path/to/binaries/");
            Environment.Exit (1);
        }

        var dir = new DirectoryInfo (args [0]);
        if (!dir.Exists) {
            Console.Error.WriteLine (args [0] + " does not exist");
            Environment.Exit (1);
        }

        Extract (dir);
    }

    private static void Extract (DirectoryInfo dir)
    {
        var dot_net_assemblies = dir.EnumerateFiles ()
            .Where (f => f.FullName.EndsWith (".dll", true, System.Globalization.CultureInfo.InvariantCulture) ||
            f.FullName.EndsWith (".exe", true, System.Globalization.CultureInfo.InvariantCulture));

        if (!dot_net_assemblies.Any ()) {
            Console.Error.WriteLine ("No binary files found in specified path");
            Environment.Exit (1);
        }

        var types = new List<Type> ();
        foreach (var file in dot_net_assemblies) {
            Assembly asm = Assembly.LoadFrom (file.FullName);
            types.AddRange (asm.GetTypes ());
            Console.WriteLine ("Inspecting types in " + file.Name);
        }

        StringBuilder sbuilder = Extract (types);
        if (sbuilder != null) {

            string outputFile = Path.Combine (dir.FullName, "org.gnome.banshee.gschema.xml");

            using (StreamWriter writer = new StreamWriter (outputFile)) {
                writer.Write (sbuilder.ToString ());
            }

            Console.WriteLine ("Successfully wrote " + Path.GetFileName (outputFile));
        }
    }

    internal static StringBuilder Extract (IEnumerable<Type> types)
    {
        Console.WriteLine ("Generating schemas");
        schema_count = 0;
        entries = new Dictionary<string, List<StringBuilder>> ();

        foreach (Type type in types) {
            foreach (FieldInfo field in type.GetFields ()) {
                if (field.FieldType.IsGenericType &&
                    field.FieldType.GetGenericTypeDefinition ().Name.StartsWith ("SchemaEntry")) {

                    if (field.Name == "Zero") {
                        continue;
                    }

                    Console.WriteLine ("Found SchemaEntry: " + type.FullName + "." + field.Name);

                    object schema = field.GetValue (null);

                    var default_value = schema.GetType ().GetField ("DefaultValue");

                    AddSchemaEntry (
                        default_value.GetValue (schema),
                        default_value.FieldType,
                        GetString (schema, "Namespace"),
                        GetString (schema, "Key"),
                        GetString (schema, "ShortDescription"),
                        GetString (schema, "LongDescription")
                    );
                }
            }
        }

        if (schema_count > 0) {
            StringBuilder final = new StringBuilder ();
            final.Append ("<schemalist>\n");

            List<string> schemas = new List<string> (entries.Keys);
            schemas.Sort ();

            foreach (string id in schemas) {
                final.AppendFormat ("  <schema id=\"{0}\" path=\"{1}\" gettext-domain=\"banshee\">\n", id, GetPath (id));
                foreach (StringBuilder sb in entries [id]) {
                    final.Append (sb);
                }
                final.Append ("  </schema>\n");
            }

            final.Append ("</schemalist>\n");

            return final;
        }
        return null;
    }

    private static string GetString(object o, string name)
    {
        FieldInfo field = o.GetType ().GetField (name);
        return (string)field.GetValue (o);
    }

    private static string GetValueString (Type type, object o, out string gctype)
    {
        // gctypes to return taken from http://developer.gnome.org/glib/unstable/glib-GVariant.html#GVariantClass

        if (type == typeof (bool)) {
            gctype = "b";
            return o == null ? null : o.ToString ().ToLower ();
        } else if (type == typeof (int)) {
            gctype = "i";
        } else if (type == typeof (double)) {
            gctype = "d";
        } else if (type == typeof (string)) {
            gctype = "s";
            return o == null ? null : "'" + o.ToString () + "'";
        } else {
            throw new Exception("Unsupported type '" + type + "'");
        }

        return o == null ? null : o.ToString ();
    }

    private static void AddSchemaEntry (object defaultValue, Type defaultValueType,
                                        string namespce, string key,
                                        string summary, string description)
    {
        schema_count++;

        string id = CreateId (namespce);
        
        bool list = defaultValueType.IsArray;
        Type type = list ? Type.GetTypeArray ((object [])defaultValue) [0] : defaultValueType;
        string str_val = null;
        string str_type = null;
        
        if (list) {
            if (defaultValue == null || ((object[])defaultValue).Length == 0) {
                GetValueString (type, null, out str_type);
                str_val = "[]";
            } else {
                str_val = "[";
                object [] arr = (object [])defaultValue;
                for (int i = 0; i < arr.Length; i++) {
                    str_val += GetValueString (type, arr [i], out str_type).Replace (",", "\\,");
                    if (i < arr.Length - 1) {
                        str_val += ",";
                    }
                }
                str_val += "]";
            }
        } else {
            str_val = GetValueString (type, defaultValue, out str_type);
        }

        string type_attrib = str_type;
        if (list) {
            type_attrib = "a" + type_attrib;
        }

        StringBuilder builder = new StringBuilder ();
        builder.AppendFormat ("    <key name=\"{0}\" type=\"{1}\">\n", key, type_attrib);
        builder.AppendFormat ("      <default>{0}</default>\n", str_val);
        builder.AppendFormat ("      <_summary>{0}</_summary>\n", summary);
        builder.AppendFormat ("      <_description>{0}</_description>\n", description);
        builder.AppendFormat ("    </key>\n");
        if (entries.ContainsKey (id)) {
            entries [id].Add (builder);
        } else {
            entries [id] = new List<StringBuilder> { builder };
        }
    }
        
    private static string CamelCaseToUnderCase (string s)
    {
        string undercase = String.Empty;
        string [] tokens = Regex.Split (s, "([A-Z]{1}[a-z]+)");
        
        for(int i = 0; i < tokens.Length; i++) {
            if (tokens[i] == String.Empty) {
                continue;
            }

            undercase += tokens [i].ToLower();
            if (i < tokens.Length - 2) {
                undercase += "_";
            }
        }

        return undercase;
    }

    private static string CreateId (string namespce)
    {
        return "org.gnome.banshee." + CamelCaseToUnderCase (namespce);
    }

    private static string GetPath (string id)
    {
        return id.Replace ("org.gnome.", "/apps/").Replace (".", "/") + "/";
    }
}


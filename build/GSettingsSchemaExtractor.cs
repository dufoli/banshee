using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

public class GSettingsSchemaExtractorProgram
{
    private static Dictionary<string, StringBuilder> entries;
    private static int schema_count;

    public static void Main(string [] args)
    {
        if (args.Length != 2) {
            Console.Error.WriteLine ("Usage: gsettings-schema-extractor.exe Foo.dll bar.schema");
            Environment.Exit (1);
        }

        Extract (args [0], args [1]);
    }

    private static void Extract (string assemblyName, string outputFile)
    {
        Assembly asm = Assembly.LoadFrom (assemblyName);
        StringBuilder sbuilder = Extract (asm.GetTypes ());
        if (sbuilder != null) {
            using (StreamWriter writer = new StreamWriter (outputFile)) {
                writer.Write (sbuilder.ToString ());
            }
        }
    }

    internal static StringBuilder Extract (IEnumerable<Type> types)
    {
        schema_count = 0;
        entries = new Dictionary<string, StringBuilder> ();

        foreach (Type type in types) {
            foreach (FieldInfo field in type.GetFields ()) {
                if (field.FieldType.IsGenericType &&
                    field.FieldType.GetGenericTypeDefinition ().Name.StartsWith ("SchemaEntry")) {

                    if (field.Name == "Zero") {
                        continue;
                    }

                    object schema = field.GetValue(null);

                    AddSchemaEntry (schema.GetType ().GetField ("DefaultValue").GetValue (schema),
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

            List<string> keys = new List<string> (entries.Keys);
            keys.Sort ();

            foreach(string key in keys) {
                final.Append (entries [key]);
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

    private static void AddSchemaEntry (object value, string namespce, string key,
        string summary, string description)
    {
        schema_count++;

        string id = CreateId (namespce);
        string path = GetPath (id);
        
        bool list = value.GetType ().IsArray;
        Type type = list ? Type.GetTypeArray ((object [])value) [0] : value.GetType ();
        string str_val = null;
        string str_type = null;
        
        if (list) {
            if(value == null || ((object [])value).Length == 0) {
                GetValueString (type, null, out str_type);
                str_val = "[]";
            } else {
                str_val = "[";
                object [] arr = (object [])value;
                for(int i = 0; i < arr.Length; i++) {
                    str_val += GetValueString (type, arr [i], out str_type).Replace (",", "\\,");
                    if(i < arr.Length - 1) {
                        str_val += ",";
                    }
                }
                str_val += "]";
            }
        } else {
            str_val = GetValueString (type, value, out str_type);
        }

        string type_attrib = str_type;
        if (list)
            type_attrib = "a" + type_attrib;

        StringBuilder builder = new StringBuilder ();
        builder.AppendFormat ("  <schema id=\"{0}\" path=\"{1}\">\n", id, path);
        builder.AppendFormat ("    <key name=\"{0}\" type=\"{1}\">\n", key, type_attrib);
        builder.AppendFormat ("      <default>{0}</default>\n", str_val);
        builder.AppendFormat ("      <_summary>{0}</_summary>\n", summary);
        builder.AppendFormat ("      <_description>{0}</_description>\n", description);
        builder.AppendFormat ("    </key>\n");
        builder.AppendFormat ("  </schema>\n");
        entries.Add (id + key, builder);
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


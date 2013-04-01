using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;

public class GSettingsSchemaExtractorProgram
{
    private static HashSet<FieldInfo> schema_fields;
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
        schema_fields = new HashSet<FieldInfo> ();

        foreach (Type type in types) {
            foreach (FieldInfo field in type.GetFields (BindingFlags.Public |
                                                        BindingFlags.NonPublic |
                                                        BindingFlags.Static)) {
                if (CheckForValidEntry (type, field)) {
                    schema_fields.Add (field);
                }
            }
        }

        if (schema_fields.Count > 0) {

            var schemaSet = new SchemaSet ();

            foreach (FieldInfo field in schema_fields) {
                GSettingsKey.ExtractFromField (schemaSet, field);
            }

            StringBuilder final = new StringBuilder ();
            final.Append ("<schemalist>\n");

            foreach (GSettingsSchema schema in schemaSet.Schemas.Values) {
                final.Append (schema.ToString ());
            }

            final.Append ("</schemalist>\n");

            return final;
        }
        return null;
    }

    internal class InvalidSchemaException : Exception
    {
        internal InvalidSchemaException (string msg) : base (msg)
        {
        }
    }

    static bool CheckForValidEntry (Type type, FieldInfo field)
    {
        if (!field.FieldType.IsGenericType || field.Name == "Zero") {
            return false;
        }

        string type_definition = field.FieldType.GetGenericTypeDefinition ().Name;

        if (type_definition.StartsWith ("SchemaEntry")) {
            Console.WriteLine ("Found SchemaEntry: " + type.FullName + "." + field.Name);

            return true;
        }

        return false;
    }

    private static string GetStringValueOfFieldNamed (object schemaEntryObject, string fieldName)
    {
        FieldInfo field = schemaEntryObject.GetType ().GetField (fieldName);
        return (string)field.GetValue (schemaEntryObject);
    }

    private static string GetValueString (Type type, object o)
    {
        if (type == typeof (bool)) {
            return o == null ? null : o.ToString ().ToLower ();
        }

        if (type == typeof (string)) {
            string value = o == null ? String.Empty : o.ToString ();
            return String.Format ("'{0}'", value);
        }

        if (type == typeof (int) || type == typeof (double)) {
            return o == null ? null : o.ToString ();
        }

        throw new Exception (String.Format ("Unsupported type '{0}'", type));
    }

    private static string GetGcType (Type type)
    {
        if (type == null) {
            throw new ArgumentNullException ("type");
        }

        // gctypes to return taken from http://developer.gnome.org/glib/unstable/glib-GVariant.html#GVariantClass

        if (type == typeof (bool)) {
            return "b";
        }

        if (type == typeof (int)) {
            return "i";
        }

        if (type == typeof (double)) {
            return "d";
        }

        if (type == typeof (string)) {
            return "s";
        }

        throw new Exception (String.Format ("Unsupported type '{0}'", type));
    }

    internal struct GSettingsSchema
    {
        internal string Id { get; private set; }
        internal string Path { get { return Id.Replace ("org.gnome.", "/apps/").Replace (".", "/") + "/"; } }

        internal HashSet<GSettingsKey> Keys { get; private set; }

        internal GSettingsSchema (string id) : this ()
        {
            Id = id;
            Keys = new HashSet<GSettingsKey> ();
        }

        public override string ToString ()
        {
            string result = String.Empty;
            result += String.Format ("  <schema id=\"{0}\" path=\"{1}\" gettext-domain=\"banshee\">\n", Id, Path);
            foreach (var key in Keys) {
                result += key.ToString ();
            }
            result += ("  </schema>\n");
            return result;
        }

    }

    internal struct GSettingsKey
    {
        internal GSettingsSchema ParentSchema { get; private set; }
        internal string Default { get; private set; }
        internal string KeyName { get; private set; }
        internal string KeyType { get; private set; }
        internal string Summary { get; private set; }
        internal string Description { get; private set; }

        internal static GSettingsKey ExtractFromField (SchemaSet schemaSet, FieldInfo field)
        {
            if (field == null) {
                throw new ArgumentNullException ("field");
            }

            object schema = field.GetValue (null);
            if (schema == null) {
                throw new InvalidSchemaException (String.Format (
                    "Schema could not be retrieved from field {0} in type {1}",
                    field.Name, field.DeclaringType.FullName));
            }

            var default_value_field = schema.GetType ().GetField ("DefaultValue");
            var default_value = default_value_field.GetValue (schema);

            var default_value_type = default_value_field.FieldType;
            var namespce = GetStringValueOfFieldNamed (schema, "Namespace");
            var key = GetStringValueOfFieldNamed (schema, "Key");
            var short_description = GetStringValueOfFieldNamed (schema, "ShortDescription");
            var long_description = GetStringValueOfFieldNamed (schema, "LongDescription");

            return new GSettingsKey (schemaSet, default_value, default_value_type, namespce, key, short_description, long_description);
        }

        private GSettingsKey (SchemaSet schemaSet, object defaultValue, Type defaultValueType,
                              string namespce, string key,
                              string short_desc, string long_desc) : this ()
        {
            ParentSchema = schemaSet.RetrieveOrCreate (namespce);

            Default = GetDefault (defaultValue, defaultValueType);
            KeyName = key.Replace ("_", "-");
            KeyType = GetTypeAttrib (defaultValue, defaultValueType);
            Summary = short_desc;
            Description = long_desc;

            ParentSchema.Keys.Add (this);
        }

        public override string ToString ()
        {
            string result = String.Empty;
            result += String.Format ("    <key name=\"{0}\" type=\"{1}\">\n", KeyName, KeyType);
            result += String.Format ("      <default>{0}</default>\n", Default);
            result += String.Format ("      <summary>{0}</summary>\n", Summary);
            result += String.Format ("      <description>{0}</description>\n", Description);
            result +=               ("    </key>\n");
            return result;
        }
    }

    internal class SchemaSet
    {
        internal SchemaSet ()
        {
            Schemas = new Dictionary<string, GSettingsSchema> ();
        }

        internal Dictionary<string, GSettingsSchema> Schemas { get; private set; }

        private static string NamespaceToId (string namespce)
        {
            return "org.gnome.banshee." + CamelCaseToUnderScoreLowerCase (namespce);
        }

        internal GSettingsSchema RetrieveOrCreate (string namespce)
        {
            string id = NamespaceToId (namespce);
            GSettingsSchema schema;
            if (!Schemas.TryGetValue (id, out schema)) {
                schema = new GSettingsSchema (id);
                Schemas [id] = schema;
            }
            return schema;
        }
    }

    internal static string GetTypeAttrib (object defaultValue, Type defaultValueType)
    {
        var i_enumerable_interface = defaultValueType.GetInterfaces ()
            .Where (i => i.IsGenericType && i.GetGenericTypeDefinition () == typeof (IEnumerable<>)).FirstOrDefault ();
        bool list = defaultValueType.IsArray;
        if (list) {
            var inner_type = i_enumerable_interface.GetGenericArguments () [0];
            return "a" + GetGcType (inner_type);
        }
        return GetGcType (defaultValueType);
    }

    internal static string GetDefault (object defaultValue, Type defaultValueType)
    {
        bool list = defaultValueType.IsArray;

        string str_val = null;

        if (list) {
            if (defaultValue == null || ((object[])defaultValue).Length == 0) {
                str_val = "[]";
            } else {
                var type = Type.GetTypeArray ((object [])defaultValue) [0];
                str_val = "[";
                object [] arr = (object [])defaultValue;
                for (int i = 0; i < arr.Length; i++) {
                    str_val += GetValueString (type, arr [i]).Replace (",", "\\,");
                    if (i < arr.Length - 1) {
                        str_val += ",";
                    }
                }
                str_val += "]";
            }
        } else {
            str_val = GetValueString (defaultValueType, defaultValue);
        }
        return str_val;
    }

    private static string CamelCaseToUnderScoreLowerCase (string s)
    {
        string undercase = String.Empty;
        string [] tokens = Regex.Split (s, "([A-Z]{1}[a-z]+)");
        
        for(int i = 0; i < tokens.Length; i++) {
            if (tokens [i] == String.Empty) {
                continue;
            }

            undercase += tokens [i].ToLower();
            if (i < tokens.Length - 2) {
                undercase += "_";
            }
        }

        return undercase;
    }
}


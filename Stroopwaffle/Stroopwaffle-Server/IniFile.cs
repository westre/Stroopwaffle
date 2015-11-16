#region license
// Copyleft (-) 2013 Mikael Lyngvig (mikael@lyngvig.org).  Donated to the Public Domain.
#endregion

/** \file
 *  Simple .INI file parser, which is very strict about enforcing a reasonable and sensible set of syntax rules.
 *
 *  A handy feature of this .INI file parser is that it can be given a strict syntax that it checks the input against.  The
 *  syntax is simply another .INI file, which contains all the sections and fields that are to be allowed and to be present in the
 *  parsed .INI file.  Each field's value is a plain .NET regular expression, which is used to check the validity of the input.
 */

using System.Collections.Generic;       // Dictionary<T1, T2>

namespace Stroopwaffle_Server {
    /** Convenient short-hand. */
    public class StringTable : Dictionary<string, string> {
    }

    /** The exception that is thrown in case this module detects or finds an error. */
    public class IniFileError : System.Exception {
        /** The line number inside the offending file (may be zero, in which case it should be ignored). */
        private int _line;
        public int Line {
            get { return _line; }
        }

        /** Constructor for the \c IniFileError class. */
        public IniFileError(string message) :
            base(message) {
            _line = 0;
        }

        /** Alternate constructor for the \c IniFileError class. */
        public IniFileError(int line, string message) :
            base(message) {
            _line = line;
        }
    }

    /** The \c Parser class, which CURRENTLY parses a Windows-style INI file from disk. */
    public static class Parser {
        /**
         * Trims all whitespace in the string:
         * Leading and trailing whitespace is removed and inner sequences of whitespaces are replaced by a single space character.
         *
         * Examples:
         *     ""            => ""
         *     "abc"         => "abc"
         *     "a b c"       => "a b c"
         *     "  a  b  c  " => "a b c"
         */
        public static string TrimAll(string value) {
            var result = new System.Text.StringBuilder(value.Length);

            bool space = false;
            foreach (char ch in value) {
                // record that we encountered a whitespace character and then continue
                if (char.IsWhiteSpace(ch)) {
                    space = true;
                    continue;
                }

                // only append a space if following a non-space character
                if (space && result.Length > 0)
                    result.Append(' ');
                space = false;

                // finally: append the real, non-whitespace character
                result.Append(ch);
            }
            // note: silently discard trailing spaces by not appending a space representing them

            return result.ToString();
        }

        public static Dictionary<string, string> Parse(string[] lines, bool caseless, string[] syntax = null) {
            Dictionary<string, string> result = new Dictionary<string, string>();

            Dictionary<string, string> checks = null;
            if (syntax != null)
                checks = Parse(syntax, caseless);

            string section = "";        // "" equals the global, unnamed section
            result[section] = "";       // The global section always exist, even if the file is empty.

            int index = 0;         // the current line number
            foreach (string item in lines) {
                string line = item;

                index += 1;

                // strip trailing comment, if any
                int comment = line.IndexOf(';');
                if (comment == 0)
                    line = "";
                else if (comment != -1)
                    line = line.Substring(0, comment - 1);

                // strip trailing whitespace, if any
                line = line.TrimEnd();

                // if an empty line
                if (line.Length == 0)
                    continue;

                // reject lines containing tabs; they are mostly bad news for everybody
                if (line.IndexOf('\t') != -1)
                    throw new IniFileError(index, "Tab detected in line");

                // if the start of a new section ([name])
                if (line[0] == '[') {
                    if (line[line.Length - 1] != ']')
                        throw new IniFileError(index, "Section begin marker ([) without section end marker (])");

                    line = line.Substring(1, line.Length - 2);
                    if (line.Trim() != line)
                        throw new IniFileError(index, "Leading or following space detected in section name");
                    if (TrimAll(line) != line)
                        throw new IniFileError(index, "Multiple embedded spaces detected in section name");

                    // enforce case insensitive by converting the section name to lowercase
                    if (caseless)
                        line = line.ToLowerInvariant();

                    // If the section has already been defined elsewhere, report error.
                    if (result.ContainsKey(line))
                        throw new IniFileError(index, "Section '" + line + "' already defined");

                    // Check that the section actually exists in the syntax, if supplied.
                    if (checks != null && !checks.ContainsKey(line))
                        throw new IniFileError(index, "Unknown section: " + line);

                    // we can now safely switch to the new section
                    section = line;

                    result[section] = "";

                    continue;
                }

                // a section field assignment (name = data)

                // ... reject lines starting with whitespace(s)
                if (line.TrimStart() != line)
                    throw new IniFileError(index, "Value begins with whitespace");

                // reject lines not containing an equal sign (=)
                int pos = line.IndexOf('=');
                if (pos == -1)
                    throw new IniFileError(index, "Value definition without equal sign (=)");
                if (pos == 0)
                    throw new IniFileError(index, "Value name missing before equal sign (=)");

                // separate out the name and data parts of the line
                string name = line.Substring(0, pos).Trim();
                string data = line.Substring(pos + 1).Trim();
                if (TrimAll(name) != name)
                    throw new IniFileError(index, "Multiple embedded spaces detected in value name");

                // enforce case insentivity by converting the field name to lowercase
                if (caseless)
                    name = name.ToLowerInvariant();

                if (result.ContainsKey(section + "." + name))
                    throw new IniFileError(index, "Value '" + name + "' already defined in section");

                // handle quoted data
                if (data.Length >= 1 && data[0] == '\"') {
                    if (data.Length < 2 || data[data.Length - 1] != '\"')
                        throw new IniFileError(index, "Value '" + name + "' value incorrectly quoted");
                    data = data.Substring(1, data.Length - 2);
                }

                string path = (section == "") ? name : section + "." + name;

                // Validate the result if a syntax has been specified.
                if (checks != null) {
                    // Check if the value is defined at all.
                    if (!checks.ContainsKey(path))
                        throw new IniFileError(index, "Unknown value '" + section + "." + name + "' encountered");

                    // Check if the data matches the specified regular expression.
                    if (!System.Text.RegularExpressions.Regex.IsMatch(data, checks[path]))
                        throw new IniFileError(index, "Syntax error in value '" + name + "'");
                }

                // and finally update the database
                result[path] = data;
            }

            // If a syntax is given, check that all defined values have been specified.
            if (checks != null) {
                foreach (string rule in checks.Keys) {
                    if (!result.ContainsKey(rule))
                        throw new IniFileError(0, "Missing value in input: " + rule);
                }
            }

            return result;
        }

        public static Dictionary<string, string> Parse(string filename, bool caseless, string[] syntax = null) {
            string[] lines = System.IO.File.ReadAllLines(filename);
            return Parse(lines, caseless, syntax);
        }

    }
}


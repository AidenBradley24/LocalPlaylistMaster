using System.Collections;
using System.Data.SQLite;
using System.Text;

namespace LocalPlaylistMaster.Backend
{
    /*
        QUERYABLES:
        id -- implicit                  int
        remote                          int
        name                            string
        artists                         string
        album                           string
        description                     string
        rating                          int
        time                            int

        OPERATORS:
        =   equals
        !=  not equals
        ^   starts with                 string only
        !^  does not start with         string only
        $   ends with                   string only
        !$  does not end with           string only
        *   contains                    string only
        !*  does not contain            string only
        <   less than                   int only
        >   greater than                int only
        <=  less than or equal to       int only
        >=  greater than or equal to    int only
        :   is between                  int only
        -   between and                 int only
        &   and (another term)

        commas act as a seperate section which are ored together    
        quotes inside values are parsed as strings, \" and \\ can be used
        everything is case insensitive
        whitespace is trimmed

     */

    /// <summary>
    /// Wrapper for a user query on track table
    /// </summary>
    public sealed class UserQuery
    {
        private string? sql;
        private string? queryText;
        private bool valid = false;
        private IEnumerable<SQLiteParameter>? parameters;

        public string? GetSQL()
        {
            return sql;
        }

        public IEnumerable<SQLiteParameter>? Parameters()
        {
            return parameters;
        }

        private static readonly HashSet<string> intQueries = new()
        {
            "id",
            "remote",
            "rating",
            "time"
        };

        private static readonly HashSet<string> stringQueries = new()
        {
            "name",
            "artists",
            "album",
            "description",
        };

        private enum ParseMode { quer, int_val, str_val, int_oper, str_oper, id, between_start, between_end }

        public void Parse(string query) // TODO add parenthesis
        {
            valid = false;
            queryText = query.ToLowerInvariant();
            IEnumerable<string> sections = SmartSplit(query); // TODO also make sure commas can be in strings
            List<SQLiteParameter> parameters = [];
            List<string> finalSections = [];

            // Checking for operators and filling in terms
            foreach (string section in sections)
            {
                List<object> tokens = [];
                string current = "";
                ParseMode mode = ParseMode.quer;
                for (int i = 0; i < section.Length; i++)
                {
                    char c = section[i];
                    if (char.IsWhiteSpace(c)) continue;
                    char? n = i < section.Length - 1 ? section[i + 1] : null;

                    if(mode == ParseMode.str_val)
                    {
                        // self contained until exit string with quotes
                        if(c != '"') throw new InvalidUserQueryException("strings must begin and end with quotes");
                        StringBuilder b = new();
                        while(i < section.Length)
                        {
                            c = section[++i];
                            if (c == '"') break;
                            if (i == section.Length - 2 && section[i + 1] != '"')
                                throw new InvalidUserQueryException("string was not terminated");

                            if (c == '\\')
                            {
                                if (i >= section.Length - 2)
                                    throw new InvalidUserQueryException("string was not terminated due to an escape character \\\"");

                                b.Append(section[++i] switch
                                {
                                    '"' => '"',
                                    '\\' => '\\',
                                    'n' => '\n',
                                    't' => '\t',
                                    _ => throw new InvalidUserQueryException("invalid escape character")
                                });
                                continue;
                            }
                            b.Append(c);
                        }
                        tokens.Add(b.ToString());
                        mode = ParseMode.quer;
                        continue;
                    }

                    if(mode == ParseMode.int_val)
                    {
                        // self contained until no longer digit
                        int start = i;
                        if (c == '-') i++; // negative number
                        while (i < section.Length && char.IsDigit(c)) c = section[i++];
                        if (start == i) throw new InvalidUserQueryException("expected number was not provided");
                        tokens.Add(int.Parse(section[start..i]));
                        mode = ParseMode.quer;
                        continue;
                    }

                    if (mode == ParseMode.quer && char.IsDigit(c))
                    {
                        mode = ParseMode.id;
                        tokens.Add("id");
                        current += c;
                        continue;
                    }

                    if (mode == ParseMode.id)
                    {
                        if (char.IsDigit(c))
                        {
                            current += c;
                        }
                        else if(c == '-')
                        {
                            tokens.Add(":");
                            tokens.Add(current);
                            current = "";
                            mode = ParseMode.between_end;
                        }
                        else
                        {
                            throw new InvalidUserQueryException("Only digits and `-` are allowed with implicit ids");
                        }
                        continue;
                    }

                    void StartOperator()
                    {
                        if (intQueries.Contains(current))
                        {
                            mode = ParseMode.int_oper;
                        }
                        else if (stringQueries.Contains(current))
                        {
                            mode = ParseMode.str_oper;
                        }
                        else
                        {
                            throw new InvalidUserQueryException($"`{current}` is not queryable");
                        }

                        tokens.Add(current);
                        current = "";
                    }

                    switch (c)
                    {
                        case '=':
                            StartOperator();
                            tokens.Add("=");
                            mode = mode switch 
                            { 
                                ParseMode.int_oper => ParseMode.int_val,
                                ParseMode.str_oper => ParseMode.str_val,
                                _ => throw new Exception()
                            };
                            continue;
                        case '^':
                        case '$':
                        case '*':
                            StartOperator();
                            if (mode != ParseMode.str_oper)
                                throw new InvalidUserQueryException($"`{c}` operator is string-only");
                            tokens.Add(c.ToString());
                            mode = ParseMode.str_val;
                            continue;
                        case '!':
                            StartOperator();
                            i++;
                            if (n == '=')
                            {
                                tokens.Add("!=");
                                mode = mode switch
                                {
                                    ParseMode.int_oper => ParseMode.int_val,
                                    ParseMode.str_oper => ParseMode.str_val,
                                    _ => throw new Exception()
                                };
                            }
                            else if(n == '^' || n == '$' || n == '*')
                            {
                                if (mode != ParseMode.str_oper)
                                    throw new InvalidUserQueryException($"`!{n}` operator is string-only");
                                tokens.Add($"!{n}");
                                mode = ParseMode.str_val;
                            }
                            else
                            {
                                throw new InvalidUserQueryException("Invalid operator: `!`");
                            }
                            continue;
                        case '<':
                        case '>':
                            StartOperator();
                            string add = c.ToString();
                            if (n == '=') 
                            {
                                add += "=";
                                i++;
                            } 
                            if(mode != ParseMode.int_oper)
                            {
                                throw new InvalidUserQueryException($"`{add}` operator is int-only");
                            }
                            tokens.Add(add);
                            mode = ParseMode.int_val;
                            continue;
                        case ':':
                            StartOperator();
                            if (mode != ParseMode.int_oper)
                            {
                                throw new InvalidUserQueryException($"`:` operator is int-only");
                            }
                            mode = ParseMode.between_start;
                            tokens.Add(":");
                            continue;
                        case '-':
                            tokens.Add(current);
                            current = "";
                            if (mode != ParseMode.between_start)
                            {
                                throw new InvalidUserQueryException("`-` operator is not used correctly");
                            }
                            mode = ParseMode.between_end;
                            continue;
                        case '&':
                            tokens.Add(current);
                            current = "";
                            if(mode == ParseMode.int_val || mode == ParseMode.str_val || mode == ParseMode.between_end)
                            {
                                tokens.Add("&");
                                mode = ParseMode.quer;
                                continue;
                            }
                            throw new InvalidUserQueryException("`&` operator is not used correctly");
                        default:
                            current += c;
                            continue;
                    }
                }

                if(mode == ParseMode.id)
                {
                    tokens.Add("=");
                    tokens.Add(current);
                }
                else if (mode == ParseMode.between_start)
                {
                    throw new InvalidUserQueryException("Incomplete range using `:` operator");
                }
                else if (!string.IsNullOrWhiteSpace(current))
                {
                    tokens.Add(current);
                }

                finalSections.Add(ParseTokens(tokens, parameters));
            }

            sql = string.Join(" OR ", finalSections);
            this.parameters = parameters;
        }


        private static string ParseTokens(List<object> tokens, List<SQLiteParameter> parameters)
        {
            // assumes no syntax errors

            StringBuilder b = new();
            ParseMode mode = ParseMode.quer;
            string? queryable = null;
            SQLiteParameter? param = null;
            for (int i = 0; i < tokens.Count; i++)
            {
                object current = tokens[i];
                object? next = i < tokens.Count - 1 ? tokens[i + 1] : null;
                
                if(mode == ParseMode.quer)
                {
                    if (intQueries.Contains(current))
                    {
                        mode = ParseMode.int_oper;
                    }
                    else if (stringQueries.Contains(current))
                    {
                        mode = ParseMode.str_oper;
                    }
                    else
                    {
                        throw new InvalidUserQueryException($"`{current}` is not queryable");
                    }

                    (queryable, param) = GetParam((string)current, parameters);
                    continue;
                }

                string WrapParam(object? value, bool escape = false)
                {
                    if (value == null) throw new Exception("value can't be null");
                    if (param == null) throw new Exception("param can't be null");

                    switch(mode)
                    {
                        case ParseMode.int_oper:
                        case ParseMode.between_start:
                            param.Value = Convert.ToInt32(value);
                            break;
                        case ParseMode.str_oper:
                            // create escape chars for sqlite
                            string str = value as string ?? "";
                            if (escape)
                            {
                                str = str.Replace("\\", "\\\\");
                                str = str.Replace("%", "\\%");
                                str = str.Replace("-", "\\-");
                            }
                            param.Value = str;
                            break;
                    }
                    
                    return param.ParameterName;
                }

                if(mode == ParseMode.int_oper || mode == ParseMode.str_oper)
                {
                    string caseInsensitive = mode == ParseMode.str_oper ? " COLLATE NOCASE" : "";
                    switch (current)
                    {
                        case "=":
                            b.Append(queryable);
                            b.Append(" = ");
                            b.Append(WrapParam(next));
                            b.Append(caseInsensitive);
                            break;
                        case "!=":
                            b.Append("(NOT ");
                            b.Append(queryable);
                            b.Append(" = ");
                            b.Append(WrapParam(next));
                            b.Append(caseInsensitive);
                            b.Append(')');
                            break;
                        case "^":
                            b.Append(queryable);
                            b.Append(" LIKE ");
                            b.Append(WrapParam(next, true));
                            if (param == null) throw new Exception();
                            param.Value = $"{param.Value}%";
                            b.Append(" ESCAPE '\\'");
                            b.Append(caseInsensitive);
                            break;
                        case "!^":
                            b.Append("(NOT ");
                            b.Append(queryable);
                            b.Append(" LIKE ");
                            b.Append(WrapParam(next, true));
                            if (param == null) throw new Exception();
                            param.Value = $"{param.Value}%";
                            b.Append(" ESCAPE '\\'");
                            b.Append(caseInsensitive);
                            b.Append(')');
                            break;
                        case "$":
                            b.Append(queryable);
                            b.Append(" LIKE ");
                            b.Append(WrapParam(next, true));
                            if (param == null) throw new Exception();
                            param.Value = $"%{param.Value}";
                            b.Append(" ESCAPE '\\'");
                            b.Append(caseInsensitive);
                            break;
                        case "!$":
                            b.Append("(NOT ");
                            b.Append(queryable);
                            b.Append(" LIKE ");
                            b.Append(WrapParam(next, true));
                            if (param == null) throw new Exception();
                            param.Value = $"%{param.Value}";
                            b.Append(" ESCAPE '\\'");
                            b.Append(caseInsensitive);
                            b.Append(')');
                            break;
                        case "*":
                            b.Append(queryable);
                            b.Append(" LIKE ");
                            b.Append(WrapParam(next, true));
                            if (param == null) throw new Exception();
                            param.Value = $"%{param.Value}%";
                            b.Append(" ESCAPE '\\'");
                            b.Append(caseInsensitive);
                            break;
                        case "!*":
                            b.Append("(NOT ");
                            b.Append(queryable);
                            b.Append(" LIKE ");
                            b.Append(WrapParam(next, true));
                            if (param == null) throw new Exception();
                            param.Value = $"%{param.Value}%";
                            b.Append(" ESCAPE '\\')");
                            b.Append(caseInsensitive);
                            break;
                        case "<":
                        case ">":
                        case "<=":
                        case ">=":
                            b.Append(queryable);
                            b.Append($" {current} ");
                            b.Append(WrapParam(next));
                            break;
                        case ":":
                            mode = ParseMode.between_start;
                            continue;
                        case "&":
                            mode = ParseMode.quer;
                            b.Append(" AND ");
                            continue;
                    }

                    mode = ParseMode.quer;
                    i++; // next already processed
                }

                if (mode == ParseMode.between_start)
                {
                    b.Append('(');
                    b.Append(queryable);
                    b.Append(" BETWEEN ");
                    b.Append(WrapParam(current));
                    b.Append(" AND ");
                    (queryable, param) = GetParam((string)tokens[i - 2], parameters);
                    b.Append(WrapParam(next));
                    b.Append(')');
                    i++;
                    mode = ParseMode.quer;
                }
            }

            return b.ToString();
        }

        private static (string name, SQLiteParameter param) GetParam(string query, List<SQLiteParameter> parameters)
        {
            SQLiteParameter parameter = new($"@p{parameters.Count}");
            string name = query switch
            {
                "id" => "Id",
                "remote" => "Remote",
                "name" => "Name",
                "artists" => "Artists",
                "album" => "Album",
                "description" => "Description",
                "rating" => "Rating",
                "time" => "TimeInSeconds",
                _ => throw new InvalidUserQueryException("Incorrect query name! This should never appear!")
            };
            parameters.Add(parameter);
            return (name, parameter);
        }

        /// <summary>
        /// Split based on commas but accounting for strings
        /// </summary>
        private static List<string> SmartSplit(string original)
        {
            List<string> splits = [];

            bool inQuote = false;
            int splitStart = 0;
            int i;
            for (i = 0; i < original.Length; i++)
            {
                char c = original[i];
                if (c == '\\') // escape char (DOES NOT CHECK TYPE OF ESCAPE)
                {
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    inQuote = !inQuote;
                    continue;
                }
                if (inQuote) continue;
                if (c == ',')
                {
                    splits.Add(original[splitStart..i].Trim());
                    splitStart = i + 1;
                    continue;
                }
            }

            splits.Add(original[splitStart..i].Trim());
            return splits;
        }
    }

    public class InvalidUserQueryException : Exception
    {
        public InvalidUserQueryException(string message) : base(message) { }
    }
}

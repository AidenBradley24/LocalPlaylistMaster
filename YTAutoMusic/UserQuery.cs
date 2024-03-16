using System.Data.SQLite;

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
        private string sql;
        private string? queryText;
        private bool valid = false;

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

        private enum ParseMode { quer, any_val, int_val, str_val, int_oper, str_oper, id, between_start, between_end }

        public void Parse(string query)
        {
            valid = false;
            queryText = query.ToLowerInvariant();
            string[] sections = queryText.Split(',').Select(t => t.Trim()).ToArray(); // TODO also make sure commas can be in strings
            List<SQLiteParameter> parameters = new();
            List<string> finalSections = new();

            foreach (string section in sections)
            {
                List<string> tokens = new();

                // Checking for operators and filling in terms
                string current = "";

                ParseMode mode = ParseMode.quer;
                bool escapeChar = false;
                for (int i = 0; i < section.Length; i++)
                {
                    char c = section[i];
                    char? n = i < section.Length - 1 ? section[i + 1] : null;

                    if (mode == 0 && char.IsDigit(c))
                    {
                        mode = ParseMode.id;
                        tokens.Add("id");
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
                            mode = ParseMode.any_val;
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
                                mode = ParseMode.any_val;
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
                            current = $"{c}";
                            if (n == '=') 
                            {
                                current += "=";
                                i++;
                            } 
                            if(mode != ParseMode.int_oper)
                            {
                                throw new InvalidUserQueryException($"`{current}` operator is int-only");
                            }
                            tokens.Add(current);
                            mode = ParseMode.int_val;
                            continue;
                        case ':':
                            StartOperator();
                            if (mode != ParseMode.int_val)
                            {
                                throw new InvalidUserQueryException($"`:` operator is int-only");
                            }
                            mode = ParseMode.between_start;
                            tokens.Add(":");
                            continue;
                        case '-':
                            tokens.Add(current);
                            current = "";
                            if (mode != ParseMode.between_start && mode != ParseMode.id)
                            {
                                throw new InvalidUserQueryException("`-` operator is not used correctly");
                            }
                            tokens.Add("-");
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

                if (!string.IsNullOrWhiteSpace(current))
                {
                    tokens.Add(current);
                }
            }

            sql = string.Join("AND ", finalSections);
        }
    }

    public class InvalidUserQueryException : Exception
    {
        public InvalidUserQueryException(string message) : base(message) { }
    }
}

namespace MiniRedis.Infra;

public static class ArgSplitter
{
    public static List<string> Split(string input)
    {
        var result = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"') 
            { 
                inQuotes = !inQuotes; continue; 
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            { 
                if (sb.Length > 0) 
                { 
                    result.Add(sb.ToString()); sb.Clear(); 
                } 
            }
            else sb.Append(c);
        }
        
        if (sb.Length > 0) 
            result.Add(sb.ToString());
        return result;
    }
}
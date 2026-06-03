using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace SimpleNorthwind.Infrastructure.Persistence;

/// <summary>
/// 以 reflection / <c>nameof</c> 由 Domain 實體屬性推導 SQL 欄位名（PascalCase → snake_case），
/// 取代硬寫的欄位字串。重新命名實體屬性時，<c>nameof(...)</c> 會在編譯期跟著變，SQL 不會悄悄走鐘。
/// </summary>
internal static class SqlNaming
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();

    /// <summary>PascalCase 屬性名 → snake_case 欄位名（例：<c>CompanyName</c> → <c>company_name</c>）。</summary>
    public static string Col(string propertyName) =>
        Cache.GetOrAdd(propertyName, static name =>
        {
            var sb = new StringBuilder(name.Length + 8);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                        sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        });

    /// <summary>多個屬性名 → 逗號分隔的 snake_case 欄位清單。</summary>
    public static string Cols(params string[] propertyNames) => string.Join(", ", propertyNames.Select(Col));

    /// <summary>多個屬性名 → 逗號分隔的 Dapper 參數清單（<c>@PropertyName</c>）。</summary>
    public static string Params(params string[] propertyNames) => string.Join(", ", propertyNames.Select(p => "@" + p));
}

/// <summary>
/// 快取某實體「全部屬性」對應的 snake_case 欄位清單，供 <c>SELECT</c> 使用（point 5：reflection 取代 hardcode）。
/// </summary>
internal static class EntityColumns<TEntity>
{
    /// <summary>全欄位（依屬性宣告順序），逗號分隔。</summary>
    public static readonly string All = string.Join(", ",
        typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => SqlNaming.Col(p.Name)));
}

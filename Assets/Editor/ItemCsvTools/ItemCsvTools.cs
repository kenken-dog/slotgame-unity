using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CSV から ItemDefinition（ScriptableObject）を作成/更新するツール。
/// - 差分更新（id で既存アセットを特定）
/// - 入力検証（重複、必須列、型、参照、効果パラメータ）
/// - ItemId enum の自動拡張（ItemDefinition.cs 内の enum ItemId に未定義 id を追記）
/// </summary>
public static class ItemCsvTools
{
    // CSV のデフォルト配置先（プロジェクト内で管理しやすい場所）
    private const string DefaultCsvPath = "Assets/_Game/Items/ItemCatalog/items.csv";

    // 生成/更新したいアセットのデフォルト格納先（新規作成時）
    private const string DefaultDefinitionsDir = "Assets/_Game/Items/Definitions/AllItems";

    private const string ItemDefinitionEnumFilePath = "Assets/_Game/Items/ItemDefinition.cs";

    // Supported effect types (CSV effectType)
    private const string EffectMultiplySymbolWeight = "MultiplySymbolWeight";

    // CSV columns (必要十分)
    private static readonly string[] Columns =
    {
        "id","assetName","displayName","description","rarity","dropWeight","iconPath","effectType","targetSymbol","multiplier"
    };

    [MenuItem("Tools/Items/CSV/Import or Update (Default Path)")]
    public static void ImportDefault()
    {
        ImportFromPath(DefaultCsvPath);
    }

    [MenuItem("Tools/Items/CSV/Import or Update (Choose CSV...)")]
    public static void ImportChoose()
    {
        var path = EditorUtility.OpenFilePanel("Select items.csv", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(path)) return;

        // Convert absolute path to project-relative path
        var projectPath = Application.dataPath.Replace("/Assets", "");
        path = path.Replace("\\", "/");
        projectPath = projectPath.Replace("\\", "/");

        if (!path.StartsWith(projectPath, StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog("Invalid Path", "CSV must be inside this Unity project folder.", "OK");
            return;
        }

        var relative = "Assets" + path.Substring(projectPath.Length);
        ImportFromPath(relative);
    }

    [MenuItem("Tools/Items/CSV/Export Existing to CSV (Template)")]
    public static void ExportExistingTemplate()
    {
        var savePath = EditorUtility.SaveFilePanel(
            "Export items CSV",
            Application.dataPath,
            "items_export.csv",
            "csv"
        );
        if (string.IsNullOrEmpty(savePath)) return;

        var rows = CollectExistingRows();

        using (var sw = new StreamWriter(savePath, false, new UTF8Encoding(true)))
        {
            sw.WriteLine(string.Join(",", Columns));
            foreach (var r in rows)
            {
                sw.WriteLine(ToCsvLine(r));
            }
        }

        EditorUtility.RevealInFinder(savePath);
        Debug.Log($"[ItemCsvTools] Exported {rows.Count} items to: {savePath}");
    }

    [MenuItem("Tools/Items/CSV/Validate CSV (Default Path)")]
    public static void ValidateDefault()
    {
        ValidateCsvAtPath(DefaultCsvPath, showDialog: true);
    }

    private static void ImportFromPath(string csvPath)
    {
        if (!ValidateCsvAtPath(csvPath, showDialog: true, out var parsedRows)) return;

        // Ensure enum contains all ids (optional but recommended for this codebase).
        EnsureItemIdEnumContains(parsedRows.Select(r => r.Id).Distinct().ToList());

        // Find existing ItemDefinition assets by id
        var existing = LoadExistingItemDefinitionsById();

        int created = 0;
        int updated = 0;

        // Ensure destination directory exists
        if (!AssetDatabase.IsValidFolder(DefaultDefinitionsDir))
        {
            Directory.CreateDirectory(DefaultDefinitionsDir);
            AssetDatabase.Refresh();
        }

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var row in parsedRows)
            {
                if (!TryResolveItemType(row.EffectType, out var itemType, out var reason))
                {
                    Debug.LogError($"[ItemCsvTools] Unsupported effectType '{row.EffectType}' for id={row.Id}. {reason}");
                    continue;
                }

                if (!TryParseItemId(row.Id, out var itemId))
                {
                    Debug.LogError($"[ItemCsvTools] Unknown ItemId '{row.Id}'. Import aborted for this row.");
                    continue;
                }

                // Create or update
                ItemDefinition def;
                if (existing.TryGetValue(itemId, out var found) && found != null)
                {
                    def = found;
                    updated++;
                }
                else
                {
                    def = (ItemDefinition)ScriptableObject.CreateInstance(itemType);
                    var assetName = string.IsNullOrEmpty(row.AssetName) ? row.Id : row.AssetName;
                    var assetPath = $"{DefaultDefinitionsDir}/{SanitizeFileName(assetName)}.asset";
                    AssetDatabase.CreateAsset(def, assetPath);
                    existing[itemId] = def;
                    created++;
                }

                ApplyCommonFields(def, row, itemId);
                ApplyEffectFields(def, row);

                EditorUtility.SetDirty(def);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[ItemCsvTools] Import complete. Created: {created}, Updated: {updated}");
        EditorUtility.DisplayDialog("Item CSV Import", $"Import complete.\nCreated: {created}\nUpdated: {updated}", "OK");
    }

    private static void ApplyCommonFields(ItemDefinition def, ItemCsvRow row, ItemId itemId)
    {
        def.id = itemId;
        def.displayName = row.DisplayName ?? "";
        def.description = row.Description ?? "";
        def.rarity = row.Rarity;
        def.dropWeight = row.DropWeight;

        if (!string.IsNullOrEmpty(row.IconPath))
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(row.IconPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[ItemCsvTools] iconPath not found or not a Sprite: '{row.IconPath}' (id={row.Id})");
            }
            else
            {
                def.icon = sprite;
            }
        }
    }

    private static void ApplyEffectFields(ItemDefinition def, ItemCsvRow row)
    {
        // For each item type, apply its specific fields
        if (def is MultiplySymbolWeightItem msw)
        {
            if (Enum.TryParse(row.TargetSymbol, out SymbolId symbol))
                msw.targetSymbol = symbol;

            if (float.TryParse(row.Multiplier, NumberStyles.Float, CultureInfo.InvariantCulture, out var mul))
                msw.multiplier = mul;
        }
        // Add more effect types here as you implement them.
    }

    private static bool ValidateCsvAtPath(string csvPath, bool showDialog)
    {
        return ValidateCsvAtPath(csvPath, showDialog, out _);
    }

    private static bool ValidateCsvAtPath(string csvPath, bool showDialog, out List<ItemCsvRow> rows)
    {
        rows = new List<ItemCsvRow>();

        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            var msg = $"CSV not found: {csvPath}\n\nExpected default path:\n{DefaultCsvPath}";
            Fail(msg, showDialog);
            return false;
        }

        try
        {
            using (var sr = new StreamReader(csvPath, Encoding.UTF8, true))
            using (var cr = new CsvReader(sr))
            {
                var header = cr.ReadHeader();
                var required = new[] { "id","displayName","description","rarity","effectType" };

                var missing = required.Where(r => !header.Contains(r)).ToList();
                if (missing.Count > 0)
                {
                    Fail($"Missing required columns: {string.Join(", ", missing)}", showDialog);
                    return false;
                }

                var lineNo = 1; // header line
                var ids = new HashSet<string>(StringComparer.Ordinal);
                var ok = true;

                while (cr.ReadRow())
                {
                    lineNo++;

                    var row = new ItemCsvRow
                    {
                        Id = cr.Get("id"),
                        AssetName = cr.GetOptional("assetName"),
                        DisplayName = cr.Get("displayName"),
                        Description = cr.Get("description"),
                        IconPath = cr.GetOptional("iconPath"),
                        EffectType = cr.Get("effectType"),
                        TargetSymbol = cr.GetOptional("targetSymbol"),
                        Multiplier = cr.GetOptional("multiplier"),
                        DropWeightRaw = cr.GetOptional("dropWeight"),
                        RarityRaw = cr.Get("rarity")
                    };

                    // Basic required checks
                    if (string.IsNullOrWhiteSpace(row.Id))
                    {
                        ok = false;
                        Debug.LogError($"[ItemCsvTools] Line {lineNo}: id is required.");
                        continue;
                    }

                    row.Id = row.Id.Trim();

                    if (!ids.Add(row.Id))
                    {
                        ok = false;
                        Debug.LogError($"[ItemCsvTools] Line {lineNo}: duplicate id '{row.Id}'.");
                        continue;
                    }

                    if (!IsValidCSharpIdentifier(row.Id))
                    {
                        ok = false;
                        Debug.LogError($"[ItemCsvTools] Line {lineNo}: id '{row.Id}' is not a valid C# identifier. Use letters/digits/_ and start with letter/_");
                    }

                    if (string.IsNullOrWhiteSpace(row.DisplayName))
                    {
                        ok = false;
                        Debug.LogError($"[ItemCsvTools] Line {lineNo}: displayName is required (id={row.Id}).");
                    }

                    if (string.IsNullOrWhiteSpace(row.Description))
                    {
                        ok = false;
                        Debug.LogError($"[ItemCsvTools] Line {lineNo}: description is required (id={row.Id}).");
                    }

                    // rarity parse
                    if (!Enum.TryParse(row.RarityRaw, true, out ItemRarity rarity))
                    {
                        ok = false;
                        Debug.LogError($"[ItemCsvTools] Line {lineNo}: invalid rarity '{row.RarityRaw}' (id={row.Id}). Allowed: Common/Rare/Epic/Legendary");
                        continue;
                    }
                    row.Rarity = rarity;

                    // dropWeight parse
                    row.DropWeight = 0f;
                    if (!string.IsNullOrEmpty(row.DropWeightRaw))
                    {
                        if (!float.TryParse(row.DropWeightRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var dw) || dw < 0f)
                        {
                            ok = false;
                            Debug.LogError($"[ItemCsvTools] Line {lineNo}: invalid dropWeight '{row.DropWeightRaw}' (id={row.Id}). Must be >= 0.");
                        }
                        else row.DropWeight = dw;
                    }

                    // effectType validation
                    if (string.IsNullOrWhiteSpace(row.EffectType))
                    {
                        ok = false;
                        Debug.LogError($"[ItemCsvTools] Line {lineNo}: effectType is required (id={row.Id}).");
                        continue;
                    }

                    if (row.EffectType != EffectMultiplySymbolWeight)
                    {
                        ok = false;
                        Debug.LogError($"[ItemCsvTools] Line {lineNo}: unsupported effectType '{row.EffectType}' (id={row.Id}). Currently supported: {EffectMultiplySymbolWeight}");
                        continue;
                    }

                    // effect param validation (MultiplySymbolWeight)
                    if (row.EffectType == EffectMultiplySymbolWeight)
                    {
                        if (string.IsNullOrWhiteSpace(row.TargetSymbol) || !Enum.TryParse(row.TargetSymbol, out SymbolId _))
                        {
                            ok = false;
                            Debug.LogError($"[ItemCsvTools] Line {lineNo}: invalid targetSymbol '{row.TargetSymbol}' (id={row.Id}). Must be one of SymbolId enum values.");
                        }

                        if (string.IsNullOrWhiteSpace(row.Multiplier) ||
                            !float.TryParse(row.Multiplier, NumberStyles.Float, CultureInfo.InvariantCulture, out var mul) ||
                            mul <= 0f)
                        {
                            ok = false;
                            Debug.LogError($"[ItemCsvTools] Line {lineNo}: invalid multiplier '{row.Multiplier}' (id={row.Id}). Must be > 0 (use dot '.' for decimals).");
                        }
                    }

                    // iconPath validation (optional)
                    if (!string.IsNullOrEmpty(row.IconPath))
                    {
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(row.IconPath);
                        if (sprite == null)
                        {
                            // Not a hard error: allow importing text first
                            Debug.LogWarning($"[ItemCsvTools] Line {lineNo}: iconPath not found or not a Sprite: '{row.IconPath}' (id={row.Id}).");
                        }
                    }

                    rows.Add(row);
                }

                if (!ok)
                {
                    Fail($"CSV validation failed. Check Console for details.\nPath: {csvPath}", showDialog);
                    return false;
                }
            }

            Debug.Log($"[ItemCsvTools] CSV validated OK: {csvPath} (rows: {rows.Count})");
            if (showDialog) EditorUtility.DisplayDialog("CSV Validate", $"OK\nRows: {rows.Count}\n\n{csvPath}", "OK");
            return true;
        }
        catch (Exception ex)
        {
            Fail($"Failed to read CSV.\n{csvPath}\n\n{ex}", showDialog);
            return false;
        }
    }

    private static void Fail(string message, bool showDialog)
    {
        Debug.LogError("[ItemCsvTools] " + message);
        if (showDialog) EditorUtility.DisplayDialog("Item CSV Tools", message, "OK");
    }

    private static List<ItemCsvRow> CollectExistingRows()
    {
        var defs = LoadAllItemDefinitions();
        var rows = new List<ItemCsvRow>();

        foreach (var def in defs)
        {
            var row = new ItemCsvRow
            {
                Id = def.id.ToString(),
                AssetName = def.name,
                DisplayName = def.displayName,
                Description = def.description,
                IconPath = def.icon != null ? AssetDatabase.GetAssetPath(def.icon) : "",
                Rarity = def.rarity,
                DropWeight = def.dropWeight
            };

            if (def is MultiplySymbolWeightItem msw)
            {
                row.EffectType = EffectMultiplySymbolWeight;
                row.TargetSymbol = msw.targetSymbol.ToString();
                row.Multiplier = msw.multiplier.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                row.EffectType = "";
                row.TargetSymbol = "";
                row.Multiplier = "";
            }

            rows.Add(row);
        }

        // stable order
        return rows.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();
    }

    private static string ToCsvLine(ItemCsvRow r)
    {
        string Get(string col)
        {
            switch (col)
            {
                case "id": return r.Id ?? "";
                case "assetName": return r.AssetName ?? "";
                case "displayName": return r.DisplayName ?? "";
                case "description": return r.Description ?? "";
                case "rarity": return r.Rarity.ToString();
                case "dropWeight": return r.DropWeight <= 0f ? "" : r.DropWeight.ToString(CultureInfo.InvariantCulture);
                case "iconPath": return r.IconPath ?? "";
                case "effectType": return r.EffectType ?? "";
                case "targetSymbol": return r.TargetSymbol ?? "";
                case "multiplier": return r.Multiplier ?? "";
                default: return "";
            }
        }

        var values = Columns.Select(c => EscapeCsv(Get(c))).ToArray();
        return string.Join(",", values);
    }

    private static string EscapeCsv(string s)
    {
        s = s ?? "";
        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private static bool TryResolveItemType(string effectType, out Type type, out string reason)
    {
        reason = "";
        type = null;

        if (effectType == EffectMultiplySymbolWeight)
        {
            type = typeof(MultiplySymbolWeightItem);
            return true;
        }

        reason = "Add mapping in ItemCsvTools.TryResolveItemType().";
        return false;
    }

    private static Dictionary<ItemId, ItemDefinition> LoadExistingItemDefinitionsById()
    {
        var dict = new Dictionary<ItemId, ItemDefinition>();
        foreach (var def in LoadAllItemDefinitions())
        {
            if (!dict.ContainsKey(def.id)) dict.Add(def.id, def);
        }
        return dict;
    }

    private static List<ItemDefinition> LoadAllItemDefinitions()
    {
        var guids = AssetDatabase.FindAssets("t:ItemDefinition");
        var list = new List<ItemDefinition>();
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var def = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (def != null) list.Add(def);
        }
        return list;
    }

    private static bool TryParseItemId(string id, out ItemId itemId)
    {
        return Enum.TryParse(id, out itemId);
    }

    private static void EnsureItemIdEnumContains(List<string> csvIds)
    {
        // Read the enum source
        if (!File.Exists(ItemDefinitionEnumFilePath))
        {
            Debug.LogWarning($"[ItemCsvTools] Could not find {ItemDefinitionEnumFilePath}. Skipping ItemId enum update.");
            return;
        }

        var source = File.ReadAllText(ItemDefinitionEnumFilePath, Encoding.UTF8);
        var enumPattern = new System.Text.RegularExpressions.Regex(
            @"public\s+enum\s+ItemId\s*\{(?<body>[\s\S]*?)\}",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        var m = enumPattern.Match(source);
        if (!m.Success)
        {
            Debug.LogWarning("[ItemCsvTools] Could not locate 'public enum ItemId { ... }' in ItemDefinition.cs. Skipping enum update.");
            return;
        }

        var existingNames = Enum.GetNames(typeof(ItemId)).ToHashSet(StringComparer.Ordinal);

        // Determine which ids are missing
        var missing = csvIds
            .Select(x => (x ?? "").Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Where(x => !existingNames.Contains(x))
            .Where(IsValidCSharpIdentifier)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (missing.Count == 0) return;

        var body = m.Groups["body"].Value;

        // Append missing enum entries before closing brace
        var sb = new StringBuilder();
        sb.Append(body.TrimEnd());
        sb.AppendLine();
        foreach (var id in missing)
            sb.AppendLine($"    {id},");
        var newBody = "\n" + sb.ToString() + "\n";

        var newSource = enumPattern.Replace(source, $"public enum ItemId\n{{{newBody}}}");

        File.WriteAllText(ItemDefinitionEnumFilePath, newSource, Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"[ItemCsvTools] Added {missing.Count} missing ItemId enum entries. Unity will recompile scripts.");
    }

    private static bool IsValidCSharpIdentifier(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (!(char.IsLetter(s[0]) || s[0] == '_')) return false;
        for (int i = 1; i < s.Length; i++)
        {
            var c = s[i];
            if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
        }
        return true;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    // ---------- CSV reader ----------
    private sealed class CsvReader : IDisposable
    {
        private readonly TextReader _reader;
        private readonly Dictionary<string, int> _headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private string[] _current;

        public CsvReader(TextReader reader) => _reader = reader;

        public HashSet<string> ReadHeader()
        {
            var line = _reader.ReadLine();
            if (line == null) throw new InvalidDataException("CSV header is missing.");
            var cols = SplitCsvLine(line);
            for (int i = 0; i < cols.Length; i++)
                _headerMap[cols[i].Trim()] = i;
            return _headerMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public bool ReadRow()
        {
            var line = _reader.ReadLine();
            if (line == null) return false;
            _current = SplitCsvLine(line);
            return true;
        }

        public string Get(string column)
        {
            if (!_headerMap.TryGetValue(column, out var idx)) return "";
            if (_current == null || idx < 0 || idx >= _current.Length) return "";
            return (_current[idx] ?? "").Trim();
        }

        public string GetOptional(string column) => Get(column);

        public void Dispose() => _reader.Dispose();

        // Minimal CSV splitter (handles quoted fields with commas)
        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"'); // escaped quote
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Length = 0;
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString());
            return result.ToArray();
        }
    }

    private sealed class ItemCsvRow
    {
        public string Id;
        public string AssetName;
        public string DisplayName;
        public string Description;
        public string IconPath;

        public string EffectType;
        public string TargetSymbol;
        public string Multiplier;

        public string RarityRaw;
        public ItemRarity Rarity;

        public string DropWeightRaw;
        public float DropWeight;
    }
}

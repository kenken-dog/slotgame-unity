# items.csv（アイテム定義CSV）

このCSVを更新し、Unityメニューから Import することで `ItemDefinition`（ScriptableObject）を作成/更新できます。

## 列（必要十分）

- id (必須)  
  `ItemId` enum 名。例: LuckyCharm  
  ※未定義の id は Import 時に `ItemDefinition.cs` の `enum ItemId` へ自動追記されます。

- assetName (任意)  
  新規作成時の `.asset` ファイル名（拡張子なし）。未指定なら id を使用。

- displayName (必須)  
  UI表示名

- description (必須)  
  説明文

- rarity (必須)  
  Common / Rare / Epic / Legendary

- dropWeight (任意)  
  0 もしくは空欄なら rarity 既定値に従います。個別調整したい場合のみ指定（>=0）。

- iconPath (任意)  
  Sprite の Project 内パス。例: `Assets/_Game/Art/Sprites/Items/LuckyCharm.png`  
  ※未設定でも Import 自体は可能（後から差し替え可）

- effectType (必須)  
  いまは `MultiplySymbolWeight` のみ対応（必要になったら追加）。

- targetSymbol (effectType=MultiplySymbolWeight の場合必須)  
  `SymbolId` enum 名。例: Clover

- multiplier (effectType=MultiplySymbolWeight の場合必須)  
  0より大きい数値（小数は `.` 区切り）

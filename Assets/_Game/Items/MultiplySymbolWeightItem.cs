using UnityEngine;

[CreateAssetMenu(menuName = "Slot/Items/Multiply Symbol Weight")]
public class MultiplySymbolWeightItem : ItemDefinition
{
    [Header("Effect")]
    public SymbolId targetSymbol = SymbolId.Seven;
    public float multiplier = 2.0f;

    public override IItemEffect CreateEffect()
    {
        return new MultiplySymbolWeightEffect(targetSymbol, multiplier, displayName);
    }

    private class MultiplySymbolWeightEffect : IItemEffect
    {
        private readonly SymbolId _target;
        private readonly float _mul;
        public string DebugName { get; }

        public MultiplySymbolWeightEffect(SymbolId target, float mul, string name)
        {
            _target = target;
            _mul = Mathf.Max(0f, mul);
            DebugName = name;
        }

        public float GetWeightMultiplier(SymbolId symbol)
        {
            return symbol == _target ? _mul : 1f;
        }
    }
}

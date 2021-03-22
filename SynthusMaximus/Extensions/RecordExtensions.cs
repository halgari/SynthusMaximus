using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using System.Linq;
using Noggog;

namespace SynthusMaximus
{
    public static class RecordExtensions
    {
        public static bool HasAnyKeyword(this IKeywordedGetter<IKeywordGetter> a, params IFormLink<IKeywordGetter>[] kws)
        {
            return kws.Any(a.HasKeyword);
        }

        public static void AddCraftingRequirement(this ConstructibleObject cobj, IFormLink<IItemGetter> item, int count)
        {
            cobj.Items ??= new ExtendedList<ContainerEntry>();
            cobj.Items.Add(new ContainerEntry()
            {
                Item = new ContainerItem()
                {
                    Item = item,
                    Count = count
                }
            });
        }

        public static void AddCraftingInventoryCondition(this ConstructibleObject cobj, IFormLink<ISkyrimMajorRecordGetter> item, int count = 0)
        {
            cobj.Conditions.Add(new ConditionFloat
            {
                Data = new FunctionConditionData
                {
                    Function = ConditionData.Function.GetItemCount,
                    ParameterOneRecord = item
                },
                CompareOperator = CompareOperator.GreaterThanOrEqualTo,
                ComparisonValue = count
            });
        }
        
        public static void AddCraftingPerkCondition(this ConstructibleObject cobj, IFormLink<IPerkGetter> perk)
        {
            cobj.Conditions.Add(new ConditionFloat
            {
                Data = new FunctionConditionData
                {
                    Function = ConditionData.Function.HasPerk,
                    ParameterOneRecord = perk,
                },
                CompareOperator = CompareOperator.EqualTo,
                ComparisonValue = 1,
            });
        }
    }
}
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

        
        /// <summary>
        /// Adds a consumed item to the recipe 
        /// </summary>
        /// <param name="cobj"></param>
        /// <param name="item">Item to consume</param>
        /// <param name="count">Item count</param>
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

        /// <summary>
        /// Adds a condition to the recipe that an item exist in the player's inventory
        /// </summary>
        /// <param name="cobj"></param>
        /// <param name="item"></param>
        /// <param name="count"></param>
        public static void AddCraftingInventoryCondition(this ConstructibleObject cobj, IFormLink<ISkyrimMajorRecordGetter> item, int count = 1)
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
        
        /// <summary>
        /// Adds a condition to the recipe that the user have a given perk
        /// </summary>
        /// <param name="cobj"></param>
        /// <param name="perk"></param>
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